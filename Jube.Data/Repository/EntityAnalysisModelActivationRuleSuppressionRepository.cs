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

public class EntityAnalysisModelActivationRuleSuppressionRepository
{
    private readonly DbContext _dbContext;
    private readonly int? _tenantRegistryId;
    private readonly string _userName;

    public EntityAnalysisModelActivationRuleSuppressionRepository(DbContext dbContext, string userName)
    {
        _dbContext = dbContext;
        _userName = userName;
        _tenantRegistryId = _dbContext.UserInTenant.Where(w => w.User == _userName)
            .Select(s => s.TenantRegistryId).FirstOrDefault();
    }

    public EntityAnalysisModelActivationRuleSuppressionRepository(DbContext dbContext, int tenantRegistryId)
    {
        _dbContext = dbContext;
        _tenantRegistryId = tenantRegistryId;
    }


    public EntityAnalysisModelActivationRuleSuppressionRepository(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IEnumerable<EntityAnalysisModelActivationRuleSuppression> Get()
    {
        return _dbContext.EntityAnalysisModelActivationRuleSuppression
            .Where(w =>
                w.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId || !_tenantRegistryId.HasValue
            );
    }

    public IEnumerable<EntityAnalysisModelActivationRuleSuppression> GetByEntityAnalysisModelGuidOrderById(
        Guid entityAnalysisModelGuid)
    {
        return _dbContext.EntityAnalysisModelActivationRuleSuppression
            .Where(w =>
                (w.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId || !_tenantRegistryId.HasValue)
                && w.EntityAnalysisModelGuid == entityAnalysisModelGuid
                && (w.Deleted == 0 || w.Deleted == null))
            .OrderBy(o => o.Id);
    }

    public EntityAnalysisModelActivationRuleSuppression GetById(int id)
    {
        return _dbContext.EntityAnalysisModelActivationRuleSuppression.FirstOrDefault(w =>
            (w.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId || !_tenantRegistryId.HasValue)
            && w.Id == id && (w.Deleted == 0 || w.Deleted == null));
    }

    public EntityAnalysisModelActivationRuleSuppression Insert(EntityAnalysisModelActivationRuleSuppression model)
    {
        model.CreatedUser = _userName;
        model.CreatedDate = DateTime.Now;
        model.Version = 1;
        model.Id = _dbContext.InsertWithInt32Identity(model);
        return model;
    }

    public EntityAnalysisModelActivationRuleSuppression Update(EntityAnalysisModelActivationRuleSuppression model)
    {
        EntityAnalysisModelActivationRuleSuppression existing;

        if (model.Id != 0) //TODO[RC}: This needs to be explained or rethought.  Is it ok to have two upset keys?
            existing = _dbContext.EntityAnalysisModelActivationRuleSuppression
                .FirstOrDefault(w =>
                    w.Id == model.Id
                    && (w.Deleted == 0 || w.Deleted == null));
        else
            existing = _dbContext.EntityAnalysisModelActivationRuleSuppression
                .FirstOrDefault(w => w.SuppressionKey == model.SuppressionKey
                                     && w.SuppressionKeyValue == model.SuppressionKeyValue
                                     && w.EntityAnalysisModelGuid == model.EntityAnalysisModelGuid
                                     && w.EntityAnalysisModelActivationRuleName ==
                                     model.EntityAnalysisModelActivationRuleName
                                     && (w.Deleted == 0 || w.Deleted == null));

        if (existing != null)
        {
            Delete(existing.Id);
        }
        else
        {
            model.CreatedUser = _userName;
            model.CreatedDate = DateTime.Now;
            model.Version = 1;
            var id = _dbContext.InsertWithInt32Identity(model);
            model.Id = id;
        }

        return model;
    }

    public void Delete(int id)
    {
        var records = _dbContext.EntityAnalysisModelActivationRuleSuppression
            .Where(d =>
                (d.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId || !_tenantRegistryId.HasValue)
                && d.Id == id
                && (d.Deleted == 0 || d.Deleted == null))
            .Set(s => s.Deleted, Convert.ToByte(1))
            .Set(s => s.DeletedDate, DateTime.Now)
            .Set(s => s.DeletedUser, _userName)
            .Update();

        if (records == 0) throw new KeyNotFoundException();
    }

    public void DeleteByTenantRegistryId(int tenantRegistryId, int importId)
    {
        var records = _dbContext.EntityAnalysisModelActivationRuleSuppression
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