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
using Jube.Engine.AdvancedAnalytics.Training;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools
{
    [AdvancedAnalyticsToolGroup("TreeAugmentedNaiveBayesClassifierTrainingRuns")]
    public class TreeAugmentedNaiveBayesClassifierTrainingRunTools(
        AdvancedAnalyticsTreeAugmentedNaiveBayesClassifierTrainingService service)
    {
        [AdvancedAnalyticsTool("SubmitTreeAugmentedNaiveBayesClassifierTraining")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Queues a tree augmented naive Bayes classifier training run for a background worker and returns its run " +
            "id straight away, before anything is trained. Tree augmented naive Bayes (TAN) is the direct " +
            "generalisation of naive Bayes that keeps most of its structure while relaxing its one weak point: plain " +
            "naive Bayes assumes every predictor is conditionally independent of every other predictor given the " +
            "class, which discards any genuine pairwise interaction between two predictors; TAN instead lets every " +
            "predictor have at most one additional parent chosen from among the other predictors, so it can capture " +
            "the single strongest such interaction per predictor while still remaining cheap to fit and to explain. " +
            "The parent structure is not a free modelling choice: it is discovered automatically by the Chow-Liu " +
            "algorithm, which measures the conditional mutual information between every pair of predictors given the " +
            "class and keeps only the maximum-weight spanning tree of those pairwise dependencies, which is the " +
            "provably best single-parent-per-predictor structure under the log-likelihood a tree-shaped model can " +
            "achieve. Within that tree, this implementation fits every predictor as Gaussian given the class and, " +
            "where it has one, linearly dependent on its parent predictor's own value, so continuous, real-valued " +
            "predictors are used directly with no need to bin them first. A new row is scored by combining, in " +
            "log-odds space, the prior log-odds of the positive class with every predictor's own log-likelihood-ratio " +
            "between its two fitted class-and-parent-conditional distributions; because the tree structure still " +
            "makes the joint log-likelihood an exact sum of one term per predictor, the contribution of every single " +
            "predictor to the final score, and which other predictor it was evaluated against, can be reported " +
            "precisely, with no approximation. As with plain naive Bayes, the raw posterior tends to be poorly " +
            "calibrated, so this training run recalibrates the raw score into a probability by fitting a logistic " +
            "(Platt) curve on a held-out validation split whenever both classes are present there; otherwise the " +
            "native posterior is reported and a warning explains why. To follow the run, call " +
            "GetAdvancedAnalyticsTrainingRunStatus with the returned run id until it reports Succeeded, Failed or " +
            "Cancelled, then call GetAdvancedAnalyticsTrainingRunResult. Training never makes the resulting version " +
            "live.")]
        public Task<AdvancedAnalyticsTrainingSubmissionDto> SubmitTreeAugmentedNaiveBayesClassifierTrainingAsync(
            [Description("Numeric identifier of the Advanced Analytics instance the run and its model belong to.")]
            int instanceId,
            [Description(
                "Name of the model the trained version is filed under. An existing model of that name receives a new version; otherwise a model is created.")]
            string modelName,
            [Description(
                "The name of each predictor column, unique and in column order. The Chow-Liu tree is built over exactly these columns, so a predictor's position here is also its identity when the fitted parent structure is reported back in the evidence.")]
            string[] variableNames,
            [Description(
                "Where each predictor comes from in the live pipeline, one number per variable name in the same order: 1 payload, 2 dictionary, 3 TTL counter, 4 sanction, 5 abstraction, 6 abstraction calculation, 7 or 99 other.")]
            int[] processingTypeIds,
            [Description(
                "Inline predictor rows, one array of numbers per case with NaN for a missing value; a missing value is imputed with that predictor's training median before the tree structure and the class-and-parent-conditional Gaussians are fitted, and the same imputation is applied again at recall time so a fresh row is scored consistently with how the model was trained. Leave empty when a cache pointer is given.")]
            double[][]? inputs = null,
            [Description(
                "Inline outcomes, one per row: 1 for the positive class being modelled, 0 for the negative class. Both classes must appear at least once across the training split, since the Chow-Liu tree and every class-conditional Gaussian are estimated separately for each class and need at least one observation to do so. Leave empty when a cache pointer is given.")]
            int[]? labels = null,
            [Description(
                "Seed that makes the row split and, where ties occur, the calibration fit reproducible. Reuse a seed to compare models fitted on identical rows.")]
            int seed = 1,
            [Description(
                "Share of the rows held back to fit the probability calibration, between 0 and 0.9. Default 0.2.")]
            double validationFraction = 0.2,
            [Description(
                "Share of the rows held back purely to measure quality on rows the model never trained or calibrated on, between 0 and 0.9. Default 0.2.")]
            double testFraction = 0.2,
            [Description(
                "An additive (Laplace-style) count blended into the observed positive and negative row counts before either class prior is computed, so that a class prior is never driven to exactly zero or one purely by how the rows happened to fall; it has no effect on the fitted tree structure or on any individual predictor's conditional distribution, only on the two class priors that the per-predictor evidence is added to. Default 1, the conventional add-one smoothing count.")]
            double smoothing = 1d,
            [Description(
                "Fitting a predictor's conditional Gaussian to a class-and-parent sample whose residual variance would otherwise be zero, undefined or implausibly small gives that predictor infinite density and lets it dominate every prediction regardless of the other predictors. This regularisation constant is the floor placed under every such residual variance, expressed as a share of the predictor's overall training variance so that it scales with the predictor's own units; a residual variance safely above the floor is never touched by it. Default 0.0001 (1e-4), the value this training run's underlying tree-fitting routine itself uses, which is deliberately higher than a plain Gaussian naive Bayes classifier's own variance floor because a residual variance left over after explaining a predictor through its chosen parent is a smaller, noisier quantity than that predictor's own unconditional variance.")]
            double varianceRegularisation = 1e-4,
            [Description(
                "A small constant added to a parent predictor's own within-class variance before it is used as the denominator when solving for the slope linking a child predictor to that parent, in the same spirit as a ridge penalty in linear regression. Without it, a parent predictor that is itself nearly constant within one class would make the fitted slope for its children numerically unstable, arbitrarily large or, once floating-point rounding is involved, incorrect in sign. Default 0.000001 (1e-6), which only matters when a parent predictor's within-class spread is itself close to zero.")]
            double ridge = 1e-6,
            [Description(
                "Whether to recalibrate the raw log-posterior-odds into a probability by fitting a logistic (Platt) curve on the validation split. The raw posterior from a tree augmented naive Bayes fit is, like plain naive Bayes, usually overconfident, so leaving this on (the default) is strongly recommended; when both classes are not present in the validation split, calibration is skipped regardless and the native posterior is reported, with a warning recorded either way. Default true.")]
            bool calibrate = true,
            [Description(
                "Probability at or above which the confusion counts recorded in the evidence (true/false positives and negatives, precision, recall) classify a row as the positive class. This is purely a diagnostic choice for reading the evidence; recall from the trained model always returns the probability itself, not a class decision. Default 0.5.")]
            double decisionThreshold = 0.5,
            [Description(
                "Pointer to a table in the Advanced Analytics cache holding the predictor columns and a label column. Leave empty when the rows are inline. The run fails if the pointer has expired by the time a worker starts it.")]
            Guid? cachePointer = null,
            [Description(
                "Zero based index of the label column in the cached table. Default is the last column; only used with a cache pointer.")]
            int labelColumn = -1,
            [Description("Optional opaque label kept with a newly created model, for finding it again.")]
            string? correlationKey = null,
            CancellationToken cancellationToken = default)
        {
            return service.SubmitTrainingAsync(new AdvancedAnalyticsTreeAugmentedNaiveBayesClassifierTrainingDto
            {
                AdvancedAnalyticsInstanceId = instanceId,
                ModelName = modelName,
                CorrelationKey = correlationKey,
                VariableNames = variableNames,
                ProcessingTypeIds = processingTypeIds,
                Input = cachePointer.HasValue
                    ? AdvancedAnalyticsTrainingInput.CachePointer(cachePointer.Value, labelColumn)
                    : null,
                Inputs = inputs,
                Labels = labels,
                Seed = seed,
                ValidationFraction = validationFraction,
                TestFraction = testFraction,
                Smoothing = smoothing,
                VarianceRegularisation = varianceRegularisation,
                Ridge = ridge,
                Calibrate = calibrate,
                DecisionThreshold = decisionThreshold
            }, cancellationToken);
        }
    }
}