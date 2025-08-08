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
using Jube.Data.Repository.Interface;
using LinqToDB;

namespace Jube.Data.Repository;

public class ExhaustiveSearchInstanceVariableRepository : IGenericRepository
{
    private readonly DbContext _dbContext;
    private readonly int? _tenantRegistryId;

    public ExhaustiveSearchInstanceVariableRepository(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public ExhaustiveSearchInstanceVariableRepository(DbContext dbContext, int tenantRegistryId)
    {
        _dbContext = dbContext;
        _tenantRegistryId = tenantRegistryId;
    }

    public int Insert(object arg)
    {
        return _dbContext.InsertWithInt32Identity((ExhaustiveSearchInstanceVariable)arg);
    }

    public void UpdateCorrelation(int id, double correlation, int rank)
    {
        _dbContext.ExhaustiveSearchInstanceVariable
            .Where(d => d.Id == id)
            .Set(s => s.Correlation, correlation)
            .Set(s => s.CorrelationAbsRank, rank)
            .Update();
    }

    public IQueryable<ExhaustiveSearchInstanceVariable> GetByExhaustiveSearchInstanceIdOrderById(
        int exhaustiveSearchInstanceId)
    {
        return _dbContext.ExhaustiveSearchInstanceVariable.Where(w =>
                w.ExhaustiveSearchInstanceId == exhaustiveSearchInstanceId)
            .OrderBy(o => o.Id);
    }

    public void DeleteByTenantRegistryId(int tenantRegistryId, int importId)
    {
        var records = _dbContext.ExhaustiveSearchInstanceVariable
            .Where(d =>
                (d.ExhaustiveSearchInstance.EntityAnalysisModel.TenantRegistryId == _tenantRegistryId ||
                 !_tenantRegistryId.HasValue)
                && d.ExhaustiveSearchInstance.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                && (d.Deleted == 0 || d.Deleted == null))
            .Set(s => s.ImportId, importId)
            .Set(s => s.Deleted, Convert.ToByte(1))
            .Set(s => s.DeletedDate, DateTime.Now)
            .Update();
    }
}