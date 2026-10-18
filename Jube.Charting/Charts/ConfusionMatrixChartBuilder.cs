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

using System.Globalization;
using Jube.Charting.Charts.Models;
using Jube.Charting.Core;
using Jube.Charting.Exceptions;
using Jube.Charting.Core.Models;

namespace Jube.Charting.Charts
{
    public sealed class ConfusionMatrixChartBuilder : ChartBuilder<ConfusionMatrixChartBuilder>
    {
        private IReadOnlyList<IReadOnlyList<double>> counts = [];
        private string[] labels = [];
        private ConfusionMode mode;

        public ConfusionMatrixChartBuilder()
        {
            Options = new ChartOptions { Width = 520, Height = 460 };
        }

        public ConfusionMatrixChartBuilder Labels(IEnumerable<string> labelNames)
        {
            labels = [.. labelNames];

            return this;
        }

        public ConfusionMatrixChartBuilder Matrix(IEnumerable<IEnumerable<double>> rowsActualColumnsPredicted)
        {
            counts = [.. rowsActualColumnsPredicted.Select(r => (IReadOnlyList<double>)[.. r])];

            return this;
        }

        public ConfusionMatrixChartBuilder Matrix(double[,] matrix)
        {
            var rows = new IReadOnlyList<double>[matrix.GetLength(0)];

            for (var r = 0; r < rows.Length; r++)
            {
                var row = new double[matrix.GetLength(1)];

                for (var c = 0; c < row.Length; c++)
                {
                    row[c] = matrix[r, c];
                }

                rows[r] = row;
            }

            counts = rows;

            return this;
        }

        public ConfusionMatrixChartBuilder Binary(double trueNegative, double falsePositive, double falseNegative,
            double truePositive, string negative = "Negative class", string positive = "Positive class")
        {
            labels = [negative, positive];
            counts = [[trueNegative, falsePositive], [falseNegative, truePositive]];

            return this;
        }

        public ConfusionMatrixChartBuilder FromPredictions(IEnumerable<int> actual, IEnumerable<int> predicted,
            IEnumerable<string>? classLabels = null)
        {
            var a = actual.ToArray();
            var p = predicted.ToArray();

            if (a.Length != p.Length)
            {
                throw new ChartSpecificationException("Actual and predicted classes must have the same length.");
            }

            ChartLimits.Observations(a.Length);
            var classes = a.Concat(p).Distinct().OrderBy(v => v).ToArray();
            ChartLimits.Categories(classes.Length);
            var index = classes.Select((v, i) => (v, i)).ToDictionary(t => t.v, t => t.i);
            var m = new double[classes.Length, classes.Length];

            for (var i = 0; i < a.Length; i++)
            {
                m[index[a[i]], index[p[i]]]++;
            }

            labels = classLabels?.ToArray() ??
                     [.. classes.Select(c => c.ToString(CultureInfo.InvariantCulture))];

            return Matrix(m);
        }

        public ConfusionMatrixChartBuilder Rates(bool rates = true)
        {
            mode = rates ? ConfusionMode.Rates : ConfusionMode.Counts;

            return this;
        }

        public ConfusionMatrixChart Build()
        {
            return new ConfusionMatrixChart(new ConfusionMatrixChartSpec
            {
                Options = Options,
                Labels = labels,
                Counts = counts,
                Mode = mode
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}