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
    [AdvancedAnalyticsToolGroup("OneClassSupportVectorAnomalyTrainingRuns")]
    public class OneClassSupportVectorAnomalyTrainingRunTools(
        AdvancedAnalyticsOneClassSupportVectorAnomalyTrainingService service)
    {
        [AdvancedAnalyticsTool("SubmitOneClassSupportVectorAnomalyTraining")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Queues a one-class support vector anomaly detection training run for a background worker and returns " +
            "its run id straight away, before anything is trained. Where a two-class support vector machine looks " +
            "for the boundary that best separates two labelled groups, a one-class support vector machine is shown " +
            "only a single, unlabelled set of rows and instead looks for a boundary that wraps as snugly as possible " +
            "around the bulk of them in a reshaped, kernel-induced space, treating whatever ends up outside that " +
            "boundary as unusual; the labelled two-class case simply does not apply here, since there is exactly one " +
            "population to describe rather than two to tell apart. The Nu hyperparameter states, before fitting " +
            "begins, roughly what share of the training rows the solver is allowed to leave outside the boundary " +
            "as margin errors, which doubles as a lower bound on how many rows end up defining that boundary (the " +
            "support vectors); a small Nu draws a generous boundary that only reacts to flagrant departures from the " +
            "bulk of the rows, a large Nu draws a tight boundary that already treats a meaningful share of ordinary " +
            "rows as unusual. Fitting fixes on a Gaussian kernel throughout, since a one-class boundary is " +
            "conventionally drawn with a similarity that fades smoothly and symmetrically in every direction as two " +
            "rows move apart, and offering a choice of kernel shape here would mostly invite tuning without a " +
            "principled way to choose between them. Because the notion of similarity between two rows depends " +
            "directly on their raw numeric scale, every predictor is z-scored to the training mean and standard " +
            "deviation before the kernel ever sees it. The rows that end up defining the boundary keep a nonzero " +
            "weight and are stored, together with that weight and the kernel's gamma, inside the model version; " +
            "every other training row is discarded once fitting finishes, since it contributed nothing to where the " +
            "boundary ended up, and because the fitted boundary lives in the reshaped space rather than the space of " +
            "the original predictors, there is no honest way to say how much any one predictor pushed a particular " +
            "score up or down, so recalls from this family carry no such breakdown. A label per row is entirely " +
            "optional and, unlike the boundary itself, is never used to influence where that boundary is drawn: when " +
            "a known-anomalous or known-normal flag is given for every row, and both kinds of row happen to fall in " +
            "the validation split, it is used only, after the fact, to Platt-calibrate the raw score into a " +
            "probability and to report a held-out measure of quality; without usable labels the raw score is " +
            "reported as is. Give the predictor rows inline, one array of numbers per case with NaN for a missing " +
            "value. To follow the run, call GetAdvancedAnalyticsTrainingRunStatus with the returned run id until it " +
            "reports Succeeded, Failed or Cancelled, then call GetAdvancedAnalyticsTrainingRunResult. Training never " +
            "makes the resulting version live.")]
        public Task<TrainingRunSubmissionView> SubmitOneClassSupportVectorAnomalyTrainingAsync(
            [Description("Numeric identifier of the Advanced Analytics instance the run and its model belong to.")]
            int instanceId,
            [Description(
                "Name of the model the trained version is filed under. An existing model of that name receives a new version; otherwise a model is created.")]
            string modelName,
            [Description("The name of each predictor column, unique and in column order.")]
            string[] variableNames,
            [Description(
                "Where each predictor comes from in the live pipeline, one number per variable name in the same order: 1 payload, 2 dictionary, 3 TTL counter, 4 sanction, 5 abstraction, 6 abstraction calculation, 7 or 99 other.")]
            int[] processingTypeIds,
            [Description(
                "Predictor rows, one array of numbers per case with NaN for a missing value. Every predictor is z-scored to the training mean and standard deviation before the kernel is applied. The boundary is fitted on these rows alone, with no notion of a labelled outcome for any of them; every one of these rows that ends up defining the boundary, after imputation and standardisation, is stored inside the trained model version, since scoring this kind of model means weighting the kernel similarity of the row being scored against every stored boundary-defining row.")]
            double[][] inputs,
            [Description(
                "Optional outcome for each row, in the same order as the rows: 1 for a row known in advance to be anomalous, 0 for a row known in advance to be normal. This plays no part in fitting the boundary itself, which is always unsupervised; when both values are present across the rows and both end up represented in the held-back validation share, they are used solely, after the boundary is already fixed, to Platt-calibrate the raw score into a probability and to report a held-out measure of separation. Leave every row out, or omit this altogether, to train with neither of those two secondary uses.")]
            int[]? labels = null,
            [Description(
                "The expected fraction of training rows the fitted boundary is allowed to treat as margin errors, stated before fitting begins; this also lower-bounds the fraction of training rows that end up kept as the boundary-defining support vectors once fitting finishes. A value close to 0 draws a wide, forgiving boundary that wraps almost every training row and only flags the most flagrant departures from it; a value close to 1 draws a boundary so tight that a large share of the ordinary training rows themselves end up excluded from it. There is no universally correct value; it should be chosen with reference to a rough prior belief about what share of the population the training rows are expected to represent unusual behaviour, or by comparing a few candidate values, such as 0.01, 0.05, 0.1 and 0.2, against held-out evidence when labels happen to be available. Must be strictly between 0 and 1. Default 0.1.")]
            double nu = 0.1,
            [Description(
                "The gamma of the Gaussian kernel used throughout this family: how far the influence of a single training row reaches when the kernel measures similarity between two standardised rows. A larger gamma treats rows as dissimilar unless they sit very close together, letting the boundary bend sharply around individual rows at greater risk of chasing noise in the training rows rather than genuine structure; a smaller gamma treats rows further apart as still meaningfully similar, giving a smoother boundary that can miss real pockets of unusual behaviour if taken too far. Zero (the default) is resolved automatically at training time to one over the number of predictor columns, a longstanding rule of thumb that scales sensibly as more predictors are added. Must not be negative. Default 0 (automatic).")]
            double gamma = 0d,
            [Description(
                "Share of the rows held back to fit the Platt probability calibration, used only when labels are given, between 0 and 0.9. Default 0.2.")]
            double validationFraction = 0.2,
            [Description(
                "Share of the rows held back purely to measure quality on rows the solver never fitted the boundary against, between 0 and 0.9. Together with the validation share it may not exceed 0.9. Default 0.2.")]
            double testFraction = 0.2,
            [Description(
                "Seed that makes the row split reproducible: the same seed and the same rows always produce the same training, validation and test split.")]
            int seed = 1,
            [Description(
                "Whether to fit a Platt calibration of the raw one-class score into a probability on the validation rows, when usable labels are given. The raw score a fitted boundary produces has no natural probability scale of its own by construction, so leaving this on, and supplying labels, is the only way the recalled value carries a bounded, probability-like meaning rather than an arbitrary signed number. Default true.")]
            bool calibrate = true,
            [Description("Optional opaque label kept with a newly created model, for finding it again.")]
            string? correlationKey = null,
            CancellationToken cancellationToken = default)
        {
            return SubmitAsync(instanceId, modelName, variableNames, processingTypeIds, inputs, labels, nu, gamma,
                validationFraction, testFraction, seed, calibrate, correlationKey, cancellationToken);
        }

        private async Task<TrainingRunSubmissionView> SubmitAsync(int instanceId, string modelName,
            string[] variableNames, int[] processingTypeIds, double[][] inputs, int[]? labels, double nu, double gamma,
            double validationFraction, double testFraction, int seed, bool calibrate, string? correlationKey,
            CancellationToken cancellationToken)
        {
            var submission = await service.SubmitTrainingAsync(
                new AdvancedAnalyticsOneClassSupportVectorAnomalyTrainingDto
                {
                    AdvancedAnalyticsInstanceId = instanceId,
                    ModelName = modelName,
                    CorrelationKey = correlationKey,
                    VariableNames = variableNames,
                    ProcessingTypeIds = processingTypeIds,
                    Inputs = inputs,
                    Labels = labels,
                    Nu = nu,
                    Gamma = gamma,
                    ValidationFraction = validationFraction,
                    TestFraction = testFraction,
                    Seed = seed,
                    Calibrate = calibrate
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