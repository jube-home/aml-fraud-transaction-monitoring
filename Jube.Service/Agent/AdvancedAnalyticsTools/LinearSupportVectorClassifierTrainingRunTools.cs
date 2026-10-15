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
    [AdvancedAnalyticsToolGroup("LinearSupportVectorClassifierTrainingRuns")]
    public class LinearSupportVectorClassifierTrainingRunTools(
        AdvancedAnalyticsLinearSupportVectorClassifierTrainingService service)
    {
        [AdvancedAnalyticsTool("SubmitLinearSupportVectorClassifierTraining")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Queues a linear support vector machine classifier training run for a background worker and returns its " +
            "run id straight away, before anything is trained. A linear support vector machine separates the two " +
            "classes with the straight-line (or, with more than two predictors, flat-plane) boundary that leaves the " +
            "widest possible margin of empty space between the boundary and the nearest training rows of either " +
            "class; those nearest rows are the support vectors that alone determine where the boundary sits, so " +
            "adding or removing a row that sits well clear of the margin does not move the boundary at all. This " +
            "implementation fits that boundary with dual coordinate descent, an optimisation method that iterates " +
            "over the training rows updating one row's contribution to the boundary at a time, which scales to far " +
            "more rows than solving the classification problem directly and is the standard approach for a purely " +
            "linear kernel. Because the boundary is linear, the fitted model reduces to a weight for every predictor " +
            "and a single offset, so recalling it later is a plain weighted sum with no trace of the training rows " +
            "or the fitting algorithm retained; every predictor's contribution to that sum can therefore be reported " +
            "exactly, to the same standard as ordinary linear regression. Unlike a probabilistic classifier, the raw " +
            "value this boundary produces is only a signed distance from the boundary in the units of the " +
            "standardised predictors, with no meaning as a probability and no upper or lower bound, so this training " +
            "run always attempts to fit a logistic (Platt) curve from that raw distance to a probability on a held-out " +
            "validation split, since an uncalibrated distance is close to useless for any decision that compares " +
            "cases against a probability threshold. To follow the run, call GetAdvancedAnalyticsTrainingRunStatus " +
            "with the returned run id until it reports Succeeded, Failed or Cancelled, then call " +
            "GetAdvancedAnalyticsTrainingRunResult. Training never makes the resulting version live.")]
        public Task<AdvancedAnalyticsTrainingSubmissionDto> SubmitLinearSupportVectorClassifierTrainingAsync(
            [Description("Numeric identifier of the Advanced Analytics instance the run and its model belong to.")]
            int instanceId,
            [Description(
                "Name of the model the trained version is filed under. An existing model of that name receives a new version; otherwise a model is created.")]
            string modelName,
            [Description("The name of each predictor column, unique and in column order.")]
            string[] variableNames,
            [Description(
                "Where each predictor comes from in the live pipeline, one number per variable name in the same order: 1 payload, 2 dictionary, 3 TTL counter, 4 sanction, 5 abstraction, 6 abstraction calculation, 7 or 99 other.")]
            int[] processingTypeIds,
            [Description(
                "Inline predictor rows, one array of numbers per case with NaN for a missing value; a missing value is imputed with that predictor's training median before the boundary is fitted. Leave empty when a cache pointer is given.")]
            double[][]? inputs = null,
            [Description(
                "Inline outcomes, one per row: 1 for the positive class being modelled, 0 for the negative class. Both classes must appear at least once across the training split, since a boundary between two classes cannot be drawn from examples of only one of them. Leave empty when a cache pointer is given.")]
            int[]? labels = null,
            [Description(
                "Seed that makes the row split and, where ties occur, the calibration fit reproducible. Reuse a seed to compare models fitted on identical rows.")]
            int seed = 1,
            [Description(
                "The regularisation strength of the dual coordinate descent solver, traditionally written C in the support vector machine literature: it sets how heavily a training row that ends up on the wrong side of the margin, or inside it, is penalised relative to the width of the margin itself. A small value accepts more such rows in exchange for a wider, more conservative margin that tends to generalise better; a large value narrows the margin to fit the training rows as closely as possible, which risks tracking noise in them. Must be strictly positive. Default 1.0, a neutral starting point that is commonly then adjusted up or down by comparing held-out performance across a handful of values on a logarithmic scale.")]
            double complexity = 1.0,
            [Description(
                "Whether every predictor is z-scored (rescaled to the training mean and standard deviation) before fitting. A support vector machine's margin is a geometric notion of distance across every predictor at once, so a predictor left on a much larger numeric scale than the others would dominate that distance purely because of its units, effectively drowning out the rest; leaving this on (the default) is strongly recommended for this family in a way it is not for models, such as a decision tree, whose fitted structure is scale-invariant.")]
            bool standardise = true,
            [Description(
                "Share of the rows held back to fit the probability calibration, between 0 and 0.9. Default 0.2.")]
            double validationFraction = 0.2,
            [Description(
                "Share of the rows held back purely to measure quality on rows the model never trained or calibrated on, between 0 and 0.9. Default 0.2.")]
            double testFraction = 0.2,
            [Description(
                "Whether to recalibrate the raw signed distance from the decision boundary into a probability by fitting a logistic (Platt) curve on the validation split. The raw distance is not a probability and is not even bounded, so leaving this on (the default) is strongly recommended; when both classes are not present in the validation split, calibration is skipped regardless and the raw distance is reported instead, with a warning recorded either way.")]
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
            return service.SubmitTrainingAsync(new AdvancedAnalyticsLinearSupportVectorClassifierTrainingDto
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
                Complexity = complexity,
                Standardise = standardise,
                ValidationFraction = validationFraction,
                TestFraction = testFraction,
                Calibrate = calibrate,
                DecisionThreshold = decisionThreshold
            }, cancellationToken);
        }
    }
}