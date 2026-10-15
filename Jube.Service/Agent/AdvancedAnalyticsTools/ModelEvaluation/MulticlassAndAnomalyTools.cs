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
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    [AdvancedAnalyticsToolGroup("ModelEvaluationTools")]
    public class AnomalyEvaluationTools
    {
        [AdvancedAnalyticsTool("EvaluateAnomalyDetector")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Fits an unsupervised anomaly detector and evaluates it on held-out rows, returning the score threshold implied by the expected contamination (the share of anomalies), how much of the training and test rows it flags, the test score quantiles, the top-scoring test rows with their original row numbers, an estimate of the contamination from the score distribution and, when known anomaly labels are supplied, ROC AUC, average precision, precision, recall and F1 at that threshold. " +
            "Algorithms: IsolationForest (random splits isolate outliers; robust default), OneClassSupportVector (boundary around normal rows in a Gaussian kernel space), KMeansDistance (distance to the nearest cluster centre), GaussianMixtureDensity (negative log density) and MahalanobisDistance (covariance-aware distance from the mean). A higher score is always more anomalous. " +
            "Contamination is taken from the value you give, else from the labels, else estimated from the scores; when the flagged share on the test rows differs a lot from the expected share a FlaggedShareShift warning appears. Labels are optional: 1 marks a known anomaly and 0 a normal row; with trainOnNormalRowsOnly the detector is fitted on rows labelled 0 only, which is the right setup when the normal population is known. " +
            "Method: seeded, stratified (by label when given) split into train, validation and test rows; scaling is learned from the training rows, the threshold from the validation scores, every metric from the test rows. " +
            "Bounds: at most 20000 rows and 200 columns; OneClassSupportVector limited to 4000 rows, up to 500 trees and 32 clusters. Nothing is saved.")]
        public AnomalyEvaluationResult EvaluateAnomalyDetector(
            [Description(
                "Rows to analyse: one array of numbers per case, all the same length and in the order of the variable names. NaN marks a missing value.")]
            double[][] inputs,
            [Description("The name of each variable, unique and in column order.")]
            string[] variableNames,
            [Description(
                "IsolationForest, OneClassSupportVector, KMeansDistance, GaussianMixtureDensity or MahalanobisDistance.")]
            AnomalyAlgorithm algorithm = AnomalyAlgorithm.IsolationForest,
            [Description(
                "Optional label for each row, in row order: 1 for a known anomaly and 0 for a normal row. Empty runs unsupervised with no quality metrics.")]
            int[]? knownAnomalyLabels = null,
            [Description(
                "The expected share of anomalies, above 0 and at most 0.5. Empty uses the labelled rate, else an estimate from the score distribution.")]
            double? contamination = null,
            [Description("When labels are given, fit the detector on the rows labelled 0 only. Default false.")]
            bool trainOnNormalRowsOnly = false,
            [Description("Number of trees for IsolationForest, 10 to 500. Default 100.")]
            int treeCount = 100,
            [Description(
                "Number of clusters or mixture components for KMeansDistance and GaussianMixtureDensity, 1 to 32. Default 5.")]
            int clusters = 5,
            [Description("Subsample size per isolation tree, 16 to 2048. Default 256.")]
            int subsampleSize = 256,
            [Description("Share of rows held back to set the score threshold, 0.05 to 0.5. Default 0.2.")]
            double validationFraction = 0.2,
            [Description("Share of rows held back to measure the detector, above 0 and at most 0.5. Default 0.3.")]
            double testFraction = 0.3,
            [Description("Seed that makes the split and every random choice reproducible.")]
            int seed = 1,
            CancellationToken cancellationToken = default)
        {
            ModelEvaluationGuard.Features(inputs, variableNames,
                algorithm == AnomalyAlgorithm.OneClassSupportVector
                    ? ModelEvaluationLimits.MaximumKernelRows
                    : ModelEvaluationLimits.MaximumRows);

            if (knownAnomalyLabels != null)
            {
                if (knownAnomalyLabels.Length != inputs.Length || knownAnomalyLabels.Any(l => l != 0 && l != 1))
                {
                    throw new ArgumentException("Labels must be one 0 or 1 per row.", nameof(knownAnomalyLabels));
                }

                if (knownAnomalyLabels.Count(l => l == 1) < 3)
                {
                    throw new ArgumentException(
                        "At least 3 known anomalies are needed to measure detection quality; otherwise leave the labels empty.",
                        nameof(knownAnomalyLabels));
                }
            }

            if (contamination.HasValue)
            {
                ModelEvaluationGuard.DoubleRange(contamination.Value, 1e-4, 0.5, nameof(contamination),
                    "The contamination");
            }

            if (validationFraction < 0.05 || validationFraction > 0.5 || !double.IsFinite(validationFraction))
            {
                throw new ArgumentException("The validation fraction must be between 0.05 and 0.5.",
                    nameof(validationFraction));
            }

            if (!double.IsFinite(testFraction) || testFraction <= 0 || testFraction > 0.5 ||
                validationFraction + testFraction > 0.8)
            {
                throw new ArgumentException(
                    "The test fraction must be above 0 and at most 0.5, with the validation fraction at most 0.8 in total.",
                    nameof(testFraction));
            }

            ModelEvaluationGuard.IntRange(treeCount, 10, ModelEvaluationLimits.MaximumTrees, nameof(treeCount),
                "The tree count");
            ModelEvaluationGuard.IntRange(clusters, 1, ModelEvaluationLimits.MaximumClusters, nameof(clusters),
                "The cluster count");

            ModelEvaluationGuard.IntRange(subsampleSize, 16, 2048, nameof(subsampleSize), "The subsample size");

            return AnomalyEngine.Evaluate(algorithm, inputs, variableNames, knownAnomalyLabels, contamination,
                trainOnNormalRowsOnly, validationFraction, testFraction, seed, treeCount, clusters, subsampleSize,
                cancellationToken);
        }

        [AdvancedAnalyticsTool("EstimateAnomalyContamination")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [Description(
            "Estimates what share of a set of anomaly scores is anomalous, without any labels, by four independent heuristics: the share above the modified z score 3.5 (median and MAD), the share above the upper Tukey fence (Q3 plus 1.5 interquartile ranges), the largest break in the top 30 percent of the sorted scores, and the weight of the upper component of a two component Gaussian mixture fitted to the (log) scores. " +
            "It returns each estimate with a note explaining it or why it is unavailable, a recommended share (the median of the estimates that agree, kept between 0.1 percent and 30 percent) and the score threshold that share implies. " +
            "Treat every estimate as a heuristic: they agree when there is a clearly separated anomalous group and disagree, or return nothing, when scores form one smooth continuum, in which case choose the contamination from business capacity (how many alerts can be worked) instead. " +
            "Bounds: at least 20 finite scores, at most 20000. Scores may come from EvaluateAnomalyDetector or from any other detector.")]
        public ContaminationEstimationResult EstimateAnomalyContamination(
            [Description("The anomaly score of each row, one number per row. NaN scores are ignored.")]
            double[] scores,
            [Description(
                "True when a higher score means more anomalous (the usual case); false when a lower score means more anomalous, such as a likelihood.")]
            bool higherIsMoreAnomalous = true)
        {
            if (scores == null || scores.Length == 0)
            {
                throw new ArgumentException("Give at least one score.", nameof(scores));
            }

            if (scores.Length > ModelEvaluationLimits.MaximumRows)
            {
                throw new ArgumentException($"At most {ModelEvaluationLimits.MaximumRows} scores are supported.",
                    nameof(scores));
            }

            if (scores.Any(double.IsInfinity))
            {
                throw new ArgumentException("Scores must be finite or NaN.", nameof(scores));
            }

            var oriented = higherIsMoreAnomalous ? scores : scores.Select(s => -s).ToArray();
            var finite = oriented.Where(double.IsFinite).ToArray();
            var estimates = ContaminationEstimator.Estimate(oriented);
            var recommended = ContaminationEstimator.Recommend(estimates);
            double? threshold = null;

            if (recommended.HasValue && finite.Length > 0)
            {
                var sorted = finite.OrderBy(v => v).ToArray();
                threshold = ModelMath.Quantile(sorted, 1d - recommended.Value);

                if (!higherIsMoreAnomalous)
                {
                    threshold = -threshold;
                }
            }

            var warnings = new List<EvaluationWarning>();

            if (finite.Length < 20)
            {
                warnings.Add(new EvaluationWarning(EvaluationWarningCodes.SmallSample,
                    "Fewer than 20 finite scores; no estimate is reliable."));
            }

            var available = estimates.Where(e => e.Share.HasValue).ToArray();

            if (available.Length >= 2 &&
                available.Max(e => e.Share!.Value) > 4d * Math.Max(available.Min(e => e.Share!.Value), 1e-4))
            {
                warnings.Add(new EvaluationWarning("EstimatesDisagree",
                    "The estimates differ by more than a factor of four, so the scores probably form a continuum without a clear anomalous group."));
            }

            if (available.Length == 0)
            {
                warnings.Add(new EvaluationWarning("NoSeparableGroup",
                    "None of the methods found a separable anomalous group; choose the contamination from alert capacity instead."));
            }

            var text = recommended.HasValue
                ? $"Of {finite.Length} scores, about {recommended.Value:P1} look anomalous (median of {available.Length} estimate(s)); flagging scores {(higherIsMoreAnomalous ? "above" : "below")} {threshold:0.####} would flag that share. Check the individual estimates: agreement makes this trustworthy."
                : "No estimate could be made from these scores; the score distribution shows no separable anomalous group.";

            return new ContaminationEstimationResult
            {
                Rows = finite.Length,
                Estimates = estimates,
                RecommendedShare = recommended,
                RecommendedThreshold = threshold,
                Warnings = warnings.ToArray(),
                Interpretation = text
            };
        }
    }
}