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
    [AdvancedAnalyticsToolGroup("IsolationForestAnomalyTrainingRuns")]
    public class IsolationForestAnomalyTrainingRunTools(AdvancedAnalyticsIsolationForestAnomalyTrainingService service)
    {
        [AdvancedAnalyticsTool("SubmitIsolationForestAnomalyTraining")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Queues an isolation forest anomaly training run for a background worker and returns its run id straight " +
            "away, before anything is trained. An isolation forest is an ensemble of random binary trees, each grown " +
            "on its own random subsample of rows drawn without replacement: at every node, a tree picks one predictor " +
            "and one threshold entirely at random, uniformly between that predictor's smallest and largest value " +
            "among the rows still at that node, and sends rows below the threshold one way and the rest the other, " +
            "stopping a branch once it has isolated a single row or reached a depth close to the base-two logarithm " +
            "of the subsample size. No candidate split is scored or compared against another; the randomness itself " +
            "is what makes the method work, since a row sitting alone in a sparse, unusual corner of the predictor " +
            "space tends to end up isolated after only a handful of such arbitrary splits, while a row surrounded by " +
            "many similar rows typically survives many more splits before it is finally alone. Recall walks a new " +
            "row down every tree, averages the number of splits taken across the whole forest (adding a standard " +
            "closed-form correction for the handful of rows, if any, still bunched together at the leaf a row lands " +
            "in), and turns that average into a score between 0 and 1 that rises smoothly as the average path " +
            "shortens, so a value near 1 marks a row that isolated unusually quickly and a value near 0 marks one " +
            "that did not. Training is unsupervised by default: give only the predictor rows and every tree is grown " +
            "with no notion of which rows are anomalous. Labels are optional and never influence how a single tree " +
            "is grown; if given, and both a known-anomalous and a known-normal row fall in the validation split, " +
            "they are used only afterwards, to fit a logistic (Platt) curve that turns the raw score into a " +
            "probability, and to report a held-out ROC AUC as evidence of how well the score already separates the " +
            "two classes. To follow the run, call GetAdvancedAnalyticsTrainingRunStatus with the returned run id " +
            "until it reports Succeeded, Failed or Cancelled, then call GetAdvancedAnalyticsTrainingRunResult. " +
            "Training never makes the resulting version live.")]
        public Task<AdvancedAnalyticsTrainingSubmissionDto> SubmitIsolationForestAnomalyTrainingAsync(
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
                         "with that column's training median, both while every tree is being grown and later " +
                         "whenever the trained model is recalled. Every tree sees only its own random subsample of " +
                         "these rows, so the same row can be included in some trees and left out of others.")]
            double[][] inputs,
            [Description("Optional outcome for each row, in the same order as inputs: 1 for a row already known to " +
                         "be anomalous, 0 for a row already known to be normal. These never change how a tree is " +
                         "grown; leave every row out to train purely unsupervised. When given, and both a 0 and a 1 " +
                         "fall in the rows held back for validation, they are used afterwards to fit a probability " +
                         "calibration on top of the raw score and to report a held-out ROC AUC as evidence.")]
            int[]? labels = null,
            [Description("Number of random trees grown for the forest, from 1 to 2000. More trees make the averaged " +
                         "path length, and so the score, steadier from one training run to the next at the cost of " +
                         "more time to train and to recall, with diminishing returns well before the upper limit. " +
                         "Default 100.")]
            int treeCount = 100,
            [Description("Number of rows drawn without replacement to grow each individual tree, from 2 to 65536, " +
                         "automatically capped at however many training rows are actually available. A smaller " +
                         "subsample makes each tree shallower and cheaper to grow and tends to isolate an anomalous " +
                         "row in fewer splits, which is usually desirable for this method; a larger one lets a tree " +
                         "see more of the data at the cost of depth and speed. Default 256, the value used in the " +
                         "paper that introduced this method.")]
            int subsampleSize = 256,
            [Description("Seed that makes which rows fall into which split, which rows are subsampled for each " +
                         "tree, and which predictor and threshold each tree picks at each node, all reproducible. " +
                         "Reuse a seed to compare forests fitted on identical rows.")]
            int seed = 1,
            [Description("Share of the rows held back to fit the probability calibration when labels are given, " +
                         "between 0 and 0.9. Default 0.2.")]
            double validationFraction = 0.2,
            [Description("Share of the rows held back purely to measure quality, between 0 and 0.9. Default 0.2.")]
            double testFraction = 0.2,
            [Description("Whether to fit a Platt calibration from the raw score to a probability when usable labels " +
                         "are given. Default true.")]
            bool calibrate = true,
            [Description("Optional opaque label kept with a newly created model, for finding it again.")]
            string? correlationKey = null,
            CancellationToken cancellationToken = default)
        {
            return service.SubmitTrainingAsync(new AdvancedAnalyticsIsolationForestAnomalyTrainingDto
            {
                AdvancedAnalyticsInstanceId = instanceId,
                ModelName = modelName,
                CorrelationKey = correlationKey,
                VariableNames = variableNames,
                ProcessingTypeIds = processingTypeIds,
                Inputs = inputs,
                Labels = labels,
                TreeCount = treeCount,
                SubsampleSize = subsampleSize,
                Seed = seed,
                ValidationFraction = validationFraction,
                TestFraction = testFraction,
                Calibrate = calibrate
            }, cancellationToken);
        }
    }
}