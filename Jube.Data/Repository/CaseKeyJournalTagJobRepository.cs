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


namespace Jube.Data.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using Dto.Options;
    using LinqToDB;
    using LinqToDB.Data;
    using Poco;

    public class CaseKeyJournalTagJobRepository(DbContext dbContext)
    {
        public const int HeartbeatTimeoutMinutes = 10;

        public Task<long> InsertAsync(CaseKeyJournalTagJob job, CancellationToken token = default)
        {
            return dbContext.InsertWithInt64IdentityAsync(job, token: token);
        }

        public Task<CaseKeyJournalTagJob> GetAsync(Guid guid, CancellationToken token = default)
        {
            return dbContext.CaseKeyJournalTagJob.FirstOrDefaultAsync(w => w.Guid == guid, token);
        }

        public Task<List<CaseKeyJournalTagJob>> ListAsync(string user, Guid caseWorkflowGuid, int take,
            CancellationToken token = default)
        {
            return dbContext.CaseKeyJournalTagJob
                .Where(w => w.RequestedUser == user && w.CaseWorkflowGuid == caseWorkflowGuid)
                .OrderByDescending(w => w.Id).Take(take).ToListAsync(token);
        }

        public Task<bool> HasLiveUndoAsync(Guid undoOf, CancellationToken token = default)
        {
            var failed = (byte)CaseKeyJournalTagJobState.Failed;
            var cancelled = (byte)CaseKeyJournalTagJobState.Cancelled;

            return dbContext.CaseKeyJournalTagJob.AnyAsync(
                w => w.UndoOfGuid == undoOf && w.StateId != failed && w.StateId != cancelled, token);
        }

        public async Task<CaseKeyJournalTagJob> ClaimNextAsync(CancellationToken token = default)
        {
            var found = await dbContext.QueryToArrayAsync<CaseKeyJournalTagJob>(
                "update \"CaseKeyJournalTagJob\" set \"StateId\" = @running, \"StartedDate\" = coalesce(\"StartedDate\", @now), " +
                "\"HeartbeatDate\" = @now " +
                "where \"Id\" = (select \"Id\" from \"CaseKeyJournalTagJob\" " +
                "where (\"StateId\" = @queued and \"CancelRequested\" = 0) or (\"StateId\" = @running and \"HeartbeatDate\" < @stale) " +
                "order by \"Id\" limit 1 for update skip locked) returning *",
                new DataParameter("running", (byte)CaseKeyJournalTagJobState.Running),
                new DataParameter("queued", (byte)CaseKeyJournalTagJobState.Queued),
                new DataParameter("now", DateTime.UtcNow, DataType.DateTime2),
                new DataParameter("stale", DateTime.UtcNow.AddMinutes(-HeartbeatTimeoutMinutes), DataType.DateTime2))
                .ConfigureAwait(false);

            return found.FirstOrDefault();
        }

        public Task<bool> CancelRequestedAsync(long id, CancellationToken token = default)
        {
            return dbContext.CaseKeyJournalTagJob.Where(w => w.Id == id).Select(w => w.CancelRequested == 1)
                .FirstOrDefaultAsync(token);
        }

        public async Task<bool> RequestCancelAsync(Guid guid, string user, CancellationToken token = default)
        {
            var queued = (byte)CaseKeyJournalTagJobState.Queued;
            var running = (byte)CaseKeyJournalTagJobState.Running;
            var done = await dbContext.CaseKeyJournalTagJob
                .Where(w => w.Guid == guid && w.RequestedUser == user && (w.StateId == queued || w.StateId == running))
                .Set(s => s.CancelRequested, (byte)1).UpdateAsync(token).ConfigureAwait(false);

            if (done == 0)
            {
                return false;
            }

            var now = DateTime.UtcNow;

            await dbContext.CaseKeyJournalTagJob.Where(w => w.Guid == guid && w.StateId == queued)
                .Set(s => s.StateId, (byte)CaseKeyJournalTagJobState.Cancelled)
                .Set(s => s.FinishedDate, now).UpdateAsync(token).ConfigureAwait(false);

            return true;
        }

        public Task SetTotalAsync(long id, int total, CancellationToken token = default)
        {
            return dbContext.CaseKeyJournalTagJob.Where(w => w.Id == id).Set(s => s.Total, total)
                .Set(s => s.HeartbeatDate, DateTime.UtcNow).UpdateAsync(token);
        }

        public Task ProgressAsync(long id, int processed, int changed, int skipped, int locked,
            CancellationToken token = default)
        {
            return dbContext.CaseKeyJournalTagJob.Where(w => w.Id == id)
                .Set(s => s.Processed, processed).Set(s => s.Changed, changed).Set(s => s.Skipped, skipped)
                .Set(s => s.Locked, locked).Set(s => s.HeartbeatDate, DateTime.UtcNow).UpdateAsync(token);
        }

        public Task FinishAsync(long id, CaseKeyJournalTagJobState state, string error,
            CancellationToken token = default)
        {
            return dbContext.CaseKeyJournalTagJob.Where(w => w.Id == id).Set(s => s.StateId, (byte)state)
                .Set(s => s.Error, error).Set(s => s.FinishedDate, DateTime.UtcNow).UpdateAsync(token);
        }

        public async Task AddRowsAsync(long jobId, IReadOnlyCollection<(Guid Transaction, string Previous)> rows,
            CancellationToken token = default)
        {
            if (rows.Count == 0)
            {
                return;
            }

            await dbContext.BulkCopyAsync(rows.Select(row => new CaseKeyJournalTagJobRow
            {
                CaseKeyJournalTagJobId = jobId,
                EntityAnalysisModelInstanceEntryGuid = row.Transaction,
                PreviousTagJson = row.Previous
            }), token).ConfigureAwait(false);
        }

        public Task<List<CaseKeyJournalTagJobRow>> RowsAsync(long jobId, long afterId, int take,
            CancellationToken token = default)
        {
            return dbContext.CaseKeyJournalTagJobRow.Where(w => w.CaseKeyJournalTagJobId == jobId && w.Id > afterId)
                .OrderBy(w => w.Id).Take(take).ToListAsync(token);
        }
    }
}
