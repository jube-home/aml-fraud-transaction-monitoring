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

namespace Jube.Data.Query
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using Repository;

    public class GetModelListsQuery(DbContext dbContext, int tenantRegistryId)
    {
        public async Task<Dictionary<string, List<string>>> ExecuteAsync(int entityAnalysisModelId,
            CancellationToken token = default)
        {
            var lists = new Dictionary<string, List<string>>();
            var valueRepository = new EntityAnalysisModelListValueRepository(dbContext, tenantRegistryId);

            foreach (var list in await new EntityAnalysisModelListRepository(dbContext, tenantRegistryId)
                         .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false))
            {
                if (list.Active != 1 || list.Name == null || lists.ContainsKey(list.Name))
                {
                    continue;
                }

                var values = new List<string>();
                foreach (var value in await valueRepository
                             .GetByEntityAnalysisModelListIdOrderByIdAsync(list.Id, token).ConfigureAwait(false))
                {
                    if (value.ListValue != null)
                    {
                        values.Add(value.ListValue);
                    }
                }

                lists.Add(list.Name, values);
            }

            return lists;
        }
    }
}