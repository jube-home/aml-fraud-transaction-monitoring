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
using Jube.Service.Agent.AdvancedAnalyticsTools.Interfaces;
using Jube.Service.Agent.AdvancedAnalyticsTools.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools
{
    [AdvancedAnalyticsToolGroup("PrincipalComponentTrainingRuns")]
    public class PrincipalComponentTrainingRunTools(IAdvancedAnalyticsPrincipalComponentTrainingRuns runs)
    {
        [AdvancedAnalyticsTool("SubmitPrincipalComponentTraining")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Queues a principal component training run for a background worker and returns its run id straight away, before anything is trained. " +
            "Principal component analysis re-expresses a set of correlated variables as a smaller set of uncorrelated components, each a weighted sum of the original variables: the first component is the single direction along which the rows vary the most, the second is the direction of the next most variance that is perpendicular to the first, and so on, until the components collectively account for as much of the original variance as the caller asks for. " +
            "It is unsupervised and has no notion of a correct answer or a label to predict; it is a description of how the rows are shaped, useful for compressing many redundant variables into a few informative ones, for visualising high-dimensional data, or for feeding a smaller number of decorrelated inputs into a downstream model. " +
            "Give the rows inline, one array of numbers per case with NaN for a missing value; every column is a variable that contributes to the fitted directions, there is no label column. " +
            "To follow the run, call GetPrincipalComponentTrainingRunStatus with the returned run id until it reports Succeeded, Failed or Cancelled, then call GetPrincipalComponentTrainingRunResult. Training never makes the resulting version live.")]
        public Task<PrincipalComponentTrainingRunSubmissionView> SubmitPrincipalComponentTrainingAsync(
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
                "Inline rows, one array of numbers per case with NaN for a missing value. Every column is a variable the components are fitted over; there is no label.")]
            double[][] inputs,
            [Description(
                "Whether every variable is z-scored (rescaled to the training mean and standard deviation) before the fit, so a variable measured on a larger numeric scale does not dominate the components purely because of its units. This fits components on the correlation matrix rather than the covariance matrix, which is almost always the right choice when variables are on different scales. Default true.")]
            bool standardise = true,
            [Description(
                "Cumulative share of the total variance the retained components must together explain, strictly greater than 0 and at most 1: components are kept, most important first, until this share is reached. A higher threshold keeps more components and discards less information, at the cost of a less compact representation. Default 0.95.")]
            double varianceThreshold = 0.95,
            [Description(
                "Optional hard cap on how many components are retained, applied together with the variance threshold: whichever limit is reached first decides the count. Leave empty to let the variance threshold alone decide.")]
            int? maximumComponents = null,
            [Description(
                "Share of the rows held back for a validation diagnostic split rather than used to fit the components, between 0 and 0.9. Default 0.2.")]
            double validationFraction = 0.2,
            [Description(
                "Share of the rows held back purely to measure how well the components reconstruct rows they never influenced, between 0 and 0.9. Together with the validation share it may not exceed 0.9. Default 0.2.")]
            double testFraction = 0.2,
            [Description(
                "Seed that makes the row split reproducible: the same seed and the same rows always fit the same components.")]
            int seed = 1,
            [Description("Optional opaque label kept with a newly created model, for finding it again.")]
            string? correlationKey = null,
            CancellationToken cancellationToken = default)
        {
            return runs.SubmitAsync(new PrincipalComponentTrainingRunSubmitRequest
            {
                InstanceId = instanceId,
                ModelName = modelName,
                CorrelationKey = correlationKey,
                VariableNames = variableNames,
                ProcessingTypeIds = processingTypeIds,
                Inputs = inputs,
                Standardise = standardise,
                VarianceThreshold = varianceThreshold,
                MaximumComponents = maximumComponents,
                ValidationFraction = validationFraction,
                TestFraction = testFraction,
                Seed = seed
            }, cancellationToken);
        }

        [AdvancedAnalyticsTool("GetPrincipalComponentTrainingRunStatus")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [Description(
            "Reads where a queued principal component training run has got to: Pending while it waits for a worker, Running while it trains, then Succeeded, Failed or Cancelled, which are final. " +
            "It also returns the elapsed seconds and, for a failed or cancelled run, the reason. Poll it every few seconds until Finished is true.")]
        public Task<PrincipalComponentTrainingRunStatusView> GetPrincipalComponentTrainingRunStatusAsync(
            [Description("The run id returned when the training was submitted.")]
            int runId,
            CancellationToken cancellationToken = default)
        {
            return runs.GetStatusAsync(runId, cancellationToken);
        }

        [AdvancedAnalyticsTool("CancelPrincipalComponentTrainingRun")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Cancels a principal component training run. A run still waiting for a worker is cancelled at once; a running one is asked to stop and ends as Cancelled at its next step boundary, and no model is stored for it. A finished run is final and is returned unchanged.")]
        public Task<PrincipalComponentTrainingRunStatusView> CancelPrincipalComponentTrainingRunAsync(
            [Description("The run id returned when the training was submitted.")]
            int runId,
            CancellationToken cancellationToken = default)
        {
            return runs.CancelAsync(runId, cancellationToken);
        }

        [AdvancedAnalyticsTool("GetPrincipalComponentTrainingRunResult")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [Description(
            "Returns the outcome of a principal component training run and, once it Succeeded, the version it produced and its complete evidence: the eigenvalue of every available component (its share of the total variance, largest first), the cumulative variance the retained components explain together, how many components were actually kept, the mean per-row reconstruction error on the training, validation and held-out test splits (how far a row's standardised values sit from what its retained component scores alone can rebuild; lower means the retained components lose less information) and warnings. " +
            "Read the warnings and the cumulative variance before deciding whether the version deserves promotion; a run that is not finished, or that failed or was cancelled, carries no evidence.")]
        public Task<PrincipalComponentTrainingRunResultView> GetPrincipalComponentTrainingRunResultAsync(
            [Description("The run id returned when the training was submitted.")]
            int runId,
            CancellationToken cancellationToken = default)
        {
            return runs.GetResultAsync(runId, cancellationToken);
        }
    }
}