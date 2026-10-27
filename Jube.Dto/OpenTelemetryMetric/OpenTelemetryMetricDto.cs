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
using Jube.Dto.Options;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.OpenTelemetryMetric
{
    [GridEndpoint("OpenTelemetryMetric", Title = "Open telemetry metric", DefaultSort = "occurredDate")]
    [GridSearch(Label = "Metric name or tags contains")]
    public sealed record OpenTelemetryMetricDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        [property: ListColumn(Hidden = true)]
        int Id,
        [property: Description("UTC timestamp of the one-minute aggregation window this row summarises.")]
        [property: ListColumn(Order = 10)]
        [property: GridTimeRange]
        DateTime OccurredDate,
        [property:
            Description(
                "The OpenTelemetry instrument name, e.g. jube.engine.stage.duration or jube.service.operation.count.")]
        [property: ListColumn(Order = 20)]
        string MetricName,
        [property: Description("The OpenTelemetry instrument kind, e.g. Counter or Histogram.")]
        [property: ListColumn(Order = 30)]
        string InstrumentType,
        [property:
            Description(
                "The tag combination this row aggregates, as a sorted \"key=value,key=value\" string -- e.g. stage=Gateway,model=HighRiskTransaction.")]
        [property: ListColumn(Order = 40)]
        string Tags,
        [property:
            Description("Number of measurements recorded for this instrument/tag combination within the window.")]
        [property: ListColumn(Order = 50, Format = GridFormat.Integer)]
        long Count,
        [property:
            Description("Sum of every measurement recorded for this instrument/tag combination within the window.")]
        [property: ListColumn(Order = 60, Format = GridFormat.Decimal)]
        double Sum,
        [property: Description("Smallest measurement recorded for this instrument/tag combination within the window.")]
        [property: ListColumn(Order = 70, Format = GridFormat.Decimal)]
        double Min,
        [property: Description("Largest measurement recorded for this instrument/tag combination within the window.")]
        [property: ListColumn(Order = 80, Format = GridFormat.Decimal)]
        double Max,
        [property: Description("UTC timestamp this row was flushed to the database.")]
        [property: ListColumn(Order = 100, Title = "Flushed Date")]
        DateTime CreatedDate,
        [property: Description("Hostname of the node that flushed this row.")]
        [property: ListColumn(Order = 90)]
        string Instance);
}