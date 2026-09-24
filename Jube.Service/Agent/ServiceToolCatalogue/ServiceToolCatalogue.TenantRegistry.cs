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
        static partial void AddTenantRegistry(List<ServiceToolDescriptor> tools) => tools.AddRange(
        [
            new ServiceToolDescriptor(
                "TenantRegistryList", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists registered Tenants, Id-ascending, keyset-paged and capped. Landlord-only."),
            new ServiceToolDescriptor(
                "TenantRegistryFilterFields", OperationKind.Read, true, false,
                "Lists the fields a query builder JSON filter over Tenant Registrys may use."),
            new ServiceToolDescriptor(
                "TenantRegistryFilter", OperationKind.Read, true, false,
                "Returns the Tenant Registrys matching query builder JSON, keyset-paged and capped."),
            new ServiceToolDescriptor(
                "TenantRegistryCount", OperationKind.Read, true, false,
                "Counts the Tenant Registrys matching query builder JSON, optionally grouped by a field."),
            new ServiceToolDescriptor(
                "TenantRegistryGet", OperationKind.Read, Idempotent: true, Destructive: false,
                "Returns a single Tenant by Id. Landlord-only."),
            new ServiceToolDescriptor(
                "TenantRegistryCreate", OperationKind.Write, Idempotent: false, Destructive: false,
                "Creates a Tenant; calling twice creates two. Landlord-only."),
            new ServiceToolDescriptor(
                "TenantRegistryValidate", OperationKind.Read, true, false,
                "Validates a Tenant Registry without saving it and returns every failure."),
            new ServiceToolDescriptor(
                "TenantRegistryUpdate", OperationKind.Write, Idempotent: true, Destructive: false,
                "Updates a Tenant; overwrites, writes a version-audit row. Landlord-only."),
            new ServiceToolDescriptor(
                "TenantRegistryDelete", OperationKind.Delete, Idempotent: true, Destructive: true,
                "Soft-deletes a Tenant; reversible only by direct database action. Landlord-only.")
        ]);
    }
}