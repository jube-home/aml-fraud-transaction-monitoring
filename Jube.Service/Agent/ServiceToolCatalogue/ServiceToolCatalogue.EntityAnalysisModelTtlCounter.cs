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
using Jube.Service.EntityAnalysisModelTtlCounter;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddEntityAnalysisModelTtlCounter(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "EntityAnalysisModelTtlCounterList", OperationKind.Read, true, false,
                    "Lists TTL Counters for the caller's tenant, keyset-paged and capped.",
                    EntityAnalysisModelTtlCounterService.ListPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelTtlCounterFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over TTL Counters may use.",
                    EntityAnalysisModelTtlCounterService.ListPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelTtlCounterFilter", OperationKind.Read, true, false,
                    "Returns the TTL Counters matching query builder JSON, keyset-paged and capped.",
                    EntityAnalysisModelTtlCounterService.ListPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelTtlCounterCount", OperationKind.Read, true, false,
                    "Counts the TTL Counters matching query builder JSON, optionally grouped by a field.",
                    EntityAnalysisModelTtlCounterService.ListPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelTtlCounterGet", OperationKind.Read, true, false,
                    "Returns one TTL Counter by id, scoped to the caller's tenant.",
                    EntityAnalysisModelTtlCounterService.ReadPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelTtlCounterGetByGuid", OperationKind.Read, true, false,
                    "Returns one TTL Counter by Guid, scoped to the caller's tenant.",
                    EntityAnalysisModelTtlCounterService.ReadPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelTtlCounterGetByEntityAnalysisModelId", OperationKind.Read, true, false,
                    "Lists TTL Counters belonging to a given Model, scoped to the caller's tenant.",
                    EntityAnalysisModelTtlCounterService.ReadByModelPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelTtlCounterGetByEntityAnalysisModelGuid", OperationKind.Read, true, false,
                    "Lists TTL Counters belonging to the Model with the given Guid, scoped to the caller's tenant.",
                    EntityAnalysisModelTtlCounterService.ReadByModelPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelTtlCounterCreate", OperationKind.Write, false, false,
                    "Registers a TTL Counter under a Model in the caller's tenant; calling twice creates two.",
                    EntityAnalysisModelTtlCounterService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelTtlCounterValidate", OperationKind.Read, true, false,
                    "Validates a TTL Counter without saving it and returns every failure.",
                    EntityAnalysisModelTtlCounterService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelTtlCounterUpdate", OperationKind.Write, true, false,
                    "Updates a TTL Counter in the caller's tenant by id.",
                    EntityAnalysisModelTtlCounterService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelTtlCounterDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a TTL Counter in the caller's tenant by id.",
                    EntityAnalysisModelTtlCounterService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelTtlCounterResubmit", OperationKind.Write, false, false,
                    "Resubmits a rejected TTL Counter for approval again, unchanged, opening a fresh approval cycle.",
                    EntityAnalysisModelTtlCounterService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelTtlCounterRetract", OperationKind.Write, false, false,
                    "Retracts a rejected TTL Counter, restoring it to its last approved version without a new approval cycle.",
                    EntityAnalysisModelTtlCounterService.WritePermissions)
            ]);
        }
    }
}