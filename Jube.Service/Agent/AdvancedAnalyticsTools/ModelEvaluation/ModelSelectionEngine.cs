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
using Jube.Analytics;
using Jube.Analytics.Models;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal sealed record CandidateSet(LearnerConfig[] Configs, string[] Labels, HyperParameter[] Parameters);

    internal sealed record TrialScore(double[] Train, double[] Held, List<BinaryRun> Runs);

    internal static class SearchEngine
    {
        public static CandidateSet Grid(LearnerConfig baseConfig, HyperParameter[] parameters, double[][] values)
        {
            var configs = new List<LearnerConfig> { baseConfig };
            var used = new List<HyperParameter>();

            for (var p = 0; p < parameters.Length; p++)
            {
                used.Add(parameters[p]);
                var next = new List<LearnerConfig>();

                foreach (var config in configs)
                {
                    foreach (var value in values[p])
                    {
                        next.Add(config.With(parameters[p], value, "values"));
                    }
                }

                configs = next;
            }

            return new CandidateSet(configs.ToArray(), configs.Select(c => c.Label(used)).ToArray(), used.ToArray());
        }

        public static CandidateSet Random(LearnerConfig baseConfig, HyperParameter[] parameters, double[] lower,
            double[] upper, bool[] logScale, int trials, int seed)
        {
            var random = new Random(SeededLibrary.Derive(seed, 4242));
            var configs = new List<LearnerConfig>();
            var seen = new HashSet<string>();
            var attempts = 0;

            while (configs.Count < trials && attempts < trials * 20)
            {
                attempts++;
                var config = baseConfig;

                for (var p = 0; p < parameters.Length; p++)
                {
                    var u = random.NextDouble();

                    var value = logScale[p]
                        ? Math.Exp(Math.Log(lower[p]) + u * (Math.Log(upper[p]) - Math.Log(lower[p])))
                        : lower[p] + u * (upper[p] - lower[p]);

                    if (IsWhole(parameters[p]))
                    {
                        value = Math.Round(value);
                        value = Math.Min(upper[p], Math.Max(lower[p], value));
                    }
                    else
                    {
                        value = Math.Round(value, 5);
                    }

                    config = config.With(parameters[p], value, "lowerBounds");
                }

                var label = config.Label(parameters);

                if (seen.Add(label))
                {
                    configs.Add(config);
                }
            }

            return new CandidateSet(configs.ToArray(), configs.Select(c => c.Label(parameters)).ToArray(), parameters);
        }

        public static bool IsWhole(HyperParameter parameter)
        {
            return parameter is HyperParameter.MaxDepth
                or HyperParameter.MinLeafSize or HyperParameter.TreeCount or HyperParameter.NeighbourCount
                or HyperParameter.HiddenUnits or HyperParameter.Epochs or HyperParameter.BinCount;
        }

        public static TrialScore CrossValidate(LearnerConfig config, double[][] x, int[] y, string[] names,
            EvaluationOptions options, SelectionMetric metric, CancellationToken token)
        {
            var plan = BinaryEvaluationEngine.Plan(y, options);
            var runs = new List<BinaryRun>();
            var held = new List<double>();
            var train = new List<double>();

            for (var f = 0; f < plan.Count; f++)
            {
                var seed = options.Folds >= 2 ? SeededLibrary.Derive(options.Seed, 31 * (f + 1)) : options.Seed;

                var run = BinaryEvaluationEngine.RunOne([config], [config.Name], x, y, names, plan[f], options, seed,
                    token);
                runs.Add(run);
                held.Add(ClassificationMetricsBuilder.Score(run.TestMetrics, metric));
                train.Add(ClassificationMetricsBuilder.Score(run.TrainMetrics, metric));
            }

            return new TrialScore(train.ToArray(), held.ToArray(), runs);
        }

        public static double Utility(double score, bool higherIsBetter)
        {
            return higherIsBetter ? score : -score;
        }

        public static HyperParameterSearchResult Run(string family, string method, CandidateSet set, double[][] x,
            int[] y,
            string[] names, EvaluationOptions options, SelectionMetric metric, bool nested, int outerFolds,
            CancellationToken token)
        {
            var higher = ClassificationMetricsBuilder.HigherIsBetter(metric);
            var configs = set.Configs;
            var innerFolds = options.Folds;
            var total = configs.Length * innerFolds + (nested ? outerFolds * (configs.Length * innerFolds + 1) : 0);

            if (total > ModelEvaluationLimits.MaximumTotalFits)
            {
                throw new ArgumentException(
                    $"This search needs {total} model fits, above the limit of {ModelEvaluationLimits.MaximumTotalFits}; use fewer settings, fewer folds or switch off nested evaluation.",
                    nameof(outerFolds));
            }

            var scores = new TrialScore[configs.Length];
            var warnings = new List<EvaluationWarning>();

            for (var k = 0; k < configs.Length; k++)
            {
                token.ThrowIfCancellationRequested();
                scores[k] = CrossValidate(configs[k], x, y, names, options, metric, token);
            }

            var means = scores.Select(s => ModelMath.Mean(s.Held.Where(double.IsFinite).ToList())).ToArray();

            var order = Enumerable.Range(0, configs.Length)
                .OrderByDescending(k => double.IsFinite(means[k]) ? Utility(means[k], higher) : double.NegativeInfinity)
                .ThenBy(k => k).ToArray();

            var rank = new int[configs.Length];

            for (var r = 0; r < order.Length; r++)
            {
                rank[order[r]] = r + 1;
            }

            var best = order[0];

            var trials = Enumerable.Range(0, configs.Length).Select(k => new HyperParameterTrial(
                k + 1,
                set.Parameters.Length == 0
                    ? [new EvaluationSetting("Family", configs[k].Name)]
                    : set.Parameters.Select(p =>
                        new EvaluationSetting(p.ToString(), LearnerConfig.Format(configs[k].ValueOf(p)))).ToArray(),
                ModelMath.Finite(ModelMath.Mean(scores[k].Train.Where(double.IsFinite).ToList())),
                ModelMath.Finite(means[k]),
                ModelMath.Finite(ModelMath.StandardDeviation(scores[k].Held.Where(double.IsFinite).ToList())),
                rank[k],
                k == best)).ToArray();

            foreach (var p in set.Parameters)
            {
                var distinct = configs.Select(c => c.ValueOf(p)).Distinct().OrderBy(v => v).ToArray();

                if (distinct.Length >= 3 &&
                    (configs[best].ValueOf(p) == distinct[0] || configs[best].ValueOf(p) == distinct[^1]))
                {
                    warnings.Add(new EvaluationWarning(EvaluationWarningCodes.EdgeOfGrid,
                        $"The best {p} ({LearnerConfig.Format(configs[best].ValueOf(p))}) is at the edge of the searched range; extend the range in that direction before trusting it."));
                }
            }

            var nestedFolds = new List<NestedFold>();
            double? nestedScore = null;
            double? optimism = null;

            if (nested)
            {
                var outerOptions = options with { Folds = outerFolds };
                var outerPlan = KFoldSplit.Stratified(y.Select(v => (double)v).ToArray(), outerFolds, options.Seed);
                var outer = new List<double>();
                var inner = new List<double>();

                for (var f = 0; f < outerPlan.Length; f++)
                {
                    token.ThrowIfCancellationRequested();
                    var trainIndex = outerPlan[f].Train;
                    var subX = DataSplit.Take(x, trainIndex);
                    var subY = DataSplit.Take(y, trainIndex);

                    var innerScores = configs.Select(c =>
                        ModelMath.Mean(CrossValidate(c, subX, subY, names,
                            options with { Seed = SeededLibrary.Derive(options.Seed, 700 + f) },
                            metric, token).Held.Where(double.IsFinite).ToList())).ToArray();

                    var innerBest = Enumerable.Range(0, configs.Length)
                        .OrderByDescending(k =>
                            double.IsFinite(innerScores[k]) ? Utility(innerScores[k], higher) : double.NegativeInfinity)
                        .ThenBy(k => k).First();

                    var carve = DataSplit.Split(subY.Select(v => (double)v).ToArray(), 1d - options.ValidationFraction,
                        options.ValidationFraction, 0d, SeededLibrary.Derive(options.Seed, 900 + f));

                    var assigned = new bool[trainIndex.Length];

                    foreach (var i in carve.Train.Concat(carve.Validation))
                    {
                        assigned[i] = true;
                    }

                    var leftovers = Enumerable.Range(0, trainIndex.Length).Where(i => !assigned[i]);

                    var split = new DataSplitIndices(carve.Train.Concat(leftovers).Select(i => trainIndex[i]).ToArray(),
                        carve.Validation.Select(i => trainIndex[i]).ToArray(), outerPlan[f].Test);

                    var run = BinaryEvaluationEngine.RunOne([configs[innerBest]], [configs[innerBest].Name], x, y,
                        names,
                        split, outerOptions, SeededLibrary.Derive(options.Seed, 31 * (f + 1)), token);

                    var outerScore = ClassificationMetricsBuilder.Score(run.TestMetrics, metric);

                    nestedFolds.Add(new NestedFold(f + 1, set.Labels[innerBest],
                        ModelMath.Finite(innerScores[innerBest]),
                        ModelMath.Finite(outerScore)));

                    if (double.IsFinite(outerScore))
                    {
                        outer.Add(outerScore);
                    }

                    if (double.IsFinite(innerScores[innerBest]))
                    {
                        inner.Add(innerScores[innerBest]);
                    }
                }

                if (outer.Count > 0 && inner.Count > 0)
                {
                    nestedScore = outer.Average();
                    optimism = higher ? inner.Average() - outer.Average() : outer.Average() - inner.Average();

                    if (optimism > 0.02 * Math.Max(Math.Abs(inner.Average()), 0.1))
                    {
                        warnings.Add(new EvaluationWarning(EvaluationWarningCodes.Optimism,
                            $"Choosing the best setting on the same folds it is scored on flatters it by {optimism:0.####}; the nested score {nestedScore:0.####} is the honest estimate."));
                    }
                }
            }

            var chosenOptions = options with { ImportanceRepeats = 0 };

            var evaluation =
                BinaryEvaluationEngine.Assemble(scores[best].Runs, configs[best], x, y, names, chosenOptions, token);

            var bestMean = ModelMath.Finite(means[best]);

            var text =
                $"{family}: the best setting by {metric} is '{set.Labels[best]}' with a cross-validated {metric} of {(bestMean.HasValue ? bestMean.Value.ToString("0.####", CultureInfo.InvariantCulture) : "n/a")} " +
                $"over {options.Folds} folds ({(higher ? "higher is better" : "lower is better")}). " +
                (nestedScore.HasValue
                    ? $"The nested cross-validated estimate that includes the choosing step is {nestedScore.Value:0.####}, so expect about that on new data rather than the optimistic {bestMean:0.####}."
                    : "Nested evaluation was not run, so the best score is optimistically biased by having been picked as the maximum; treat it as an upper bound.");

            return new HyperParameterSearchResult
            {
                Family = family,
                Metric = metric.ToString(),
                HigherIsBetter = higher,
                Seed = options.Seed,
                Method = method,
                Folds = options.Folds,
                Trials = trials.OrderBy(t => t.Rank).ToArray(),
                BestParameters = trials[best].Parameters,
                BestScore = bestMean,
                Nested = nestedFolds.ToArray(),
                NestedScore = nestedScore,
                SelectionOptimism = optimism,
                BestConfigurationEvaluation = evaluation,
                Warnings = warnings.ToArray(),
                Interpretation = text
            };
        }
    }
}