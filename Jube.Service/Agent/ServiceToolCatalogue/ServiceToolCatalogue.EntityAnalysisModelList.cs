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
using Jube.Service.EntityAnalysisModelList;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddEntityAnalysisModelList(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "EntityAnalysisModelListList", OperationKind.Read, true, false,
                    "Lists Lists for the caller's tenant, keyset-paged and capped.",
                    EntityAnalysisModelListService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelListFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over Lists may use.",
                    EntityAnalysisModelListService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelListFilter", OperationKind.Read, true, false,
                    "Returns the Lists matching query builder JSON, keyset-paged and capped.",
                    EntityAnalysisModelListService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelListCount", OperationKind.Read, true, false,
                    "Counts the Lists matching query builder JSON, optionally grouped by a field.",
                    EntityAnalysisModelListService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelListGet", OperationKind.Read, true, false,
                    "Returns one List by id, scoped to the caller's tenant.",
                    EntityAnalysisModelListService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelListGetByGuid", OperationKind.Read, true, false,
                    "Returns one List by Guid, scoped to the caller's tenant.",
                    EntityAnalysisModelListService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelListGetByEntityAnalysisModelId", OperationKind.Read, true, false,
                    "Lists the Lists belonging to a given Model, ordered by Id, scoped to the caller's tenant.",
                    EntityAnalysisModelListService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelListCreate", OperationKind.Write, false, false,
                    "Registers a List under a Model in the caller's tenant; calling twice creates two.",
                    EntityAnalysisModelListService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelListValidate", OperationKind.Read, true, false,
                    "Validates a List without saving it and returns every failure.",
                    EntityAnalysisModelListService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelListUpdate", OperationKind.Write, true, false,
                    "Updates a List in the caller's tenant by id.", EntityAnalysisModelListService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelListDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a List in the caller's tenant by id.",
                    EntityAnalysisModelListService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelListResubmit", OperationKind.Write, false, false,
                    "Resubmits a rejected List for approval again, unchanged, opening a fresh approval cycle.",
                    EntityAnalysisModelListService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelListRetract", OperationKind.Write, false, false,
                    "Retracts a rejected List, restoring it to its last approved version without a new approval cycle.",
                    EntityAnalysisModelListService.RequiredPermissions)
            ]);
        }
    }
}