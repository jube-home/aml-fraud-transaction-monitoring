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

using System.ComponentModel;
using Jube.Analytics;
using Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival
{
    [AdvancedAnalyticsToolGroup("CategoricalAndSurvival")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class ConfusionMatrixTools
    {
        private const int MaximumClasses = 500;

        [AdvancedAnalyticsTool("CalculateBinaryClassificationMetricsFromCounts")]
        [Description(
            "Computes the full set of binary classification metrics from the four confusion counts of a rule, alert or model at one operating point. " +
            "Returns accuracy, precision (positive predictive value), recall (sensitivity, detection rate), specificity, negative predictive value, false positive and negative rates, false discovery and omission rates, F1 and F-beta (beta above 1 favours recall, below 1 precision), Matthews correlation, Cohen kappa, balanced accuracy, Youden J, markedness, geometric mean, likelihood ratios, diagnostic odds ratio, alert rate (PredictedPositiveRate) and lift, with Wilson intervals for accuracy, precision, recall and specificity. " +
            "Precision, recall, specificity, F-scores, MCC and kappa follow the shared platform definitions and report 0 when their denominator is empty; every other ratio is NaN when undefined. Supply costs to also get the expected cost per case.")]
        public ConfusionMetricsResult CalculateBinaryClassificationMetricsFromCounts(
            [Description("True positives: cases flagged that truly were events.")]
            int truePositives,
            [Description("False positives: cases flagged that were not events.")]
            int falsePositives,
            [Description("True negatives: cases not flagged that were not events.")]
            int trueNegatives,
            [Description("False negatives: events that were not flagged.")]
            int falseNegatives,
            [Description(
                "F-beta weight, above 0: 1 balances precision and recall, 2 favours recall, 0.5 favours precision. Default 1.")]
            double beta = 1d,
            [Description("Confidence level for the Wilson intervals, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95,
            [Description(
                "Cost of missing an event (false negative); give together with costFalsePositive to get the expected cost per case.")]
            double? costFalseNegative = null,
            [Description("Cost of a false alert (false positive); give together with costFalseNegative.")]
            double? costFalsePositive = null)
        {
            if (truePositives < 0 || falsePositives < 0 || trueNegatives < 0 || falseNegatives < 0)
            {
                throw new ArgumentException("Confusion counts cannot be negative.", nameof(truePositives));
            }

            if ((long)truePositives + falsePositives + trueNegatives + falseNegatives == 0L)
            {
                throw new ArgumentException("All four counts are zero, so there is nothing to measure.",
                    nameof(truePositives));
            }

            ClassifierMath.CheckBeta(beta);
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));
            ClassifierMath.CheckCosts(costFalseNegative, costFalsePositive);

            return ClassifierMath.Metrics(truePositives, falsePositives, trueNegatives, falseNegatives, beta,
                confidenceLevel, double.NaN, costFalseNegative, costFalsePositive);
        }

        [AdvancedAnalyticsTool("CalculateBinaryClassificationMetricsFromLabels")]
        [Description(
            "Counts the confusion matrix from two parallel 0/1 lists, actual outcome and predicted (flagged) outcome, and returns the full set of binary classification metrics: accuracy, precision, recall, specificity, negative predictive value, error rates, F1 and F-beta, Matthews correlation, Cohen kappa, balanced accuracy, Youden J, geometric mean, likelihood ratios, alert rate and lift, with Wilson intervals. " +
            "Use it when you have per-case flags rather than counts. Precision, recall, specificity, F-scores, MCC and kappa report 0 when their denominator is empty; other undefined ratios are NaN.")]
        public ConfusionMetricsResult CalculateBinaryClassificationMetricsFromLabels(
            [Description("Actual outcome per case: 1 for the event, 0 otherwise.")]
            int[] actual,
            [Description("Predicted outcome per case in the same order and length: 1 if flagged, 0 otherwise.")]
            int[] predicted,
            [Description("F-beta weight, above 0: 1 balances precision and recall, 2 favours recall. Default 1.")]
            double beta = 1d,
            [Description("Confidence level for the Wilson intervals, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95,
            [Description(
                "Cost of a missed event (false negative); give together with costFalsePositive for the expected cost per case.")]
            double? costFalseNegative = null,
            [Description("Cost of a false alert (false positive); give together with costFalseNegative.")]
            double? costFalsePositive = null)
        {
            CategoricalGuards.BinaryLabels(actual, nameof(actual));
            CategoricalGuards.BinaryLabels(predicted, nameof(predicted));
            CategoricalGuards.SameLength(actual.Length, predicted.Length, nameof(actual), nameof(predicted));

            if (actual.Length == 0)
            {
                throw new ArgumentException("There are no cases to evaluate.", nameof(actual));
            }

            ClassifierMath.CheckBeta(beta);
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));
            ClassifierMath.CheckCosts(costFalseNegative, costFalsePositive);
            var tp = 0;
            var fp = 0;
            var tn = 0;
            var fn = 0;

            for (var i = 0; i < actual.Length; i++)
            {
                if (actual[i] == 1)
                {
                    if (predicted[i] == 1)
                    {
                        tp++;
                    }
                    else
                    {
                        fn++;
                    }
                }
                else if (predicted[i] == 1)
                {
                    fp++;
                }
                else
                {
                    tn++;
                }
            }

            return ClassifierMath.Metrics(tp, fp, tn, fn, beta, confidenceLevel, double.NaN, costFalseNegative,
                costFalsePositive);
        }

        [AdvancedAnalyticsTool("CalculateBinaryClassificationMetricsAtThreshold")]
        [Description(
            "Turns a continuous score or probability into flags at one threshold (score at or above the threshold is flagged) and returns the full set of binary classification metrics against the true 0/1 outcome: accuracy, precision, recall, specificity, F1 and F-beta, Matthews correlation, Cohen kappa, balanced accuracy, Youden J, likelihood ratios, alert rate and lift, with Wilson intervals. " +
            "Rows with a NaN score are dropped. Use ComputeThresholdSweep to see many thresholds at once or the SelectThreshold tools to pick one.")]
        public ConfusionMetricsResult CalculateBinaryClassificationMetricsAtThreshold(
            [Description("Model score or probability for each case; NaN rows are dropped.")]
            double[] scores,
            [Description("True outcome per case in the same order: 1 for the event, 0 otherwise.")]
            int[] labels,
            [Description("Cut-off: cases with a score greater than or equal to it are flagged.")]
            double threshold,
            [Description("F-beta weight, above 0: 1 balances precision and recall, 2 favours recall. Default 1.")]
            double beta = 1d,
            [Description("Confidence level for the Wilson intervals, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95,
            [Description(
                "Cost of a missed event (false negative); give together with costFalsePositive for the expected cost per case.")]
            double? costFalseNegative = null,
            [Description("Cost of a false alert (false positive); give together with costFalseNegative.")]
            double? costFalsePositive = null)
        {
            ScoreGuards.Pairs(scores, labels, out var s, out var y, out _);

            if (double.IsNaN(threshold))
            {
                throw new ArgumentException("threshold must be a number, not NaN.", nameof(threshold));
            }

            ClassifierMath.CheckBeta(beta);
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));
            ClassifierMath.CheckCosts(costFalseNegative, costFalsePositive);
            var matrix = BinaryMetrics.Confusion(s, y, threshold);

            return ClassifierMath.Metrics(matrix.TruePositives, matrix.FalsePositives, matrix.TrueNegatives,
                matrix.FalseNegatives, beta, confidenceLevel, threshold, costFalseNegative, costFalsePositive);
        }

        [AdvancedAnalyticsTool("CalculateMulticlassClassificationMetrics")]
        [Description(
            "Evaluates a multi-class classifier from two parallel lists of class labels, the actual class and the predicted class per case. " +
            "Returns the confusion matrix (rows actual, columns predicted), overall accuracy with an interval, balanced accuracy, Cohen kappa with standard error and interval (optionally linear or quadratic weighted for ordered classes), multi-class Matthews correlation, per-class precision, recall, specificity, F1, F-beta and support, macro, micro and support-weighted averages, and the most frequent confusions. " +
            "Macro treats every class equally, weighted follows class size, micro pools all cases and equals accuracy. Classes that are never predicted have precision 0. Pairs with a blank label on either side are skipped. For weighted kappa list the classes in their natural order in classOrder.")]
        public MulticlassMetricsResult CalculateMulticlassClassificationMetrics(
            [Description("Actual class label per case; blank entries skip the pair.")]
            string[] actual,
            [Description(
                "Predicted class label per case, same order and length as actual; blank entries skip the pair.")]
            string[] predicted,
            [Description("F-beta weight, above 0, applied per class and to the averages. Default 1.")]
            double beta = 1d,
            [Description("Confidence level for intervals, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95,
            [Description(
                "None for ordinary kappa, Linear or Quadratic to penalise confusions by how far apart the ordered classes are.")]
            KappaWeighting kappaWeighting = KappaWeighting.None,
            [Description(
                "Optional class names in their natural order, used for the matrix layout and weighted kappa; must include every class that occurs. Empty sorts labels alphabetically.")]
            string[]? classOrder = null,
            [Description("How many of the most frequent off-diagonal confusions to list. Default 10.")]
            int topConfusions = 10)
        {
            CategoricalGuards.Required(actual, nameof(actual));
            CategoricalGuards.Required(predicted, nameof(predicted));
            CategoricalGuards.SameLength(actual.Length, predicted.Length, nameof(actual), nameof(predicted));
            ClassifierMath.CheckBeta(beta);
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));

            if (topConfusions < 0)
            {
                throw new ArgumentException("topConfusions cannot be negative.", nameof(topConfusions));
            }

            var pairs = new List<(string Actual, string Predicted)>();

            for (var i = 0; i < actual.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(actual[i]) && !string.IsNullOrWhiteSpace(predicted[i]))
                {
                    pairs.Add((actual[i], predicted[i]));
                }
            }

            if (pairs.Count == 0)
            {
                throw new ArgumentException("There are no cases with both an actual and a predicted class.",
                    nameof(actual));
            }

            var observed = pairs.SelectMany(p => new[] { p.Actual, p.Predicted }).Distinct().ToList();
            string[] classes;

            if (classOrder is { Length: > 0 })
            {
                var missing = observed.Where(o => !classOrder.Contains(o)).ToList();

                if (missing.Count > 0)
                {
                    throw new ArgumentException(
                        $"classOrder is missing classes that occur in the data: {string.Join(", ", missing)}.",
                        nameof(classOrder));
                }

                if (classOrder.Distinct().Count() != classOrder.Length)
                {
                    throw new ArgumentException("classOrder contains duplicate class names.", nameof(classOrder));
                }

                classes = classOrder;
            }
            else
            {
                classes = observed.OrderBy(o => o, StringComparer.Ordinal).ToArray();
            }

            if (classes.Length > MaximumClasses)
            {
                throw new ArgumentException($"More than {MaximumClasses} distinct classes; merge rare classes first.",
                    nameof(actual));
            }

            var index = classes.Select((name, position) => (name, position)).ToDictionary(t => t.name, t => t.position);
            var matrix = new int[classes.Length][];

            for (var i = 0; i < matrix.Length; i++)
            {
                matrix[i] = new int[classes.Length];
            }

            foreach (var pair in pairs)
            {
                matrix[index[pair.Actual]][index[pair.Predicted]]++;
            }

            return ClassifierMath.Multiclass(matrix, classes, beta, confidenceLevel, kappaWeighting, topConfusions);
        }

        [AdvancedAnalyticsTool("CalculateMulticlassMetricsFromConfusionMatrix")]
        [Description(
            "Evaluates a multi-class classifier from a square confusion matrix of counts whose rows are actual classes and columns are predicted classes, in the same class order. " +
            "Returns overall accuracy with an interval, balanced accuracy, Cohen kappa with standard error and interval (optionally linear or quadratic weighted for ordered classes), multi-class Matthews correlation, per-class precision, recall, specificity, F1, F-beta and support, macro, micro and weighted averages, and the most frequent confusions. " +
            "Counts must be whole numbers. Classes never predicted have precision 0; classes with no actual cases are excluded from balanced accuracy.")]
        public MulticlassMetricsResult CalculateMulticlassMetricsFromConfusionMatrix(
            [Description(
                "Square matrix of counts, one array per actual class, columns are predicted classes in the same order; whole non-negative numbers.")]
            double[][] confusionMatrix,
            [Description("Optional class names in matrix order; empty labels them Class1, Class2 and so on.")]
            string[]? classNames = null,
            [Description("F-beta weight, above 0, applied per class and to the averages. Default 1.")]
            double beta = 1d,
            [Description("Confidence level for intervals, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95,
            [Description(
                "None for ordinary kappa, Linear or Quadratic to penalise confusions by class distance in the given order.")]
            KappaWeighting kappaWeighting = KappaWeighting.None,
            [Description("How many of the most frequent off-diagonal confusions to list. Default 10.")]
            int topConfusions = 10)
        {
            var table = CategoricalGuards.Table(confusionMatrix, nameof(confusionMatrix));

            if (table.Length != table[0].Length)
            {
                throw new ArgumentException(
                    "The confusion matrix must be square: one row and one column per class in the same order.",
                    nameof(confusionMatrix));
            }

            if (table.Length > MaximumClasses)
            {
                throw new ArgumentException($"More than {MaximumClasses} classes; merge rare classes first.",
                    nameof(confusionMatrix));
            }

            ClassifierMath.CheckBeta(beta);
            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));

            if (topConfusions < 0)
            {
                throw new ArgumentException("topConfusions cannot be negative.", nameof(topConfusions));
            }

            var matrix = new int[table.Length][];

            for (var i = 0; i < table.Length; i++)
            {
                matrix[i] = new int[table.Length];

                for (var j = 0; j < table.Length; j++)
                {
                    if (Math.Abs(table[i][j] - Math.Round(table[i][j])) > 1e-9 || table[i][j] > int.MaxValue / 4.0)
                    {
                        throw new ArgumentException(
                            $"Counts must be whole numbers of manageable size; row {i} column {j} is {table[i][j]}.",
                            nameof(confusionMatrix));
                    }

                    matrix[i][j] = (int)Math.Round(table[i][j]);
                }
            }

            return ClassifierMath.Multiclass(matrix,
                CategoricalGuards.Names(classNames, table.Length, "Class", nameof(classNames)), beta,
                confidenceLevel, kappaWeighting, topConfusions);
        }
    }
}