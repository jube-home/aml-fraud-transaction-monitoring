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

using System.ComponentModel;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace Jube.Dto.ContainerLogEntry
{
    public sealed record ContainerLogEntryDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        int Id,
        [property:
            Description(
                "UTC timestamp the container itself logged this line at, parsed from Docker's own RFC3339Nano prefix on the line.")]
        DateTime OccurredDate,
        [property:
            Description("Which container this line came from (Docker's own container name, leading '/' stripped).")]
        string ContainerName,
        [property:
            Description("Which stream this line came from -- see StreamTypeName for the human-readable form.")]
        int StreamTypeId,
        [property: Description("\"Stdout\" or \"Stderr\", as reported by Docker's own multiplexed log stream.")]
        string StreamTypeName,
        [property: Description("The log line content.")]
        string Message,
        [property: Description("UTC timestamp this row was flushed to the database.")]
        DateTime CreatedDate,
        [property:
            Description(
                "Hostname of the Jube.Monitoring sidecar instance that captured this row (i.e. which Docker host, not which container).")]
        string Instance);
}