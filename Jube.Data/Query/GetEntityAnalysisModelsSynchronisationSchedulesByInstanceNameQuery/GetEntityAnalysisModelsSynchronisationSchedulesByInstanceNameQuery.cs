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

namespace Jube.Data.Query.GetEntityAnalysisModelsSynchronisationSchedulesByInstanceNameQuery
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;

    public class GetEntityAnalysisModelsSynchronisationSchedulesByInstanceNameQuery(DbContext dbContext)
    {
        public async Task<List<GetEntityAnalysisModelsSynchronisationSchedulesByInstanceNameQueryDto>>
            ExecuteAsync(string instance, CancellationToken token = default)
        {
            var now = DateTime.UtcNow;

            var scheduled = await dbContext.EntityAnalysisModelSynchronisationSchedule
                .Where(w => w.TenantRegistryId != null)
                .Select(s => s.TenantRegistryId.Value)
                .Distinct()
                .ToListAsync(token).ConfigureAwait(false);

            var pending = await (from s in dbContext.EntityAnalysisModelSynchronisationSchedule
                    from e in dbContext.EntityAnalysisModelSynchronisationNodeStatusEntry.LeftJoin(e
                        => e.TenantRegistryId == s.TenantRegistryId && e.Instance == instance)
                    where s.TenantRegistryId != null
                          && s.ScheduleDate != null
                          && s.ScheduleDate <= now
                          && (e.SynchronisedDate == null || s.ScheduleDate > e.SynchronisedDate)
                    select new
                    {
                        TenantRegistryId = s.TenantRegistryId.Value,
                        s.EntityAnalysisModelId
                    })
                .ToListAsync(token).ConfigureAwait(false);

            return scheduled.Select(tenantRegistryId =>
            {
                var rows = pending.Where(w => w.TenantRegistryId == tenantRegistryId).ToList();
                var wholeTenant = rows.Any(a => a.EntityAnalysisModelId == null);

                return new GetEntityAnalysisModelsSynchronisationSchedulesByInstanceNameQueryDto
                {
                    TenantRegistryId = tenantRegistryId,
                    SynchronisationPending = rows.Count > 0,
                    EntityAnalysisModelIds = rows.Count == 0 || wholeTenant
                        ? null
                        : rows.Select(s => s.EntityAnalysisModelId.Value).Distinct().ToArray()
                };
            }).ToList();
        }
    }
}