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

namespace Jube.Data.Query.GetUserTenantEnabledQuery
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using LinqToDB.Async;

    public class GetUserTenantEnabledQuery(DbContext dbContext)
    {
        public Task<bool> ExecuteAsync(Guid roleRegistryGuid, CancellationToken token = default)
        {
            return (from rr in dbContext.RoleRegistry
                    join tr in dbContext.TenantRegistry on rr.TenantRegistryId equals tr.Id
                    where rr.Guid == roleRegistryGuid
                    select tr)
                .AnyAsync(tr => tr.Landlord == 1
                                || (tr.Active == 1 && (tr.Deleted == 0 || tr.Deleted == null)), token);
        }
    }
}
