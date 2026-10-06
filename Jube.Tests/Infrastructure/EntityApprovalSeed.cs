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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using LinqToDB;

namespace Jube.Test.Infrastructure
{
    public static class EntityApprovalSeed
    {
        public const string Checker = "ZzTestChecker";

        public static Task ApproveAsync(DbContext dbContext, EntityApprovalKind kind, int entityId,
            int version = 1, int? tenantRegistryId = null, string approvedBy = Checker,
            CancellationToken token = default)
        {
            return dbContext.InsertAsync(new EntityApproval
            {
                TenantRegistryId = tenantRegistryId,
                EntityApprovalKindId = (int)kind,
                EntityId = entityId,
                EntityVersion = version,
                StateId = (int)EntityApprovalState.Approved,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = approvedBy,
                Guid = Guid.NewGuid()
            }, token: token);
        }

        public static Task RejectAsync(DbContext dbContext, EntityApprovalKind kind, int entityId,
            int version = 1, int? tenantRegistryId = null, string rejectedBy = Checker,
            CancellationToken token = default)
        {
            return dbContext.InsertAsync(new EntityApproval
            {
                TenantRegistryId = tenantRegistryId,
                EntityApprovalKindId = (int)kind,
                EntityId = entityId,
                EntityVersion = version,
                StateId = (int)EntityApprovalState.Rejected,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = rejectedBy,
                Guid = Guid.NewGuid()
            }, token: token);
        }

        public static Task RemoveForEntityAsync(DbContext dbContext, EntityApprovalKind kind, int entityId,
            CancellationToken token = default)
        {
            var kindId = (int)kind;

            return dbContext.EntityApproval
                .Where(w => w.EntityApprovalKindId == kindId && w.EntityId == entityId)
                .DeleteAsync(token);
        }
    }
}