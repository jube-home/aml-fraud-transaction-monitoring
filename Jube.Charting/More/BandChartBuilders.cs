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

using Jube.Charting.Core;
using Jube.Charting.Exceptions;
using Jube.Charting.More.Models;
using Jube.Charting.Scales;
using Jube.Charting.Core.Models;

namespace Jube.Charting.More
{
    public sealed class BandChartBuilder : ChartBuilder<BandChartBuilder>
    {
        private readonly List<EventMarker> events = [];
        private readonly List<ReferenceLine> references = [];
        private readonly List<PlotBand> regions = [];
        private readonly List<BandSeries> series = [];
        private BandSeriesStyle defaultStyle = BandSeriesStyle.Line;
        private bool fromZero;
        private string kind = "Band chart";
        private bool selector;
        private RangeWindow? window;
        private XAxisKind xAxis = XAxisKind.Linear;
        private (double Min, double Max)? xDomain;
        private Func<double, string>? xFormat;
        private (double Min, double Max)? yDomain;
        private Func<double, string>? yFormat;
        private AxisKind yScale;

        public BandChartBuilder DefaultStyle(BandSeriesStyle style)
        {
            defaultStyle = style;

            return this;
        }

        public BandChartBuilder Kind(string kindName)
        {
            kind = kindName;

            return this;
        }

        public BandChartBuilder TimeAxis()
        {
            xAxis = XAxisKind.Time;

            return this;
        }

        public BandChartBuilder LogX()
        {
            xAxis = XAxisKind.Log;

            return this;
        }

        public BandChartBuilder LogY()
        {
            yScale = AxisKind.Log;

            return this;
        }

        public BandChartBuilder Series(string name, IEnumerable<double> xs, IEnumerable<double> ys,
            string? color = null,
            BandSeriesStyle? style = null)
        {
            series.Add(new BandSeries(name, [.. xs], [.. ys], color, style ?? defaultStyle));

            return this;
        }

        public BandChartBuilder Series(string name, IEnumerable<DateTime> times, IEnumerable<double> ys,
            string? color = null,
            BandSeriesStyle? style = null)
        {
            xAxis = XAxisKind.Time;

            return Series(name, times.Select(TimeScale.ToSeconds), ys, color, style);
        }

        public BandChartBuilder Series(string name, IEnumerable<double> ys, string? color = null)
        {
            var values = ys.ToArray();

            return Series(name, Enumerable.Range(0, values.Length).Select(i => (double)i), values, color);
        }

        public BandChartBuilder Points(string name, IEnumerable<double> xs, IEnumerable<double> ys,
            string? color = null)
        {
            return Series(name, xs, ys, color, BandSeriesStyle.Points);
        }

        public BandChartBuilder Steps(string name, IEnumerable<double> xs, IEnumerable<double> ys, string? color = null)
        {
            return Series(name, xs, ys, color, BandSeriesStyle.Step);
        }

        public BandChartBuilder Steps(string name, IEnumerable<DateTime> times, IEnumerable<double> ys,
            string? color = null)
        {
            return Series(name, times, ys, color, BandSeriesStyle.Step);
        }

        public BandChartBuilder Forecast(string name, IEnumerable<double> xs, IEnumerable<double> ys,
            double startsAfter,
            string? color = null)
        {
            series.Add(new BandSeries(name, [.. xs], [.. ys], color, BandSeriesStyle.Line, true, null, null,
                null,
                null, null, startsAfter));

            return this;
        }

        public BandChartBuilder Forecast(string name, IEnumerable<DateTime> times, IEnumerable<double> ys,
            DateTime startsAfter,
            string? color = null)
        {
            xAxis = XAxisKind.Time;

            return Forecast(name, times.Select(TimeScale.ToSeconds), ys, TimeScale.ToSeconds(startsAfter), color);
        }

        private BandSeries Last()
        {
            return series.Count > 0
                ? series[^1]
                : throw new ChartSpecificationException("Add a series before adding bands or error bars to it.");
        }

        public BandChartBuilder Interval(string label, IEnumerable<double> lower, IEnumerable<double> upper)
        {
            var last = Last();

            series[^1] = last with
            {
                Bands = [.. last.Bands ?? [], new IntervalBand(label, [.. lower], [.. upper])]
            };

            return this;
        }

        public BandChartBuilder YErrors(IEnumerable<double> low, IEnumerable<double> high)
        {
            series[^1] = Last() with { YErrorLow = [.. low], YErrorHigh = [.. high] };

            return this;
        }

        public BandChartBuilder YErrors(IEnumerable<double> symmetric)
        {
            var values = symmetric.ToArray();

            return YErrors(values, values);
        }

        public BandChartBuilder XErrors(IEnumerable<double> low, IEnumerable<double> high)
        {
            series[^1] = Last() with { XErrorLow = [.. low], XErrorHigh = [.. high] };

            return this;
        }

        public BandChartBuilder XErrors(IEnumerable<double> symmetric)
        {
            var values = symmetric.ToArray();

            return XErrors(values, values);
        }

        public BandChartBuilder WithPoints()
        {
            var last = Last();

            series[^1] = last with
            {
                Style = last.Style == BandSeriesStyle.Line ? BandSeriesStyle.LineAndPoints : last.Style
            };

            return this;
        }

        public BandChartBuilder Event(double x, string label)
        {
            events.Add(new EventMarker(x, label));

            return this;
        }

        public BandChartBuilder Event(DateTime when, string label)
        {
            xAxis = XAxisKind.Time;

            return Event(TimeScale.ToSeconds(when), label);
        }

        public BandChartBuilder ChangePoint(double x, string label)
        {
            events.Add(new EventMarker(x, label, true));

            return this;
        }

        public BandChartBuilder ChangePoint(DateTime when, string label)
        {
            xAxis = XAxisKind.Time;

            return ChangePoint(TimeScale.ToSeconds(when), label);
        }

        public BandChartBuilder Region(double from, double to, string? label = null)
        {
            regions.Add(new PlotBand(false, from, to, label));

            return this;
        }

        public BandChartBuilder Region(DateTime from, DateTime to, string? label = null)
        {
            xAxis = XAxisKind.Time;

            return Region(TimeScale.ToSeconds(from), TimeScale.ToSeconds(to), label);
        }

        public BandChartBuilder HorizontalBand(double from, double to, string? label = null)
        {
            regions.Add(new PlotBand(true, from, to, label));

            return this;
        }

        public BandChartBuilder HorizontalLine(double y, string? label = null)
        {
            references.Add(new ReferenceLine(double.NegativeInfinity, y, double.PositiveInfinity, y, label));

            return this;
        }

        public BandChartBuilder RangeSelector(double? from = null, double? to = null)
        {
            selector = true;
            window = from.HasValue && to.HasValue ? new RangeWindow(from.Value, to.Value) : null;

            return this;
        }

        public BandChartBuilder RangeSelector(DateTime from, DateTime to)
        {
            xAxis = XAxisKind.Time;

            return RangeSelector(TimeScale.ToSeconds(from), TimeScale.ToSeconds(to));
        }

        public BandChartBuilder XDomain(double min, double max)
        {
            xDomain = (min, max);

            return this;
        }

        public BandChartBuilder YDomain(double min, double max)
        {
            yDomain = (min, max);

            return this;
        }

        public BandChartBuilder XFormat(Func<double, string> format)
        {
            xFormat = format;

            return this;
        }

        public BandChartBuilder YFormat(Func<double, string> format)
        {
            yFormat = format;

            return this;
        }

        public BandChartBuilder FromZero(bool enabled = true)
        {
            fromZero = enabled;

            return this;
        }

        public BandChart Build()
        {
            return new BandChart(new BandChartSpec
            {
                Options = Options,
                Kind = kind,
                Series = [.. series],
                XAxis = xAxis,
                YScale = yScale,
                Events = [.. events],
                Regions = [.. regions],
                References = [.. references],
                RangeSelector = selector,
                Window = window,
                XFormat = xFormat,
                YFormat = yFormat,
                XDomain = xDomain,
                YDomain = yDomain,
                YFromZero = fromZero
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}