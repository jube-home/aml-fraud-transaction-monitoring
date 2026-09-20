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

using System.Diagnostics.CodeAnalysis;
using Jube.Dto.Repository.CaseWorkflowXPathRole;
using RulePoco = Jube.Data.Poco.CaseWorkflowXPathRole;

namespace Jube.Service.Repository.CaseWorkflowXPathRole
{
    internal static class CaseWorkflowXPathRoleMapper
    {
        [return: NotNullIfNotNull(nameof(rulePoco))]
        public static CaseWorkflowXPathRoleDto? ToDto(RulePoco? rulePoco) => rulePoco is null
            ? null
            : new CaseWorkflowXPathRoleDto
            {
                Id = rulePoco.Id,
                CaseWorkflowXPathGuid = rulePoco.CaseWorkflowXPathGuid,
                RoleRegistryGuid = rulePoco.RoleRegistryGuid
            };

        public static List<CaseWorkflowXPathRoleDto> ToDto(IEnumerable<RulePoco> source) =>
            source.Select(rulePoco => ToDto(rulePoco)).ToList();

        public static RulePoco ToPoco(CaseWorkflowXPathRoleDto dto) => new()
        {
            CaseWorkflowXPathGuid = dto.CaseWorkflowXPathGuid,
            RoleRegistryGuid = dto.RoleRegistryGuid
        };
    }
}