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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Dto.Repository.CaseAgentRun;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using LinqToDB;
using LinqToDB.Async;
using CaseExceptions = Jube.Service.Exceptions.Repository.Case;

namespace Jube.Service.Repository.CaseAgentRun
{
    public sealed class CaseAgentRunService
    {
        public const int CasePermission = 1;
        public const int MaximumReason = 500;
        private static readonly int[] permissions = [CasePermission];

        private readonly DbContext dbContext;
        private readonly string userName;
        private readonly int tenantRegistryId;
        private readonly PermissionValidation permissionValidation;
        private readonly ILog log;
        private readonly ILog auditLog;
        private readonly IServiceChangeBus serviceChangeBus;

        private CaseAgentRunService(DbContext dbContext, string userName, int tenantRegistryId,
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

        public static async Task<CaseAgentRunService> CreateAsync(DbContext dbContext, string? userName, ILog log,
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

            return new CaseAgentRunService(dbContext, userName, tenant.Value, validation, log,
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

        public async Task<List<CaseAgentRunDto>> ListAsync(int caseId, CancellationToken token = default)
        {
            Ensure();
            await EnsureVisibleAsync(caseId, token).ConfigureAwait(false);

            var runs = await new CaseAgentRunRepository(dbContext).ListForCaseAsync(caseId, 10, token)
                .ConfigureAwait(false);

            return
            [
                .. runs.Where(w => w.TenantRegistryId == tenantRegistryId).Select(r => new CaseAgentRunDto
                {
                    Guid = r.Guid,
                    RecommendedOutcomeId = r.RecommendedOutcomeId,
                    Rationale = r.Rationale,
                    Evidence = Evidence(r.Evidence),
                    StateId = r.StateId,
                    OverrideReason = r.OverrideReason,
                    DecidedUser = r.DecidedUser,
                    DecidedDate = r.DecidedDate is null
                        ? null
                        : new DateTimeOffset(DateTime.SpecifyKind(r.DecidedDate.Value, DateTimeKind.Utc)),
                    CreatedDate = new DateTimeOffset(DateTime.SpecifyKind(r.CreatedDate, DateTimeKind.Utc)),
                    ModelName = r.ModelName
                })
            ];
        }

        public async Task DecideAsync(CaseAgentRunDecisionDto? model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseAgentRun", "Decide", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                Ensure();

                var reason = string.IsNullOrWhiteSpace(model.Reason) ? null : model.Reason.Trim();

                if (!model.Accept && reason is null)
                {
                    throw new ArgumentException("A reason is required to override a recommendation.");
                }

                if (reason is { Length: > MaximumReason })
                {
                    throw new ArgumentException($"The reason must be at most {MaximumReason} characters.");
                }

                var repository = new CaseAgentRunRepository(dbContext);
                var run = await repository.GetByGuidAsync(model.Guid, token).ConfigureAwait(false);

                if (run is null || run.TenantRegistryId != tenantRegistryId)
                {
                    throw new CaseExceptions.NotFoundException("The recommendation was not found.");
                }

                await EnsureVisibleAsync(run.CaseId, token).ConfigureAwait(false);

                var state = model.Accept ? CaseAgentRunState.Accepted : CaseAgentRunState.Overridden;

                if (await repository.DecideAsync(run.Guid, (short)state, model.Accept ? null : reason, userName, token)
                        .ConfigureAwait(false) == 0)
                {
                    throw new CaseExceptions.ConflictException("The recommendation has already been decided or replaced.");
                }

                var theCase = await dbContext.Case.Where(c => c.Id == run.CaseId)
                    .Select(c => new { c.CaseKey, c.CaseKeyValue }).FirstOrDefaultAsync(token).ConfigureAwait(false);

                await new CaseEventRepository(dbContext, userName).InsertAsync(new Data.Poco.CaseEvent
                {
                    CaseId = run.CaseId,
                    CaseKey = theCase?.CaseKey,
                    CaseKeyValue = theCase?.CaseKeyValue,
                    CaseEventTypeId = model.Accept
                        ? CaseEventRepository.TriageAcceptedEventTypeId
                        : CaseEventRepository.TriageOverriddenEventTypeId,
                    Before = run.RecommendedOutcomeId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    After = model.Accept ? null : reason
                }, token).ConfigureAwait(false);

                if (!model.Accept)
                {
                    await Jube.Service.Query.CaseTriage.QaDisagreementSampling
                        .ApplyAsync(dbContext, userName, run.CaseId, token).ConfigureAwait(false);
                }

                op.Entity(run.CaseId);
                op.Updated();
            }
            catch (Exception ex)
            {
                op.Error(ex);

                throw;
            }
        }

        private static List<string> Evidence(string? json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return [];
            }

            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }
    }
}
