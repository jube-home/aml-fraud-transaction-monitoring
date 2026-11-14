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

using Jube.Service.Agent.Models;
using Jube.Service.Repository.EntityAnalysisModelSynchronisationSchedule;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddEntityAnalysisModelSynchronisationSchedule(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "EntityAnalysisModelSynchronisationScheduleGetCurrent", OperationKind.Read, true,
                    false,
                    "Returns the most recently scheduled model-synchronisation row for the caller's tenant, or for one model when given.",
                    EntityAnalysisModelSynchronisationScheduleService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelSynchronisationScheduleList", OperationKind.Read, true,
                    false,
                    "Lists scheduled model-synchronisation rows for the caller's tenant, keyset-paged and capped.",
                    EntityAnalysisModelSynchronisationScheduleService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelSynchronisationScheduleFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over Synchronisation Schedules may use.",
                    EntityAnalysisModelSynchronisationScheduleService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelSynchronisationScheduleFilter", OperationKind.Read, true, false,
                    "Returns the Synchronisation Schedules matching query builder JSON, keyset-paged and capped.",
                    EntityAnalysisModelSynchronisationScheduleService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelSynchronisationScheduleCount", OperationKind.Read, true, false,
                    "Counts the Synchronisation Schedules matching query builder JSON, optionally grouped by a field.",
                    EntityAnalysisModelSynchronisationScheduleService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelSynchronisationScheduleCreate", OperationKind.Write, false,
                    false,
                    "Schedules model synchronisation across the cluster, for one model or for every model in the tenant; calling twice creates two rows.",
                    EntityAnalysisModelSynchronisationScheduleService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelSynchronisationScheduleValidate", OperationKind.Read, true, false,
                    "Validates a Synchronisation Schedule without saving it and returns every failure.",
                    EntityAnalysisModelSynchronisationScheduleService.RequiredPermissions)
            ]);
        }
    }
}