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
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
    public class ModelEvaluationTools
    {
        private const string Bounds =
            "Bounds: at most 20000 rows, 200 predictor columns, 500 trees, 1000 training epochs; kernel support vector machines are limited to 4000 rows. " +
            "Nothing is saved: the tool fits, evaluates and discards the model.";

        private const string Method =
            "Method: rows are split by seed into train, validation and test sets (stratified by class), or into stratified cross validation folds when crossValidationFolds is 2 or more. " +
            "Missing-value filling and scaling are learned from the training rows only, any class imbalance handling touches the training rows only, the probability calibration (Platt scaling) is fitted on the validation rows only, and every reported metric comes from the untouched held-out rows. " +
            "Permutation importance is the drop in held-out ROC AUC when one variable is shuffled. ";

        [AdvancedAnalyticsTool("EvaluateDecisionTree")]
        [Description(
            "Fits a single decision tree for a 0/1 outcome and evaluates it on held-out rows, returning ROC AUC, average precision, log loss, Brier score, a confusion table, calibration, the readable tree text, the if-then rules and variable importance. " +
            "Use it when the model must be explainable as rules, or as a transparent baseline. Algorithm C45 (information gain ratio on numeric splits), Id3 (numeric predictors are cut into bins first) or Cart (variance-reducing binary splits); maxDepth and minLeafSize limit growth, and Pruning ReducedError collapses branches that do not help on the validation rows. " +
            "Trees overfit easily: compare the training and test ROC AUC and read any TrainTestGap warning before trusting the rules. " +
            Method + Bounds)]
        public ClassifierEvaluationResult EvaluateDecisionTree(
            [Description(
                "Predictor rows: one array of numbers per case, all the same length and in the order of the variable names. NaN marks a missing value.")]
            double[][] inputs,
            [Description(
                "The outcome for each row, in the same order as the rows: 1 for the event of interest and 0 otherwise.")]
            int[] labels,
            [Description("The name of each predictor column, unique and in column order.")]
            string[] variableNames,
            [Description(
                "C45, Id3 or Cart. C45 and Cart split numeric predictors directly; Id3 first cuts each predictor into binCount quantile bins.")]
            TreeAlgorithm algorithm = TreeAlgorithm.C45,
            [Description(
                "Maximum number of splits from the root to a leaf, 1 to 24. Smaller trees are more readable and overfit less. Default 4.")]
            int maxDepth = 4,
            [Description(
                "Minimum training rows in every leaf, 1 to 1000. Larger values give smoother probabilities. Default 10.")]
            int minLeafSize = 10,
            [Description(
                "None keeps the grown tree. ReducedError removes branches that do not lower the squared error on the validation rows.")]
            TreePruning pruning = TreePruning.None,
            [Description(
                "Number of quantile bins per predictor for Id3, 2 to 32. Ignored by the other algorithms. Default 8.")]
            int binCount = 8,
            [Description(
                "Share of rows held back to fit the probability calibration and the threshold, 0 to 0.5. Default 0.2.")]
            double validationFraction = 0.2,
            [Description(
                "Share of rows held back purely to measure quality, above 0 and at most 0.5. Ignored when cross validation is used. Default 0.2.")]
            double testFraction = 0.2,
            [Description(
                "Seed that makes the splits and any random choice reproducible. Reuse a seed to compare models on identical rows.")]
            int seed = 1,
            [Description(
                "0 evaluates on one train, validation and test split; 2 to 10 runs stratified cross validation and pools the held-out predictions of every fold.")]
            int crossValidationFolds = 0,
            [Description(
                "None fits the data as it is. BalancedWeights gives the rarer class more weight; Oversample duplicates rare rows; Undersample drops common rows. Applied to training rows only.")]
            ClassImbalanceHandling classImbalance = ClassImbalanceHandling.None,
            [Description(
                "Whether to fit a Platt calibration on the validation rows so probabilities match observed frequencies. Default true.")]
            bool calibrate = true,
            [Description(
                "Probability at or above which a row counts as positive in the confusion table. Empty chooses the threshold that maximises F1 on the validation rows.")]
            double? decisionThreshold = null,
            [Description(
                "How many times each variable is shuffled to measure permutation importance, 0 to 10; 0 skips importance. Default 3.")]
            int permutationRepeats = 3,
            CancellationToken cancellationToken = default)
        {
            var options = ClassificationToolInput.Validate(inputs, labels, variableNames, validationFraction,
                testFraction,
                seed, crossValidationFolds, decisionThreshold, permutationRepeats, calibrate);

            ModelEvaluationGuard.IntRange(maxDepth, 1, ModelEvaluationLimits.MaximumTreeDepth, nameof(maxDepth),
                "The maximum depth");

            ModelEvaluationGuard.IntRange(minLeafSize, 1, 1000, nameof(minLeafSize), "The minimum leaf size");

            ModelEvaluationGuard.IntRange(binCount, 2, ModelEvaluationLimits.MaximumBins, nameof(binCount),
                "The bin count");

            var config = new LearnerConfig
            {
                Family = EvaluationFamily.DecisionTree,
                Algorithm = algorithm,
                MaxDepth = maxDepth,
                MinLeaf = minLeafSize,
                Pruning = pruning,
                Bins = binCount,
                Imbalance = classImbalance
            };

            return BinaryEvaluationEngine.Evaluate([config], [config.Name], inputs, labels, variableNames, options,
                cancellationToken);
        }

        [AdvancedAnalyticsTool("EvaluateRandomForest")]
        [Description(
            "Fits a random forest or a bagged tree ensemble for a 0/1 outcome and evaluates it on held-out rows, returning ROC AUC, average precision, log loss, Brier score, a confusion table, calibration, the out of bag ROC AUC and variable importance. " +
            "RandomForest grows many deep trees on bootstrap samples and lets each split consider only a random subset of the variables; Bagging lets every split see all variables. Forests are robust defaults for tabular fraud and AML data with interactions and need little tuning. " +
            "The forest score is the average leaf probability, which is then calibrated on the validation rows. A large gap between training and test ROC AUC is normal for forests; judge on the test metrics. " +
            Method + Bounds)]
        public ClassifierEvaluationResult EvaluateRandomForest(
            [Description(
                "Predictor rows: one array of numbers per case, all the same length and in the order of the variable names. NaN marks a missing value.")]
            double[][] inputs,
            [Description(
                "The outcome for each row, in the same order as the rows: 1 for the event of interest and 0 otherwise.")]
            int[] labels,
            [Description("The name of each predictor column, unique and in column order.")]
            string[] variableNames,
            [Description(
                "RandomForest considers a random subset of variables at every split; Bagging considers all of them.")]
            ForestMode mode = ForestMode.RandomForest,
            [Description("Number of trees, 1 to 500. More trees reduce variance and cost time. Default 100.")]
            int treeCount = 100,
            [Description("Maximum depth of every tree, 1 to 24. Default 10.")]
            int maxDepth = 10,
            [Description("Minimum training rows in every leaf, 1 to 1000. Default 3.")]
            int minLeafSize = 3,
            [Description(
                "Share of variables tried at each split for RandomForest, above 0 and at most 1; 0 means the square root of the variable count. Default 0.")]
            double featureFraction = 0,
            [Description("Share of the training rows drawn with replacement for each tree, 0.05 to 1. Default 1.")]
            double sampleFraction = 1,
            [Description(
                "Share of rows held back to fit the probability calibration and the threshold, 0 to 0.5. Default 0.2.")]
            double validationFraction = 0.2,
            [Description(
                "Share of rows held back purely to measure quality, above 0 and at most 0.5. Ignored when cross validation is used. Default 0.2.")]
            double testFraction = 0.2,
            [Description("Seed that makes the splits and every random choice reproducible.")]
            int seed = 1,
            [Description(
                "0 evaluates on one train, validation and test split; 2 to 10 runs stratified cross validation and pools the held-out predictions of every fold.")]
            int crossValidationFolds = 0,
            [Description(
                "None fits the data as it is. BalancedWeights gives the rarer class more weight; Oversample duplicates rare rows; Undersample drops common rows. Applied to training rows only.")]
            ClassImbalanceHandling classImbalance = ClassImbalanceHandling.None,
            [Description(
                "Whether to fit a Platt calibration on the validation rows so probabilities match observed frequencies. Default true.")]
            bool calibrate = true,
            [Description(
                "Probability at or above which a row counts as positive in the confusion table. Empty chooses the threshold that maximises F1 on the validation rows.")]
            double? decisionThreshold = null,
            [Description(
                "How many times each variable is shuffled to measure permutation importance, 0 to 10; 0 skips importance. Default 3.")]
            int permutationRepeats = 3,
            CancellationToken cancellationToken = default)
        {
            var options = ClassificationToolInput.Validate(inputs, labels, variableNames, validationFraction,
                testFraction,
                seed, crossValidationFolds, decisionThreshold, permutationRepeats, calibrate);

            ModelEvaluationGuard.IntRange(treeCount, 1, ModelEvaluationLimits.MaximumTrees, nameof(treeCount),
                "The tree count");
            ModelEvaluationGuard.IntRange(maxDepth, 1, ModelEvaluationLimits.MaximumTreeDepth, nameof(maxDepth),
                "The maximum depth");

            ModelEvaluationGuard.IntRange(minLeafSize, 1, 1000, nameof(minLeafSize), "The minimum leaf size");
            ModelEvaluationGuard.DoubleRange(featureFraction, 0d, 1d, nameof(featureFraction), "The feature fraction");
            ModelEvaluationGuard.DoubleRange(sampleFraction, 0.05, 1d, nameof(sampleFraction), "The sample fraction");

            var config = new LearnerConfig
            {
                Family = mode == ForestMode.Bagging ? EvaluationFamily.Bagging : EvaluationFamily.RandomForest,
                Trees = treeCount,
                MaxDepth = maxDepth,
                MinLeaf = minLeafSize,
                FeatureFraction = featureFraction,
                SampleFraction = sampleFraction,
                Imbalance = classImbalance
            };

            return BinaryEvaluationEngine.Evaluate([config], [config.Name], inputs, labels, variableNames, options,
                cancellationToken);
        }

        [AdvancedAnalyticsTool("EvaluateBoostedTrees")]
        [Description(
            "Fits a boosted tree ensemble for a 0/1 outcome and evaluates it on held-out rows, returning ROC AUC, average precision, log loss, Brier score, a confusion table, calibration and variable importance. " +
            "AdaBoost fits shallow trees in sequence, reweighting the rows the previous ones got wrong; GradientBoosting fits shallow trees to the gradient of the logistic loss with a learning rate. Boosting is usually the most accurate tree method on tabular data but can overfit noisy labels: keep trees shallow, the learning rate small and read the TrainTestGap warning. " +
            "Use ComputeLearningCurve or ComputeValidationCurve on TreeCount to choose how many rounds are enough. " +
            Method + Bounds)]
        public ClassifierEvaluationResult EvaluateBoostedTrees(
            [Description(
                "Predictor rows: one array of numbers per case, all the same length and in the order of the variable names. NaN marks a missing value.")]
            double[][] inputs,
            [Description(
                "The outcome for each row, in the same order as the rows: 1 for the event of interest and 0 otherwise.")]
            int[] labels,
            [Description("The name of each predictor column, unique and in column order.")]
            string[] variableNames,
            [Description("AdaBoost or GradientBoosting.")]
            BoostingAlgorithm algorithm = BoostingAlgorithm.GradientBoosting,
            [Description("Number of boosting rounds, 1 to 500. Default 100.")]
            int treeCount = 100,
            [Description(
                "Depth of each weak tree, 1 to 8; 1 is a decision stump. Default 3 for gradient boosting, 1 to 2 is usual for AdaBoost.")]
            int maxDepth = 3,
            [Description("Minimum training rows in every leaf, 1 to 1000. Default 5.")]
            int minLeafSize = 5,
            [Description(
                "Shrinkage applied to every round, above 0 and at most 1. Smaller values need more rounds. Default 0.1.")]
            double learningRate = 0.1,
            [Description(
                "Share of training rows drawn for each gradient boosting round, 0.05 to 1; below 1 gives stochastic boosting. Ignored by AdaBoost. Default 1.")]
            double sampleFraction = 1,
            [Description(
                "Share of rows held back to fit the probability calibration and the threshold, 0 to 0.5. Default 0.2.")]
            double validationFraction = 0.2,
            [Description(
                "Share of rows held back purely to measure quality, above 0 and at most 0.5. Ignored when cross validation is used. Default 0.2.")]
            double testFraction = 0.2,
            [Description("Seed that makes the splits and every random choice reproducible.")]
            int seed = 1,
            [Description(
                "0 evaluates on one train, validation and test split; 2 to 10 runs stratified cross validation and pools the held-out predictions of every fold.")]
            int crossValidationFolds = 0,
            [Description(
                "None fits the data as it is. BalancedWeights gives the rarer class more weight; Oversample duplicates rare rows; Undersample drops common rows. Applied to training rows only.")]
            ClassImbalanceHandling classImbalance = ClassImbalanceHandling.None,
            [Description(
                "Whether to fit a Platt calibration on the validation rows so probabilities match observed frequencies. Default true.")]
            bool calibrate = true,
            [Description(
                "Probability at or above which a row counts as positive in the confusion table. Empty chooses the threshold that maximises F1 on the validation rows.")]
            double? decisionThreshold = null,
            [Description(
                "How many times each variable is shuffled to measure permutation importance, 0 to 10; 0 skips importance. Default 3.")]
            int permutationRepeats = 3,
            CancellationToken cancellationToken = default)
        {
            var options = ClassificationToolInput.Validate(inputs, labels, variableNames, validationFraction,
                testFraction,
                seed, crossValidationFolds, decisionThreshold, permutationRepeats, calibrate);

            ModelEvaluationGuard.IntRange(treeCount, 1, ModelEvaluationLimits.MaximumTrees, nameof(treeCount),
                "The tree count");

            ModelEvaluationGuard.IntRange(maxDepth, 1, 8, nameof(maxDepth), "The weak tree depth");
            ModelEvaluationGuard.IntRange(minLeafSize, 1, 1000, nameof(minLeafSize), "The minimum leaf size");
            ModelEvaluationGuard.DoubleRange(learningRate, 0.001, 1d, nameof(learningRate), "The learning rate");
            ModelEvaluationGuard.DoubleRange(sampleFraction, 0.05, 1d, nameof(sampleFraction), "The sample fraction");

            var config = new LearnerConfig
            {
                Family = algorithm == BoostingAlgorithm.AdaBoost
                    ? EvaluationFamily.AdaBoost
                    : EvaluationFamily.GradientBoosting,
                Trees = treeCount,
                MaxDepth = maxDepth,
                MinLeaf = minLeafSize,
                LearningRate = learningRate,
                SampleFraction = sampleFraction,
                Imbalance = classImbalance
            };

            return BinaryEvaluationEngine.Evaluate([config], [config.Name], inputs, labels, variableNames, options,
                cancellationToken);
        }

        [AdvancedAnalyticsTool("EvaluateNaiveBayes")]
        [Description(
            "Fits a naive Bayes classifier for a 0/1 outcome and evaluates it on held-out rows, returning ROC AUC, average precision, log loss, Brier score, a confusion table, calibration and variable importance. " +
            "Gaussian models every predictor as a normal distribution within each class; Categorical cuts predictors into quantile bins and counts them with Laplace smoothing; TreeAugmented lets each predictor depend on one other predictor, relaxing the independence assumption. " +
            "Naive Bayes is fast and stable on small samples and gives well ranked but overconfident probabilities, which the Platt calibration on the validation rows corrects. Strongly correlated predictors are counted twice, so check the variable list for near duplicates. " +
            Method + Bounds)]
        public ClassifierEvaluationResult EvaluateNaiveBayes(
            [Description(
                "Predictor rows: one array of numbers per case, all the same length and in the order of the variable names. NaN marks a missing value.")]
            double[][] inputs,
            [Description(
                "The outcome for each row, in the same order as the rows: 1 for the event of interest and 0 otherwise.")]
            int[] labels,
            [Description("The name of each predictor column, unique and in column order.")]
            string[] variableNames,
            [Description("Gaussian, Categorical or TreeAugmented.")]
            NaiveBayesKind kind = NaiveBayesKind.Gaussian,
            [Description(
                "Number of quantile bins per predictor for Categorical, 2 to 32. Ignored by the other kinds. Default 8.")]
            int binCount = 8,
            [Description(
                "Smoothing strength, 0 or more: the variance floor share for Gaussian, the Laplace count for Categorical and TreeAugmented. Empty uses 1e-6 for Gaussian and 1 otherwise.")]
            double? regularisation = null,
            [Description(
                "Share of rows held back to fit the probability calibration and the threshold, 0 to 0.5. Default 0.2.")]
            double validationFraction = 0.2,
            [Description(
                "Share of rows held back purely to measure quality, above 0 and at most 0.5. Ignored when cross validation is used. Default 0.2.")]
            double testFraction = 0.2,
            [Description("Seed that makes the splits and every random choice reproducible.")]
            int seed = 1,
            [Description(
                "0 evaluates on one train, validation and test split; 2 to 10 runs stratified cross validation and pools the held-out predictions of every fold.")]
            int crossValidationFolds = 0,
            [Description(
                "None fits the data as it is. BalancedWeights gives the rarer class more weight; Oversample duplicates rare rows; Undersample drops common rows. Applied to training rows only.")]
            ClassImbalanceHandling classImbalance = ClassImbalanceHandling.None,
            [Description(
                "Whether to fit a Platt calibration on the validation rows so probabilities match observed frequencies. Default true.")]
            bool calibrate = true,
            [Description(
                "Probability at or above which a row counts as positive in the confusion table. Empty chooses the threshold that maximises F1 on the validation rows.")]
            double? decisionThreshold = null,
            [Description(
                "How many times each variable is shuffled to measure permutation importance, 0 to 10; 0 skips importance. Default 3.")]
            int permutationRepeats = 3,
            CancellationToken cancellationToken = default)
        {
            var options = ClassificationToolInput.Validate(inputs, labels, variableNames, validationFraction,
                testFraction,
                seed, crossValidationFolds, decisionThreshold, permutationRepeats, calibrate);

            ModelEvaluationGuard.IntRange(binCount, 2, ModelEvaluationLimits.MaximumBins, nameof(binCount),
                "The bin count");

            if (regularisation.HasValue)
            {
                ModelEvaluationGuard.DoubleRange(regularisation.Value, 0d, 1e6, nameof(regularisation),
                    "The regularisation");
            }

            var config = new LearnerConfig
            {
                Family = kind == NaiveBayesKind.TreeAugmented
                    ? EvaluationFamily.TreeAugmentedNaiveBayes
                    : EvaluationFamily.NaiveBayes,
                BayesKind = kind,
                Bins = binCount,
                Regularisation = regularisation ?? -1d,
                Imbalance = classImbalance
            };

            return BinaryEvaluationEngine.Evaluate([config], [config.Name], inputs, labels, variableNames, options,
                cancellationToken);
        }

        [AdvancedAnalyticsTool("EvaluateKNearestNeighbours")]
        [Description(
            "Fits a k nearest neighbours classifier for a 0/1 outcome over a sweep of k and evaluates the chosen k on held-out rows, returning a sweep table (training and validation ROC AUC for every k), ROC AUC, average precision, log loss, calibration and variable importance. " +
            "Each row is scored by the share of positives among its k closest training rows on standardised predictors, so it needs no training but is sensitive to irrelevant or badly scaled variables and slow on large data. Small k has low bias and high variance; large k smooths. " +
            "The best k is chosen on the validation rows only, so the test metrics remain honest; a training ROC AUC of 1 at k=1 is expected and meaningless. " +
            Method + Bounds)]
        public ClassifierEvaluationResult EvaluateKNearestNeighbours(
            [Description(
                "Predictor rows: one array of numbers per case, all the same length and in the order of the variable names. NaN marks a missing value.")]
            double[][] inputs,
            [Description(
                "The outcome for each row, in the same order as the rows: 1 for the event of interest and 0 otherwise.")]
            int[] labels,
            [Description("The name of each predictor column, unique and in column order.")]
            string[] variableNames,
            [Description(
                "The values of k to try, up to 16 whole numbers between 1 and 500. Empty tries 1, 3, 5, 9, 15, 25 and 51.")]
            int[]? neighbourCounts = null,
            [Description(
                "Share of rows held back to choose k, fit the probability calibration and the threshold, 0 to 0.5. Default 0.2.")]
            double validationFraction = 0.2,
            [Description(
                "Share of rows held back purely to measure quality, above 0 and at most 0.5. Ignored when cross validation is used. Default 0.2.")]
            double testFraction = 0.2,
            [Description("Seed that makes the splits and every random choice reproducible.")]
            int seed = 1,
            [Description(
                "0 evaluates on one train, validation and test split; 2 to 10 runs stratified cross validation with k chosen inside each fold.")]
            int crossValidationFolds = 0,
            [Description(
                "None fits the data as it is. BalancedWeights and Oversample duplicate rare training rows; Undersample drops common ones. Applied to training rows only.")]
            ClassImbalanceHandling classImbalance = ClassImbalanceHandling.None,
            [Description(
                "Whether to fit a Platt calibration on the validation rows so probabilities match observed frequencies. Default true.")]
            bool calibrate = true,
            [Description(
                "Probability at or above which a row counts as positive in the confusion table. Empty chooses the threshold that maximises F1 on the validation rows.")]
            double? decisionThreshold = null,
            [Description(
                "How many times each variable is shuffled to measure permutation importance, 0 to 10; 0 skips importance. Default 3.")]
            int permutationRepeats = 3,
            CancellationToken cancellationToken = default)
        {
            var options = ClassificationToolInput.Validate(inputs, labels, variableNames, validationFraction,
                testFraction,
                seed, crossValidationFolds, decisionThreshold, permutationRepeats, calibrate);

            ModelEvaluationGuard.Sweep(neighbourCounts, nameof(neighbourCounts), 1, 500, "neighbour count");

            var counts = neighbourCounts is { Length: > 0 }
                ? neighbourCounts.Distinct().OrderBy(k => k).ToArray()
                : [1, 3, 5, 9, 15, 25, 51];

            var configs = counts.Select(k => new LearnerConfig
            {
                Family = EvaluationFamily.KNearestNeighbours,
                K = k,
                Imbalance = classImbalance
            }).ToArray();

            return BinaryEvaluationEngine.Evaluate(configs, counts.Select(k => "NeighbourCount=" + k).ToArray(), inputs,
                labels, variableNames, options, cancellationToken);
        }

        [AdvancedAnalyticsTool("EvaluateSupportVectorMachine")]
        [Description(
            "Fits a support vector machine for a 0/1 outcome over a sweep of the complexity C and evaluates the chosen setting on held-out rows, returning a sweep table (training and validation ROC AUC for every C), ROC AUC, average precision, log loss, calibration and the support vector count. " +
            "Linear finds a linear boundary and scales to 20000 rows; Gaussian, Polynomial, Sigmoid and Laplacian kernels find curved boundaries but are limited to 4000 rows because training is quadratic. Predictors are standardised on the training rows. " +
            "Large C fits the training rows tightly and can overfit; small C smooths. The raw margin is turned into probabilities by Platt scaling on the validation rows, and C is chosen on the validation rows only. " +
            Method + Bounds)]
        public ClassifierEvaluationResult EvaluateSupportVectorMachine(
            [Description(
                "Predictor rows: one array of numbers per case, all the same length and in the order of the variable names. NaN marks a missing value.")]
            double[][] inputs,
            [Description(
                "The outcome for each row, in the same order as the rows: 1 for the event of interest and 0 otherwise.")]
            int[] labels,
            [Description("The name of each predictor column, unique and in column order.")]
            string[] variableNames,
            [Description(
                "Linear, Gaussian, Polynomial, Sigmoid or Laplacian. Gaussian is the usual choice for curved boundaries.")]
            SvmKernel kernel = SvmKernel.Linear,
            [Description(
                "The complexity values C to try, up to 16 numbers between 0.0001 and 10000. Empty tries 0.1, 1 and 10.")]
            double[]? complexityValues = null,
            [Description(
                "Kernel width as gamma for the non-linear kernels, between 0.00001 and 1000; empty uses 1 divided by the number of variables.")]
            double? kernelWidth = null,
            [Description(
                "Share of rows held back to choose C, fit the probability calibration and the threshold, 0 to 0.5. Default 0.2.")]
            double validationFraction = 0.2,
            [Description(
                "Share of rows held back purely to measure quality, above 0 and at most 0.5. Ignored when cross validation is used. Default 0.2.")]
            double testFraction = 0.2,
            [Description("Seed that makes the splits and every random choice reproducible.")]
            int seed = 1,
            [Description(
                "0 evaluates on one train, validation and test split; 2 to 10 runs stratified cross validation with C chosen inside each fold.")]
            int crossValidationFolds = 0,
            [Description(
                "None fits the data as it is. BalancedWeights gives the rarer class more weight; Oversample duplicates rare rows; Undersample drops common rows. Applied to training rows only.")]
            ClassImbalanceHandling classImbalance = ClassImbalanceHandling.None,
            [Description(
                "Whether to fit a Platt calibration on the validation rows so probabilities match observed frequencies. Default true.")]
            bool calibrate = true,
            [Description(
                "Probability at or above which a row counts as positive in the confusion table. Empty chooses the threshold that maximises F1 on the validation rows.")]
            double? decisionThreshold = null,
            [Description(
                "How many times each variable is shuffled to measure permutation importance, 0 to 10; 0 skips importance. Default 3.")]
            int permutationRepeats = 3,
            CancellationToken cancellationToken = default)
        {
            var options = ClassificationToolInput.Validate(inputs, labels, variableNames, validationFraction,
                testFraction,
                seed, crossValidationFolds, decisionThreshold, permutationRepeats, calibrate,
                kernel == SvmKernel.Linear
                    ? ModelEvaluationLimits.MaximumRows
                    : ModelEvaluationLimits.MaximumKernelRows);

            ModelEvaluationGuard.Sweep(complexityValues, nameof(complexityValues), 1e-4, 1e4, "complexity");

            if (kernelWidth.HasValue)
            {
                ModelEvaluationGuard.DoubleRange(kernelWidth.Value, 1e-5, 1e3, nameof(kernelWidth), "The kernel width");
            }

            var values = complexityValues is { Length: > 0 }
                ? complexityValues.Distinct().OrderBy(c => c).ToArray()
                : [0.1, 1d, 10d];

            var configs = values.Select(c => new LearnerConfig
            {
                Family = kernel == SvmKernel.Linear
                    ? EvaluationFamily.LinearSupportVector
                    : EvaluationFamily.KernelSupportVector,
                Kernel = kernel,
                Complexity = c,
                Gamma = kernelWidth ?? 0d,
                Imbalance = classImbalance
            }).ToArray();

            return BinaryEvaluationEngine.Evaluate(configs,
                values.Select(c => "Complexity=" + LearnerConfig.Format(c)).ToArray(),
                inputs, labels, variableNames, options, cancellationToken);
        }

        [AdvancedAnalyticsTool("EvaluateNeuralNetworkClassifier")]
        [Description(
            "Fits a small multilayer perceptron for a 0/1 outcome and evaluates it on held-out rows, returning ROC AUC, average precision, log loss, Brier score, a confusion table, calibration, the training error trace and permutation importance. " +
            "Layers of sigmoid units are trained by resilient backpropagation on standardised predictors from a seeded random start, so the result is reproducible; hiddenLayers gives the unit count of each hidden layer (up to 3 layers of up to 64 units) and epochs is capped at 1000. " +
            "Neural networks need more rows than trees and are the hardest family to explain; use them when smooth non-linear structure is expected and compare against EvaluateRandomForest and EvaluateBoostedTrees. Early stopping halts training when the validation error stops improving. " +
            Method + Bounds)]
        public ClassifierEvaluationResult EvaluateNeuralNetworkClassifier(
            [Description(
                "Predictor rows: one array of numbers per case, all the same length and in the order of the variable names. NaN marks a missing value.")]
            double[][] inputs,
            [Description(
                "The outcome for each row, in the same order as the rows: 1 for the event of interest and 0 otherwise.")]
            int[] labels,
            [Description("The name of each predictor column, unique and in column order.")]
            string[] variableNames,
            [Description(
                "Number of units in each hidden layer, up to 3 layers of 1 to 64 units. Empty uses one layer of 8 units.")]
            int[]? hiddenLayers = null,
            [Description("Maximum training passes over the data, 1 to 1000. Default 100.")]
            int epochs = 100,
            [Description(
                "Stop after this many epochs without a validation error improvement and keep the best weights, 0 to 100; 0 trains for every epoch. Default 10.")]
            int earlyStoppingPatience = 10,
            [Description(
                "Share of rows held back for early stopping, the probability calibration and the threshold, 0 to 0.5. Default 0.2.")]
            double validationFraction = 0.2,
            [Description(
                "Share of rows held back purely to measure quality, above 0 and at most 0.5. Ignored when cross validation is used. Default 0.2.")]
            double testFraction = 0.2,
            [Description("Seed that fixes the split and the starting weights so the run is reproducible.")]
            int seed = 1,
            [Description(
                "0 evaluates on one train, validation and test split; 2 to 10 runs stratified cross validation and pools the held-out predictions of every fold.")]
            int crossValidationFolds = 0,
            [Description(
                "None fits the data as it is. BalancedWeights and Oversample duplicate rare training rows; Undersample drops common ones. Applied to training rows only.")]
            ClassImbalanceHandling classImbalance = ClassImbalanceHandling.None,
            [Description(
                "Whether to fit a Platt calibration on the validation rows so probabilities match observed frequencies. Default true.")]
            bool calibrate = true,
            [Description(
                "Probability at or above which a row counts as positive in the confusion table. Empty chooses the threshold that maximises F1 on the validation rows.")]
            double? decisionThreshold = null,
            [Description(
                "How many times each variable is shuffled to measure permutation importance, 0 to 10; 0 skips importance. Default 3.")]
            int permutationRepeats = 3,
            CancellationToken cancellationToken = default)
        {
            var options = ClassificationToolInput.Validate(inputs, labels, variableNames, validationFraction,
                testFraction,
                seed, crossValidationFolds, decisionThreshold, permutationRepeats, calibrate);

            ModelEvaluationGuard.HiddenLayers(hiddenLayers, nameof(hiddenLayers));

            ModelEvaluationGuard.IntRange(epochs, 1, ModelEvaluationLimits.MaximumEpochs, nameof(epochs),
                "The epoch count");

            ModelEvaluationGuard.IntRange(earlyStoppingPatience, 0, 100, nameof(earlyStoppingPatience),
                "The early stopping patience");

            var config = new LearnerConfig
            {
                Family = EvaluationFamily.NeuralNetwork,
                Hidden = hiddenLayers is { Length: > 0 } ? hiddenLayers : [8],
                Epochs = epochs,
                Patience = validationFraction > 0 ? earlyStoppingPatience : 0,
                Imbalance = classImbalance
            };

            return BinaryEvaluationEngine.Evaluate([config], [config.Name], inputs, labels, variableNames, options,
                cancellationToken);
        }

        [AdvancedAnalyticsTool("CrossValidateLogisticRegression")]
        [Description(
            "Evaluates a logistic regression for a 0/1 outcome with stratified cross validation, returning the pooled held-out ROC AUC, average precision, log loss and Brier score plus the metrics of every fold and their spread. " +
            "It reuses the same fitting, preprocessing and calibration as TrainLogisticRegression but repeats it across folds, so it shows how stable the model is; use TrainLogisticRegression when you need the coefficient table, odds ratios and a model snapshot. " +
            "A large standard deviation of ROC AUC between folds means the estimate is unreliable, usually because positives are few. " +
            Method + Bounds)]
        public ClassifierEvaluationResult CrossValidateLogisticRegression(
            [Description(
                "Predictor rows: one array of numbers per case, all the same length and in the order of the variable names. NaN marks a missing value.")]
            double[][] inputs,
            [Description(
                "The outcome for each row, in the same order as the rows: 1 for the event of interest and 0 otherwise.")]
            int[] labels,
            [Description("The name of each predictor column, unique and in column order.")]
            string[] variableNames,
            [Description("Number of stratified cross validation folds, 2 to 10. Default 5.")]
            int crossValidationFolds = 5,
            [Description(
                "Ridge penalty strength on the standardised coefficients; 0 fits by plain maximum likelihood. Use a positive value when predictors are collinear or the classes separate.")]
            double ridgeLambda = 0,
            [Description(
                "Share of each training part held back to fit the probability calibration, 0 to 0.5. Default 0.2.")]
            double validationFraction = 0.2,
            [Description(
                "Seed that makes the fold assignment reproducible. Reuse a seed to compare models on identical folds.")]
            int seed = 1,
            [Description(
                "None fits the data as it is. BalancedWeights gives the rarer class more weight during fitting; the probabilities are corrected back afterwards.")]
            ClassImbalanceHandling classImbalance = ClassImbalanceHandling.None,
            [Description("Whether to fit a Platt calibration on the validation rows. Default true.")]
            bool calibrate = true,
            [Description(
                "How many times each variable is shuffled to measure permutation importance, 0 to 10; 0 skips importance. Default 0.")]
            int permutationRepeats = 0,
            CancellationToken cancellationToken = default)
        {
            ModelEvaluationGuard.StrictFolds(crossValidationFolds, nameof(crossValidationFolds));
            ModelEvaluationGuard.DoubleRange(ridgeLambda, 0d, 1e6, nameof(ridgeLambda), "The ridge strength");

            var options = ClassificationToolInput.Validate(inputs, labels, variableNames, validationFraction, 0.2, seed,
                crossValidationFolds, null, permutationRepeats, calibrate);

            var config = new LearnerConfig
            {
                Family = EvaluationFamily.LogisticRegression,
                Regularisation = ridgeLambda,
                Imbalance = classImbalance
            };

            return BinaryEvaluationEngine.Evaluate([config], [config.Name], inputs, labels, variableNames, options,
                cancellationToken);
        }
    }
}