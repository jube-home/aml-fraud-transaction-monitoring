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
using Jube.Dto.Repository.UserInTenant;
using RulePoco = Jube.Data.Poco.UserInTenant;

namespace Jube.Service.Repository.UserInTenant
{
    internal static class UserInTenantMapper
    {
        [return: NotNullIfNotNull(nameof(userInTenant))]
        public static UserInTenantDto? ToDto(RulePoco? userInTenant) => userInTenant is null
            ? null
            : new UserInTenantDto
            {
                User = userInTenant.User,
                TenantRegistryId = userInTenant.TenantRegistryId
            };

        public static List<UserInTenantDto> ToDto(IEnumerable<RulePoco> source) =>
            source.Select(userInTenant => ToDto(userInTenant)).ToList();
    }
}