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
    [AdvancedAnalyticsToolGroup("AdaBoostClassifierTrainingRuns")]
    public class AdaBoostClassifierTrainingRunTools(AdvancedAnalyticsAdaBoostClassifierTrainingService service)
    {
        [AdvancedAnalyticsTool("SubmitAdaBoostClassifierTraining")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Queues an AdaBoost training run for a background worker and returns its run id straight away, before " +
            "anything is trained. AdaBoost (the SAMME variant of discrete Adaptive Boosting) fits a fixed number of " +
            "shallow decision trees one after another rather than independently: the first tree is fitted on every " +
            "row weighted equally, then every later round reweights the rows so the ones the ensemble so far still " +
            "gets wrong count for more and the ones it already gets right count for less, so each new tree is " +
            "steered onto the cases the series is currently failing on. Each tree's say in the final vote is not " +
            "equal: a tree fitted a round where its weighted error was low earns a large vote weight, one that " +
            "barely beat chance earns a vote weight close to zero, and the final raw score is the alpha-weighted " +
            "average of every tree's plus-or-minus-one vote, a margin from -1 (every vote against) to +1 (every vote " +
            "for) rather than a probability by itself; that margin is calibrated to a probability by Platt scaling, " +
            "exactly as the other classifier families are. Because each round corrects the previous rounds' mistakes, " +
            "the individual trees are kept deliberately shallow (a stump or a small tree) and the boosting rounds, " +
            "not any one tree's depth, do the work of fitting the data; a round whose tree cannot beat chance on its " +
            "reweighted rows ends the series early rather than folding in a useless or harmful vote. Give the " +
            "predictor rows and the binary label (1 for the event of interest, 0 otherwise) for each row inline, " +
            "or a cache pointer to a stored table whose last column is the label. To " +
            "follow the run, call GetAdvancedAnalyticsTrainingRunStatus with the returned run id until it reports " +
            "Succeeded, Failed or Cancelled, then call GetAdvancedAnalyticsTrainingRunResult. Training never makes " +
            "the resulting version live.")]
        public async Task<TrainingRunSubmissionView> SubmitAdaBoostClassifierTrainingAsync(
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
                "Inline predictor rows, one array of numbers per case with NaN for a missing value. Leave empty when a cache pointer is given.")]
            double[][]? inputs = null,
            [Description(
                "Inline outcomes, one per row: 1 for the event of interest and 0 otherwise. Both classes must be present. Leave empty when a cache pointer is given.")]
            int[]? labels = null,
            [Description(
                "Number of boosting rounds: each round fits one shallow tree on the full training split, reweighted to favour whatever the ensemble so far still gets wrong, and casts a vote sized by how well that round's tree did. A round may fit fewer than this number when a tree turns out no better than chance, which ends the series rather than folding in a useless vote. Default 100.")]
            int rounds = 100,
            [Description(
                "Maximum number of splits from the root to a leaf in every round's tree. AdaBoost classically boosts weak learners rather than strong ones, so this is deliberately shallow: 1 fits a single-split stump, 2 or 3 a small tree; a deep tree fitted in an early round can already separate the training rows well enough that later rounds have nothing left to correct, which defeats the point of boosting. Default 2.")]
            int maxDepth = 2,
            [Description(
                "Minimum number of training rows required on each side of a candidate split for it to be accepted; a larger value forces coarser, more stable splits and shorter effective trees. Default 5.")]
            int minimumLeafSize = 5,
            [Description(
                "Whether the ensemble's raw decision-function margin (from -1, unanimous vote for the negative class, to +1, unanimous vote for the positive class) is calibrated to a probability by Platt scaling, fitted on the validation split. Default true.")]
            bool calibrate = true,
            [Description(
                "Probability threshold used to turn a calibrated probability into a positive/negative decision for the confusion counts in the evidence. Defaults to 0.5 when not given.")]
            double? decisionThreshold = null,
            [Description(
                "Seed that makes the row split reproducible. The boosting rounds themselves have nothing random left to seed: every round exhaustively searches every variable for its best weighted split, so the same rows and weights always fit the same tree.")]
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
            var submission = await service.SubmitTrainingAsync(new AdvancedAnalyticsAdaBoostClassifierTrainingDto
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
                Rounds = rounds,
                MaxDepth = maxDepth,
                MinimumLeafSize = minimumLeafSize,
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