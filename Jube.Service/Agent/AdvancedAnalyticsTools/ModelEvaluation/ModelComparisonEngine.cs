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
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal static class ModelComparisonEngine
    {
        private static int Complexity(EvaluationFamily family)
        {
            return family switch
            {
                EvaluationFamily.LogisticRegression => 1,
                EvaluationFamily.NaiveBayes => 2,
                EvaluationFamily.TreeAugmentedNaiveBayes => 3,
                EvaluationFamily.DecisionTree => 3,
                EvaluationFamily.LinearSupportVector => 3,
                EvaluationFamily.KNearestNeighbours => 4,
                EvaluationFamily.KernelSupportVector => 5,
                EvaluationFamily.Bagging => 5,
                EvaluationFamily.RandomForest => 6,
                EvaluationFamily.AdaBoost => 6,
                EvaluationFamily.GradientBoosting => 7,
                _ => 7
            };
        }

        private static int Complexity(RegressionFamily family)
        {
            return family switch
            {
                RegressionFamily.LinearRegression => 1,
                RegressionFamily.DecisionTree => 3,
                RegressionFamily.KNearestNeighbours => 4,
                RegressionFamily.KernelRidge => 5,
                RegressionFamily.RandomForest => 6,
                RegressionFamily.GradientBoosting => 7,
                _ => 7
            };
        }

        private static string F(double? value)
        {
            return value.HasValue ? value.Value.ToString("0.####", CultureInfo.InvariantCulture) : "n/a";
        }

        public static ModelComparisonResult Compare(EvaluationFamily[] families, ClassImbalanceHandling imbalance,
            double[][] x, int[] y, string[] names, EvaluationOptions options, SelectionMetric metric,
            CancellationToken token)
        {
            var higher = ClassificationMetricsBuilder.HigherIsBetter(metric);
            var warnings = new List<EvaluationWarning>();
            var results = new List<(EvaluationFamily Family, TrialScore? Scores, string? Failure)>();

            foreach (var family in families)
            {
                token.ThrowIfCancellationRequested();

                if (family == EvaluationFamily.KernelSupportVector &&
                    x.Length > ModelEvaluationLimits.MaximumKernelRows)
                {
                    results.Add((family, null,
                        $"Skipped: kernel support vector machines are limited to {ModelEvaluationLimits.MaximumKernelRows} rows."));

                    continue;
                }

                var config = ConfigFactory.Build(family, null, null, imbalance, TreeAlgorithm.C45, SvmKernel.Gaussian,
                    NaiveBayesKind.Gaussian, "families");

                try
                {
                    results.Add((family, SearchEngine.CrossValidate(config, x, y, names, options, metric, token),
                        null));
                }
                catch (ArgumentException exception)
                {
                    results.Add((family, null, "Failed: " + exception.Message));
                }
                catch (InvalidOperationException exception)
                {
                    results.Add((family, null, "Failed: " + exception.Message));
                }
            }

            var scored = results.Where(r => r.Scores != null && r.Scores.Held.Any(double.IsFinite)).ToList();

            if (scored.Count == 0)
            {
                throw new ArgumentException("None of the requested families could be fitted on these data.",
                    "families");
            }

            double Utility(double v)
            {
                return SearchEngine.Utility(v, higher);
            }

            var means = scored.ToDictionary(r => r.Family, r => r.Scores!.Held.Where(double.IsFinite).Average());
            var best = scored.OrderByDescending(r => Utility(means[r.Family])).ThenBy(r => (int)r.Family).First();
            var bestUtilities = best.Scores!.Held.Select(Utility).ToArray();
            var folds = options.Folds;
            var rows = new List<ComparedModelRow>();

            foreach (var entry in scored.OrderByDescending(r => Utility(means[r.Family])).ThenBy(r => (int)r.Family))
            {
                var held = entry.Scores!.Held;
                var train = entry.Scores.Train.Where(double.IsFinite).ToList();

                var differences = Enumerable.Range(0, held.Length)
                    .Where(i => double.IsFinite(held[i]) && double.IsFinite(bestUtilities[i]))
                    .Select(i => bestUtilities[i] - Utility(held[i])).ToArray();

                var meanDifference = differences.Length == 0 ? double.NaN : differences.Average();

                var standardError = differences.Length > 1
                    ? ModelMath.StandardDeviation(differences) / Math.Sqrt(differences.Length)
                    : double.NaN;

                var same = entry.Family == best.Family || (double.IsFinite(meanDifference) &&
                                                           (double.IsFinite(standardError)
                                                               ? meanDifference <= standardError
                                                               : meanDifference <= 0));

                var pooled = ClassificationMetricsBuilder.Pool(entry.Scores.Runs);
                var trainMean = train.Count == 0 ? double.NaN : train.Average();

                var gap = double.IsFinite(trainMean)
                    ? higher ? trainMean - means[entry.Family] : means[entry.Family] - trainMean
                    : double.NaN;

                rows.Add(new ComparedModelRow(0, entry.Family.ToString(), ModelMath.Finite(means[entry.Family]),
                    ModelMath.Finite(ModelMath.StandardDeviation(held.Where(double.IsFinite).ToList())),
                    ModelMath.Finite(trainMean), ModelMath.Finite(gap), ModelMath.Finite(meanDifference), same,
                    pooled?.RocAuc, pooled?.AveragePrecision, pooled?.LogLoss, pooled?.BrierScore, pooled?.F1,
                    gap > ModelEvaluationLimits.OverfitGap && metric == SelectionMetric.RocAuc
                        ? "Large train to held-out gap: likely overfitting."
                        : null));
            }

            foreach (var failure in results.Where(r => r.Scores == null || !r.Scores.Held.Any(double.IsFinite)))
            {
                rows.Add(new ComparedModelRow(0, failure.Family.ToString(), null, null, null, null, null, false, null,
                    null,
                    null, null, null, failure.Failure ?? "No finite score was produced."));
            }

            var ranked = rows.Select((r, i) => r with { Rank = i + 1 }).ToArray();
            var indistinguishable = ranked.Where(r => r.IndistinguishableFromBest && r.MeanScore.HasValue).ToArray();

            var recommended = indistinguishable
                .OrderBy(r => Complexity(Enum.Parse<EvaluationFamily>(r.Model)))
                .ThenBy(r => r.Rank).First();

            var top = ranked[0];

            var text =
                $"Every family was scored on the same {folds} stratified folds by {metric} ({(higher ? "higher" : "lower")} is better). " +
                $"{top.Model} scores best on average ({F(top.MeanScore)} with standard deviation {F(top.StandardDeviation)} across folds).";

            if (indistinguishable.Length > 1)
            {
                var others = indistinguishable.Where(r => r.Model != top.Model).Select(r => r.Model).ToArray();

                text +=
                    $" {string.Join(", ", others)} cannot be told apart from it: the paired fold-by-fold difference is within one standard error, so the ranking between them is noise.";

                if (recommended.Model != top.Model)
                {
                    text +=
                        $" {recommended.Model} is recommended because it is the simplest of the statistically equivalent choices, which is easier to explain, cheaper to run and less likely to drift.";
                }
                else
                {
                    text +=
                        $" {recommended.Model} is recommended: it is both the best and the simplest of the equivalent choices.";
                }
            }
            else
            {
                text += $" No other family is within one standard error, so {top.Model} is recommended.";
            }

            if (recommended.OverfitGap is > ModelEvaluationLimits.OverfitGap && metric == SelectionMetric.RocAuc)
            {
                text +=
                    " Its training score is much higher than its held-out score, so treat the held-out figure as the real one and consider more regularisation.";
            }

            if (metric == SelectionMetric.RocAuc && top.MeanScore is < 0.55)
            {
                warnings.Add(new EvaluationWarning(EvaluationWarningCodes.NoSignal,
                    "No family beats chance by a useful margin: the predictors carry little signal for this outcome."));
            }

            if (ranked.Any(r => r.MeanScore == null))
            {
                warnings.Add(new EvaluationWarning("FamilyNotScored",
                    "One or more families could not be scored; see the Note column."));
            }

            if (x.Length < 300 || y.Count(v => v == 1) < 40)
            {
                warnings.Add(new EvaluationWarning(EvaluationWarningCodes.SmallSample,
                    "The sample or the number of positives is small, so fold-to-fold noise is large and the ranking may not reproduce."));
            }

            var positives = y.Count(v => v == 1);

            var ratio = Math.Max(positives, y.Length - positives) /
                        (double)Math.Max(1, Math.Min(positives, y.Length - positives));

            if (ratio >= ModelEvaluationLimits.SevereImbalanceRatio && imbalance == ClassImbalanceHandling.None)
            {
                warnings.Add(new EvaluationWarning(EvaluationWarningCodes.SevereImbalance,
                    $"The classes are imbalanced {ratio:0}:1; consider ranking by AveragePrecision and using BalancedWeights."));
            }

            return new ModelComparisonResult
            {
                Metric = metric.ToString(),
                HigherIsBetter = higher,
                Seed = options.Seed,
                Scheme = BinaryEvaluationEngine.SchemeName(options),
                Rows = x.Length,
                Folds = folds,
                Ranking = ranked,
                Recommended = recommended.Model,
                Recommendation = text,
                Warnings = warnings.ToArray()
            };
        }

        public static RegressorComparisonResult CompareRegressors(RegressionFamily[] families, double[][] x, double[] y,
            string[] names, EvaluationOptions options, RegressionSelectionMetric metric, CancellationToken token)
        {
            var higher = RegressionMetricsBuilder.HigherIsBetter(metric);
            var warnings = new List<EvaluationWarning>();
            var results = new List<(RegressionFamily Family, List<RegressionRun>? Runs, string? Failure)>();

            foreach (var family in families)
            {
                token.ThrowIfCancellationRequested();

                if (family == RegressionFamily.KernelRidge && x.Length > ModelEvaluationLimits.MaximumKernelRows / 2)
                {
                    results.Add((family, null,
                        $"Skipped: kernel ridge regression is limited to {ModelEvaluationLimits.MaximumKernelRows / 2} rows."));

                    continue;
                }

                var config =
                    ConfigFactory.BuildRegressor(family, null, null, LinearPenalty.None, SvmKernel.Gaussian,
                        "families");

                var plan = RegressionEvaluationEngine.Plan(y, options);
                var runs = new List<RegressionRun>();

                try
                {
                    for (var f = 0; f < plan.Count; f++)
                    {
                        runs.Add(RegressionEvaluationEngine.RunOne([config], [config.Name], x, y, names, plan[f],
                            options,
                            SeededLibrary.Derive(options.Seed, 31 * (f + 1)), token));
                    }

                    results.Add((family, runs, null));
                }
                catch (ArgumentException exception)
                {
                    results.Add((family, null, "Failed: " + exception.Message));
                }
            }

            var scored = results.Where(r => r.Runs != null).ToList();

            if (scored.Count == 0)
            {
                throw new ArgumentException("None of the requested families could be fitted on these data.",
                    "families");
            }

            double Held(RegressionRun run)
            {
                return RegressionMetricsBuilder.Score(run.TestMetrics, metric);
            }

            double Utility(double v)
            {
                return higher ? v : -v;
            }

            var means = scored.ToDictionary(r => r.Family,
                r => r.Runs!.Select(Held).Where(double.IsFinite).DefaultIfEmpty(double.NaN).Average());

            var ordered = scored.OrderByDescending(r =>
                    double.IsFinite(means[r.Family]) ? Utility(means[r.Family]) : double.NegativeInfinity)
                .ThenBy(r => (int)r.Family).ToList();

            var best = ordered[0];
            var bestUtilities = best.Runs!.Select(r => Utility(Held(r))).ToArray();
            var rows = new List<ComparedRegressorRow>();

            foreach (var entry in ordered)
            {
                var held = entry.Runs!.Select(Held).ToArray();

                var train = entry.Runs!.Select(r => RegressionMetricsBuilder.Score(r.TrainMetrics, metric))
                    .Where(double.IsFinite).ToList();

                var differences = Enumerable.Range(0, held.Length)
                    .Where(i => double.IsFinite(held[i]) && double.IsFinite(bestUtilities[i]))
                    .Select(i => bestUtilities[i] - Utility(held[i])).ToArray();

                var meanDifference = differences.Length == 0 ? double.NaN : differences.Average();

                var standardError = differences.Length > 1
                    ? ModelMath.StandardDeviation(differences) / Math.Sqrt(differences.Length)
                    : double.NaN;

                var same = entry.Family == best.Family || (double.IsFinite(meanDifference) &&
                                                           (double.IsFinite(standardError)
                                                               ? meanDifference <= standardError
                                                               : meanDifference <= 0));

                var pooled = RegressionMetricsBuilder.Build(entry.Runs!.SelectMany(r => r.TestTargets).ToArray(),
                    entry.Runs!.SelectMany(r => r.TestPredictions).ToArray());

                var trainMean = train.Count == 0 ? double.NaN : train.Average();

                var gap = double.IsFinite(trainMean)
                    ? higher ? trainMean - means[entry.Family] : means[entry.Family] - trainMean
                    : double.NaN;

                rows.Add(new ComparedRegressorRow(0, entry.Family.ToString(), ModelMath.Finite(means[entry.Family]),
                    ModelMath.Finite(ModelMath.StandardDeviation(held.Where(double.IsFinite).ToList())),
                    ModelMath.Finite(trainMean), ModelMath.Finite(gap), ModelMath.Finite(meanDifference), same,
                    pooled?.RSquared, pooled?.MeanAbsoluteError, pooled?.RootMeanSquaredError,
                    metric == RegressionSelectionMetric.RSquared && gap > 0.2
                        ? "Large train to held-out gap: likely overfitting."
                        : null));
            }

            foreach (var failure in results.Where(r => r.Runs == null))
            {
                rows.Add(new ComparedRegressorRow(0, failure.Family.ToString(), null, null, null, null, null, false,
                    null,
                    null, null, failure.Failure));
            }

            var ranked = rows.Select((r, i) => r with { Rank = i + 1 }).ToArray();
            var indistinguishable = ranked.Where(r => r.IndistinguishableFromBest && r.MeanScore.HasValue).ToArray();

            var recommended = indistinguishable.OrderBy(r => Complexity(Enum.Parse<RegressionFamily>(r.Model)))
                .ThenBy(r => r.Rank).First();

            var top = ranked[0];
            var baseline = RegressionMetricsBuilder.Build(y, y.Select(_ => y.Average()).ToArray());
            var baselineScore = RegressionMetricsBuilder.Score(baseline, metric);

            var text =
                $"Every family was scored on the same {options.Folds} folds by {metric} ({(higher ? "higher" : "lower")} is better). " +
                $"{top.Model} scores best on average ({F(top.MeanScore)} with standard deviation {F(top.StandardDeviation)}); always predicting the mean would score {F(baselineScore)}.";

            if (indistinguishable.Length > 1)
            {
                var others = indistinguishable.Where(r => r.Model != top.Model).Select(r => r.Model).ToArray();

                text +=
                    $" {string.Join(", ", others)} cannot be told apart from it within one standard error of the paired fold differences. {recommended.Model} is recommended as the simplest of the equivalent choices.";
            }
            else
            {
                text += $" No other family is within one standard error, so {top.Model} is recommended.";
            }

            if (metric == RegressionSelectionMetric.RSquared && top.MeanScore is < 0.05)
            {
                warnings.Add(new EvaluationWarning(EvaluationWarningCodes.NoSignal,
                    "No family explains a useful share of the target: the predictors carry little signal."));
            }

            if (ranked.Any(r => r.MeanScore == null))
            {
                warnings.Add(new EvaluationWarning("FamilyNotScored",
                    "One or more families could not be scored; see the Note column."));
            }

            return new RegressorComparisonResult
            {
                Metric = metric.ToString(),
                HigherIsBetter = higher,
                Seed = options.Seed,
                Scheme = $"CrossValidation({options.Folds} folds)",
                Rows = x.Length,
                Folds = options.Folds,
                BaselineScore = baselineScore,
                Ranking = ranked,
                Recommended = recommended.Model,
                Recommendation = text,
                Warnings = warnings.ToArray()
            };
        }
    }
}