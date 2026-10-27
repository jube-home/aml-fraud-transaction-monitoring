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
    [AdvancedAnalyticsToolGroup("BalancedKMeansClustererTrainingRuns")]
    public class BalancedKMeansClustererTrainingRunTools(
        AdvancedAnalyticsBalancedKMeansClustererTrainingService service)
    {
        [AdvancedAnalyticsTool("SubmitBalancedKMeansClustererTraining")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Queues a balanced k-means clustering training run for a background worker and returns its run id " +
            "straight away, before anything is trained. Balanced k-means starts from an ordinary k-means fit and then " +
            "repeatedly re-assigns every row to a cluster by solving a single combinatorial optimal-assignment " +
            "problem across the whole training set at once (rather than letting each row simply pick its nearest " +
            "centre), constrained so that every cluster ends up with as close to an equal share of the rows as the " +
            "row count allows; each cluster's centre is then recomputed from the rows that assignment gave it, and " +
            "the two steps repeat until the centres stop moving by more than the tolerance. This trades away some of " +
            "plain k-means' freedom to fit whatever sizes the data naturally suggests, in exchange for clusters whose " +
            "sizes are guaranteed to be almost identical, which is useful when downstream a cluster stands for " +
            "something like a caseload, a cohort or a bucket that should not be lopsided. Because the optimal-" +
            "assignment step considers every row against every other row at once, its cost grows with the cube of " +
            "the row count, so the number of training rows accepted for this family is capped well below plain " +
            "k-means. The exactly-balanced assignment is only meaningful for the rows used to train it; scoring a " +
            "new row afterwards, as recall does, falls back to whichever fitted centre that row sits closest to, the " +
            "same way plain k-means works, since there is no whole training set to re-solve the assignment against. " +
            "Clustering is unsupervised: give the rows inline, one array of numbers per case with NaN for a missing " +
            "value; every column is a feature that contributes to the distance calculation, there is no label " +
            "column. To follow the run, call GetAdvancedAnalyticsTrainingRunStatus with the returned run id until it " +
            "reports Succeeded, Failed or Cancelled, then call GetAdvancedAnalyticsTrainingRunResult. Training never " +
            "makes the resulting version live.")]
        public Task<AdvancedAnalyticsTrainingSubmissionDto> SubmitBalancedKMeansClustererTrainingAsync(
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
                "Inline rows, one array of numbers per case with NaN for a missing value. Every column is a feature that the distance between rows is computed over; there is no label. Kept small compared with plain k-means because fitting solves an assignment problem whose cost grows with the cube of the row count.")]
            double[][] inputs,
            [Description(
                "Number of clusters (k) to partition the rows into: the balanced fit divides the rows into this many groups of as close to equal size as the row count allows. From 1 up to the number of rows. Default 3.")]
            int clusters = 3,
            [Description(
                "How the k starting centres are chosen for the ordinary k-means pass that seeds the balanced fit: KMeansPlusPlus (default) spreads the starting centres apart by favouring rows far from the centres already picked; Uniform simply picks k rows at random.")]
            KMeansSeeding seeding = KMeansSeeding.KMeansPlusPlus,
            [Description(
                "Upper bound accepted by the fitting routine before it treats the centres as settled; the balanced fit stops purely once the centres stop moving by more than the tolerance below, so a very small value here has no practical effect. Default 100.")]
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
            return service.SubmitTrainingAsync(new AdvancedAnalyticsBalancedKMeansClustererTrainingDto
            {
                AskJoobyAnalyticsInstanceId = instanceId,
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