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
using Jube.Dto.Repository.UserRegistry;
using RulePoco = Jube.Data.Poco.UserRegistry;

namespace Jube.Service.Repository.UserRegistry
{
    internal static class UserRegistryMapper
    {
        [return: NotNullIfNotNull(nameof(userRegistry))]
        public static UserRegistryDto? ToDto(RulePoco? userRegistry)
        {
            return userRegistry is null
                ? null
                : new UserRegistryDto
                {
                    Id = userRegistry.Id,
                    RoleRegistryGuid = userRegistry.RoleRegistryGuid,
                    Email = userRegistry.Email,
                    Name = userRegistry.Name,
                    Active = userRegistry.Active == 1,
                    PasswordLocked = userRegistry.PasswordLocked == 1,
                    WirePasswordHash = userRegistry.WirePasswordHash == 1,
                    InheritedId = userRegistry.InheritedId.GetValueOrDefault(),
                    CreatedUser = userRegistry.CreatedUser,
                    CreatedDate = ToOffset(userRegistry.CreatedDate),
                    Version = userRegistry.Version.GetValueOrDefault()
                };
        }

        public static List<UserRegistryDto> ToDto(IEnumerable<RulePoco>? source)
        {
            return (source ?? Enumerable.Empty<RulePoco>()).Select(p => ToDto(p)).ToList();
        }

        public static RulePoco ToPoco(UserRegistryDto dto)
        {
            return new RulePoco
            {
                Id = dto.Id,
                RoleRegistryGuid = dto.RoleRegistryGuid,
                Email = dto.Email,
                Name = dto.Name,
                Active = (byte)(dto.Active ? 1 : 0),
                PasswordLocked = (byte)(dto.PasswordLocked == true ? 1 : 0),
                WirePasswordHash = (byte)(dto.WirePasswordHash ? 1 : 0),
                InheritedId = dto.InheritedId
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