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
    public class MulticlassEvaluationTools
    {
        [AdvancedAnalyticsTool("EvaluateMulticlassClassifier")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Fits a classifier for an outcome with three or more classes (for example a fraud typology, an alert disposition or a risk band) and evaluates it on held-out rows, returning accuracy against the largest-class baseline, balanced accuracy, macro, micro and weighted F1, Cohen's kappa, multiclass MCC, log loss, Brier score, macro one-versus-rest ROC AUC, per-class precision, recall, F1 and support, and the full confusion matrix (rows actual, columns predicted). " +
            "Algorithms: MultinomialLogistic (softmax regression, the interpretable baseline), DecisionTree (readable rules), RandomForest, NaiveBayes, KNearestNeighbours and NeuralNetwork. " +
            "Read the confusion matrix and per-class recall before the headline accuracy: with a dominant class, accuracy can look good while rare classes are missed, so compare it with MajorityClassAccuracy and prefer balanced accuracy and macro F1. Class probabilities are the model's own and are not recalibrated. " +
            "Method: rows are split by seed into stratified train, validation and test sets (or stratified cross validation folds); scaling and missing-value filling are learned from training rows only, any imbalance handling touches training rows only, and every metric comes from the untouched held-out rows. Class labels can be any whole numbers; every class needs at least 5 rows. " +
            "Bounds: at most 20000 rows, 200 predictor columns, 20 classes, 500 trees, 1000 epochs. Nothing is saved.")]
        public MulticlassEvaluationResult EvaluateMulticlassClassifier(
            [Description(
                "Predictor rows: one array of numbers per case, all the same length and in the order of the variable names. NaN marks a missing value.")]
            double[][] inputs,
            [Description(
                "The class of each row, in the same order as the rows, as a whole number. Between 3 and 20 distinct classes, each with at least 5 rows.")]
            int[] classLabels,
            [Description("The name of each predictor column, unique and in column order.")]
            string[] variableNames,
            [Description(
                "MultinomialLogistic, DecisionTree, RandomForest, NaiveBayes, KNearestNeighbours or NeuralNetwork.")]
            MulticlassAlgorithm algorithm = MulticlassAlgorithm.MultinomialLogistic,
            [Description("Maximum tree depth for DecisionTree and RandomForest, 1 to 24. Default 8.")]
            int maxDepth = 8,
            [Description(
                "Minimum training rows in every leaf for DecisionTree and RandomForest, 1 to 1000. Default 3.")]
            int minLeafSize = 3,
            [Description("Number of trees for RandomForest, 1 to 500. Default 100.")]
            int treeCount = 100,
            [Description("The k for KNearestNeighbours, 1 to 500. Default 7.")]
            int neighbourCount = 7,
            [Description(
                "Number of units in each hidden layer for NeuralNetwork, up to 3 layers of 1 to 64 units. Empty uses one layer of 12 units.")]
            int[]? hiddenLayers = null,
            [Description("Training passes for NeuralNetwork, 1 to 1000. Default 200.")]
            int epochs = 200,
            [Description("Share of rows held back for calibration-style checks, 0 to 0.5. Default 0.2.")]
            double validationFraction = 0.2,
            [Description(
                "Share of rows held back purely to measure quality, above 0 and at most 0.5. Ignored when cross validation is used. Default 0.2.")]
            double testFraction = 0.2,
            [Description("Seed that makes the splits and every random choice reproducible.")]
            int seed = 1,
            [Description(
                "0 evaluates on one train, validation and test split; 2 to 10 runs stratified cross validation and pools the held-out predictions.")]
            int crossValidationFolds = 0,
            [Description(
                "None fits the data as it is. BalancedWeights weights rows by inverse class size where the algorithm supports it, otherwise oversamples; Oversample and Undersample equalise the training classes.")]
            ClassImbalanceHandling classImbalance = ClassImbalanceHandling.None,
            [Description(
                "How many times each variable is shuffled to measure permutation importance on log loss, 0 to 10; 0 skips importance. Default 3.")]
            int permutationRepeats = 3,
            CancellationToken cancellationToken = default)
        {
            ModelEvaluationGuard.Features(inputs, variableNames);

            if (classLabels == null || classLabels.Length != inputs.Length)
            {
                throw new ArgumentException("There must be exactly one class label per row.", nameof(classLabels));
            }

            var counts = classLabels.GroupBy(l => l).ToDictionary(g => g.Key, g => g.Count());

            if (counts.Count < 3 || counts.Count > ModelEvaluationLimits.MaximumClasses)
            {
                throw new ArgumentException(
                    $"Give between 3 and {ModelEvaluationLimits.MaximumClasses} distinct classes; use the binary evaluation tools for two classes.",
                    nameof(classLabels));
            }

            if (counts.Any(c => c.Value < 5))
            {
                throw new ArgumentException("Every class needs at least 5 rows; merge or remove the rarer classes.",
                    nameof(classLabels));
            }

            ModelEvaluationGuard.Fractions(validationFraction, testFraction);
            ModelEvaluationGuard.Folds(crossValidationFolds, nameof(crossValidationFolds));

            ModelEvaluationGuard.IntRange(permutationRepeats, 0, ModelEvaluationLimits.MaximumPermutationRepeats,
                nameof(permutationRepeats), "The number of permutation repeats");
            ModelEvaluationGuard.IntRange(maxDepth, 1, ModelEvaluationLimits.MaximumTreeDepth, nameof(maxDepth),
                "The maximum depth");

            ModelEvaluationGuard.IntRange(minLeafSize, 1, 1000, nameof(minLeafSize), "The minimum leaf size");

            ModelEvaluationGuard.IntRange(treeCount, 1, ModelEvaluationLimits.MaximumTrees, nameof(treeCount),
                "The tree count");

            ModelEvaluationGuard.IntRange(neighbourCount, 1, 500, nameof(neighbourCount), "The neighbour count");

            ModelEvaluationGuard.IntRange(epochs, 1, ModelEvaluationLimits.MaximumEpochs, nameof(epochs),
                "The epoch count");

            ModelEvaluationGuard.HiddenLayers(hiddenLayers, nameof(hiddenLayers));

            var config = new MulticlassConfig
            {
                Algorithm = algorithm,
                MaxDepth = maxDepth,
                MinLeaf = minLeafSize,
                Trees = treeCount,
                K = neighbourCount,
                Hidden = hiddenLayers is { Length: > 0 } ? hiddenLayers : [12],
                Epochs = epochs,
                Imbalance = classImbalance
            };

            var options = new EvaluationOptions(validationFraction, testFraction, crossValidationFolds, seed, false,
                null,
                permutationRepeats);

            return MulticlassEngine.Evaluate(config, inputs, classLabels, variableNames, options, cancellationToken);
        }
    }
}