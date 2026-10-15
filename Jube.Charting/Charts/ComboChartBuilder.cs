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

namespace Jube.Charting.Charts
{
    public sealed class ComboChartBuilder : ChartBuilder<ComboChartBuilder>
    {
        private readonly List<ComboSeries> series = [];
        private string[] categories = [];
        private Func<double, string>? format;
        private Func<double, string>? format2;
        private bool smooth;

        public ComboChartBuilder Categories(IEnumerable<string> categoryNames)
        {
            categories = [.. categoryNames];

            return this;
        }

        public ComboChartBuilder Columns(string name, IEnumerable<double> values, string? color = null,
            bool secondary = false)
        {
            series.Add(new ComboSeries(name, ComboKind.Column, [.. values], color, secondary));

            return this;
        }

        public ComboChartBuilder Line(string name, IEnumerable<double> values, string? color = null,
            bool secondary = false)
        {
            series.Add(new ComboSeries(name, ComboKind.Line, [.. values], color, secondary));

            return this;
        }

        public ComboChartBuilder Area(string name, IEnumerable<double> values, string? color = null,
            bool secondary = false)
        {
            series.Add(new ComboSeries(name, ComboKind.Area, [.. values], color, secondary));

            return this;
        }

        public ComboChartBuilder SecondaryYAxisTitle(string? title)
        {
            Options = Options with { SecondaryYAxisTitle = title };

            return this;
        }

        public ComboChartBuilder Smooth(bool enabled = true)
        {
            smooth = enabled;

            return this;
        }

        public ComboChartBuilder ValueFormat(Func<double, string> formatter)
        {
            format = formatter;

            return this;
        }

        public ComboChartBuilder SecondaryFormat(Func<double, string> formatter)
        {
            format2 = formatter;

            return this;
        }

        public ComboChart Build()
        {
            return new ComboChart(new ComboChartSpec
            {
                Options = Options,
                Categories = categories,
                Series = [.. series],
                Smooth = smooth,
                ValueFormat = format,
                SecondaryFormat = format2
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}