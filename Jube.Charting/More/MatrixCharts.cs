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
using Jube.Charting.Exceptions;
using Jube.Charting.More.Models;

namespace Jube.Charting.More
{
    public sealed class MatrixChartBuilder : ChartBuilder<MatrixChartBuilder>
    {
        private readonly List<(string From, string To, double Weight)> edges = [];
        private string[] columns = [];
        private int decimals = 2;
        private bool directed;
        private bool diverging;
        private string? legendTitle;
        private string[]? nodes;
        private MatrixOrder order;
        private string[] rows = [];
        private bool showValues = true;
        private double[][] values = [];

        public MatrixChartBuilder Rows(IEnumerable<string> labels)
        {
            rows = [.. labels];

            return this;
        }

        public MatrixChartBuilder Columns(IEnumerable<string> labels)
        {
            columns = [.. labels];

            return this;
        }

        public MatrixChartBuilder Values(IEnumerable<IEnumerable<double>> rowValues)
        {
            values = [.. rowValues.Select(r => r.ToArray())];

            return this;
        }

        public MatrixChartBuilder Nodes(IEnumerable<string> nodeNames)
        {
            nodes = [.. nodeNames];

            return this;
        }

        public MatrixChartBuilder Edge(string from, string to, double weight = 1)
        {
            edges.Add((from, to, weight));

            return this;
        }

        public MatrixChartBuilder Directed(bool isDirected = true)
        {
            directed = isDirected;

            return this;
        }

        public MatrixChartBuilder Order(MatrixOrder orderMode)
        {
            order = orderMode;

            return this;
        }

        public MatrixChartBuilder Diverging(bool enabled = true)
        {
            diverging = enabled;

            return this;
        }

        public MatrixChartBuilder ShowValues(bool show = true)
        {
            showValues = show;

            return this;
        }

        public MatrixChartBuilder Decimals(int decimalPlaces)
        {
            decimals = decimalPlaces;

            return this;
        }

        public MatrixChartBuilder LegendTitle(string? title)
        {
            legendTitle = title;

            return this;
        }

        public HeatmapChart Build()
        {
            var strings = this.rows;
            var second = this.columns;
            var doubles = this.values;
            var square = false;

            if (nodes is not null)
            {
                ChartLimits.Categories(nodes.Length);
                ChartLimits.Cells((long)nodes.Length * nodes.Length);
                var index = new Dictionary<string, int>();

                for (var i = 0; i < nodes.Length; i++)
                {
                    if (!index.TryAdd(nodes[i], i))
                    {
                        throw new ChartSpecificationException("Node names must be unique.");
                    }
                }

                var matrix = Enumerable.Range(0, nodes.Length).Select(_ => new double[nodes.Length]).ToArray();

                foreach (var (from, to, weight) in edges)
                {
                    if (!index.TryGetValue(from, out var a) || !index.TryGetValue(to, out var b))
                    {
                        throw new ChartSpecificationException("Every edge must join two named nodes.");
                    }

                    if (!double.IsFinite(weight))
                    {
                        throw new ChartSpecificationException("Edge weights must be finite.");
                    }

                    matrix[a][b] += weight;

                    if (!directed && a != b)
                    {
                        matrix[b][a] += weight;
                    }
                }

                strings = nodes;
                second = nodes;
                doubles = matrix;
                square = true;
            }

            if (doubles.Length != strings.Length || doubles.Any(r => r.Length != second.Length))
            {
                throw new ChartSpecificationException(
                    "The matrix must have one row per row label and one column per column label.");
            }

            var rowOrder = Enumerable.Range(0, strings.Length).ToArray();
            var columnOrder = Enumerable.Range(0, second.Length).ToArray();

            if (order == MatrixOrder.Cluster)
            {
                if (square || strings.SequenceEqual(second))
                {
                    var n = strings.Length;
                    var d = new double[n, n];

                    for (var i = 0; i < n; i++)
                    {
                        for (var j = 0; j < n; j++)
                        {
                            var sum = 0.0;

                            for (var k = 0; k < n; k++)
                            {
                                var x = doubles[i][k];
                                var y = doubles[j][k];
                                sum += double.IsFinite(x) && double.IsFinite(y) ? (x - y) * (x - y) : 0;
                            }

                            d[i, j] = Math.Sqrt(sum);
                        }
                    }

                    rowOrder = HierarchicalOrder.Order(d);
                    columnOrder = rowOrder;
                }
                else
                {
                    rowOrder = HierarchicalOrder.OrderRows([.. doubles.Select(r => (IReadOnlyList<double>)r)]);

                    var transposed = Enumerable.Range(0, second.Length)
                        .Select(c => (IReadOnlyList<double>)[.. doubles.Select(r => r[c])]).ToArray();

                    columnOrder = HierarchicalOrder.OrderRows(transposed);
                }
            }

            var ordered = rowOrder.Select(r => columnOrder.Select(c => doubles[r][c]).ToArray()).ToArray();

            var description = Options.Description ??
                              "Matrix of " + strings.Length + " rows and " + second.Length + " columns" +
                              (order == MatrixOrder.Cluster ? ", ordered by hierarchical clustering." : ".");

            var builder = new HeatmapChartBuilder().WithOptions(Options with { Description = description })
                .Rows(rowOrder.Select(i => strings[i])).Columns(columnOrder.Select(i => second[i])).Values(ordered)
                .ShowValues(showValues).Decimals(decimals).LegendTitle(legendTitle);

            if (diverging)
            {
                builder.Diverging();
            }

            return builder.Build();
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}