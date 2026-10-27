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
using Jube.Engine.AdvancedAnalytics.Training;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools
{
    [AdvancedAnalyticsToolGroup("NaiveBayesClassifierTrainingRuns")]
    public class NaiveBayesClassifierTrainingRunTools(AdvancedAnalyticsNaiveBayesClassifierTrainingService service)
    {
        [AdvancedAnalyticsTool("SubmitNaiveBayesClassifierTraining")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Queues a Gaussian naive Bayes classifier training run for a background worker and returns its run id " +
            "straight away, before anything is trained. Naive Bayes is a probabilistic classifier built on Bayes' " +
            "theorem under one simplifying assumption: given the class, every predictor is conditionally independent " +
            "of every other predictor. In practice this means the model never needs to learn how predictors interact " +
            "with each other, only how each predictor behaves on its own within each class, which makes it fast to " +
            "fit and robust with modest data, at the cost of being unable to represent a genuine interaction between " +
            "two predictors (for example, a combination of two ordinarily unremarkable values that is only suspicious " +
            "together). This implementation fits a Gaussian (Normal) distribution to each predictor within each " +
            "class, so it works directly on continuous, real-valued predictors without needing to bin them first, " +
            "unlike a categorical naive Bayes. A new row is scored by combining, in log-odds space, the prior log-odds " +
            "of the positive class with every predictor's own log-likelihood-ratio between the two fitted class " +
            "distributions; because the independence assumption makes this combination an exact sum, the contribution " +
            "of every single predictor to the final score can be reported precisely, with no approximation, which is " +
            "naive Bayes's real advantage for explaining a decision. Naive Bayes's raw posterior probability is known " +
            "to be poorly calibrated in practice, since the independence assumption is rarely exactly true and errors " +
            "in it compound multiplicatively across predictors, so this training run recalibrates the raw score into " +
            "a probability by fitting a logistic (Platt) curve on a held-out validation split whenever both classes " +
            "are present there; otherwise the native posterior is reported and a warning explains why. To follow the " +
            "run, call GetAdvancedAnalyticsTrainingRunStatus with the returned run id until it reports Succeeded, " +
            "Failed or Cancelled, then call GetAdvancedAnalyticsTrainingRunResult. Training never makes the resulting " +
            "version live.")]
        public Task<AdvancedAnalyticsTrainingSubmissionDto> SubmitNaiveBayesClassifierTrainingAsync(
            [Description("Numeric identifier of the AskJooby Analytics instance the run and its model belong to.")]
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
                "Inline predictor rows, one array of numbers per case with NaN for a missing value; a missing value is imputed with that predictor's training median before the class-conditional Gaussians are fitted. Leave empty when a cache pointer is given.")]
            double[][]? inputs = null,
            [Description(
                "Inline outcomes, one per row: 1 for the positive class being modelled, 0 for the negative class. Both classes must appear at least once across the training split, since a naive Bayes classifier needs at least one example of each class to estimate a prior and a class-conditional distribution for it. Leave empty when a cache pointer is given.")]
            int[]? labels = null,
            [Description(
                "Seed that makes the row split and, where ties occur, the calibration fit reproducible. Reuse a seed to compare models fitted on identical rows.")]
            int seed = 1,
            [Description(
                "Share of the rows held back to fit the probability calibration, between 0 and 0.9. Default 0.2.")]
            double validationFraction = 0.2,
            [Description(
                "Share of the rows held back purely to measure quality on rows the model never trained or calibrated on, between 0 and 0.9. Default 0.2.")]
            double testFraction = 0.2,
            [Description(
                "Fitting a Gaussian to a class-conditional sample that has fewer than two rows, or whose values are all identical, gives a variance of exactly zero, which would make that predictor's density infinite and dominate every prediction. This regularisation constant is the floor variance used in that situation, expressed as a share of the predictor's overall training variance so that it scales with the predictor's own units; a positive-variance fit is never touched by it. Default 0.000000001 (1e-9), which only matters in genuinely degenerate cases.")]
            double varianceRegularisation = 1e-9,
            [Description(
                "Whether to recalibrate the raw log-posterior-odds into a probability by fitting a logistic (Platt) curve on the validation split. Naive Bayes's raw posterior is usually overconfident because the independence assumption is only ever approximately true, so leaving this on (the default) is strongly recommended; when both classes are not present in the validation split, calibration is skipped regardless and the native posterior is reported, with a warning recorded either way. Default true.")]
            bool calibrate = true,
            [Description(
                "Probability at or above which the confusion counts recorded in the evidence (true/false positives and negatives, precision, recall) classify a row as the positive class. This is purely a diagnostic choice for reading the evidence; recall from the trained model always returns the probability itself, not a class decision. Default 0.5.")]
            double decisionThreshold = 0.5,
            [Description(
                "Pointer to a table in the Advanced Analytics cache holding the predictor columns and a label column. Leave empty when the rows are inline. The run fails if the pointer has expired by the time a worker starts it.")]
            Guid? cachePointer = null,
            [Description(
                "Zero based index of the label column in the cached table. Default is the last column; only used with a cache pointer.")]
            int labelColumn = -1,
            [Description("Optional opaque label kept with a newly created model, for finding it again.")]
            string? correlationKey = null,
            CancellationToken cancellationToken = default)
        {
            return service.SubmitTrainingAsync(new AdvancedAnalyticsNaiveBayesClassifierTrainingDto
            {
                AskJoobyAnalyticsInstanceId = instanceId,
                ModelName = modelName,
                CorrelationKey = correlationKey,
                VariableNames = variableNames,
                ProcessingTypeIds = processingTypeIds,
                Input = cachePointer.HasValue
                    ? AdvancedAnalyticsTrainingInput.CachePointer(cachePointer.Value, labelColumn)
                    : null,
                Inputs = inputs,
                Labels = labels,
                Seed = seed,
                ValidationFraction = validationFraction,
                TestFraction = testFraction,
                VarianceRegularisation = varianceRegularisation,
                Calibrate = calibrate,
                DecisionThreshold = decisionThreshold
            }, cancellationToken);
        }
    }
}