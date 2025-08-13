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
using Jube.Data.Context;
using Jube.Data.Poco;
using LinqToDB;

namespace Jube.Data.Repository;

public class CaseWorkflowFormEntryRepository
{
    private readonly DbContext _dbContext;
    private readonly int? _tenantRegistryId;
    private readonly string _userName;

    public CaseWorkflowFormEntryRepository(DbContext dbContext, string userName)
    {
        _dbContext = dbContext;
        _userName = userName;
        _tenantRegistryId = dbContext.UserInTenant.Where(w => w.User == _userName)
            .Select(s => s.TenantRegistryId).FirstOrDefault();
    }

    public CaseWorkflowFormEntryRepository(DbContext dbContext, int tenantRegistryId)
    {
        _dbContext = dbContext;
        _tenantRegistryId = tenantRegistryId;
    }

    public CaseWorkflowFormEntryRepository(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IEnumerable<CaseWorkflowFormEntry> Get()
    {
        return _dbContext.CaseWorkflowFormEntry.Where(w =>
            w.Case.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId ||
            !_tenantRegistryId.HasValue);
    }

    public IEnumerable<CaseWorkflowFormEntry> GetByCaseKeyValue(string key, string value)
    {
        return _dbContext.CaseWorkflowFormEntry.Where(w
                => (w.Case.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId ||
                    !_tenantRegistryId.HasValue)
                   && (w.Case.CaseWorkflow.EntityAnalysisModel.Deleted == 0 ||
                       w.Case.CaseWorkflow.EntityAnalysisModel.Deleted == null)
                   && w.CaseKey == key && w.CaseKeyValue == value)
            .OrderByDescending(o => o.Id);
    }

    public CaseWorkflowFormEntry Insert(CaseWorkflowFormEntry model)
    {
        model.CreatedUser = _userName;
        model.CreatedDate = DateTime.Now;
        model.Id = _dbContext.InsertWithInt32Identity(model);
        return model;
    }
}