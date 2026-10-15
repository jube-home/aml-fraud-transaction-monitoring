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
    [AdvancedAnalyticsToolGroup("MeanShiftClustererTrainingRuns")]
    public class MeanShiftClustererTrainingRunTools(AdvancedAnalyticsMeanShiftClustererTrainingService service)
    {
        [AdvancedAnalyticsTool("SubmitMeanShiftClustererTraining")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Queues a mean shift clustering training run for a background worker and returns its run id straight " +
            "away, before anything is trained. Mean shift treats the rows as samples drawn from an unknown density " +
            "and lets every row climb, in small steps, towards the nearest peak (mode) of that density: at each " +
            "step a row moves to the (kernel-weighted) mean of the other rows that fall within its bandwidth, and " +
            "this repeats until the row settles or the iteration cap is reached, after which rows that settled at " +
            "the same peak form one cluster. Unlike k-means, the number of clusters is never chosen up front: it " +
            "falls out of the fit as however many distinct peaks the rows actually climb to, so the same bandwidth " +
            "can discover a different mode count on different data, and a different bandwidth can merge or split " +
            "the modes found on the same data. Clustering is unsupervised: give the rows inline, one array of " +
            "numbers per case with NaN for a missing value; every column is a feature that contributes to distances, " +
            "there is no label column. To follow the run, call GetAdvancedAnalyticsTrainingRunStatus with the " +
            "returned run id until it reports Succeeded, Failed or Cancelled, then call " +
            "GetAdvancedAnalyticsTrainingRunResult, whose evidence reports the discovered mode count. Training never " +
            "makes the resulting version live.")]
        public Task<AdvancedAnalyticsTrainingSubmissionDto> SubmitMeanShiftClustererTrainingAsync(
            [Description("Numeric identifier of the Advanced Analytics instance the run and its model belong to.")]
            int instanceId,
            [Description("Name of the model the trained version is filed under. An existing model of that name " +
                         "receives a new version; otherwise a model is created.")]
            string modelName,
            [Description("The name of each variable, unique and in column order.")]
            string[] variableNames,
            [Description("Where each variable comes from in the live pipeline, one number per variable name in the " +
                         "same order: 1 payload, 2 dictionary, 3 TTL counter, 4 sanction, 5 abstraction, 6 abstraction " +
                         "calculation, 7 or 99 other.")]
            int[] processingTypeIds,
            [Description("Inline rows, one array of numbers per case with NaN for a missing value. Every column is " +
                         "a feature that the distance between rows is computed over; there is no label.")]
            double[][] inputs,
            [Description("The radius that every row looks around itself, in the units the rows are fitted in (after " +
                         "standardising when standardise is true), while it climbs towards the nearest peak in the " +
                         "density of the rows. Must be greater than zero. There is no single correct value and no " +
                         "cluster count to target directly: a small bandwidth finds many narrow, separate peaks and " +
                         "a large one merges everything into few, broad peaks, so it is usually worth trying a " +
                         "handful of values and comparing the discovered mode count reported in the evidence against " +
                         "what the data is expected to contain. Default 2.")]
            double bandwidth = 2.0,
            [Description("The density kernel that weighs nearby rows while a row climbs towards a mode: Uniform " +
                         "(default) weighs every row inside the bandwidth equally and is the fastest to fit; " +
                         "Gaussian and Epanechnikov weigh closer rows more heavily than distant ones, which can " +
                         "smooth the discovered modes at the cost of more computation.")]
            MeanShiftKernel kernel = MeanShiftKernel.Uniform,
            [Description("Cap on how many of the nearest rows are considered while a row climbs towards a mode. 0 " +
                         "(default) considers every row within the bandwidth with no cap, which is exact but can be " +
                         "slow with many rows; a positive value trades a little accuracy for speed on larger " +
                         "datasets.")]
            int maxNeighbours = 0,
            [Description("Maximum number of climbing steps a row may take before it is accepted where it stands even " +
                         "if it has not fully settled. Default 100.")]
            int maximumIterations = 100,
            [Description("How little a row may move in one climbing step, relative to the bandwidth, before that " +
                         "row is treated as having reached its mode. Default 0.001.")]
            double tolerance = 1e-3,
            [Description("Whether every variable is z-scored (rescaled to the training mean and standard deviation) " +
                         "before clustering, so the bandwidth is not dominated by whichever variable happens to be " +
                         "measured on the largest numeric scale. Default true.")]
            bool standardise = true,
            [Description("Share of the rows held back for a validation diagnostic split rather than used to find the " +
                         "modes, between 0 and 0.9. Default 0.2.")]
            double validationFraction = 0.2,
            [Description("Share of the rows held back purely to measure mode quality on rows the fit never saw, " +
                         "between 0 and 0.9. Together with the validation share it may not exceed 0.9. Default 0.2.")]
            double testFraction = 0.2,
            [Description("Seed that makes the row split reproducible. Mean shift itself has no random starting " +
                         "point, so the same rows and bandwidth always discover the same modes regardless of seed; " +
                         "only which rows fall into the train, validation and test splits depends on it.")]
            int seed = 1,
            [Description("Optional opaque label kept with a newly created model, for finding it again.")]
            string? correlationKey = null,
            CancellationToken cancellationToken = default)
        {
            return service.SubmitTrainingAsync(new AdvancedAnalyticsMeanShiftClustererTrainingDto
            {
                AdvancedAnalyticsInstanceId = instanceId,
                ModelName = modelName,
                CorrelationKey = correlationKey,
                VariableNames = variableNames,
                ProcessingTypeIds = processingTypeIds,
                Inputs = inputs,
                Bandwidth = bandwidth,
                Kernel = kernel,
                MaxNeighbours = maxNeighbours,
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