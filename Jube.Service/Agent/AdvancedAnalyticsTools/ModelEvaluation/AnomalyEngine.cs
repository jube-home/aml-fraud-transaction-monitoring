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
using Accord.Math;
using Jube.Analytics;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Interfaces;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal static class AnomalyEngine
    {
        public static IAnomalyModel Fit(AnomalyAlgorithm algorithm, double[][] x, int trees, int clusters, double nu,
            int subsample, int seed, CancellationToken token)
        {
            switch (algorithm)
            {
                case AnomalyAlgorithm.IsolationForest:
                    return IsolationForestModel.Fit(x, trees, seed, subsample, token);
                case AnomalyAlgorithm.OneClassSupportVector:
                    return OneClassSvmModel.Fit(x, nu, seed, token);
                case AnomalyAlgorithm.KMeansDistance:
                    return KMeansDistanceModel.Fit(x, clusters, seed, token);
                case AnomalyAlgorithm.GaussianMixtureDensity:
                    return GaussianMixtureDensityModel.Fit(x, clusters, seed, token);
                default:
                    return MahalanobisModel.Fit(x);
            }
        }

        public static AnomalyEvaluationResult Evaluate(AnomalyAlgorithm algorithm, double[][] x, string[] names,
            int[]? labels, double? contamination, bool trainOnNormalOnly, double validationFraction,
            double testFraction,
            int seed, int trees, int clusters, int subsample, CancellationToken token)
        {
            var hasLabels = labels != null;
            var doubles = hasLabels ? labels!.Select(l => (double)l).ToArray() : new double[x.Length];
            var trainFraction = 1d - validationFraction - testFraction;
            var split = DataSplit.Split(doubles, trainFraction, validationFraction, testFraction, seed, hasLabels);
            var assigned = new bool[x.Length];

            foreach (var i in split.Train.Concat(split.Validation).Concat(split.Test))
            {
                assigned[i] = true;
            }

            var trainRows = split.Train.Concat(Enumerable.Range(0, x.Length).Where(i => !assigned[i])).ToArray();
            var warnings = new List<EvaluationWarning>();

            if (hasLabels && trainOnNormalOnly)
            {
                trainRows = trainRows.Where(i => labels![i] == 0).ToArray();
            }

            if (trainRows.Length < 10)
            {
                throw new ArgumentException("Fewer than 10 rows are left to train on after the split.");
            }

            var trainX = DataSplit.Take(x, trainRows);
            var validationX = DataSplit.Take(x, split.Validation);
            var testX = DataSplit.Take(x, split.Test);
            var transform = FeatureTransform.Fit(trainX, names, true);
            var trainT = transform.Apply(trainX);
            var validationT = transform.Apply(validationX);
            var testT = transform.Apply(testX);

            double? knownRate = null;

            if (hasLabels)
            {
                var poolRows = split.Train.Concat(split.Validation).ToArray();

                knownRate = poolRows.Length == 0
                    ? null
                    : poolRows.Count(i => labels![i] == 1) / (double)poolRows.Length;
            }

            var nu = Math.Min(0.5, Math.Max(0.01, contamination ?? knownRate ?? 0.05));
            var model = Fit(algorithm, trainT, trees, clusters, nu, subsample, seed, token);

            var trainScores = model.Score(trainT);
            var validationScores = validationT.Length >= 20 ? model.Score(validationT) : trainScores;
            var testScores = model.Score(testT);

            double used;
            string source;
            var estimates = ContaminationEstimator.Estimate(validationScores);
            var estimated = ContaminationEstimator.Recommend(estimates);

            if (contamination.HasValue)
            {
                used = contamination.Value;
                source = "Supplied";
            }
            else if (knownRate.HasValue && knownRate.Value > 0)
            {
                used = Math.Min(0.5, knownRate.Value);
                source = "KnownAnomalyRateInLabels";
            }
            else if (estimated.HasValue)
            {
                used = estimated.Value;
                source = "EstimatedFromScoreDistribution";

                warnings.Add(new EvaluationWarning("ContaminationEstimated",
                    "No contamination rate was supplied, so it was estimated from the score distribution; treat the flagged share as a heuristic and supply a rate if one is known."));
            }
            else
            {
                used = 0.01;
                source = "DefaultOnePercent";

                warnings.Add(new EvaluationWarning("ContaminationDefaulted",
                    "No contamination rate was supplied and the scores show no separable group, so a default of 1 percent was used."));
            }

            var sortedValidation = validationScores.OrderBy(v => v).ToArray();
            var threshold = ModelMath.Quantile(sortedValidation, 1d - used);

            var testFlagged = testScores.Length == 0
                ? 0d
                : testScores.Count(s => s > threshold) / (double)testScores.Length;

            var trainFlagged = trainScores.Count(s => s > threshold) / (double)trainScores.Length;

            if (testScores.Length >= 50 && used > 0 &&
                (testFlagged > 2d * used + 0.01 || testFlagged < used / 2d - 0.005))
            {
                warnings.Add(new EvaluationWarning("FlaggedShareShift",
                    $"{testFlagged:P1} of the test rows are flagged against the expected {used:P1}: the test rows differ in distribution from the training rows, or the contamination rate is wrong."));
            }

            if (transform.ConstantColumns.Length > 0)
            {
                warnings.Add(new EvaluationWarning(EvaluationWarningCodes.ConstantColumns,
                    $"{transform.ConstantColumns.Length} variable(s) are constant in the training split."));
            }

            if (transform.MissingCounts.Any(m => m > 0))
            {
                warnings.Add(new EvaluationWarning(EvaluationWarningCodes.MissingValues,
                    "Missing values were replaced with the training median of each variable."));
            }

            if (testScores.Length < 50)
            {
                warnings.Add(new EvaluationWarning(EvaluationWarningCodes.TinyTestSet,
                    $"The test split has only {testScores.Length} rows; the flagged share and any metrics will be noisy."));
            }

            double? auc = null, average = null, precision = null, recall = null, f1 = null;
            var testLabels = hasLabels ? DataSplit.Take(labels!, split.Test) : [];
            var knownInTest = testLabels.Count(l => l == 1);

            if (hasLabels)
            {
                if (knownInTest > 0 && knownInTest < testLabels.Length)
                {
                    auc = ModelMath.Finite(BinaryMetrics.RocAuc(testScores, testLabels));
                    average = ModelMath.Finite(BinaryMetrics.AveragePrecision(testScores, testLabels));
                    var confusion = BinaryMetrics.Confusion(testScores, testLabels, threshold + double.Epsilon);
                    precision = ModelMath.Finite(confusion.Precision);
                    recall = ModelMath.Finite(confusion.Recall);
                    f1 = ModelMath.Finite(confusion.F1);

                    if (auc < 0.5)
                    {
                        warnings.Add(new EvaluationWarning(EvaluationWarningCodes.AntiPredictive,
                            "The test ROC AUC is below 0.5: the known anomalies score as more normal than normal rows with this detector."));
                    }
                }
                else
                {
                    warnings.Add(new EvaluationWarning("NoKnownAnomaliesInTest",
                        "The test split has no known anomalies (or only anomalies), so detection quality cannot be measured."));
                }
            }
            else
            {
                warnings.Add(new EvaluationWarning("Unsupervised",
                    "No labels were given, so detection quality is not measured; the result only describes the score distribution and the flagged share."));
            }

            var sortedTest = testScores.OrderBy(v => v).ToArray();

            var quantiles = new[] { 0.5, 0.9, 0.95, 0.99, 1d }
                .Select(q => new AnomalyQuantile(q, ModelMath.Quantile(sortedTest, q))).ToArray();

            var top = Enumerable.Range(0, testScores.Length).OrderByDescending(i => testScores[i])
                .Take(ModelEvaluationLimits.MaximumReturnedRows)
                .Select(i => new AnomalyFlaggedRow(split.Test[i], testScores[i])).ToArray();

            var settings = new List<EvaluationSetting>
            {
                new("Algorithm", algorithm.ToString()),
                new("TrainOnNormalRowsOnly", (hasLabels && trainOnNormalOnly).ToString())
            };

            if (algorithm == AnomalyAlgorithm.IsolationForest)
            {
                settings.Add(new EvaluationSetting("TreeCount", trees.ToString(CultureInfo.InvariantCulture)));
                settings.Add(new EvaluationSetting("SubsampleSize", subsample.ToString(CultureInfo.InvariantCulture)));
            }

            if (algorithm is AnomalyAlgorithm.KMeansDistance or AnomalyAlgorithm.GaussianMixtureDensity)
            {
                settings.Add(new EvaluationSetting("Clusters", clusters.ToString(CultureInfo.InvariantCulture)));
            }

            var facts = model.Facts().ToList();
            facts.Add(ModelText.Fact("TrainingRowsUsed", trainRows.Length));

            var parts = new List<string>
            {
                $"{algorithm}: {model.Summary}",
                $"With an expected contamination of {used:P1} ({source}) the score threshold is {F(threshold)}; {testFlagged:P1} of the test rows are flagged."
            };

            if (auc.HasValue)
            {
                parts.Add(
                    $"Against the {knownInTest} known anomalies in the test split the ROC AUC is {F(auc)}, average precision {F(average)}, precision {F(precision)} and recall {F(recall)} at that threshold.");
            }

            if (warnings.Count > 0)
            {
                parts.Add($"Read the {warnings.Count} warning(s) before acting on the flags.");
            }

            return new AnomalyEvaluationResult
            {
                Algorithm = algorithm.ToString(),
                Seed = seed,
                Settings = settings.ToArray(),
                Rows = x.Length,
                Features = names.Length,
                TrainRows = trainRows.Length,
                ValidationRows = split.Validation.Length,
                TestRows = split.Test.Length,
                ContaminationUsed = used,
                ContaminationSource = source,
                Threshold = threshold,
                EstimatedContamination = estimated,
                TestFlaggedShare = testFlagged,
                TrainFlaggedShare = trainFlagged,
                TestScoreQuantiles = quantiles,
                TopTestRows = top,
                HasLabels = hasLabels,
                RocAuc = auc,
                AveragePrecision = average,
                Precision = precision,
                Recall = recall,
                F1 = f1,
                TestKnownAnomalies = knownInTest,
                Facts = facts.ToArray(),
                Warnings = warnings.ToArray(),
                Interpretation = string.Join(" ", parts)
            };
        }

        private static string F(double? value)
        {
            return value.HasValue ? value.Value.ToString("0.###", CultureInfo.InvariantCulture) : "n/a";
        }
    }
}