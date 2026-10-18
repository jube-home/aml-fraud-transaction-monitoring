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
using Jube.Service.Repository.TenantRegistry;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddTenantRegistry(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "TenantRegistryList", OperationKind.Read, true, false,
                    "Lists registered Tenants, Id-ascending, keyset-paged and capped. Landlord-only.",
                    TenantRegistryService.RequiredPermissions, TenantRegistryService.RequiresLandlord),
                new ServiceToolDescriptor(
                    "TenantRegistryFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over Tenant Registrys may use.",
                    TenantRegistryService.RequiredPermissions, TenantRegistryService.RequiresLandlord),
                new ServiceToolDescriptor(
                    "TenantRegistryFilter", OperationKind.Read, true, false,
                    "Returns the Tenant Registrys matching query builder JSON, keyset-paged and capped.",
                    TenantRegistryService.RequiredPermissions, TenantRegistryService.RequiresLandlord),
                new ServiceToolDescriptor(
                    "TenantRegistryCount", OperationKind.Read, true, false,
                    "Counts the Tenant Registrys matching query builder JSON, optionally grouped by a field.",
                    TenantRegistryService.RequiredPermissions, TenantRegistryService.RequiresLandlord),
                new ServiceToolDescriptor(
                    "TenantRegistryGet", OperationKind.Read, true, false,
                    "Returns a single Tenant by Id. Landlord-only.",
                    TenantRegistryService.RequiredPermissions, TenantRegistryService.RequiresLandlord),
                new ServiceToolDescriptor(
                    "TenantRegistryCreate", OperationKind.Write, false, false,
                    "Creates a Tenant; calling twice creates two. Landlord-only.",
                    TenantRegistryService.RequiredPermissions, TenantRegistryService.RequiresLandlord),
                new ServiceToolDescriptor(
                    "TenantRegistryValidate", OperationKind.Read, true, false,
                    "Validates a Tenant Registry without saving it and returns every failure.",
                    TenantRegistryService.RequiredPermissions, TenantRegistryService.RequiresLandlord),
                new ServiceToolDescriptor(
                    "TenantRegistryUpdate", OperationKind.Write, true, false,
                    "Updates a Tenant; overwrites, writes a version-audit row. Landlord-only.",
                    TenantRegistryService.RequiredPermissions, TenantRegistryService.RequiresLandlord),
                new ServiceToolDescriptor(
                    "TenantRegistryDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a Tenant; reversible only by direct database action. Landlord-only.",
                    TenantRegistryService.RequiredPermissions, TenantRegistryService.RequiresLandlord)
            ]);
        }
    }
}