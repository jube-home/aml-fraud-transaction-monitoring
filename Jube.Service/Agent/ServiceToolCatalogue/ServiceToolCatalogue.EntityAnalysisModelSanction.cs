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
using Jube.Service.EntityAnalysisModelSanction;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddEntityAnalysisModelSanction(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "EntityAnalysisModelSanctionList", OperationKind.Read, true, false,
                    "Lists Sanction checks for the caller's tenant, keyset-paged and capped.",
                    EntityAnalysisModelSanctionService.ListPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelSanctionFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over Sanctions may use.",
                    EntityAnalysisModelSanctionService.ListPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelSanctionFilter", OperationKind.Read, true, false,
                    "Returns the Sanctions matching query builder JSON, keyset-paged and capped.",
                    EntityAnalysisModelSanctionService.ListPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelSanctionCount", OperationKind.Read, true, false,
                    "Counts the Sanctions matching query builder JSON, optionally grouped by a field.",
                    EntityAnalysisModelSanctionService.ListPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelSanctionGet", OperationKind.Read, true, false,
                    "Returns one Sanction check by id, scoped to the caller's tenant.",
                    EntityAnalysisModelSanctionService.ReadPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelSanctionGetByGuid", OperationKind.Read, true, false,
                    "Returns one Sanction check by Guid, scoped to the caller's tenant.",
                    EntityAnalysisModelSanctionService.ReadPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelSanctionGetByEntityAnalysisModelId", OperationKind.Read, true,
                    false,
                    "Lists Sanction checks belonging to a given Model, scoped to the caller's tenant.",
                    EntityAnalysisModelSanctionService.ReadPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelSanctionCreate", OperationKind.Write, false, false,
                    "Registers a Sanction check under a Model in the caller's tenant; calling twice creates two.",
                    EntityAnalysisModelSanctionService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelSanctionValidate", OperationKind.Read, true, false,
                    "Validates a Sanction without saving it and returns every failure.",
                    EntityAnalysisModelSanctionService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelSanctionUpdate", OperationKind.Write, true, false,
                    "Updates a Sanction check in the caller's tenant by id.",
                    EntityAnalysisModelSanctionService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelSanctionDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a Sanction check in the caller's tenant by id.",
                    EntityAnalysisModelSanctionService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelSanctionResubmit", OperationKind.Write, false, false,
                    "Resubmits a rejected Sanction check for approval again, unchanged, opening a fresh approval cycle.",
                    EntityAnalysisModelSanctionService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelSanctionRetract", OperationKind.Write, false, false,
                    "Retracts a rejected Sanction check, restoring it to its last approved version without a new approval cycle.",
                    EntityAnalysisModelSanctionService.WritePermissions)
            ]);
        }
    }
}