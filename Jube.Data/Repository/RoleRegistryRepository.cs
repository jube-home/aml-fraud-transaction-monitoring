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

public class RoleRegistryRepository
{
    private readonly DbContext _dbContext;
    private readonly int _tenantRegistryId;
    private readonly string _userName;

    public RoleRegistryRepository(DbContext dbContext, string userName)
    {
        _dbContext = dbContext;
        _userName = userName;
        _tenantRegistryId = _dbContext.UserInTenant.Where(w => w.User == _userName)
            .Select(s => s.TenantRegistryId).FirstOrDefault();
    }

    public RoleRegistryRepository(DbContext dbContext, TenantRegistry tenantRegistry)
    {
        _dbContext = dbContext;
        _tenantRegistryId = tenantRegistry.Id;
    }

    public IEnumerable<RoleRegistry> Get()
    {
        return _dbContext.RoleRegistry.Where(w => w.TenantRegistryId == _tenantRegistryId
                                                  && (w.Deleted == 0 || w.Deleted == null));
    }

    public RoleRegistry GetById(int id)
    {
        return _dbContext.RoleRegistry.FirstOrDefault(w => w.Id == id
                                                           && w.TenantRegistryId == _tenantRegistryId
                                                           && (w.Deleted == 0 || w.Deleted == null));
    }

    public RoleRegistry Insert(RoleRegistry model)
    {
        model.CreatedUser = _userName;
        model.TenantRegistryId = _tenantRegistryId;
        model.Version = 1;
        model.CreatedDate = DateTime.Now;
        model.Guid = Guid.NewGuid();
        model.Id = _dbContext.InsertWithInt32Identity(model);
        return model;
    }

    public RoleRegistry Update(RoleRegistry model)
    {
        var existing = _dbContext.RoleRegistry
            .FirstOrDefault(u => u.TenantRegistryId == _tenantRegistryId
                                 && u.Id == model.Id
                                 && (u.Deleted == 0 || u.Deleted == null)
                                 && (u.Locked == 0 || u.Locked == null));

        if (existing == null) throw new KeyNotFoundException();

        model.Version = existing.Version + 1;
        model.Guid = existing.Guid;
        model.CreatedUser = _userName;
        model.CreatedDate = DateTime.Now;
        model.TenantRegistryId = _tenantRegistryId;

        _dbContext.Update(model);

        var config = new MapperConfiguration(cfg => { cfg.CreateMap<RoleRegistry, RoleRegistryVersion>(); });
        var mapper = new Mapper(config);

        var audit = mapper.Map<RoleRegistryVersion>(existing);
        audit.RoleRegistryId = existing.Id;

        _dbContext.Insert(audit);

        return model;
    }

    public void Delete(int id)
    {
        var records = _dbContext.RoleRegistry
            .Where(d => d.Id == id
                        && d.TenantRegistryId == _tenantRegistryId
                        && (d.Deleted == 0 || d.Deleted == null)
                        && (d.Locked == 0 || d.Locked == null)).Delete();

        if (records == 0) throw new KeyNotFoundException();
    }
}