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

using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using LinqToDB;
using LinqToDB.Async;

namespace Jube.Service.Query.Graph.Jobs
{
    public static class GraphCaseEdge
    {
        public const string Name = "In case";
        public const string NodeKey = "Case";
        public const string FromKey = "CaseKey";
        public const byte KindId = 5;
        public const int HalfLifeDays = 36500;
        private const string ProvisioningUser = "Administrator";

        public static async Task<List<GraphRegistryEdge>> EnsureAsync(DbContext dbContext, int tenant,
            CancellationToken token = default)
        {
            var registries = await dbContext.GraphRegistry
                .Where(r => r.TenantRegistryId == tenant && r.Active == 1 && (r.Deleted == null || r.Deleted == 0))
                .OrderBy(r => r.Id).ToListAsync(token).ConfigureAwait(false);
            var repository = new GraphRegistryEdgeRepository(dbContext, ProvisioningUser);
            var definitions = new List<GraphRegistryEdge>();

            foreach (var registry in registries)
            {
                var existing = await dbContext.GraphRegistryEdge
                    .Where(d => d.GraphRegistryId == registry.Id && d.KindId == KindId && (d.Deleted == null || d.Deleted == 0))
                    .OrderBy(d => d.Id).FirstOrDefaultAsync(token).ConfigureAwait(false);

                if (existing == null)
                {
                    existing = await repository.InsertAsync(new GraphRegistryEdge
                    {
                        GraphRegistryId = registry.Id, Name = Name, KindId = KindId, FromKey = FromKey, ToKey = NodeKey,
                        FromNodeKey = null, ToNodeKey = null, HalfLifeDays = HalfLifeDays, KindPrior = 1, MinimumAmount = 0,
                        Hard = 0, Restricted = 0, Active = 1, Locked = 1, Deleted = 0
                    }, token).ConfigureAwait(false);
                }

                if (existing.Active == 1)
                {
                    definitions.Add(existing);
                }
            }

            return definitions;
        }
    }
}
