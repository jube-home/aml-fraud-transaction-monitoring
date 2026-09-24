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
        static partial void AddEntityAnalysisModelHistorySimulation(List<ServiceToolDescriptor> tools) =>
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "EntityAnalysisModelHistorySimulationAbstractionAggregation", OperationKind.Read, Idempotent: true,
                    Destructive: false,
                    "Works out an abstraction rule's value for a transaction from cached history, as the engine does."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelHistorySimulationTtlCounterValue", OperationKind.Read, Idempotent: true,
                    Destructive: false,
                    "Reads a TTL counter's value for a transaction from the cache, as the engine does; increments nothing.")
            ]);
    }
}