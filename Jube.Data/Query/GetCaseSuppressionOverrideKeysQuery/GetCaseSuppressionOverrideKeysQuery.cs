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

namespace Jube.Data.Query.GetCaseSuppressionOverrideKeysQuery
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using LinqToDB.Async;

    public class GetCaseSuppressionOverrideKeysQuery(DbContext dbContext)
    {
        public Task<List<GetCaseSuppressionOverrideKeysQueryDto>> ExecuteAsync(int entityAnalysisModelId, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelRequestXpath
                .Where(w => w.EntityAnalysisModelId == entityAnalysisModelId && w.EnableOverride == 1
                                                                             && (w.Deleted == 0 || w.Deleted == null)
                                                                             && w.Name != null)
                .Select(s => s.Name).Distinct().OrderBy(o => o).Select(n => new GetCaseSuppressionOverrideKeysQueryDto { Name = n })
                .ToListAsync(token);
        }
    }
}
