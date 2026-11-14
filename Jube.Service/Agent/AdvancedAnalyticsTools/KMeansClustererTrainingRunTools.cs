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
using Jube.Engine.AdvancedAnalytics.Clustering.Models;
using Jube.Service.Agent.AdvancedAnalyticsTools.Interfaces;
using Jube.Service.Agent.AdvancedAnalyticsTools.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools
{
    [AdvancedAnalyticsToolGroup("KMeansClustererTrainingRuns")]
    public class KMeansClustererTrainingRunTools(IAdvancedAnalyticsClustererTrainingRuns runs)
    {
        [AdvancedAnalyticsTool("SubmitKMeansClustererTraining")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Queues a k-means clustering training run for a background worker and returns its run id straight away, before anything is trained. " +
            "K-means clustering groups rows into k clusters using only the numbers themselves, with no labels and no notion of right or wrong answer: starting from k centres, every row is assigned to whichever centre is nearest, each centre is then moved to the mean of the rows assigned to it, and this repeats until assignments stop changing or the iteration cap is reached. " +
            "Clustering is unsupervised: give the rows inline, one array of numbers per case with NaN for a missing value; every column is a feature that contributes to the distance calculation, there is no label column. " +
            "To follow the run, call GetTrainingRunStatus with the returned run id until it reports Succeeded, Failed or Cancelled, then call GetTrainingRunResult. Training never makes the resulting version live.")]
        public Task<ClustererTrainingRunSubmissionView> SubmitKMeansClustererTrainingAsync(
            [Description("Numeric identifier of the AskJooby Analytics instance the run and its model belong to.")]
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
                "Inline rows, one array of numbers per case with NaN for a missing value. Every column is a feature that the distance between rows is computed over; there is no label.")]
            double[][] inputs,
            [Description(
                "Number of clusters (k) to partition the rows into: k-means fits exactly this many centres and assigns every row to its nearest one. Choosing k is a modelling decision with no single correct answer; more clusters always fit the training rows at least as tightly but can overfit to noise. From 1 up to the number of rows. Default 3.")]
            int clusters = 3,
            [Description(
                "How the k starting centres are chosen before the first row is assigned: KMeansPlusPlus (default) spreads the starting centres apart by favouring rows far from the centres already picked, which converges faster and more reliably than Uniform, which simply picks k rows at random and can start two centres inside the same natural group.")]
            KMeansSeeding seeding = KMeansSeeding.KMeansPlusPlus,
            [Description(
                "Maximum number of assign-then-recentre iterations before the fit stops even if centres are still moving. Default 100.")]
            int maximumIterations = 100,
            [Description(
                "How little the centres may move between one iteration and the next before the fit is treated as converged; smaller values demand a more exact fit at the cost of more iterations. Default 0.00001.")]
            double tolerance = 1e-5,
            [Description(
                "Whether every variable is z-scored (rescaled to the training mean and standard deviation) before clustering, so that a variable measured on a larger numeric scale does not dominate the distance calculation purely because of its units. Default true.")]
            bool standardise = true,
            [Description(
                "Share of the rows held back for a validation diagnostic split rather than used to fit the centres, between 0 and 0.9. Default 0.2.")]
            double validationFraction = 0.2,
            [Description(
                "Share of the rows held back purely to measure cluster quality on rows the centres never saw, between 0 and 0.9. Together with the validation share it may not exceed 0.9. Default 0.2.")]
            double testFraction = 0.2,
            [Description(
                "Seed that makes the row split and the choice of starting centres reproducible: the same seed and the same rows always fit the same centres.")]
            int seed = 1,
            [Description("Optional opaque label kept with a newly created model, for finding it again.")]
            string? correlationKey = null,
            CancellationToken cancellationToken = default)
        {
            return runs.SubmitAsync(new ClustererTrainingRunSubmitRequest
            {
                InstanceId = instanceId,
                ModelName = modelName,
                CorrelationKey = correlationKey,
                VariableNames = variableNames,
                ProcessingTypeIds = processingTypeIds,
                Inputs = inputs,
                Clusters = clusters,
                Seeding = seeding,
                MaximumIterations = maximumIterations,
                Tolerance = tolerance,
                Standardise = standardise,
                ValidationFraction = validationFraction,
                TestFraction = testFraction,
                Seed = seed
            }, cancellationToken);
        }

        [AdvancedAnalyticsTool("GetClustererTrainingRunStatus")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [Description(
            "Reads where a queued clustering training run has got to: Pending while it waits for a worker, Running while it trains, then Succeeded, Failed or Cancelled, which are final. " +
            "It also returns the elapsed seconds and, for a failed or cancelled run, the reason. Poll it every few seconds until Finished is true.")]
        public Task<ClustererTrainingRunStatusView> GetClustererTrainingRunStatusAsync(
            [Description("The run id returned when the training was submitted.")]
            int runId,
            CancellationToken cancellationToken = default)
        {
            return runs.GetStatusAsync(runId, cancellationToken);
        }

        [AdvancedAnalyticsTool("CancelClustererTrainingRun")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Cancels a clustering training run. A run still waiting for a worker is cancelled at once; a running one is asked to stop and ends as Cancelled at its next step boundary, and no model is stored for it. A finished run is final and is returned unchanged.")]
        public Task<ClustererTrainingRunStatusView> CancelClustererTrainingRunAsync(
            [Description("The run id returned when the training was submitted.")]
            int runId,
            CancellationToken cancellationToken = default)
        {
            return runs.CancelAsync(runId, cancellationToken);
        }

        [AdvancedAnalyticsTool("GetClustererTrainingRunResult")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [Description(
            "Returns the outcome of a clustering training run and, once it Succeeded, the version it produced and its complete evidence: per-cluster size and proportion of rows, inertia (the total squared distance of rows to their assigned centre; lower means tighter clusters, and it always falls as k rises so it is not by itself a reason to raise k), a silhouette score where cheap to compute (how much closer, on average, rows sit to their own cluster than to the next nearest one, from -1 to 1, higher meaning better separated) and warnings. " +
            "Read the warnings before deciding whether the version deserves promotion; a run that is not finished, or that failed or was cancelled, carries no evidence.")]
        public Task<ClustererTrainingRunResultView> GetClustererTrainingRunResultAsync(
            [Description("The run id returned when the training was submitted.")]
            int runId,
            CancellationToken cancellationToken = default)
        {
            return runs.GetResultAsync(runId, cancellationToken);
        }
    }
}