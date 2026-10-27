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
    [AdvancedAnalyticsToolGroup("NeuralNetworkRegressorTrainingRuns")]
    public class NeuralNetworkRegressorTrainingRunTools(AdvancedAnalyticsNeuralNetworkRegressorTrainingService service)
    {
        [AdvancedAnalyticsTool("SubmitNeuralNetworkRegressorTraining")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Queues the training of a small feedforward neural network regressor for a background worker and " +
            "returns its run id straight away, before anything is trained; use this for any dataset larger than " +
            "trivial, or whenever the rows are already at hand. A feedforward network with one hidden layer maps " +
            "each row of predictors through a layer of hidden units, each a weighted sum of the inputs passed " +
            "through a sigmoid non-linearity, and then combines the hidden units into a single further sigmoid- " +
            "bounded output; because that bounded output cannot by itself represent an outcome of arbitrary scale, " +
            "the continuous outcome is compressed onto the network's own 0 to 1 output range before training and " +
            "every prediction is mapped back onto the outcome's original scale afterwards. Unlike ordinary least " +
            "squares regression, the hidden layer lets it fit a curved response surface rather than a single flat " +
            "hyperplane, at the cost of a less directly interpretable fit and no exact per-input coefficient; the " +
            "reported per-input contribution is instead a local gradient, honest about being a linear approximation " +
            "at each recalled point rather than an exact decomposition. Give the predictor rows and the continuous " +
            "outcome for each row inline (up to a configured cap). To follow the run, call " +
            "GetAdvancedAnalyticsTrainingRunStatus with the returned run id until it reports Succeeded, Failed or " +
            "Cancelled, then call GetAdvancedAnalyticsTrainingRunResult. Training never makes the resulting version " +
            "live.")]
        public Task<TrainingRunSubmissionView> SubmitNeuralNetworkRegressorTrainingAsync(
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
                "Predictor rows, one array of numbers per case with NaN for a missing value; a missing value is filled with that column's training median.")]
            double[][] inputs,
            [Description(
                "The continuous outcome for each row, in the same order as the rows; if every row shares the same value the network is fitted to reproduce that constant and the reported R squared is undefined.")]
            double[] outputs,
            [Description(
                "Number of units in the single hidden layer. Few units (2 to 8) give a small, fast, less flexible network that resists overfitting on small data; more units let the hidden layer represent a more intricate response surface at the cost of needing more rows and more epochs to fit reliably and a greater risk of overfitting. From 1 to 128. Default 8.")]
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
                "Share of the rows held back for a validation diagnostic split, reported alongside the training and test metrics but not used to fit the weights, between 0 and 0.9. Default 0.2.")]
            double validationFraction = 0.2,
            [Description(
                "Share of the rows held back purely to measure quality on rows the fit never saw, between 0 and 0.9. Together with the validation share it may not exceed 0.9. Default 0.2.")]
            double testFraction = 0.2,
            [Description("Whether predictors are z-scored with training statistics before fitting. Default true.")]
            bool standardise = true,
            [Description("Optional opaque label kept with a newly created model, for finding it again.")]
            string? correlationKey = null,
            CancellationToken cancellationToken = default)
        {
            return SubmitAsync(instanceId, modelName, variableNames, processingTypeIds, inputs, outputs, hiddenUnits,
                epochs, learningRate, seed, validationFraction, testFraction, standardise, correlationKey,
                cancellationToken);
        }

        private async Task<TrainingRunSubmissionView> SubmitAsync(int instanceId, string modelName,
            string[] variableNames, int[] processingTypeIds, double[][] inputs, double[] outputs, int hiddenUnits,
            int epochs, double learningRate, int seed, double validationFraction, double testFraction,
            bool standardise, string? correlationKey, CancellationToken cancellationToken)
        {
            var submission = await service.SubmitTrainingAsync(new AdvancedAnalyticsNeuralNetworkRegressorTrainingDto
            {
                AskJoobyAnalyticsInstanceId = instanceId,
                ModelName = modelName,
                CorrelationKey = correlationKey,
                VariableNames = variableNames,
                ProcessingTypeIds = processingTypeIds,
                Inputs = inputs,
                Outputs = outputs,
                HiddenUnits = hiddenUnits,
                Epochs = epochs,
                LearningRate = learningRate,
                Standardise = standardise,
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