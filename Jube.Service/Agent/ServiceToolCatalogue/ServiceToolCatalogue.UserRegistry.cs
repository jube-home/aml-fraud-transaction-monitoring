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

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddUserRegistry(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "UserRegistryList", OperationKind.Read, true, false,
                    "Lists user accounts for the caller's tenant, keyset-paged and capped."),
                new ServiceToolDescriptor(
                    "UserRegistryFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over User Registrys may use."),
                new ServiceToolDescriptor(
                    "UserRegistryFilter", OperationKind.Read, true, false,
                    "Returns the User Registrys matching query builder JSON, keyset-paged and capped."),
                new ServiceToolDescriptor(
                    "UserRegistryCount", OperationKind.Read, true, false,
                    "Counts the User Registrys matching query builder JSON, optionally grouped by a field."),
                new ServiceToolDescriptor(
                    "UserRegistryGet", OperationKind.Read, true, false,
                    "Returns one user account by id, scoped to the caller's tenant."),
                new ServiceToolDescriptor(
                    "UserRegistryListByRoleRegistryGuid", OperationKind.Read, true, false,
                    "Lists the user accounts assigned to a given Role Registry, scoped to the caller's tenant."),
                new ServiceToolDescriptor(
                    "UserRegistryCreate", OperationKind.Write, false, false,
                    "Registers a user account under a Role Registry in the caller's tenant; calling twice creates two."),
                new ServiceToolDescriptor(
                    "UserRegistryValidate", OperationKind.Read, true, false,
                    "Validates an User Registry without saving it and returns every failure."),
                new ServiceToolDescriptor(
                    "UserRegistryUpdate", OperationKind.Write, true, false,
                    "Updates a user account in the caller's tenant by id. Never touches the password."),
                new ServiceToolDescriptor(
                    "UserRegistryDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a user account in the caller's tenant by id.")
            ]);
        }
    }
}