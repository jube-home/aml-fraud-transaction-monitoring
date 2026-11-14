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
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Service.Query.CaseChecklist.Interfaces;
using Jube.Service.Query.CaseChecklist.Models;
using LinqToDB;
using LinqToDB.Async;
using log4net;
using CaseRow = Jube.Data.Poco.Case;

namespace Jube.Service.Query.CaseChecklist
{
    public sealed class CaseChecklistGate(DbContext dbContext, ICaseChecklistReconciler reconciler,
        Func<DbContext> auditContextFactory, TimeProvider clock, ILog log) : ICaseChecklistGate
    {
        public async Task<IReadOnlyList<OpenChecklistItem>> EvaluateAsync(int caseId, string actor,
            CancellationToken token = default)
        {
            await reconciler.ReconcileAsync(caseId, actor, "Gate", token).ConfigureAwait(false);

            var caseRow = await dbContext.Case.FirstOrDefaultAsync(w => w.Id == caseId, token).ConfigureAwait(false);

            if (caseRow == null)
            {
                return [];
            }

            var tenantRegistryId = await TenantAsync(dbContext, caseRow, token).ConfigureAwait(false);

            if (!tenantRegistryId.HasValue)
            {
                return [];
            }

            var rows = await new CaseChecklistItemRepository(dbContext, tenantRegistryId.Value)
                .ListForCaseAsync(caseId, token).ConfigureAwait(false);

            return rows.Where(w => w.CaseWorkflowStatusGuid == caseRow.CaseWorkflowStatusGuid
                                   && w.StateId == (byte)CaseChecklistItemState.Shown)
                .Select(s => new OpenChecklistItem(s.Id, s.CaseWorkflowChecklistItemGuid, s.ItemName ?? string.Empty))
                .ToList();
        }

        public async Task<CaseChecklistOverride?> FindLiveOverrideAsync(int caseId, Guid statusGuid, string mover,
            CancellationToken token = default)
        {
            var caseRow = await dbContext.Case.FirstOrDefaultAsync(w => w.Id == caseId, token).ConfigureAwait(false);

            if (caseRow == null || caseRow.CaseWorkflowStatusGuid != statusGuid)
            {
                return null;
            }

            var tenantRegistryId = await TenantAsync(dbContext, caseRow, token).ConfigureAwait(false);

            if (!tenantRegistryId.HasValue)
            {
                return null;
            }

            var grant = await new CaseChecklistOverrideRepository(dbContext, tenantRegistryId.Value)
                .GetOpenAsync(caseId, statusGuid, token).ConfigureAwait(false);

            if (grant == null || string.Equals(grant.GrantedUser, mover, StringComparison.Ordinal))
            {
                return null;
            }

            var grantedDate = grant.GrantedDate;

            var moved = await dbContext.CaseStatusTransition
                .AnyAsync(w => w.CaseId == caseId && w.EnteredDate > grantedDate, token).ConfigureAwait(false);

            return moved ? null : grant;
        }

        public async Task RecordBlockedAsync(int caseId, string actor, IReadOnlyList<OpenChecklistItem> open,
            CancellationToken token = default)
        {
            try
            {
                await using var audit = auditContextFactory();

                var caseRow = await audit.Case.FirstOrDefaultAsync(w => w.Id == caseId, token).ConfigureAwait(false);

                if (caseRow == null)
                {
                    return;
                }

                var tenantRegistryId = await TenantAsync(audit, caseRow, token).ConfigureAwait(false);

                if (!tenantRegistryId.HasValue)
                {
                    return;
                }

                await new CaseChecklistAuditWriter(audit, tenantRegistryId.Value).WriteAsync(caseRow,
                    CaseChecklistAction.GateBlocked, actor, null, null,
                    $"The move was refused while {open.Count} item(s) were still open: " +
                    string.Join("; ", open.Select(s => s.Name)) + ".",
                    CaseChecklistService.GateBlockedEventTypeId, $"{open.Count} open",
                    clock.GetUtcNow().UtcDateTime, token).ConfigureAwait(false);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                log.Error($"CaseChecklist.Gate: the refusal of case {caseId} could not be audited.", e);
            }
        }

        public async Task ConsumeOverrideAsync(CaseChecklistOverride grant, string mover,
            CancellationToken token = default)
        {
            var caseRow = await dbContext.Case.FirstOrDefaultAsync(w => w.Id == grant.CaseId, token)
                .ConfigureAwait(false);

            if (caseRow == null)
            {
                return;
            }

            var tenantRegistryId = await TenantAsync(dbContext, caseRow, token).ConfigureAwait(false);

            if (!tenantRegistryId.HasValue)
            {
                return;
            }

            await new CaseChecklistOverrideRepository(dbContext, tenantRegistryId.Value)
                .ConsumeAsync(grant.Id, mover, clock.GetUtcNow().UtcDateTime, token).ConfigureAwait(false);
        }

        public async Task RecordBypassAsync(int caseId, string actor, CancellationToken token = default)
        {
            try
            {
                var open = await EvaluateAsync(caseId, actor, token).ConfigureAwait(false);

                if (open.Count == 0)
                {
                    return;
                }

                var caseRow = await dbContext.Case.FirstOrDefaultAsync(w => w.Id == caseId, token)
                    .ConfigureAwait(false);

                var tenantRegistryId = caseRow == null
                    ? null
                    : await TenantAsync(dbContext, caseRow, token).ConfigureAwait(false);

                if (caseRow == null || !tenantRegistryId.HasValue)
                {
                    return;
                }

                await new CaseChecklistAuditWriter(dbContext, tenantRegistryId.Value).WriteAsync(caseRow,
                    CaseChecklistAction.GateBypassed, actor, null, null,
                    $"{actor} moved the case with {open.Count} item(s) still open: " +
                    string.Join("; ", open.Select(s => s.Name)) + ".",
                    CaseChecklistService.GateBypassedEventTypeId, $"{open.Count} open",
                    clock.GetUtcNow().UtcDateTime, token).ConfigureAwait(false);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                log.Error($"CaseChecklist.Gate: the bypass of case {caseId} by {actor} could not be recorded.", e);
            }
        }

        private static Task<int?> TenantAsync(DbContext context, CaseRow caseRow, CancellationToken token)
        {
            var workflowGuid = caseRow.CaseWorkflowGuid;

            return (from w in context.CaseWorkflow
                where w.Guid == workflowGuid && (w.Deleted == 0 || w.Deleted == null)
                select w.EntityAnalysisModel.TenantRegistryId).FirstOrDefaultAsync(token);
        }
    }
}
