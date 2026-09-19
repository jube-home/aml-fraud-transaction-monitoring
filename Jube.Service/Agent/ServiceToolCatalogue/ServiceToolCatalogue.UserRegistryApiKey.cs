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
        static partial void AddUserRegistryApiKey(List<ServiceToolDescriptor> tools) => tools.AddRange(
        [
            new ServiceToolDescriptor(
                "UserRegistryApiKeyListByUserRegistryId", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists the API keys issued against a UserRegistry, masked to an 8-character display prefix."),
            new ServiceToolDescriptor(
                "UserRegistryApiKeyDelete", OperationKind.Delete, Idempotent: true, Destructive: true,
                "Revokes an API key; reversible only by direct database action. A repeat call on an " +
                "already-revoked key succeeds as a no-op if it was fully removed, or is reported not-found if " +
                "it belongs to another tenant.")
        ]);
    }
}