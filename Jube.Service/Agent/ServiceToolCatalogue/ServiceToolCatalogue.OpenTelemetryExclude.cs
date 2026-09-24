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
        static partial void AddOpenTelemetryExclude(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "OpenTelemetryExcludeList", OperationKind.Read, true, false,
                    "Lists every OpenTelemetryExclude row, most recent first. Not scoped to any tenant."),
                new ServiceToolDescriptor(
                    "OpenTelemetryExcludeGet", OperationKind.Read, true, false,
                    "Returns one OpenTelemetryExclude row by id."),
                new ServiceToolDescriptor(
                    "OpenTelemetryExcludeCreate", OperationKind.Write, false, false,
                    "Registers an OpenTelemetry instrument/counter name to stop exporting; calling twice " +
                    "creates two rows."),
                new ServiceToolDescriptor(
                    "OpenTelemetryExcludeValidate", OperationKind.Read, true, false,
                    "Validates an Open Telemetry Exclude without saving it and returns every failure."),
                new ServiceToolDescriptor(
                    "OpenTelemetryExcludeUpdate", OperationKind.Write, true, false,
                    "Updates an existing exclusion row in place."),
                new ServiceToolDescriptor(
                    "OpenTelemetryExcludeDelete", OperationKind.Delete, true, true,
                    "Soft-deletes an exclusion row by id; the named instrument becomes eligible for export " +
                    "again.")
            ]);
        }
    }
}