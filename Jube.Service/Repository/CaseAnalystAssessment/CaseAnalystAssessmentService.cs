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
using System.Globalization;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Dto.Repository.CaseAnalystAssessment;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using LinqToDB.Async;
using CaseExceptions = Jube.Service.Exceptions.Repository.Case;

namespace Jube.Service.Repository.CaseAnalystAssessment
{
    public sealed class CaseAnalystAssessmentService
    {
        public const int CasePermission = 1;
        public const int MaximumReason = 1000;
        private const int HistoryTake = 20;
        private static readonly int[] permissions = [CasePermission];

        private readonly DbContext dbContext;
        private readonly string userName;
        private readonly int tenantRegistryId;
        private readonly PermissionValidation permissionValidation;
        private readonly ILog log;
        private readonly ILog auditLog;
        private readonly IServiceChangeBus serviceChangeBus;

        private CaseAnalystAssessmentService(DbContext dbContext, string userName, int tenantRegistryId,
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

        public static async Task<CaseAnalystAssessmentService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IServiceChangeBus serviceChangeBus, CancellationToken token = default)
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

            return new CaseAnalystAssessmentService(dbContext, userName, tenant.Value, validation, log,
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

        public async Task<CaseAnalystAssessmentStateDto> ListAsync(int caseId, CancellationToken token = default)
        {
            Ensure();
            await EnsureVisibleAsync(caseId, token).ConfigureAwait(false);

            var rows = await new CaseAnalystAssessmentRepository(dbContext)
                .ListForCaseAsync(caseId, tenantRegistryId, HistoryTake, token).ConfigureAwait(false);

            var history = rows.Select(Map).ToList();

            return new CaseAnalystAssessmentStateDto
            {
                Current = history.FirstOrDefault(h => h.WithdrawnDate is null),
                History = history
            };
        }

        public async Task<CaseAnalystAssessmentDto> RecordAsync(CaseAnalystAssessmentDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseAnalystAssessment", "Record", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                Ensure();
                Check(model);
                await EnsureVisibleAsync(model.CaseId, token).ConfigureAwait(false);

                var computed = await dbContext.Case.Where(c => c.Id == model.CaseId)
                    .Select(c => new { c.RiskSequence, c.RiskLikelihoodProbability, c.CaseKey, c.CaseKeyValue })
                    .FirstOrDefaultAsync(token).ConfigureAwait(false);

                var repository = new CaseAnalystAssessmentRepository(dbContext);
                var standing = await repository.GetStandingLikelihoodAsync(model.CaseId, tenantRegistryId, token)
                    .ConfigureAwait(false);

                var row = new Data.Poco.CaseAnalystAssessment
                {
                    Guid = Guid.NewGuid(),
                    TenantRegistryId = tenantRegistryId,
                    CaseId = model.CaseId,
                    LikelihoodId = (short)model.LikelihoodId,
                    Reason = model.Reason!.Trim(),
                    ComputedSequence = computed?.RiskSequence,
                    ComputedLikelihoodProbability = computed?.RiskLikelihoodProbability,
                    CreatedUser = userName,
                    CreatedDate = DateTime.UtcNow
                };

                await repository.InsertAsync(row, token).ConfigureAwait(false);

                await new CaseEventRepository(dbContext, userName).InsertAsync(new Data.Poco.CaseEvent
                {
                    CaseId = model.CaseId,
                    CaseKey = computed?.CaseKey,
                    CaseKeyValue = computed?.CaseKeyValue,
                    CaseEventTypeId = CaseEventRepository.AnalystAssessmentRecordedEventTypeId,
                    Before = standing?.ToString(CultureInfo.InvariantCulture),
                    After = row.LikelihoodId.ToString(CultureInfo.InvariantCulture)
                }, token).ConfigureAwait(false);

                op.Entity(model.CaseId);
                op.Created();

                return Map(row);
            }
            catch (Exception ex)
            {
                op.Error(ex);

                throw;
            }
        }

        public async Task WithdrawAsync(CaseAnalystAssessmentWithdrawDto? model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseAnalystAssessment", "Withdraw", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                Ensure();

                var target = await dbContext.CaseAnalystAssessment
                    .Where(w => w.Guid == model.Guid && w.TenantRegistryId == tenantRegistryId)
                    .Select(w => new { w.CaseId, w.CreatedUser, w.WithdrawnDate, w.LikelihoodId })
                    .FirstOrDefaultAsync(token).ConfigureAwait(false);

                if (target is null)
                {
                    throw new CaseExceptions.NotFoundException("The assessment was not found.");
                }

                await EnsureVisibleAsync(target.CaseId, token).ConfigureAwait(false);

                if (!string.Equals(target.CreatedUser, userName, StringComparison.Ordinal))
                {
                    throw new CaseExceptions.ConflictException("Only the analyst who recorded an assessment can withdraw it.");
                }

                var withdrawn = target.WithdrawnDate is null
                    ? await new CaseAnalystAssessmentRepository(dbContext)
                        .WithdrawAsync(model.Guid, tenantRegistryId, userName, DateTime.UtcNow, token)
                        .ConfigureAwait(false)
                    : 0;

                if (withdrawn > 0)
                {
                    var key = await dbContext.Case.Where(c => c.Id == target.CaseId)
                        .Select(c => new { c.CaseKey, c.CaseKeyValue })
                        .FirstOrDefaultAsync(token).ConfigureAwait(false);

                    await new CaseEventRepository(dbContext, userName).InsertAsync(new Data.Poco.CaseEvent
                    {
                        CaseId = target.CaseId,
                        CaseKey = key?.CaseKey,
                        CaseKeyValue = key?.CaseKeyValue,
                        CaseEventTypeId = CaseEventRepository.AnalystAssessmentWithdrawnEventTypeId,
                        Before = target.LikelihoodId.ToString(CultureInfo.InvariantCulture)
                    }, token).ConfigureAwait(false);
                }

                op.Entity(target.CaseId);
                op.Updated();
            }
            catch (Exception ex)
            {
                op.Error(ex);

                throw;
            }
        }

        private CaseAnalystAssessmentDto Map(Data.Poco.CaseAnalystAssessment row)
        {
            return new CaseAnalystAssessmentDto
            {
                Guid = row.Guid,
                CaseId = row.CaseId,
                LikelihoodId = row.LikelihoodId,
                Reason = row.Reason,
                ComputedLikelihoodProbability = row.ComputedLikelihoodProbability,
                CreatedUser = row.CreatedUser,
                CreatedDate = new DateTimeOffset(DateTime.SpecifyKind(row.CreatedDate, DateTimeKind.Utc)),
                WithdrawnDate = row.WithdrawnDate is { } withdrawn
                    ? new DateTimeOffset(DateTime.SpecifyKind(withdrawn, DateTimeKind.Utc))
                    : null,
                Mine = string.Equals(row.CreatedUser, userName, StringComparison.Ordinal)
            };
        }

        private static void Check(CaseAnalystAssessmentDto model)
        {
            if (model.CaseId <= 0)
            {
                throw new ArgumentException("The case is required.");
            }

            if (!Enum.IsDefined((CaseAnalystLikelihood)model.LikelihoodId))
            {
                throw new ArgumentException("The likelihood must be 1, 2 or 3.");
            }

            if (string.IsNullOrWhiteSpace(model.Reason))
            {
                throw new ArgumentException("A reason is required.");
            }

            if (model.Reason.Trim().Length > MaximumReason)
            {
                throw new ArgumentException($"The reason must be at most {MaximumReason} characters.");
            }
        }
    }
}
