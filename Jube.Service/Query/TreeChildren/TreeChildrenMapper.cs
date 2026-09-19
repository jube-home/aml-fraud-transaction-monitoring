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

using Jube.Dto.Query.TreeChildren;

namespace Jube.Service.Query.TreeChildren
{
    internal static class TreeChildrenMapper
    {
        private static string Colour(bool active) => active ? "green" : "red";

        internal static EntityAnalysisModelTreeChildDto ToModelChild(int key, string? name, int? active,
            int? entityAnalysisModelId) => new()
        {
            Color = Colour(active == 1),
            Key = key,
            Name = name,
            EntityAnalysisModelId = entityAnalysisModelId
        };

        internal static EntityAnalysisModelTreeChildDto ToModelGuidChild(int key, string? name, int? active,
            Guid? entityAnalysisModelGuid) => new()
        {
            Color = Colour(active == 1),
            Key = key,
            Name = name,
            EntityAnalysisModelGuid = entityAnalysisModelGuid
        };

        internal static VisualisationRegistryTreeChildDto ToVisualisationRegistryChild(int key, string? name,
            int? active, int? visualisationRegistryId) => new()
        {
            Color = Colour(active == 1),
            Key = key,
            Name = name,
            VisualisationRegistryId = visualisationRegistryId ?? 0
        };

        internal static RoleRegistryTreeChildDto ToRoleRegistryChild(int key, string? name, int? active,
            Guid roleRegistryGuid) => ToRoleRegistryChild(key, name, active == 1, roleRegistryGuid);

        internal static RoleRegistryTreeChildDto ToRoleRegistryChild(int key, string? name, bool active,
            Guid roleRegistryGuid) => new()
        {
            Color = Colour(active),
            Key = key,
            Name = name,
            RoleRegistryGuid = roleRegistryGuid
        };

        internal static CasesWorkflowTreeChildDto ToCasesWorkflowChild(int key, string? name, int? active,
            int? caseWorkflowId) => new()
        {
            Color = Colour(active == 1),
            Key = key,
            Name = name,
            CasesWorkflowId = caseWorkflowId ?? 0
        };
    }
}