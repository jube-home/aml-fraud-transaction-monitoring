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

using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal static class PartialDependenceEngine
    {
        public static PartialDependenceResult Compute(string family, Func<double[][], double[]> predict,
            double[][] trainRows,
            double[][] rows, string[] names, string variable, int points, int seed, string units,
            List<EvaluationWarning> warnings)
        {
            var column = Array.IndexOf(names, variable);

            if (column < 0)
            {
                throw new ArgumentException($"Variable '{variable}' is not one of the variable names.",
                    nameof(variable));
            }

            var observed = trainRows.Select(r => r[column]).Where(double.IsFinite).OrderBy(v => v).ToArray();

            if (observed.Length == 0 || observed[0] == observed[^1])
            {
                throw new ArgumentException(
                    $"Variable '{variable}' is constant or empty, so it has no dependence to show.",
                    nameof(variable));
            }

            var grid = Enumerable.Range(0, points)
                .Select(i => ModelMath.Quantile(observed, 0.05 + 0.9 * i / Math.Max(1, points - 1))).Distinct()
                .ToArray();

            var random = new Random(SeededLibrary.Derive(seed, 3131));
            var take = Math.Min(rows.Length, 500);
            var used = rows.Length <= take ? rows : rows.OrderBy(_ => random.Next()).Take(take).ToArray();
            var result = new List<PartialDependencePoint>();
            var work = used.Select(r => (double[])r.Clone()).ToArray();

            foreach (var value in grid)
            {
                foreach (var row in work)
                {
                    row[column] = value;
                }

                var predictions = predict(work);
                var sorted = predictions.OrderBy(v => v).ToArray();

                result.Add(new PartialDependencePoint(value, predictions.Average(), ModelMath.Quantile(sorted, 0.1),
                    ModelMath.Quantile(sorted, 0.5), ModelMath.Quantile(sorted, 0.9)));
            }

            var means = result.Select(p => p.Mean).ToArray();
            var range = means.Max() - means.Min();
            var increasing = Enumerable.Range(1, means.Length - 1).Count(i => means[i] >= means[i - 1] - 1e-12);
            var decreasing = Enumerable.Range(1, means.Length - 1).Count(i => means[i] <= means[i - 1] + 1e-12);
            var steps = Math.Max(1, means.Length - 1);
            var scale = Math.Max(Math.Abs(means.Average()), 1e-9);
            string shape;

            if (range < 0.02 * Math.Max(scale, 0.05))
            {
                shape = "Flat: the average prediction barely moves with this variable.";
            }
            else if (increasing == steps)
            {
                shape = "Increasing: a higher value of the variable always raises the average prediction.";
            }
            else if (decreasing == steps)
            {
                shape = "Decreasing: a higher value of the variable always lowers the average prediction.";
            }
            else
            {
                shape =
                    "Non-monotone: the average prediction rises and falls across the range, an interaction or threshold effect.";
            }

            var text =
                $"Partial dependence of {family} on {variable}: the average predicted {units} moves by {range:0.####} across the 5th to 95th percentile of the variable ({result[0].Value:0.###} to {result[^1].Value:0.###}). {shape} " +
                "The 10th to 90th percentile band shows how much individual rows differ; a wide band around a flat mean means the effect depends on other variables.";

            return new PartialDependenceResult
            {
                Family = family,
                Variable = variable,
                Seed = seed,
                RowsUsed = used.Length,
                Points = result.ToArray(),
                Range = range,
                Shape = shape,
                Warnings = warnings.ToArray(),
                Interpretation = text
            };
        }
    }
}