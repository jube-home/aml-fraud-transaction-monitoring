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

namespace Jube.Data.Query
{
    public class GetEntityAnalysisModelSynchronisationNodeStatusEntriesQuery
    {
        private readonly DbContext _dbContext;
        private readonly int _tenantRegistryId;

        public GetEntityAnalysisModelSynchronisationNodeStatusEntriesQuery(DbContext dbContext, string userName)
        {
            _dbContext = dbContext;
            _tenantRegistryId = _dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public IEnumerable<Dto> Execute()
        {
            var schedule = _dbContext.EntityAnalysisModelSynchronisationSchedule
                .Where(w => w.TenantRegistryId == _tenantRegistryId)
                .OrderByDescending(o => o.Id)
                .FirstOrDefault();

            if (schedule == null) return null;
            {
                var entries = _dbContext.EntityAnalysisModelSynchronisationNodeStatusEntry
                    .Where(w => w.TenantRegistryId == _tenantRegistryId)
                    .Select(s => new Dto
                    {
                        HeartbeatDate = s.HeartbeatDate.Value,
                        Instance = s.Instance,
                        SynchronisedDate = s.SynchronisedDate ?? default(DateTime),
                        SynchronisationPending = schedule.ScheduleDate > s.SynchronisedDate &&
                                                 DateTime.Now > schedule.ScheduleDate
                                                 || !s.SynchronisedDate.HasValue,
                        InstanceAvailable = s.HeartbeatDate > DateTime.Now.AddMinutes(-2)
                    });

                return entries;
            }
        }

        public class Dto
        {
            public bool SynchronisationPending { get; set; }
            public bool InstanceAvailable { get; set; }
            public string Instance { get; set; }
            public DateTime SynchronisedDate { get; set; }
            public DateTime HeartbeatDate { get; set; }
        }
    }
}