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
using AutoMapper;
using Jube.Data.Context;
using Jube.Data.Poco;
using LinqToDB;

namespace Jube.Data.Repository;

public class CaseWorkflowDisplayRepository
{
    private readonly DbContext _dbContext;
    private readonly int _tenantRegistryId;
    private readonly string _userName;

    public CaseWorkflowDisplayRepository(DbContext dbContext, string userName)
    {
        _dbContext = dbContext;
        _userName = userName;
        _tenantRegistryId = _dbContext.UserInTenant.Where(w => w.User == _userName)
            .Select(s => s.TenantRegistryId).FirstOrDefault();
    }

    public CaseWorkflowDisplayRepository(DbContext dbContext, int tenantRegistryId)
    {
        _dbContext = dbContext;
        _tenantRegistryId = tenantRegistryId;
    }

    public IEnumerable<CaseWorkflowDisplay> Get()
    {
        return _dbContext.CaseWorkflowDisplay
            .Where(w => w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId);
    }

    public IEnumerable<CaseWorkflowDisplay> GetByCasesWorkflowIdOrderById(int casesWorkflowId)
    {
        return _dbContext.CaseWorkflowDisplay
            .Where(w => w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId
                        && w.CaseWorkflowId == casesWorkflowId && (w.Deleted == 0 || w.Deleted == null))
            .OrderBy(o => o.Id);
    }

    public IEnumerable<CaseWorkflowDisplay> GetByCasesWorkflowIdActiveOnly(int casesWorkflowId)
    {
        return _dbContext.CaseWorkflowDisplay
            .Where(w => w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId
                        && w.Active == 1
                        && w.CaseWorkflowId == casesWorkflowId
                        && (w.Deleted == 0 || w.Deleted == null));
    }

    public IEnumerable<CaseWorkflowDisplay> GetByCasesWorkflowGuidActiveOnly(Guid casesWorkflowGuid)
    {
        return _dbContext.CaseWorkflowDisplay
            .Where(w => w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId
                        && w.Active == 1
                        && w.CaseWorkflow.Guid == casesWorkflowGuid
                        && (w.CaseWorkflow.EntityAnalysisModel.Deleted == 0 ||
                            w.CaseWorkflow.EntityAnalysisModel.Deleted == null)
                        && (w.Deleted == 0 || w.Deleted == null));
    }

    public CaseWorkflowDisplay GetById(int id)
    {
        return _dbContext.CaseWorkflowDisplay.FirstOrDefault(w =>
            w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId
            && w.Id == id && (w.Deleted == 0 || w.Deleted == null));
    }

    public CaseWorkflowDisplay Insert(CaseWorkflowDisplay model)
    {
        model.CreatedUser = _userName;
        model.CreatedDate = DateTime.Now;
        model.Version = 1;
        model.Guid = Guid.NewGuid();
        model.Id = _dbContext.InsertWithInt32Identity(model);
        return model;
    }

    public CaseWorkflowDisplay Update(CaseWorkflowDisplay model)
    {
        var existing = _dbContext.CaseWorkflowDisplay
            .FirstOrDefault(w => w.Id
                                 == model.Id
                                 && w.CaseWorkflow.EntityAnalysisModel.TenantRegistryId ==
                                 _tenantRegistryId
                                 && (w.Deleted == 0 || w.Deleted == null)
                                 && (w.Locked == 0 || w.Locked == null));

        if (existing == null) throw new KeyNotFoundException();

        model.Version = existing.Version + 1;
        model.CreatedUser = _userName ?? model.CreatedUser;
        model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
        model.CreatedDate = DateTime.Now;

        _dbContext.Update(model);

        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<CaseWorkflowDisplay, CaseWorkflowDisplayVersion>();
        });
        var mapper = new Mapper(config);

        var audit = mapper.Map<CaseWorkflowDisplayVersion>(existing);
        audit.CaseWorkflowDisplayId = existing.Id;

        _dbContext.Insert(audit);

        return model;
    }

    public void Delete(int id)
    {
        var records = _dbContext.CaseWorkflowDisplay
            .Where(d => d.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId
                        && d.Id == id
                        && (d.Locked == 0 || d.Locked == null)
                        && (d.Deleted == 0 || d.Deleted == null))
            .Set(s => s.Deleted, Convert.ToByte(1))
            .Set(s => s.DeletedDate, DateTime.Now)
            .Set(s => s.DeletedUser, _userName)
            .Update();

        if (records == 0) throw new KeyNotFoundException();
    }

    public void DeleteByTenantRegistryId(int tenantRegistryId, int importId)
    {
        _dbContext.CaseWorkflowDisplay
            .Where(d => d.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId
                        && d.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                        && (d.Deleted == 0 || d.Deleted == null))
            .Set(s => s.ImportId, importId)
            .Set(s => s.Deleted, Convert.ToByte(1))
            .Set(s => s.DeletedDate, DateTime.Now)
            .Update();
    }
}