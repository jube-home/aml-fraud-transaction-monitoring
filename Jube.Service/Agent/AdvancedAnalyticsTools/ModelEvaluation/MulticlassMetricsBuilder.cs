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
    internal static class MulticlassMetricsBuilder
    {
        public static MulticlassMetrics? Build(double[][] probabilities, int[] y, int[] classLabels)
        {
            if (y.Length == 0)
            {
                return null;
            }

            var k = classLabels.Length;
            var confusion = new int[k][];

            for (var i = 0; i < k; i++)
            {
                confusion[i] = new int[k];
            }

            var predicted = probabilities.Select(MulticlassMath.ArgMax).ToArray();

            for (var i = 0; i < y.Length; i++)
            {
                confusion[y[i]][predicted[i]]++;
            }

            var total = (double)y.Length;
            var perClass = new List<ClassMetricsRow>();
            var recalls = new List<double>();
            var f1S = new List<double>();
            var supports = new List<int>();

            for (var c = 0; c < k; c++)
            {
                var tp = confusion[c][c];
                var support = confusion[c].Sum();
                var predictedCount = Enumerable.Range(0, k).Sum(r => confusion[r][c]);
                double? precision = predictedCount > 0 ? tp / (double)predictedCount : null;
                double? recall = support > 0 ? tp / (double)support : null;

                double? f1 = precision.HasValue && recall.HasValue && precision + recall > 0
                    ? 2d * precision.Value * recall.Value / (precision.Value + recall.Value)
                    : support > 0 && (precision ?? 0) + (recall ?? 0) == 0
                        ? 0d
                        : null;

                double? auc = null;

                var binary = y.Select(v => v == c ? 1 : 0).ToArray();

                if (binary.Any(v => v == 1) && binary.Any(v => v == 0))
                {
                    auc = ModelMath.Finite(BinaryMetrics.RocAuc(probabilities.Select(p => p[c]).ToArray(), binary));
                }

                perClass.Add(new ClassMetricsRow(classLabels[c], support, precision, recall, f1, auc));

                if (recall.HasValue)
                {
                    recalls.Add(recall.Value);
                }

                if (f1.HasValue)
                {
                    f1S.Add(f1.Value);
                }

                supports.Add(support);
            }

            var accuracy = Enumerable.Range(0, k).Sum(c => confusion[c][c]) / total;

            var expected = Enumerable.Range(0, k).Sum(c =>
                confusion[c].Sum() / total * (Enumerable.Range(0, k).Sum(r => confusion[r][c]) / total));

            double? kappa = expected < 1d ? (accuracy - expected) / (1d - expected) : null;

            double? mcc = null;

            var covariance = accuracy * total * total - Enumerable.Range(0, k).Sum(c =>
                confusion[c].Sum() * (double)Enumerable.Range(0, k).Sum(r => confusion[r][c]));

            var predictedSquares = total * total - Enumerable.Range(0, k).Sum(c =>
                Math.Pow(Enumerable.Range(0, k).Sum(r => confusion[r][c]), 2));

            var actualSquares = total * total - Enumerable.Range(0, k).Sum(c => Math.Pow(confusion[c].Sum(), 2));

            if (predictedSquares > 0 && actualSquares > 0)
            {
                mcc = covariance / Math.Sqrt(predictedSquares * actualSquares);
            }

            var logLoss = 0d;
            var brier = 0d;

            for (var i = 0; i < y.Length; i++)
            {
                logLoss -= Math.Log(Math.Max(probabilities[i][y[i]], 1e-15));

                for (var c = 0; c < k; c++)
                {
                    var d = probabilities[i][c] - (c == y[i] ? 1d : 0d);
                    brier += d * d;
                }
            }

            var aucs = perClass.Where(p => p.OneVersusRestAuc.HasValue).Select(p => p.OneVersusRestAuc!.Value).ToList();
            var supportTotal = supports.Sum();

            var weighted = supportTotal > 0
                ? perClass.Sum(p => (p.F1 ?? 0d) * p.Support) / supportTotal
                : (double?)null;

            return new MulticlassMetrics(
                y.Length,
                accuracy,
                recalls.Count > 0 ? recalls.Average() : null,
                f1S.Count > 0 ? f1S.Average() : null,
                accuracy,
                weighted,
                ModelMath.Finite(kappa ?? double.NaN),
                ModelMath.Finite(mcc ?? double.NaN),
                logLoss / y.Length,
                brier / y.Length,
                aucs.Count > 0 ? aucs.Average() : null,
                perClass.ToArray(),
                classLabels,
                confusion);
        }
    }
}