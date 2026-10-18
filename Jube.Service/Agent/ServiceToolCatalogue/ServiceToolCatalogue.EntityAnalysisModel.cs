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
using Jube.Service.EntityAnalysisModel;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddEntityAnalysisModel(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "EntityAnalysisModelList", OperationKind.Read, true, false,
                    "Lists Models for the caller's tenant, keyset-paged and capped.",
                    EntityAnalysisModelService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over Entity Analysis Models may use.",
                    EntityAnalysisModelService.ListPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelFilter", OperationKind.Read, true, false,
                    "Returns the Entity Analysis Models matching query builder JSON, keyset-paged and capped.",
                    EntityAnalysisModelService.ListPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelCount", OperationKind.Read, true, false,
                    "Counts the Entity Analysis Models matching query builder JSON, optionally grouped by a field.",
                    EntityAnalysisModelService.ListPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelGet", OperationKind.Read, true, false,
                    "Returns one Model by id, scoped to the caller's tenant.",
                    EntityAnalysisModelService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelCreate", OperationKind.Write, false, false,
                    "Creates a Model in the caller's tenant; calling twice creates two.",
                    EntityAnalysisModelService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelValidate", OperationKind.Read, true, false,
                    "Validates an Entity Analysis Model without saving it and returns every failure.",
                    EntityAnalysisModelService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelUpdate", OperationKind.Write, true, false,
                    "Updates a Model in the caller's tenant by id.", EntityAnalysisModelService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a Model in the caller's tenant by id.",
                    EntityAnalysisModelService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelResubmit", OperationKind.Write, false, false,
                    "Resubmits a rejected Model for approval again, unchanged, opening a fresh approval cycle.",
                    EntityAnalysisModelService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelRetract", OperationKind.Write, false, false,
                    "Retracts a rejected Model, restoring it to its last approved version without a new approval cycle.",
                    EntityAnalysisModelService.RequiredPermissions)
            ]);
        }
    }
}