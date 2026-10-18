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
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools
{
    [AdvancedAnalyticsToolGroup("KMedoidsClustererTrainingRuns")]
    public class KMedoidsClustererTrainingRunTools(AdvancedAnalyticsKMedoidsClustererTrainingService service)
    {
        [AdvancedAnalyticsTool("SubmitKMedoidsClustererTraining")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Queues a k-medoids clustering training run for a background worker and returns its run id straight " +
            "away, before anything is trained. K-medoids partitions rows into k clusters much as k-means does, but " +
            "every cluster centre (its medoid) is required to be one of the actual rows rather than an averaged " +
            "point: starting from k candidate rows chosen by a greedy build that favours rows spread apart from one " +
            "another, every row is assigned to whichever candidate is nearest, and the search then repeatedly tries " +
            "swapping a non-medoid row in for the medoid of its cluster, keeping the swap only when it lowers the " +
            "total distance of every row to its cluster's medoid, until no further swap helps or the iteration cap " +
            "is reached. Because a medoid is always a genuine row, this tends to be more robust to outliers than " +
            "k-means and works with a plain distance rather than an averageable coordinate space, though only the " +
            "straight-line (Euclidean) distance on the standardised variables is used here. Clustering is " +
            "unsupervised: give the rows inline, one array of numbers per case with NaN for a missing value; every " +
            "column is a feature that contributes to the distance calculation, there is no label column. To follow " +
            "the run, call GetAdvancedAnalyticsTrainingRunStatus with the returned run id until it reports Succeeded, " +
            "Failed or Cancelled, then call GetAdvancedAnalyticsTrainingRunResult. Training never makes the " +
            "resulting version live.")]
        public Task<AdvancedAnalyticsTrainingSubmissionDto> SubmitKMedoidsClustererTrainingAsync(
            [Description("Numeric identifier of the Advanced Analytics instance that the run and its trained model " +
                         "belong to; every model name is only unique within one instance.")]
            int instanceId,
            [Description("Name of the model the trained version is filed under. An existing model of that name in " +
                         "the same instance receives this run's result as a new version; otherwise a model is created.")]
            string modelName,
            [Description("The name of each variable, one per column, unique within the list and given in the same " +
                         "left-to-right order as the columns inside each row of inputs.")]
            string[] variableNames,
            [Description("Where each variable comes from in the live pipeline, one number per entry in variableNames " +
                         "and in the same order: 1 payload, 2 dictionary, 3 TTL counter, 4 sanction, 5 abstraction, " +
                         "6 abstraction calculation, 7 or 99 other. The live pipeline consults this when it later " +
                         "looks each value up to score a case with the trained model.")]
            int[] processingTypeIds,
            [Description("Inline rows, one array of numbers per case with NaN for a value that is missing on that " +
                         "row; a missing value is filled with that column's training median, both while the medoids " +
                         "are fitted and later whenever the trained model is recalled. Every column contributes to " +
                         "the distance between rows; there is no label.")]
            double[][] inputs,
            [Description("Number of clusters (k) to partition the rows into: k-medoids fits exactly this many " +
                         "medoids, each a genuine training row, and assigns every row to its nearest one. Choosing k " +
                         "is a modelling decision with no single correct answer. From 1 up to the number of rows. " +
                         "Default 3.")]
            int clusters = 3,
            [Description("How the k starting medoids are chosen before the first swap is attempted: PamBuild " +
                         "(default) greedily picks the rows that most reduce the total distance if added next, which " +
                         "is deterministic and usually converges to a better result than Uniform, which simply picks " +
                         "k rows at random.")]
            KMedoidsSeeding seeding = KMedoidsSeeding.PamBuild,
            [Description("Maximum number of swap iterations before the fit stops even if a better swap might still " +
                         "exist. Default 100.")]
            int maximumIterations = 100,
            [Description("Whether every variable is z-scored (rescaled to the training mean and standard deviation) " +
                         "before clustering, so that a variable measured on a larger numeric scale does not dominate " +
                         "the distance calculation purely because of its units. Default true.")]
            bool standardise = true,
            [Description("Share of the rows held back for a validation diagnostic split rather than used to fit the " +
                         "medoids, between 0 and 0.9. Default 0.2.")]
            double validationFraction = 0.2,
            [Description("Share of the rows held back purely to measure cluster quality on rows the medoids never " +
                         "saw, between 0 and 0.9. Together with the validation share it may not exceed 0.9. Default " +
                         "0.2.")]
            double testFraction = 0.2,
            [Description("Seed that makes the row split and the choice of starting medoids reproducible: the same " +
                         "seed and the same rows always fit the same medoids.")]
            int seed = 1,
            [Description("Optional opaque label kept with a newly created model, for finding it again.")]
            string? correlationKey = null,
            CancellationToken cancellationToken = default)
        {
            return service.SubmitTrainingAsync(new AdvancedAnalyticsKMedoidsClustererTrainingDto
            {
                AdvancedAnalyticsInstanceId = instanceId,
                ModelName = modelName,
                CorrelationKey = correlationKey,
                VariableNames = variableNames,
                ProcessingTypeIds = processingTypeIds,
                Inputs = inputs,
                Clusters = clusters,
                Seeding = seeding,
                MaximumIterations = maximumIterations,
                Standardise = standardise,
                ValidationFraction = validationFraction,
                TestFraction = testFraction,
                Seed = seed
            }, cancellationToken);
        }
    }
}