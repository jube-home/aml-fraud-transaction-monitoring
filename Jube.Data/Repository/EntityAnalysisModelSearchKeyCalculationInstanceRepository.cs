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

public class EntityAnalysisModelSearchKeyCalculationInstanceRepository
{
    private readonly DbContext _dbContext;

    public EntityAnalysisModelSearchKeyCalculationInstanceRepository(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public EntityAnalysisModelSearchKeyCalculationInstance Insert(
        EntityAnalysisModelSearchKeyCalculationInstance model)
    {
        model.Id = _dbContext.InsertWithInt32Identity(model);
        return model;
    }

    public void UpdateDistinctValuesCount(int id,
        int distinctValuesCount)
    {
        _dbContext.EntityAnalysisModelSearchKeyCalculationInstance
            .Where(d => d.Id == id)
            .Set(s => s.DistinctValuesCount, distinctValuesCount)
            .Set(s => s.DistinctValuesUpdatedDate, DateTime.Now)
            .Update();
    }

    public void UpdateExpiredSearchKeyCacheCount(int id,
        int expiredSearchKeyCacheCount)
    {
        _dbContext.EntityAnalysisModelSearchKeyCalculationInstance
            .Where(d => d.Id == id)
            .Set(s => s.ExpiredSearchKeyCacheCount, expiredSearchKeyCacheCount)
            .Set(s => s.ExpiredSearchKeyCacheDate, DateTime.Now)
            .Update();
    }

    public void UpdateDistinctValuesProcessedValuesCount(int id,
        int distinctValuesProcessedValuesCount)
    {
        _dbContext.EntityAnalysisModelSearchKeyCalculationInstance
            .Where(d => d.Id == id)
            .Set(s => s.DistinctValuesProcessedValuesCount, distinctValuesProcessedValuesCount)
            .Set(s => s.DistinctValuesProcessedValuesUpdatedDate, DateTime.Now)
            .Update();
    }

    public void UpdateCompleted(int id)
    {
        _dbContext.EntityAnalysisModelSearchKeyCalculationInstance
            .Where(d => d.Id == id)
            .Set(s => s.Completed, (byte)1)
            .Set(s => s.CompletedDate, DateTime.Now)
            .Update();
    }
}