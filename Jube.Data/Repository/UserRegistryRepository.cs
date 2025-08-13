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

public class UserRegistryRepository
{
    private readonly DbContext _dbContext;
    private readonly int? _tenantRegistryId;
    private readonly string _userName;

    public UserRegistryRepository(DbContext dbContext, string userName)
    {
        _dbContext = dbContext;
        _userName = userName;
        _tenantRegistryId = _dbContext.UserInTenant.Where(w => w.User == _userName)
            .Select(s => s.TenantRegistryId).FirstOrDefault();
    }

    public UserRegistryRepository(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public UserRegistryRepository(DbContext dbContext, RoleRegistry roleRegistry)
    {
        _dbContext = dbContext;
        _tenantRegistryId = roleRegistry.TenantRegistryId;
    }

    public IEnumerable<UserRegistry> Get()
    {
        return _dbContext.UserRegistry
            .Where(w => w.RoleRegistry.TenantRegistryId == _tenantRegistryId || !_tenantRegistryId.HasValue);
    }

    public IEnumerable<UserRegistry> GetByRoleRegistryId(int roleRegistryId)
    {
        return _dbContext.UserRegistry
            .Where(w => (w.RoleRegistry.TenantRegistryId == _tenantRegistryId || !_tenantRegistryId.HasValue)
                        && w.RoleRegistryId == roleRegistryId && (w.Deleted == 0 || w.Deleted == null));
    }

    public UserRegistry GetById(int id)
    {
        return _dbContext.UserRegistry.FirstOrDefault(w =>
            (w.RoleRegistry.TenantRegistryId == _tenantRegistryId || !_tenantRegistryId.HasValue)
            && w.Id == id && (w.Deleted == 0 || w.Deleted == null));
    }

    public UserRegistry GetByUserName(string userName)
    {
        return _dbContext.UserRegistry.FirstOrDefault(w =>
            (w.RoleRegistry.TenantRegistryId == _tenantRegistryId || !_tenantRegistryId.HasValue)
            && w.Name == userName && (w.Deleted == 0 || w.Deleted == null));
    }

    public UserRegistry Insert(UserRegistry model)
    {
        if (!_tenantRegistryId.HasValue) throw new KeyNotFoundException();

        model.CreatedUser = _userName;
        model.Version = 1;
        model.CreatedDate = DateTime.Now;
        model.Guid = Guid.NewGuid();
        model.Id = _dbContext.InsertWithInt32Identity(model);

        var userInTenant = new UserInTenant
        {
            User = model.Name,
            TenantRegistryId = _tenantRegistryId.Value,
            SwitchedUser = _userName,
            SwitchedDate = DateTime.Now
        };

        _dbContext.Insert(userInTenant);

        return model;
    }

    public UserRegistry Update(UserRegistry model)
    {
        var existing = _dbContext.UserRegistry
            .FirstOrDefault(w => w.Id
                                 == model.Id
                                 && (w.Deleted == 0 || w.Deleted == null));

        if (existing == null) throw new KeyNotFoundException();

        model.Version = existing.Version + 1;
        model.Guid = existing.Guid;
        model.CreatedUser = _userName;
        model.CreatedDate = DateTime.Now;

        _dbContext.Update(model);

        var config = new MapperConfiguration(cfg => { cfg.CreateMap<UserRegistry, UserRegistryVersion>(); });
        var mapper = new Mapper(config);

        var audit = mapper.Map<UserRegistryVersion>(existing);
        audit.UserRegistryId = existing.Id;

        _dbContext.Insert(audit);

        return model;
    }

    public void ResetFailedPasswordCount(int id)
    {
        var records = _dbContext.UserRegistry
            .Where(d => (d.RoleRegistry.TenantRegistryId == _tenantRegistryId || !_tenantRegistryId.HasValue)
                        && d.Id == id
                        && (d.Deleted == 0 || d.Deleted == null))
            .Set(s => s.FailedPasswordCount, 0)
            .Update();

        if (records == 0) throw new KeyNotFoundException();
    }

    public void SetPassword(int id, string password, DateTime expiryDate)
    {
        var records = _dbContext.UserRegistry
            .Where(d => (d.RoleRegistry.TenantRegistryId == _tenantRegistryId || !_tenantRegistryId.HasValue)
                        && d.Id == id
                        && (d.Deleted == 0 || d.Deleted == null))
            .Set(s => s.Password, password)
            .Set(s => s.PasswordLocked, (byte)0)
            .Set(s => s.FailedPasswordCount, 0)
            .Set(s => s.PasswordExpiryDate, expiryDate)
            .Set(s => s.PasswordCreatedDate, DateTime.Now)
            .Update();

        if (records == 0) throw new KeyNotFoundException();
    }

    public void SetLocked(int id)
    {
        var records = _dbContext.UserRegistry
            .Where(d => (d.RoleRegistry.TenantRegistryId == _tenantRegistryId || !_tenantRegistryId.HasValue)
                        && d.Id == id
                        && (d.Deleted == 0 || d.Deleted == null))
            .Set(s => s.PasswordLocked, (byte)1)
            .Set(s => s.PasswordLockedDate, DateTime.Now)
            .Update();

        if (records == 0) throw new KeyNotFoundException();
    }

    public void IncrementFailedPassword(int id)
    {
        var existing = _dbContext.UserRegistry
            .FirstOrDefault(w => w.Id
                                 == id
                                 && (w.Deleted == 0 || w.Deleted == null));

        if (existing == null) throw new KeyNotFoundException();

        existing.FailedPasswordCount += 1;

        _dbContext.Update(existing);
    }

    public void Delete(int id)
    {
        var records = _dbContext.UserRegistry
            .Where(d => (d.RoleRegistry.TenantRegistryId == _tenantRegistryId || !_tenantRegistryId.HasValue)
                        && d.Id == id
                        && (d.Deleted == 0 || d.Deleted == null))
            .Set(s => s.Deleted, Convert.ToByte(1))
            .Set(s => s.DeletedDate, DateTime.Now)
            .Set(s => s.DeletedUser, _userName)
            .Update();

        if (records == 0) throw new KeyNotFoundException();
    }
}