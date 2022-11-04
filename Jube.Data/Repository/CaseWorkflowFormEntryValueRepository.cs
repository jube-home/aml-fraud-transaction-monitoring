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

using System.Collections.Generic;
using System.Linq;
using Jube.Data.Context;
using Jube.Data.Poco;
using LinqToDB;

namespace Jube.Data.Repository
{
    public class CaseWorkflowFormEntryValueRepository
    {
        private readonly DbContext _dbContext;
        private readonly int? _tenantRegistryId;

        public CaseWorkflowFormEntryValueRepository(DbContext dbContext, string userName)
        {
            _dbContext = dbContext;
            _tenantRegistryId = dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public CaseWorkflowFormEntryValueRepository(DbContext dbContext, int tenantRegistryId)
        {
            _dbContext = dbContext;
            _tenantRegistryId = tenantRegistryId;
        }

        public CaseWorkflowFormEntryValueRepository(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IEnumerable<CaseWorkflowFormEntryValue> GetByCaseWorkflowFormEntryId(int caseWorkflowFormEntryId)
        {
            return _dbContext.CaseWorkflowFormEntryValue.Where(w
                    => (w.CaseWorkflowsFormsEntry.Case.CaseWorkflows.EntityAnalysisModel.TenantRegistryId ==
                        _tenantRegistryId ||
                        !_tenantRegistryId.HasValue)
                       && w.CaseWorkflowFormEntryId == caseWorkflowFormEntryId)
                .OrderByDescending(o => o.Id);
        }

        public CaseWorkflowFormEntryValue Insert(CaseWorkflowFormEntryValue model)
        {
            model.Id = _dbContext.InsertWithInt32Identity(model);
            return model;
        }
    }
}