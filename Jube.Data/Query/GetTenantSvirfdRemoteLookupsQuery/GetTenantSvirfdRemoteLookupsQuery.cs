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

namespace Jube.Data.Query.GetTenantSvirfdRemoteLookupsQuery
{
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using LinqToDB.Async;

    public class GetTenantSvirfdRemoteLookupsQuery(DbContext dbContext)
    {
        public async Task<bool> ExecuteAsync(string userName, CancellationToken token = default)
        {
            var flags = await (from uit in dbContext.UserInTenant
                    join tr in dbContext.TenantRegistry on uit.TenantRegistryId equals tr.Id
                    where uit.User == userName && (tr.Deleted == 0 || tr.Deleted == null)
                    select tr.SvirfdRemoteLookupsEnabled)
                .Take(1)
                .ToListAsync(token);

            return flags.Count == 1 && flags[0] != 0;
        }
    }
}
