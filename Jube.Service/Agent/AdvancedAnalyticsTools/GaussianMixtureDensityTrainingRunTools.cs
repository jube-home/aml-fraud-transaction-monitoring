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
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools
{
    [AdvancedAnalyticsToolGroup("GaussianMixtureDensityTrainingRuns")]
    public class GaussianMixtureDensityTrainingRunTools(AdvancedAnalyticsGaussianMixtureDensityTrainingService service)
    {
        [AdvancedAnalyticsTool("SubmitGaussianMixtureDensityTraining")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Queues a Gaussian mixture density estimation training run for a background worker and returns its run " +
            "id straight away, before anything is trained. A Gaussian mixture describes the shape of a set of rows " +
            "as a weighted sum of several multivariate Gaussian (bell-curve) components: each component has its own " +
            "centre and covariance, and the mixture as a whole can trace a shape no single Gaussian could, such as " +
            "several separate clusters or a lopsided spread. It is unsupervised and has no notion of a label to " +
            "predict: fitting it, and scoring a row against it afterwards, describes only how typical or unusual " +
            "that row's combination of values is relative to the rows it was fitted on, expressed as a log-density " +
            "(the natural logarithm of the fitted mixture's probability density at that row). A row that sits where " +
            "the training rows were dense scores a high (less negative) log-density; a row far from every component " +
            "scores a low (very negative) one. This score is never turned into a probability or calibrated against " +
            "labels, because density estimation has no labels to calibrate against; it always reports the raw " +
            "log-density. Give the rows inline, one array of numbers per case with NaN for a missing value; every " +
            "column is a variable that contributes to the fitted mixture, there is no label column. " +
            "To follow the run, call GetGaussianMixtureDensityTrainingRunStatus with the returned run id until it " +
            "reports Succeeded, Failed or Cancelled, then call GetGaussianMixtureDensityTrainingRunResult. Training " +
            "never makes the resulting version live.")]
        public Task<AdvancedAnalyticsTrainingSubmissionDto> SubmitGaussianMixtureDensityTrainingAsync(
            [Description("Numeric identifier of the Advanced Analytics instance the run and its model belong to.")]
            int instanceId,
            [Description(
                "Name of the model the trained version is filed under. An existing model of that name receives a new version; otherwise a model is created.")]
            string modelName,
            [Description("The name of each variable, unique and in column order.")]
            string[] variableNames,
            [Description(
                "Where each variable comes from in the live pipeline, one number per variable name in the same order: 1 payload, 2 dictionary, 3 TTL counter, 4 sanction, 5 abstraction, 6 abstraction calculation, 7 or 99 other.")]
            int[] processingTypeIds,
            [Description(
                "Inline rows, one array of numbers per case with NaN for a missing value. Every column is a variable the mixture is fitted over; there is no label.")]
            double[][] inputs,
            [Description(
                "Number of Gaussian components mixed together to describe the rows' shape. Each component is its own weighted multivariate Gaussian bump; more components can trace a more irregular, multi-modal shape at the cost of needing more rows to fit reliably and being easier to over-fit. Compare BIC and AIC across a few component counts rather than guessing one. Default 3.")]
            int components = 3,
            [Description(
                "Whether every variable is z-scored (rescaled to the training mean and standard deviation) before the fit, so a variable measured on a larger numeric scale does not dominate the fitted covariances purely because of its units. Default true.")]
            bool standardise = true,
            [Description("Maximum number of Expectation-Maximisation iterations. Default 100.")]
            int maxIterations = 100,
            [Description(
                "Convergence tolerance on the Expectation-Maximisation log-likelihood improvement. Default 1e-5.")]
            double tolerance = 1e-5,
            [Description(
                "Each component's covariance matrix must be inverted to score a row, and with few rows relative to the number of variables, or with near-duplicate variables, a component's raw covariance can be singular or numerically unstable to invert. This shrinkage parameter is the ridge added to a component's covariance diagonal, as a share of its average variance, used both during the fit itself and, only if still needed afterwards, again when the fitted covariance is inverted for recall. Default 1e-6.")]
            double regularizationEpsilon = 1e-6,
            [Description(
                "Share of the rows held back for a validation diagnostic split, between 0 and 0.9. Default 0.2.")]
            double validationFraction = 0.2,
            [Description(
                "Share of the rows held back purely to measure held-out log-likelihood, between 0 and 0.9. Together with the validation share it may not exceed 0.9. Default 0.2.")]
            double testFraction = 0.2,
            [Description(
                "Seed that makes the row split reproducible. The mixture fit itself is only reproducible at single-threaded parallelism and, even then, is not guaranteed to reproduce bit for bit between runs; treat repeated fits on the same rows as close rather than identical.")]
            int seed = 1,
            [Description("Optional opaque label kept with a newly created model, for finding it again.")]
            string? correlationKey = null,
            CancellationToken cancellationToken = default)
        {
            return service.SubmitTrainingAsync(new AdvancedAnalyticsGaussianMixtureDensityTrainingDto
            {
                AdvancedAnalyticsInstanceId = instanceId,
                ModelName = modelName,
                CorrelationKey = correlationKey,
                VariableNames = variableNames,
                ProcessingTypeIds = processingTypeIds,
                Inputs = inputs,
                Components = components,
                Standardise = standardise,
                MaxIterations = maxIterations,
                Tolerance = tolerance,
                RegularizationEpsilon = regularizationEpsilon,
                ValidationFraction = validationFraction,
                TestFraction = testFraction,
                Seed = seed
            }, cancellationToken);
        }

        [AdvancedAnalyticsTool("GetGaussianMixtureDensityTrainingRunStatus")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [Description(
            "Reads where a queued Gaussian mixture density training run has got to: Pending while it waits for a " +
            "worker, Running while it trains, then Succeeded, Failed or Cancelled, which are final. It also returns " +
            "the elapsed seconds and, for a failed or cancelled run, the reason. Poll it every few seconds until " +
            "Finished is true.")]
        public Task<AdvancedAnalyticsTrainingRunStatusDto> GetTrainingRunStatusAsync(
            [Description("The run id returned when the training was submitted.")]
            int runId,
            CancellationToken cancellationToken = default)
        {
            return service.GetRunAsync(runId, cancellationToken);
        }

        [AdvancedAnalyticsTool("CancelGaussianMixtureDensityTrainingRun")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Cancels a Gaussian mixture density training run. A run still waiting for a worker is cancelled at once; " +
            "a running one is asked to stop and ends as Cancelled at its next step boundary, and no model is stored " +
            "for it. A finished run is final and is returned unchanged.")]
        public Task<AdvancedAnalyticsTrainingRunStatusDto> CancelTrainingRunAsync(
            [Description("The run id returned when the training was submitted.")]
            int runId,
            CancellationToken cancellationToken = default)
        {
            return service.CancelRunAsync(runId, cancellationToken);
        }

        [AdvancedAnalyticsTool("GetGaussianMixtureDensityTrainingRunResult")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [Description(
            "Returns the outcome of a Gaussian mixture density training run and, once it Succeeded, the version it " +
            "produced and its complete evidence: each component's weight, mean and covariance, the mean per-row " +
            "log-likelihood on the train, validation and held-out test splits, BIC and AIC, whether the " +
            "Expectation-Maximisation fit converged and warnings. Read the warnings and compare the validation and " +
            "test log-likelihoods before deciding whether the version deserves promotion; a run that is not " +
            "finished, or that failed or was cancelled, carries no evidence.")]
        public Task<AdvancedAnalyticsGaussianMixtureDensityTrainingRunResultDto> GetTrainingRunResultAsync(
            [Description("The run id returned when the training was submitted.")]
            int runId,
            CancellationToken cancellationToken = default)
        {
            return service.GetRunResultAsync(runId, cancellationToken);
        }
    }
}