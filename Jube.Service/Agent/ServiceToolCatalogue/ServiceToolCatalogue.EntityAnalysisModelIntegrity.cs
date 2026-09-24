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
        static partial void AddEntityAnalysisModelIntegrity(List<ServiceToolDescriptor> tools) => tools.AddRange(
        [
            new ServiceToolDescriptor(
                "EntityAnalysisModelIntegrityModels", OperationKind.Read, Idempotent: true,
                Destructive: false,
                "Lists the tenant's models that can be checked."),
            new ServiceToolDescriptor(
                "EntityAnalysisModelIntegrityCheck", OperationKind.Read, Idempotent: true,
                Destructive: false,
                "Checks a model's dependencies, compilation, configuration and engine state; returns coded findings."),
            new ServiceToolDescriptor(
                "EntityAnalysisModelIntegrityEngineState", OperationKind.Read, Idempotent: true,
                Destructive: false,
                "Returns what the engine instances have loaded and started for a model."),
            new ServiceToolDescriptor(
                "EntityAnalysisModelIntegrityGraph", OperationKind.Read, Idempotent: true,
                Destructive: false,
                "Returns a model's dependency graph as nodes and edges, optionally centred on one entity.")
        ]);
    }
}