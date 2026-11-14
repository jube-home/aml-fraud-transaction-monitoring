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
using Jube.Engine.AdvancedAnalytics.Classification.Models;
using Jube.Engine.AdvancedAnalytics.Training;
using Jube.Service.Agent.AdvancedAnalyticsTools.Models;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools
{
    [AdvancedAnalyticsToolGroup("DecisionTreeClassifierTrainingRuns")]
    public class DecisionTreeClassifierTrainingRunTools(AdvancedAnalyticsDecisionTreeClassifierTrainingService service)
    {
        [AdvancedAnalyticsTool("SubmitDecisionTreeClassifierTraining")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Queues a decision tree classification training run for a background worker and returns its run id " +
            "straight away, before anything is trained. A decision tree is a nested sequence of yes/no questions, " +
            "each of the form 'is this variable above or below some threshold': starting from every training row " +
            "together, the fit repeatedly picks whichever single variable and threshold splits the rows into two " +
            "groups that are the most different from each other in their share of the event of interest, and keeps " +
            "splitting each resulting group the same way until a group is too small to split further or the maximum " +
            "depth is reached. Recall then walks a new row down the same sequence of questions to a leaf, and " +
            "predicts the (optionally calibrated) share of training rows with the event of interest that ended up in " +
            "that leaf. Unlike a linear or logistic model, a tree needs no scaling of its inputs and can represent " +
            "an interaction between two variables (the effect of one depending on the value of another) without " +
            "being told to; the price is that a single tree can be unstable, since a small change in the training " +
            "rows can send a split to a different variable or threshold entirely. Training is supervised: give the " +
            "predictor rows and the 0/1 outcome for each row inline, or a cache pointer to a stored table whose " +
            "last column is the label. " +
            "To follow the run, call GetAdvancedAnalyticsTrainingRunStatus with the returned run id until it reports " +
            "Succeeded, Failed or Cancelled, then call GetAdvancedAnalyticsTrainingRunResult. Training never makes " +
            "the resulting version live.")]
        public Task<TrainingRunSubmissionView> SubmitDecisionTreeClassifierTrainingAsync(
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
                "Inline predictor rows, one array of numbers per case with NaN for a missing value; a missing value is filled with the training median of its column both when fitting and when the model is later recalled. Leave empty when a cache pointer is given.")]
            double[][]? inputs = null,
            [Description(
                "Inline outcomes, one per row in the same order as the rows: 1 for the event of interest, otherwise 0. Both values must appear in the training split. Leave empty when a cache pointer is given.")]
            int[]? labels = null,
            [Description(
                "Deepest a root-to-leaf chain of questions may run before the fit is forced to stop splitting further, from 1 to 30. A shallow tree generalises better but can miss real structure; a deep one fits the training rows more closely but is more likely to be fitting noise. Default 6.")]
            int maxDepth = 6,
            [Description(
                "Fewest training rows a leaf is allowed to hold; a candidate split that would leave fewer rows than this on either side is rejected, which keeps the tree from making decisions on a handful of rows. From 1 upward. Default 5.")]
            int minimumLeafSize = 5,
            [Description(
                "None fits the split search and the leaf shares as the rows are; Balanced reweights every row so the rarer of the two classes counts as much in total as the common one, both when choosing splits and, if requested, when fitting the calibration.")]
            DecisionTreeClassWeighting classWeighting = DecisionTreeClassWeighting.None,
            [Description(
                "Whether to fit a Platt calibration on the validation rows, mapping the tree's raw leaf share of positives onto a probability that is honest about how often the event actually occurs. Default true.")]
            bool calibrate = true,
            [Description(
                "Probability at or above which a row counts as predicted positive in the confusion counts reported in the evidence. Default 0.5.")]
            double decisionThreshold = 0.5,
            [Description(
                "Seed that makes the row split reproducible: the same seed and the same rows always fit the same tree.")]
            int seed = 1,
            [Description(
                "Share of the rows held back to fit the probability calibration rather than to grow the tree, between 0 and 0.9. Default 0.2.")]
            double validationFraction = 0.2,
            [Description(
                "Share of the rows held back purely to measure quality on rows the tree never saw, between 0 and 0.9. Together with the validation share it may not exceed 0.9. Default 0.2.")]
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
            return SubmitAsync(instanceId, modelName, variableNames, processingTypeIds, inputs, labels, cachePointer,
                labelColumn, maxDepth, minimumLeafSize, classWeighting, calibrate, decisionThreshold, seed,
                validationFraction, testFraction, correlationKey, cancellationToken);
        }

        private async Task<TrainingRunSubmissionView> SubmitAsync(int instanceId, string modelName,
            string[] variableNames, int[] processingTypeIds, double[][]? inputs, int[]? labels, Guid? cachePointer,
            int labelColumn, int maxDepth, int minimumLeafSize, DecisionTreeClassWeighting classWeighting,
            bool calibrate, double decisionThreshold, int seed, double validationFraction, double testFraction,
            string? correlationKey, CancellationToken cancellationToken)
        {
            var submission = await service.SubmitTrainingAsync(new AdvancedAnalyticsDecisionTreeClassifierTrainingDto
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
                MaxDepth = maxDepth,
                MinimumLeafSize = minimumLeafSize,
                ClassWeighting = classWeighting,
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