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
using Jube.Engine.AdvancedAnalytics.Regression.Models;
using Jube.Service.Agent.AdvancedAnalyticsTools.Models;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools
{
    [AdvancedAnalyticsToolGroup("LinearRegressionTrainingRuns")]
    public class LinearRegressionTrainingRunTools(AdvancedAnalyticsLinearRegressionTrainingService service)
    {
        [AdvancedAnalyticsTool("SubmitLinearRegressionTraining")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Queues a multiple linear regression (ordinary least squares) training run for a background worker and " +
            "returns its run id straight away, before anything is trained; use this for any dataset larger than " +
            "trivial, or whenever the rows are already at hand. Give the predictor rows and the continuous outcome " +
            "for each row inline (up to a configured cap). To follow the run, call GetTrainingRunStatus with the " +
            "returned run id until it reports Succeeded, Failed or Cancelled, then call GetTrainingRunResult. " +
            "Training never makes the resulting version live.")]
        public Task<TrainingRunSubmissionView> SubmitLinearRegressionTrainingAsync(
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
            [Description("Predictor rows, one array of numbers per case with NaN for a missing value.")]
            double[][] inputs,
            [Description("The continuous outcome for each row, in the same order as the rows.")]
            double[] outputs,
            [Description(
                "Seed that makes the row split reproducible. Reuse a seed to compare models on identical rows.")]
            int seed = 1,
            [Description(
                "Share of the rows held back for a validation diagnostic split, reported alongside the training and test metrics but not used to fit the coefficients, between 0 and 0.9. Default 0.2.")]
            double validationFraction = 0.2,
            [Description(
                "Share of the rows held back purely to measure quality on rows the fit never saw, between 0 and 0.9. Together with the validation share it may not exceed 0.9. Default 0.2.")]
            double testFraction = 0.2,
            [Description(
                "ImputeMedianWithIndicators fills a missing value with the training median and adds a missing flag; Reject fails on any missing value.")]
            LinearMissingValuePolicy missingValuePolicy = LinearMissingValuePolicy.ImputeMedianWithIndicators,
            [Description("Whether predictors are z-scored with training statistics before fitting. Default true.")]
            bool standardise = true,
            [Description("Optional opaque label kept with a newly created model, for finding it again.")]
            string? correlationKey = null,
            CancellationToken cancellationToken = default)
        {
            return SubmitAsync(instanceId, modelName, variableNames, processingTypeIds, inputs, outputs, seed,
                validationFraction, testFraction, missingValuePolicy, standardise, correlationKey, cancellationToken);
        }

        [AdvancedAnalyticsTool("GetLinearRegressionTrainingRunStatus")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [Description(
            "Reads where a queued linear regression training run has got to: Pending while it waits for a worker, " +
            "Running while it trains, then Succeeded, Failed or Cancelled, which are final. It also returns the " +
            "elapsed seconds and, for a failed or cancelled run, the reason; run time grows in step with the amount " +
            "of data. Poll it every few seconds until Finished is true.")]
        public async Task<TrainingRunStatusView> GetTrainingRunStatusAsync(
            [Description("The run id returned when the training was submitted.")]
            int runId,
            CancellationToken cancellationToken = default)
        {
            var status = await service.GetRunAsync(runId, cancellationToken).ConfigureAwait(false);

            return ToView(status);
        }

        [AdvancedAnalyticsTool("CancelLinearRegressionTrainingRun")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Cancels a linear regression training run. A run still waiting for a worker is cancelled at once; a " +
            "running one is asked to stop and ends as Cancelled at its next step boundary or inside the fit, and no " +
            "model is stored for it. A finished run is final and is returned unchanged.")]
        public async Task<TrainingRunStatusView> CancelTrainingRunAsync(
            [Description("The run id returned when the training was submitted.")]
            int runId,
            CancellationToken cancellationToken = default)
        {
            var status = await service.CancelRunAsync(runId, cancellationToken).ConfigureAwait(false);

            return ToView(status);
        }

        [AdvancedAnalyticsTool("GetLinearRegressionTrainingRunResult")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [Description(
            "Returns the outcome of a linear regression training run and, once it Succeeded, the version it produced " +
            "and its complete evidence: held-out metrics, coefficient significance and warnings. Read the warnings " +
            "and the test metrics from this evidence before deciding whether the version deserves promotion; a run " +
            "that is not finished, or that failed or was cancelled, carries no evidence.")]
        public async Task<LinearRegressionTrainingRunResultView> GetTrainingRunResultAsync(
            [Description("The run id returned when the training was submitted.")]
            int runId,
            CancellationToken cancellationToken = default)
        {
            var result = await service.GetRunResultAsync(runId, cancellationToken).ConfigureAwait(false);

            return new LinearRegressionTrainingRunResultView
            {
                Run = ToView(result.Run),
                TrainedModelId = result.TrainedModelId,
                TrainedModelVersionId = result.TrainedModelVersionId,
                VersionNumber = result.VersionNumber,
                Activated = result.Activated,
                Evidence = result.Evidence
            };
        }

        private async Task<TrainingRunSubmissionView> SubmitAsync(int instanceId, string modelName,
            string[] variableNames, int[] processingTypeIds, double[][] inputs, double[] outputs, int seed,
            double validationFraction, double testFraction, LinearMissingValuePolicy missingValuePolicy,
            bool standardise, string? correlationKey, CancellationToken cancellationToken)
        {
            var submission = await service.SubmitTrainingAsync(new AdvancedAnalyticsLinearRegressionTrainingDto
            {
                AdvancedAnalyticsInstanceId = instanceId,
                ModelName = modelName,
                CorrelationKey = correlationKey,
                VariableNames = variableNames,
                ProcessingTypeIds = processingTypeIds,
                Inputs = inputs,
                Outputs = outputs,
                Seed = seed,
                ValidationFraction = validationFraction,
                TestFraction = testFraction,
                MissingValuePolicy = missingValuePolicy,
                Standardise = standardise
            }, cancellationToken).ConfigureAwait(false);

            return new TrainingRunSubmissionView
            {
                RunId = submission.RunId,
                Status = submission.Status.ToString(),
                HowToPoll = submission.HowToPoll
            };
        }

        private static TrainingRunStatusView ToView(AdvancedAnalyticsTrainingRunStatusDto dto)
        {
            return new TrainingRunStatusView
            {
                RunId = dto.RunId,
                Status = dto.Status.ToString(),
                ElapsedSeconds = dto.ElapsedSeconds,
                Error = dto.Error,
                Finished = dto.Finished,
                TrainedModelVersionId = dto.TrainedModelVersionId
            };
        }
    }
}