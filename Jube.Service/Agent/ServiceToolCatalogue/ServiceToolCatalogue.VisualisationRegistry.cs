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
        static partial void AddVisualisationRegistry(List<ServiceToolDescriptor> tools) => tools.AddRange(
        [
            new ServiceToolDescriptor(
                "VisualisationRegistryList", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists Visualisation Registries visible to the caller's tenant, keyset-paged and capped."),
            new ServiceToolDescriptor(
                "VisualisationRegistryGet", OperationKind.Read, Idempotent: true, Destructive: false,
                "Returns a single Visualisation Registry by Id."),
            new ServiceToolDescriptor(
                "VisualisationRegistryGetByGuidActiveOnly", OperationKind.Read, Idempotent: true, Destructive: false,
                "Returns a single active Visualisation Registry by Guid, for embedded recall, provided the " +
                "caller holds a role grant on it."),
            new ServiceToolDescriptor(
                "VisualisationRegistryListByShowInDirectory", OperationKind.Read, Idempotent: true,
                Destructive: false,
                "Lists active, Show In Directory Visualisation Registries that the caller holds a role grant on."),
            new ServiceToolDescriptor(
                "VisualisationRegistryCreate", OperationKind.Write, Idempotent: false, Destructive: false,
                "Creates a Visualisation Registry; calling twice creates two."),
            new ServiceToolDescriptor(
                "VisualisationRegistryUpdate", OperationKind.Write, Idempotent: true, Destructive: false,
                "Updates a Visualisation Registry; overwrites, writes a version-audit row."),
            new ServiceToolDescriptor(
                "VisualisationRegistryDelete", OperationKind.Delete, Idempotent: true, Destructive: true,
                "Soft-deletes a Visualisation Registry; reversible only by direct database action.")
        ]);
    }
}