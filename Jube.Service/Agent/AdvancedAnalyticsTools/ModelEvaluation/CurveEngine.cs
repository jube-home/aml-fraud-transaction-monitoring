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
using Jube.Analytics.Models;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal static class CurveEngine
    {
        public static bool MoreParametersMeansMoreFlexible(HyperParameter p)
        {
            return p is HyperParameter.MaxDepth
                or HyperParameter.TreeCount or HyperParameter.HiddenUnits or HyperParameter.Epochs
                or HyperParameter.Complexity
                or HyperParameter.KernelWidth or HyperParameter.LearningRate or HyperParameter.FeatureFraction
                or HyperParameter.SampleFraction or HyperParameter.BinCount;
        }

        public static EvaluationCurveResult Validation(EvaluationFamily family, LearnerConfig baseConfig,
            HyperParameter parameter,
            double[] values, double[][] x, int[] y, string[] names, EvaluationOptions options, SelectionMetric metric,
            CancellationToken token)
        {
            var higher = ClassificationMetricsBuilder.HigherIsBetter(metric);
            var points = new List<EvaluationCurvePoint>();

            foreach (var value in values)
            {
                var config = baseConfig.With(parameter, value, "values");
                var scores = SearchEngine.CrossValidate(config, x, y, names, options, metric, token);
                var held = scores.Held.Where(double.IsFinite).ToList();
                var train = scores.Train.Where(double.IsFinite).ToList();

                points.Add(new EvaluationCurvePoint(value, LearnerConfig.Format(value),
                    ModelMath.Finite(ModelMath.Mean(train)),
                    ModelMath.Finite(ModelMath.Mean(held)), ModelMath.Finite(ModelMath.StandardDeviation(train)),
                    ModelMath.Finite(ModelMath.StandardDeviation(held)), 0));
            }

            var warnings = new List<EvaluationWarning>();

            var best = Enumerable.Range(0, points.Count)
                .Where(i => points[i].ValidationScore.HasValue)
                .OrderByDescending(i => SearchEngine.Utility(points[i].ValidationScore!.Value, higher))
                .FirstOrDefault(-1);

            var diagnosis = "No usable points were produced.";

            if (best >= 0)
            {
                var sorted = points.OrderBy(p => p.X).ToList();
                var position = sorted.FindIndex(p => p.X == points[best].X);
                var flexible = MoreParametersMeansMoreFlexible(parameter);
                var atEdge = position == 0 || position == sorted.Count - 1;

                var gap = points[best].TrainScore.HasValue && points[best].ValidationScore.HasValue
                    ? Math.Abs(points[best].TrainScore!.Value - points[best].ValidationScore!.Value)
                    : (double?)null;

                var direction = position == 0
                    ? flexible ? "the least flexible value tried" : "the most flexible value tried"
                    : position == sorted.Count - 1
                        ? flexible ? "the most flexible value tried" : "the least flexible value tried"
                        : "an interior value";

                diagnosis = $"The held-out {metric} is best at {parameter}={points[best].Label}, {direction}. " +
                            (atEdge
                                ? "Because the optimum is at the edge of the range, extend the range to see whether it improves further."
                                : "It falls away on both sides, so the optimum is bracketed: smaller values underfit or overfit on one side and larger on the other.") +
                            (gap.HasValue
                                ? $" At the best value the train and held-out scores differ by {gap.Value:0.####}."
                                : string.Empty);

                if (atEdge)
                {
                    warnings.Add(new EvaluationWarning(EvaluationWarningCodes.EdgeOfGrid,
                        "The best value is at the edge of the range that was tried."));
                }
            }

            return new EvaluationCurveResult
            {
                Family = family.ToString(),
                Metric = metric.ToString(),
                HigherIsBetter = higher,
                Kind = "ValidationCurve",
                Axis = parameter.ToString(),
                Seed = options.Seed,
                Folds = options.Folds,
                Points = points.ToArray(),
                Diagnosis = diagnosis,
                Warnings = warnings.ToArray(),
                Interpretation = diagnosis
            };
        }

        public static EvaluationCurveResult Learning(EvaluationFamily family, LearnerConfig config, double[] fractions,
            double[][] x, int[] y, string[] names, EvaluationOptions options, SelectionMetric metric,
            CancellationToken token)
        {
            var higher = ClassificationMetricsBuilder.HigherIsBetter(metric);
            var folds = KFoldSplit.Stratified(y.Select(v => (double)v).ToArray(), options.Folds, options.Seed);
            var warnings = new List<EvaluationWarning>();
            var points = new List<EvaluationCurvePoint>();

            foreach (var fraction in fractions.OrderBy(f => f))
            {
                var trains = new List<double>();
                var helds = new List<double>();
                var sizes = new List<int>();

                for (var f = 0; f < folds.Length; f++)
                {
                    token.ThrowIfCancellationRequested();
                    var pool = folds[f].Train;
                    var poolLabels = pool.Select(i => (double)y[i]).ToArray();

                    var subset = fraction >= 0.9999
                        ? Enumerable.Range(0, pool.Length).ToArray()
                        : DataSplit.Split(poolLabels, fraction, 0d, 0d, SeededLibrary.Derive(options.Seed, 100 + f))
                            .Train;

                    if (subset.Length < 12)
                    {
                        continue;
                    }

                    var subLabels = subset.Select(i => (double)y[pool[i]]).ToArray();

                    var carve = DataSplit.Split(subLabels, 1d - options.ValidationFraction, options.ValidationFraction,
                        0d,
                        SeededLibrary.Derive(options.Seed, 200 + f));

                    var assigned = new bool[subset.Length];

                    foreach (var i in carve.Train.Concat(carve.Validation))
                    {
                        assigned[i] = true;
                    }

                    var leftovers = Enumerable.Range(0, subset.Length).Where(i => !assigned[i]);

                    var split = new DataSplitIndices(
                        carve.Train.Concat(leftovers).Select(i => pool[subset[i]]).ToArray(),
                        carve.Validation.Select(i => pool[subset[i]]).ToArray(), folds[f].Test);

                    if (split.Train.Count(i => y[i] == 1) == 0 || split.Train.Count(i => y[i] == 0) == 0)
                    {
                        continue;
                    }

                    var run = BinaryEvaluationEngine.RunOne([config], [config.Name], x, y, names, split, options,
                        SeededLibrary.Derive(options.Seed, 31 * (f + 1)), token);

                    sizes.Add(split.Train.Length + split.Validation.Length);
                    var train = ClassificationMetricsBuilder.Score(run.TrainMetrics, metric);
                    var held = ClassificationMetricsBuilder.Score(run.TestMetrics, metric);

                    if (double.IsFinite(train))
                    {
                        trains.Add(train);
                    }

                    if (double.IsFinite(held))
                    {
                        helds.Add(held);
                    }
                }

                if (sizes.Count == 0)
                {
                    warnings.Add(new EvaluationWarning(EvaluationWarningCodes.SmallSample,
                        $"Training fraction {fraction:0.##} left too few rows or a missing class and was skipped."));

                    continue;
                }

                points.Add(new EvaluationCurvePoint(fraction, LearnerConfig.Format(fraction),
                    ModelMath.Finite(ModelMath.Mean(trains)),
                    ModelMath.Finite(ModelMath.Mean(helds)), ModelMath.Finite(ModelMath.StandardDeviation(trains)),
                    ModelMath.Finite(ModelMath.StandardDeviation(helds)), (int)Math.Round(sizes.Average())));
            }

            var diagnosis = DiagnoseLearning(points, higher, metric);

            return new EvaluationCurveResult
            {
                Family = family.ToString(),
                Metric = metric.ToString(),
                HigherIsBetter = higher,
                Kind = "LearningCurve",
                Axis = "TrainingFraction",
                Seed = options.Seed,
                Folds = options.Folds,
                Points = points.ToArray(),
                Diagnosis = diagnosis,
                Warnings = warnings.ToArray(),
                Interpretation = diagnosis
            };
        }

        private static string DiagnoseLearning(List<EvaluationCurvePoint> points, bool higher, SelectionMetric metric)
        {
            var usable = points.Where(p => p.TrainScore.HasValue && p.ValidationScore.HasValue).ToList();

            if (usable.Count < 2)
            {
                return "Too few points to diagnose the learning curve.";
            }

            var last = usable[^1];
            var previous = usable[^2];

            var gap = higher
                ? last.TrainScore!.Value - last.ValidationScore!.Value
                : last.ValidationScore!.Value - last.TrainScore!.Value;
            var improvement = higher
                ? last.ValidationScore!.Value - previous.ValidationScore!.Value
                : previous.ValidationScore!.Value - last.ValidationScore!.Value;

            var scale = Math.Max(Math.Abs(last.ValidationScore!.Value), 0.1);
            var relativeGap = gap / scale;
            var stillImproving = improvement > 0.01 * scale;

            var text =
                $"At {last.TrainRows} training rows the training {metric} is {last.TrainScore:0.###} and the held-out {metric} is {last.ValidationScore:0.###}. ";

            if (relativeGap > 0.1 && stillImproving)
            {
                return text +
                       "There is a large gap and the held-out score is still rising: high variance, so more training data is likely to help.";
            }

            if (relativeGap > 0.1)
            {
                return text +
                       "There is a large gap and the held-out score has flattened: the model overfits, so simplify or regularise it rather than adding data.";
            }

            if (stillImproving)
            {
                return text +
                       "The curves are close but the held-out score is still rising: more data is likely to help a little.";
            }

            var poor = metric == SelectionMetric.RocAuc && last.ValidationScore < 0.7;

            return text + (poor
                ? "The curves have converged at a poor score: high bias, so the model is too simple or the predictors carry little signal; try a more flexible model or better features."
                : "The curves have converged close together: more data will not help; only better features or a different model can improve this.");
        }
    }
}