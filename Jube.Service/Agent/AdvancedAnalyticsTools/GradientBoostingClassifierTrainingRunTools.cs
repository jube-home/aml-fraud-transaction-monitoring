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
using Jube.Service.Agent.AdvancedAnalyticsTools.Models;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools
{
    [AdvancedAnalyticsToolGroup("GradientBoostingClassifierTrainingRuns")]
    public class GradientBoostingClassifierTrainingRunTools(
        AdvancedAnalyticsGradientBoostingClassifierTrainingService service)
    {
        [AdvancedAnalyticsTool("SubmitGradientBoostingClassifierTraining")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Queues a gradient boosting training run for a background worker and returns its run id straight away, " +
            "before anything is trained. Gradient boosting for binomial classification starts the additive model at " +
            "the log-odds of the training prevalence and then, round after round, computes each row's residual (the " +
            "label minus the ensemble's current probability), fits one shallow regression tree to those residuals, " +
            "and adds a shrunk copy of that tree's output onto the running log-odds margin; unlike a random forest's " +
            "independent trees voting in parallel, every tree here is fitted to clean up specifically what every " +
            "earlier tree still got wrong, so the rounds are not independent and their order matters. The final raw " +
            "score is the running log-odds margin itself, so its own sigmoid is already a meaningful probability " +
            "even before calibration; a Platt scaling step is still fitted on the validation split and applied on " +
            "top for consistency with the other classifier families. Give the predictor rows and the binary label (1 " +
            "for the event of interest, 0 otherwise) for each row inline, or a cache pointer to a stored table " +
            "whose last column is the label. To follow the run, call " +
            "GetAdvancedAnalyticsTrainingRunStatus with the returned run id until it reports Succeeded, Failed or " +
            "Cancelled, then call GetAdvancedAnalyticsTrainingRunResult. Training never makes the resulting version " +
            "live.")]
        public async Task<TrainingRunSubmissionView> SubmitGradientBoostingClassifierTrainingAsync(
            [Description("Numeric identifier of the AskJooby Analytics instance the run and its model belong to.")]
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
                "Inline predictor rows, one array of numbers per case with NaN for a missing value. Leave empty when a cache pointer is given.")]
            double[][]? inputs = null,
            [Description(
                "Inline outcomes, one per row: 1 for the event of interest and 0 otherwise. Both classes must be present. Leave empty when a cache pointer is given.")]
            int[]? labels = null,
            [Description(
                "Number of boosting rounds: every round fits one shallow regression tree to the current residuals and adds a shrunk copy of its output onto the running log-odds margin. More rounds keep reducing the training loss, so the honest stopping point is read from the test metrics rather than assumed beforehand. Default 100.")]
            int rounds = 100,
            [Description(
                "Maximum number of splits from the root to a leaf in every round's regression tree. Gradient boosting deliberately uses shallow trees, each one only a small, weak correction that many rounds accumulate; a deeper tree fits sharper local patterns in a single round but leaves less of the learning to the boosting process itself. Default 3.")]
            int maxDepth = 3,
            [Description(
                "Shrinkage factor multiplying every tree's output before it is added to the running log-odds margin. A smaller learning rate needs more rounds to reach the same fit but generally generalises better; a larger rate reaches a given training loss in fewer rounds but is more prone to overfitting individual rounds' noise. Default 0.1.")]
            double learningRate = 0.1,
            [Description(
                "Minimum number of training rows required on each side of a candidate split in a round's regression tree for it to be accepted; a larger value forces coarser, more stable splits. Default 5.")]
            int minimumLeafSize = 5,
            [Description(
                "Share of the training rows drawn, without replacement, to fit each round's regression tree. The default of 1 fits every round on the whole training split; a smaller share is stochastic gradient boosting, giving every tree a fresh random subset of rows so the rounds are less correlated with one another, at the cost of a noisier per-round fit. Default 1.")]
            double sampleFraction = 1d,
            [Description(
                "Whether the ensemble's raw log-odds margin is calibrated to a probability by Platt scaling, fitted on the validation split. The margin's own sigmoid is already a meaningful probability even when this is switched off, since it is a genuine additive log-odds score, unlike a raw AdaBoost margin. Default true.")]
            bool calibrate = true,
            [Description(
                "Probability threshold used to turn a calibrated probability into a positive/negative decision for the confusion counts in the evidence. Defaults to 0.5 when not given.")]
            double? decisionThreshold = null,
            [Description(
                "Seed that makes the row split and every round's row subsample reproducible. Reuse a seed to compare ensembles on identical rows.")]
            int seed = 1,
            [Description(
                "Share of the rows held back for a validation split used to fit the probability calibration, between 0 and 0.9. Default 0.2.")]
            double validationFraction = 0.2,
            [Description(
                "Share of the rows held back purely to measure quality on rows the ensemble never saw, between 0 and 0.9. Together with the validation share it may not exceed 0.9. Default 0.2.")]
            double testFraction = 0.2,
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
            var submission = await service.SubmitTrainingAsync(
                new AdvancedAnalyticsGradientBoostingClassifierTrainingDto
                {
                    AskJoobyAnalyticsInstanceId = instanceId,
                    ModelName = modelName,
                    CorrelationKey = correlationKey,
                    VariableNames = variableNames,
                    ProcessingTypeIds = processingTypeIds,
                    Input = cachePointer.HasValue
                        ? AdvancedAnalyticsTrainingInput.CachePointer(cachePointer.Value, labelColumn)
                        : null,
                    Inputs = inputs,
                    Labels = labels,
                    Rounds = rounds,
                    MaxDepth = maxDepth,
                    LearningRate = learningRate,
                    MinimumLeafSize = minimumLeafSize,
                    SampleFraction = sampleFraction,
                    Calibrate = calibrate,
                    DecisionThreshold = decisionThreshold,
                    ValidationFraction = validationFraction,
                    TestFraction = testFraction,
                    Seed = seed
                }, cancellationToken).ConfigureAwait(false);

            return new TrainingRunSubmissionView
            {
                RunId = submission.RunId,
                Status = submission.Status.ToString(),
                HowToPoll = submission.HowToPoll
            };
        }
    }
}