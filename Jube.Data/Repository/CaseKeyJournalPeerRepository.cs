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
    using Concurrency;
    using Context;
    using LinqToDB;
    using Poco;

    public class CaseKeyJournalPeerRepository(DbContext dbContext)
    {
        public Task<List<CaseKeyJournalPeer>> GetAsync(Guid caseWorkflowGuid, string caseKey, string segment,
            CancellationToken token = default)
        {
            return dbContext.CaseKeyJournalPeer
                .Where(w => w.CaseWorkflowGuid == caseWorkflowGuid && w.CaseKey == caseKey && w.Segment == segment
                            && w.Deleted == 0)
                .ToListAsync(token);
        }

        public Task<DateTime?> NewestAsync(Guid caseWorkflowGuid, string caseKey, CancellationToken token = default)
        {
            return dbContext.CaseKeyJournalPeer
                .Where(w => w.CaseWorkflowGuid == caseWorkflowGuid && w.CaseKey == caseKey && w.Deleted == 0)
                .MaxAsync(w => (DateTime?)w.ComputedDate, token);
        }

        public Task<int> MarkNotRebuiltDeletedAsync(Guid caseWorkflowGuid, string caseKey, DateTime builtFrom,
            CancellationToken token = default)
        {
            var now = DateTime.UtcNow;

            return dbContext.CaseKeyJournalPeer
                .Where(w => w.CaseWorkflowGuid == caseWorkflowGuid && w.CaseKey == caseKey && w.Deleted == 0
                            && w.ComputedDate < builtFrom)
                .Set(s => s.Deleted, (byte)1)
                .Set(s => s.DeletedDate, now)
                .UpdateAsync(token);
        }

        public async Task UpsertAsync(CaseKeyJournalPeer peer, CancellationToken token = default)
        {
            async Task<int> UpdateAsync()
            {
                return await dbContext.CaseKeyJournalPeer
                    .Where(w => w.CaseWorkflowGuid == peer.CaseWorkflowGuid && w.CaseKey == peer.CaseKey
                                                                             && w.Field == peer.Field
                                                                             && w.Segment == peer.Segment)
                    .Set(s => s.KeyCount, peer.KeyCount)
                    .Set(s => s.RowCount, peer.RowCount)
                    .Set(s => s.Median, peer.Median)
                    .Set(s => s.Scale, peer.Scale)
                    .Set(s => s.Q1, peer.Q1)
                    .Set(s => s.Q3, peer.Q3)
                    .Set(s => s.Minimum, peer.Minimum)
                    .Set(s => s.Maximum, peer.Maximum)
                    .Set(s => s.Mean, peer.Mean)
                    .Set(s => s.Bins, peer.Bins)
                    .Set(s => s.WindowDays, peer.WindowDays)
                    .Set(s => s.ComputedDate, peer.ComputedDate)
                    .Set(s => s.Deleted, (byte)0)
                    .Set(s => s.DeletedDate, (DateTime?)null)
                    .UpdateAsync(token);
            }

            if (await UpdateAsync() > 0)
            {
                return;
            }

            try
            {
                await dbContext.InsertAsync(peer, token: token);
            }
            catch (Exception ex) when (UniqueViolationCheck.Is(ex))
            {
                await UpdateAsync();
            }
        }
    }
}
