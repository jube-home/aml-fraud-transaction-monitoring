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
using System.Linq;
using Jube.Data.Context;
using Jube.Data.Poco;
using LinqToDB;

namespace Jube.Data.Repository;

public class ExhaustiveSearchInstanceTrialInstanceTopologyTrialRepository
{
    private readonly DbContext _dbContext;
    private readonly int? _tenantRegistryId;

    public ExhaustiveSearchInstanceTrialInstanceTopologyTrialRepository(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public ExhaustiveSearchInstanceTrialInstanceTopologyTrialRepository(DbContext dbContext, int tenantRegistryId)
    {
        _dbContext = dbContext;
        _tenantRegistryId = tenantRegistryId;
    }

    public ExhaustiveSearchInstanceTrialInstanceTopologyTrial Insert(
        ExhaustiveSearchInstanceTrialInstanceTopologyTrial model)
    {
        model.Id = _dbContext.InsertWithInt32Identity(model);
        return model;
    }

    public IQueryable<ExhaustiveSearchInstanceTrialInstanceTopologyTrial>
        GetByExhaustiveSearchInstanceTrialInstanceIdOrderById(
            int exhaustiveSearchInstanceTrialInstanceId)
    {
        return _dbContext.ExhaustiveSearchInstanceTrialInstanceTopologyTrial.Where(w =>
                w.ExhaustiveSearchInstanceTrialInstanceId == exhaustiveSearchInstanceTrialInstanceId)
            .OrderBy(o => o.Id);
    }

    public void DeleteByTenantRegistryId(int tenantRegistryId, int importId)
    {
        var records = _dbContext.ExhaustiveSearchInstanceTrialInstanceTopologyTrial
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