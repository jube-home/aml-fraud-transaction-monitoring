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
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Interfaces;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal sealed record MulticlassConfig
    {
        public MulticlassAlgorithm Algorithm { get; init; } = MulticlassAlgorithm.MultinomialLogistic;
        public int MaxDepth { get; init; } = 6;
        public int MinLeaf { get; init; } = 5;
        public int Trees { get; init; } = 100;
        public double FeatureFraction { get; init; }
        public int K { get; init; } = 5;
        public int[] Hidden { get; init; } = [8];
        public int Epochs { get; init; } = 200;
        public double VarianceSmoothing { get; init; } = 1e-6;
        public ClassImbalanceHandling Imbalance { get; init; } = ClassImbalanceHandling.None;

        public bool NeedsScaling => Algorithm is MulticlassAlgorithm.MultinomialLogistic
            or MulticlassAlgorithm.KNearestNeighbours or MulticlassAlgorithm.NeuralNetwork
            or MulticlassAlgorithm.NaiveBayes;

        public bool SupportsWeights => Algorithm is MulticlassAlgorithm.DecisionTree or MulticlassAlgorithm.RandomForest
            or MulticlassAlgorithm.NaiveBayes;

        public EvaluationSetting[] Settings()
        {
            var list = new List<EvaluationSetting> { new("Algorithm", Algorithm.ToString()) };

            string I(int v)
            {
                return v.ToString(CultureInfo.InvariantCulture);
            }

            switch (Algorithm)
            {
                case MulticlassAlgorithm.DecisionTree:
                    list.Add(new EvaluationSetting("MaxDepth", I(MaxDepth)));
                    list.Add(new EvaluationSetting("MinLeafSize", I(MinLeaf)));
                    break;
                case MulticlassAlgorithm.RandomForest:
                    list.Add(new EvaluationSetting("TreeCount", I(Trees)));
                    list.Add(new EvaluationSetting("MaxDepth", I(MaxDepth)));
                    list.Add(new EvaluationSetting("MinLeafSize", I(MinLeaf)));

                    list.Add(new EvaluationSetting("FeatureFraction",
                        FeatureFraction > 0 ? LearnerConfig.Format(FeatureFraction) : "sqrt"));

                    break;
                case MulticlassAlgorithm.KNearestNeighbours:
                    list.Add(new EvaluationSetting("NeighbourCount", I(K)));
                    break;
                case MulticlassAlgorithm.NeuralNetwork:
                    list.Add(new EvaluationSetting("HiddenLayers", string.Join("-", Hidden)));
                    list.Add(new EvaluationSetting("Epochs", I(Epochs)));
                    break;
            }

            list.Add(new EvaluationSetting("ClassImbalance", Imbalance.ToString()));

            return list.ToArray();
        }
    }

    internal static class MulticlassEngine
    {
        public static int[] Encode(int[] labels, out int[] classLabels)
        {
            classLabels = labels.Distinct().OrderBy(v => v).ToArray();
            var map = classLabels.Select((v, i) => (v, i)).ToDictionary(t => t.v, t => t.i);

            return labels.Select(v => map[v]).ToArray();
        }

        public static (double[][] X, int[] Y, double[]? W, string? Note) Balance(MulticlassConfig c, double[][] x,
            int[] y,
            int classes, int seed)
        {
            if (c.Imbalance == ClassImbalanceHandling.None)
            {
                return (x, y, null, null);
            }

            var counts = new int[classes];

            foreach (var v in y)
            {
                counts[v]++;
            }

            if (c.Imbalance == ClassImbalanceHandling.BalancedWeights && c.SupportsWeights)
            {
                return (x, y, y.Select(v => y.Length / ((double)classes * counts[v])).ToArray(), null);
            }

            var random = new Random(SeededLibrary.Derive(seed, 771));
            var rows = new List<int>();

            if (c.Imbalance == ClassImbalanceHandling.Undersample)
            {
                var keep = Math.Max(counts.Min(), 10);

                for (var k = 0; k < classes; k++)
                {
                    var inClass = Enumerable.Range(0, y.Length).Where(i => y[i] == k).OrderBy(_ => random.Next());
                    rows.AddRange(inClass.Take(keep));
                }

                return (rows.Select(i => x[i]).ToArray(), rows.Select(i => y[i]).ToArray(), null,
                    $"Undersampled every training class to at most {keep} rows.");
            }

            var target = counts.Max();

            for (var k = 0; k < classes; k++)
            {
                var inClass = Enumerable.Range(0, y.Length).Where(i => y[i] == k).ToArray();
                rows.AddRange(inClass);

                for (var extra = inClass.Length; extra < target; extra++)
                {
                    rows.Add(inClass[random.Next(inClass.Length)]);
                }
            }

            var note = c.Imbalance == ClassImbalanceHandling.BalancedWeights
                ? $"{c.Algorithm} cannot use row weights, so smaller training classes were oversampled to {target} rows instead."
                : $"Oversampled every training class to {target} rows.";

            return (rows.Select(i => x[i]).ToArray(), rows.Select(i => y[i]).ToArray(), null, note);
        }

        public static MulticlassRun RunOne(MulticlassConfig c, double[][] x, int[] y, string[] names, int classes,
            int[] classLabels, DataSplitIndices split, int seed, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var trainX = DataSplit.Take(x, split.Train);
            var trainY = DataSplit.Take(y, split.Train);
            var testX = DataSplit.Take(x, split.Test);
            var testY = DataSplit.Take(y, split.Test);
            var warnings = new List<EvaluationWarning>();

            if (trainY.Distinct().Count() < classes)
            {
                throw new ArgumentException(
                    "Every class must appear in the training split; give more rows for the rare classes.");
            }

            var transform = FeatureTransform.Fit(trainX, names, c.NeedsScaling);
            var trainT = transform.Apply(trainX);
            var testT = transform.Apply(testX);
            var prepared = Balance(c, trainT, trainY, classes, seed);

            if (prepared.Note != null)
            {
                warnings.Add(new EvaluationWarning(EvaluationWarningCodes.Resampled, prepared.Note));
            }

            IMulticlassModel model = c.Algorithm switch
            {
                MulticlassAlgorithm.DecisionTree or MulticlassAlgorithm.RandomForest => TreeMulticlassModel.Fit(c,
                    prepared.X,
                    prepared.Y, prepared.W, classes, classLabels, seed, token),
                MulticlassAlgorithm.NaiveBayes => GaussianBayesMulticlassModel.Fit(c, prepared.X, prepared.Y,
                    prepared.W,
                    classes),
                MulticlassAlgorithm.KNearestNeighbours => NearestNeighbourMulticlassModel.Fit(c, prepared.X, prepared.Y,
                    classes),
                MulticlassAlgorithm.NeuralNetwork => NeuralMulticlassModel.Fit(c, prepared.X, prepared.Y, classes, seed,
                    token),
                _ => MultinomialLogisticModel.Fit(prepared.X, prepared.Y, classes, token)
            };

            var trainProbabilities = model.Probabilities(trainT);
            var testProbabilities = model.Probabilities(testT);
            var trainMetrics = MulticlassMetricsBuilder.Build(trainProbabilities, trainY, classLabels);
            var testMetrics = MulticlassMetricsBuilder.Build(testProbabilities, testY, classLabels);

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

            if (testMetrics != null)
            {
                var smallest = testMetrics.PerClass.Min(p => p.Support);

                if (testMetrics.Rows < ModelEvaluationLimits.MinimumTestRows || smallest < 5)
                {
                    warnings.Add(new EvaluationWarning(EvaluationWarningCodes.TinyTestSet,
                        $"The test split has {testMetrics.Rows} rows and its smallest class only {smallest}; per-class metrics will be noisy."));
                }

                if (trainMetrics?.Accuracy is { } trainAccuracy && testMetrics.Accuracy is { } testAccuracy &&
                    trainAccuracy - testAccuracy > 0.15)
                {
                    warnings.Add(new EvaluationWarning(EvaluationWarningCodes.Overfit,
                        $"Training accuracy {trainAccuracy:0.###} is far above test accuracy {testAccuracy:0.###}: the model is overfitting."));
                }

                var majority = testMetrics.PerClass.Max(p => p.Support) / (double)testMetrics.Rows;

                if (testMetrics.Accuracy is { } accuracy && accuracy <= majority + 0.02)
                {
                    warnings.Add(new EvaluationWarning(EvaluationWarningCodes.NoSignal,
                        "Test accuracy is no better than always predicting the largest class; the predictors show no usable signal with this model."));
                }

                if (testMetrics.MacroRocAuc is >= ModelEvaluationLimits.SuspiciousAuc)
                {
                    warnings.Add(new EvaluationWarning(EvaluationWarningCodes.SuspiciouslyPerfect,
                        "The macro ROC AUC is almost perfect; check for a predictor that leaks the class."));
                }
            }

            return new MulticlassRun
            {
                Config = c,
                Predict = rows => model.Probabilities(transform.Apply(rows)),
                TrainRows = split.Train,
                ValidationRows = split.Validation,
                TestRows = split.Test,
                TestProbabilities = testProbabilities,
                TestLabels = testY,
                TrainLabels = trainY,
                TrainMetrics = trainMetrics,
                TestMetrics = testMetrics,
                Description = model.Describe(names),
                Warnings = warnings
            };
        }

        public static MulticlassEvaluationResult Evaluate(MulticlassConfig c, double[][] x, int[] labels,
            string[] names,
            EvaluationOptions options, CancellationToken token)
        {
            var y = Encode(labels, out var classLabels);
            var classes = classLabels.Length;
            var doubles = y.Select(v => (double)v).ToArray();
            var plans = new List<DataSplitIndices>();

            if (options.Folds >= 2)
            {
                var folds = KFoldSplit.Stratified(doubles, options.Folds, options.Seed);

                for (var f = 0; f < folds.Length; f++)
                {
                    plans.Add(new DataSplitIndices(folds[f].Train, [], folds[f].Test));
                }
            }
            else
            {
                var split = DataSplit.Split(doubles, 1d - options.TestFraction - options.ValidationFraction,
                    options.ValidationFraction, options.TestFraction, options.Seed);

                var assigned = new bool[y.Length];

                foreach (var i in split.Train.Concat(split.Validation).Concat(split.Test))
                {
                    assigned[i] = true;
                }

                var leftovers = Enumerable.Range(0, y.Length).Where(i => !assigned[i]);
                plans.Add(new DataSplitIndices(split.Train.Concat(leftovers).ToArray(), split.Validation, split.Test));
            }

            var runs = new List<MulticlassRun>();

            for (var f = 0; f < plans.Count; f++)
            {
                var seed = options.Folds >= 2 ? SeededLibrary.Derive(options.Seed, 31 * (f + 1)) : options.Seed;
                runs.Add(RunOne(c, x, y, names, classes, classLabels, plans[f], seed, token));
            }

            return Assemble(runs, c, x, y, names, classLabels, options, token);
        }

        public static MulticlassEvaluationResult Assemble(IReadOnlyList<MulticlassRun> runs, MulticlassConfig c,
            double[][] x, int[] y, string[] names, int[] classLabels, EvaluationOptions options,
            CancellationToken token)
        {
            var cv = options.Folds >= 2;
            var first = runs[0];
            var warnings = new List<EvaluationWarning>();

            foreach (var warning in runs.SelectMany(r => r.Warnings))
            {
                if (!warnings.Any(w => w.Code == warning.Code))
                {
                    warnings.Add(warning);
                }
            }

            var test = cv
                ? MulticlassMetricsBuilder.Build(runs.SelectMany(r => r.TestProbabilities).ToArray(),
                    runs.SelectMany(r => r.TestLabels).ToArray(), classLabels)
                : first.TestMetrics;

            var summary = new List<EvaluationMetricSummary>();

            if (cv)
            {
                summary.Add(ModelMath.Summarise("Accuracy",
                    runs.Select(r => r.TestMetrics?.Accuracy ?? double.NaN).ToList()));
                summary.Add(ModelMath.Summarise("MacroF1",
                    runs.Select(r => r.TestMetrics?.MacroF1 ?? double.NaN).ToList()));
                summary.Add(ModelMath.Summarise("LogLoss",
                    runs.Select(r => r.TestMetrics?.LogLoss ?? double.NaN).ToList()));
            }

            var importance = Array.Empty<EvaluationImportanceRow>();

            if (options.ImportanceRepeats > 0)
            {
                var drops = names.Select(_ => new List<double>()).ToArray();
                var any = false;

                for (var f = 0; f < runs.Count; f++)
                {
                    var run = runs[f];

                    var take = PermutationImportance.RowsFor(run.TestRows.Length, names.Length,
                        options.ImportanceRepeats,
                        runs.Count);

                    var chosen = run.TestRows.Take(take).ToArray();
                    var rows = DataSplit.Take(x, chosen);
                    var labelsHere = DataSplit.Take(y, chosen);

                    var baseline = PermutationImportance.Permute(run.Predict, rows,
                        scores => MulticlassLogLoss(scores, labelsHere), false,
                        options.ImportanceRepeats,
                        SeededLibrary.Derive(options.Seed, 5000 + f), drops, token);

                    any |= double.IsFinite(baseline);
                }

                importance = any ? PermutationImportance.Rows(names, drops) : [];
            }

            var counts = classLabels.Select((_, k) => y.Count(v => v == k)).ToArray();
            var majority = counts.Max() / (double)y.Length;

            var result = new MulticlassEvaluationResult
            {
                Algorithm = c.Algorithm.ToString(),
                Scheme = options.Folds >= 2
                    ? $"StratifiedCrossValidation({options.Folds} folds)"
                    : $"TrainValidationTest({1d - options.ValidationFraction - options.TestFraction:0.##}/{options.ValidationFraction:0.##}/{options.TestFraction:0.##})",
                Seed = options.Seed,
                Settings = c.Settings(),
                Rows = x.Length,
                Features = names.Length,
                ClassLabels = classLabels,
                ClassCounts = counts,
                TrainRows = cv ? (int)Math.Round(runs.Average(r => r.TrainRows.Length)) : first.TrainRows.Length,
                ValidationRows = cv ? 0 : first.ValidationRows.Length,
                TestRows = cv ? runs.Sum(r => r.TestRows.Length) : first.TestRows.Length,
                Test = test,
                Train = cv ? null : first.TrainMetrics,
                MajorityClassAccuracy = majority,
                FoldSummary = summary.ToArray(),
                Importance = importance,
                Model = first.Description,
                Warnings = warnings.ToArray(),
                Interpretation = string.Empty
            };

            return new MulticlassEvaluationResult
            {
                Algorithm = result.Algorithm,
                Scheme = result.Scheme,
                Seed = result.Seed,
                Settings = result.Settings,
                Rows = result.Rows,
                Features = result.Features,
                ClassLabels = result.ClassLabels,
                ClassCounts = result.ClassCounts,
                TrainRows = result.TrainRows,
                ValidationRows = result.ValidationRows,
                TestRows = result.TestRows,
                Test = result.Test,
                Train = result.Train,
                MajorityClassAccuracy = result.MajorityClassAccuracy,
                FoldSummary = result.FoldSummary,
                Importance = result.Importance,
                Model = result.Model,
                Warnings = result.Warnings,
                Interpretation = Interpret(result)
            };
        }

        private static double MulticlassLogLoss(double[][] probabilities, int[] y)
        {
            var sum = 0d;

            for (var i = 0; i < y.Length; i++)
            {
                sum -= Math.Log(Math.Max(probabilities[i][y[i]], 1e-15));
            }

            return y.Length == 0 ? double.NaN : sum / y.Length;
        }

        private static string F(double? value)
        {
            return value.HasValue ? value.Value.ToString("0.###", CultureInfo.InvariantCulture) : "n/a";
        }

        private static string Interpret(MulticlassEvaluationResult r)
        {
            if (r.Test == null)
            {
                return "No held-out metrics were produced.";
            }

            var weakest = r.Test.PerClass.Where(p => p.F1.HasValue).OrderBy(p => p.F1).FirstOrDefault();

            var parts = new List<string>
            {
                $"{r.Algorithm} across {r.ClassLabels.Length} classes on held-out rows: accuracy {F(r.Test.Accuracy)} against {F(r.MajorityClassAccuracy)} for always predicting the largest class, balanced accuracy {F(r.Test.BalancedAccuracy)}, macro F1 {F(r.Test.MacroF1)}, log loss {F(r.Test.LogLoss)}, macro one-versus-rest ROC AUC {F(r.Test.MacroRocAuc)}."
            };

            if (weakest != null)
            {
                parts.Add(
                    $"The hardest class is {weakest.Class} (F1 {F(weakest.F1)}, recall {F(weakest.Recall)}, {weakest.Support} rows); check the confusion matrix for what it is mistaken for.");
            }

            if (r.Importance.Length > 0)
            {
                parts.Add("Most influential variables by permutation importance on log loss: " +
                          string.Join(", ", r.Importance.Take(3).Select(i => $"{i.Variable} ({F(i.Importance)})")) +
                          ".");
            }

            if (r.Warnings.Length > 0)
            {
                parts.Add($"Read the {r.Warnings.Length} warning(s) before trusting this result.");
            }

            return string.Join(" ", parts);
        }
    }
}