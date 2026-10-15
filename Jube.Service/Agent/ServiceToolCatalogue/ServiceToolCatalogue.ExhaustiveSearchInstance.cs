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
using Jube.Service.Repository.ExhaustiveSearchInstance;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddExhaustiveSearchInstance(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "ExhaustiveSearchInstanceList", OperationKind.Read, true, false,
                    "Lists Exhaustive Adaptations for the caller's tenant, keyset-paged and capped.",
                    ExhaustiveSearchInstanceService.ListPermissions),
                new ServiceToolDescriptor(
                    "ExhaustiveSearchInstanceFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over Exhaustive Search Instances may use.",
                    ExhaustiveSearchInstanceService.ListPermissions),
                new ServiceToolDescriptor(
                    "ExhaustiveSearchInstanceFilter", OperationKind.Read, true, false,
                    "Returns the Exhaustive Search Instances matching query builder JSON, keyset-paged and capped.",
                    ExhaustiveSearchInstanceService.ListPermissions),
                new ServiceToolDescriptor(
                    "ExhaustiveSearchInstanceCount", OperationKind.Read, true, false,
                    "Counts the Exhaustive Search Instances matching query builder JSON, optionally grouped by a field.",
                    ExhaustiveSearchInstanceService.ListPermissions),
                new ServiceToolDescriptor(
                    "ExhaustiveSearchInstanceGet", OperationKind.Read, true, false,
                    "Returns one Exhaustive Adaptation by id, scoped to the caller's tenant.",
                    ExhaustiveSearchInstanceService.ReadPermissions),
                new ServiceToolDescriptor(
                    "ExhaustiveSearchInstanceGetByGuid", OperationKind.Read, true, false,
                    "Returns one Exhaustive Adaptation by Guid, scoped to the caller's tenant.",
                    ExhaustiveSearchInstanceService.ReadPermissions),
                new ServiceToolDescriptor(
                    "ExhaustiveSearchInstanceGetByEntityAnalysisModelId", OperationKind.Read, true, false,
                    "Lists Exhaustive Adaptations belonging to a given Model, scoped to the caller's tenant.",
                    ExhaustiveSearchInstanceService.ReadPermissions),
                new ServiceToolDescriptor(
                    "ExhaustiveSearchInstanceCreate", OperationKind.Write, false, false,
                    "Registers an Exhaustive Adaptation under a Model in the caller's tenant; calling twice creates two.",
                    ExhaustiveSearchInstanceService.WritePermissions),
                new ServiceToolDescriptor(
                    "ExhaustiveSearchInstanceValidate", OperationKind.Read, true, false,
                    "Validates an Exhaustive Search Instance without saving it and returns every failure.",
                    ExhaustiveSearchInstanceService.WritePermissions),
                new ServiceToolDescriptor(
                    "ExhaustiveSearchInstanceUpdate", OperationKind.Write, true, false,
                    "Updates an Exhaustive Adaptation in the caller's tenant by id, only while training has not started.",
                    ExhaustiveSearchInstanceService.WritePermissions),
                new ServiceToolDescriptor(
                    "ExhaustiveSearchInstanceStop", OperationKind.Write, true, false,
                    "Requests that an in-progress or queued training run stop, by Guid.",
                    ExhaustiveSearchInstanceService.WritePermissions),
                new ServiceToolDescriptor(
                    "ExhaustiveSearchInstanceDelete", OperationKind.Delete, true, true,
                    "Soft-deletes an Exhaustive Adaptation in the caller's tenant by id.",
                    ExhaustiveSearchInstanceService.WritePermissions),
                new ServiceToolDescriptor(
                    "ExhaustiveSearchInstanceSetActive", OperationKind.Write, true, false,
                    "Sets whether an Exhaustive Adaptation is Active in the caller's tenant.",
                    ExhaustiveSearchInstanceService.WritePermissions),
                new ServiceToolDescriptor(
                    "ExhaustiveSearchInstanceResubmit", OperationKind.Write, false, false,
                    "Resubmits a rejected Exhaustive Adaptation for approval again, unchanged, opening a fresh approval cycle.",
                    ExhaustiveSearchInstanceService.WritePermissions),
                new ServiceToolDescriptor(
                    "ExhaustiveSearchInstanceRetract", OperationKind.Write, false, false,
                    "Retracts a rejected Exhaustive Adaptation, restoring it to its last approved version without a new approval cycle.",
                    ExhaustiveSearchInstanceService.WritePermissions)
            ]);
        }
    }
}