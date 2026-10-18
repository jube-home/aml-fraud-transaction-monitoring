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
    [AdvancedAnalyticsToolGroup("BinarySplitClustererTrainingRuns")]
    public class BinarySplitClustererTrainingRunTools(AdvancedAnalyticsBinarySplitClustererTrainingService service)
    {
        [AdvancedAnalyticsTool("SubmitBinarySplitClustererTraining")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Queues a binary split clustering training run for a background worker and returns its run id straight " +
            "away, before anything is trained. Binary Split is a divisive, hierarchical way of reaching k clusters: " +
            "it starts with every row in a single cluster, repeatedly finds whichever existing cluster currently has " +
            "the largest distortion (the total squared distance of its own rows to its own centre) and bisects only " +
            "that one cluster with an ordinary 2-means split, replacing it with the two halves; this repeats, one " +
            "bisection at a time, until exactly k clusters exist. Because it only ever bisects the single worst " +
            "cluster rather than re-fitting every centre from scratch each round, it tends to reach a stable, evenly " +
            "spread partition in fewer passes over the rows than starting k-means fresh with k centres, though it " +
            "can still settle into a locally rather than globally best split. Clustering is unsupervised: give the " +
            "rows inline, one array of numbers per case with NaN for a missing value; every column is a feature that " +
            "contributes to the distance calculation, there is no label column. Requesting exactly one cluster is " +
            "not accepted, since a single cluster is never bisected and so never receives a usable centre; ask for " +
            "at least two. To follow the run, call GetAdvancedAnalyticsTrainingRunStatus with the returned run id " +
            "until it reports Succeeded, Failed or Cancelled, then call GetAdvancedAnalyticsTrainingRunResult. " +
            "Training never makes the resulting version live.")]
        public Task<AdvancedAnalyticsTrainingSubmissionDto> SubmitBinarySplitClustererTrainingAsync(
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
            [Description("Where each predictor comes from in the live pipeline, one number per entry in " +
                         "variableNames and in the same order: 1 payload, 2 dictionary, 3 TTL counter, 4 sanction, " +
                         "5 abstraction, 6 abstraction calculation, 7 or 99 other. The live pipeline consults this " +
                         "when it later looks each value up to score a case with the trained model.")]
            int[] processingTypeIds,
            [Description("Predictor rows: one array of numbers per case, every array the same length as " +
                         "variableNames. Use NaN for a value that is missing on that row; a missing value is filled " +
                         "with that column's training median both while the model is fitted and later whenever the " +
                         "trained model is recalled. Every column is a feature the distance between rows is computed " +
                         "over; there is no label.")]
            double[][] inputs,
            [Description("Number of clusters (k) to reach: Binary Split performs exactly k minus one bisections, " +
                         "each time splitting whichever existing cluster currently has the largest distortion, until " +
                         "k clusters exist. Choosing k is a modelling decision with no single correct answer; more " +
                         "clusters always fit the training rows at least as tightly but can overfit to noise. From " +
                         "2 (one bisection is the least that is meaningful) up to the number of rows. Default 3.")]
            int clusters = 3,
            [Description("How each individual 2-means bisection chooses its two starting centres inside the cluster " +
                         "being split: KMeansPlusPlus (default) spreads the two starting centres apart by favouring " +
                         "rows far from the centre already picked, which converges faster and more reliably than " +
                         "Uniform, which simply picks two rows from that cluster at random.")]
            KMeansSeeding seeding = KMeansSeeding.KMeansPlusPlus,
            [Description("Maximum number of assign-then-recentre iterations allowed for each individual 2-means " +
                         "bisection before it stops even if its two centres are still moving. Default 100.")]
            int maximumIterations = 100,
            [Description("How little a bisection's two centres may move between one iteration and the next before " +
                         "that bisection is treated as converged; smaller values demand a more exact split at the " +
                         "cost of more iterations. Default 0.00001.")]
            double tolerance = 1e-5,
            [Description("Whether every variable is z-scored (rescaled to the training mean and standard deviation) " +
                         "before clustering, so that a variable measured on a larger numeric scale does not dominate " +
                         "the distance calculation purely because of its units. Default true.")]
            bool standardise = true,
            [Description("Share of the rows held back for a validation diagnostic split rather than used to fit the " +
                         "centres, between 0 and 0.9. Default 0.2.")]
            double validationFraction = 0.2,
            [Description("Share of the rows held back purely to measure cluster quality on rows the centres never " +
                         "saw, between 0 and 0.9. Together with the validation share it may not exceed 0.9. Default " +
                         "0.2.")]
            double testFraction = 0.2,
            [Description("Seed that makes the row split and every bisection's choice of starting centres " +
                         "reproducible: the same seed and the same rows always fit the same clusters and " +
                         "practically the same centres, though the fit runs across several threads internally so " +
                         "the last few decimal places of a centre can vary very slightly between runs.")]
            int seed = 1,
            [Description("Optional opaque label kept with a newly created model, for finding it again.")]
            string? correlationKey = null,
            CancellationToken cancellationToken = default)
        {
            return service.SubmitTrainingAsync(new AdvancedAnalyticsBinarySplitClustererTrainingDto
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
                Tolerance = tolerance,
                Standardise = standardise,
                ValidationFraction = validationFraction,
                TestFraction = testFraction,
                Seed = seed
            }, cancellationToken);
        }
    }
}