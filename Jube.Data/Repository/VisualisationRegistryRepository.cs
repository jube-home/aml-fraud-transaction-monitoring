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

public class VisualisationRegistryRepository
{
    private readonly DbContext _dbContext;
    private readonly int _tenantRegistryId;
    private readonly string _userName;

    public VisualisationRegistryRepository(DbContext dbContext, string userName)
    {
        _dbContext = dbContext;
        _userName = userName;
        _tenantRegistryId = _dbContext.UserInTenant.Where(w => w.User == _userName)
            .Select(s => s.TenantRegistryId).FirstOrDefault();
    }

    public VisualisationRegistryRepository(DbContext dbContext, int tenantRegistryId)
    {
        _dbContext = dbContext;
        _tenantRegistryId = tenantRegistryId;
    }

    public IEnumerable<VisualisationRegistry> GetOrderById()
    {
        return _dbContext.VisualisationRegistry.Where(w =>
                w.TenantRegistryId == _tenantRegistryId
                && (w.Deleted == 0 || w.Deleted == null))
            .OrderBy(o => o.Id);
    }

    public VisualisationRegistry GetByGuid(Guid guid)
    {
        return _dbContext.VisualisationRegistry.FirstOrDefault(w => w.Guid == guid
                                                                    && w.TenantRegistryId == _tenantRegistryId
                                                                    && (w.Deleted == 0 || w.Deleted == null));
    }

    public VisualisationRegistry GetById(int id)
    {
        return _dbContext.VisualisationRegistry.FirstOrDefault(w => w.Id == id
                                                                    && w.TenantRegistryId == _tenantRegistryId
                                                                    && (w.Deleted == 0 || w.Deleted == null));
    }


    public IEnumerable<VisualisationRegistry> GetByShowInDirectory()
    {
        return _dbContext.VisualisationRegistry.Where(w => w.ShowInDirectory == 1
                                                           && w.Active == 1
                                                           && w.TenantRegistryId == _tenantRegistryId
                                                           && (w.Deleted == 0 || w.Deleted == null));
    }


    public VisualisationRegistry Insert(VisualisationRegistry model)
    {
        model.CreatedUser = _userName ?? model.CreatedUser;
        model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
        model.CreatedDate = DateTime.Now;
        model.TenantRegistryId = _tenantRegistryId;
        model.Version = 1;
        model.Id = _dbContext.InsertWithInt32Identity(model);

        return model;
    }

    public VisualisationRegistry Update(VisualisationRegistry model)
    {
        var existing = _dbContext.VisualisationRegistry.FirstOrDefault(w => w.Id
                                                                            == model.Id
                                                                            && w.TenantRegistryId == _tenantRegistryId
                                                                            && (w.Deleted == 0 || w.Deleted == null)
                                                                            && (w.Locked == 0 || w.Locked == null));

        if (existing == null) throw new KeyNotFoundException();

        model.CreatedUser = _userName;
        model.CreatedDate = DateTime.Now;
        model.TenantRegistryId = _tenantRegistryId;
        model.Version = existing.Version + 1;
        model.Guid = existing.Guid;

        _dbContext.Update(model);

        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<VisualisationRegistry, VisualisationRegistryVersion>();
        });
        var mapper = new Mapper(config);

        var audit = mapper.Map<VisualisationRegistryVersion>(existing);
        audit.VisualisationRegistryId = existing.Id;

        return model;
    }

    public void Delete(int id)
    {
        var record = _dbContext.VisualisationRegistry
            .FirstOrDefault(u => u.TenantRegistryId == _tenantRegistryId
                                 && u.Id == id);

        if (record == null) throw new KeyNotFoundException();

        record.DeletedUser = _userName;
        record.DeletedDate = DateTime.Now;
        record.Deleted = 1;
        _dbContext.Update(record);
    }

    public void DeleteByTenantRegistryId(int tenantRegistryId, int importId)
    {
        _dbContext.VisualisationRegistry
            .Where(d => d.TenantRegistryId == _tenantRegistryId
                        && d.TenantRegistryId == tenantRegistryId
                        && (d.Deleted == 0 || d.Deleted == null))
            .Set(s => s.ImportId, importId)
            .Set(s => s.Deleted, Convert.ToByte(1))
            .Set(s => s.DeletedDate, DateTime.Now)
            .Update();
    }
}