/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Dto.Repository.CaseQaReview;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using LinqToDB.Async;
using CaseExceptions = Jube.Service.Exceptions.Repository.Case;

namespace Jube.Service.Repository.CaseQaReview
{
    public sealed class CaseQaReviewService
    {
        public const int CasePermission = 1;
        public const int MaximumComment = 1000;
        private const byte ClosedStatus = 3;
        private const byte QaPicked = 2;
        private static readonly int[] permissions = [CasePermission];

        private readonly DbContext dbContext;
        private readonly string userName;
        private readonly int tenantRegistryId;
        private readonly PermissionValidation permissionValidation;
        private readonly ILog log;
        private readonly ILog auditLog;
        private readonly IServiceChangeBus serviceChangeBus;

        private CaseQaReviewService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
        }

        public static async Task<CaseQaReviewService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new CaseExceptions.NotAuthenticatedException("Not authenticated.");
            }

            var tenant = await UserInTenantRepository.GetTenantRegistryIdAsync(dbContext, userName, token)
                .ConfigureAwait(false);

            if (tenant is null)
            {
                throw new CaseExceptions.NotAuthenticatedException("Not authenticated.");
            }

            var validation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseQaReviewService(dbContext, userName, tenant.Value, validation, log,
                LogManager.GetLogger("Jube.Audit"), serviceChangeBus);
        }

        private void Ensure()
        {
            if (!permissionValidation.Validate(permissions))
            {
                throw new CaseExceptions.ForbiddenException("Permission denied.", permissions);
            }
        }

        private async Task EnsureVisibleAsync(int caseId, CancellationToken token)
        {
            var visible = await new CaseRepository(dbContext, userName)
                .GetVisibleIdsActiveOnlyAsync([caseId], token).ConfigureAwait(false);

            if (visible.Count == 0)
            {
                throw new CaseExceptions.NotFoundException("The case was not found.");
            }
        }

        public async Task<CaseQaReviewStateDto> ListAsync(int caseId, CancellationToken token = default)
        {
            Ensure();
            await EnsureVisibleAsync(caseId, token).ConfigureAwait(false);

            var reviews = await new CaseQaReviewRepository(dbContext).ListForCaseAsync(caseId, 20, token)
                .ConfigureAwait(false);

            var target = await dbContext.Case.Where(c => c.Id == caseId)
                .Select(c => new { c.ClosedStatusId, c.ClosedUser, c.QaSampleStatusId })
                .FirstOrDefaultAsync(token).ConfigureAwait(false);

            var hasRecommendation = (await new CaseAgentRunRepository(dbContext).ListForCaseAsync(caseId, 10, token)
                .ConfigureAwait(false)).Any(r => r.StateId != (short)CaseAgentRunState.Superseded);

            return new CaseQaReviewStateDto
            {
                Eligible = target is { QaSampleStatusId: QaPicked, ClosedStatusId: ClosedStatus }
                           && !string.Equals(target.ClosedUser, userName, StringComparison.OrdinalIgnoreCase),
                HasRecommendation = hasRecommendation,
                Reviews =
            [
                .. reviews.Where(r => r.TenantRegistryId == tenantRegistryId).Select(r => new CaseQaReviewDto
                {
                    CaseId = r.CaseId,
                    AnalystVerdictId = r.AnalystVerdictId,
                    AgentVerdictId = r.AgentVerdictId,
                    Comment = r.Comment,
                    ReviewerUser = r.ReviewerUser,
                    CreatedDate = new DateTimeOffset(DateTime.SpecifyKind(r.CreatedDate, DateTimeKind.Utc))
                })
            ]
            };
        }

        public async Task SubmitAsync(CaseQaReviewDto? model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseQaReview", "Submit", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                Ensure();
                Check(model);
                await EnsureVisibleAsync(model.CaseId, token).ConfigureAwait(false);

                var target = await dbContext.Case.Where(c => c.Id == model.CaseId)
                    .Select(c => new { c.ClosedStatusId, c.ClosedUser, c.QaSampleStatusId })
                    .FirstOrDefaultAsync(token).ConfigureAwait(false);

                if (target is null)
                {
                    throw new CaseExceptions.NotFoundException("The case was not found.");
                }

                if (target.QaSampleStatusId != QaPicked)
                {
                    throw new ArgumentException("The case was not picked for quality review.");
                }

                if (target.ClosedStatusId != ClosedStatus)
                {
                    throw new ArgumentException("Only a closed case can be reviewed.");
                }

                if (string.Equals(target.ClosedUser, userName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new CaseExceptions.ConflictException("A case cannot be reviewed by the user who closed it.");
                }

                Guid? run = null;

                if (model.AgentVerdictId is not null)
                {
                    var latest = (await new CaseAgentRunRepository(dbContext).ListForCaseAsync(model.CaseId, 10, token)
                        .ConfigureAwait(false)).FirstOrDefault(r => r.StateId != (short)CaseAgentRunState.Superseded);

                    if (latest is null)
                    {
                        throw new ArgumentException("There was no triage recommendation to judge.");
                    }

                    run = latest.Guid;
                }

                await new CaseQaReviewRepository(dbContext).InsertAsync(new Data.Poco.CaseQaReview
                {
                    Guid = Guid.NewGuid(),
                    TenantRegistryId = tenantRegistryId,
                    CaseId = model.CaseId,
                    ReviewerUser = userName,
                    AnalystVerdictId = (short)model.AnalystVerdictId,
                    AgentVerdictId = (short?)model.AgentVerdictId,
                    CaseAgentRunGuid = run,
                    Comment = string.IsNullOrWhiteSpace(model.Comment) ? null : model.Comment.Trim(),
                    CreatedDate = DateTime.UtcNow
                }, token).ConfigureAwait(false);

                op.Entity(model.CaseId);
                op.Created();
            }
            catch (Exception ex)
            {
                op.Error(ex);

                throw;
            }
        }

        private static void Check(CaseQaReviewDto model)
        {
            if (model.CaseId <= 0)
            {
                throw new ArgumentException("The case is required.");
            }

            if (!Enum.IsDefined((CaseQaVerdict)model.AnalystVerdictId))
            {
                throw new ArgumentException("The analyst verdict must be 1, 2 or 3.");
            }

            if (model.AgentVerdictId is { } agent && !Enum.IsDefined((CaseQaVerdict)agent))
            {
                throw new ArgumentException("The agent verdict must be 1, 2 or 3.");
            }

            if (model.Comment is { Length: > MaximumComment })
            {
                throw new ArgumentException($"The comment must be at most {MaximumComment} characters.");
            }
        }
    }
}
