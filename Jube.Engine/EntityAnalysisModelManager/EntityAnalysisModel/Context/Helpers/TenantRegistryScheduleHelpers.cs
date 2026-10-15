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

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Data.Context;
    using log4net;
    using Jube.Data.Query.GetEntityAnalysisModelsSynchronisationSchedulesByInstanceNameQuery;

    public static class TenantRegistryScheduleHelpers
    {
        public static async Task<List<GetEntityAnalysisModelsSynchronisationSchedulesByInstanceNameQueryDto>>
            GetScheduledAsync(ILog log, DbContext dbContext, CancellationToken token)
        {
            var values = new List<GetEntityAnalysisModelsSynchronisationSchedulesByInstanceNameQueryDto>();

            try
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        "Entity Start: Executing a fetch of all tenant schedules for the entity sub system using GetEntityAnalysisModelsSynchronisationSchedulesByInstanceNameQuery.");
                }

                var query = new GetEntityAnalysisModelsSynchronisationSchedulesByInstanceNameQuery(dbContext);
                values = await query.ExecuteAsync(Dns.GetHostName(), token).ConfigureAwait(false);

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        "Entity Start: Executed GetEntityAnalysisModelsSynchronisationSchedulesByInstanceNameQuery.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.Error($"GetTenantRegistrySchedule: has produced an error {ex}");
            }

            return values;
        }
    }
}