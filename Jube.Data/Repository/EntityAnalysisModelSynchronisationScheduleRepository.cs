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

public class EntityAnalysisModelSynchronisationScheduleRepository
{
    private readonly DbContext _dbContext;
    private readonly int? _tenantRegistryId;
    private readonly string _userName;

    public EntityAnalysisModelSynchronisationScheduleRepository(DbContext dbContext, string userName)
    {
        _dbContext = dbContext;
        _userName = userName;
        _tenantRegistryId = _dbContext.UserInTenant.Where(w => w.User == _userName)
            .Select(s => s.TenantRegistryId).FirstOrDefault();
    }

    public EntityAnalysisModelSynchronisationScheduleRepository(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IEnumerable<EntityAnalysisModelSynchronisationSchedule> Get()
    {
        return _dbContext.EntityAnalysisModelSynchronisationSchedule
            .Where(w => w.TenantRegistryId == _tenantRegistryId || !_tenantRegistryId.HasValue);
    }

    public EntityAnalysisModelSynchronisationSchedule GetCurrent()
    {
        return _dbContext.EntityAnalysisModelSynchronisationSchedule.Where(w
                => w.TenantRegistryId == _tenantRegistryId)
            .OrderByDescending(o => o.Id)
            .FirstOrDefault();
    }

    public EntityAnalysisModelSynchronisationSchedule Insert(EntityAnalysisModelSynchronisationSchedule model)
    {
        model.CreatedUser = _userName;
        model.TenantRegistryId = _tenantRegistryId;
        model.CreatedDate = DateTime.Now;

        if (model.ScheduleDate == default(DateTime) || model.ScheduleDate == null)
            model.ScheduleDate = model.CreatedDate;

        model.Id = _dbContext.InsertWithInt32Identity(model);
        return model;
    }
}