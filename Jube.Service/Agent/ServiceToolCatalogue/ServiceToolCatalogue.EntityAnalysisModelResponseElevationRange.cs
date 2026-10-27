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
using Jube.Service.EntityAnalysisModelResponseElevationRange;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddEntityAnalysisModelResponseElevationRange(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "EntityAnalysisModelResponseElevationRangeList", OperationKind.Read, true, false,
                    "Lists Response Elevation Ranges for the caller's tenant, keyset-paged and capped.",
                    EntityAnalysisModelResponseElevationRangeService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelResponseElevationRangeFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over Response Elevation Ranges may use.",
                    EntityAnalysisModelResponseElevationRangeService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelResponseElevationRangeFilter", OperationKind.Read, true, false,
                    "Returns the Response Elevation Ranges matching query builder JSON, keyset-paged and capped.",
                    EntityAnalysisModelResponseElevationRangeService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelResponseElevationRangeCount", OperationKind.Read, true, false,
                    "Counts the Response Elevation Ranges matching query builder JSON, optionally grouped by a field.",
                    EntityAnalysisModelResponseElevationRangeService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelResponseElevationRangeGet", OperationKind.Read, true, false,
                    "Returns one Response Elevation Range by id, scoped to the caller's tenant.",
                    EntityAnalysisModelResponseElevationRangeService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelResponseElevationRangeGetByGuid", OperationKind.Read, true, false,
                    "Returns one Response Elevation Range by Guid, scoped to the caller's tenant.",
                    EntityAnalysisModelResponseElevationRangeService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelResponseElevationRangeGetByEntityAnalysisModelId", OperationKind.Read, true, false,
                    "Lists the Active Response Elevation Ranges belonging to a given Model, ordered by Name, scoped to the caller's tenant.",
                    EntityAnalysisModelResponseElevationRangeService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelResponseElevationRangeCreate", OperationKind.Write, false, false,
                    "Registers a Response Elevation Range under a Model in the caller's tenant; calling twice creates two.",
                    EntityAnalysisModelResponseElevationRangeService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelResponseElevationRangeValidate", OperationKind.Read, true, false,
                    "Validates a Response Elevation Range without saving it and returns every failure.",
                    EntityAnalysisModelResponseElevationRangeService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelResponseElevationRangeUpdate", OperationKind.Write, true, false,
                    "Updates a Response Elevation Range in the caller's tenant by id.", EntityAnalysisModelResponseElevationRangeService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelResponseElevationRangeDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a Response Elevation Range in the caller's tenant by id.",
                    EntityAnalysisModelResponseElevationRangeService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelResponseElevationRangeResubmit", OperationKind.Write, false, false,
                    "Resubmits a rejected Response Elevation Range for approval again, unchanged, opening a fresh approval cycle.",
                    EntityAnalysisModelResponseElevationRangeService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelResponseElevationRangeRetract", OperationKind.Write, false, false,
                    "Retracts a rejected Response Elevation Range, restoring it to its last approved version without a new approval cycle.",
                    EntityAnalysisModelResponseElevationRangeService.RequiredPermissions)
            ]);
        }
    }
}