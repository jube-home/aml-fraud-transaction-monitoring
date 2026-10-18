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
using Jube.Data.Poco;
using LinqToDB;

namespace Jube.Data.Repository
{
    public class EntityAnalysisModelAsynchronousQueueBalanceRepository
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;

        public EntityAnalysisModelAsynchronousQueueBalanceRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;

            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public EntityAnalysisModelAsynchronousQueueBalanceRepository(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<IEnumerable<EntityAnalysisModelAsynchronousQueueBalance>> GetAsync(int limit,
            CancellationToken token = default)
        {
            return await dbContext
                .EntityAnalysisModelAsynchronousQueueBalance
                .Where(w => w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId)
                .OrderByDescending(o => o.Id)
                .Take(limit).ToListAsync(token);
        }

        public async Task<IEnumerable<EntityAnalysisModelAsynchronousQueueBalance>> GetByEntityModelIdAsync(
            Guid entityAnalysisModelGuid,
            int limit, CancellationToken token = default)
        {
            return await dbContext
                .EntityAnalysisModelAsynchronousQueueBalance
                .Where(w => w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                            && w.EntityAnalysisModelGuid == entityAnalysisModelGuid)
                .OrderByDescending(o => o.Id)
                .Take(limit).ToListAsync(token);
        }

        public async Task<EntityAnalysisModelAsynchronousQueueBalance> InsertAsync(
            EntityAnalysisModelAsynchronousQueueBalance model, CancellationToken token = default)
        {
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);

            return model;
        }
    }
}