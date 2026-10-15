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
    [AdvancedAnalyticsToolGroup("GaussianMixtureClustererTrainingRuns")]
    public class GaussianMixtureClustererTrainingRunTools(
        AdvancedAnalyticsGaussianMixtureClustererTrainingService service)
    {
        [AdvancedAnalyticsTool("SubmitGaussianMixtureClustererTraining")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Queues a Gaussian mixture clustering training run for a background worker and returns its run id " +
            "straight away, before anything is trained. A Gaussian mixture models the rows as drawn from a small " +
            "number of overlapping bell-shaped (Gaussian) groups rather than from hard, non-overlapping regions: it " +
            "fits, for every component, a mean position, a covariance describing the size, orientation and spread of " +
            "its cloud of rows, and a mixture weight giving that component's overall share of rows, all by " +
            "iteratively re-estimating, for every row, how much each component is responsible for it (expectation) " +
            "and then re-fitting every component's mean, covariance and weight from those responsibility-weighted " +
            "rows (maximisation), repeating until the total likelihood stops improving by more than the tolerance or " +
            "the iteration cap is reached. This is a soft, probabilistic cousin of k-means clustering: rather than " +
            "assigning a row to exactly one nearest centre, it reports, for every component, the posterior " +
            "probability that the row belongs to it, so a row sitting between two overlapping components gets a " +
            "share of probability on both rather than being forced into one. Recall returns this same posterior " +
            "probability for every component, with the single most likely component moved to the front of the " +
            "array, so unlike this family's k-means sibling (whose recall reports distances, where a smaller number " +
            "at the front means a closer match) a LARGER number at the front here means a more confident match. This " +
            "is a distinct, hard/soft cluster-assignment tool: it is not the separate anomaly-scoring tool that also " +
            "fits a Gaussian mixture but reports a single density-based anomaly score for a row rather than " +
            "per-component membership probabilities. Clustering is unsupervised: give the rows inline, one array of " +
            "numbers per case with NaN for a missing value; every column is a feature, there is no label column. To " +
            "follow the run, call GetAdvancedAnalyticsTrainingRunStatus with the returned run id until it reports " +
            "Succeeded, Failed or Cancelled, then call GetAdvancedAnalyticsTrainingRunResult. Training never makes " +
            "the resulting version live.")]
        public Task<AdvancedAnalyticsTrainingSubmissionDto> SubmitGaussianMixtureClustererTrainingAsync(
            [Description("Numeric identifier of the Advanced Analytics instance that the run and its trained model " +
                         "belong to; every model name is only unique within one instance.")]
            int instanceId,
            [Description("Name of the model the trained version is filed under. An existing model of that name in " +
                         "the same instance receives this run's result as a new, additional version; if no model of " +
                         "that name exists yet, one is created.")]
            string modelName,
            [Description("The name of each variable, one per column, unique within the list and given in the same " +
                         "left-to-right order as the columns inside each row of inputs.")]
            string[] variableNames,
            [Description("Where each variable comes from in the live pipeline, one number per entry in variableNames " +
                         "and in the same order: 1 payload, 2 dictionary, 3 TTL counter, 4 sanction, 5 abstraction, " +
                         "6 abstraction calculation, 7 or 99 other. The live pipeline consults this when it later " +
                         "looks each value up to score a case with the trained model.")]
            int[] processingTypeIds,
            [Description("Inline rows, one array of numbers per case with NaN for a missing value; a missing value " +
                         "is filled with that column's training median both while fitting and later whenever the " +
                         "trained model is recalled. Every column is a feature that the fit is computed over; there " +
                         "is no label.")]
            double[][] inputs,
            [Description("Number of Gaussian components to fit, from 1 up to the number of rows. Choosing the count " +
                         "is a modelling decision with no single correct answer; more components always fit the " +
                         "training rows at least as well but can overfit to noise or leave a component with too few " +
                         "rows to fit reliably. Default 3.")]
            int components = 3,
            [Description("Whether every component's covariance is fitted as diagonal, treating every variable as " +
                         "independent within that component, rather than as a full matrix that also captures " +
                         "correlation between variables. A diagonal fit needs fewer rows per component relative to " +
                         "the number of variables and is cheaper to fit and recall, at the cost of being unable to " +
                         "represent a component whose cloud of rows runs diagonally across two or more variables at " +
                         "once. Default false, meaning a full covariance is fitted.")]
            bool diagonalCovariance = false,
            [Description("Small constant added to the diagonal of every fitted covariance matrix so it stays " +
                         "invertible even with few rows per component or near-collinear variables. Raise this if " +
                         "fitting fails on a small or highly correlated dataset; leave it at the default otherwise. " +
                         "Default 0.000001.")]
            double regularization = 1e-6,
            [Description("Maximum number of expectation-maximisation iterations before the fit stops even if the " +
                         "log-likelihood is still improving. Default 100.")]
            int maximumIterations = 100,
            [Description("How little the total log-likelihood may improve between one iteration and the next before " +
                         "the fit is treated as converged; smaller values demand a more exact fit at the cost of more " +
                         "iterations. Default 0.001.")]
            double tolerance = 1e-3,
            [Description("Whether every variable is z-scored (rescaled to the training mean and standard deviation) " +
                         "before fitting, so that a variable measured on a larger numeric scale does not dominate the " +
                         "fit purely because of its units. Default true.")]
            bool standardise = true,
            [Description("Share of the rows held back for a validation diagnostic split rather than used to fit the " +
                         "mixture, between 0 and 0.9. Default 0.2.")]
            double validationFraction = 0.2,
            [Description("Share of the rows held back purely to measure fit quality on rows the mixture never saw, " +
                         "between 0 and 0.9. Together with the validation share it may not exceed 0.9. Default 0.2.")]
            double testFraction = 0.2,
            [Description("Seed that makes the row split and the fit's random starting point reproducible: the same " +
                         "seed and the same rows always fit the same mixture.")]
            int seed = 1,
            [Description("Optional opaque label kept with a newly created model, for finding it again.")]
            string? correlationKey = null,
            CancellationToken cancellationToken = default)
        {
            return service.SubmitTrainingAsync(new AdvancedAnalyticsGaussianMixtureClustererTrainingDto
            {
                AdvancedAnalyticsInstanceId = instanceId,
                ModelName = modelName,
                CorrelationKey = correlationKey,
                VariableNames = variableNames,
                ProcessingTypeIds = processingTypeIds,
                Inputs = inputs,
                Components = components,
                DiagonalCovariance = diagonalCovariance,
                Regularization = regularization,
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