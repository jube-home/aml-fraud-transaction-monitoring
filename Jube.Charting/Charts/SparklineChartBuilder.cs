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
using Jube.Charting.Core.Models;

namespace Jube.Charting.Charts
{
    public sealed class SparklineChartBuilder : ChartBuilder<SparklineChartBuilder>
    {
        private bool extremes;
        private SparklineKind kind;
        private string[]? labels;
        private bool last = true;
        private string? name;
        private double[] values = [];

        public SparklineChartBuilder()
        {
            Options = new ChartOptions
            {
                Width = 160, Height = 36, TransparentBackground = true, Legend = LegendPosition.None
            };
        }

        public SparklineChartBuilder Values(IEnumerable<double> dataValues, IEnumerable<string>? pointLabels = null)
        {
            values = [.. dataValues];
            labels = pointLabels?.ToArray();

            return this;
        }

        public SparklineChartBuilder Name(string chartName)
        {
            name = chartName;

            return this;
        }

        public SparklineChartBuilder Kind(SparklineKind sparklineKind)
        {
            kind = sparklineKind;

            return this;
        }

        public SparklineChartBuilder MarkLast(bool mark = true)
        {
            last = mark;

            return this;
        }

        public SparklineChartBuilder MarkExtremes(bool mark = true)
        {
            extremes = mark;

            return this;
        }

        public SparklineChart Build()
        {
            return new SparklineChart(new SparklineChartSpec
            {
                Options = Options,
                Values = values,
                Labels = labels,
                Kind = kind,
                MarkLast = last,
                MarkExtremes = extremes,
                Name = name
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}