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
    [AdvancedAnalyticsToolGroup("MahalanobisAnomalyTrainingRuns")]
    public class MahalanobisAnomalyTrainingRunTools(AdvancedAnalyticsMahalanobisAnomalyTrainingService service)
    {
        [AdvancedAnalyticsTool("SubmitMahalanobisAnomalyTraining")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Queues an anomaly detection training run for a background worker and returns its run id straight away, " +
            "before anything is trained. The fitted detector measures how unusual a case is with the Mahalanobis " +
            "distance: it fits a mean and a covariance matrix on the predictor rows, then scores any row by how far " +
            "it lies from that mean once every predictor is expressed in units of the training spread and every " +
            "pairwise correlation between predictors has been accounted for; this is what separates it from a plain " +
            "Euclidean distance, which would treat a correlated pair of predictors as two independent sources of " +
            "distance and so overstate how unusual a jointly-typical combination of values is. Give the predictor " +
            "rows and, optionally, a known-anomalous or known-normal flag per row: with labels present and both " +
            "classes represented in the validation split, the raw distance is calibrated into a probability by " +
            "fitting a logistic (Platt) curve from distance to label; without usable labels the model instead " +
            "reports the raw distance, since manufacturing a probability with no evidence for its scale would be " +
            "dishonest. To follow the run, call GetMahalanobisAnomalyTrainingRunStatus with the returned run id " +
            "until it reports Succeeded, Failed or Cancelled, then call GetMahalanobisAnomalyTrainingRunResult. " +
            "Training never makes the resulting version live.")]
        public Task<AdvancedAnalyticsTrainingSubmissionDto> SubmitMahalanobisAnomalyTrainingAsync(
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
                "Predictor rows, one array of numbers per case with NaN for a missing value; the mean and covariance are fitted from these rows once split into training, validation and test shares.")]
            double[][] inputs,
            [Description(
                "Optional outcome for each row: 1 for a known anomaly, 0 for known normal. Leave every row empty to train unsupervised on rows that are assumed mostly normal, with no attempt to grade how unusual is unusual enough.")]
            int[]? labels = null,
            [Description(
                "Seed that makes the row split reproducible. Reuse a seed to compare models fitted on identical rows.")]
            int seed = 1,
            [Description(
                "Share of the rows held back to fit the probability calibration when labels are given, between 0 and 0.9. Default 0.2.")]
            double validationFraction = 0.2,
            [Description("Share of the rows held back purely to measure quality, between 0 and 0.9. Default 0.2.")]
            double testFraction = 0.2,
            [Description(
                "Whether every predictor is z-scored (centred on its training mean and divided by its training standard deviation) before the covariance is fitted, so that predictors on very different scales, such as an amount in currency units next to a small count, contribute to the distance in comparable units rather than the largest-scale predictor dominating it. Default true.")]
            bool standardise = true,
            [Description(
                "Whether to fit a Platt calibration from the distance to a probability when usable labels are given. Default true.")]
            bool calibrate = true,
            [Description(
                "The covariance matrix must be inverted to compute a distance, and with few rows relative to the number of predictors, or with near-duplicate predictors, the raw covariance can be singular or numerically unstable to invert. This shrinkage parameter is the ridge added to the covariance diagonal, as a share of its average variance, and is only applied when the raw covariance cannot be inverted cleanly; it trades a small, documented bias in the covariance estimate for a matrix that is reliably invertible. Default 1e-6.")]
            double ridgeEpsilon = 1e-6,
            [Description(
                "A rough proxy for the covariance matrix's condition number, computed as the ratio of the largest to the smallest variance on its diagonal; a large ratio means some directions in the predictor space are far better determined by the training rows than others, so the fitted distance is trusted unevenly across directions. A warning is raised once this ratio exceeds the threshold given here. Default 1e6.")]
            double highConditionNumberThreshold = 1e6,
            [Description("Optional opaque label kept with a newly created model, for finding it again.")]
            string? correlationKey = null,
            CancellationToken cancellationToken = default)
        {
            return service.SubmitTrainingAsync(new AdvancedAnalyticsMahalanobisAnomalyTrainingDto
            {
                AskJoobyAnalyticsInstanceId = instanceId,
                ModelName = modelName,
                CorrelationKey = correlationKey,
                VariableNames = variableNames,
                ProcessingTypeIds = processingTypeIds,
                Inputs = inputs,
                Labels = labels,
                Seed = seed,
                ValidationFraction = validationFraction,
                TestFraction = testFraction,
                Standardise = standardise,
                Calibrate = calibrate,
                RidgeEpsilon = ridgeEpsilon,
                HighConditionNumberThreshold = highConditionNumberThreshold
            }, cancellationToken);
        }

        [AdvancedAnalyticsTool("GetMahalanobisAnomalyTrainingRunStatus")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [Description(
            "Reads where a queued Mahalanobis anomaly training run has got to: Pending while it waits for a worker, " +
            "Running while it trains, then Succeeded, Failed or Cancelled, which are final. It also returns the " +
            "elapsed seconds and, for a failed or cancelled run, the reason. Poll it every few seconds until Finished " +
            "is true.")]
        public Task<AdvancedAnalyticsTrainingRunStatusDto> GetTrainingRunStatusAsync(
            [Description("The run id returned when the training was submitted.")]
            int runId,
            CancellationToken cancellationToken = default)
        {
            return service.GetRunAsync(runId, cancellationToken);
        }

        [AdvancedAnalyticsTool("CancelMahalanobisAnomalyTrainingRun")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Cancels a Mahalanobis anomaly training run. A run still waiting for a worker is cancelled at once; a " +
            "running one is asked to stop and ends as Cancelled at its next step boundary, and no model is stored " +
            "for it. A finished run is final and is returned unchanged.")]
        public Task<AdvancedAnalyticsTrainingRunStatusDto> CancelTrainingRunAsync(
            [Description("The run id returned when the training was submitted.")]
            int runId,
            CancellationToken cancellationToken = default)
        {
            return service.CancelRunAsync(runId, cancellationToken);
        }

        [AdvancedAnalyticsTool("GetMahalanobisAnomalyTrainingRunResult")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [Description(
            "Returns the outcome of a Mahalanobis anomaly training run and, once it Succeeded, the version it " +
            "produced and its complete evidence: distance percentiles, the covariance condition number, calibration " +
            "and warnings. Read the warnings before deciding whether the version deserves promotion; a run that is " +
            "not finished, or that failed or was cancelled, carries no evidence.")]
        public Task<AdvancedAnalyticsMahalanobisAnomalyTrainingRunResultDto> GetTrainingRunResultAsync(
            [Description("The run id returned when the training was submitted.")]
            int runId,
            CancellationToken cancellationToken = default)
        {
            return service.GetRunResultAsync(runId, cancellationToken);
        }
    }
}