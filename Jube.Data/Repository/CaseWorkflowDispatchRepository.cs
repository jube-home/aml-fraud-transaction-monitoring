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

namespace Jube.Data.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using Poco;

    public class CaseWorkflowDispatchRepository
    {
        private readonly DbContext dbContext;
        private readonly int tenantRegistryId;
        private readonly string userName;

        public CaseWorkflowDispatchRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;

            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public CaseWorkflowDispatchRepository(DbContext dbContext, int tenantRegistryId, string userName)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
            this.userName = userName;
        }

        public async Task<CaseWorkflowDispatch> InsertAsync(CaseWorkflowDispatch model,
            CancellationToken token = default)
        {
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.CreatedUser = userName ?? model.CreatedUser;
            model.CreatedDate = DateTime.UtcNow;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);

            return model;
        }

        public Task<List<CaseWorkflowDispatch>> GetByCaseKeyValueAsync(string key, string value, int maximum,
            CancellationToken token = default)
        {
            return (from d in dbContext.CaseWorkflowDispatch
                join c in dbContext.Case on d.CaseId equals c.Id
                where d.CaseKey == key && d.CaseKeyValue == value
                      && (c.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId)
                      && (c.CaseWorkflow.EntityAnalysisModel.Deleted == 0 ||
                          c.CaseWorkflow.EntityAnalysisModel.Deleted == null)
                      && (c.CaseWorkflow.Deleted == 0 || c.CaseWorkflow.Deleted == null)
                      && (c.CaseWorkflowStatus.Deleted == 0 || c.CaseWorkflowStatus.Deleted == null)
                      && dbContext.CaseWorkflowRole
                          .Where(r => r.CaseWorkflowGuid == c.CaseWorkflow.Guid
                                      && (r.Deleted == 0 || r.Deleted == null))
                          .Any(r => dbContext.RoleRegistry
                              .Where(rr => rr.Guid == r.RoleRegistryGuid && (rr.Deleted == 0 || rr.Deleted == null))
                              .Any(rr => dbContext.UserRegistry
                                  .Any(u => u.RoleRegistryGuid == rr.Guid && u.Name == userName)))
                      && dbContext.CaseWorkflowStatusRole
                          .Where(r => r.CaseWorkflowStatusGuid == c.CaseWorkflowStatus.Guid
                                      && (r.Deleted == 0 || r.Deleted == null))
                          .Any(r => dbContext.RoleRegistry
                              .Where(rr => rr.Guid == r.RoleRegistryGuid && (rr.Deleted == 0 || rr.Deleted == null))
                              .Any(rr => dbContext.UserRegistry
                                  .Any(u => u.RoleRegistryGuid == rr.Guid && u.Name == userName)))
                orderby d.Id descending
                select d).Take(maximum).ToListAsync(token);
        }
    }
}
