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
    internal static class ClassificationMetricsBuilder
    {
        public static ClassificationMetrics? Build(double[] probabilities, int[] labels, double threshold)
        {
            if (labels.Length == 0)
            {
                return null;
            }

            var positives = labels.Count(l => l == 1);
            var negatives = labels.Length - positives;
            var both = positives > 0 && negatives > 0;
            var confusion = BinaryMetrics.Confusion(probabilities, labels, threshold);
            double? auc = null, lower = null, upper = null, average = null, ks = null;

            if (both)
            {
                var withError = BinaryMetrics.RocAucWithStandardError(probabilities, labels);
                auc = ModelMath.Finite(withError.Auc);
                var interval = withError.Interval();
                lower = ModelMath.Finite(interval.Lower);
                upper = ModelMath.Finite(interval.Upper);
                average = ModelMath.Finite(BinaryMetrics.AveragePrecision(probabilities, labels));
                ks = ModelMath.Finite(BinaryMetrics.KolmogorovSmirnov(probabilities, labels));
            }

            var recall = ModelMath.Finite(confusion.Recall);
            var specificity = ModelMath.Finite(confusion.Specificity);

            return new ClassificationMetrics(
                labels.Length,
                positives,
                auc,
                lower,
                upper,
                average,
                ModelMath.Finite(BinaryMetrics.LogLoss(probabilities, labels)),
                ModelMath.Finite(BinaryMetrics.BrierScore(probabilities, labels)),
                ModelMath.Finite(BinaryMetrics.ExpectedCalibrationError(probabilities, labels)),
                ks,
                threshold,
                confusion.TruePositives,
                confusion.FalsePositives,
                confusion.TrueNegatives,
                confusion.FalseNegatives,
                ModelMath.Finite(confusion.Accuracy),
                ModelMath.Finite(confusion.Precision),
                recall,
                specificity,
                ModelMath.Finite(confusion.F1),
                recall.HasValue && specificity.HasValue ? (recall.Value + specificity.Value) / 2d : null,
                ModelMath.Finite(confusion.Mcc));
        }

        public static double Score(ClassificationMetrics? metrics, SelectionMetric metric)
        {
            if (metrics == null)
            {
                return double.NaN;
            }

            return metric switch
            {
                SelectionMetric.RocAuc => metrics.RocAuc ?? double.NaN,
                SelectionMetric.AveragePrecision => metrics.AveragePrecision ?? double.NaN,
                SelectionMetric.LogLoss => metrics.LogLoss ?? double.NaN,
                SelectionMetric.BrierScore => metrics.BrierScore ?? double.NaN,
                SelectionMetric.F1 => metrics.F1 ?? double.NaN,
                SelectionMetric.MatthewsCorrelation => metrics.MatthewsCorrelation ?? double.NaN,
                _ => double.NaN
            };
        }

        public static bool HigherIsBetter(SelectionMetric metric)
        {
            return metric is not (SelectionMetric.LogLoss or SelectionMetric.BrierScore);
        }

        public static ClassificationMetrics? Pool(IReadOnlyList<BinaryRun> runs)
        {
            var probabilities = runs.SelectMany(r => r.TestProbability).ToArray();
            var labels = runs.SelectMany(r => r.TestLabels).ToArray();

            if (labels.Length == 0)
            {
                return null;
            }

            var pooled = Build(probabilities, labels, 0.5)!;
            var tp = runs.Sum(r => r.TestMetrics?.TruePositives ?? 0);
            var fp = runs.Sum(r => r.TestMetrics?.FalsePositives ?? 0);
            var tn = runs.Sum(r => r.TestMetrics?.TrueNegatives ?? 0);
            var fn = runs.Sum(r => r.TestMetrics?.FalseNegatives ?? 0);
            var confusion = new ConfusionMatrix(tp, fp, tn, fn);
            var recall = ModelMath.Finite(confusion.Recall);
            var specificity = ModelMath.Finite(confusion.Specificity);

            return pooled with
            {
                Threshold = runs.Average(r => r.Threshold),
                TruePositives = tp,
                FalsePositives = fp,
                TrueNegatives = tn,
                FalseNegatives = fn,
                Accuracy = ModelMath.Finite(confusion.Accuracy),
                Precision = ModelMath.Finite(confusion.Precision),
                Recall = recall,
                Specificity = specificity,
                F1 = ModelMath.Finite(confusion.F1),
                BalancedAccuracy = recall.HasValue && specificity.HasValue
                    ? (recall.Value + specificity.Value) / 2d
                    : null,
                MatthewsCorrelation = ModelMath.Finite(confusion.Mcc)
            };
        }
    }
}