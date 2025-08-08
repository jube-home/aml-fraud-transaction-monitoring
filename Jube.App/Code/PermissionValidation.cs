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

using System.Linq;
using Jube.Data.Context;
using Jube.Data.Security;

namespace Jube.App.Code
{
    public class PermissionValidation
    {
        private readonly PermissionValidationDto _permissionValidationDto;

        public PermissionValidation(DbContext dbContext, string userName)
        {
            var permissionValidation = new Data.Security.PermissionValidation();
            _permissionValidationDto = permissionValidation.GetPermissionsAsync(dbContext, userName).Result;
        }

        public PermissionValidation(string connectionString, string userName)
        {
            var permissionValidation = new Data.Security.PermissionValidation();
            _permissionValidationDto = permissionValidation.GetPermissionsAsync(connectionString, userName).Result;
        }

        public bool Landlord => _permissionValidationDto.Landlord;

        public bool Validate(int[] testPermissionSpecifications, bool write = false)
        {
            return testPermissionSpecifications.Any(testPermissionSpecification
                => _permissionValidationDto.Permissions.Contains(testPermissionSpecification));
        }
    }
}