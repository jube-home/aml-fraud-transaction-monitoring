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
using Jube.Service.Agent.AdvancedAnalyticsTools.Models;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools
{
    [AdvancedAnalyticsToolGroup("DecisionTreeRegressorTrainingRuns")]
    public class DecisionTreeRegressorTrainingRunTools(AdvancedAnalyticsDecisionTreeRegressorTrainingService service)
    {
        [AdvancedAnalyticsTool("SubmitDecisionTreeRegressorTraining")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Queues a decision tree regression training run for a background worker and returns its run id straight " +
            "away, before anything is trained. A regression tree is a nested sequence of yes/no questions, each of " +
            "the form 'is this variable above or below some threshold': starting from every training row together, " +
            "the fit repeatedly picks whichever single variable and threshold splits the rows into two groups whose " +
            "outcome values are, taken together, as tightly clustered around their own two group means as possible " +
            "(equivalently, the split that removes the most variance from the outcome), and keeps splitting each " +
            "resulting group the same way until a group is too small to split further or the maximum depth is " +
            "reached. Recall then walks a new row down the same sequence of questions to a leaf and predicts the " +
            "mean outcome of the training rows that ended up in that leaf. Unlike a linear model, a tree needs no " +
            "scaling of its inputs, can represent a step change or a threshold effect exactly, and can represent an " +
            "interaction between two variables without being told to; the price is that a single tree can be " +
            "unstable, since a small change in the training rows can send a split to a different variable or " +
            "threshold entirely, and its predictions are a small set of flat values rather than a smooth surface. " +
            "Training is supervised: give the predictor rows and a continuous outcome for each row inline. " +
            "To follow the run, call GetAdvancedAnalyticsTrainingRunStatus with the returned run id until it reports " +
            "Succeeded, Failed or Cancelled, then call GetAdvancedAnalyticsTrainingRunResult. Training never makes " +
            "the resulting version live.")]
        public Task<TrainingRunSubmissionView> SubmitDecisionTreeRegressorTrainingAsync(
            [Description("Numeric identifier of the Advanced Analytics instance that the run and its trained model " +
                         "belong to; every model name is only unique within one instance.")]
            int instanceId,
            [Description("Name of the model the trained version is filed under. An existing model of that name in " +
                         "the same instance receives this run's result as a new, additional version; if no model of " +
                         "that name exists yet, one is created.")]
            string modelName,
            [Description("The name of each predictor column, one per column, unique within the list and given in " +
                         "the same left-to-right order as the columns inside each row of inputs.")]
            string[] variableNames,
            [Description(
                "Where each predictor comes from in the live pipeline, one number per entry in variableNames " +
                "and in the same order: 1 payload, 2 dictionary, 3 TTL counter, 4 sanction, 5 abstraction, " +
                "6 abstraction calculation, 7 or 99 other. The live pipeline consults this when it later " +
                "looks each value up to score a case with the trained model.")]
            int[] processingTypeIds,
            [Description("Predictor rows: one array of numbers per case, every array the same length as " +
                         "variableNames. Use NaN for a value that is missing on that row; a missing value is filled " +
                         "with that column's training median, both while the tree is being fitted and later whenever " +
                         "the trained model is recalled.")]
            double[][] inputs,
            [Description("The continuous outcome to predict, one number per row and in the same order as inputs; " +
                         "every value must be an ordinary finite number, since this is a regression target rather " +
                         "than a 0/1 label.")]
            double[] outputs,
            [Description("Deepest a root-to-leaf chain of yes/no questions may run before the fit is forced to stop " +
                         "splitting further, from 1 to 30. A shallow tree generalises better but may miss real " +
                         "structure in the outcome; a deep one fits the training rows more closely but is more " +
                         "likely to be fitting noise rather than signal. Default 6.")]
            int maxDepth = 6,
            [Description("Fewest training rows a leaf is allowed to hold; a candidate split that would leave fewer " +
                         "rows than this on either side is rejected, which keeps the tree from basing a prediction " +
                         "on a handful of rows and their possibly unrepresentative outcome. From 1 upward. Default 5.")]
            int minimumLeafSize = 5,
            [Description("Seed that makes the row split into training, validation and test rows reproducible: the " +
                         "same seed and the same rows always produce the same split and therefore the same tree.")]
            int seed = 1,
            [Description("Share of the rows held back to monitor the fit rather than to grow the tree, between 0 " +
                         "and 0.9. This decision tree family does not currently use the validation rows to tune " +
                         "anything automatically; they are still reported separately in the evidence so a reviewer " +
                         "can see how the tree behaves on rows it did not grow on. Default 0.2.")]
            double validationFraction = 0.2,
            [Description("Share of the rows held back purely to measure quality on rows the tree never saw during " +
                         "fitting, between 0 and 0.9. Together with the validation share it may not exceed 0.9. " +
                         "These are the honest, held-out metrics to judge the tree by. Default 0.2.")]
            double testFraction = 0.2,
            [Description("Optional opaque label kept alongside a newly created model, useful only for the caller to " +
                         "recognise or search for this model again later; it has no effect on training itself.")]
            string? correlationKey = null,
            CancellationToken cancellationToken = default)
        {
            return SubmitAsync(instanceId, modelName, variableNames, processingTypeIds, inputs, outputs, maxDepth,
                minimumLeafSize, seed, validationFraction, testFraction, correlationKey, cancellationToken);
        }

        private async Task<TrainingRunSubmissionView> SubmitAsync(int instanceId, string modelName,
            string[] variableNames, int[] processingTypeIds, double[][] inputs, double[] outputs, int maxDepth,
            int minimumLeafSize, int seed, double validationFraction, double testFraction, string? correlationKey,
            CancellationToken cancellationToken)
        {
            var submission = await service.SubmitTrainingAsync(new AdvancedAnalyticsDecisionTreeRegressorTrainingDto
            {
                AdvancedAnalyticsInstanceId = instanceId,
                ModelName = modelName,
                CorrelationKey = correlationKey,
                VariableNames = variableNames,
                ProcessingTypeIds = processingTypeIds,
                Inputs = inputs,
                Outputs = outputs,
                MaxDepth = maxDepth,
                MinimumLeafSize = minimumLeafSize,
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