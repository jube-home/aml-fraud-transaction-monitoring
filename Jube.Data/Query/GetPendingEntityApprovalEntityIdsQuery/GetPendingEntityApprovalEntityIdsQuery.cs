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

namespace Jube.Data.Query.GetPendingEntityApprovalEntityIdsQuery
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using Models;
    using Repository;

    public sealed class GetPendingEntityApprovalEntityIdsQuery(DbContext dbContext, int tenantRegistryId)
    {
        public Task<HashSet<int>> ExecuteAsync(EntityApprovalKind kind, int modelId, int approvalsRequired,
            CancellationToken token = default)
        {
            return EntityApprovalEntityIdSelector.SelectAsync(dbContext, tenantRegistryId, kind, modelId,
                (rows, subject) => EntityApprovalResolver.IsPending(rows, subject.Version, approvalsRequired), token);
        }
    }
}