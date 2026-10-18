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
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    [AdvancedAnalyticsToolGroup("ModelEvaluationTools")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
    public class RegressionEvaluationTools
    {
        private const string Bounds =
            "Bounds: at most 20000 rows, 200 predictor columns, 500 trees, 1000 training epochs; kernel ridge regression is limited to 4000 rows. Nothing is saved: the tool fits, evaluates and discards the model.";

        private const string Method =
            "Method: rows are split by seed into train, validation and test sets, or into cross validation folds when crossValidationFolds is 2 or more. Missing-value filling and scaling are learned from the training rows only, any setting sweep is decided on the validation rows only, and every reported metric comes from the untouched held-out rows. " +
            "The test metrics are R-squared, mean absolute error, root mean squared error, MAPE (which skips zero targets), symmetric MAPE, median and maximum absolute error, correlation and residual mean, spread and skewness, next to a baseline that always predicts the training mean. " +
            "Permutation importance is the drop in held-out R-squared when one variable is shuffled. ";

        [AdvancedAnalyticsTool("EvaluateLinearRegression")]
        [Description(
            "Fits a linear regression for a continuous target and evaluates it on held-out rows. Penalty None is ordinary least squares; Ridge shrinks coefficients (good for collinear predictors), Lasso sets weak coefficients exactly to zero (good for selecting variables) and ElasticNet mixes both. " +
            "Coefficients are returned on the original variable scale, the change in the target per one unit of a variable, with the intercept; a sweep over regularisation strengths picks the best one on the validation rows. " +
            "Use it as the transparent baseline: if a tree or forest beats it by a lot, the relationship is non-linear or has interactions. Collinear predictors make None unstable; use Ridge. " +
            Method + Bounds)]
        public RegressionEvaluationResult EvaluateLinearRegression(
            [Description(
                "Predictor rows: one array of numbers per case, all the same length and in the order of the variable names. NaN marks a missing value.")]
            double[][] inputs,
            [Description(
                "The continuous target value for each row, in the same order as the rows. Every value must be finite.")]
            double[] targets,
            [Description("The name of each predictor column, unique and in column order.")]
            string[] variableNames,
            [Description("None, Ridge, Lasso or ElasticNet.")]
            LinearPenalty penalty = LinearPenalty.None,
            [Description(
                "The penalty strengths to try, up to 16 numbers between 0 and 1000000, on standardised predictors. Empty tries 0.01, 0.1 and 1. Ignored for None.")]
            double[]? regularisationValues = null,
            [Description(
                "For ElasticNet, the share of the penalty that is Lasso (L1), 0 to 1; 1 is pure Lasso, 0 is pure Ridge. Default 0.5.")]
            double l1Ratio = 0.5,
            [Description("Share of rows held back to choose the penalty strength, 0 to 0.5. Default 0.2.")]
            double validationFraction = 0.2,
            [Description(
                "Share of rows held back purely to measure quality, above 0 and at most 0.5. Ignored when cross validation is used. Default 0.2.")]
            double testFraction = 0.2,
            [Description("Seed that makes the splits reproducible. Reuse a seed to compare models on identical rows.")]
            int seed = 1,
            [Description(
                "0 evaluates on one train, validation and test split; 2 to 10 runs cross validation and pools the held-out predictions of every fold.")]
            int crossValidationFolds = 0,
            [Description(
                "How many times each variable is shuffled to measure permutation importance, 0 to 10; 0 skips importance. Default 3.")]
            int permutationRepeats = 3,
            CancellationToken cancellationToken = default)
        {
            var options = RegressionToolInput.Validate(inputs, targets, variableNames, validationFraction, testFraction,
                seed, crossValidationFolds, permutationRepeats);

            ModelEvaluationGuard.Sweep(regularisationValues, nameof(regularisationValues), 0d, 1e6,
                "regularisation strength");

            ModelEvaluationGuard.DoubleRange(l1Ratio, 0d, 1d, nameof(l1Ratio), "The L1 ratio");

            var values = penalty == LinearPenalty.None
                ? [0d]
                : regularisationValues is { Length: > 0 }
                    ? regularisationValues.Distinct().OrderBy(v => v).ToArray()
                    : [0.01, 0.1, 1d];

            var configs = values.Select(v => new RegressorConfig
            {
                Family = RegressionFamily.LinearRegression,
                Penalty = penalty,
                Lambda = v,
                L1Ratio = l1Ratio
            }).ToArray();

            var labels = values.Select(v => "Regularisation=" + LearnerConfig.Format(v)).ToArray();

            return RegressionEvaluationEngine.Evaluate(configs, labels, inputs, targets, variableNames, options,
                cancellationToken);
        }

        [AdvancedAnalyticsTool("EvaluateTreeRegression")]
        [Description(
            "Fits a tree based regressor for a continuous target and evaluates it on held-out rows. DecisionTree is one readable regression tree (with the rules and tree text); RandomForest averages many deep trees on bootstrap samples; GradientBoosting adds shallow trees that fit the remaining error with a learning rate. " +
            "Trees capture non-linear effects, thresholds and interactions without scaling, but predict step functions and cannot extrapolate beyond the target range seen in training. Compare against EvaluateLinearRegression: a small gain means the relationship is close to linear. " +
            "Read the TrainTestGap warning: a single deep tree overfits, forests are safer and boosting needs shallow trees and a small learning rate. " +
            Method + Bounds)]
        public RegressionEvaluationResult EvaluateTreeRegression(
            [Description(
                "Predictor rows: one array of numbers per case, all the same length and in the order of the variable names. NaN marks a missing value.")]
            double[][] inputs,
            [Description(
                "The continuous target value for each row, in the same order as the rows. Every value must be finite.")]
            double[] targets,
            [Description("The name of each predictor column, unique and in column order.")]
            string[] variableNames,
            [Description("DecisionTree, RandomForest or GradientBoosting.")]
            RegressionFamily algorithm = RegressionFamily.RandomForest,
            [Description(
                "Number of trees for RandomForest and rounds for GradientBoosting, 1 to 500. Ignored for DecisionTree. Default 100.")]
            int treeCount = 100,
            [Description(
                "Maximum depth of each tree, 1 to 24. Use about 3 for GradientBoosting and 10 or more for RandomForest. Default 6.")]
            int maxDepth = 6,
            [Description("Minimum training rows in every leaf, 1 to 1000. Default 5.")]
            int minLeafSize = 5,
            [Description(
                "Shrinkage applied to every GradientBoosting round, above 0 and at most 1. Ignored by the other algorithms. Default 0.1.")]
            double learningRate = 0.1,
            [Description(
                "Share of variables tried at each RandomForest split, above 0 and at most 1; 0 means one third of the variables. Default 0.")]
            double featureFraction = 0,
            [Description("None keeps the grown tree. ReducedError prunes a DecisionTree using the validation rows.")]
            TreePruning pruning = TreePruning.None,
            [Description("Share of rows held back for pruning, 0 to 0.5. Default 0.2.")]
            double validationFraction = 0.2,
            [Description(
                "Share of rows held back purely to measure quality, above 0 and at most 0.5. Ignored when cross validation is used. Default 0.2.")]
            double testFraction = 0.2,
            [Description("Seed that makes the splits and every random choice reproducible.")]
            int seed = 1,
            [Description(
                "0 evaluates on one train, validation and test split; 2 to 10 runs cross validation and pools the held-out predictions of every fold.")]
            int crossValidationFolds = 0,
            [Description(
                "How many times each variable is shuffled to measure permutation importance, 0 to 10; 0 skips importance. Default 3.")]
            int permutationRepeats = 3,
            CancellationToken cancellationToken = default)
        {
            var options = RegressionToolInput.Validate(inputs, targets, variableNames, validationFraction, testFraction,
                seed, crossValidationFolds, permutationRepeats);

            if (algorithm is not (RegressionFamily.DecisionTree or RegressionFamily.RandomForest
                or RegressionFamily.GradientBoosting))
            {
                throw new ArgumentException("Algorithm must be DecisionTree, RandomForest or GradientBoosting.",
                    nameof(algorithm));
            }

            ModelEvaluationGuard.IntRange(treeCount, 1, ModelEvaluationLimits.MaximumTrees, nameof(treeCount),
                "The tree count");
            ModelEvaluationGuard.IntRange(maxDepth, 1, ModelEvaluationLimits.MaximumTreeDepth, nameof(maxDepth),
                "The maximum depth");

            ModelEvaluationGuard.IntRange(minLeafSize, 1, 1000, nameof(minLeafSize), "The minimum leaf size");
            ModelEvaluationGuard.DoubleRange(learningRate, 0.001, 1d, nameof(learningRate), "The learning rate");
            ModelEvaluationGuard.DoubleRange(featureFraction, 0d, 1d, nameof(featureFraction), "The feature fraction");

            var config = new RegressorConfig
            {
                Family = algorithm,
                Trees = treeCount,
                MaxDepth = maxDepth,
                MinLeaf = minLeafSize,
                LearningRate = learningRate,
                FeatureFraction = featureFraction,
                Pruning = pruning
            };

            return RegressionEvaluationEngine.Evaluate([config], [config.Name], inputs, targets, variableNames, options,
                cancellationToken);
        }

        [AdvancedAnalyticsTool("EvaluateNeighbourAndKernelRegression")]
        [Description(
            "Fits a k nearest neighbours regressor or a kernel ridge regressor for a continuous target over a sweep of settings and evaluates the chosen one on held-out rows. " +
            "KNearestNeighbours predicts the mean target of the k closest training rows on standardised predictors (sweep neighbourCounts); KernelRidge fits a smooth non-linear surface with a ridge penalty of 1 divided by complexity (sweep complexityValues) and a chosen kernel. " +
            "Both are flexible local methods that cannot extrapolate and slow down with rows, so kernel ridge is limited to 2000 rows here. The setting is chosen on the validation rows only. " +
            Method + Bounds)]
        public RegressionEvaluationResult EvaluateNeighbourAndKernelRegression(
            [Description(
                "Predictor rows: one array of numbers per case, all the same length and in the order of the variable names. NaN marks a missing value.")]
            double[][] inputs,
            [Description(
                "The continuous target value for each row, in the same order as the rows. Every value must be finite.")]
            double[] targets,
            [Description("The name of each predictor column, unique and in column order.")]
            string[] variableNames,
            [Description("KNearestNeighbours or KernelRidge.")]
            RegressionFamily algorithm = RegressionFamily.KNearestNeighbours,
            [Description(
                "For KNearestNeighbours, the values of k to try, up to 16 whole numbers between 1 and 500. Empty tries 3, 5, 9, 15 and 25.")]
            int[]? neighbourCounts = null,
            [Description(
                "For KernelRidge, the complexity values to try (penalty is 1 divided by complexity), up to 16 numbers between 0.0001 and 10000. Empty tries 1, 10 and 100.")]
            double[]? complexityValues = null,
            [Description(
                "For KernelRidge, the kernel: Linear, Gaussian, Polynomial, Sigmoid or Laplacian. Default Gaussian.")]
            SvmKernel kernel = SvmKernel.Gaussian,
            [Description(
                "For KernelRidge, the kernel width as gamma, between 0.00001 and 1000; empty uses 1 divided by the number of variables.")]
            double? kernelWidth = null,
            [Description("Share of rows held back to choose the setting, 0 to 0.5. Default 0.2.")]
            double validationFraction = 0.2,
            [Description(
                "Share of rows held back purely to measure quality, above 0 and at most 0.5. Ignored when cross validation is used. Default 0.2.")]
            double testFraction = 0.2,
            [Description("Seed that makes the splits reproducible.")]
            int seed = 1,
            [Description(
                "0 evaluates on one train, validation and test split; 2 to 10 runs cross validation with the setting chosen inside each fold.")]
            int crossValidationFolds = 0,
            [Description(
                "How many times each variable is shuffled to measure permutation importance, 0 to 10; 0 skips importance. Default 3.")]
            int permutationRepeats = 3,
            CancellationToken cancellationToken = default)
        {
            if (algorithm is not (RegressionFamily.KNearestNeighbours or RegressionFamily.KernelRidge))
            {
                throw new ArgumentException("Algorithm must be KNearestNeighbours or KernelRidge.", nameof(algorithm));
            }

            var options = RegressionToolInput.Validate(inputs, targets, variableNames, validationFraction, testFraction,
                seed, crossValidationFolds, permutationRepeats,
                algorithm == RegressionFamily.KernelRidge
                    ? ModelEvaluationLimits.MaximumKernelRows / 2
                    : ModelEvaluationLimits.MaximumNeighbourRows);

            ModelEvaluationGuard.Sweep(neighbourCounts, nameof(neighbourCounts), 1, 500, "neighbour count");
            ModelEvaluationGuard.Sweep(complexityValues, nameof(complexityValues), 1e-4, 1e4, "complexity");

            if (kernelWidth.HasValue)
            {
                ModelEvaluationGuard.DoubleRange(kernelWidth.Value, 1e-5, 1e3, nameof(kernelWidth), "The kernel width");
            }

            RegressorConfig[] configs;
            string[] labels;

            if (algorithm == RegressionFamily.KNearestNeighbours)
            {
                var counts = neighbourCounts is { Length: > 0 }
                    ? neighbourCounts.Distinct().OrderBy(k => k).ToArray()
                    : [3, 5, 9, 15, 25];

                configs = counts.Select(k => new RegressorConfig { Family = algorithm, K = k }).ToArray();
                labels = counts.Select(k => "NeighbourCount=" + k).ToArray();
            }
            else
            {
                var values = complexityValues is { Length: > 0 }
                    ? complexityValues.Distinct().OrderBy(v => v).ToArray()
                    : [1d, 10d, 100d];

                configs = values.Select(v => new RegressorConfig
                {
                    Family = algorithm,
                    Complexity = v,
                    Kernel = kernel,
                    Gamma = kernelWidth ?? 0d
                }).ToArray();

                labels = values.Select(v => "Complexity=" + LearnerConfig.Format(v)).ToArray();
            }

            return RegressionEvaluationEngine.Evaluate(configs, labels, inputs, targets, variableNames, options,
                cancellationToken);
        }

        [AdvancedAnalyticsTool("EvaluateNeuralNetworkRegression")]
        [Description(
            "Fits a small multilayer perceptron for a continuous target and evaluates it on held-out rows, returning R-squared, mean absolute error, root mean squared error, MAPE, residual diagnostics, the training error trace and permutation importance. " +
            "Sigmoid units are trained by resilient backpropagation on standardised predictors and a target rescaled into the sigmoid range, from a seeded start so the result is reproducible; hiddenLayers gives the unit count of each layer (up to 3 layers of up to 64 units) and epochs is capped at 1000. " +
            "Neural networks need more rows than trees, cannot extrapolate beyond the training target range and are hard to explain; compare them with EvaluateLinearRegression and EvaluateTreeRegression before adopting one. " +
            Method + Bounds)]
        public RegressionEvaluationResult EvaluateNeuralNetworkRegression(
            [Description(
                "Predictor rows: one array of numbers per case, all the same length and in the order of the variable names. NaN marks a missing value.")]
            double[][] inputs,
            [Description(
                "The continuous target value for each row, in the same order as the rows. Every value must be finite.")]
            double[] targets,
            [Description("The name of each predictor column, unique and in column order.")]
            string[] variableNames,
            [Description(
                "Number of units in each hidden layer, up to 3 layers of 1 to 64 units. Empty uses one layer of 8 units.")]
            int[]? hiddenLayers = null,
            [Description("Maximum training passes over the data, 1 to 1000. Default 200.")]
            int epochs = 200,
            [Description("Share of rows held back for a validation summary, 0 to 0.5. Default 0.2.")]
            double validationFraction = 0.2,
            [Description(
                "Share of rows held back purely to measure quality, above 0 and at most 0.5. Ignored when cross validation is used. Default 0.2.")]
            double testFraction = 0.2,
            [Description("Seed that fixes the split and the starting weights so the run is reproducible.")]
            int seed = 1,
            [Description(
                "0 evaluates on one train, validation and test split; 2 to 10 runs cross validation and pools the held-out predictions of every fold.")]
            int crossValidationFolds = 0,
            [Description(
                "How many times each variable is shuffled to measure permutation importance, 0 to 10; 0 skips importance. Default 3.")]
            int permutationRepeats = 3,
            CancellationToken cancellationToken = default)
        {
            var options = RegressionToolInput.Validate(inputs, targets, variableNames, validationFraction, testFraction,
                seed, crossValidationFolds, permutationRepeats);

            ModelEvaluationGuard.HiddenLayers(hiddenLayers, nameof(hiddenLayers));

            ModelEvaluationGuard.IntRange(epochs, 1, ModelEvaluationLimits.MaximumEpochs, nameof(epochs),
                "The epoch count");

            var config = new RegressorConfig
            {
                Family = RegressionFamily.NeuralNetwork,
                Hidden = hiddenLayers is { Length: > 0 } ? hiddenLayers : [8],
                Epochs = epochs
            };

            return RegressionEvaluationEngine.Evaluate([config], [config.Name], inputs, targets, variableNames, options,
                cancellationToken);
        }

        [AdvancedAnalyticsTool("CompareRegressors")]
        [Description(
            "Runs several regression families on identical cross validation folds and ranks them, returning for each the mean held-out score with its spread across folds, the training to held-out gap, the paired difference to the best and whether it is statistically indistinguishable from the best, plus a plain-language recommendation. " +
            "Families available are LinearRegression, DecisionTree, RandomForest, GradientBoosting, KNearestNeighbours, KernelRidge and NeuralNetwork, each at sensible defaults; leave families empty to try all of them. " +
            "Two families are called indistinguishable when their fold-by-fold difference is within one standard error; the recommendation then prefers the simplest of them. Use the single-family tools to tune the winner. " +
            "Bounds: at most 20000 rows and 200 predictor columns, 2 to 10 folds; kernel ridge is skipped above 2000 rows. Nothing is saved.")]
        public RegressorComparisonResult CompareRegressors(
            [Description(
                "Predictor rows: one array of numbers per case, all the same length and in the order of the variable names. NaN marks a missing value.")]
            double[][] inputs,
            [Description(
                "The continuous target value for each row, in the same order as the rows. Every value must be finite.")]
            double[] targets,
            [Description("The name of each predictor column, unique and in column order.")]
            string[] variableNames,
            [Description("The families to compare, at most 7. Empty compares all of them.")]
            RegressionFamily[]? families = null,
            [Description(
                "The metric to rank by: RSquared (higher is better), MeanAbsoluteError, RootMeanSquaredError or MeanAbsolutePercentageError (lower is better). Default RootMeanSquaredError.")]
            RegressionSelectionMetric metric = RegressionSelectionMetric.RootMeanSquaredError,
            [Description("Number of cross validation folds shared by every family, 2 to 10. Default 5.")]
            int folds = 5,
            [Description("Seed that makes the folds reproducible.")]
            int seed = 1,
            CancellationToken cancellationToken = default)
        {
            ModelEvaluationGuard.StrictFolds(folds);
            var options = RegressionToolInput.Validate(inputs, targets, variableNames, 0.2, 0.2, seed, folds, 0);

            var chosen = families is { Length: > 0 }
                ? families.Distinct().ToArray()
                : Enum.GetValues<RegressionFamily>();

            if (chosen.Length > 7)
            {
                throw new ArgumentException("At most 7 families can be compared.", nameof(families));
            }

            return ModelComparisonEngine.CompareRegressors(chosen, inputs, targets, variableNames, options, metric,
                cancellationToken);
        }

        [AdvancedAnalyticsTool("ComputeRegressionPermutationImportance")]
        [Description(
            "Measures how much each variable matters to a regression model by shuffling it in the held-out rows and recording how much the R-squared falls, averaged over repeats and, optionally, over cross validation folds. " +
            "It is model agnostic (works for every regression family) and, unlike coefficient sizes or tree split counts, is measured on unseen data, so it reflects what generalises. A variable whose importance is near zero or within its standard deviation of zero can be dropped; correlated variables share importance and can each look weak. " +
            "The model is fitted on training rows only and nothing is saved. Bounds: at most 20000 rows and 200 predictor columns; the shuffling budget limits the rows used per fold.")]
        public PermutationImportanceResult ComputeRegressionPermutationImportance(
            [Description(
                "Predictor rows: one array of numbers per case, all the same length and in the order of the variable names. NaN marks a missing value.")]
            double[][] inputs,
            [Description(
                "The continuous target value for each row, in the same order as the rows. Every value must be finite.")]
            double[] targets,
            [Description("The name of each predictor column, unique and in column order.")]
            string[] variableNames,
            [Description(
                "The model family to fit: LinearRegression, DecisionTree, RandomForest, GradientBoosting, KNearestNeighbours, KernelRidge or NeuralNetwork.")]
            RegressionFamily family = RegressionFamily.RandomForest,
            [Description(
                "Fixed hyper-parameters to set on the model, in the same order as fixedValues. Empty keeps the defaults.")]
            HyperParameter[]? fixedParameters = null,
            [Description("The value for each entry of fixedParameters, in the same order.")]
            double[]? fixedValues = null,
            [Description("How many times each variable is shuffled, 1 to 10. Default 5.")]
            int repeats = 5,
            [Description(
                "0 uses one train, validation and test split; 2 to 10 averages over cross validation folds. Default 0.")]
            int crossValidationFolds = 0,
            [Description("Share of rows held back for validation, 0 to 0.5. Default 0.2.")]
            double validationFraction = 0.2,
            [Description(
                "Share of rows held back for the importance test rows, above 0 and at most 0.5. Ignored with cross validation. Default 0.3.")]
            double testFraction = 0.3,
            [Description("Seed that makes the splits and the shuffles reproducible.")]
            int seed = 1,
            CancellationToken cancellationToken = default)
        {
            ModelEvaluationGuard.IntRange(repeats, 1, ModelEvaluationLimits.MaximumPermutationRepeats, nameof(repeats),
                "The number of repeats");

            var options = RegressionToolInput.Validate(inputs, targets, variableNames, validationFraction, testFraction,
                seed, crossValidationFolds, repeats);

            var config = ConfigFactory.BuildRegressor(family, fixedParameters, fixedValues, LinearPenalty.Ridge,
                SvmKernel.Gaussian, nameof(fixedParameters));

            var plan = RegressionEvaluationEngine.Plan(targets, options);
            var runs = new List<RegressionRun>();

            for (var f = 0; f < plan.Count; f++)
            {
                var runSeed = options.Folds >= 2 ? SeededLibrary.Derive(options.Seed, 31 * (f + 1)) : options.Seed;

                runs.Add(RegressionEvaluationEngine.RunOne([config], [config.Name], inputs, targets, variableNames,
                    plan[f],
                    options, runSeed, cancellationToken));
            }

            var drops = variableNames.Select(_ => new List<double>()).ToArray();
            var baselines = new List<double>();

            for (var f = 0; f < runs.Count; f++)
            {
                var run = runs[f];

                var take = PermutationImportance.RowsFor(run.TestRows.Length, variableNames.Length, repeats,
                    runs.Count);

                var chosenRows = run.TestRows.Take(take).ToArray();
                var rows = DataSplit.Take(inputs, chosenRows);
                var y = DataSplit.Take(targets, chosenRows);

                if (y.Distinct().Count() < 2)
                {
                    continue;
                }

                var baseline = PermutationImportance.Permute(run.Predict, rows,
                    predictions => RegressionMetrics.RSquared(y, predictions), true, repeats,
                    SeededLibrary.Derive(seed, 5000 + f), drops, cancellationToken);

                if (double.IsFinite(baseline))
                {
                    baselines.Add(baseline);
                }
            }

            return ImportanceReport.Build(config.Name, "RSquared", seed, repeats,
                options.Folds >= 2 ? options.Folds : 1,
                baselines, variableNames, drops, runs.SelectMany(r => r.Warnings).ToList(),
                "R-squared", "held-out R-squared falls");
        }

        [AdvancedAnalyticsTool("ComputeRegressionPartialDependence")]
        [Description(
            "Shows how a regression model's average prediction changes as one variable moves across its range while every other variable keeps its real values (partial dependence), with the 10th, 50th and 90th percentile of the individual predictions at each point (the spread of individual conditional expectations). " +
            "It reveals the shape the model learned: increasing, decreasing, flat, or non-monotone (thresholds and interactions). A wide band around a flat mean means the effect depends on other variables. " +
            "The model is fitted on training rows and the curve is computed on up to 500 held-out rows over the 5th to 95th percentile of the variable, with up to 50 grid points. Read it together with the variable's permutation importance: a shape means little for an unimportant variable. Nothing is saved.")]
        public PartialDependenceResult ComputeRegressionPartialDependence(
            [Description(
                "Predictor rows: one array of numbers per case, all the same length and in the order of the variable names. NaN marks a missing value.")]
            double[][] inputs,
            [Description(
                "The continuous target value for each row, in the same order as the rows. Every value must be finite.")]
            double[] targets,
            [Description("The name of each predictor column, unique and in column order.")]
            string[] variableNames,
            [Description("The name of the predictor to vary; it must be one of the variable names.")]
            string variable,
            [Description(
                "The model family to fit: LinearRegression, DecisionTree, RandomForest, GradientBoosting, KNearestNeighbours, KernelRidge or NeuralNetwork.")]
            RegressionFamily family = RegressionFamily.RandomForest,
            [Description(
                "Fixed hyper-parameters to set on the model, in the same order as fixedValues. Empty keeps the defaults.")]
            HyperParameter[]? fixedParameters = null,
            [Description("The value for each entry of fixedParameters, in the same order.")]
            double[]? fixedValues = null,
            [Description("Number of grid points between the 5th and 95th percentile, 2 to 50. Default 20.")]
            int gridPoints = 20,
            [Description("Share of rows held back for validation, 0 to 0.5. Default 0.2.")]
            double validationFraction = 0.2,
            [Description(
                "Share of rows held back as the rows the curve is averaged over, above 0 and at most 0.5. Default 0.3.")]
            double testFraction = 0.3,
            [Description("Seed that makes the split and the row sample reproducible.")]
            int seed = 1,
            CancellationToken cancellationToken = default)
        {
            var options = RegressionToolInput.Validate(inputs, targets, variableNames, validationFraction, testFraction,
                seed, 0, 0);

            ModelEvaluationGuard.IntRange(gridPoints, 2, ModelEvaluationLimits.MaximumPartialDependencePoints,
                nameof(gridPoints), "The number of grid points");

            if (string.IsNullOrWhiteSpace(variable) || !variableNames.Contains(variable))
            {
                throw new ArgumentException($"Variable '{variable}' is not one of the variable names.",
                    nameof(variable));
            }

            var config = ConfigFactory.BuildRegressor(family, fixedParameters, fixedValues, LinearPenalty.Ridge,
                SvmKernel.Gaussian, nameof(fixedParameters));

            var plan = RegressionEvaluationEngine.Plan(targets, options)[0];

            var run = RegressionEvaluationEngine.RunOne([config], [config.Name], inputs, targets, variableNames, plan,
                options,
                seed, cancellationToken);

            return PartialDependenceEngine.Compute(config.Name, run.Predict, DataSplit.Take(inputs, run.TrainRows),
                DataSplit.Take(inputs, run.TestRows), variableNames, variable, gridPoints, seed, "target value",
                run.Warnings);
        }
    }
}