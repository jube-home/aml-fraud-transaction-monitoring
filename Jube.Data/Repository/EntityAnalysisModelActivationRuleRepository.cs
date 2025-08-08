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

public class EntityAnalysisModelActivationRuleRepository
{
    private readonly DbContext _dbContext;
    private readonly int? _tenantRegistryId;
    private readonly string _userName;

    public EntityAnalysisModelActivationRuleRepository(DbContext dbContext, string userName)
    {
        _dbContext = dbContext;
        _userName = userName;
        _tenantRegistryId = _dbContext.UserInTenant.Where(w => w.User == _userName)
            .Select(s => s.TenantRegistryId).FirstOrDefault();
    }

    public EntityAnalysisModelActivationRuleRepository(DbContext dbContext, int tenantRegistryId)
    {
        _dbContext = dbContext;
        _tenantRegistryId = tenantRegistryId;
    }

    public EntityAnalysisModelActivationRuleRepository(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IEnumerable<EntityAnalysisModelActivationRule> Get()
    {
        return _dbContext.EntityAnalysisModelActivationRule
            .Where(w => w.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId || !_tenantRegistryId.HasValue
            );
    }

    public IEnumerable<EntityAnalysisModelActivationRule> GetByEntityAnalysisModelIdOrderByIdDesc(
        int entityAnalysisModelId)
    {
        return _dbContext.EntityAnalysisModelActivationRule
            .Where(w =>
                (w.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId || !_tenantRegistryId.HasValue)
                && w.EntityAnalysisModelId == entityAnalysisModelId && (w.Deleted == 0 || w.Deleted == null))
            .OrderBy(o => o.Id);
    }

    public IEnumerable<EntityAnalysisModelActivationRule> GetByEntityAnalysisModelIdInPriorityOrder(
        int entityAnalysisModelId)
    {
        return _dbContext.EntityAnalysisModelActivationRule
            .Where(w =>
                (w.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId || !_tenantRegistryId.HasValue)
                && w.EntityAnalysisModelId == entityAnalysisModelId
                && (w.Deleted == 0 || w.Deleted == null))
            .Where(s => s.CaseWorkflowStatus.CaseWorkflow.EntityAnalysisModel.Deleted == 0 ||
                        s.CaseWorkflowStatus.CaseWorkflow.EntityAnalysisModel.Deleted == null)
            .OrderByDescending(o => o.EnableResponseElevation)
            .ThenByDescending(o => o.ResponseElevation)
            .ThenByDescending(o => o.EnableCaseWorkflow)
            .ThenBy(o => o.CaseWorkflowStatus.Priority)
            .ThenByDescending(o => o.EnableNotification)
            .ThenBy(o => o.Name);
    }

    public EntityAnalysisModelActivationRule GetById(int id)
    {
        return _dbContext.EntityAnalysisModelActivationRule.FirstOrDefault(w =>
            (w.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId || !_tenantRegistryId.HasValue)
            && w.Id == id && (w.Deleted == 0 || w.Deleted == null));
    }

    public EntityAnalysisModelActivationRule Insert(EntityAnalysisModelActivationRule model)
    {
        model.CreatedUser = _userName ?? model.CreatedUser;
        model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
        model.CreatedDate = DateTime.Now;
        model.Version = 1;
        model.Id = _dbContext.InsertWithInt32Identity(model);
        return model;
    }

    public void UpdateCounter(int id, int activationCounter)
    {
        var records = _dbContext.EntityAnalysisModelActivationRule
            .Where(d =>
                (d.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId || !_tenantRegistryId.HasValue)
                && d.Id == id
                && (d.Deleted == 0 || d.Deleted == null))
            .Set(s => s.ActivationCounter, activationCounter)
            .Set(s => s.ActivationCounterDate, DateTime.Now)
            .Update();

        if (records == 0) throw new KeyNotFoundException();
    }

    public EntityAnalysisModelActivationRule Update(EntityAnalysisModelActivationRule model)
    {
        var existing = _dbContext.EntityAnalysisModelActivationRule
            .FirstOrDefault(w => w.Id
                                 == model.Id
                                 && (w.Deleted == 0 || w.Deleted == null)
                                 && (w.Locked == 0 || w.Locked == null));

        if (existing == null) throw new KeyNotFoundException();

        model.Version = existing.Version + 1;
        model.Guid = existing.Guid;
        model.CreatedUser = _userName;
        model.CreatedDate = DateTime.Now;

        _dbContext.Update(model);

        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<EntityAnalysisModelActivationRule, EntityAnalysisModelActivationRuleVersion>();
        });
        var mapper = new Mapper(config);

        var audit = mapper.Map<EntityAnalysisModelActivationRuleVersion>(existing);
        audit.EntityAnalysisModelActivationRuleId = existing.Id;

        _dbContext.Insert(audit);

        return model;
    }

    public void Delete(int id)
    {
        var records = _dbContext.EntityAnalysisModelActivationRule
            .Where(d =>
                (d.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId || !_tenantRegistryId.HasValue)
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
        _dbContext.EntityAnalysisModelActivationRule
            .Where(d =>
                (d.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId || !_tenantRegistryId.HasValue)
                && d.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                && (d.Deleted == 0 || d.Deleted == null))
            .Set(s => s.ImportId, importId)
            .Set(s => s.Deleted, Convert.ToByte(1))
            .Set(s => s.DeletedDate, DateTime.Now)
            .Update();
    }
}