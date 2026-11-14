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

using Jube.Data.Context;
using Jube.Engine.CaseEscalation;
using Jube.Service.Reactivity.Interfaces;
using log4net;

namespace Jube.Service.Query.CaseChecklist
{
    public sealed class ServiceCaseChecklistSlaSweep(IServiceChangeBus serviceChangeBus, TimeProvider clock, ILog log)
        : ICaseChecklistSlaSweep
    {
        public ServiceCaseChecklistSlaSweep(IServiceChangeBus serviceChangeBus)
            : this(serviceChangeBus, TimeProvider.System, LogManager.GetLogger(typeof(ServiceCaseChecklistSlaSweep)))
        {
        }

        public async Task<int> SweepAsync(DbContext dbContext, int batchSize, CancellationToken token = default)
        {
            var recorded = await new CaseChecklistSlaBreachRecorder(dbContext, clock, log)
                .SweepAsync(batchSize, token).ConfigureAwait(false);

            foreach (var (tenantRegistryId, caseId) in recorded.Distinct())
            {
                CaseChecklistChangeEvents.Publish(serviceChangeBus, tenantRegistryId, caseId,
                    CaseChecklistService.SystemActor, log);
            }

            return recorded.Count;
        }
    }
}
