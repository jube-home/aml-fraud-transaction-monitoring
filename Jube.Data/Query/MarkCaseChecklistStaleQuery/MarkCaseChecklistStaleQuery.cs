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
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using LinqToDB;

namespace Jube.Data.Query.MarkCaseChecklistStaleQuery
{
    public class MarkCaseChecklistStaleQuery(DbContext dbContext)
    {
        public Task<int> ExecuteAsync(IEnumerable<int> caseIds, CancellationToken token = default)
        {
            var distinct = caseIds.Where(w => w > 0).Distinct().ToList();

            if (distinct.Count == 0)
            {
                return Task.FromResult(0);
            }

            var now = DateTime.UtcNow;

            return dbContext.Case.Where(w => distinct.Contains(w.Id)).Set(s => s.ChecklistStaleDate, now)
                .UpdateAsync(token);
        }
    }
}
