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
    public sealed class HeatmapChartBuilder : ChartBuilder<HeatmapChartBuilder>
    {
        private double center;
        private string[] cols = [];
        private int decimals = 2;
        private bool diverging;
        private string? legendTitle;
        private double? max;
        private double? min;
        private string[] rows = [];
        private bool showValues = true;
        private IReadOnlyList<IReadOnlyList<double>> values = [];

        public HeatmapChartBuilder Rows(IEnumerable<string> labels)
        {
            rows = [.. labels];

            return this;
        }

        public HeatmapChartBuilder Columns(IEnumerable<string> labels)
        {
            cols = [.. labels];

            return this;
        }

        public HeatmapChartBuilder Values(IEnumerable<IEnumerable<double>> rowValues)
        {
            values = [.. rowValues.Select(r => (IReadOnlyList<double>)[.. r])];

            return this;
        }

        public HeatmapChartBuilder Values(double[,] matrix)
        {
            var lists = new IReadOnlyList<double>[matrix.GetLength(0)];

            for (var r = 0; r < lists.Length; r++)
            {
                var row = new double[matrix.GetLength(1)];

                for (var c = 0; c < row.Length; c++)
                {
                    row[c] = matrix[r, c];
                }

                lists[r] = row;
            }

            values = lists;

            return this;
        }

        public HeatmapChartBuilder Correlation(IEnumerable<string> labels, double[,] matrix)
        {
            var l = labels.ToArray();
            rows = l;
            cols = l;
            diverging = true;
            min = -1;
            max = 1;
            center = 0;
            legendTitle = "r";

            return Values(matrix);
        }

        public HeatmapChartBuilder Diverging(double centerValue = 0)
        {
            diverging = true;
            center = centerValue;

            return this;
        }

        public HeatmapChartBuilder Range(double minimum, double maximum)
        {
            min = minimum;
            max = maximum;

            return this;
        }

        public HeatmapChartBuilder ShowValues(bool show = true)
        {
            showValues = show;

            return this;
        }

        public HeatmapChartBuilder Decimals(int precision)
        {
            decimals = precision;

            return this;
        }

        public HeatmapChartBuilder LegendTitle(string? title)
        {
            legendTitle = title;

            return this;
        }

        public HeatmapChart Build()
        {
            return new HeatmapChart(new HeatmapChartSpec
            {
                Options = Options,
                RowLabels = rows,
                ColumnLabels = cols,
                Values = values,
                Diverging = diverging,
                Min = min,
                Max = max,
                Center = center,
                ShowValues = showValues,
                Decimals = decimals,
                LegendTitle = legendTitle
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}