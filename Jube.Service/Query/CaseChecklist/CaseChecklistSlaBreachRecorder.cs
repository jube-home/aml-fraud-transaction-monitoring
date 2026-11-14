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
using Jube.Data.Poco;
using Jube.Data.Query.GetCaseChecklistSlaBreachCandidatesQuery;
using Jube.Data.Repository;
using Jube.Dto.Options;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using log4net;
using CaseRow = Jube.Data.Poco.Case;

namespace Jube.Service.Query.CaseChecklist
{
    public sealed class CaseChecklistSlaBreachRecorder(DbContext dbContext, TimeProvider clock, ILog log)
    {
        private const int AdvisoryLockNamespace = 20263108;
        private const int ClosedStatusId = 3;

        public async Task<int> RecordOpenBreachesAsync(CaseRow caseRow, int tenantRegistryId, DateTime now,
            CancellationToken token = default)
        {
            if (caseRow.ClosedStatusId == ClosedStatusId)
            {
                return 0;
            }

            var items = new CaseChecklistItemRepository(dbContext, tenantRegistryId);
            var due = await items.ListOpenPastDueAsync(caseRow.Id, now, token).ConfigureAwait(false);
            var recorded = 0;

            foreach (var item in due.Where(w => w.CaseWorkflowStatusGuid == caseRow.CaseWorkflowStatusGuid))
            {
                if (await RecordAsync(caseRow, tenantRegistryId, items, item, now,
                        $"The SLA of {Describe(item)} was breached: the item is still open.", token)
                    .ConfigureAwait(false))
                {
                    recorded++;
                }
            }

            return recorded;
        }

        public async Task<bool> RecordCompletedLateAsync(CaseRow caseRow, int tenantRegistryId,
            CaseChecklistItem item, string completedBy, DateTime now, CancellationToken token = default)
        {
            if (!item.DueDate.HasValue || !item.CompletedDate.HasValue || item.CompletedDate.Value <= item.DueDate.Value)
            {
                return false;
            }

            var items = new CaseChecklistItemRepository(dbContext, tenantRegistryId);

            return await RecordAsync(caseRow, tenantRegistryId, items, item, now,
                $"The SLA of {Describe(item)} was breached: the item was ticked by {completedBy} at " +
                $"{item.CompletedDate.Value.ToString("u", CultureInfo.InvariantCulture)}, after it fell due.", token)
                .ConfigureAwait(false);
        }

        public async Task<bool> RecordItemAsync(long itemId, int caseId, int tenantRegistryId,
            CancellationToken token = default)
        {
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

                var now = clock.GetUtcNow().UtcDateTime;

                var caseRow = await dbContext.Case.FirstOrDefaultAsync(w => w.Id == caseId, token)
                    .ConfigureAwait(false);

                var recorded = false;

                if (caseRow != null && caseRow.ClosedStatusId != ClosedStatusId)
                {
                    var items = new CaseChecklistItemRepository(dbContext, tenantRegistryId);
                    var item = await items.GetAsync(caseId, itemId, token).ConfigureAwait(false);

                    if (item != null
                        && item.StateId == (byte)CaseChecklistItemState.Shown
                        && item.CaseWorkflowStatusGuid == caseRow.CaseWorkflowStatusGuid
                        && item.SlaBreachedDate == null
                        && item.DueDate.HasValue
                        && item.DueDate.Value < now)
                    {
                        recorded = await RecordAsync(caseRow, tenantRegistryId, items, item, now,
                            $"The SLA of {Describe(item)} was breached: the item is still open.", token)
                            .ConfigureAwait(false);
                    }
                }

                if (ownsTransaction)
                {
                    await dbContext.CommitTransactionAsync(token).ConfigureAwait(false);
                }

                return recorded;
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

        public async Task<IReadOnlyList<(int TenantRegistryId, int CaseId)>> SweepAsync(int batchSize,
            CancellationToken token = default)
        {
            var now = clock.GetUtcNow().UtcDateTime;

            var candidates = await new GetCaseChecklistSlaBreachCandidatesQuery(dbContext)
                .ExecuteAsync(now, batchSize, token).ConfigureAwait(false);

            var recorded = new List<(int TenantRegistryId, int CaseId)>();

            foreach (var candidate in candidates.Where(w => w.TenantRegistryId > 0))
            {
                try
                {
                    if (await RecordItemAsync(candidate.ItemId, candidate.CaseId, candidate.TenantRegistryId, token)
                            .ConfigureAwait(false))
                    {
                        recorded.Add((candidate.TenantRegistryId, candidate.CaseId));
                    }
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    log.Error($"CaseChecklist.SlaSweep: item {candidate.ItemId} of case {candidate.CaseId} " +
                              "could not be recorded.", e);
                }
            }

            return recorded;
        }

        private async Task<bool> RecordAsync(CaseRow caseRow, int tenantRegistryId, CaseChecklistItemRepository items,
            CaseChecklistItem item, DateTime now, string detail, CancellationToken token)
        {
            if (!await items.TryRecordSlaBreachAsync(item.Id, caseRow.Id, now, token).ConfigureAwait(false))
            {
                return false;
            }

            item.SlaBreachedDate = now;

            await new CaseChecklistAuditWriter(dbContext, tenantRegistryId).WriteAsync(caseRow,
                CaseChecklistAction.SlaBreached, CaseChecklistService.SystemActor, item, item.ItemName, detail,
                CaseChecklistService.SlaBreachedEventTypeId, null, now, token).ConfigureAwait(false);

            return true;
        }

        private static string Describe(CaseChecklistItem item)
        {
            return item.DueDate.HasValue
                ? $"'{item.ItemName}' (due {item.DueDate.Value.ToString("u", CultureInfo.InvariantCulture)})"
                : $"'{item.ItemName}'";
        }
    }
}
