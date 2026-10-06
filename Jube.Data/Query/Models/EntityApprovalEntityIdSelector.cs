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
using Jube.Data.Repository;

namespace Jube.Data.Query.Models
{
    public static class EntityApprovalEntityIdSelector
    {
        public static async Task<HashSet<int>> SelectAsync(DbContext dbContext, int tenantRegistryId,
            EntityApprovalKind kind, int modelId,
            Func<List<EntityApprovalRow>, EntityApprovalSubject, bool> matches, CancellationToken token)
        {
            var subjects = await new Jube.Data.Query.GetEntityApprovalSubjectsByKindAndModelQuery
                    .GetEntityApprovalSubjectsByKindAndModelQuery(dbContext, tenantRegistryId)
                .ExecuteAsync(kind, modelId, token).ConfigureAwait(false);

            var ids = subjects.Select(s => s.EntityId).Distinct().ToList();
            if (ids.Count == 0)
            {
                return [];
            }

            var rows = await new EntityApprovalRepository(dbContext, tenantRegistryId)
                .GetRowsByKindAndEntityIdsAsync(kind, ids, token).ConfigureAwait(false);

            var selected = new HashSet<int>();

            foreach (var subject in subjects)
            {
                var entityRows = rows.Where(r => r.EntityId == subject.EntityId).ToList();

                if (matches(entityRows, subject))
                {
                    selected.Add(subject.EntityId);
                }
            }

            return selected;
        }
    }
}