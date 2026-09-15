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
        static partial void AddOpenTelemetryLogCounter(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "OpenTelemetryLogCounterList", OperationKind.Read, true, false,
                    "Lists every OpenTelemetryLogCounter rule (Name + Regex), most recent first. Not scoped " +
                    "to any tenant."),
                new ServiceToolDescriptor(
                    "OpenTelemetryLogCounterGet", OperationKind.Read, true, false,
                    "Returns one OpenTelemetryLogCounter rule by id."),
                new ServiceToolDescriptor(
                    "OpenTelemetryLogCounterCreate", OperationKind.Write, false, false,
                    "Registers a rule that increments an OpenTelemetry counter named Name whenever Regex " +
                    "matches an incoming unstructured log line; calling twice creates two rows."),
                new ServiceToolDescriptor(
                    "OpenTelemetryLogCounterUpdate", OperationKind.Write, true, false,
                    "Updates an existing rule in place."),
                new ServiceToolDescriptor(
                    "OpenTelemetryLogCounterDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a rule by id; it immediately stops being checked against incoming log lines.")
            ]);
        }
    }
}