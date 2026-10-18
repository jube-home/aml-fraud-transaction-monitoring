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

namespace Jube.Charting.More.Models
{
    public sealed record IntervalBand(
        string Label,
        IReadOnlyList<double> Lower,
        IReadOnlyList<double> Upper,
        bool Hidden = false);

    public sealed record BandSeries(
        string Name,
        IReadOnlyList<double> X,
        IReadOnlyList<double> Y,
        string? Color = null,
        BandSeriesStyle Style = BandSeriesStyle.Line,
        bool Dashed = false,
        IReadOnlyList<IntervalBand>? Bands = null,
        IReadOnlyList<double>? YErrorLow = null,
        IReadOnlyList<double>? YErrorHigh = null,
        IReadOnlyList<double>? XErrorLow = null,
        IReadOnlyList<double>? XErrorHigh = null,
        double? ForecastFrom = null);

    public sealed record EventMarker(double X, string Label, bool ChangePoint = false);

    public sealed record RangeWindow(double From, double To);

    public sealed record BandChartSpec
    {
        public ChartOptions Options { get; init; } = new();
        public string Kind { get; init; } = "Band chart";
        public IReadOnlyList<BandSeries> Series { get; init; } = [];
        public XAxisKind XAxis { get; init; } = XAxisKind.Linear;
        public AxisKind YScale { get; init; }
        public IReadOnlyList<EventMarker> Events { get; init; } = [];
        public IReadOnlyList<PlotBand> Regions { get; init; } = [];
        public IReadOnlyList<ReferenceLine> References { get; init; } = [];
        public bool RangeSelector { get; init; }
        public RangeWindow? Window { get; init; }
        public Func<double, string>? XFormat { get; init; }
        public Func<double, string>? YFormat { get; init; }
        public (double Min, double Max)? XDomain { get; init; }
        public (double Min, double Max)? YDomain { get; init; }
        public bool YFromZero { get; init; }
    }
}