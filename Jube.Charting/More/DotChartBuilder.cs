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
using Jube.Charting.More.Models;
using Jube.Charting.Core.Models;

namespace Jube.Charting.More
{
    public sealed class DotChartBuilder : ChartBuilder<DotChartBuilder>
    {
        private readonly List<ValueSeries> series = [];
        private double baseline;
        private IReadOnlyList<string> categories = [];
        private Func<double, string>? format;
        private bool horizontal = true;
        private DotSort sort;
        private int sortSeries;
        private DotStyle style;
        private bool values;

        public DotChartBuilder Style(DotStyle dotStyle)
        {
            style = dotStyle;

            return this;
        }

        public DotChartBuilder Categories(IEnumerable<string> categoryNames)
        {
            categories = [.. categoryNames];

            return this;
        }

        public DotChartBuilder Series(string name, IEnumerable<double> seriesValues, string? color = null)
        {
            series.Add(new ValueSeries(name, [.. seriesValues], color));

            return this;
        }

        public DotChartBuilder Vertical(bool vertical = true)
        {
            horizontal = !vertical;

            return this;
        }

        public DotChartBuilder Sorted(DotSort sortMode, int bySeries = 0)
        {
            sort = sortMode;
            sortSeries = bySeries;

            return this;
        }

        public DotChartBuilder ShowValues(bool show = true)
        {
            values = show;

            return this;
        }

        public DotChartBuilder Baseline(double baselineValue)
        {
            baseline = baselineValue;

            return this;
        }

        public DotChartBuilder ValueFormat(Func<double, string> formatter)
        {
            format = formatter;

            return this;
        }

        public DotChart Build()
        {
            return new DotChart(new DotChartSpec
            {
                Options = Options,
                Categories = categories,
                Series = [.. series],
                Style = style,
                Horizontal = horizontal,
                Sort = sort,
                SortSeries = sortSeries,
                ShowValues = values,
                Baseline = baseline,
                ValueFormat = format
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}