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
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using LinqToDB;

namespace Jube.Data.Helpers
{
    public static class ChunkedPurge
    {
        public static async Task<long> DeleteOlderThanAsync<T>(DbContext dbContext,
            Expression<Func<T, bool>> olderThanPredicate, Expression<Func<T, int>> idSelector,
            Func<List<int>, Expression<Func<T, bool>>> byIdsPredicate, int chunkSize,
            CancellationToken token = default) where T : class
        {
            var totalDeleted = 0L;

            while (!token.IsCancellationRequested)
            {
                var ids = await dbContext.GetTable<T>()
                    .Where(olderThanPredicate)
                    .OrderBy(idSelector)
                    .Select(idSelector)
                    .Take(chunkSize)
                    .ToListAsync(token).ConfigureAwait(false);

                if (ids.Count == 0)
                {
                    break;
                }

                var deletedThisChunk = await dbContext.GetTable<T>()
                    .Where(byIdsPredicate(ids))
                    .DeleteAsync(token).ConfigureAwait(false);
                totalDeleted += deletedThisChunk;

                if (ids.Count < chunkSize)
                {
                    break;
                }
            }

            return totalDeleted;
        }
    }
}