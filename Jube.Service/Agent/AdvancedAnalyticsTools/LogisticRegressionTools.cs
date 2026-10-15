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
using Jube.Analytics;
using Jube.Engine.AdvancedAnalytics.Regression;
using Jube.Engine.AdvancedAnalytics.Regression.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools
{
    [AdvancedAnalyticsToolGroup("LogisticRegression")]
    public class LogisticRegressionTools
    {
        [AdvancedAnalyticsTool("TrainLogisticRegression")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Fits a logistic regression that estimates the probability of the positive class (label 1) from numeric predictors. " +
            "The rows are split by seed into train, validation and test sets; scaling and missing value handling are learned from train only, the probability calibration from validation only, and every reported quality metric from the untouched test set. " +
            "It returns the held-out metrics, a coefficient table with significance tests and odds ratios, diagnostics warnings and a model snapshot that RecallLogisticRegression can score with. " +
            "Read the warnings and the test metrics before deciding whether the model is good enough to use. " +
            "This is the small synchronous path, meant for datasets of up to 2,000 rows, the limit of the immediate training run; larger datasets, or rows held in the Advanced Analytics cache, must be queued with SubmitLogisticRegressionTraining instead.")]
        public LogisticRegressionResult TrainLogisticRegression(
            [Description(
                "Predictor rows: one array of numbers per case, all the same length and in the same order as the variable names. NaN marks a missing value.")]
            double[][] inputs,
            [Description(
                "The outcome for each row, in the same order as the rows: 1 for the event of interest and 0 otherwise.")]
            int[] labels,
            [Description("The name of each predictor column, unique and in column order.")]
            string[] variableNames,
            [Description(
                "Share of the rows held back to fit the probability calibration, between 0 and 0.9. Default 0.2.")]
            double validationFraction = 0.2,
            [Description("Share of the rows held back purely to measure quality, between 0 and 0.9. Default 0.2.")]
            double testFraction = 0.2,
            [Description(
                "Seed that makes the row split reproducible. Reuse a seed to compare models on identical rows.")]
            int seed = 1,
            [Description(
                "Ridge penalty strength on the standardised coefficients; 0 fits by plain maximum likelihood. Use a positive value when predictors are collinear or the classes separate.")]
            double ridgeLambda = 0,
            [Description(
                "None fits the data as it is. Balanced gives the rarer class more weight during fitting, and probabilities are corrected back to the true event rate afterwards.")]
            LogisticClassWeighting classWeighting = LogisticClassWeighting.None,
            [Description(
                "ImputeMedianWithIndicators replaces a missing value with the training median and adds a missing flag column where training had gaps. Reject fails on any missing value.")]
            LogisticMissingValuePolicy missingValuePolicy = LogisticMissingValuePolicy.ImputeMedianWithIndicators,
            [Description(
                "Whether predictors are z-scored using training statistics before fitting. Default true; coefficients are always reported on the original scale.")]
            bool standardise = true,
            [Description(
                "Whether to fit a Platt calibration on the validation set so the probabilities match observed frequencies. Default true.")]
            bool calibrate = true,
            [Description(
                "The real long run event rate to report probabilities at. Leave empty to use the rate observed in the train and validation rows.")]
            double? baseRateOverride = null,
            [Description(
                "Cost of missing a true positive (false negative). Give together with costFalsePositive to receive the minimum expected cost threshold.")]
            double? costFalseNegative = null,
            [Description("Cost of wrongly flagging a negative (false positive). Give together with costFalseNegative.")]
            double? costFalsePositive = null,
            [Description(
                "Probability at or above which a row counts as positive in the confusion counts, precision and recall. Empty uses the minimum cost threshold when costs are given, otherwise 0.5.")]
            double? decisionThreshold = null,
            [Description("Maximum number of fitting iterations. Default 50.")]
            int maximumIterations = 50,
            [Description("Convergence tolerance on the largest coefficient update. Default 1e-8.")]
            double tolerance = 1e-8,
            CancellationToken cancellationToken = default)
        {
            if (costFalseNegative.HasValue != costFalsePositive.HasValue)
            {
                throw new ArgumentException("Give both costFalseNegative and costFalsePositive, or neither.");
            }

            var options = new LogisticRegressionOptions
            {
                ValidationFraction = validationFraction,
                TestFraction = testFraction,
                Seed = seed,
                RidgeLambda = ridgeLambda,
                ClassWeighting = classWeighting,
                MissingValuePolicy = missingValuePolicy,
                Standardise = standardise,
                Calibrate = calibrate,
                BaseRateOverride = baseRateOverride,
                Costs = costFalseNegative.HasValue
                    ? new CostMatrix(costFalseNegative.Value, costFalsePositive!.Value)
                    : null,
                DecisionThreshold = decisionThreshold,
                MaximumIterations = maximumIterations,
                Tolerance = tolerance
            };

            return LogisticRegressionTrainer.Train(inputs, labels, variableNames, options, cancellationToken);
        }

        [AdvancedAnalyticsTool("RecallLogisticRegression")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Recall)]
        [Description(
            "Scores new rows with a logistic regression snapshot produced by TrainLogisticRegression and returns one probability of the positive class per row. " +
            "The stored preprocessing, calibration and base rate correction are applied, so missing values are filled and flagged exactly as in training.")]
        public double[] RecallLogisticRegression(
            [Description("The model snapshot JSON text returned by TrainLogisticRegression.")]
            string snapshotJson,
            [Description(
                "Rows to score: one array of numbers per case, in the variable order the model was trained with. NaN marks a missing value.")]
            double[][] inputs)
        {
            return LogisticRegressionRecall.FromSnapshot(snapshotJson).PredictProbabilities(inputs);
        }

        [AdvancedAnalyticsTool("AssessPredictorSignificance")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [Description(
            "Tests each predictor on its own against a 0/1 outcome, with no model saved: for every variable it returns a one variable logistic regression's estimate, Wald z, p-value, odds ratio per unit with a 95 percent interval, and the ROC AUC of the raw variable. " +
            "A Benjamini-Hochberg adjusted p-value is added to control false discoveries across the many variables. Use it to screen candidate predictors before training.")]
        public PredictorSignificance[] AssessPredictorSignificance(
            [Description(
                "Predictor rows: one array of numbers per case, all the same length. NaN marks a missing value, which drops that row for that variable only.")]
            double[][] inputs,
            [Description("The outcome for each row: 1 for the event of interest and 0 otherwise.")]
            int[] labels,
            [Description("The name of each predictor column, unique and in column order.")]
            string[] variableNames,
            CancellationToken cancellationToken = default)
        {
            return PredictorSignificanceAnalysis.Assess(inputs, labels, variableNames, cancellationToken);
        }
    }
}