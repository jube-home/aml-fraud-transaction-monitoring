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
    using LinqToDB;
    using Poco;

    public class CaseKeyJournalReplayRepository(DbContext dbContext, string userName)
    {
        public Task<List<CaseKeyJournalReplay>> ListAsync(int caseId, CancellationToken token = default)
        {
            return dbContext.CaseKeyJournalReplay.Where(w => w.CaseId == caseId && w.Deleted == 0)
                .OrderByDescending(w => w.CreatedDate).ThenByDescending(w => w.Id).ToListAsync(token);
        }

        public Task<CaseKeyJournalReplay> GetAsync(Guid guid, CancellationToken token = default)
        {
            return dbContext.CaseKeyJournalReplay.Where(w => w.Guid == guid && w.Deleted == 0)
                .FirstOrDefaultAsync(token);
        }

        public Task<int> CountAsync(int caseId, CancellationToken token = default)
        {
            return dbContext.CaseKeyJournalReplay.CountAsync(w => w.CaseId == caseId && w.Deleted == 0, token);
        }

        public async Task<CaseKeyJournalReplay> InsertAsync(int caseId, string name, int days, string mergeJson,
            string filterJson, double? limitKmh, double? minimumKilometres, bool impossibleOnly,
            bool showAll, string lens, CancellationToken token = default)
        {
            var replay = new CaseKeyJournalReplay
            {
                Guid = Guid.NewGuid(),
                CaseId = caseId,
                Name = name,
                Days = days,
                MergeJson = mergeJson,
                FilterJson = filterJson,
                LimitKmh = limitKmh,
                MinimumKilometres = minimumKilometres,
                ImpossibleOnly = (byte)(impossibleOnly ? 1 : 0),
                ShowAll = (byte)(showAll ? 1 : 0),
                Lens = lens,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0
            };

            replay.Id = await dbContext.InsertWithInt64IdentityAsync(replay, token: token);

            return replay;
        }

        public Task<int> DeleteAsync(Guid guid, CancellationToken token = default)
        {
            return dbContext.CaseKeyJournalReplay.Where(w => w.Guid == guid && w.Deleted == 0)
                .Set(s => s.Deleted, (byte)1)
                .Set(s => s.DeletedUser, userName)
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }
    }
}
