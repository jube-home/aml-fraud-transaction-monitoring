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
using Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival
{
    public enum KappaWeighting
    {
        None = 0,
        Linear = 1,
        Quadratic = 2
    }

    internal static class ClassifierMath
    {
        public static double Ratio(double numerator, double denominator)
        {
            return denominator == 0d ? double.NaN : numerator / denominator;
        }

        public static void CheckBeta(double beta)
        {
            if (double.IsNaN(beta) || double.IsInfinity(beta) || beta <= 0d)
            {
                throw new ArgumentException(
                    "beta must be a positive number: 1 weights precision and recall equally, 2 favours recall, 0.5 favours precision.",
                    nameof(beta));
            }
        }

        public static ConfusionMetricsResult Metrics(int tp, int fp, int tn, int fn, double beta, double level,
            double threshold, double? costFalseNegative, double? costFalsePositive)
        {
            var matrix = new ConfusionMatrix(tp, fp, tn, fn);
            var total = matrix.Total;
            var positives = tp + fn;
            var negatives = fp + tn;
            var recall = Ratio(tp, positives);
            var specificity = Ratio(tn, negatives);
            var precisionDefined = Ratio(tp, tp + fp);
            var npv = Ratio(tn, tn + fn);
            var accuracy = CategoricalIntervals.Proportion(tp + tn, total, level, ProportionIntervalMethod.Wilson);
            var precision = CategoricalIntervals.Proportion(tp, tp + fp, level, ProportionIntervalMethod.Wilson);
            var recallInterval = CategoricalIntervals.Proportion(tp, positives, level, ProportionIntervalMethod.Wilson);
            var specificityInterval =
                CategoricalIntervals.Proportion(tn, negatives, level, ProportionIntervalMethod.Wilson);

            var prevalence = Ratio(positives, total);
            var fpr = Ratio(fp, negatives);
            var fnr = Ratio(fn, positives);
            var lrPositive = Ratio(recall, fpr);
            var lrNegative = Ratio(fnr, specificity);
            var expectedCost = double.NaN;

            if (costFalseNegative.HasValue && costFalsePositive.HasValue)
            {
                expectedCost = matrix.ExpectedCost(new CostMatrix(costFalseNegative.Value, costFalsePositive.Value));
            }

            return new ConfusionMetricsResult
            {
                Threshold = threshold,
                TruePositives = tp,
                FalsePositives = fp,
                TrueNegatives = tn,
                FalseNegatives = fn,
                Total = total,
                Prevalence = prevalence,
                Accuracy = matrix.Accuracy,
                AccuracyLower = accuracy.Lower,
                AccuracyUpper = accuracy.Upper,
                Precision = matrix.Precision,
                PrecisionLower = precision.Lower,
                PrecisionUpper = precision.Upper,
                Recall = matrix.Recall,
                RecallLower = recallInterval.Lower,
                RecallUpper = recallInterval.Upper,
                Specificity = matrix.Specificity,
                SpecificityLower = specificityInterval.Lower,
                SpecificityUpper = specificityInterval.Upper,
                NegativePredictiveValue = npv,
                FalsePositiveRate = fpr,
                FalseNegativeRate = fnr,
                FalseDiscoveryRate = Ratio(fp, tp + fp),
                FalseOmissionRate = Ratio(fn, tn + fn),
                Beta = beta,
                F1 = matrix.F1,
                FBeta = matrix.FBeta(beta),
                MatthewsCorrelation = matrix.Mcc,
                CohenKappa = matrix.CohenKappa,
                BalancedAccuracy = (recall + specificity) / 2d,
                YoudenJ = recall + specificity - 1d,
                Markedness = precisionDefined + npv - 1d,
                GeometricMean = Math.Sqrt(recall * specificity),
                PositiveLikelihoodRatio = lrPositive,
                NegativeLikelihoodRatio = lrNegative,
                DiagnosticOddsRatio = Ratio(lrPositive, lrNegative),
                PredictedPositiveRate = Ratio(tp + fp, total),
                Lift = Ratio(precisionDefined, prevalence),
                ExpectedCostPerCase = expectedCost
            };
        }

        public static void CheckCosts(double? costFalseNegative, double? costFalsePositive)
        {
            if (costFalseNegative.HasValue != costFalsePositive.HasValue)
            {
                throw new ArgumentException("Give both costFalseNegative and costFalsePositive, or neither.",
                    nameof(costFalseNegative));
            }

            foreach (var cost in new[] { costFalseNegative, costFalsePositive })
            {
                if (cost.HasValue && (double.IsNaN(cost.Value) || double.IsInfinity(cost.Value) || cost.Value < 0d))
                {
                    throw new ArgumentException("Costs must be finite numbers that are not negative.",
                        nameof(costFalseNegative));
                }
            }
        }

        public static MulticlassMetricsResult Multiclass(int[][] matrix, string[] classes, double beta, double level,
            KappaWeighting weighting, int topConfusions)
        {
            var k = classes.Length;
            var rowTotals = new int[k];
            var columnTotals = new int[k];
            var total = 0;
            var trace = 0;

            for (var i = 0; i < k; i++)
            {
                for (var j = 0; j < k; j++)
                {
                    rowTotals[i] += matrix[i][j];
                    columnTotals[j] += matrix[i][j];
                    total += matrix[i][j];
                }

                trace += matrix[i][i];
            }

            if (total == 0)
            {
                throw new ArgumentException("The confusion matrix has no observations.");
            }

            var perClass = new List<MulticlassClassRow>();

            for (var c = 0; c < k; c++)
            {
                var tp = matrix[c][c];
                var fn = rowTotals[c] - tp;
                var fp = columnTotals[c] - tp;
                var tn = total - tp - fn - fp;
                var binary = new ConfusionMatrix(tp, fp, tn, fn);
                var recall = Ratio(tp, tp + fn);
                var specificity = Ratio(tn, tn + fp);

                perClass.Add(new MulticlassClassRow
                {
                    Class = classes[c],
                    Support = rowTotals[c],
                    PredictedCount = columnTotals[c],
                    TruePositives = tp,
                    FalsePositives = fp,
                    FalseNegatives = fn,
                    TrueNegatives = tn,
                    Precision = binary.Precision,
                    Recall = binary.Recall,
                    Specificity = binary.Specificity,
                    F1 = binary.F1,
                    FBeta = binary.FBeta(beta),
                    BalancedAccuracy = (recall + specificity) / 2d,
                    MatthewsCorrelation = binary.Mcc
                });
            }

            var macro = new MulticlassAverageRow
            {
                Kind = "macro",
                Precision = perClass.Average(r => r.Precision),
                Recall = perClass.Average(r => r.Recall),
                F1 = perClass.Average(r => r.F1),
                FBeta = perClass.Average(r => r.FBeta)
            };

            var supportTotal = perClass.Sum(r => (double)r.Support);

            var weighted = new MulticlassAverageRow
            {
                Kind = "weighted",
                Precision = perClass.Sum(r => r.Precision * r.Support) / supportTotal,
                Recall = perClass.Sum(r => r.Recall * r.Support) / supportTotal,
                F1 = perClass.Sum(r => r.F1 * r.Support) / supportTotal,
                FBeta = perClass.Sum(r => r.FBeta * r.Support) / supportTotal
            };

            var microConfusion = new ConfusionMatrix(perClass.Sum(r => r.TruePositives),
                perClass.Sum(r => r.FalsePositives), perClass.Sum(r => r.TrueNegatives),
                perClass.Sum(r => r.FalseNegatives));

            var micro = new MulticlassAverageRow
            {
                Kind = "micro",
                Precision = microConfusion.Precision,
                Recall = microConfusion.Recall,
                F1 = microConfusion.F1,
                FBeta = microConfusion.FBeta(beta)
            };

            double s = total;
            double sumProducts = 0;
            double sumPredicted = 0;
            double sumActual = 0;

            for (var c = 0; c < k; c++)
            {
                sumProducts += (double)columnTotals[c] * rowTotals[c];
                sumPredicted += (double)columnTotals[c] * columnTotals[c];
                sumActual += (double)rowTotals[c] * rowTotals[c];
            }

            var mccDenominator = Math.Sqrt((s * s - sumPredicted) * (s * s - sumActual));
            var mcc = mccDenominator == 0d ? 0d : (trace * s - sumProducts) / mccDenominator;

            var kappa = double.NaN;
            var kappaSe = double.NaN;
            var matrix2 = new int[k, k];

            for (var i = 0; i < k; i++)
            {
                for (var j = 0; j < k; j++)
                {
                    matrix2[i, j] = matrix[i][j];
                }
            }

            if (k >= 2)
            {
                var pair = CategoricalLibrary.Kappa(matrix2, (int)weighting);
                kappa = pair.Kappa;
                kappaSe = pair.StandardError;
            }

            var z = CategoricalGuards.TwoSidedZ(level);
            var accuracy = CategoricalIntervals.Proportion(trace, total, level, ProportionIntervalMethod.Wilson);
            var confusions = new List<ConfusedClassPair>();

            for (var i = 0; i < k; i++)
            {
                for (var j = 0; j < k; j++)
                {
                    if (i != j && matrix[i][j] > 0)
                    {
                        confusions.Add(new ConfusedClassPair
                        {
                            ActualClass = classes[i],
                            PredictedClass = classes[j],
                            Count = matrix[i][j],
                            ShareOfActualClass = matrix[i][j] / (double)rowTotals[i]
                        });
                    }
                }
            }

            return new MulticlassMetricsResult
            {
                Classes = classes,
                ConfusionMatrix = matrix,
                Total = total,
                Accuracy = trace / (double)total,
                AccuracyLower = accuracy.Lower,
                AccuracyUpper = accuracy.Upper,
                BalancedAccuracy = perClass.Where(r => r.Support > 0).Average(r => r.Recall),
                CohenKappa = kappa,
                CohenKappaStandardError = kappaSe,
                CohenKappaLower = kappa - z * kappaSe,
                CohenKappaUpper = kappa + z * kappaSe,
                KappaWeightingUsed = weighting.ToString(),
                MatthewsCorrelation = mcc,
                Beta = beta,
                PerClass = perClass.ToArray(),
                Macro = macro,
                Micro = micro,
                Weighted = weighted,
                TopConfusions = confusions.OrderByDescending(c => c.Count)
                    .ThenBy(c => c.ActualClass, StringComparer.Ordinal)
                    .ThenBy(c => c.PredictedClass, StringComparer.Ordinal).Take(topConfusions).ToArray()
            };
        }
    }
}