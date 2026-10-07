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

namespace Jube.Data.Query.GetEntityApprovalSubjectsByModelQuery
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using Models;
    using Repository;

    public sealed class GetEntityApprovalSubjectsByModelQuery(DbContext dbContext, int tenantRegistryId)
    {
        public async Task<List<EntityApprovalSubject>> ExecuteAsync(int modelId,
            CancellationToken token = default)
        {
            var all = new List<EntityApprovalSubject>();

            foreach (var kind in Enum.GetValues<EntityApprovalKind>())
            {
                var rows = await new EntityApprovalSubjectProjection(dbContext, tenantRegistryId).SubjectsOf(kind)
                    .Where(w => w.ModelId == modelId)
                    .ToListAsync(token)
                    .ConfigureAwait(false);
                all.AddRange(rows);
            }

            return all;
        }
    }
}