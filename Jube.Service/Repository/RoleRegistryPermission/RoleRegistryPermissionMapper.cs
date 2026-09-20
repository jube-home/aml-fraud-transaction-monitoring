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
using Jube.Dto.Repository.RoleRegistryPermission;
using RulePoco = Jube.Data.Poco.RoleRegistryPermission;

namespace Jube.Service.Repository.RoleRegistryPermission
{
    internal static class RoleRegistryPermissionMapper
    {
        [return: NotNullIfNotNull(nameof(rulePoco))]
        public static RoleRegistryPermissionDto? ToDto(RulePoco? rulePoco)
        {
            return rulePoco is null
                ? null
                : new RoleRegistryPermissionDto
                {
                    Id = rulePoco.Id,
                    RoleRegistryId = rulePoco.RoleRegistryId.GetValueOrDefault(),
                    PermissionSpecificationId = rulePoco.PermissionSpecificationId.GetValueOrDefault(),
                    Active = rulePoco.Active == 1,
                    Locked = rulePoco.Locked == 1,
                    CreatedUser = rulePoco.CreatedUser,
                    CreatedDate = ToOffset(rulePoco.CreatedDate),
                    UpdatedUser = rulePoco.UpdatedUser,
                    UpdatedDate = ToOffset(rulePoco.UpdatedDate),
                    Version = rulePoco.Version.GetValueOrDefault(),
                    DeletedUser = rulePoco.DeletedUser,
                    DeletedDate = ToOffset(rulePoco.DeletedDate)
                };
        }

        public static List<RoleRegistryPermissionDto> ToDto(IEnumerable<RulePoco>? source)
        {
            return (source ?? Enumerable.Empty<RulePoco>()).Select(p => ToDto(p)).ToList();
        }

        public static RulePoco ToPoco(RoleRegistryPermissionDto dto)
        {
            return new RulePoco
            {
                Id = dto.Id,
                RoleRegistryId = dto.RoleRegistryId,
                PermissionSpecificationId = dto.PermissionSpecificationId,
                Active = (byte)(dto.Active ? 1 : 0),
                Locked = (byte)(dto.Locked ? 1 : 0)
            };
        }

        private static DateTimeOffset? ToOffset(DateTime? value)
        {
            return value.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
                : null;
        }
    }
}