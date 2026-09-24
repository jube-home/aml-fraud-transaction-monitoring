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
        static partial void AddEntityAnalysisModelTimeWindow(List<ServiceToolDescriptor> tools) => tools.AddRange(
        [
            new ServiceToolDescriptor(
                "EntityAnalysisModelTimeWindowIntervals", OperationKind.Read, Idempotent: true,
                Destructive: false,
                "Lists the kinds of time window and the interval codes each allows."),
            new ServiceToolDescriptor(
                "EntityAnalysisModelTimeWindowCalculate", OperationKind.Read, Idempotent: true,
                Destructive: false,
                "Works out a time window's exact start and end as the engine does."),
            new ServiceToolDescriptor(
                "EntityAnalysisModelTimeWindowAbstractionRule", OperationKind.Read, Idempotent: true,
                Destructive: false,
                "Works out the window an abstraction rule aggregates over for a reference date."),
            new ServiceToolDescriptor(
                "EntityAnalysisModelTimeWindowTtlCounter", OperationKind.Read, Idempotent: true,
                Destructive: false,
                "Works out the window a TTL counter counts over for a reference date.")
        ]);
    }
}