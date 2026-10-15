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

using Jube.Charting.Core.Models;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Charting.Charts.Models
{
    internal sealed class XyModel
    {
        public required ChartOptions Options { get; init; }
        public required string Kind { get; init; }
        public required XyMode Mode { get; init; }
        public IReadOnlyList<PointSeries> Series { get; init; } = [];
        public XAxisKind XAxis { get; init; }
        public IReadOnlyList<string>? Categories { get; init; }
        public AxisKind YScale { get; init; }
        public bool Stacked { get; init; }
        public bool Crosshair { get; init; }
        public MarkerMode Markers { get; init; }
        public IReadOnlyList<ReferenceLine> References { get; init; } = [];
        public IReadOnlyList<PointHighlight> Highlights { get; init; } = [];
        public (double Min, double Max)? XDomain { get; init; }
        public (double Min, double Max)? YDomain { get; init; }
        public bool YFromZero { get; init; }
        public Func<double, string>? XFormat { get; init; }
        public Func<double, string>? YFormat { get; init; }
        public IReadOnlyList<StripBar>? Strip { get; init; }
        public string? StripTitle { get; init; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public bool ConnectPoints { get; init; }
        public bool Step { get; init; }
        public bool Smooth { get; init; }
        public bool DataLabels { get; init; }
        public bool PercentStack { get; init; }
        public IReadOnlyList<PlotBand> Bands { get; init; } = [];
        public IReadOnlyList<Annotation> Annotations { get; init; } = [];
        public Func<double, string>? Y2Format { get; init; }
        public IReadOnlyList<string>? LegendOverride { get; init; }
        public IReadOnlyList<string?>? TipYNames { get; init; }
        public string SummaryDetail { get; init; } = string.Empty;
    }
}