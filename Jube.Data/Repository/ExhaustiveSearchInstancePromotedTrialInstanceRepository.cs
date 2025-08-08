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

public class ExhaustiveSearchInstancePromotedTrialInstanceRepository
{
    private readonly DbContext _dbContext;
    private readonly int? _tenantRegistryId;

    public ExhaustiveSearchInstancePromotedTrialInstanceRepository(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public ExhaustiveSearchInstancePromotedTrialInstanceRepository(DbContext dbContext, int tenantRegistryId)
    {
        _dbContext = dbContext;
        _tenantRegistryId = tenantRegistryId;
    }

    public ExhaustiveSearchInstancePromotedTrialInstanceRepository(DbContext dbContext, string userName)
    {
        _dbContext = dbContext;
        _tenantRegistryId = _dbContext.UserInTenant.Where(w => w.User == userName)
            .Select(s => s.TenantRegistryId).FirstOrDefault();
    }

    public void UpdateActive(int id, bool active)
    {
        var records = _dbContext.ExhaustiveSearchInstancePromotedTrialInstance
            .Where(d =>
                (d.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance
                    .EntityAnalysisModel.TenantRegistryId == _tenantRegistryId || !_tenantRegistryId.HasValue)
                && d.Id == id)
            .Set(s => s.Active, (byte)(active ? 1 : 0))
            .Update();

        if (records == 0) throw new KeyNotFoundException();
    }

    public ExhaustiveSearchInstancePromotedTrialInstance Insert(ExhaustiveSearchInstancePromotedTrialInstance model)
    {
        model.CreatedDate = DateTime.Now;
        model.Id = _dbContext.InsertWithInt32Identity(model);
        return model;
    }

    public IQueryable<ExhaustiveSearchInstancePromotedTrialInstance>
        GetByExhaustiveSearchInstanceTrialInstanceIdOrderById(
            int exhaustiveSearchInstanceTrialInstanceId)
    {
        return _dbContext.ExhaustiveSearchInstancePromotedTrialInstance.Where(w =>
                w.ExhaustiveSearchInstanceTrialInstanceId == exhaustiveSearchInstanceTrialInstanceId)
            .OrderBy(o => o.Id);
    }

    public void DeleteByTenantRegistryId(int tenantRegistryId, int importId)
    {
        var records = _dbContext.ExhaustiveSearchInstancePromotedTrialInstance
            .Where(d =>
                (d.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.EntityAnalysisModel
                    .TenantRegistryId == _tenantRegistryId || !_tenantRegistryId.HasValue)
                && d.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstance.EntityAnalysisModel
                    .TenantRegistryId == tenantRegistryId
                && (d.Deleted == 0 || d.Deleted == null))
            .Set(s => s.ImportId, importId)
            .Set(s => s.Deleted, Convert.ToByte(1))
            .Set(s => s.DeletedDate, DateTime.Now)
            .Update();
    }
}