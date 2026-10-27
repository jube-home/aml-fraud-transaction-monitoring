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

    public class CaseKeyJournalPinRepository(DbContext dbContext, string userName)
    {
        public Task<List<Guid>> GetPinnedAsync(int caseId, CancellationToken token = default)
        {
            return dbContext.CaseKeyJournalPin.Where(w => w.CaseId == caseId && w.Pinned == 1)
                .OrderByDescending(w => w.UpdatedDate).ThenByDescending(w => w.Id)
                .Select(w => w.EntityAnalysisModelInstanceEntryGuid).ToListAsync(token);
        }

        public async Task SetAsync(int caseId, IReadOnlyCollection<Guid> transactions, bool pinned,
            CancellationToken token = default)
        {
            var now = DateTime.UtcNow;
            var value = (byte)(pinned ? 1 : 0);

            foreach (var transaction in transactions)
            {
                async Task<int> UpdateAsync()
                {
                    return await dbContext.CaseKeyJournalPin
                        .Where(w => w.CaseId == caseId && w.EntityAnalysisModelInstanceEntryGuid == transaction)
                        .Set(s => s.Pinned, value)
                        .Set(s => s.UpdatedUser, userName)
                        .Set(s => s.UpdatedDate, now)
                        .UpdateAsync(token);
                }

                if (await UpdateAsync() > 0)
                {
                    continue;
                }

                if (!pinned)
                {
                    continue;
                }

                try
                {
                    await dbContext.InsertAsync(new CaseKeyJournalPin
                    {
                        CaseId = caseId,
                        EntityAnalysisModelInstanceEntryGuid = transaction,
                        Pinned = value,
                        CreatedUser = userName,
                        CreatedDate = now,
                        UpdatedUser = userName,
                        UpdatedDate = now
                    }, token: token);
                }
                catch (Exception ex) when (UniqueViolationCheck.Is(ex))
                {
                    await UpdateAsync();
                }
            }
        }
    }
}
