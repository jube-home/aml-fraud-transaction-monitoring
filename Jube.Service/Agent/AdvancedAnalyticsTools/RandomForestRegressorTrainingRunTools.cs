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
using Jube.Service.Agent.AdvancedAnalyticsTools.Models;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools
{
    [AdvancedAnalyticsToolGroup("RandomForestRegressorTrainingRuns")]
    public class RandomForestRegressorTrainingRunTools(AdvancedAnalyticsRandomForestRegressorTrainingService service)
    {
        [AdvancedAnalyticsTool("SubmitRandomForestRegressorTraining")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Queues a random forest regression training run for a background worker and returns its run id straight " +
            "away, before anything is trained. A random forest regressor is an ensemble of many regression trees, " +
            "each grown on its own bootstrap sample (a random draw of rows, with replacement, from the training " +
            "split) and each considering only a random subset of the variables at every split, so no two trees see " +
            "quite the same data or the same candidate splits; every split is chosen to reduce the variance of the " +
            "continuous outcome within the two resulting groups as much as possible, and a leaf predicts the mean " +
            "outcome of the training rows that reached it. Averaging every tree's prediction gives a continuous " +
            "estimate that is far steadier and harder to overfit than any single tree's. Unlike the classifier " +
            "families, a regression forest's output is never calibrated: it is already on the scale of the outcome " +
            "being predicted. Give the predictor rows and the continuous outcome for each row inline. To follow the " +
            "run, call GetAdvancedAnalyticsTrainingRunStatus with the returned run id until it reports Succeeded, " +
            "Failed or Cancelled, then call GetAdvancedAnalyticsTrainingRunResult. Training never makes the " +
            "resulting version live.")]
        public async Task<TrainingRunSubmissionView> SubmitRandomForestRegressorTrainingAsync(
            [Description("Numeric identifier of the Advanced Analytics instance the run and its model belong to, " +
                         "identifying which tenant-scoped collection of models the new version is filed under.")]
            int instanceId,
            [Description("Name of the model the trained version is filed under. An existing model of that name in " +
                         "the same instance receives the result as a new, separately versioned entry; when no model " +
                         "of that name exists yet, one is created to hold it.")]
            string modelName,
            [Description("The name of each predictor column, unique within the request and given in the same left-" +
                         "to-right order as every row of the inputs array, so that position j in a row always refers " +
                         "to the variable named at position j here.")]
            string[] variableNames,
            [Description("Where each predictor is sourced from within the live scoring pipeline, one integer per " +
                         "entry in variableNames and in the same order: 1 payload, 2 dictionary, 3 TTL counter, 4 " +
                         "sanction, 5 abstraction, 6 abstraction calculation, 7 or 99 other. This provenance is " +
                         "recorded with the trained version so a later promoted model knows where to fetch each " +
                         "variable's live value from.")]
            int[] processingTypeIds,
            [Description("The predictor rows to fit the forest on, one array of numbers per case in the same order " +
                         "as the continuous outcomes, each array the same length as variableNames; use NaN for a " +
                         "value that was not observed for that case, which the forest will impute with that " +
                         "variable's training-split median.")]
            double[][] inputs,
            [Description("The continuous outcome to be predicted for each row, in the same order as the rows of " +
                         "inputs; every value must be a finite number, since a missing or infinite outcome cannot be " +
                         "used to grow a tree.")]
            double[] outputs,
            [Description("Number of independently bootstrapped regression trees to bag into the forest. A larger " +
                         "forest averages away more of any one tree's idiosyncratic overfitting at a roughly linear " +
                         "cost in fitting time, with the improvement flattening out well before this default. Default " +
                         "100.")]
            int treeCount = 100,
            [Description("Maximum number of splits from the root to a leaf allowed in any one tree. A shallower " +
                         "limit forces coarser, more stable predictions from each tree; a forest tolerates a deeper " +
                         "limit than a single regression tree would be trusted with, because the trees' individual " +
                         "overfitting tends to cancel out when their predictions are averaged. Default 6.")]
            int maxDepth = 6,
            [Description("Minimum number of training rows that must fall on each side of a candidate split for that " +
                         "split to be accepted; raising it forces coarser splits and shorter effective trees, which " +
                         "trades away some sharpness of fit for steadier leaf means. Default 5.")]
            int minimumLeafSize = 5,
            [Description("Share of the variables randomly offered as candidate splitters at every node, which is " +
                         "what decorrelates the trees from one another so the forest's average is steadier than any " +
                         "single tree's prediction. The default of 0 resolves to one third of the variable count, the " +
                         "customary rule of thumb for a regression forest (a classifier forest instead conventionally " +
                         "uses the square root of the variable count, since the two tasks trade off bias and variance " +
                         "differently); a value strictly between 0 and 1 sets the offered share directly, and 1 offers " +
                         "every variable at every split, which turns the forest into a plain bagging ensemble of " +
                         "otherwise unrestricted trees. Default 0.")]
            double featureFraction = 0d,
            [Description("Share of the training rows drawn, with replacement, into each tree's bootstrap sample. The " +
                         "default of 1 draws as many rows as the training split holds, so some rows are repeated and " +
                         "others are left out of that tree's sample entirely; a smaller share makes each tree see " +
                         "less data and diversifies the trees further, while also leaving more rows out of bag with " +
                         "which to estimate the forest's held-out quality at no extra cost. Default 1.")]
            double sampleFraction = 1d,
            [Description("Share of the rows set aside as a validation split, reported alongside the training and " +
                         "test metrics in the evidence but never used to grow a tree, between 0 and 0.9. Default 0.2.")]
            double validationFraction = 0.2,
            [Description("Share of the rows set aside purely to measure quality on cases the forest never saw " +
                         "during fitting, between 0 and 0.9. Together with the validation share it may not exceed " +
                         "0.9. Default 0.2.")]
            double testFraction = 0.2,
            [Description("Seed governing the row split, every tree's bootstrap draw and every node's random subset " +
                         "of candidate variables, so the same seed on the same rows always reproduces the same " +
                         "forest bit for bit; reuse a seed to compare forests fairly across a change in some other " +
                         "setting.")]
            int seed = 1,
            [Description("Optional opaque label kept alongside a newly created model, for the caller to find that " +
                         "model again later by some identifier of its own rather than by name.")]
            string? correlationKey = null,
            CancellationToken cancellationToken = default)
        {
            var submission = await service.SubmitTrainingAsync(new AdvancedAnalyticsRandomForestRegressorTrainingDto
            {
                AdvancedAnalyticsInstanceId = instanceId,
                ModelName = modelName,
                CorrelationKey = correlationKey,
                VariableNames = variableNames,
                ProcessingTypeIds = processingTypeIds,
                Inputs = inputs,
                Outputs = outputs,
                TreeCount = treeCount,
                MaxDepth = maxDepth,
                MinimumLeafSize = minimumLeafSize,
                FeatureFraction = featureFraction,
                SampleFraction = sampleFraction,
                ValidationFraction = validationFraction,
                TestFraction = testFraction,
                Seed = seed
            }, cancellationToken).ConfigureAwait(false);

            return new TrainingRunSubmissionView
            {
                RunId = submission.RunId,
                Status = submission.Status.ToString(),
                HowToPoll = submission.HowToPoll
            };
        }
    }
}