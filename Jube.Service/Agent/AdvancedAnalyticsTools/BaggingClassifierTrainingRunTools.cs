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
    [AdvancedAnalyticsToolGroup("BaggingClassifierTrainingRuns")]
    public class BaggingClassifierTrainingRunTools(AdvancedAnalyticsBaggingClassifierTrainingService service)
    {
        [AdvancedAnalyticsTool("SubmitBaggingClassifierTraining")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Queues a bagging training run for a background worker and returns its run id straight away, before " +
            "anything is trained. Bagging (bootstrap aggregating) fits many CART-style decision trees, each grown on " +
            "its own bootstrap sample (a random draw of rows, with replacement, from the training split so that " +
            "some rows appear more than once and others are left out entirely) and, unlike a random forest, every " +
            "tree is free to split on any variable at any node: the only source of disagreement between trees is " +
            "which rows they happened to be grown on. Averaging the leaf probabilities of every tree gives a single " +
            "probability that is markedly steadier than any one tree's, because the trees' individual errors, each " +
            "coming from a different resample of the same data, tend to cancel out rather than reinforce one " +
            "another. The ensemble's raw vote fraction is calibrated to a probability by Platt scaling, exactly as " +
            "the other classifier families are. Give the predictor rows and the binary label (1 for the event of " +
            "interest, 0 otherwise) for each row inline, or a cache pointer to a stored table whose last column " +
            "is the label. To follow the run, call GetAdvancedAnalyticsTrainingRunStatus " +
            "with the returned run id until it reports Succeeded, Failed or Cancelled, then call " +
            "GetAdvancedAnalyticsTrainingRunResult. Training never makes the resulting version live.")]
        public async Task<TrainingRunSubmissionView> SubmitBaggingClassifierTrainingAsync(
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
                "Number of decision trees to fit, each independently on its own bootstrap sample. More trees give a steadier, lower variance vote fraction at a roughly linear cost in fitting time, with diminishing returns well before this default. Default 100.")]
            int treeCount = 100,
            [Description(
                "Maximum number of splits from the root to a leaf in every tree. A deeper tree can fit sharper, more local patterns but is more likely to memorise noise in its own bootstrap sample; the ensemble tolerates deeper trees than a single tree would because their individual overfitting tends to average out across the trees. Default 6.")]
            int maxDepth = 6,
            [Description(
                "Minimum number of training rows required on each side of a candidate split for it to be accepted; a larger value forces coarser, more stable splits and shorter effective trees. Default 5.")]
            int minLeafSize = 5,
            [Description(
                "Share of the training rows drawn, with replacement, into each tree's bootstrap sample. The default of 1 draws as many rows as the training split holds (some repeated, some left out); a smaller share makes each tree see less data and the trees more different from one another, and leaves more rows out of bag to estimate held-out quality from. Default 1.")]
            double sampleFraction = 1d,
            [Description(
                "Whether the ensemble's raw vote fraction is calibrated to a probability by Platt scaling, fitted on the validation split. Default true.")]
            bool calibrate = true,
            [Description(
                "Probability threshold used to turn a calibrated probability into a positive/negative decision for the confusion counts in the evidence. Defaults to 0.5 when not given.")]
            double? decisionThreshold = null,
            [Description(
                "Seed that makes the row split and every tree's bootstrap sample reproducible. Reuse a seed to compare bagged ensembles on identical rows.")]
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
            var submission = await service.SubmitTrainingAsync(new AdvancedAnalyticsBaggingClassifierTrainingDto
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
                TreeCount = treeCount,
                MaxDepth = maxDepth,
                MinLeafSize = minLeafSize,
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