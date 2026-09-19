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
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using LinqToDB;

namespace Jube.Data.Repository
{
    public static class EntityAnalysisModelParentGuard
    {
        public static Task<bool> IsVisibleAsync(DbContext dbContext, int? tenantRegistryId, int entityAnalysisModelId,
            CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModel.AnyAsync(w =>
                w.Id == entityAnalysisModelId
                && (w.Deleted == 0 || w.Deleted == null)
                && (!tenantRegistryId.HasValue || w.TenantRegistryId == tenantRegistryId), token);
        }

        public static Task<bool> IsVisibleByGuidAsync(DbContext dbContext, int? tenantRegistryId,
            Guid entityAnalysisModelGuid, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModel.AnyAsync(w =>
                w.Guid == entityAnalysisModelGuid
                && (w.Deleted == 0 || w.Deleted == null)
                && (!tenantRegistryId.HasValue || w.TenantRegistryId == tenantRegistryId), token);
        }
    }
}