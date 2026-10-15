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

using Jube.Charting.Charts;
using Jube.Charting.Core;
using Jube.Charting.More.Models;

namespace Jube.Charting.More
{
    public sealed class CorrelationMatrixChartBuilder : ChartBuilder<CorrelationMatrixChartBuilder>
    {
        private readonly List<(string Name, double[] Values)> variables = [];
        private bool absolute = true;
        private int decimals = 2;
        private MatrixOrder order = MatrixOrder.Cluster;
        private bool showValues = true;

        public CorrelationMatrixChartBuilder Variable(string name, IEnumerable<double> values)
        {
            variables.Add((name, [.. values]));

            return this;
        }

        public CorrelationMatrixChartBuilder Order(MatrixOrder orderMode)
        {
            order = orderMode;

            return this;
        }

        public CorrelationMatrixChartBuilder ClusterOnAbsolute(bool byAbsoluteValue = true)
        {
            absolute = byAbsoluteValue;

            return this;
        }

        public CorrelationMatrixChartBuilder Decimals(int decimalPlaces)
        {
            decimals = decimalPlaces;

            return this;
        }

        public CorrelationMatrixChartBuilder ShowValues(bool show = true)
        {
            showValues = show;

            return this;
        }

        public static double[,] Correlations(IReadOnlyList<(string Name, double[] Values)> variables)
        {
            var n = variables.Count;
            var matrix = new double[n, n];

            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < n; j++)
                {
                    matrix[i, j] = i == j ? 1 : ChartStats.Pearson(variables[i].Values, variables[j].Values, out _);
                }
            }

            return matrix;
        }

        public HeatmapChart Build()
        {
            ChartLimits.Categories(variables.Count);
            var n = variables.Count;
            var matrix = Correlations(variables);
            var array = Enumerable.Range(0, n).ToArray();

            if (this.order == MatrixOrder.Cluster && n > 2)
            {
                var distance = new double[n, n];

                for (var i = 0; i < n; i++)
                {
                    for (var j = 0; j < n; j++)
                    {
                        var r = matrix[i, j];
                        distance[i, j] = double.IsFinite(r) ? 1 - (absolute ? Math.Abs(r) : r) : double.NaN;
                    }
                }

                array = HierarchicalOrder.Order(distance);
            }

            var labels = array.Select(i => variables[i].Name).ToArray();
            var ordered = new double[n, n];

            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < n; j++)
                {
                    ordered[i, j] = matrix[array[i], array[j]];
                }
            }

            var description = Options.Description ??
                              "Correlation heatmap of " + n + " variables" + (this.order == MatrixOrder.Cluster
                                  ? " ordered by hierarchical clustering: " + MoreKit.Names(labels, 8) + "."
                                  : ".");

            return new HeatmapChartBuilder().WithOptions(Options with { Description = description })
                .Correlation(labels, ordered).Decimals(decimals).ShowValues(showValues).Build();
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}