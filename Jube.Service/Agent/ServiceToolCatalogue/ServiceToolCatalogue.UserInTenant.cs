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
using Jube.Service.Repository.UserInTenant;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddUserInTenant(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "UserInTenantList", OperationKind.Read, true, false,
                    "Lists the users allocated to the caller's own tenant, keyset-paged and capped.",
                    UserInTenantService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "UserInTenantGetCurrentTenantRegistry", OperationKind.Read, true, false,
                    "Returns the caller's own tenant allocation, or nothing if unallocated.",
                    UserInTenantService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "UserInTenantSwitchTenant", OperationKind.Write, true, false,
                    "Switches the caller to a different tenant; restricted to a Landlord caller.",
                    UserInTenantService.RequiredPermissions, UserInTenantService.RequiresLandlord)
            ]);
        }
    }
}