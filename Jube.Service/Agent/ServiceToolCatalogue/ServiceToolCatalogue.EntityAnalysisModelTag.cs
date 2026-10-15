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
using Jube.Service.EntityAnalysisModelTag;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddEntityAnalysisModelTag(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "EntityAnalysisModelTagList", OperationKind.Read, true, false,
                    "Lists Tags for the caller's tenant, keyset-paged and capped.",
                    EntityAnalysisModelTagService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelTagFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over Tags may use.",
                    EntityAnalysisModelTagService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelTagFilter", OperationKind.Read, true, false,
                    "Returns the Tags matching query builder JSON, keyset-paged and capped.",
                    EntityAnalysisModelTagService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelTagCount", OperationKind.Read, true, false,
                    "Counts the Tags matching query builder JSON, optionally grouped by a field.",
                    EntityAnalysisModelTagService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelTagGet", OperationKind.Read, true, false,
                    "Returns one Tag by id, scoped to the caller's tenant.",
                    EntityAnalysisModelTagService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelTagGetByGuid", OperationKind.Read, true, false,
                    "Returns one Tag by Guid, scoped to the caller's tenant.",
                    EntityAnalysisModelTagService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelTagGetByEntityAnalysisModelId", OperationKind.Read, true, false,
                    "Lists the Active Tags belonging to a given Model, ordered by Name, scoped to the caller's tenant.",
                    EntityAnalysisModelTagService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelTagCreate", OperationKind.Write, false, false,
                    "Registers a Tag under a Model in the caller's tenant; calling twice creates two.",
                    EntityAnalysisModelTagService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelTagValidate", OperationKind.Read, true, false,
                    "Validates a Tag without saving it and returns every failure.",
                    EntityAnalysisModelTagService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelTagUpdate", OperationKind.Write, true, false,
                    "Updates a Tag in the caller's tenant by id.", EntityAnalysisModelTagService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelTagDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a Tag in the caller's tenant by id.",
                    EntityAnalysisModelTagService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelTagResubmit", OperationKind.Write, false, false,
                    "Resubmits a rejected Tag for approval again, unchanged, opening a fresh approval cycle.",
                    EntityAnalysisModelTagService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelTagRetract", OperationKind.Write, false, false,
                    "Retracts a rejected Tag, restoring it to its last approved version without a new approval cycle.",
                    EntityAnalysisModelTagService.RequiredPermissions)
            ]);
        }
    }
}