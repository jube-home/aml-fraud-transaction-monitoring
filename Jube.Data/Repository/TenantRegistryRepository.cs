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

public class TenantRegistryRepository
{
    private readonly DbContext _dbContext;
    private readonly string _userName;

    public TenantRegistryRepository(DbContext dbContext, string userName)
    {
        _dbContext = dbContext;
        _userName = userName;
    }

    public IEnumerable<TenantRegistry> Get()
    {
        return _dbContext.TenantRegistry.Where(w => w.Deleted == 0 || w.Deleted == null);
    }

    public TenantRegistry GetById(int id)
    {
        return _dbContext.TenantRegistry.FirstOrDefault(w => w.Id == id
                                                             && (w.Deleted == 0 || w.Deleted == null));
    }

    public IEnumerable<TenantRegistry> GetByFilter(string filter)
    {
        return _dbContext.TenantRegistry.Where(w => w.Name.ToLower().Contains(filter)
                                                    && (w.Deleted == 0 || w.Deleted == null));
    }

    public TenantRegistry Insert(TenantRegistry model)
    {
        model.CreatedUser = _userName;
        model.Version = 1;
        model.CreatedDate = DateTime.Now;
        model.Id = _dbContext.InsertWithInt32Identity(model);
        return model;
    }

    public TenantRegistry Update(TenantRegistry model)
    {
        var existing = _dbContext.TenantRegistry
            .FirstOrDefault(w => w.Id
                                 == model.Id
                                 && (w.Deleted == 0 || w.Deleted == null)
                                 && (w.Locked == 0 || w.Locked == null));

        if (existing == null) throw new KeyNotFoundException();

        model.CreatedUser = _userName;
        model.CreatedDate = DateTime.Now;
        model.Version = existing.Version + 1;
        model.CreatedUser = _userName;

        _dbContext.Update(model);

        var config = new MapperConfiguration(cfg => { cfg.CreateMap<TenantRegistry, TenantRegistryVersion>(); });
        var mapper = new Mapper(config);

        var audit = mapper.Map<TenantRegistryVersion>(existing);
        audit.TenantRegistryId = existing.Id;

        _dbContext.Insert(audit);

        return model;
    }

    public void Delete(int id)
    {
        var records = _dbContext.TenantRegistry
            .Where(d => d.Id == id
                        && (d.Locked == 0 || d.Locked == null)
                        && (d.Deleted == 0 || d.Deleted == null))
            .Set(s => s.Deleted, Convert.ToByte(1))
            .Set(s => s.DeletedDate, DateTime.Now)
            .Set(s => s.DeletedUser, _userName)
            .Update();

        if (records == 0) throw new KeyNotFoundException();
    }
}