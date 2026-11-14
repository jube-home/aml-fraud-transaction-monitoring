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
using CaseRow = Jube.Data.Poco.Case;
using Jube.Data.Query.GetCaseChecklistDefinitionsQuery;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Service.Query.CaseChecklist.Interfaces;
using Jube.Service.Query.CaseChecklist.Models;
using Jube.Service.Repository.CaseWorkflowFormValidator.Interfaces;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using log4net;

namespace Jube.Service.Query.CaseChecklist
{
    public sealed class CaseChecklistReconciler(DbContext dbContext, IFormSqlRunner sqlRunner, TimeProvider clock,
        ILog log) : ICaseChecklistReconciler
    {
        private const int AdvisoryLockNamespace = 20263108;
        private const int ClosedStatusId = 3;
        private static readonly TimeSpan sqlTimeout = TimeSpan.FromSeconds(3);

        public async Task<int> ReconcileIfNeededAsync(int caseId, string actor, string trigger, TimeSpan maxAge,
            CancellationToken token = default)
        {
            var current = await dbContext.Case.Where(w => w.Id == caseId)
                .Select(s => new { s.ClosedStatusId, s.ChecklistReconciledDate, s.ChecklistStaleDate })
                .FirstOrDefaultAsync(token).ConfigureAwait(false);

            if (current == null || current.ClosedStatusId == ClosedStatusId)
            {
                return 0;
            }

            var now = clock.GetUtcNow().UtcDateTime;
            var reconciled = current.ChecklistReconciledDate;

            var needed = !reconciled.HasValue
                         || maxAge <= TimeSpan.Zero
                         || (current.ChecklistStaleDate.HasValue && current.ChecklistStaleDate.Value > reconciled.Value)
                         || now - reconciled.Value > maxAge;

            return needed ? await ReconcileAsync(caseId, actor, trigger, token).ConfigureAwait(false) : 0;
        }

        public async Task<int> ReconcileAsync(int caseId, string actor, string trigger,
            CancellationToken token = default)
        {
            var caseRow = await dbContext.Case.FirstOrDefaultAsync(w => w.Id == caseId, token)
                .ConfigureAwait(false);

            if (caseRow == null || caseRow.ClosedStatusId == ClosedStatusId)
            {
                return 0;
            }

            var workflowGuid = caseRow.CaseWorkflowGuid;

            var tenantRegistryId = await (from w in dbContext.CaseWorkflow
                where w.Guid == workflowGuid && (w.Deleted == 0 || w.Deleted == null)
                select w.EntityAnalysisModel.TenantRegistryId).FirstOrDefaultAsync(token).ConfigureAwait(false);

            if (!tenantRegistryId.HasValue)
            {
                return 0;
            }

            var ownsTransaction = dbContext.Transaction == null;

            if (ownsTransaction)
            {
                await dbContext.BeginTransactionAsync(token).ConfigureAwait(false);
            }

            try
            {
                await dbContext.ExecuteAsync("select pg_advisory_xact_lock(@lockNamespace, @lockCaseId)", token,
                    new DataParameter("lockNamespace", AdvisoryLockNamespace, DataType.Int32),
                    new DataParameter("lockCaseId", caseId, DataType.Int32)).ConfigureAwait(false);

                caseRow = await dbContext.Case.FirstOrDefaultAsync(w => w.Id == caseId, token).ConfigureAwait(false);

                if (caseRow == null || caseRow.ClosedStatusId == ClosedStatusId)
                {
                    await CommitAsync(ownsTransaction, token).ConfigureAwait(false);
                    return 0;
                }

                var changes = await ApplyAsync(caseRow, tenantRegistryId.Value, actor, trigger, token)
                    .ConfigureAwait(false);

                await CommitAsync(ownsTransaction, token).ConfigureAwait(false);

                return changes;
            }
            catch
            {
                if (ownsTransaction)
                {
                    await dbContext.RollbackTransactionAsync(CancellationToken.None).ConfigureAwait(false);
                }

                throw;
            }
        }

        private async Task CommitAsync(bool ownsTransaction, CancellationToken token)
        {
            if (ownsTransaction)
            {
                await dbContext.CommitTransactionAsync(token).ConfigureAwait(false);
            }
        }

        private async Task<int> ApplyAsync(CaseRow caseRow, int tenantRegistryId, string actor, string trigger,
            CancellationToken token)
        {
            var now = clock.GetUtcNow().UtcDateTime;
            var items = new CaseChecklistItemRepository(dbContext, tenantRegistryId);
            var events = new CaseChecklistEventRepository(dbContext, tenantRegistryId);

            var definitions = await new GetCaseChecklistDefinitionsQuery(dbContext)
                .ExecuteAsync(caseRow.CaseWorkflowStatusGuid, token).ConfigureAwait(false);

            var existing = await items.ListForCaseAsync(caseRow.Id, token).ConfigureAwait(false);

            var planDefinitions = new List<ChecklistPlanDefinition>();

            foreach (var definition in definitions)
            {
                var outcome = await EvaluateAsync(definition, caseRow, token).ConfigureAwait(false);

                planDefinitions.Add(new ChecklistPlanDefinition(definition.CaseWorkflowChecklistItemGuid,
                    definition.CaseWorkflowChecklistGuid, definition.CaseWorkflowStatusGuid,
                    definition.ChecklistName, definition.ItemName, definition.ItemPriority,
                    definition.SlaMinutes, UsesShowSql(definition), outcome));
            }

            var operations = CaseChecklistReconcilePlan.Compute(caseRow.CaseWorkflowStatusGuid, planDefinitions,
                existing.Select(e => new ChecklistPlanExisting(e.Id, e.CaseWorkflowChecklistItemGuid,
                    e.CaseWorkflowStatusGuid, e.ItemName, (CaseChecklistItemState)e.StateId)).ToList(), now);

            var rowsById = existing.ToDictionary(e => e.Id);

            foreach (var operation in operations)
            {
                await ApplyOperationAsync(operation, caseRow, rowsById, items, events, actor, trigger, now, token)
                    .ConfigureAwait(false);
            }

            var evaluatedIds = planDefinitions.Where(d => d.Show != ChecklistShowOutcome.Error)
                .Select(d => existing.FirstOrDefault(e => e.CaseWorkflowChecklistItemGuid == d.ItemGuid)?.Id)
                .Where(id => id.HasValue).Select(id => id!.Value).ToList();

            if (evaluatedIds.Count > 0)
            {
                await dbContext.CaseChecklistItem.Where(w => evaluatedIds.Contains(w.Id))
                    .Set(s => s.LastEvaluatedDate, now).UpdateAsync(token).ConfigureAwait(false);
            }

            var breaches = await new CaseChecklistSlaBreachRecorder(dbContext, clock, log)
                .RecordOpenBreachesAsync(caseRow, tenantRegistryId, now, token).ConfigureAwait(false);

            await dbContext.Case.Where(w => w.Id == caseRow.Id).Set(s => s.ChecklistReconciledDate, now)
                .UpdateAsync(token).ConfigureAwait(false);

            return operations.Count + breaches;
        }

        private static bool UsesShowSql(GetCaseChecklistDefinitionsQueryDto definition)
        {
            return definition.EnableShow && !string.IsNullOrWhiteSpace(definition.ShowSql);
        }

        private async Task<ChecklistShowOutcome> EvaluateAsync(GetCaseChecklistDefinitionsQueryDto definition,
            CaseRow caseRow, CancellationToken token)
        {
            if (!UsesShowSql(definition))
            {
                return ChecklistShowOutcome.Shown;
            }

            var parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = caseRow.Id, ["CaseKeyValue"] = caseRow.CaseKeyValue ?? string.Empty
            };

            using var limit = CancellationTokenSource.CreateLinkedTokenSource(token);

            limit.CancelAfter(sqlTimeout);

            try
            {
                return await sqlRunner.PassesAsync(definition.ShowSql, parameters, limit.Token)
                    .ConfigureAwait(false)
                    ? ChecklistShowOutcome.Shown
                    : ChecklistShowOutcome.NotShown;
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                Warn(definition, caseRow, "timed out", null);
                return ChecklistShowOutcome.Error;
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                Warn(definition, caseRow, "failed", e);
                return ChecklistShowOutcome.Error;
            }
        }

        private void Warn(GetCaseChecklistDefinitionsQueryDto definition, CaseRow caseRow, string what, Exception? e)
        {
            if (log.IsWarnEnabled)
            {
                log.Warn(
                    $"CaseChecklist.Reconcile: show SQL {what} caseId={caseRow.Id} item={definition.CaseWorkflowChecklistItemGuid}",
                    e);
            }
        }

        private async Task ApplyOperationAsync(ChecklistPlanOperation operation, CaseRow caseRow,
            Dictionary<long, CaseChecklistItem> rowsById, CaseChecklistItemRepository items,
            CaseChecklistEventRepository events, string actor, string trigger, DateTime now,
            CancellationToken token)
        {
            CaseChecklistItem row;
            CaseChecklistAction action;
            string detail;

            switch (operation.Kind)
            {
                case ChecklistPlanOperationKind.Insert:
                    var definition = operation.Definition!;

                    row = await items.InsertAsync(new CaseChecklistItem
                    {
                        CaseId = caseRow.Id,
                        CaseWorkflowChecklistItemGuid = definition.ItemGuid,
                        CaseWorkflowChecklistGuid = definition.ChecklistGuid,
                        CaseWorkflowStatusGuid = definition.StatusGuid,
                        ChecklistName = definition.ChecklistName,
                        ItemName = definition.ItemName,
                        Priority = definition.Priority,
                        StateId = (byte)CaseChecklistItemState.Shown,
                        FirstShownDate = operation.ShownDate!.Value,
                        ShownDate = operation.ShownDate.Value,
                        DueDate = operation.DueDate,
                        LastEvaluatedDate = now,
                        CreatedDate = now
                    }, token).ConfigureAwait(false);

                    action = CaseChecklistAction.Shown;
                    detail = ShownDetail(trigger, definition.Conditional);
                    break;
                case ChecklistPlanOperationKind.Show:
                    row = rowsById[operation.ExistingId!.Value];
                    row.StateId = (byte)CaseChecklistItemState.Shown;
                    row.ShownDate = operation.ShownDate!.Value;
                    row.LastEvaluatedDate = now;
                    row.UpdatedDate = now;
                    await items.UpdateAsync(row, token).ConfigureAwait(false);

                    action = CaseChecklistAction.Shown;
                    detail = ShownDetail(trigger, operation.Definition?.Conditional ?? false);
                    break;
                default:
                    row = rowsById[operation.ExistingId!.Value];
                    row.StateId = (byte)CaseChecklistItemState.Hidden;
                    row.LastEvaluatedDate = now;
                    row.UpdatedDate = now;
                    await items.UpdateAsync(row, token).ConfigureAwait(false);

                    action = CaseChecklistAction.Hidden;
                    detail = operation.Definition == null
                        ? $"{trigger}: the item is no longer in force for the status."
                        : $"{trigger}: the show condition no longer passes.";
                    break;
            }

            var audit = await events.InsertAsync(new CaseChecklistEvent
            {
                CaseId = caseRow.Id,
                CaseChecklistItemId = row.Id,
                CaseWorkflowChecklistItemGuid = operation.ItemGuid,
                ActionId = (byte)action,
                Actor = actor,
                CreatedDate = now,
                Detail = detail
            }, token).ConfigureAwait(false);

            var timelineEventId = await dbContext.InsertWithInt32IdentityAsync(new CaseEvent
            {
                CaseId = caseRow.Id,
                CaseEventTypeId = action == CaseChecklistAction.Shown
                    ? CaseChecklistService.ShownEventTypeId
                    : CaseChecklistService.HiddenEventTypeId,
                CaseKey = caseRow.CaseKey,
                CaseKeyValue = caseRow.CaseKeyValue,
                Before = row.ItemName,
                After = audit.Id.ToString(),
                CreatedUser = actor,
                CreatedDate = now
            }, token: token).ConfigureAwait(false);

            await events.SetCaseEventIdAsync(audit.Id, timelineEventId, token).ConfigureAwait(false);
        }

        private static string ShownDetail(string trigger, bool conditional)
        {
            return conditional
                ? $"{trigger}: the show condition passes."
                : $"{trigger}: the item has no show condition, so it is always shown.";
        }
    }
}
