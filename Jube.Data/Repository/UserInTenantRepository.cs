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

public class UserInTenantRepository
{
    private readonly DbContext _dbContext;
    private readonly UserInTenant _userInTenant;
    private readonly string _userName;

    public UserInTenantRepository(DbContext dbContext, string userName)
    {
        _dbContext = dbContext;
        _userInTenant = _dbContext.UserInTenant.FirstOrDefault(w => w.User == userName);
        _userName = userName;
    }

    public void Update(string user, int tenantRegistryId)
    {
        var existing = _dbContext.UserInTenant
            .FirstOrDefault(w => w.User == user);

        if (existing != null)
        {
            existing.TenantRegistryId = tenantRegistryId;
            existing.SwitchedUser = _userName;
            existing.SwitchedDate = DateTime.Now;

            _dbContext.Update(existing);

            var userInTenantSwitchLog = new UserInTenantSwitchLog
            {
                TenantRegistryId = tenantRegistryId,
                SwitchedDate = existing.SwitchedDate,
                SwitchedUser = _userName,
                UserInTenantId = existing.Id
            };

            _dbContext.Insert(userInTenantSwitchLog);
        }
        else
        {
            var userInTenant = new UserInTenant
            {
                TenantRegistryId = tenantRegistryId,
                SwitchedUser = _userName,
                SwitchedDate = DateTime.Now
            };

            _dbContext.Insert(userInTenant);
        }
    }

    public UserInTenant GetCurrentTenantRegistry()
    {
        return _userInTenant;
    }

    public IEnumerable<UserInTenant> Get()
    {
        return _dbContext.UserInTenant.Where(w =>
            w.TenantRegistryId == _userInTenant.TenantRegistryId);
    }
}