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

using Jube.Charting;
using Jube.Charting.Charts;
using Jube.Charting.Core;
using Jube.Data.Reporting;
using Jube.Dto.Options;
using Jube.Service.Charts.Datasource.Models;
using Jube.Service.Charts.Datasource.Template;
using static Jube.Dto.Options.DatasourceChartRole;
using Spec = Jube.Service.Charts.Datasource.Models.DatasourceChartRoleSpec;

namespace Jube.Service.Charts.Datasource
{
    public static class DatasourceChartKinds
    {
        private const DatasourceRoleAccepts Number = DatasourceRoleAccepts.Numeric;
        private const double MapAspect = 2.05;

        private static readonly Spec CategoryRole = new(Category);
        private static readonly Spec ValuesRole = new(Value, true, false, Number);
        private static readonly Spec SeriesRole = new(Series, false, true);
        private static readonly Spec ValueRole = new(Value, false, false, Number);

        private static readonly IReadOnlyList<DatasourceChartKind> kinds = Build();

        private static readonly IReadOnlyDictionary<DatasourceDisplayKind, DatasourceChartKind> byKind =
            kinds.ToDictionary(k => k.Kind);

        public static IReadOnlyList<DatasourceChartKind> All => kinds;

        public static DatasourceChartKind Get(DatasourceDisplayKind kind)
        {
            return byKind.TryGetValue(kind, out var found)
                ? found
                : throw new DatasourceChartException(DatasourceMessages.KindUnknown, (int)kind);
        }

        public static bool TryGet(int kindId, out DatasourceChartKind kind)
        {
            return byKind.TryGetValue((DatasourceDisplayKind)kindId, out kind!);
        }

        private static T Present<T>(T builder, DatasourceChartPresentation p) where T : ChartBuilder<T>
        {
            var configured = builder.Size(p.Width, p.Height).Theme(p.Theme).StyleMode(p.StyleMode)
                .Interactive(p.Interactive);

            if (!string.IsNullOrWhiteSpace(p.Title))
            {
                configured = configured.Title(p.Title);
            }

            return string.IsNullOrWhiteSpace(p.Id) ? configured : configured.Id(p.Id);
        }

        private static (string[] Categories, List<(string Name, double[] Values)> Series) Categorical(
            DatasourceChartInput input)
        {
            var mapping = input.Mapping;
            var frame = input.Frame;
            var category = mapping.One(Category);

            if (mapping.Has(Series))
            {
                var pivot = DatasourceSeriesPivot.Pivot(frame, category, mapping.One(Series), mapping.Many(Value)[0]);

                return (pivot.Categories, pivot.Series);
            }

            return (frame.Texts(category), mapping.Many(Value).Select(c => (c, frame.Doubles(c))).ToList());
        }

        private static DatasourceChartException? SeriesNeedsOneValue(DatasourceChartMapping mapping)
        {
            return mapping.Has(Series) && mapping.Many(Value).Count != 1
                ? new DatasourceChartException(DatasourceMessages.SeriesNeedsOneValue)
                : null;
        }

        private static string Bars(DatasourceChartInput input, bool horizontal, bool stacked, bool percent)
        {
            var (categories, series) = Categorical(input);
            var builder = (horizontal ? Chart.Bar() : Chart.Column()).Categories(categories);

            foreach (var (name, values) in series)
            {
                builder = builder.Series(name, values);
            }

            if (stacked)
            {
                builder = builder.Stacked();
            }

            if (percent)
            {
                builder = builder.PercentStacked();
            }

            return Present(builder, input.Presentation).Render();
        }

        private static T FillXy<T>(T builder, DatasourceChartInput input) where T : XyBuilderBase<T>
        {
            var mapping = input.Mapping;
            var frame = input.Frame;
            var category = mapping.One(Category);
            var type = frame.Columns[frame.IndexOf(category)].Type;

            if (type == SeriesDataType.Text || type == SeriesDataType.Flag ||
                (type != SeriesDataType.DateTime && type != SeriesDataType.Integer && type != SeriesDataType.Decimal))
            {
                var (categories, series) = Categorical(input);
                builder = builder.Categories(categories);

                foreach (var (name, values) in series)
                {
                    builder = builder.Series(name, values);
                }

                return builder;
            }

            var groups = mapping.Has(Series)
                ? frame.Texts(mapping.One(Series))
                    .Select((name, row) => (name, row)).GroupBy(g => g.name, StringComparer.Ordinal).ToList()
                : null;

            var valueColumn = mapping.Many(Value);

            if (groups != null)
            {
                var values = frame.Doubles(valueColumn[0]);

                foreach (var group in groups)
                {
                    var rows = group.Select(g => g.row).ToArray();
                    builder = type == SeriesDataType.DateTime
                        ? builder.TimeSeries(group.Key, rows.Select(r => frame.Times(category)[r]).ToArray(),
                            rows.Select(r => values[r]).ToArray())
                        : builder.Series(group.Key, rows.Select(r => frame.Doubles(category)[r]).ToArray(),
                            rows.Select(r => values[r]).ToArray());
                }

                return builder;
            }

            foreach (var column in valueColumn)
            {
                builder = type == SeriesDataType.DateTime
                    ? builder.TimeSeries(column, frame.Times(category), frame.Doubles(column))
                    : builder.Series(column, frame.Doubles(category), frame.Doubles(column));
            }

            return builder;
        }

        private static string Pie(DatasourceChartInput input, bool donut)
        {
            var frame = input.Frame;
            var builder = (donut ? Chart.Donut() : Chart.Pie())
                .Slices(frame.Texts(input.Mapping.One(Category)), frame.Doubles(input.Mapping.One(Value)));

            return Present(builder, input.Presentation).Render();
        }

        private static IReadOnlyList<DatasourceChartKind> Build()
        {
            Spec[] categorical = [CategoryRole, ValuesRole, SeriesRole];

            DatasourceChartKind Bar(DatasourceDisplayKind kind, string name, string description, bool horizontal,
                bool stacked, bool percent)
            {
                return new DatasourceChartKind(kind, name, description, categorical,
                    input => Bars(input, horizontal, stacked, percent), SeriesNeedsOneValue);
            }

            return
            [
                Bar(DatasourceDisplayKind.Column, "Column chart", "Compares values across categories with vertical bars, one series per value column.", false, false, false),
                Bar(DatasourceDisplayKind.Bar, "Bar chart", "Compares values across categories with horizontal bars, which suits long category names.", true, false, false),
                Bar(DatasourceDisplayKind.StackedColumn, "Stacked column chart", "Stacks each series on top of the other to show the total and its parts.", false, true, false),
                Bar(DatasourceDisplayKind.StackedBar, "Stacked bar chart", "Stacks each series horizontally to show the total and its parts.", true, true, false),
                Bar(DatasourceDisplayKind.PercentStackedColumn, "Percent stacked column chart", "Shows each series as a share of the category total.", false, true, true),
                Bar(DatasourceDisplayKind.PercentStackedBar, "Percent stacked bar chart", "Shows each series as a share of the category total, horizontally.", true, true, true),
                new DatasourceChartKind(DatasourceDisplayKind.Line, "Line chart", "Shows how values change along an ordered category, number or time.", categorical,
                    input => Present(FillXy(Chart.Line(), input), input.Presentation).Render(), SeriesNeedsOneValue),
                new DatasourceChartKind(DatasourceDisplayKind.Area, "Area chart", "A line chart with the area below each line filled.", categorical,
                    input => Present(FillXy(Chart.Area(), input), input.Presentation).Render(), SeriesNeedsOneValue),
                new DatasourceChartKind(DatasourceDisplayKind.StackedArea, "Stacked area chart", "Stacks filled areas to show a total and its parts over an ordered axis.", categorical,
                    input => Present(FillXy(Chart.Area(), input).Stacked(), input.Presentation).Render(), SeriesNeedsOneValue),
                new DatasourceChartKind(DatasourceDisplayKind.PercentStackedArea, "Percent stacked area chart", "Shows each series as a share of the total over an ordered axis.", categorical,
                    input => Present(FillXy(Chart.Area(), input).PercentStacked(), input.Presentation).Render(), SeriesNeedsOneValue),
                new DatasourceChartKind(DatasourceDisplayKind.Pie, "Pie chart", "Shows parts of a whole. Best for a handful of categories.", [CategoryRole, ValueRole],
                    input => Pie(input, false)),
                new DatasourceChartKind(DatasourceDisplayKind.Donut, "Donut chart", "A pie chart with a hollow centre.", [CategoryRole, ValueRole],
                    input => Pie(input, true)),
                new DatasourceChartKind(DatasourceDisplayKind.Scatter, "Scatter or bubble chart",
                    "Plots one numeric column against another; a size column turns the points into bubbles.",
                    [new Spec(X, false, false, Number),
                        new Spec(Y, true, false, Number),
                        new Spec(Size, false, true, Number),
                        SeriesRole],
                    Scatter, mapping => mapping.Has(Series) && mapping.Many(Y).Count != 1
                        ? new DatasourceChartException(DatasourceMessages.SeriesNeedsOneVertical)
                        : null),
                new DatasourceChartKind(DatasourceDisplayKind.Histogram, "Histogram", "Bins the raw values of each numeric column to show how they are distributed.",
                    [ValuesRole],
                    input => Present(Histogram(input), input.Presentation).Render()),
                new DatasourceChartKind(DatasourceDisplayKind.Heatmap, "Heatmap", "Colours the cell where a row category meets a column category by a numeric value.",
                    [new Spec(Row), new Spec(Column), ValueRole],
                    Heatmap),
                new DatasourceChartKind(DatasourceDisplayKind.Combo, "Combination chart", "Draws columns and lines on the same categories, with an optional secondary axis.",
                    [CategoryRole,
                        new Spec(Value, true, false, Number, 1),
                        new Spec(Line, true, true, Number, 0, true),
                        new Spec(SecondaryLine, true, true, Number)],
                    Combo),
                new DatasourceChartKind(DatasourceDisplayKind.BoxPlot, "Box plot", "Shows the spread of each category from its minimum to its maximum, around the median.",
                    [CategoryRole, Numeric(Minimum), Numeric(FirstQuartile),
                        Numeric(Median), Numeric(ThirdQuartile), Numeric(Maximum)],
                    BoxPlot),
                new DatasourceChartKind(DatasourceDisplayKind.Candlestick, "Candlestick chart", "Shows the open, high, low and close for each period.",
                    [new Spec(Category),
                        Numeric(Open), Numeric(High), Numeric(Low), Numeric(Close)],
                    Candlestick),
                new DatasourceChartKind(DatasourceDisplayKind.Waterfall, "Waterfall chart", "Shows how a running total rises and falls with each step.", [CategoryRole, ValueRole],
                    input => Present(Waterfall(input), input.Presentation).Render()),
                new DatasourceChartKind(DatasourceDisplayKind.Funnel, "Funnel chart", "Shows how a value narrows through ordered stages.", [CategoryRole, ValueRole],
                    input => Present(Funnel(input), input.Presentation).Render()),
                new DatasourceChartKind(DatasourceDisplayKind.Radar, "Radar chart", "Compares series across several axes on a polar grid.", categorical,
                    Radar, SeriesNeedsOneValue),
                new DatasourceChartKind(DatasourceDisplayKind.Polar, "Polar chart", "Plots a radius against an angle in degrees.",
                    [new Spec(X, false, false, Number), new Spec(Y, false, false, Number), SeriesRole],
                    Polar),
                new DatasourceChartKind(DatasourceDisplayKind.Gauge, "Gauge", "Shows a single value against a range and an optional target, from the first row.",
                    [ValueRole, new Spec(Target, false, true, Number, 0, true),
                        new Spec(Minimum, false, true, Number), new Spec(Maximum, false, true, Number)],
                    Gauge),
                new DatasourceChartKind(DatasourceDisplayKind.Bullet, "Bullet chart", "Shows each row's value against its target as a compact bar.",
                    [new Spec(Category, false, true, DatasourceRoleAccepts.Any, 0, true), ValueRole,
                        new Spec(Target, false, true, Number, 0, true)],
                    Bullet),
                new DatasourceChartKind(DatasourceDisplayKind.Sparkline, "Sparkline", "A small word-sized line of ordered values.",
                    [ValueRole, new Spec(Category, false, true)],
                    Sparkline),
                new DatasourceChartKind(DatasourceDisplayKind.Treemap, "Treemap", "Sizes rectangles by value, optionally grouped under a parent.",
                    [CategoryRole, ValueRole, new Spec(Parent, false, true)],
                    Treemap),
                new DatasourceChartKind(DatasourceDisplayKind.Gantt, "Gantt chart", "Shows tasks as bars between a start and an end time.",
                    [CategoryRole, new Spec(Start, false, false, DatasourceRoleAccepts.Time),
                        new Spec(End, false, false, DatasourceRoleAccepts.Time),
                        new Spec(Series, false, true)],
                    Gantt),
                new DatasourceChartKind(DatasourceDisplayKind.Map, "Map",
                    "Plots points by latitude and longitude, sized by a value and split by series, or colours countries by a value.",
                    [new Spec(Latitude, false, true, Number, 0, false, ["lat", "latitude"], true, [Country]),
                        new Spec(Longitude, false, true, Number, 0, false, ["lon", "lng", "long", "longitude"], true, [Country]),
                        new Spec(Country, false, true, DatasourceRoleAccepts.Any, 0, false, ["country", "iso", "iso2", "iso3", "nation"], true, [Latitude, Longitude]),
                        new Spec(Value, false, true, Number, 0, true),
                        new Spec(Label, false, true),
                        SeriesRole],
                    Map, CheckMap),
                new DatasourceChartKind(DatasourceDisplayKind.Template, "Template",
                    "Lays the rows out in your own markup, using a small Vue-style dialect: interpolation, v-for, v-if and bound classes over a fixed set of safe tags.",
                    [],
                    TemplateMarkup, null, DatasourceMarkupKind.Html)
            ];
        }

        private static string TemplateMarkup(DatasourceChartInput input)
        {
            return DatasourceTemplate.Render(input.Presentation.TemplateText, input.Frame,
                input.Presentation.Truncated);
        }

        private static DatasourceChartException? CheckMap(DatasourceChartMapping mapping)
        {
            var points = mapping.Has(Latitude) && mapping.Has(Longitude);

            if (mapping.Has(Latitude) != mapping.Has(Longitude))
            {
                return new DatasourceChartException(DatasourceMessages.MapNeedsBoth);
            }

            if (!points && !mapping.Has(Country))
            {
                return new DatasourceChartException(DatasourceMessages.MapNeedsPlace);
            }

            if (points && mapping.Has(Country))
            {
                return new DatasourceChartException(DatasourceMessages.MapEither);
            }

            return !points && !mapping.Has(Value) ? new DatasourceChartException(DatasourceMessages.MapNeedsValue) : null;
        }

        private static string Map(DatasourceChartInput input)
        {
            var mapping = input.Mapping;
            var frame = input.Frame;
            var builder = Chart.Map();

            if (mapping.Has(Country))
            {
                var value = mapping.One(Value);

                builder = builder.Countries(frame.Texts(mapping.One(Country)), frame.Doubles(value)).LegendTitle(value);
            }
            else
            {
                builder = builder.Points(frame.Doubles(mapping.One(Latitude)), frame.Doubles(mapping.One(Longitude)),
                    mapping.Has(Value) ? frame.Doubles(mapping.One(Value)) : null,
                    mapping.Has(Label) ? frame.Texts(mapping.One(Label)) : null,
                    mapping.Has(Series) ? frame.Texts(mapping.One(Series)) : null);
            }

            var presentation = input.Presentation with
            {
                Height = Math.Min(input.Presentation.Height, Math.Floor(input.Presentation.Width / MapAspect))
            };

            return Present(builder, presentation).Render();
        }

        private static Spec Numeric(DatasourceChartRole role)
        {
            return new Spec(role, false, false, Number);
        }

        private static string Scatter(DatasourceChartInput input)
        {
            var mapping = input.Mapping;
            var frame = input.Frame;
            var builder = Chart.Scatter();
            var xs = frame.Doubles(mapping.One(X));
            var sizes = mapping.Has(Size) ? frame.Doubles(mapping.One(Size)) : null;

            if (mapping.Has(Series))
            {
                var ys = frame.Doubles(mapping.Many(Y)[0]);

                foreach (var group in frame.Texts(mapping.One(Series)).Select((n, r) => (n, r))
                             .GroupBy(g => g.n, StringComparer.Ordinal))
                {
                    var rows = group.Select(g => g.r).ToArray();
                    var gx = rows.Select(r => xs[r]).ToArray();
                    var gy = rows.Select(r => ys[r]).ToArray();

                    builder = sizes == null
                        ? builder.Series(group.Key, gx, gy)
                        : builder.Bubbles(group.Key, gx, gy, rows.Select(r => sizes[r]).ToArray());
                }
            }
            else
            {
                foreach (var column in mapping.Many(Y))
                {
                    var ys = frame.Doubles(column);

                    builder = sizes == null ? builder.Series(column, xs, ys) : builder.Bubbles(column, xs, ys, sizes);
                }
            }

            return Present(builder, input.Presentation).Render();
        }

        private static HistogramChartBuilder Histogram(DatasourceChartInput input)
        {
            var builder = Chart.Histogram();

            foreach (var column in input.Mapping.Many(Value))
            {
                builder = builder.Values(column, input.Frame.Doubles(column).Where(double.IsFinite).ToArray());
            }

            return builder;
        }

        private static string Heatmap(DatasourceChartInput input)
        {
            var mapping = input.Mapping;
            var frame = input.Frame;
            var rowTexts = frame.Texts(mapping.One(Row));
            var columnTexts = frame.Texts(mapping.One(Column));
            var values = frame.Doubles(mapping.One(Value));
            var rows = rowTexts.Distinct(StringComparer.Ordinal).ToList();
            var columns = columnTexts.Distinct(StringComparer.Ordinal).ToList();
            var rowIndex = rows.Select((r, i) => (r, i)).ToDictionary(x => x.r, x => x.i, StringComparer.Ordinal);
            var columnIndex = columns.Select((c, i) => (c, i)).ToDictionary(x => x.c, x => x.i, StringComparer.Ordinal);
            var matrix = rows.Select(_ => Enumerable.Repeat(double.NaN, columns.Count).ToArray()).ToArray();
            var seen = new HashSet<(int, int)>();

            for (var i = 0; i < values.Length; i++)
            {
                var r = rowIndex[rowTexts[i]];
                var c = columnIndex[columnTexts[i]];

                if (!seen.Add((r, c)))
                {
                    throw new DatasourceChartException(DatasourceMessages.HeatmapDuplicate, rowTexts[i],
                        columnTexts[i]);
                }

                matrix[r][c] = values[i];
            }

            return Present(Chart.Heatmap().Rows(rows).Columns(columns).Values(matrix), input.Presentation).Render();
        }

        private static string Combo(DatasourceChartInput input)
        {
            var mapping = input.Mapping;
            var frame = input.Frame;
            var builder = Chart.Combo().Categories(frame.Texts(mapping.One(Category)));

            foreach (var column in mapping.Many(Value))
            {
                builder = builder.Columns(column, frame.Doubles(column));
            }

            foreach (var column in mapping.Many(Line))
            {
                builder = builder.Line(column, frame.Doubles(column));
            }

            foreach (var column in mapping.Many(SecondaryLine))
            {
                builder = builder.Line(column, frame.Doubles(column), null, true);
            }

            return Present(builder, input.Presentation).Render();
        }

        private static string BoxPlot(DatasourceChartInput input)
        {
            var mapping = input.Mapping;
            var frame = input.Frame;
            var labels = frame.Texts(mapping.One(Category));
            var min = frame.Doubles(mapping.One(Minimum));
            var q1 = frame.Doubles(mapping.One(FirstQuartile));
            var median = frame.Doubles(mapping.One(Median));
            var q3 = frame.Doubles(mapping.One(ThirdQuartile));
            var max = frame.Doubles(mapping.One(Maximum));
            var builder = Chart.BoxPlot();

            for (var i = 0; i < labels.Length; i++)
            {
                builder = builder.Box(labels[i], min[i], q1[i], median[i], q3[i], max[i]);
            }

            return Present(builder, input.Presentation).Render();
        }

        private static string Candlestick(DatasourceChartInput input)
        {
            var mapping = input.Mapping;
            var frame = input.Frame;
            var category = mapping.One(Category);
            var open = frame.Doubles(mapping.One(Open));
            var high = frame.Doubles(mapping.One(High));
            var low = frame.Doubles(mapping.One(Low));
            var close = frame.Doubles(mapping.One(Close));
            var builder = Chart.Candlestick();

            if (frame.Columns[frame.IndexOf(category)].Type == SeriesDataType.DateTime)
            {
                var times = frame.Times(category);

                for (var i = 0; i < times.Length; i++)
                {
                    builder = builder.Candle(times[i], open[i], high[i], low[i], close[i]);
                }
            }
            else
            {
                var labels = frame.Texts(category);

                for (var i = 0; i < labels.Length; i++)
                {
                    builder = builder.Candle(labels[i], open[i], high[i], low[i], close[i]);
                }
            }

            return Present(builder, input.Presentation).Render();
        }

        private static WaterfallChartBuilder Waterfall(DatasourceChartInput input)
        {
            var labels = input.Frame.Texts(input.Mapping.One(Category));
            var values = input.Frame.Doubles(input.Mapping.One(Value));
            var builder = Chart.Waterfall();

            for (var i = 0; i < labels.Length; i++)
            {
                builder = builder.Step(labels[i], values[i]);
            }

            return builder;
        }

        private static FunnelChartBuilder Funnel(DatasourceChartInput input)
        {
            var labels = input.Frame.Texts(input.Mapping.One(Category));
            var values = input.Frame.Doubles(input.Mapping.One(Value));
            var builder = Chart.Funnel();

            for (var i = 0; i < labels.Length; i++)
            {
                builder = builder.Stage(labels[i], values[i]);
            }

            return builder;
        }

        private static string Radar(DatasourceChartInput input)
        {
            var (categories, series) = Categorical(input);
            var builder = Chart.Radar().Axes(categories);

            foreach (var (name, values) in series)
            {
                builder = builder.Series(name, values);
            }

            return Present(builder, input.Presentation).Render();
        }

        private static string Polar(DatasourceChartInput input)
        {
            var mapping = input.Mapping;
            var frame = input.Frame;
            var angles = frame.Doubles(mapping.One(X));
            var radii = frame.Doubles(mapping.One(Y));
            var builder = Chart.Polar();

            if (mapping.Has(Series))
            {
                foreach (var group in frame.Texts(mapping.One(Series)).Select((n, r) => (n, r))
                             .GroupBy(g => g.n, StringComparer.Ordinal))
                {
                    var rows = group.Select(g => g.r).ToArray();
                    builder = builder.Series(group.Key, rows.Select(r => angles[r]).ToArray(),
                        rows.Select(r => radii[r]).ToArray());
                }
            }
            else
            {
                builder = builder.Series(mapping.One(Y), angles, radii);
            }

            return Present(builder, input.Presentation).Render();
        }

        private static string Gauge(DatasourceChartInput input)
        {
            var mapping = input.Mapping;
            var frame = input.Frame;

            if (frame.Rows.Count == 0)
            {
                throw new DatasourceChartException(DatasourceMessages.GaugeNeedsRow);
            }

            double First(string column) => frame.Doubles(column)[0];

            var value = First(mapping.One(Value));
            var builder = Chart.Gauge().Value(value, mapping.One(Value));

            if (mapping.Has(Target))
            {
                builder = builder.Target(First(mapping.One(Target)));
            }

            if (mapping.Has(Minimum) || mapping.Has(Maximum))
            {
                var minimum = mapping.Has(Minimum) ? First(mapping.One(Minimum)) : 0;
                var maximum = mapping.Has(Maximum) ? First(mapping.One(Maximum)) : Math.Max(value, minimum + 1);
                builder = builder.Range(minimum, maximum);
            }

            return Present(builder, input.Presentation).Render();
        }

        private static string Bullet(DatasourceChartInput input)
        {
            var mapping = input.Mapping;
            var frame = input.Frame;
            var values = frame.Doubles(mapping.One(Value));
            var labels = mapping.Has(Category)
                ? frame.Texts(mapping.One(Category))
                : values.Select((_, i) => (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            var targets = mapping.Has(Target) ? frame.Doubles(mapping.One(Target)) : null;
            var builder = Chart.Bullet();

            for (var i = 0; i < values.Length; i++)
            {
                builder = builder.Item(labels[i], values[i],
                    targets != null && double.IsFinite(targets[i]) ? targets[i] : null);
            }

            return Present(builder, input.Presentation).Render();
        }

        private static string Sparkline(DatasourceChartInput input)
        {
            var mapping = input.Mapping;
            var frame = input.Frame;
            var builder = Chart.Sparkline().Name(mapping.One(Value))
                .Values(frame.Doubles(mapping.One(Value)),
                    mapping.Has(Category) ? frame.Texts(mapping.One(Category)) : null);

            return Present(builder, input.Presentation).Render();
        }

        private static string Treemap(DatasourceChartInput input)
        {
            var mapping = input.Mapping;
            var frame = input.Frame;
            var labels = frame.Texts(mapping.One(Category));
            var values = frame.Doubles(mapping.One(Value));
            var builder = Chart.Treemap();

            if (!mapping.Has(Parent))
            {
                for (var i = 0; i < labels.Length; i++)
                {
                    builder = builder.Item(labels[i], values[i]);
                }

                return Present(builder, input.Presentation).Render();
            }

            var parents = frame.Texts(mapping.One(Parent));

            foreach (var group in parents.Select((p, i) => (p, i)).GroupBy(g => g.p, StringComparer.Ordinal))
            {
                builder = builder.Group(group.Key, group.Select(g => (labels[g.i], values[g.i])).ToArray());
            }

            return Present(builder, input.Presentation).Render();
        }

        private static string Gantt(DatasourceChartInput input)
        {
            var mapping = input.Mapping;
            var frame = input.Frame;
            var labels = frame.Texts(mapping.One(Category));
            var starts = frame.Times(mapping.One(Start));
            var ends = frame.Times(mapping.One(End));
            var groups = mapping.Has(Series) ? frame.Texts(mapping.One(Series)) : null;
            var builder = Chart.Gantt();

            for (var i = 0; i < labels.Length; i++)
            {
                builder = builder.Task(labels[i], starts[i], ends[i], groups?[i]);
            }

            return Present(builder, input.Presentation).Render();
        }
    }
}
