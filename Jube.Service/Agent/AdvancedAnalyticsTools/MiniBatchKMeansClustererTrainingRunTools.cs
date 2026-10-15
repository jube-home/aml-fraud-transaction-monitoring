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
    [AdvancedAnalyticsToolGroup("MiniBatchKMeansClustererTrainingRuns")]
    public class MiniBatchKMeansClustererTrainingRunTools(
        AdvancedAnalyticsMiniBatchKMeansClustererTrainingService service)
    {
        [AdvancedAnalyticsTool("SubmitMiniBatchKMeansClustererTraining")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Queues a mini-batch k-means clustering training run for a background worker and returns its run id " +
            "straight away, before anything is trained. Mini-batch k-means is a faster approximation of ordinary " +
            "k-means for large row counts: instead of recomputing every centroid from the whole dataset on every " +
            "iteration, it repeatedly draws a small random batch of rows, assigns each of them to its nearest " +
            "current centre, and nudges only those centres a little towards the batch, with the size of each nudge " +
            "shrinking as a centre accumulates more assigned rows over time so that early, noisier batches do not " +
            "keep disturbing a centre once it has settled. This repeats for a capped number of batches or until the " +
            "centres stop moving by more than the given tolerance. Before the first batch, several candidate " +
            "starting centre sets are drawn at random and each is scored on its own held-out sample of rows, keeping " +
            "whichever candidate set has the lowest distortion, which guards against an unlucky start. Clustering is " +
            "unsupervised: give the rows inline, one array of numbers per case with NaN for a missing value; every " +
            "column is a feature that contributes to the distance calculation and there is no label column. Recall " +
            "returns the distance from a row to every cluster centre, with the nearest distance always moved to the " +
            "front of the array, exactly as for ordinary k-means. To follow the run, call " +
            "GetAdvancedAnalyticsTrainingRunStatus with the returned run id until it reports Succeeded, Failed or " +
            "Cancelled, then call GetAdvancedAnalyticsTrainingRunResult. Training never makes the resulting version " +
            "live.")]
        public Task<AdvancedAnalyticsTrainingSubmissionDto> SubmitMiniBatchKMeansClustererTrainingAsync(
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
                         "with that column's training median, both while the centres are being fitted and later " +
                         "whenever the trained model is recalled. There is no label column: every column is a " +
                         "feature that contributes to the distance a row is clustered by.")]
            double[][] inputs,
            [Description("Number of clusters (k) to partition the rows into, from 1 up to the number of rows. " +
                         "Choosing k is a modelling decision with no single correct answer; more clusters always fit " +
                         "the training rows at least as tightly but can overfit to noise. Default 3.")]
            int clusters = 3,
            [Description("Number of rows drawn into each random mini-batch used to update the centres on every " +
                         "iteration, from 1 up to the number of training rows. A smaller batch makes every iteration " +
                         "cheaper but the centres wander more before settling; a larger batch behaves closer to " +
                         "ordinary k-means at a higher cost per iteration. Default 100.")]
            int batchSize = 100,
            [Description("Number of rows used, per candidate starting centre set, to seed and then score that set " +
                         "before mini-batch training begins; when given it must be at least the cluster count. Leave " +
                         "unset to default to three times the cluster count, automatically capped at the number of " +
                         "training rows available.")]
            int? initializationBatchSize = null,
            [Description("Number of independent random candidate starting centre sets tried before training begins, " +
                         "keeping whichever set has the lowest distortion on its own held-out sample. Raising this " +
                         "lowers the chance of an unlucky start at the cost of more setup work. Default 1.")]
            int numberOfInitializations = 1,
            [Description("How the initial candidate centres are chosen before each candidate set is scored: " +
                         "KMeansPlusPlus (default) spreads them apart by favouring rows far from the centres already " +
                         "picked, which tends to settle faster and more reliably than Uniform, which simply picks k " +
                         "rows at random and can start two centres inside the same natural group.")]
            KMeansSeeding seeding = KMeansSeeding.KMeansPlusPlus,
            [Description("Maximum number of mini-batch iterations before the fit stops even if centres are still " +
                         "moving. Default 100.")]
            int maximumIterations = 100,
            [Description("How little the centres may move, relative to their previous position, between one " +
                         "iteration and the next before the fit is treated as converged; smaller values demand a " +
                         "more exact fit at the cost of more iterations. Default 0.00001.")]
            double tolerance = 1e-5,
            [Description("Whether every variable is z-scored (rescaled to the training mean and standard deviation) " +
                         "before clustering, so that a variable measured on a larger numeric scale does not dominate " +
                         "the distance calculation purely because of its units. Default true.")]
            bool standardise = true,
            [Description("Share of the rows held back for a validation diagnostic split rather than used to fit the " +
                         "centres, between 0 and 0.9. Default 0.2.")]
            double validationFraction = 0.2,
            [Description("Share of the rows held back purely to measure cluster quality on rows the centres never " +
                         "saw, between 0 and 0.9. Together with the validation share it may not exceed 0.9. " +
                         "Default 0.2.")]
            double testFraction = 0.2,
            [Description("Seed that makes the row split, which rows land in each random mini-batch, and the choice " +
                         "of starting centres all reproducible: the same seed and the same rows always fit the same " +
                         "centres.")]
            int seed = 1,
            [Description("Optional opaque label kept with a newly created model, for finding it again.")]
            string? correlationKey = null,
            CancellationToken cancellationToken = default)
        {
            return service.SubmitTrainingAsync(new AdvancedAnalyticsMiniBatchKMeansClustererTrainingDto
            {
                AdvancedAnalyticsInstanceId = instanceId,
                ModelName = modelName,
                CorrelationKey = correlationKey,
                VariableNames = variableNames,
                ProcessingTypeIds = processingTypeIds,
                Inputs = inputs,
                Clusters = clusters,
                BatchSize = batchSize,
                InitializationBatchSize = initializationBatchSize,
                NumberOfInitializations = numberOfInitializations,
                Seeding = seeding,
                MaximumIterations = maximumIterations,
                Tolerance = tolerance,
                Standardise = standardise,
                ValidationFraction = validationFraction,
                TestFraction = testFraction,
                Seed = seed
            }, cancellationToken);
        }
    }
}