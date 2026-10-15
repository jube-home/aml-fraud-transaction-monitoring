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
using Jube.Analytics.Models;
using Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival
{
    [AdvancedAnalyticsToolGroup("CategoricalAndSurvival")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class ProbabilityCalibrationTools
    {
        private const int MaximumBins = 1000;

        [AdvancedAnalyticsTool("ComputeCalibrationCurve")]
        [Description(
            "Builds a reliability (calibration) curve for predicted probabilities against a 0/1 outcome: cases are grouped into bins by predicted probability and each bin's average predicted probability is compared with the event rate actually observed there, with a Wilson interval on the observed rate. " +
            "A well-calibrated model has observed close to predicted in every bin; Gap is observed minus predicted (positive means the model under-predicts). The result also gives the expected calibration error (count-weighted mean absolute gap), the maximum calibration error, Brier score, log loss and the calibration slope and intercept from a logistic recalibration on the logit of the probability (slope 1 and intercept 0 are ideal; slope below 1 means over-confident predictions). " +
            "Probabilities must lie between 0 and 1; rows with NaN are dropped. UniformWidth bins split 0 to 1 evenly, EqualCount bins hold similar numbers of cases and suit skewed rare-event probabilities. Empty bins are omitted.")]
        public CalibrationCurveResult ComputeCalibrationCurve(
            [Description("Predicted probability of the event for each case, between 0 and 1; NaN rows are dropped.")]
            double[] probabilities,
            [Description("True outcome per case in the same order: 1 for the event, 0 otherwise.")]
            int[] labels,
            [Description("Number of bins, from 1 to 1000. Default 10.")]
            int bins = 10,
            [Description("UniformWidth (default) or EqualCount binning.")]
            CalibrationBinning binning = CalibrationBinning.UniformWidth,
            [Description("Confidence level for the observed-rate intervals, strictly between 0 and 1. Default 0.95.")]
            double confidenceLevel = 0.95)
        {
            CalibrationMath.Clean(probabilities, labels, out var p, out var y, out var skipped);

            if (bins < 1 || bins > MaximumBins)
            {
                throw new ArgumentException($"bins must be between 1 and {MaximumBins}.", nameof(bins));
            }

            CategoricalGuards.Level(confidenceLevel, nameof(confidenceLevel));
            var rows = new List<CalibrationCurveBin>();

            if (binning == CalibrationBinning.UniformWidth)
            {
                var buckets = new List<int>[bins];

                for (var b = 0; b < bins; b++)
                {
                    buckets[b] = [];
                }

                for (var i = 0; i < p.Length; i++)
                {
                    buckets[Math.Min(bins - 1, (int)Math.Floor(p[i] * bins))].Add(i);
                }

                for (var b = 0; b < bins; b++)
                {
                    if (buckets[b].Count > 0)
                    {
                        rows.Add(Bin(buckets[b].ToArray(), p, y, b / (double)bins, (b + 1) / (double)bins,
                            confidenceLevel));
                    }
                }
            }
            else
            {
                foreach (var group in CalibrationMath.EqualCountGroups(p, bins))
                {
                    rows.Add(Bin(group, p, y, group.Min(i => p[i]), group.Max(i => p[i]), confidenceLevel));
                }
            }

            var total = p.Length;
            var ece = rows.Sum(r => r.Count / (double)total * Math.Abs(r.Gap));
            double slope;
            double intercept;

            try
            {
                var fit = PlattCalibration.Fit(p, y, null, CalibrationInput.LogitOfBoundedScore);
                slope = fit.Slope;
                intercept = fit.Intercept;
            }
            catch (InvalidOperationException)
            {
                slope = double.NaN;
                intercept = double.NaN;
            }

            return new CalibrationCurveResult
            {
                Bins = rows.ToArray(),
                Total = total,
                MeanPredicted = p.Average(),
                ObservedRate = y.Average(),
                ExpectedCalibrationError = ece,
                MaximumCalibrationError = rows.Max(r => Math.Abs(r.Gap)),
                BrierScore = BinaryMetrics.BrierScore(p, y),
                LogLoss = BinaryMetrics.LogLoss(p, y),
                CalibrationSlope = slope,
                CalibrationIntercept = intercept,
                SkippedRows = skipped
            };
        }

        [AdvancedAnalyticsTool("CalculateBrierScoreDecomposition")]
        [Description(
            "Splits the Brier score (mean squared error of predicted probabilities) into Murphy's components using probability bins: Reliability (calibration error, lower is better), Resolution (how much the bins separate events from non-events, higher is better) and Uncertainty (the base-rate variance, fixed by the data), so that Brier equals Reliability minus Resolution plus Uncertainty plus WithinBinTerm, a usually small correction (within-bin forecast variance minus twice the within-bin covariance of forecast and outcome) that is zero when every probability in a bin is identical. " +
            "BrierSkillScore is one minus the Brier score over the Brier score of always predicting the base rate: 0 means no better than the base rate, 1 is perfect, negative is worse. " +
            "Use it to tell whether a model's error is caused by poor calibration (large Reliability) or poor discrimination (small Resolution). Probabilities must lie between 0 and 1; NaN rows are dropped.")]
        public BrierDecompositionResult CalculateBrierScoreDecomposition(
            [Description("Predicted probability of the event for each case, between 0 and 1; NaN rows are dropped.")]
            double[] probabilities,
            [Description("True outcome per case in the same order: 1 for the event, 0 otherwise.")]
            int[] labels,
            [Description("Number of equal-width probability bins, from 1 to 1000. Default 10.")]
            int bins = 10)
        {
            CalibrationMath.Clean(probabilities, labels, out var p, out var y, out var skipped);

            if (bins < 1 || bins > MaximumBins)
            {
                throw new ArgumentException($"bins must be between 1 and {MaximumBins}.", nameof(bins));
            }

            var n = p.Length;
            var baseRate = y.Average();
            var buckets = new List<int>[bins];

            for (var b = 0; b < bins; b++)
            {
                buckets[b] = [];
            }

            for (var i = 0; i < n; i++)
            {
                buckets[Math.Min(bins - 1, (int)Math.Floor(p[i] * bins))].Add(i);
            }

            double reliability = 0;
            double resolution = 0;
            double within = 0;

            foreach (var bucket in buckets.Where(b => b.Count > 0))
            {
                var meanP = bucket.Average(i => p[i]);
                var meanY = bucket.Average(i => (double)y[i]);
                reliability += bucket.Count * (meanP - meanY) * (meanP - meanY);
                resolution += bucket.Count * (meanY - baseRate) * (meanY - baseRate);

                within += bucket.Sum(i => (p[i] - meanP) * (p[i] - meanP)) -
                          2d * bucket.Sum(i => (p[i] - meanP) * (y[i] - meanY));
            }

            var brier = BinaryMetrics.BrierScore(p, y);
            var uncertainty = baseRate * (1d - baseRate);

            return new BrierDecompositionResult
            {
                BrierScore = brier,
                Reliability = reliability / n,
                Resolution = resolution / n,
                Uncertainty = uncertainty,
                WithinBinTerm = within / n,
                BrierSkillScore = uncertainty > 0d ? 1d - brier / uncertainty : double.NaN,
                BaseRate = baseRate,
                Bins = bins,
                Total = n,
                SkippedRows = skipped
            };
        }

        [AdvancedAnalyticsTool("CalculateProbabilityScoringRules")]
        [Description(
            "Scores predicted probabilities against a 0/1 outcome with the proper scoring rules: Brier score (mean squared error, lower is better) and log loss (mean negative log likelihood, lower is better, punishes confident wrong predictions heavily), each compared with the trivial model that always predicts the base rate. " +
            "BrierSkillScore and LogLossSkillScore are one minus the ratio to that baseline: above 0 the model adds information, 0 is no better than the base rate. CalibrationInTheLarge is the observed event rate minus the mean predicted probability, so positive means the model under-predicts on average. " +
            "Probabilities must lie between 0 and 1 and are clipped at epsilon before the logarithm; NaN rows are dropped.")]
        public ProbabilityScoringRulesResult CalculateProbabilityScoringRules(
            [Description("Predicted probability of the event for each case, between 0 and 1; NaN rows are dropped.")]
            double[] probabilities,
            [Description("True outcome per case in the same order: 1 for the event, 0 otherwise.")]
            int[] labels,
            [Description(
                "Clipping floor for probabilities before taking logarithms, above 0 and below 0.5. Default 1e-15.")]
            double epsilon = 1e-15)
        {
            CalibrationMath.Clean(probabilities, labels, out var p, out var y, out var skipped);

            if (double.IsNaN(epsilon) || epsilon <= 0d || epsilon >= 0.5)
            {
                throw new ArgumentException("epsilon must be above 0 and below 0.5.", nameof(epsilon));
            }

            var baseRate = y.Average();
            var brier = BinaryMetrics.BrierScore(p, y);
            var logLoss = BinaryMetrics.LogLoss(p, y, null, epsilon);
            var brierBase = baseRate * (1d - baseRate);
            var clipped = Math.Min(Math.Max(baseRate, epsilon), 1d - epsilon);
            var logLossBase = -(baseRate * Math.Log(clipped) + (1d - baseRate) * Math.Log(1d - clipped));

            return new ProbabilityScoringRulesResult
            {
                Total = p.Length,
                BaseRate = baseRate,
                MeanPredicted = p.Average(),
                BrierScore = brier,
                BrierScoreOfBaseRate = brierBase,
                BrierSkillScore = brierBase > 0d ? 1d - brier / brierBase : double.NaN,
                LogLoss = logLoss,
                LogLossOfBaseRate = logLossBase,
                LogLossSkillScore = logLossBase > 0d ? 1d - logLoss / logLossBase : double.NaN,
                CalibrationInTheLarge = baseRate - p.Average(),
                SkippedRows = skipped
            };
        }

        [AdvancedAnalyticsTool("TestHosmerLemeshowCalibration")]
        [Description(
            "Runs the Hosmer-Lemeshow goodness-of-fit test of predicted probabilities: cases are sorted by predicted probability and cut into equal-sized groups, and the observed number of events in each group is compared with the number expected from the average predicted probability. " +
            "The chi-square statistic has groups minus 2 degrees of freedom; a small p-value (below 0.05) means the predictions are significantly miscalibrated, while a large one only means no miscalibration was detected, which is weak evidence on small samples and over-sensitive on very large ones. " +
            "Each group's contribution shows where the miscalibration lies. At least 3 groups are needed and each group should have a few expected events. Probabilities must lie between 0 and 1; NaN rows are dropped.")]
        public HosmerLemeshowResult TestHosmerLemeshowCalibration(
            [Description("Predicted probability of the event for each case, between 0 and 1; NaN rows are dropped.")]
            double[] probabilities,
            [Description("True outcome per case in the same order: 1 for the event, 0 otherwise.")]
            int[] labels,
            [Description("Number of equal-count groups, from 3 to 1000. Default 10.")]
            int groups = 10)
        {
            CalibrationMath.Clean(probabilities, labels, out var p, out var y, out var skipped);

            if (groups < 3 || groups > MaximumBins)
            {
                throw new ArgumentException($"groups must be between 3 and {MaximumBins}.", nameof(groups));
            }

            if (groups > p.Length)
            {
                throw new ArgumentException(
                    $"There are only {p.Length} usable rows, fewer than the {groups} groups requested.",
                    nameof(groups));
            }

            var rows = new List<HosmerLemeshowGroup>();
            var chi = 0d;
            var number = 0;

            foreach (var members in CalibrationMath.EqualCountGroups(p, groups))
            {
                number++;
                var n = members.Length;
                var meanP = members.Average(i => p[i]);
                var observed = members.Sum(i => y[i]);
                var expected = n * meanP;
                var variance = n * meanP * (1d - meanP);
                var contribution = variance > 0d ? (observed - expected) * (observed - expected) / variance : 0d;
                chi += contribution;

                rows.Add(new HosmerLemeshowGroup
                {
                    Group = number,
                    Count = n,
                    ExpectedEvents = expected,
                    ObservedEvents = observed,
                    ChiSquareContribution = contribution
                });
            }

            var df = number - 2;
            var pValue = CategoricalLibrary.ChiSquareSurvival(chi, df);

            return new HosmerLemeshowResult
            {
                Groups = rows.ToArray(),
                ChiSquare = chi,
                DegreesOfFreedom = df,
                PValue = pValue,
                Interpretation = pValue < 0.05
                    ? "significant miscalibration detected"
                    : "no significant miscalibration detected",
                SkippedRows = skipped
            };
        }

        [AdvancedAnalyticsTool("FitCalibrationSlopeAndIntercept")]
        [Description(
            "Estimates the calibration slope and intercept of predicted probabilities by recalibrating them with a logistic regression of the outcome on the logit of the predicted probability. " +
            "A slope near 1 and an intercept near 0 mean the probabilities are well calibrated; a slope below 1 means predictions are too extreme (over-fitted, over-confident), above 1 too timid; a non-zero intercept with slope near 1 means the model systematically over- or under-predicts, which is often a base-rate shift. " +
            "CalibrationInTheLarge is the observed event rate minus the mean predicted probability. NaN slope and intercept mean the fit was impossible, for example only one class or identical probabilities. Probabilities must lie between 0 and 1; NaN rows are dropped.")]
        public CalibrationSlopeInterceptResult FitCalibrationSlopeAndIntercept(
            [Description("Predicted probability of the event for each case, between 0 and 1; NaN rows are dropped.")]
            double[] probabilities,
            [Description("True outcome per case in the same order: 1 for the event, 0 otherwise.")]
            int[] labels)
        {
            CalibrationMath.Clean(probabilities, labels, out var p, out var y, out var skipped);
            double slope;
            double intercept;

            try
            {
                var fit = PlattCalibration.Fit(p, y, null, CalibrationInput.LogitOfBoundedScore);
                slope = fit.Slope;
                intercept = fit.Intercept;
            }
            catch (InvalidOperationException)
            {
                slope = double.NaN;
                intercept = double.NaN;
            }

            var meanP = p.Average();
            var observed = y.Average();
            string interpretation;

            if (double.IsNaN(slope))
            {
                interpretation = "the recalibration could not be fitted";
            }
            else if (slope < 0.7)
            {
                interpretation = "predictions are far too extreme (over-confident)";
            }
            else if (slope > 1.3)
            {
                interpretation = "predictions are too timid (under-confident)";
            }
            else if (Math.Abs(intercept) > 0.3)
            {
                interpretation = "slope is acceptable but predictions are systematically biased (check the base rate)";
            }
            else
            {
                interpretation = "well calibrated";
            }

            return new CalibrationSlopeInterceptResult
            {
                Slope = slope,
                Intercept = intercept,
                MeanPredicted = meanP,
                ObservedRate = observed,
                CalibrationInTheLarge = observed - meanP,
                Interpretation = interpretation,
                Total = p.Length,
                SkippedRows = skipped
            };
        }

        [AdvancedAnalyticsTool("CalculateMulticlassLogLoss")]
        [Description(
            "Scores multi-class probability predictions with log loss and the multi-class Brier score. " +
            "Each row holds a probability per class in the order of classNames, and the actual class gives which of them should have been high; log loss is the mean of minus the natural log of the probability given to the actual class (lower is better, 0 is perfect), Brier is the mean squared distance to the one-hot outcome (0 to 2). " +
            "Rows are rescaled to sum to one, so unnormalised scores are accepted, and probabilities are clipped at epsilon. Log loss per actual class shows which classes the model handles worst. Rows containing NaN are dropped.")]
        public MulticlassLogLossResult CalculateMulticlassLogLoss(
            [Description(
                "One array per case with a non-negative probability for each class, columns in the same order as classNames.")]
            double[][] probabilities,
            [Description(
                "Actual class of each case, one of the entries of classNames, in the same order as probabilities.")]
            string[] actual,
            [Description("Class names in probability column order; each must be unique.")]
            string[] classNames,
            [Description(
                "Clipping floor for probabilities before taking logarithms, above 0 and below 0.5. Default 1e-15.")]
            double epsilon = 1e-15)
        {
            CategoricalGuards.Required(probabilities, nameof(probabilities));
            CategoricalGuards.Required(actual, nameof(actual));
            CategoricalGuards.Required(classNames, nameof(classNames));
            CategoricalGuards.SameLength(probabilities.Length, actual.Length, nameof(probabilities), nameof(actual));

            if (classNames.Length < 2 || classNames.Distinct().Count() != classNames.Length)
            {
                throw new ArgumentException("Give at least two unique class names.", nameof(classNames));
            }

            if (double.IsNaN(epsilon) || epsilon <= 0d || epsilon >= 0.5)
            {
                throw new ArgumentException("epsilon must be above 0 and below 0.5.", nameof(epsilon));
            }

            var k = classNames.Length;
            double lossSum = 0;
            double brierSum = 0;
            var correct = 0;
            var used = 0;
            var perClassLoss = new double[k];
            var perClassCount = new int[k];

            for (var i = 0; i < probabilities.Length; i++)
            {
                if (probabilities[i] == null || probabilities[i].Length != k)
                {
                    throw new ArgumentException($"Row {i} of probabilities must have {k} entries, one per class.",
                        nameof(probabilities));
                }

                var index = Array.IndexOf(classNames, actual[i]);

                if (index < 0)
                {
                    throw new ArgumentException($"Actual class '{actual[i]}' in row {i} is not listed in classNames.",
                        nameof(actual));
                }

                if (probabilities[i].Any(double.IsNaN))
                {
                    continue;
                }

                if (probabilities[i].Any(v => v < 0d || double.IsInfinity(v)))
                {
                    throw new ArgumentException($"Row {i} of probabilities has a negative or infinite entry.",
                        nameof(probabilities));
                }

                var sum = probabilities[i].Sum();

                if (sum <= 0d)
                {
                    throw new ArgumentException($"Row {i} of probabilities sums to zero.", nameof(probabilities));
                }

                var normalised = probabilities[i].Select(v => v / sum).ToArray();
                var loss = -Math.Log(Math.Min(Math.Max(normalised[index], epsilon), 1d - epsilon));
                lossSum += loss;
                perClassLoss[index] += loss;
                perClassCount[index]++;

                brierSum += normalised.Select((v, c) => (v - (c == index ? 1d : 0d)) * (v - (c == index ? 1d : 0d)))
                    .Sum();

                if (Array.IndexOf(normalised, normalised.Max()) == index)
                {
                    correct++;
                }

                used++;
            }

            if (used == 0)
            {
                throw new ArgumentException("There are no rows without NaN probabilities.", nameof(probabilities));
            }

            return new MulticlassLogLossResult
            {
                LogLoss = lossSum / used,
                BrierScore = brierSum / used,
                Accuracy = correct / (double)used,
                Classes = classNames,
                LogLossPerActualClass = perClassLoss
                    .Select((v, c) => perClassCount[c] > 0 ? v / perClassCount[c] : double.NaN)
                    .ToArray(),
                SupportPerClass = perClassCount,
                Total = used,
                SkippedRows = probabilities.Length - used
            };
        }

        [AdvancedAnalyticsTool("AdjustProbabilitiesForBaseRate")]
        [Description(
            "Re-bases predicted probabilities from the event rate the model was trained at to a different real-world event rate, by shifting the log odds by the difference between the two base rates (prior shift correction). " +
            "Use it after training on a rebalanced or sampled dataset, or when the event rate in production differs from training, so that probabilities can be read as true likelihoods and compared with a cost-based threshold. " +
            "Ranking and AUC are unchanged; only calibration moves. Returns one adjusted probability per input, NaN staying NaN. Probabilities must lie between 0 and 1 and both base rates strictly between 0 and 1.")]
        public double[] AdjustProbabilitiesForBaseRate(
            [Description("Predicted probabilities of the event, each between 0 and 1; NaN entries stay NaN.")]
            double[] probabilities,
            [Description("The event rate of the data the model was trained on, strictly between 0 and 1.")]
            double trainingBaseRate,
            [Description("The event rate the probabilities should reflect, strictly between 0 and 1.")]
            double targetBaseRate)
        {
            CategoricalGuards.Required(probabilities, nameof(probabilities));

            if (double.IsNaN(trainingBaseRate) || trainingBaseRate <= 0d || trainingBaseRate >= 1d)
            {
                throw new ArgumentException("trainingBaseRate must be strictly between 0 and 1.",
                    nameof(trainingBaseRate));
            }

            if (double.IsNaN(targetBaseRate) || targetBaseRate <= 0d || targetBaseRate >= 1d)
            {
                throw new ArgumentException("targetBaseRate must be strictly between 0 and 1.", nameof(targetBaseRate));
            }

            for (var i = 0; i < probabilities.Length; i++)
            {
                if (!double.IsNaN(probabilities[i]) && (probabilities[i] < 0d || probabilities[i] > 1d))
                {
                    throw new ArgumentException(
                        $"Probability {i} is {probabilities[i]}; each must lie between 0 and 1.",
                        nameof(probabilities));
                }
            }

            return PriorShift.Apply(probabilities, trainingBaseRate, targetBaseRate);
        }

        private static CalibrationCurveBin Bin(int[] members, double[] p, int[] y, double lower, double upper,
            double level)
        {
            var count = members.Length;
            var events = members.Sum(i => y[i]);
            var meanP = members.Average(i => p[i]);
            var interval = CategoricalIntervals.Proportion(events, count, level, ProportionIntervalMethod.Wilson);

            return new CalibrationCurveBin
            {
                Lower = lower,
                Upper = upper,
                Count = count,
                MeanPredicted = meanP,
                ObservedRate = events / (double)count,
                ObservedLower = interval.Lower,
                ObservedUpper = interval.Upper,
                Gap = events / (double)count - meanP
            };
        }
    }
}