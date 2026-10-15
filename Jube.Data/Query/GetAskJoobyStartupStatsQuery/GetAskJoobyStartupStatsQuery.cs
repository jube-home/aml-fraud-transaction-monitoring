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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Repository.Models;
using LinqToDB;

namespace Jube.Data.Query.GetAskJoobyStartupStatsQuery
{
    public class GetAskJoobyStartupStatsQuery(DbContext dbContext)
    {
        public async Task<GetAskJoobyStartupStatsQueryDto> ExecuteAsync(int tenantRegistryId, string userName,
            CancellationToken token = default)
        {
            var bites = await dbContext.AskJoobyRagChunk.CountAsync(token).ConfigureAwait(false);

            var trails = await dbContext.AskJoobyConversation
                .Where(w => w.TenantRegistryId == tenantRegistryId && (w.Deleted == 0 || w.Deleted == null))
                .CountAsync(token).ConfigureAwait(false);

            var saltlicks = await dbContext.AskJoobyMemory
                .Where(w => w.TenantRegistryId == tenantRegistryId
                            && (w.ScopeId == (byte)AskJoobyMemoryScope.System
                                || (w.ScopeId == (byte)AskJoobyMemoryScope.User && w.CreatedUser == userName)))
                .CountAsync(token).ConfigureAwait(false);

            return new GetAskJoobyStartupStatsQueryDto
            {
                Bites = bites, Trails = trails, Saltlicks = saltlicks
            };
        }
    }
}