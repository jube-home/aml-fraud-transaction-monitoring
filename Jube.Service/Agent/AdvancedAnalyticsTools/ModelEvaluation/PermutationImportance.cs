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

using Jube.Analytics;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal static class PermutationImportance
    {
        private const long PredictionBudget = 1_500_000;

        public static int RowsFor(int available, int features, int repeats, int runs)
        {
            var budgeted = (int)Math.Max(50,
                PredictionBudget / Math.Max(1, (long)features * Math.Max(1, repeats) * Math.Max(1, runs)));

            return Math.Min(available, Math.Min(ModelEvaluationLimits.MaximumImportanceRows, budgeted));
        }

        public static double Permute<TOut>(Func<double[][], TOut> predict, double[][] rows, Func<TOut, double> score,
            bool higherIsBetter, int repeats, int seed, List<double>[] drops, CancellationToken token)
        {
            var baseline = score(predict(rows));

            if (!double.IsFinite(baseline))
            {
                return double.NaN;
            }

            var work = rows.Select(r => (double[])r.Clone()).ToArray();
            var features = rows[0].Length;

            for (var j = 0; j < features; j++)
            {
                token.ThrowIfCancellationRequested();
                var original = rows.Select(r => r[j]).ToArray();

                for (var r = 0; r < repeats; r++)
                {
                    var random = new Random(SeededLibrary.Derive(seed, j * 131 + r + 7));
                    var shuffled = (double[])original.Clone();

                    for (var i = shuffled.Length - 1; i > 0; i--)
                    {
                        var k = random.Next(i + 1);
                        (shuffled[i], shuffled[k]) = (shuffled[k], shuffled[i]);
                    }

                    for (var i = 0; i < work.Length; i++)
                    {
                        work[i][j] = shuffled[i];
                    }

                    var permuted = score(predict(work));

                    if (double.IsFinite(permuted))
                    {
                        drops[j].Add(higherIsBetter ? baseline - permuted : permuted - baseline);
                    }
                }

                for (var i = 0; i < work.Length; i++)
                {
                    work[i][j] = original[i];
                }
            }

            return baseline;
        }

        public static EvaluationImportanceRow[] Rows(string[] names, List<double>[] drops)
        {
            var means = drops.Select(d => d.Count == 0 ? 0d : d.Average()).ToArray();
            var deviations = drops.Select(d => ModelMath.Finite(ModelMath.StandardDeviation(d))).ToArray();
            var order = Enumerable.Range(0, names.Length).OrderByDescending(i => means[i]).ThenBy(i => i).ToArray();

            return order.Select((i, rank) => new EvaluationImportanceRow(names[i], means[i], deviations[i], rank + 1))
                .ToArray();
        }

        public static EvaluationImportanceRow[] ComputeMany(IReadOnlyList<BinaryRun> runs, double[][] x, int[] y,
            string[] names, EvaluationOptions options, CancellationToken token)
        {
            var drops = names.Select(_ => new List<double>()).ToArray();
            var any = false;

            for (var f = 0; f < runs.Count; f++)
            {
                var run = runs[f];
                var take = RowsFor(run.TestRows.Length, names.Length, options.ImportanceRepeats, runs.Count);
                var chosen = run.TestRows.Take(take).ToArray();
                var rows = DataSplit.Take(x, chosen);
                var labels = DataSplit.Take(y, chosen);

                if (labels.All(l => l == 1) || labels.All(l => l == 0))
                {
                    continue;
                }

                var baseline = Permute(run.Predict, rows, scores => BinaryMetrics.RocAuc(scores, labels), true,
                    options.ImportanceRepeats, SeededLibrary.Derive(options.Seed, 5000 + f), drops, token);

                any |= double.IsFinite(baseline);
            }

            return any ? Rows(names, drops) : [];
        }
    }
}