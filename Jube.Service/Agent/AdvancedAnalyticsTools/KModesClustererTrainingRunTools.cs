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
    [AdvancedAnalyticsToolGroup("KModesClustererTrainingRuns")]
    public class KModesClustererTrainingRunTools(AdvancedAnalyticsKModesClustererTrainingService service)
    {
        [AdvancedAnalyticsTool("SubmitKModesClustererTraining")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Queues a k-modes clustering training run for a background worker and returns its run id straight away, " +
            "before anything is trained. K-modes clusters rows described by categories rather than measurements: " +
            "instead of averaging numbers into a centre, as k-means does, it repeatedly assigns every row to " +
            "whichever cluster's mode (its most frequent category on each variable, among the rows currently " +
            "assigned to it) it disagrees with on the fewest variables, then recomputes each cluster's mode from its " +
            "newly assigned rows, and repeats until no row's assignment changes or the iteration cap is reached. " +
            "Every input column must already be a small integer-coded category, such as a country code, a " +
            "transaction-type code or a merchant-category code, and never an arbitrary continuous measurement such " +
            "as an amount or a duration; a value that is not already a whole number is rounded to the nearest one " +
            "and treated as its own category, which is usually a sign that a continuous variable was fed in by " +
            "mistake. Clustering is unsupervised: give the rows inline, one array of numbers per case with NaN for a " +
            "missing value (filled with that column's most frequent training category); every column is a category " +
            "that contributes to the mismatch count between rows, there is no label column. To follow the run, call " +
            "GetAdvancedAnalyticsTrainingRunStatus with the returned run id until it reports Succeeded, Failed or " +
            "Cancelled, then call GetAdvancedAnalyticsTrainingRunResult. Training never makes the resulting version " +
            "live.")]
        public Task<AdvancedAnalyticsTrainingSubmissionDto> SubmitKModesClustererTrainingAsync(
            [Description("Numeric identifier of the Advanced Analytics instance that the run and its trained model " +
                         "belong to; every model name is only unique within one instance.")]
            int instanceId,
            [Description("Name of the model the trained version is filed under. An existing model of that name in " +
                         "the same instance receives this run's result as a new, additional version; if no model of " +
                         "that name exists yet, one is created.")]
            string modelName,
            [Description("The name of each category column, one per column, unique within the list and given in the " +
                         "same left-to-right order as the columns inside each row of inputs.")]
            string[] variableNames,
            [Description("Where each category column comes from in the live pipeline, one number per entry in " +
                         "variableNames and in the same order: 1 payload, 2 dictionary, 3 TTL counter, 4 sanction, " +
                         "5 abstraction, 6 abstraction calculation, 7 or 99 other. The live pipeline consults this " +
                         "when it later looks each value up to score a case with the trained model.")]
            int[] processingTypeIds,
            [Description("Category rows: one array of numbers per case, every array the same length as " +
                         "variableNames. Every value should already be a small integer-coded category; a value that " +
                         "is not a whole number is rounded to the nearest one before it is compared to any other " +
                         "row. Use NaN for a value that is missing on that row; a missing value is filled with that " +
                         "column's most frequent training category, both while the clusters are fitted and later " +
                         "whenever the trained model is recalled.")]
            double[][] inputs,
            [Description("Number of clusters to partition the rows into: k-modes fits exactly this many modes and " +
                         "assigns every row to whichever one it disagrees with on the fewest categories. Choosing " +
                         "this is a modelling decision with no single correct answer. From 1 up to the number of " +
                         "rows. Default 3.")]
            int clusters = 3,
            [Description("How the starting modes are chosen before the first row is assigned: KModesPlusPlus " +
                         "(default) spreads the starting modes apart by favouring rows that disagree the most with " +
                         "the modes already picked, which converges faster and more reliably than Uniform, which " +
                         "simply picks that many rows at random and can start two modes on very similar rows.")]
            KModesSeeding seeding = KModesSeeding.KModesPlusPlus,
            [Description("Maximum number of assign-then-recompute-mode iterations before the fit stops even if " +
                         "assignments are still changing. Default 100.")]
            int maximumIterations = 100,
            [Description("How little the modes may change between one iteration and the next before the fit is " +
                         "treated as converged. Because every mode holds whole-number categories, in practice this " +
                         "only matters at the default: the fit stops as soon as no mode changes on any category at " +
                         "all. Default 0.00001.")]
            double tolerance = 1e-5,
            [Description("Share of the rows held back for a validation diagnostic split rather than used to fit the " +
                         "modes, between 0 and 0.9. Default 0.2.")]
            double validationFraction = 0.2,
            [Description("Share of the rows held back purely to measure cluster quality on rows the modes never " +
                         "saw, between 0 and 0.9. Together with the validation share it may not exceed 0.9. Default " +
                         "0.2.")]
            double testFraction = 0.2,
            [Description("Seed that makes the row split and the choice of starting modes reproducible: the same " +
                         "seed and the same rows always fit the same modes.")]
            int seed = 1,
            [Description("Optional opaque label kept with a newly created model, for finding it again.")]
            string? correlationKey = null,
            CancellationToken cancellationToken = default)
        {
            return service.SubmitTrainingAsync(new AdvancedAnalyticsKModesClustererTrainingDto
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
                ValidationFraction = validationFraction,
                TestFraction = testFraction,
                Seed = seed
            }, cancellationToken);
        }
    }
}