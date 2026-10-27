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
using Jube.Dto.Forms;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.ContainerLogEntry
{
    [GridEndpoint("ContainerLogEntry", Title = "Container log entry", DefaultSort = "occurredDate")]
    [GridFilter(Param = "streamTypeId", Label = "Stream", Choices = ["1=Stdout", "2=Stderr"])]
    [GridSearch(Label = "Container or message contains")]
    public sealed record ContainerLogEntryDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        [property: ListColumn(Hidden = true)]
        int Id,
        [property:
            Description(
                "UTC timestamp the container itself logged this line at, parsed from Docker's own RFC3339Nano prefix on the line.")]
        [property: ListColumn(Order = 10)]
        [property: GridTimeRange]
        DateTime OccurredDate,
        [property:
            Description("Which container this line came from (Docker's own container name, leading '/' stripped).")]
        [property: ListColumn(Order = 20, Title = "Container")]
        string ContainerName,
        [property:
            Description("Which stream this line came from -- see StreamTypeName for the human-readable form.")]
        [property: ListColumn(Hidden = true)]
        int StreamTypeId,
        [property: Description("\"Stdout\" or \"Stderr\", as reported by Docker's own multiplexed log stream.")]
        [property: ListColumn(Order = 30, Title = "Stream")]
        string StreamTypeName,
        [property: Description("The log line content.")]
        [property: ListColumn(Order = 40)]
        string Message,
        [property: Description("UTC timestamp this row was flushed to the database.")]
        [property: ListColumn(Order = 60, Title = "Flushed Date")]
        DateTime CreatedDate,
        [property:
            Description(
                "Hostname of the Jube.Monitoring sidecar instance that captured this row (i.e. which Docker host, not which container).")]
        [property: ListColumn(Order = 50, Title = "Host Instance")]
        string Instance);
}