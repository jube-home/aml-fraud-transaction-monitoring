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
    [AdvancedAnalyticsToolGroup("KNearestNeighboursRegressorTrainingRuns")]
    public class KNearestNeighboursRegressorTrainingRunTools(
        AdvancedAnalyticsKNearestNeighboursRegressorTrainingService service)
    {
        [AdvancedAnalyticsTool("SubmitKNearestNeighboursRegressorTraining")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Queues a k nearest neighbours regression training run for a background worker and returns its run id " +
            "straight away, before anything is trained. K nearest neighbours regression is a supervised technique " +
            "that makes no assumption about the shape of the relationship between the predictors and the continuous " +
            "target: rather than fitting a line, a tree or a set of weights, it simply remembers every training row, " +
            "and to score a new row it finds the k training rows that are nearest to it, measured by Euclidean " +
            "distance on standardised predictors, and predicts the plain arithmetic mean of those k neighbours' " +
            "targets. Unlike a classifier that turns its raw vote into a probability, no calibration of any kind is " +
            "applied here: the averaged target already sits on the same scale the target itself was recorded in, so " +
            "there is nothing further to correct. Because distance depends directly on every predictor's numeric " +
            "scale, standardising the predictors before comparing rows matters far more here than for most other " +
            "families: an unstandardised variable recorded in larger units would otherwise dominate every distance " +
            "calculation regardless of how informative it actually is. There is no separate fitting step to compress " +
            "the training rows into a small set of parameters, so every standardised, imputed training row is " +
            "carried inside the stored model version itself and a recall searches those rows directly; this makes " +
            "the stored model grow with the size of the training split, which is an expected characteristic of this " +
            "family rather than a defect. Give the predictor rows and the continuous target for each row inline, one " +
            "array of numbers per case with NaN for a missing value. To follow the run, call " +
            "GetAdvancedAnalyticsTrainingRunStatus with the returned run id until it reports Succeeded, Failed or " +
            "Cancelled, then call GetAdvancedAnalyticsTrainingRunResult. Training never makes the resulting version " +
            "live.")]
        public Task<TrainingRunSubmissionView> SubmitKNearestNeighboursRegressorTrainingAsync(
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
                "Predictor rows, one array of numbers per case with NaN for a missing value. Every one of these rows, after imputation and standardisation, is stored inside the trained model version, since scoring a k nearest neighbours model means searching the stored training rows for the ones nearest to the row being scored.")]
            double[][] inputs,
            [Description(
                "The continuous target for each row, in the same order as the rows, on whatever scale it was originally measured.")]
            double[] outputs,
            [Description(
                "Number of nearest training rows (k) whose targets are averaged to form a prediction, from 1 up to the number of training rows. A small k follows the local shape of the training rows closely but is noisy and sensitive to a single unusual neighbour; a large k gives a smoother, more stable average but increasingly blurs across whatever structure actually relates the predictors to the target, eventually washing it out. There is no single correct k; choose it by comparing held-out performance across a few values rather than by rule of thumb. Default 15.")]
            int neighbourCount = 15,
            [Description(
                "Whether every predictor is z-scored (rescaled to the training mean and standard deviation) before distances are computed. Since distance is computed across every predictor's raw numeric scale at once, leaving this off lets a predictor recorded in larger units dominate every distance purely because of its units, regardless of how informative it actually is; standardising first puts every predictor on the same footing. Default true, and should normally be left on.")]
            bool standardise = true,
            [Description(
                "Share of the rows held back for a validation diagnostic split, reported alongside the training and test metrics but never averaged into a prediction, between 0 and 0.9. Default 0.2.")]
            double validationFraction = 0.2,
            [Description(
                "Share of the rows held back purely to measure quality on rows the model never averaged over, between 0 and 0.9. Together with the validation share it may not exceed 0.9. Default 0.2.")]
            double testFraction = 0.2,
            [Description(
                "Seed that makes the row split reproducible: the same seed and the same rows always produce the same training, validation and test split.")]
            int seed = 1,
            [Description("Optional opaque label kept with a newly created model, for finding it again.")]
            string? correlationKey = null,
            CancellationToken cancellationToken = default)
        {
            return SubmitAsync(instanceId, modelName, variableNames, processingTypeIds, inputs, outputs, neighbourCount,
                standardise, validationFraction, testFraction, seed, correlationKey, cancellationToken);
        }

        private async Task<TrainingRunSubmissionView> SubmitAsync(int instanceId, string modelName,
            string[] variableNames, int[] processingTypeIds, double[][] inputs, double[] outputs, int neighbourCount,
            bool standardise, double validationFraction, double testFraction, int seed, string? correlationKey,
            CancellationToken cancellationToken)
        {
            var submission = await service.SubmitTrainingAsync(
                new AdvancedAnalyticsKNearestNeighboursRegressorTrainingDto
                {
                    AskJoobyAnalyticsInstanceId = instanceId,
                    ModelName = modelName,
                    CorrelationKey = correlationKey,
                    VariableNames = variableNames,
                    ProcessingTypeIds = processingTypeIds,
                    Inputs = inputs,
                    Outputs = outputs,
                    NeighbourCount = neighbourCount,
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