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
    using Dto.Options;
    using LinqToDB;
    using Poco;

    public class CaseKeyJournalKeyIndexRepository(DbContext dbContext)
    {
        public Task<List<CaseKeyJournalKeyIndex>> GetAsync(CancellationToken token = default)
        {
            return dbContext.CaseKeyJournalKeyIndex.OrderBy(w => w.KeyName).ToListAsync(token);
        }

        public async Task<HashSet<string>> GetReadyKeyNamesAsync(CancellationToken token = default)
        {
            var ready = (byte)CaseKeyJournalKeyIndexState.Ready;

            var names = await dbContext.CaseKeyJournalKeyIndex.Where(w => w.StateId == ready)
                .Select(w => w.KeyName).ToListAsync(token).ConfigureAwait(false);

            return names.ToHashSet(StringComparer.Ordinal);
        }

        public async Task MarkAsync(string keyName, string indexName, CaseKeyJournalKeyIndexState state,
            string error, bool attempt, CancellationToken token = default)
        {
            var now = DateTime.UtcNow;
            var stateId = (byte)state;

            async Task<int> UpdateAsync()
            {
                return await dbContext.CaseKeyJournalKeyIndex.Where(w => w.KeyName == keyName)
                    .Set(s => s.IndexName, indexName)
                    .Set(s => s.StateId, stateId)
                    .Set(s => s.Error, error)
                    .Set(s => s.UpdatedDate, now)
                    .Set(s => s.AttemptCount, s => s.AttemptCount + (attempt ? 1 : 0))
                    .Set(s => s.ReadyDate, s => state == CaseKeyJournalKeyIndexState.Ready ? now : s.ReadyDate)
                    .UpdateAsync(token);
            }

            if (await UpdateAsync() > 0)
            {
                return;
            }

            try
            {
                await dbContext.InsertAsync(new CaseKeyJournalKeyIndex
                {
                    KeyName = keyName,
                    IndexName = indexName,
                    StateId = stateId,
                    AttemptCount = attempt ? 1 : 0,
                    Error = error,
                    CreatedDate = now,
                    UpdatedDate = now,
                    ReadyDate = state == CaseKeyJournalKeyIndexState.Ready ? now : null
                }, token: token);
            }
            catch (Exception ex) when (UniqueViolationCheck.Is(ex))
            {
                await UpdateAsync();
            }
        }
    }
}
