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
using LinqToDB.Data;

namespace Jube.Data.Repository;

public class CaseEventRepository
{
    private readonly DbContext _dbContext;
    private readonly int? _tenantRegistryId;
    private readonly string _userName;

    public CaseEventRepository(DbContext dbContext, string userName)
    {
        _dbContext = dbContext;
        _userName = userName;
        _tenantRegistryId = dbContext.UserInTenant.Where(w => w.User == _userName)
            .Select(s => s.TenantRegistryId).FirstOrDefault();
    }

    public CaseEventRepository(DbContext dbContext, int tenantRegistryId)
    {
        _dbContext = dbContext;
        _tenantRegistryId = tenantRegistryId;
    }

    public CaseEventRepository(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IEnumerable<CaseEvent> Get()
    {
        return _dbContext.CaseEvent.Where(w =>
            w.Case.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId ||
            !_tenantRegistryId.HasValue);
    }

    public CaseEvent GetById(int id)
    {
        return _dbContext.CaseEvent.FirstOrDefault(w
            => (w.Case.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId ||
                !_tenantRegistryId.HasValue)
               && w.Id == id);
    }

    public void UpdateAbstractionRuleMatches(int id)
    {
        _dbContext.EntityAnalysisModelSearchKeyDistinctValueCalculationInstance
            .Where(d => d.EntityAnalysisModelSearchKeyCalculationInstanceId == id)
            .Set(s => s.AbstractionRulesMatchesUpdatedDate, DateTime.Now)
            .Update();
    }

    public CaseEvent Insert(CaseEvent model)
    {
        model.CreatedUser = _userName;
        model.CreatedDate = DateTime.Now;
        model.Id = _dbContext.InsertWithInt32Identity(model);
        return model;
    }

    public void BulkInsert(IEnumerable<CaseEvent> models)
    {
        _dbContext.BulkCopy(models);
    }
}