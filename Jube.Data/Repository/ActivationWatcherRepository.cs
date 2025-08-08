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
using LinqToDB.Data;

namespace Jube.Data.Repository;

public class ActivationWatcherRepository
{
    private readonly DbContext _dbContext;
    private readonly int? _tenantRegistryId;

    public ActivationWatcherRepository(DbContext dbContext, string userName)
    {
        _dbContext = dbContext;
        _tenantRegistryId = dbContext.UserInTenant.Where(w => w.User == userName)
            .Select(s => s.TenantRegistryId).FirstOrDefault();
    }

    public ActivationWatcherRepository(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public ActivationWatcher GetLast()
    {
        return _dbContext.ActivationWatcher.OrderByDescending(s => s.Id).FirstOrDefault();
    }

    public IEnumerable<ActivationWatcher> GetAllSinceId(int id, int limit)
    {
        return _dbContext.ActivationWatcher
            .Where(w => w.Id > id)
            .OrderByDescending(s => s.Id).Take(limit);
    }

    public IEnumerable<ActivationWatcher> GetByDateRangeAscending(DateTime dateFrom, DateTime dateTo, int limit)
    {
        return _dbContext.ActivationWatcher
            .Where(w => w.CreatedDate > dateFrom && w.CreatedDate <= dateTo
                                                 && w.TenantRegistryId == _tenantRegistryId)
            .OrderBy(s => s.Id).Take(limit);
    }

    public void BulkCopy(List<ActivationWatcher> models)
    {
        _dbContext.BulkCopy(models);
    }
}