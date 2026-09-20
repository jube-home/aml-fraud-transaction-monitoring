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
using Jube.Dto.Repository.CaseWorkflowDisplayRole;
using RulePoco = Jube.Data.Poco.CaseWorkflowDisplayRole;

namespace Jube.Service.Repository.CaseWorkflowDisplayRole
{
    internal static class CaseWorkflowDisplayRoleMapper
    {
        [return: NotNullIfNotNull(nameof(rulePoco))]
        public static CaseWorkflowDisplayRoleDto? ToDto(RulePoco? rulePoco) => rulePoco is null
            ? null
            : new CaseWorkflowDisplayRoleDto
            {
                Id = rulePoco.Id,
                CaseWorkflowDisplayGuid = rulePoco.CaseWorkflowDisplayGuid,
                RoleRegistryGuid = rulePoco.RoleRegistryGuid
            };

        public static List<CaseWorkflowDisplayRoleDto> ToDto(IEnumerable<RulePoco> source) =>
            source.Select(rulePoco => ToDto(rulePoco)).ToList();

        public static RulePoco ToPoco(CaseWorkflowDisplayRoleDto dto) => new()
        {
            CaseWorkflowDisplayGuid = dto.CaseWorkflowDisplayGuid,
            RoleRegistryGuid = dto.RoleRegistryGuid
        };
    }
}