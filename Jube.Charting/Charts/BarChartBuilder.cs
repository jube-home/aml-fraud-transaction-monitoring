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

using Jube.Charting.Charts.Models;
using Jube.Charting.Core;
using Jube.Charting.Exceptions;
using Jube.Charting.Core.Models;

namespace Jube.Charting.Charts
{
    public sealed class BarChartBuilder : ChartBuilder<BarChartBuilder>
    {
        private readonly List<ValueBand> bands = [];
        private readonly List<ValueLine> lines = [];
        private readonly List<ValueSeries> series = [];
        private string[] categories = [];
        private Func<double, string>? format;
        private BarGrouping grouping;
        private BarOrientation orientation;
        private bool showValues;

        public BarChartBuilder Categories(IEnumerable<string> categoryNames)
        {
            categories = [.. categoryNames];

            return this;
        }

        public BarChartBuilder Series(string name, IEnumerable<double> values, string? color = null,
            IEnumerable<double>? errors = null)
        {
            series.Add(new ValueSeries(name, [.. values], color, errors?.ToArray()));

            return this;
        }

        public BarChartBuilder PointColors(IEnumerable<string?> colors)
        {
            if (series.Count == 0)
            {
                throw new ChartSpecificationException("Add a series before assigning per-point colours.");
            }

            series[^1] = series[^1] with { PointColors = [.. colors] };

            return this;
        }

        public BarChartBuilder Range(string name, IEnumerable<double> from, IEnumerable<double> to,
            string? color = null)
        {
            series.Add(new ValueSeries(name, [.. to], color, null, [.. from]));

            return this;
        }

        public BarChartBuilder Line(double value, string? label = null)
        {
            lines.Add(new ValueLine(value, label));

            return this;
        }

        public BarChartBuilder Band(double from, double to, string? label = null)
        {
            bands.Add(new ValueBand(from, to, label));

            return this;
        }

        public BarChartBuilder PercentStacked(bool enabled = true)
        {
            grouping = enabled ? BarGrouping.Percent : BarGrouping.Grouped;

            return this;
        }

        public BarChartBuilder Horizontal(bool enabled = true)
        {
            orientation = enabled ? BarOrientation.Horizontal : BarOrientation.Vertical;

            return this;
        }

        public BarChartBuilder Stacked(bool enabled = true)
        {
            grouping = enabled ? BarGrouping.Stacked : BarGrouping.Grouped;

            return this;
        }

        public BarChartBuilder ShowValues(bool enabled = true)
        {
            showValues = enabled;

            return this;
        }

        public BarChartBuilder ValueFormat(Func<double, string> formatter)
        {
            format = formatter;

            return this;
        }

        public BarChart Build()
        {
            return new BarChart(new BarChartSpec
            {
                Options = Options,
                Categories = categories,
                Series = [.. series],
                Orientation = orientation,
                Grouping = grouping,
                ShowValues = showValues,
                ValueFormat = format,
                Lines = [.. lines],
                Bands = [.. bands]
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}