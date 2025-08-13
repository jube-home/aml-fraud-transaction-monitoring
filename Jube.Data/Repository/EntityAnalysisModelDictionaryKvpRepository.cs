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

public class EntityAnalysisModelDictionaryKvpRepository
{
    private readonly DbContext _dbContext;
    private readonly int? _tenantRegistryId;
    private readonly string _userName;

    public EntityAnalysisModelDictionaryKvpRepository(DbContext dbContext, string userName)
    {
        _dbContext = dbContext;
        _userName = userName;
        _tenantRegistryId = _dbContext.UserInTenant.Where(w => w.User == _userName)
            .Select(s => s.TenantRegistryId).FirstOrDefault();
    }

    public EntityAnalysisModelDictionaryKvpRepository(DbContext dbContext, int tenantRegistryId)
    {
        _dbContext = dbContext;
        _tenantRegistryId = tenantRegistryId;
    }

    public EntityAnalysisModelDictionaryKvpRepository(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IEnumerable<EntityAnalysisModelDictionaryKvp> Get()
    {
        return _dbContext.EntityAnalysisModelDictionaryKvp
            .Where(w =>
                w.EntityAnalysisModelDictionary.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId ||
                !_tenantRegistryId.HasValue);
    }

    public EntityAnalysisModelDictionaryKvp GetByIdKvpKey(int id, string key)
    {
        return _dbContext.EntityAnalysisModelDictionaryKvp.FirstOrDefault(w =>
            (w.EntityAnalysisModelDictionary.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId ||
             !_tenantRegistryId.HasValue)
            && w.EntityAnalysisModelDictionaryId == id && w.KvpKey == key
            && (w.Deleted == 0 || w.Deleted == null));
    }

    public IEnumerable<EntityAnalysisModelDictionaryKvp> GetByEntityAnalysisModelDictionaryIdOrderById(
        int entityAnalysisModelDictionaryId)
    {
        return _dbContext.EntityAnalysisModelDictionaryKvp
            .Where(w =>
                (w.EntityAnalysisModelDictionary.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId ||
                 !_tenantRegistryId.HasValue)
                && (w.EntityAnalysisModelDictionary.EntityAnalysisModel.Deleted == 0 ||
                    w.EntityAnalysisModelDictionary.EntityAnalysisModel.Deleted == null)
                && w.EntityAnalysisModelDictionaryId == entityAnalysisModelDictionaryId &&
                (w.Deleted == 0 || w.Deleted == null))
            .OrderBy(o => o.Id);
    }

    public EntityAnalysisModelDictionaryKvp GetById(int id)
    {
        return _dbContext.EntityAnalysisModelDictionaryKvp.FirstOrDefault(w =>
            (w.EntityAnalysisModelDictionary.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId ||
             !_tenantRegistryId.HasValue)
            && w.EntityAnalysisModelDictionaryId == id && (w.Deleted == 0 || w.Deleted == null));
    }

    public EntityAnalysisModelDictionaryKvp Insert(EntityAnalysisModelDictionaryKvp model)
    {
        model.CreatedUser = _userName ?? model.CreatedUser;
        model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
        model.CreatedDate = DateTime.Now;
        model.Version = 1;
        model.Id = _dbContext.InsertWithInt32Identity(model);
        return model;
    }

    public EntityAnalysisModelDictionaryKvp
        Update(EntityAnalysisModelDictionaryKvp model)
    {
        var existing = _dbContext.EntityAnalysisModelDictionaryKvp
            .FirstOrDefault(w => w.Id == model.Id
                                 && w.EntityAnalysisModelDictionary.EntityAnalysisModel.TenantRegistryId ==
                                 _tenantRegistryId
                                 && (w.Deleted == 0 || w.Deleted == null));

        if (existing == null) throw new KeyNotFoundException();

        model.Version = existing.Version + 1;
        model.Guid = existing.Guid;
        model.CreatedUser = _userName;
        model.CreatedDate = DateTime.Now;

        _dbContext.Update(model);

        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<EntityAnalysisModelDictionaryKvp, EntityAnalysisModelDictionaryKvpVersion>();
        });
        var mapper = new Mapper(config);

        var audit = mapper.Map<EntityAnalysisModelDictionaryKvpVersion>(existing);
        audit.EntityAnalysisModelDictionaryKvpId = existing.Id;

        _dbContext.Insert(audit);

        return model;
    }

    public void Delete(int id)
    {
        var records = _dbContext.EntityAnalysisModelDictionaryKvp
            .Where(d =>
                (d.EntityAnalysisModelDictionary.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId ||
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
        var records = _dbContext.EntityAnalysisModelDictionaryKvp
            .Where(d =>
                (d.EntityAnalysisModelDictionary.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId ||
                 !_tenantRegistryId.HasValue)
                && d.EntityAnalysisModelDictionary.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                && (d.Deleted == 0 || d.Deleted == null))
            .Set(s => s.ImportId, importId)
            .Set(s => s.Deleted, Convert.ToByte(1))
            .Set(s => s.DeletedDate, DateTime.Now)
            .Update();
    }
}