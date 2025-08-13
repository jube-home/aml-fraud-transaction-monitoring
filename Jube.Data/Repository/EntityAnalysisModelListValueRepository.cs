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

public class EntityAnalysisModelListValueRepository
{
    private readonly DbContext _dbContext;
    private readonly int? _tenantRegistryId;
    private readonly string _userName;

    public EntityAnalysisModelListValueRepository(DbContext dbContext, string userName)
    {
        _dbContext = dbContext;
        _userName = userName;
        _tenantRegistryId = _dbContext.UserInTenant.Where(w => w.User == _userName)
            .Select(s => s.TenantRegistryId).FirstOrDefault();
    }

    public EntityAnalysisModelListValueRepository(DbContext dbContext, int tenantRegistryId)
    {
        _dbContext = dbContext;
        _tenantRegistryId = tenantRegistryId;
    }

    public EntityAnalysisModelListValueRepository(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    //TODO[RC]: Check all deleted flags all repo
    public IEnumerable<EntityAnalysisModelListValue> Get()
    {
        return _dbContext.EntityAnalysisModelListValue
                .Where(w =>
                    w.EntityAnalysisModelList.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId ||
                    !_tenantRegistryId.HasValue)
            ;
    }

    public IEnumerable<EntityAnalysisModelListValue> GetByEntityAnalysisModelListIdOrderById(
        int entityAnalysisModelListId)
    {
        return _dbContext.EntityAnalysisModelListValue
            .Where(w =>
                (w.EntityAnalysisModelList.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId ||
                 !_tenantRegistryId.HasValue)
                && w.EntityAnalysisModelListId == entityAnalysisModelListId
                && (w.EntityAnalysisModelList.EntityAnalysisModel.Deleted == 0 ||
                    w.EntityAnalysisModelList.EntityAnalysisModel.Deleted == null)
                && (w.Deleted == 0 || w.Deleted == null))
            .OrderBy(o => o.Id);
    }

    public EntityAnalysisModelListValue GetById(int id)
    {
        return _dbContext.EntityAnalysisModelListValue.FirstOrDefault(w =>
            (w.EntityAnalysisModelList.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId ||
             !_tenantRegistryId.HasValue)
            && w.EntityAnalysisModelListId == id && (w.Deleted == 0 || w.Deleted == null));
    }

    public EntityAnalysisModelListValue Insert(EntityAnalysisModelListValue model)
    {
        model.CreatedUser = _userName ?? model.CreatedUser;
        model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
        model.CreatedDate = DateTime.Now;
        model.Version = 1;
        model.Id = _dbContext.InsertWithInt32Identity(model);
        return model;
    }

    public EntityAnalysisModelListValue
        Update(EntityAnalysisModelListValue model)

    {
        var existing = _dbContext.EntityAnalysisModelListValue
            .FirstOrDefault(w => w.Id
                                 == model.Id
                                 && w.EntityAnalysisModelList.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId
                                 && (w.Deleted == 0 || w.Deleted == null));

        if (existing == null) throw new KeyNotFoundException();

        model.Version = existing.Version + 1;
        model.Guid = existing.Guid;
        model.CreatedUser = _userName;
        model.CreatedDate = DateTime.Now;

        _dbContext.Update(model);

        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<EntityAnalysisModelListValue, EntityAnalysisModelListValueVersion>();
        });
        var mapper = new Mapper(config);

        var audit = mapper.Map<EntityAnalysisModelListValueVersion>(existing);
        audit.EntityAnalysisModelListValueId = existing.Id;

        _dbContext.Insert(audit);

        return model;
    }

    public void Delete(int id)
    {
        var records = _dbContext.EntityAnalysisModelListValue
            .Where(d =>
                (d.EntityAnalysisModelList.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId ||
                 !_tenantRegistryId.HasValue)
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
        _dbContext.EntityAnalysisModelListValue
            .Where(d =>
                (d.EntityAnalysisModelList.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId ||
                 !_tenantRegistryId.HasValue)
                && d.EntityAnalysisModelList.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                && (d.Deleted == 0 || d.Deleted == null))
            .Set(s => s.ImportId, importId)
            .Set(s => s.Deleted, Convert.ToByte(1))
            .Set(s => s.DeletedDate, DateTime.Now)
            .Update();
    }
}