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
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Interfaces;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;
using Jube.Service.Repository.AdvancedAnalyticsTrainingData.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    [AdvancedAnalyticsToolGroup("ModelSelection")]
    public class ModelSelectionTools(
        IAdvancedAnalyticsTrainingDataPointerReader? pointerReader = null,
        IAdvancedAnalyticsTrainingDataPointerWriter? pointerWriter = null)
    {
        private const string PointerHelp =
            " Give the predictor rows and labels inline, or a cachePointer to a stored table whose last column " +
            "is the label; leave inputs and labels empty when a cache pointer is given. A cachePointer is not " +
            "a data_pointer -- turn a data_pointer from RunSqlQuery or GetDemoDataset into one with " +
            "PutModelComparisonData first.";

        [AdvancedAnalyticsTool("PutModelComparisonData")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Turns a cached table into the Advanced Analytics cache pointer that cachePointer on every other " +
            "tool here actually needs -- the data_pointer RunSqlQuery, GetDemoDataset or an attached file " +
            "leaves behind cannot be passed to cachePointer directly, because that names a different cache " +
            "keyed by a plain Guid rather than by the \"tbl_...\" pointer string. Name the predictor columns " +
            "in the order the comparison should receive them and the outcome column by name; the labelColumn " +
            "this returns is always the last column, so pass it straight to cachePointer and labelColumn on " +
            "whichever tool is called next. Every value must be a number: a blank becomes a missing value, " +
            "and a column of text that is not numeric is refused rather than silently encoded. The label must " +
            "be 0 or 1 in every row, and the entry expires, so put the data and use it in the same turn.")]
        public Task<AdvancedAnalyticsTrainingDataDto> PutModelComparisonDataAsync(
            [Description("The data_pointer of the cached table, as returned by RunSqlQuery or GetDemoDataset.")]
            string dataPointer,
            [Description("The predictor column names, in the order the comparison should receive them.")]
            string[] variableNames,
            [Description("The name of the column holding the outcome, which must be 0 or 1 in every row.")]
            string labelColumnName,
            CancellationToken cancellationToken = default)
        {
            if (pointerWriter is null)
            {
                throw new InvalidOperationException(
                    "This tool was not bound with cache pointer support.");
            }

            return pointerWriter.PutAsync(dataPointer, variableNames, labelColumnName, cancellationToken);
        }

        private async Task<(double[][] Inputs, int[] Labels)> ResolveAsync(double[][]? inputs, int[]? labels,
            string[] variableNames, Guid? cachePointer, int labelColumn, CancellationToken cancellationToken)
        {
            if (cachePointer.HasValue)
            {
                if (inputs is not null || labels is not null)
                {
                    throw new ArgumentException(
                        "Give either inline inputs and labels or a cachePointer, not both.", nameof(cachePointer));
                }

                if (pointerReader is null)
                {
                    throw new InvalidOperationException(
                        "This tool was not bound with cache pointer support.");
                }

                return await pointerReader.ResolveAsync(cachePointer.Value, labelColumn, variableNames.Length,
                    cancellationToken).ConfigureAwait(false);
            }

            if (inputs is null || labels is null)
            {
                throw new ArgumentException(
                    "Give either inline inputs and labels or a cachePointer.", nameof(inputs));
            }

            return (inputs, labels);
        }

        private const string FamilyHelp =
            "Families: LogisticRegression, DecisionTree, RandomForest, Bagging, AdaBoost, GradientBoosting, NaiveBayes, TreeAugmentedNaiveBayes, KNearestNeighbours, LinearSupportVector, KernelSupportVector, NeuralNetwork. " +
            "Hyper-parameters and the families they apply to: MaxDepth, MinLeafSize (trees, forests, boosting), TreeCount (forests, boosting), FeatureFraction (RandomForest, GradientBoosting), SampleFraction (forests, GradientBoosting), LearningRate (boosting), NeighbourCount (KNearestNeighbours), Complexity (support vector machines), KernelWidth (KernelSupportVector), HiddenUnits and Epochs (NeuralNetwork), Regularisation (LogisticRegression, NaiveBayes, TreeAugmentedNaiveBayes), BinCount (DecisionTree with Id3, NaiveBayes categorical). ";

        private const string Bounds =
            "Bounds: at most 10000 rows (4000 for KernelSupportVector), 200 predictor columns, 2 to 10 folds, and no more than 1500 model fits in total. Nothing is saved.";

        [AdvancedAnalyticsTool("GridSearchHyperParameters")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Searches every combination of one or two hyper-parameters (a grid of at most 64 settings) for one model family by stratified cross validation, returning a trials table with each setting's mean training and held-out score, the spread across folds and its rank, the best setting, and an evaluation of the best configuration. " +
            "Set nested to true to also run a nested cross validation: the best setting is re-chosen inside every outer training part and scored on the outer held-out part, which gives the honest estimate of what tuning delivers; the difference to the best score is the selection optimism. " +
            "A BestAtEdgeOfGrid warning means the optimum touches the border of the grid, so widen it. Without nested evaluation the best score is optimistically biased because it is the maximum over many trials. " +
            FamilyHelp + Bounds + PointerHelp)]
        public async Task<HyperParameterSearchResult> GridSearchHyperParametersAsync(
            [Description("The name of each predictor column, unique and in column order.")]
            string[] variableNames,
            [Description("The model family to tune.")]
            EvaluationFamily family,
            [Description("The first hyper-parameter to vary; it must apply to the family.")]
            HyperParameter parameterA,
            [Description("The values to try for the first hyper-parameter, 1 to 16 numbers.")]
            double[] valuesA,
            [Description(
                "Predictor rows: one array of numbers per case, all the same length and in the order of the variable names. NaN marks a missing value. Leave empty when a cache pointer is given.")]
            double[][]? inputs = null,
            [Description(
                "The outcome for each row, in the same order as the rows: 1 for the event of interest and 0 otherwise. Leave empty when a cache pointer is given.")]
            int[]? labels = null,
            [Description(
                "An optional second hyper-parameter to vary, crossed with the first. Empty varies only the first.")]
            HyperParameter? parameterB = null,
            [Description(
                "The values to try for the second hyper-parameter, 1 to 16 numbers. Required when parameterB is given.")]
            double[]? valuesB = null,
            [Description(
                "Hyper-parameters to hold fixed for every trial, in the same order as fixedValues. Empty keeps the family defaults.")]
            HyperParameter[]? fixedParameters = null,
            [Description("The value for each entry of fixedParameters, in the same order.")]
            double[]? fixedValues = null,
            [Description(
                "The score to rank by: RocAuc, AveragePrecision, F1, MatthewsCorrelation (higher is better), LogLoss or BrierScore (lower is better). Default RocAuc.")]
            SelectionMetric metric = SelectionMetric.RocAuc,
            [Description("Number of stratified cross validation folds, 2 to 10. Default 5.")]
            int folds = 5,
            [Description(
                "Whether to run a nested cross validation for an unbiased estimate of the tuned model. Default false.")]
            bool nested = false,
            [Description("Number of outer folds for the nested evaluation, 2 to 5. Default 3.")]
            int outerFolds = 3,
            [Description("Tree algorithm when the family is DecisionTree: C45, Id3 or Cart. Default C45.")]
            TreeAlgorithm treeAlgorithm = TreeAlgorithm.C45,
            [Description("Kernel when the family is KernelSupportVector. Default Gaussian.")]
            SvmKernel svmKernel = SvmKernel.Gaussian,
            [Description(
                "Kind when the family is NaiveBayes: Gaussian, Categorical or TreeAugmented. Default Gaussian.")]
            NaiveBayesKind naiveBayesKind = NaiveBayesKind.Gaussian,
            [Description("None, BalancedWeights, Oversample or Undersample, applied to training rows in every fit.")]
            ClassImbalanceHandling classImbalance = ClassImbalanceHandling.None,
            [Description(
                "Share of each training part held back to fit the probability calibration, 0 to 0.5. Default 0.2.")]
            double validationFraction = 0.2,
            [Description("Seed that makes the folds and every random choice reproducible.")]
            int seed = 1,
            [Description(
                "Pointer to a table in the Advanced Analytics cache holding the predictor columns and a label column. Leave empty when the rows are inline.")]
            Guid? cachePointer = null,
            [Description(
                "Zero based index of the label column in the cached table. Default is the last column; only used with a cache pointer.")]
            int labelColumn = -1,
            CancellationToken cancellationToken = default)
        {
            SelectionToolInput.ValidateSearch(folds, metric);

            var (resolvedInputs, resolvedLabels) = await ResolveAsync(inputs, labels, variableNames, cachePointer,
                labelColumn, cancellationToken).ConfigureAwait(false);

            var options = SelectionToolInput.Common(resolvedInputs, resolvedLabels, variableNames, family,
                ModelEvaluationLimits.MaximumSearchRows,
                validationFraction, 0.2, seed, folds, 0);

            ModelEvaluationGuard.IntRange(outerFolds, 2, 5, nameof(outerFolds), "The number of outer folds");

            if (valuesA == null || valuesA.Length == 0)
            {
                throw new ArgumentException("Give at least one value for the first hyper-parameter.", nameof(valuesA));
            }

            if (parameterB.HasValue != valuesB is { Length: > 0 })
            {
                throw new ArgumentException("Give both parameterB and valuesB, or neither.", nameof(valuesB));
            }

            if (parameterB == parameterA)
            {
                throw new ArgumentException("The two hyper-parameters must differ.", nameof(parameterB));
            }

            ModelEvaluationGuard.Sweep(valuesA, nameof(valuesA), double.MinValue, double.MaxValue, "value");
            ModelEvaluationGuard.Sweep(valuesB, nameof(valuesB), double.MinValue, double.MaxValue, "value");

            var size = valuesA.Length * (valuesB?.Length ?? 1);

            if (size > ModelEvaluationLimits.MaximumGridSize)
            {
                throw new ArgumentException(
                    $"The grid has {size} settings, above the limit of {ModelEvaluationLimits.MaximumGridSize}; use fewer values or RandomSearchHyperParameters.",
                    nameof(valuesA));
            }

            var baseConfig = SelectionToolInput.Config(family, fixedParameters, fixedValues, classImbalance,
                treeAlgorithm,
                svmKernel, naiveBayesKind);

            var parameters = parameterB.HasValue ? [parameterA, parameterB.Value] : new[] { parameterA };

            var values = parameterB.HasValue
                ? new[] { valuesA.Distinct().ToArray(), valuesB!.Distinct().ToArray() }
                : new[] { valuesA.Distinct().ToArray() };

            if (!LearnerConfig.Supported(baseConfig.Family).Contains(parameterA) ||
                (parameterB.HasValue && !LearnerConfig.Supported(baseConfig.Family).Contains(parameterB.Value)))
            {
                throw new ArgumentException(
                    $"The hyper-parameters must apply to {baseConfig.Family}; the supported ones are {string.Join(", ", LearnerConfig.Supported(baseConfig.Family))}.",
                    nameof(parameterA));
            }

            var set = SearchEngine.Grid(baseConfig, parameters, values);

            return SearchEngine.Run(baseConfig.Family.ToString(), "Grid", set, resolvedInputs, resolvedLabels,
                variableNames, options, metric, nested, outerFolds, cancellationToken);
        }

        [AdvancedAnalyticsTool("RandomSearchHyperParameters")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Samples random combinations of hyper-parameters from ranges you give (uniform, or log-uniform for scale parameters such as Complexity, KernelWidth, LearningRate and Regularisation) and scores each by stratified cross validation, returning a trials table with each setting's mean training and held-out score, spread and rank, the best setting and an evaluation of it. " +
            "Random search covers a wide space with far fewer fits than a grid and is the better first step when there are more than two hyper-parameters; refine around the winner with GridSearchHyperParameters. Whole-number parameters are rounded. Set nested to true for a nested cross validation estimate that includes the choosing step. " +
            FamilyHelp + Bounds + PointerHelp +
            " Trials are capped at 64 and drawn with the seed, so the same seed gives the same trials.")]
        public async Task<HyperParameterSearchResult> RandomSearchHyperParametersAsync(
            [Description("The name of each predictor column, unique and in column order.")]
            string[] variableNames,
            [Description("The model family to tune.")]
            EvaluationFamily family,
            [Description("The hyper-parameters to vary, 1 to 4 distinct ones that apply to the family.")]
            HyperParameter[] parameters,
            [Description("The lower bound of each parameter, in the same order as parameters.")]
            double[] lowerBounds,
            [Description("The upper bound of each parameter, in the same order; each above its lower bound.")]
            double[] upperBounds,
            [Description(
                "Predictor rows: one array of numbers per case, all the same length and in the order of the variable names. NaN marks a missing value. Leave empty when a cache pointer is given.")]
            double[][]? inputs = null,
            [Description(
                "The outcome for each row, in the same order as the rows: 1 for the event of interest and 0 otherwise. Leave empty when a cache pointer is given.")]
            int[]? labels = null,
            [Description(
                "For each parameter, whether to sample on a log scale (needs a lower bound above 0). Empty samples all uniformly.")]
            bool[]? logScale = null,
            [Description("Number of random settings to try, 2 to 64. Default 20.")]
            int trials = 20,
            [Description(
                "Hyper-parameters to hold fixed for every trial, in the same order as fixedValues. Empty keeps the family defaults.")]
            HyperParameter[]? fixedParameters = null,
            [Description("The value for each entry of fixedParameters, in the same order.")]
            double[]? fixedValues = null,
            [Description(
                "The score to rank by: RocAuc, AveragePrecision, F1, MatthewsCorrelation (higher is better), LogLoss or BrierScore (lower is better). Default RocAuc.")]
            SelectionMetric metric = SelectionMetric.RocAuc,
            [Description("Number of stratified cross validation folds, 2 to 10. Default 5.")]
            int folds = 5,
            [Description(
                "Whether to run a nested cross validation for an unbiased estimate of the tuned model. Default false.")]
            bool nested = false,
            [Description("Number of outer folds for the nested evaluation, 2 to 5. Default 3.")]
            int outerFolds = 3,
            [Description("Tree algorithm when the family is DecisionTree: C45, Id3 or Cart. Default C45.")]
            TreeAlgorithm treeAlgorithm = TreeAlgorithm.C45,
            [Description("Kernel when the family is KernelSupportVector. Default Gaussian.")]
            SvmKernel svmKernel = SvmKernel.Gaussian,
            [Description(
                "Kind when the family is NaiveBayes: Gaussian, Categorical or TreeAugmented. Default Gaussian.")]
            NaiveBayesKind naiveBayesKind = NaiveBayesKind.Gaussian,
            [Description("None, BalancedWeights, Oversample or Undersample, applied to training rows in every fit.")]
            ClassImbalanceHandling classImbalance = ClassImbalanceHandling.None,
            [Description(
                "Share of each training part held back to fit the probability calibration, 0 to 0.5. Default 0.2.")]
            double validationFraction = 0.2,
            [Description("Seed that makes the folds and the sampled trials reproducible.")]
            int seed = 1,
            [Description(
                "Pointer to a table in the Advanced Analytics cache holding the predictor columns and a label column. Leave empty when the rows are inline.")]
            Guid? cachePointer = null,
            [Description(
                "Zero based index of the label column in the cached table. Default is the last column; only used with a cache pointer.")]
            int labelColumn = -1,
            CancellationToken cancellationToken = default)
        {
            SelectionToolInput.ValidateSearch(folds, metric);

            var (resolvedInputs, resolvedLabels) = await ResolveAsync(inputs, labels, variableNames, cachePointer,
                labelColumn, cancellationToken).ConfigureAwait(false);

            var options = SelectionToolInput.Common(resolvedInputs, resolvedLabels, variableNames, family,
                ModelEvaluationLimits.MaximumSearchRows,
                validationFraction, 0.2, seed, folds, 0);

            ModelEvaluationGuard.IntRange(outerFolds, 2, 5, nameof(outerFolds), "The number of outer folds");

            ModelEvaluationGuard.IntRange(trials, 2, ModelEvaluationLimits.MaximumGridSize, nameof(trials),
                "The number of trials");

            if (parameters == null || parameters.Length < 1 || parameters.Length > 4 ||
                parameters.Distinct().Count() != parameters.Length)
            {
                throw new ArgumentException("Give 1 to 4 distinct hyper-parameters.", nameof(parameters));
            }

            if (lowerBounds == null || upperBounds == null || lowerBounds.Length != parameters.Length ||
                upperBounds.Length != parameters.Length)
            {
                throw new ArgumentException("Give one lower and one upper bound for every parameter.",
                    nameof(lowerBounds));
            }

            var scale = logScale is { Length: > 0 } ? logScale : new bool[parameters.Length];

            if (scale.Length != parameters.Length)
            {
                throw new ArgumentException("Give one log-scale flag for every parameter, or none.", nameof(logScale));
            }

            for (var i = 0; i < parameters.Length; i++)
            {
                if (!double.IsFinite(lowerBounds[i]) || !double.IsFinite(upperBounds[i]) ||
                    upperBounds[i] <= lowerBounds[i])
                {
                    throw new ArgumentException($"The upper bound of {parameters[i]} must be above its lower bound.",
                        nameof(upperBounds));
                }

                if (scale[i] && lowerBounds[i] <= 0)
                {
                    throw new ArgumentException(
                        $"{parameters[i]} is sampled on a log scale, so its lower bound must be above 0.",
                        nameof(lowerBounds));
                }
            }

            var baseConfig = SelectionToolInput.Config(family, fixedParameters, fixedValues, classImbalance,
                treeAlgorithm,
                svmKernel, naiveBayesKind);

            foreach (var parameter in parameters)
            {
                if (!LearnerConfig.Supported(baseConfig.Family).Contains(parameter))
                {
                    throw new ArgumentException(
                        $"{parameter} does not apply to {baseConfig.Family}; the supported ones are {string.Join(", ", LearnerConfig.Supported(baseConfig.Family))}.",
                        nameof(parameters));
                }
            }

            var set = SearchEngine.Random(baseConfig, parameters, lowerBounds, upperBounds, scale, trials, seed);

            return SearchEngine.Run(baseConfig.Family.ToString(), "Random", set, resolvedInputs, resolvedLabels,
                variableNames, options, metric, nested, outerFolds, cancellationToken);
        }

        [AdvancedAnalyticsTool("ComputeLearningCurve")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Shows how a model's training and held-out score change as it is given more training rows, by cross validation at several training fractions, and diagnoses the result: high variance (large gap, held-out score still rising, so more data helps), overfitting (large gap that has flattened), high bias (both curves converge at a poor score, so the model or features are the limit) or converged (more data will not help). " +
            "Use it to decide whether collecting or labelling more data is worth it, and how small a sample can be used. Each point averages the folds and reports the mean number of training rows. Fractions that leave fewer than 12 rows or a missing class are skipped with a warning. " +
            FamilyHelp + Bounds + PointerHelp)]
        public async Task<EvaluationCurveResult> ComputeLearningCurveAsync(
            [Description("The name of each predictor column, unique and in column order.")]
            string[] variableNames,
            [Description(
                "Predictor rows: one array of numbers per case, all the same length and in the order of the variable names. NaN marks a missing value. Leave empty when a cache pointer is given.")]
            double[][]? inputs = null,
            [Description(
                "The outcome for each row, in the same order as the rows: 1 for the event of interest and 0 otherwise. Leave empty when a cache pointer is given.")]
            int[]? labels = null,
            [Description("The model family to assess.")]
            EvaluationFamily family = EvaluationFamily.GradientBoosting,
            [Description(
                "The fractions of each training part to train on, 2 to 10 numbers above 0 and at most 1. Empty uses 0.1, 0.25, 0.5, 0.75 and 1.")]
            double[]? trainingFractions = null,
            [Description(
                "Hyper-parameters to set on the model, in the same order as fixedValues. Empty keeps the family defaults.")]
            HyperParameter[]? fixedParameters = null,
            [Description("The value for each entry of fixedParameters, in the same order.")]
            double[]? fixedValues = null,
            [Description(
                "The score to plot: RocAuc, AveragePrecision, F1, MatthewsCorrelation (higher is better), LogLoss or BrierScore (lower is better). Default RocAuc.")]
            SelectionMetric metric = SelectionMetric.RocAuc,
            [Description("Number of stratified cross validation folds, 2 to 10. Default 4.")]
            int folds = 4,
            [Description("Tree algorithm when the family is DecisionTree: C45, Id3 or Cart. Default C45.")]
            TreeAlgorithm treeAlgorithm = TreeAlgorithm.C45,
            [Description("Kernel when the family is KernelSupportVector. Default Gaussian.")]
            SvmKernel svmKernel = SvmKernel.Gaussian,
            [Description(
                "Kind when the family is NaiveBayes: Gaussian, Categorical or TreeAugmented. Default Gaussian.")]
            NaiveBayesKind naiveBayesKind = NaiveBayesKind.Gaussian,
            [Description("None, BalancedWeights, Oversample or Undersample, applied to training rows in every fit.")]
            ClassImbalanceHandling classImbalance = ClassImbalanceHandling.None,
            [Description(
                "Share of each training subset held back to fit the probability calibration, 0 to 0.5. Default 0.2.")]
            double validationFraction = 0.2,
            [Description("Seed that makes the folds and subsamples reproducible.")]
            int seed = 1,
            [Description(
                "Pointer to a table in the Advanced Analytics cache holding the predictor columns and a label column. Leave empty when the rows are inline.")]
            Guid? cachePointer = null,
            [Description(
                "Zero based index of the label column in the cached table. Default is the last column; only used with a cache pointer.")]
            int labelColumn = -1,
            CancellationToken cancellationToken = default)
        {
            SelectionToolInput.ValidateSearch(folds, metric);

            var (resolvedInputs, resolvedLabels) = await ResolveAsync(inputs, labels, variableNames, cachePointer,
                labelColumn, cancellationToken).ConfigureAwait(false);

            var options = SelectionToolInput.Common(resolvedInputs, resolvedLabels, variableNames, family,
                ModelEvaluationLimits.MaximumSearchRows,
                validationFraction, 0.2, seed, folds, 0);

            var fractions = trainingFractions is { Length: > 0 }
                ? trainingFractions.Distinct().OrderBy(f => f).ToArray()
                : [0.1, 0.25, 0.5, 0.75, 1d];

            if (fractions.Length < 2 || fractions.Length > 10 ||
                fractions.Any(f => !double.IsFinite(f) || f <= 0 || f > 1))
            {
                throw new ArgumentException("Give 2 to 10 training fractions above 0 and at most 1.",
                    nameof(trainingFractions));
            }

            if (folds * fractions.Length > ModelEvaluationLimits.MaximumTotalFits)
            {
                throw new ArgumentException("Too many fits requested.", nameof(folds));
            }

            var config = SelectionToolInput.Config(family, fixedParameters, fixedValues, classImbalance, treeAlgorithm,
                svmKernel, naiveBayesKind);

            return CurveEngine.Learning(config.Family, config, fractions, resolvedInputs, resolvedLabels,
                variableNames, options, metric, cancellationToken);
        }

        [AdvancedAnalyticsTool("ComputeValidationCurve")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Shows how a model's training and held-out score change as one hyper-parameter is varied, by stratified cross validation at each value, and diagnoses where the model underfits, where it overfits and where the held-out optimum lies. " +
            "Read it as a picture of model flexibility: a training score that keeps rising while the held-out score falls means overfitting; both low means underfitting. A BestAtEdgeOfGrid warning means the optimum touches the end of the values you tried, so extend them. Use GridSearchHyperParameters when two parameters interact. " +
            FamilyHelp + Bounds + PointerHelp)]
        public async Task<EvaluationCurveResult> ComputeValidationCurveAsync(
            [Description("The name of each predictor column, unique and in column order.")]
            string[] variableNames,
            [Description("The model family to assess.")]
            EvaluationFamily family,
            [Description("The hyper-parameter to vary; it must apply to the family.")]
            HyperParameter parameter,
            [Description("The values to try, 2 to 16 numbers.")]
            double[] values,
            [Description(
                "Predictor rows: one array of numbers per case, all the same length and in the order of the variable names. NaN marks a missing value. Leave empty when a cache pointer is given.")]
            double[][]? inputs = null,
            [Description(
                "The outcome for each row, in the same order as the rows: 1 for the event of interest and 0 otherwise. Leave empty when a cache pointer is given.")]
            int[]? labels = null,
            [Description(
                "Other hyper-parameters to hold fixed, in the same order as fixedValues. Empty keeps the family defaults.")]
            HyperParameter[]? fixedParameters = null,
            [Description("The value for each entry of fixedParameters, in the same order.")]
            double[]? fixedValues = null,
            [Description(
                "The score to plot: RocAuc, AveragePrecision, F1, MatthewsCorrelation (higher is better), LogLoss or BrierScore (lower is better). Default RocAuc.")]
            SelectionMetric metric = SelectionMetric.RocAuc,
            [Description("Number of stratified cross validation folds, 2 to 10. Default 5.")]
            int folds = 5,
            [Description("Tree algorithm when the family is DecisionTree: C45, Id3 or Cart. Default C45.")]
            TreeAlgorithm treeAlgorithm = TreeAlgorithm.C45,
            [Description("Kernel when the family is KernelSupportVector. Default Gaussian.")]
            SvmKernel svmKernel = SvmKernel.Gaussian,
            [Description(
                "Kind when the family is NaiveBayes: Gaussian, Categorical or TreeAugmented. Default Gaussian.")]
            NaiveBayesKind naiveBayesKind = NaiveBayesKind.Gaussian,
            [Description("None, BalancedWeights, Oversample or Undersample, applied to training rows in every fit.")]
            ClassImbalanceHandling classImbalance = ClassImbalanceHandling.None,
            [Description(
                "Share of each training part held back to fit the probability calibration, 0 to 0.5. Default 0.2.")]
            double validationFraction = 0.2,
            [Description("Seed that makes the folds reproducible.")]
            int seed = 1,
            [Description(
                "Pointer to a table in the Advanced Analytics cache holding the predictor columns and a label column. Leave empty when the rows are inline.")]
            Guid? cachePointer = null,
            [Description(
                "Zero based index of the label column in the cached table. Default is the last column; only used with a cache pointer.")]
            int labelColumn = -1,
            CancellationToken cancellationToken = default)
        {
            SelectionToolInput.ValidateSearch(folds, metric);

            var (resolvedInputs, resolvedLabels) = await ResolveAsync(inputs, labels, variableNames, cachePointer,
                labelColumn, cancellationToken).ConfigureAwait(false);

            var options = SelectionToolInput.Common(resolvedInputs, resolvedLabels, variableNames, family,
                ModelEvaluationLimits.MaximumSearchRows,
                validationFraction, 0.2, seed, folds, 0);

            if (values == null || values.Length < 2 || values.Length > ModelEvaluationLimits.MaximumSweepValues)
            {
                throw new ArgumentException(
                    $"Give 2 to {ModelEvaluationLimits.MaximumSweepValues} values.", nameof(values));
            }

            var config = SelectionToolInput.Config(family, fixedParameters, fixedValues, classImbalance, treeAlgorithm,
                svmKernel, naiveBayesKind);

            if (!LearnerConfig.Supported(config.Family).Contains(parameter))
            {
                throw new ArgumentException(
                    $"{parameter} does not apply to {config.Family}; the supported ones are {string.Join(", ", LearnerConfig.Supported(config.Family))}.",
                    nameof(parameter));
            }

            return CurveEngine.Validation(config.Family, config, parameter, values.Distinct().ToArray(),
                resolvedInputs, resolvedLabels, variableNames, options, metric, cancellationToken);
        }

        [AdvancedAnalyticsTool("ComputePermutationImportance")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Measures how much each variable matters to any classification model family by shuffling it in the held-out rows and recording how much the chosen score falls (ROC AUC by default), averaged over repeats and, optionally, over cross validation folds, with the standard deviation across shuffles. " +
            "It is model agnostic and measured on unseen data, so it shows what generalises, unlike coefficient sizes or split counts. A variable whose importance is within its own noise of zero (listed as negligible) can be dropped; correlated variables share importance so each can look weak. Importance can be negative when shuffling a useless variable happens to help. " +
            "The model is fitted on training rows only (with calibration on validation rows) and nothing is saved. " +
            FamilyHelp +
            "Bounds: at most 20000 rows (4000 for KernelSupportVector) and 200 predictor columns; the shuffling budget limits the rows used per fold." +
            PointerHelp)]
        public async Task<PermutationImportanceResult> ComputePermutationImportanceAsync(
            [Description("The name of each predictor column, unique and in column order.")]
            string[] variableNames,
            [Description(
                "Predictor rows: one array of numbers per case, all the same length and in the order of the variable names. NaN marks a missing value. Leave empty when a cache pointer is given.")]
            double[][]? inputs = null,
            [Description(
                "The outcome for each row, in the same order as the rows: 1 for the event of interest and 0 otherwise. Leave empty when a cache pointer is given.")]
            int[]? labels = null,
            [Description("The model family to fit.")]
            EvaluationFamily family = EvaluationFamily.RandomForest,
            [Description(
                "Hyper-parameters to set on the model, in the same order as fixedValues. Empty keeps the family defaults.")]
            HyperParameter[]? fixedParameters = null,
            [Description("The value for each entry of fixedParameters, in the same order.")]
            double[]? fixedValues = null,
            [Description("The score whose fall is measured: RocAuc, AveragePrecision or LogLoss. Default RocAuc.")]
            SelectionMetric metric = SelectionMetric.RocAuc,
            [Description("How many times each variable is shuffled, 1 to 10. Default 5.")]
            int repeats = 5,
            [Description(
                "0 uses one train, validation and test split; 2 to 10 averages over stratified cross validation folds. Default 0.")]
            int crossValidationFolds = 0,
            [Description("Tree algorithm when the family is DecisionTree: C45, Id3 or Cart. Default C45.")]
            TreeAlgorithm treeAlgorithm = TreeAlgorithm.C45,
            [Description("Kernel when the family is KernelSupportVector. Default Gaussian.")]
            SvmKernel svmKernel = SvmKernel.Gaussian,
            [Description(
                "Kind when the family is NaiveBayes: Gaussian, Categorical or TreeAugmented. Default Gaussian.")]
            NaiveBayesKind naiveBayesKind = NaiveBayesKind.Gaussian,
            [Description("None, BalancedWeights, Oversample or Undersample, applied to training rows in every fit.")]
            ClassImbalanceHandling classImbalance = ClassImbalanceHandling.None,
            [Description("Share of rows held back to fit the probability calibration, 0 to 0.5. Default 0.2.")]
            double validationFraction = 0.2,
            [Description(
                "Share of rows held back as the importance test rows, above 0 and at most 0.5. Ignored with cross validation. Default 0.3.")]
            double testFraction = 0.3,
            [Description("Seed that makes the splits and the shuffles reproducible.")]
            int seed = 1,
            [Description(
                "Pointer to a table in the Advanced Analytics cache holding the predictor columns and a label column. Leave empty when the rows are inline.")]
            Guid? cachePointer = null,
            [Description(
                "Zero based index of the label column in the cached table. Default is the last column; only used with a cache pointer.")]
            int labelColumn = -1,
            CancellationToken cancellationToken = default)
        {
            if (metric is not (SelectionMetric.RocAuc or SelectionMetric.AveragePrecision or SelectionMetric.LogLoss))
            {
                throw new ArgumentException("The metric must be RocAuc, AveragePrecision or LogLoss.", nameof(metric));
            }

            ModelEvaluationGuard.IntRange(repeats, 1, ModelEvaluationLimits.MaximumPermutationRepeats, nameof(repeats),
                "The number of repeats");

            var (resolvedInputs, resolvedLabels) = await ResolveAsync(inputs, labels, variableNames, cachePointer,
                labelColumn, cancellationToken).ConfigureAwait(false);

            var options = SelectionToolInput.Common(resolvedInputs, resolvedLabels, variableNames, family,
                ModelEvaluationLimits.MaximumRows,
                validationFraction, testFraction, seed, crossValidationFolds, repeats);

            var config = SelectionToolInput.Config(family, fixedParameters, fixedValues, classImbalance, treeAlgorithm,
                svmKernel, naiveBayesKind);

            var plan = BinaryEvaluationEngine.Plan(resolvedLabels, options);
            var runs = new List<BinaryRun>();

            for (var f = 0; f < plan.Count; f++)
            {
                var runSeed = options.Folds >= 2 ? SeededLibrary.Derive(options.Seed, 31 * (f + 1)) : options.Seed;

                runs.Add(BinaryEvaluationEngine.RunOne([config], [config.Name], resolvedInputs, resolvedLabels,
                    variableNames, plan[f], options, runSeed, cancellationToken));
            }

            var higher = ClassificationMetricsBuilder.HigherIsBetter(metric);
            var drops = variableNames.Select(_ => new List<double>()).ToArray();
            var baselines = new List<double>();

            for (var f = 0; f < runs.Count; f++)
            {
                var run = runs[f];

                var take = PermutationImportance.RowsFor(run.TestRows.Length, variableNames.Length, repeats,
                    runs.Count);

                var chosen = run.TestRows.Take(take).ToArray();
                var rows = DataSplit.Take(resolvedInputs, chosen);
                var y = DataSplit.Take(resolvedLabels, chosen);

                if (y.All(v => v == 1) || y.All(v => v == 0))
                {
                    continue;
                }

                double Score(double[] probabilities)
                {
                    return metric switch
                    {
                        SelectionMetric.AveragePrecision => BinaryMetrics.AveragePrecision(probabilities, y),
                        SelectionMetric.LogLoss => BinaryMetrics.LogLoss(probabilities, y),
                        _ => BinaryMetrics.RocAuc(probabilities, y)
                    };
                }

                var baseline = PermutationImportance.Permute(run.Predict, rows, Score, higher, repeats,
                    SeededLibrary.Derive(seed, 5000 + f), drops, cancellationToken);

                if (double.IsFinite(baseline))
                {
                    baselines.Add(baseline);
                }
            }

            return ImportanceReport.Build(config.Family.ToString(), metric.ToString(), seed, repeats,
                options.Folds >= 2 ? options.Folds : 1, baselines, variableNames, drops,
                runs.SelectMany(r => r.Warnings).ToList(), metric.ToString(),
                higher ? "held-out score falls" : "held-out loss rises");
        }

        [AdvancedAnalyticsTool("ComputePartialDependence")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Shows how a classification model's predicted probability changes as one variable moves across its range while every other variable keeps its real values (partial dependence), with the 10th, 50th and 90th percentile of the individual probabilities at each grid point (the spread of individual conditional expectations). " +
            "It reveals the shape the model learned for a variable: increasing, decreasing, flat or non-monotone (a threshold or interaction). A wide band around a flat mean means the effect depends on other variables. Use it after ComputePermutationImportance identifies the variables worth examining; the shape of an unimportant variable means little. " +
            "The model is fitted on training rows with calibration on validation rows, and the curve is averaged over up to 500 held-out rows across the 5th to 95th percentile of the variable using up to 50 grid points. Nothing is saved. " +
            FamilyHelp + "Bounds: at most 20000 rows (4000 for KernelSupportVector) and 200 predictor columns." +
            PointerHelp)]
        public async Task<PartialDependenceResult> ComputePartialDependenceAsync(
            [Description("The name of each predictor column, unique and in column order.")]
            string[] variableNames,
            [Description("The name of the predictor to vary; it must be one of the variable names.")]
            string variable,
            [Description(
                "Predictor rows: one array of numbers per case, all the same length and in the order of the variable names. NaN marks a missing value. Leave empty when a cache pointer is given.")]
            double[][]? inputs = null,
            [Description(
                "The outcome for each row, in the same order as the rows: 1 for the event of interest and 0 otherwise. Leave empty when a cache pointer is given.")]
            int[]? labels = null,
            [Description("The model family to fit.")]
            EvaluationFamily family = EvaluationFamily.GradientBoosting,
            [Description(
                "Hyper-parameters to set on the model, in the same order as fixedValues. Empty keeps the family defaults.")]
            HyperParameter[]? fixedParameters = null,
            [Description("The value for each entry of fixedParameters, in the same order.")]
            double[]? fixedValues = null,
            [Description("Number of grid points between the 5th and 95th percentile, 2 to 50. Default 20.")]
            int gridPoints = 20,
            [Description("Tree algorithm when the family is DecisionTree: C45, Id3 or Cart. Default C45.")]
            TreeAlgorithm treeAlgorithm = TreeAlgorithm.C45,
            [Description("Kernel when the family is KernelSupportVector. Default Gaussian.")]
            SvmKernel svmKernel = SvmKernel.Gaussian,
            [Description(
                "Kind when the family is NaiveBayes: Gaussian, Categorical or TreeAugmented. Default Gaussian.")]
            NaiveBayesKind naiveBayesKind = NaiveBayesKind.Gaussian,
            [Description("None, BalancedWeights, Oversample or Undersample, applied to training rows.")]
            ClassImbalanceHandling classImbalance = ClassImbalanceHandling.None,
            [Description("Share of rows held back to fit the probability calibration, 0 to 0.5. Default 0.2.")]
            double validationFraction = 0.2,
            [Description(
                "Share of rows held back as the rows the curve is averaged over, above 0 and at most 0.5. Default 0.3.")]
            double testFraction = 0.3,
            [Description("Seed that makes the split and the row sample reproducible.")]
            int seed = 1,
            [Description(
                "Pointer to a table in the Advanced Analytics cache holding the predictor columns and a label column. Leave empty when the rows are inline.")]
            Guid? cachePointer = null,
            [Description(
                "Zero based index of the label column in the cached table. Default is the last column; only used with a cache pointer.")]
            int labelColumn = -1,
            CancellationToken cancellationToken = default)
        {
            var (resolvedInputs, resolvedLabels) = await ResolveAsync(inputs, labels, variableNames, cachePointer,
                labelColumn, cancellationToken).ConfigureAwait(false);

            var options = SelectionToolInput.Common(resolvedInputs, resolvedLabels, variableNames, family,
                ModelEvaluationLimits.MaximumRows,
                validationFraction, testFraction, seed, 0, 0);

            ModelEvaluationGuard.IntRange(gridPoints, 2, ModelEvaluationLimits.MaximumPartialDependencePoints,
                nameof(gridPoints), "The number of grid points");

            if (string.IsNullOrWhiteSpace(variable) || !variableNames.Contains(variable))
            {
                throw new ArgumentException($"Variable '{variable}' is not one of the variable names.",
                    nameof(variable));
            }

            var config = SelectionToolInput.Config(family, fixedParameters, fixedValues, classImbalance, treeAlgorithm,
                svmKernel, naiveBayesKind);

            var split = BinaryEvaluationEngine.Plan(resolvedLabels, options)[0];

            var run = BinaryEvaluationEngine.RunOne([config], [config.Name], resolvedInputs, resolvedLabels,
                variableNames, split, options, seed, cancellationToken);

            return PartialDependenceEngine.Compute(config.Family.ToString(), run.Predict,
                DataSplit.Take(resolvedInputs, run.TrainRows),
                DataSplit.Take(resolvedInputs, run.TestRows), variableNames, variable, gridPoints, seed,
                "probability", run.Warnings);
        }

        [AdvancedAnalyticsTool("CompareCalibrationMethods")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Fits a classification model and compares how well four ways of turning its scores into probabilities match reality on held-out rows: the uncalibrated output, Platt scaling (a logistic curve, robust on small samples), isotonic regression (a free-form monotone curve, flexible but prone to overfit small calibration sets) and a constant base rate as the no-skill reference. " +
            "For each it returns log loss, Brier score, expected calibration error, ROC AUC, and the calibration slope and intercept (ideal slope 1 and intercept 0; a slope below 1 means overconfident, above 1 underconfident). The best method by log loss is named. Calibration is fitted on the validation rows only and every figure comes from the untouched test rows. " +
            "Calibration changes probabilities, not ranking: ROC AUC only moves for isotonic because it can create ties. Calibration matters most when probabilities are used for thresholds, expected loss or alert budgets. " +
            FamilyHelp +
            "Bounds: at most 20000 rows (4000 for KernelSupportVector) and 200 predictor columns. Nothing is saved.")]
        public CalibrationComparisonResult CompareCalibrationMethods(
            [Description(
                "Predictor rows: one array of numbers per case, all the same length and in the order of the variable names. NaN marks a missing value.")]
            double[][] inputs,
            [Description(
                "The outcome for each row, in the same order as the rows: 1 for the event of interest and 0 otherwise.")]
            int[] labels,
            [Description("The name of each predictor column, unique and in column order.")]
            string[] variableNames,
            [Description("The model family to fit.")]
            EvaluationFamily family = EvaluationFamily.RandomForest,
            [Description(
                "Hyper-parameters to set on the model, in the same order as fixedValues. Empty keeps the family defaults.")]
            HyperParameter[]? fixedParameters = null,
            [Description("The value for each entry of fixedParameters, in the same order.")]
            double[]? fixedValues = null,
            [Description("Tree algorithm when the family is DecisionTree: C45, Id3 or Cart. Default C45.")]
            TreeAlgorithm treeAlgorithm = TreeAlgorithm.C45,
            [Description("Kernel when the family is KernelSupportVector. Default Gaussian.")]
            SvmKernel svmKernel = SvmKernel.Gaussian,
            [Description(
                "Kind when the family is NaiveBayes: Gaussian, Categorical or TreeAugmented. Default Gaussian.")]
            NaiveBayesKind naiveBayesKind = NaiveBayesKind.Gaussian,
            [Description("None, BalancedWeights, Oversample or Undersample, applied to training rows.")]
            ClassImbalanceHandling classImbalance = ClassImbalanceHandling.None,
            [Description("Share of rows used to fit the calibrators, above 0 and at most 0.5. Default 0.3.")]
            double validationFraction = 0.3,
            [Description(
                "Share of rows held back purely to measure calibration, above 0 and at most 0.5. Default 0.3.")]
            double testFraction = 0.3,
            [Description("Seed that makes the splits reproducible.")]
            int seed = 1,
            CancellationToken cancellationToken = default)
        {
            if (validationFraction <= 0)
            {
                throw new ArgumentException("The validation fraction must be above 0 to fit a calibrator.",
                    nameof(validationFraction));
            }

            var options = SelectionToolInput.Common(inputs, labels, variableNames, family,
                ModelEvaluationLimits.MaximumRows,
                validationFraction, testFraction, seed, 0, 0);

            var config = SelectionToolInput.Config(family, fixedParameters, fixedValues, classImbalance, treeAlgorithm,
                svmKernel, naiveBayesKind);

            var split = BinaryEvaluationEngine.Plan(labels, options)[0];

            var run = BinaryEvaluationEngine.RunOne([config], [config.Name], inputs, labels, variableNames, split,
                options with { Calibrate = false }, seed, cancellationToken);

            var comparison = CalibrationComparer.Compare(run.ValidationRaw, run.ValidationLabels, run.TestRaw,
                run.TestLabels,
                run.RawIsProbability);

            var best = comparison.Best;
            var uncalibrated = comparison.Methods[0];
            var chosen = comparison.Methods.FirstOrDefault(m => m.Method == best);

            var text =
                $"On {run.TestLabels.Length} held-out rows the best method by log loss is {best} ({chosen?.LogLoss:0.####}) against {uncalibrated.LogLoss:0.####} uncalibrated. " +
                (uncalibrated.CalibrationSlope is { } slope
                    ? $"The uncalibrated calibration slope is {slope:0.###}: {(slope < 0.9 ? "the raw probabilities are overconfident" : slope > 1.1 ? "the raw probabilities are underconfident" : "the raw probabilities are already close to calibrated")}."
                    : string.Empty);

            return new CalibrationComparisonResult
            {
                Family = config.Family.ToString(),
                Seed = seed,
                CalibrationRows = run.ValidationLabels.Length,
                TestRows = run.TestLabels.Length,
                TestPositiveRate = run.TestLabels.Length == 0 ? 0d : run.TestLabels.Average(v => (double)v),
                Methods = comparison.Methods,
                BestMethod = best,
                Warnings = run.Warnings.Concat(comparison.Warnings).ToArray(),
                Interpretation = text
            };
        }

        [AdvancedAnalyticsTool("CompareScoreCalibrations")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [Description(
            "Compares ways of turning existing model scores into probabilities without fitting any model: the rows are split by seed into a calibration part and a test part, Platt scaling and isotonic regression are fitted on the calibration part only, and the uncalibrated scores, Platt, isotonic and a constant base rate are compared on the test part by log loss, Brier score, expected calibration error, ROC AUC and calibration slope and intercept. " +
            "Use it to check scores from any external or earlier model, for example a rules engine score or a vendor risk score, before treating them as probabilities. Set scoresAreProbabilities to true when the scores already lie in 0 to 1; otherwise they are read as log-odds style scores and squashed with a sigmoid for the uncalibrated row. " +
            "Bounds: 30 to 20000 rows, each class needing at least 8 rows.")]
        public CalibrationComparisonResult CompareScoreCalibrations(
            [Description("The model score of each row, one number per row, all finite.")]
            double[] scores,
            [Description(
                "The outcome for each row, in the same order as the scores: 1 for the event of interest and 0 otherwise.")]
            int[] labels,
            [Description(
                "True when the scores are already probabilities between 0 and 1; false when they are unbounded scores. Default true.")]
            bool scoresAreProbabilities = true,
            [Description(
                "Share of rows used to fit the calibrators, 0.1 to 0.9; the rest measure calibration. Default 0.5.")]
            double calibrationFraction = 0.5,
            [Description("Seed that makes the split reproducible.")]
            int seed = 1)
        {
            if (scores == null || scores.Length < 30 || scores.Length > ModelEvaluationLimits.MaximumRows)
            {
                throw new ArgumentException($"Give between 30 and {ModelEvaluationLimits.MaximumRows} scores.",
                    nameof(scores));
            }

            if (scores.Any(s => !double.IsFinite(s)))
            {
                throw new ArgumentException("Every score must be finite.", nameof(scores));
            }

            if (scoresAreProbabilities && scores.Any(s => s < 0 || s > 1))
            {
                throw new ArgumentException("Scores flagged as probabilities must lie between 0 and 1.",
                    nameof(scores));
            }

            if (labels == null || labels.Length != scores.Length || labels.Any(l => l != 0 && l != 1))
            {
                throw new ArgumentException("There must be one 0 or 1 label per score.", nameof(labels));
            }

            if (labels.Count(l => l == 1) < 8 || labels.Count(l => l == 0) < 8)
            {
                throw new ArgumentException("Each class needs at least 8 rows.", nameof(labels));
            }

            ModelEvaluationGuard.DoubleRange(calibrationFraction, 0.1, 0.9, nameof(calibrationFraction),
                "The calibration fraction");

            var split = DataSplit.Split(labels.Select(l => (double)l).ToArray(), calibrationFraction, 0d,
                1d - calibrationFraction, seed);

            var calibrationScores = DataSplit.Take(scores, split.Train);
            var calibrationLabels = DataSplit.Take(labels, split.Train);
            var testScores = DataSplit.Take(scores, split.Test);
            var testLabels = DataSplit.Take(labels, split.Test);

            var comparison = CalibrationComparer.Compare(calibrationScores, calibrationLabels, testScores, testLabels,
                scoresAreProbabilities);

            var uncalibrated = comparison.Methods[0];
            var chosen = comparison.Methods.FirstOrDefault(m => m.Method == comparison.Best);

            var text =
                $"On {testLabels.Length} held-out rows the best method by log loss is {comparison.Best} ({chosen?.LogLoss:0.####}) against {uncalibrated.LogLoss:0.####} for the raw scores. " +
                (uncalibrated.CalibrationSlope is { } slope
                    ? $"The raw calibration slope is {slope:0.###}: {(slope < 0.9 ? "the raw scores are overconfident" : slope > 1.1 ? "the raw scores are underconfident" : "the raw scores are already close to calibrated")}."
                    : string.Empty);

            return new CalibrationComparisonResult
            {
                Family = "ExternalScores",
                Seed = seed,
                CalibrationRows = calibrationLabels.Length,
                TestRows = testLabels.Length,
                TestPositiveRate = testLabels.Length == 0 ? 0d : testLabels.Average(v => (double)v),
                Methods = comparison.Methods,
                BestMethod = comparison.Best,
                Warnings = comparison.Warnings.ToArray(),
                Interpretation = text
            };
        }

        [AdvancedAnalyticsTool("CompareModels")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Runs several classification families on identical stratified cross validation folds and ranks them, returning for each the mean held-out score with its spread across folds, the training to held-out gap, the paired difference to the best, whether it is statistically indistinguishable from the best, the pooled held-out ROC AUC, average precision, log loss, Brier score and F1, and a plain-language recommendation. " +
            "Two families are called indistinguishable when their fold-by-fold difference is within one standard error, and the recommendation then prefers the simplest of them (logistic regression, naive Bayes, a single tree, then forests, boosting and neural networks), because a simpler model is easier to explain, cheaper and drifts less. Logistic regression is included by calling the same implementation as TrainLogisticRegression. " +
            "Every family runs at sensible default settings on the same folds and seeds, with calibration on a validation part inside each fold; tune the winner with GridSearchHyperParameters afterwards. Leave families empty to compare all twelve. " +
            "Bounds: at most 10000 rows (kernel support vector machines are skipped above 4000), 200 predictor columns, 2 to 10 folds and 12 families. Nothing is saved." +
            PointerHelp)]
        public async Task<ModelComparisonResult> CompareModelsAsync(
            [Description("The name of each predictor column, unique and in column order.")]
            string[] variableNames,
            [Description(
                "Predictor rows: one array of numbers per case, all the same length and in the order of the variable names. NaN marks a missing value. Leave empty when a cache pointer is given.")]
            double[][]? inputs = null,
            [Description(
                "The outcome for each row, in the same order as the rows: 1 for the event of interest and 0 otherwise. Leave empty when a cache pointer is given.")]
            int[]? labels = null,
            [Description("The families to compare, at most 12. Empty compares all of them.")]
            EvaluationFamily[]? families = null,
            [Description(
                "The score to rank by: RocAuc, AveragePrecision, F1, MatthewsCorrelation (higher is better), LogLoss or BrierScore (lower is better). Default RocAuc.")]
            SelectionMetric metric = SelectionMetric.RocAuc,
            [Description("Number of stratified cross validation folds shared by every family, 2 to 10. Default 5.")]
            int folds = 5,
            [Description(
                "None, BalancedWeights, Oversample or Undersample, applied to training rows for every family.")]
            ClassImbalanceHandling classImbalance = ClassImbalanceHandling.None,
            [Description(
                "Share of each training part held back to fit the probability calibration, 0 to 0.5. Default 0.2.")]
            double validationFraction = 0.2,
            [Description("Seed that makes the folds and every random choice reproducible; identical for every family.")]
            int seed = 1,
            [Description(
                "Pointer to a table in the Advanced Analytics cache holding the predictor columns and a label column. Leave empty when the rows are inline.")]
            Guid? cachePointer = null,
            [Description(
                "Zero based index of the label column in the cached table. Default is the last column; only used with a cache pointer.")]
            int labelColumn = -1,
            CancellationToken cancellationToken = default)
        {
            SelectionToolInput.ValidateSearch(folds, metric);

            var (resolvedInputs, resolvedLabels) = await ResolveAsync(inputs, labels, variableNames, cachePointer,
                labelColumn, cancellationToken).ConfigureAwait(false);

            var options = ClassificationToolInput.Validate(resolvedInputs, resolvedLabels, variableNames,
                validationFraction, 0.2, seed, folds, null, 0, true, ModelEvaluationLimits.MaximumSearchRows);

            var chosen = families is { Length: > 0 }
                ? families.Distinct().ToArray()
                : Enum.GetValues<EvaluationFamily>();

            if (chosen.Length > ModelEvaluationLimits.MaximumComparedFamilies)
            {
                throw new ArgumentException(
                    $"At most {ModelEvaluationLimits.MaximumComparedFamilies} families can be compared.",
                    nameof(families));
            }

            return ModelComparisonEngine.Compare(chosen, classImbalance, resolvedInputs, resolvedLabels, variableNames,
                options, metric,
                cancellationToken);
        }
    }
}