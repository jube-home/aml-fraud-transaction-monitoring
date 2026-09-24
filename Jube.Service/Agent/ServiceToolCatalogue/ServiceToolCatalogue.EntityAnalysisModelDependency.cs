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
        static partial void AddEntityAnalysisModelDependency(List<ServiceToolDescriptor> tools) => tools.AddRange(
        [
            new ServiceToolDescriptor(
                "EntityAnalysisModelDependencyDependents", OperationKind.Read, Idempotent: true,
                Destructive: false,
                "Lists every entity that uses an entity, directly or indirectly, and whether deleting it would be refused."),
            new ServiceToolDescriptor(
                "EntityAnalysisModelDependencyDependencies", OperationKind.Read, Idempotent: true,
                Destructive: false,
                "Lists what an entity uses, resolved to the entities it names."),
            new ServiceToolDescriptor(
                "EntityAnalysisModelDependencyIssues", OperationKind.Read, Idempotent: true,
                Destructive: false,
                "Finds dangling names, uses of inactive entities and unused entities in a model.")
        ]);
    }
}