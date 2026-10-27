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
    [AdvancedAnalyticsToolGroup("NeuralNetworkClassifierTrainingRuns")]
    public class NeuralNetworkClassifierTrainingRunTools(
        AdvancedAnalyticsNeuralNetworkClassifierTrainingService service)
    {
        [AdvancedAnalyticsTool("SubmitNeuralNetworkClassifierTraining")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Queues the training of a small feedforward neural network classifier for a background worker and " +
            "returns its run id straight away, before anything is trained; use this for any dataset larger than " +
            "trivial, or whenever the rows are already at hand. A feedforward network with one hidden layer maps " +
            "each row of predictors through a layer of hidden units, each a weighted sum of the inputs passed " +
            "through a sigmoid non-linearity, and then combines the hidden units into a single output that is " +
            "itself squashed to a 0 to 1 value and Platt-calibrated on a held-out validation split into a genuine " +
            "probability; unlike logistic regression, the hidden layer lets it fit boundaries that curve rather " +
            "than a single straight line, at the cost of a less directly interpretable fit. Give the predictor rows " +
            "and the binary outcome (0 or 1, both classes required) for each row inline (up to a configured cap), " +
            "or a cache pointer to a stored table whose last column is the label. " +
            "To follow the run, call GetAdvancedAnalyticsTrainingRunStatus with the returned run id until it reports " +
            "Succeeded, Failed or Cancelled, then call GetAdvancedAnalyticsTrainingRunResult. Training never makes " +
            "the resulting version live.")]
        public Task<TrainingRunSubmissionView> SubmitNeuralNetworkClassifierTrainingAsync(
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
                "Inline predictor rows, one array of numbers per case with NaN for a missing value; a missing value is filled with that column's training median. Leave empty when a cache pointer is given.")]
            double[][]? inputs = null,
            [Description(
                "Inline outcomes, one per row (0 or 1). Both classes must be present in the rows used for training. Leave empty when a cache pointer is given.")]
            int[]? labels = null,
            [Description(
                "Number of units in the single hidden layer. Few units (2 to 8) give a small, fast, less flexible network that resists overfitting on small data; more units let the hidden layer represent a more intricate decision boundary at the cost of needing more rows and more epochs to fit reliably and a greater risk of overfitting. From 1 to 128. Default 8.")]
            int hiddenUnits = 8,
            [Description(
                "Number of full passes the gradient descent fit makes over the training rows. Too few leaves the network underfitted with a high training loss still falling; too many, especially with a small network and few rows, can overfit the training split. From 1 to 5000. Default 200.")]
            int epochs = 200,
            [Description(
                "The step size the gradient descent update takes at every epoch, applied to the average gradient over all training rows. A larger learning rate fits faster but risks overshooting and diverging to non-finite weights, which fails the run with a clear error; a smaller one is slower but more stable. Default 0.1.")]
            double learningRate = 0.1,
            [Description(
                "Seed that makes the row split and the network's initial random weights reproducible. Reuse a seed to compare models on identical rows and starting weights.")]
            int seed = 1,
            [Description(
                "Share of the rows held back for a validation split used to fit the Platt calibrator, between 0 and 0.9. Default 0.2.")]
            double validationFraction = 0.2,
            [Description(
                "Share of the rows held back purely to measure quality on rows the fit never saw, between 0 and 0.9. Together with the validation share it may not exceed 0.9. Default 0.2.")]
            double testFraction = 0.2,
            [Description("Whether predictors are z-scored with training statistics before fitting. Default true.")]
            bool standardise = true,
            [Description(
                "Whether the network's raw 0 to 1 output is further Platt-calibrated to a probability on the validation split; even though the output layer is already a sigmoid, a small or lightly trained network's raw output is not automatically well calibrated, so this is on by default.")]
            bool calibrate = true,
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
                labelColumn, hiddenUnits, epochs, learningRate, seed, validationFraction, testFraction, standardise,
                calibrate, correlationKey, cancellationToken);
        }

        private async Task<TrainingRunSubmissionView> SubmitAsync(int instanceId, string modelName,
            string[] variableNames, int[] processingTypeIds, double[][]? inputs, int[]? labels, Guid? cachePointer,
            int labelColumn, int hiddenUnits, int epochs, double learningRate, int seed, double validationFraction,
            double testFraction, bool standardise, bool calibrate, string? correlationKey,
            CancellationToken cancellationToken)
        {
            var submission = await service.SubmitTrainingAsync(new AdvancedAnalyticsNeuralNetworkClassifierTrainingDto
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
                HiddenUnits = hiddenUnits,
                Epochs = epochs,
                LearningRate = learningRate,
                Standardise = standardise,
                Calibrate = calibrate,
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