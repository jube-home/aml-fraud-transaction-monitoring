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

namespace Jube.Dto.OpenTelemetryMetric
{
    public sealed record OpenTelemetryMetricDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        int Id,
        [property: Description("UTC timestamp of the one-minute aggregation window this row summarises.")]
        DateTime OccurredDate,
        [property:
            Description(
                "The OpenTelemetry instrument name, e.g. jube.engine.stage.duration or jube.service.operation.count.")]
        string MetricName,
        [property: Description("The OpenTelemetry instrument kind, e.g. Counter or Histogram.")]
        string InstrumentType,
        [property:
            Description(
                "The tag combination this row aggregates, as a sorted \"key=value,key=value\" string -- e.g. stage=Gateway,model=HighRiskTransaction.")]
        string Tags,
        [property:
            Description("Number of measurements recorded for this instrument/tag combination within the window.")]
        long Count,
        [property:
            Description("Sum of every measurement recorded for this instrument/tag combination within the window.")]
        double Sum,
        [property: Description("Smallest measurement recorded for this instrument/tag combination within the window.")]
        double Min,
        [property: Description("Largest measurement recorded for this instrument/tag combination within the window.")]
        double Max,
        [property: Description("UTC timestamp this row was flushed to the database.")]
        DateTime CreatedDate,
        [property: Description("Hostname of the node that flushed this row.")]
        string Instance);
}