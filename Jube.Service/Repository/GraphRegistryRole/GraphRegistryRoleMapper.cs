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
using Jube.Dto.Repository.GraphRegistryRole;
using RolePoco = Jube.Data.Poco.GraphRegistryRole;

namespace Jube.Service.Repository.GraphRegistryRole
{
    internal static class GraphRegistryRoleMapper
    {
        [return: NotNullIfNotNull(nameof(rolePoco))]
        public static GraphRegistryRoleDto? ToDto(RolePoco? rolePoco) => rolePoco is null
            ? null
            : new GraphRegistryRoleDto
            {
                Id = rolePoco.Id,
                GraphRegistryGuid = rolePoco.GraphRegistryGuid,
                RoleRegistryGuid = rolePoco.RoleRegistryGuid
            };

        public static List<GraphRegistryRoleDto> ToDto(IEnumerable<RolePoco> source) =>
            source.Select(rolePoco => ToDto(rolePoco)).ToList();

        public static RolePoco ToPoco(GraphRegistryRoleDto dto) => new()
        {
            GraphRegistryGuid = dto.GraphRegistryGuid,
            RoleRegistryGuid = dto.RoleRegistryGuid
        };
    }
}
