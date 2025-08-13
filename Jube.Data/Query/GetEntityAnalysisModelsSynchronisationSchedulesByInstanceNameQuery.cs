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
using LinqToDB;

namespace Jube.Data.Query;

public class GetEntityAnalysisModelsSynchronisationSchedulesByInstanceNameQuery
{
    private readonly DbContext _dbContext;

    public GetEntityAnalysisModelsSynchronisationSchedulesByInstanceNameQuery(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public List<Dto>
        Execute(string instance)
    {
        var tenants = (from e
                    in _dbContext.EntityAnalysisModelSynchronisationNodeStatusEntry
                from t
                    in _dbContext.TenantRegistry.RightJoin
                    (t => t.Id == e.TenantRegistryId &&
                          e.Instance == instance)
                select new
                {
                    t.Id,
                    SynchronisedDate = e.SynchronisedDate ?? default(DateTime)
                })
            .ToDictionary(s => s.Id, s => s.SynchronisedDate);

        return (from y in _dbContext.EntityAnalysisModelSynchronisationSchedule
                join m in from t in _dbContext.EntityAnalysisModelSynchronisationSchedule
                    group t by t.TenantRegistryId
                    into g
                    select new
                    {
                        TenantRegistryId = g.Key,
                        EntityAnalysisModelSyncronisationScheduleId =
                            (from t2 in g select t2.Id).Max()
                    } on y.Id equals m
                        .EntityAnalysisModelSyncronisationScheduleId
                select
                    new Dto
                    {
                        SynchronisationPending = y.ScheduleDate > tenants[y.TenantRegistryId.Value]
                                                 && DateTime.Now > y.ScheduleDate,
                        TenantRegistryId = y.TenantRegistryId.Value
                    }
            ).ToList();
    }

    public class Dto
    {
        public bool SynchronisationPending { get; set; }
        public int TenantRegistryId { get; set; }
    }
}