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
using Jube.Engine.AdvancedAnalytics.Regression.Models;
using Jube.Service.Agent.AdvancedAnalyticsTools.Interfaces;
using Jube.Service.Agent.AdvancedAnalyticsTools.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools
{
    [AdvancedAnalyticsToolGroup("LogisticRegressionTrainingRuns")]
    public class LogisticRegressionTrainingRunTools(IAdvancedAnalyticsTrainingRuns runs)
    {
        [AdvancedAnalyticsTool("SubmitLogisticRegressionTraining")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Queues a logistic regression training run for a background worker and returns its run id straight away, before anything is trained; use this for any dataset larger than the quick TrainLogisticRegression path allows, or whenever the rows already sit in the Advanced Analytics cache. " +
            "Give the rows inline (up to a configured cap) or a cache pointer to a stored table whose last column is the label. " +
            "To follow the run, call GetTrainingRunStatus with the returned run id until it reports Succeeded, Failed or Cancelled, then call GetTrainingRunResult. Training never makes the resulting version live.")]
        public Task<TrainingRunSubmissionView> SubmitLogisticRegressionTrainingAsync(
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
                "Inline predictor rows, one array of numbers per case with NaN for a missing value. Leave empty when a cache pointer is given.")]
            double[][]? inputs = null,
            [Description(
                "Inline outcomes, one per row: 1 for the event of interest and 0 otherwise. Leave empty when a cache pointer is given.")]
            int[]? labels = null,
            [Description(
                "Pointer to a table in the Advanced Analytics cache holding the predictor columns and a label column. Leave empty when the rows are inline. The run fails if the pointer has expired by the time a worker starts it.")]
            Guid? cachePointer = null,
            [Description(
                "Zero based index of the label column in the cached table. Default is the last column; only used with a cache pointer.")]
            int labelColumn = -1,
            [Description(
                "Seed that makes the row split reproducible. Reuse a seed to compare models on identical rows.")]
            int seed = 1,
            [Description(
                "Share of the rows held back to fit the probability calibration, between 0 and 0.9. Default 0.2.")]
            double validationFraction = 0.2,
            [Description("Share of the rows held back purely to measure quality, between 0 and 0.9. Default 0.2.")]
            double testFraction = 0.2,
            [Description(
                "Ridge penalty strength on the standardised coefficients; 0 fits by plain maximum likelihood.")]
            double ridgeLambda = 0,
            [Description("None fits the data as it is. Balanced gives the rarer class more weight during fitting.")]
            LogisticClassWeighting classWeighting = LogisticClassWeighting.None,
            [Description(
                "ImputeMedianWithIndicators fills a missing value with the training median and adds a missing flag; Reject fails on any missing value.")]
            LogisticMissingValuePolicy missingValuePolicy = LogisticMissingValuePolicy.ImputeMedianWithIndicators,
            [Description("Whether predictors are z-scored with training statistics before fitting. Default true.")]
            bool standardise = true,
            [Description("Whether to fit a Platt calibration on the validation rows. Default true.")]
            bool calibrate = true,
            [Description(
                "The real long run event rate to report probabilities at. Leave empty to use the observed rate.")]
            double? baseRateOverride = null,
            [Description("Cost of missing a true positive. Give together with costFalsePositive, or leave both empty.")]
            double? costFalseNegative = null,
            [Description(
                "Cost of wrongly flagging a negative. Give together with costFalseNegative, or leave both empty.")]
            double? costFalsePositive = null,
            [Description(
                "Probability at or above which a row counts as positive in the confusion counts. Empty uses the minimum cost threshold when costs are given, otherwise 0.5.")]
            double? decisionThreshold = null,
            [Description("Maximum number of fitting iterations. Default 50.")]
            int maximumIterations = 50,
            [Description("Convergence tolerance on the largest coefficient update. Default 1e-8.")]
            double tolerance = 1e-8,
            [Description("Optional opaque label kept with a newly created model, for finding it again.")]
            string? correlationKey = null,
            CancellationToken cancellationToken = default)
        {
            return runs.SubmitAsync(new TrainingRunSubmitRequest
            {
                InstanceId = instanceId,
                ModelName = modelName,
                CorrelationKey = correlationKey,
                VariableNames = variableNames,
                ProcessingTypeIds = processingTypeIds,
                Inputs = inputs,
                Labels = labels,
                CachePointer = cachePointer,
                LabelColumn = labelColumn,
                Seed = seed,
                ValidationFraction = validationFraction,
                TestFraction = testFraction,
                RidgeLambda = ridgeLambda,
                ClassWeighting = classWeighting,
                MissingValuePolicy = missingValuePolicy,
                Standardise = standardise,
                Calibrate = calibrate,
                BaseRateOverride = baseRateOverride,
                CostFalseNegative = costFalseNegative,
                CostFalsePositive = costFalsePositive,
                DecisionThreshold = decisionThreshold,
                MaximumIterations = maximumIterations,
                Tolerance = tolerance
            }, cancellationToken);
        }

        [AdvancedAnalyticsTool("GetTrainingRunStatus")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [Description(
            "Reads where a queued training run has got to: Pending while it waits for a worker, Running while it trains, then Succeeded, Failed or Cancelled, which are final. " +
            "It also returns the elapsed seconds and, for a failed or cancelled run, the reason; run time grows in step with the amount of data. Poll it every few seconds until Finished is true.")]
        public Task<TrainingRunStatusView> GetTrainingRunStatusAsync(
            [Description("The run id returned when the training was submitted.")]
            int runId,
            CancellationToken cancellationToken = default)
        {
            return runs.GetStatusAsync(runId, cancellationToken);
        }

        [AdvancedAnalyticsTool("CancelTrainingRun")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Cancels a training run. A run still waiting for a worker is cancelled at once; a running one is asked to stop and ends as Cancelled at its next step boundary or inside the fit, and no model is stored for it. A finished run is final and is returned unchanged.")]
        public Task<TrainingRunStatusView> CancelTrainingRunAsync(
            [Description("The run id returned when the training was submitted.")]
            int runId,
            CancellationToken cancellationToken = default)
        {
            return runs.CancelAsync(runId, cancellationToken);
        }

        [AdvancedAnalyticsTool("GetTrainingRunResult")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [Description(
            "Returns the outcome of a training run and, once it Succeeded, the version it produced and its complete evidence: held-out metrics, coefficient significance, calibration, fit diagnostics and warnings. " +
            "Read the warnings and the test metrics from this evidence before deciding whether the version deserves promotion; a run that is not finished, or that failed or was cancelled, carries no evidence.")]
        public Task<TrainingRunResultView> GetTrainingRunResultAsync(
            [Description("The run id returned when the training was submitted.")]
            int runId,
            CancellationToken cancellationToken = default)
        {
            return runs.GetResultAsync(runId, cancellationToken);
        }
    }
}