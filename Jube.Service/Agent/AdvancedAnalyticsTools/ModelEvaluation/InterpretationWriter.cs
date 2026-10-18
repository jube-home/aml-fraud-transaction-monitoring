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

using System.Globalization;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal static class InterpretationWriter
    {
        private static string F(double? value)
        {
            return value.HasValue ? value.Value.ToString("0.###", CultureInfo.InvariantCulture) : "n/a";
        }

        public static string Classifier(ClassifierEvaluationResult r, LearnerConfig config)
        {
            var parts = new List<string>();
            var test = r.Test;

            if (test == null)
            {
                return "No held-out metrics were produced.";
            }

            var where = r.Folds.Length > 1
                ? $"pooled over {r.Folds.Length} held-out folds"
                : "on the untouched test split";

            var interval = test.RocAucLower95.HasValue && test.RocAucUpper95.HasValue
                ? $" (95% interval {F(test.RocAucLower95)} to {F(test.RocAucUpper95)})"
                : string.Empty;

            parts.Add(
                $"{r.Family} {where}: ROC AUC {F(test.RocAuc)}{interval}, average precision {F(test.AveragePrecision)} against a base rate of {F(r.PositiveRate)}, log loss {F(test.LogLoss)}, Brier score {F(test.BrierScore)}.");

            parts.Add(
                $"At threshold {F(test.Threshold)} it catches {F(test.Recall)} of the positives with precision {F(test.Precision)} (F1 {F(test.F1)}, {test.TruePositives} true positives, {test.FalsePositives} false positives, {test.FalseNegatives} missed).");

            if (r.Train?.RocAuc is { } trainAuc && test.RocAuc is { } testAuc)
            {
                parts.Add(trainAuc - testAuc > ModelEvaluationLimits.OverfitGap
                    ? $"Training ROC AUC {F(trainAuc)} is much higher than test {F(testAuc)}, a sign of overfitting."
                    : $"Training ROC AUC {F(trainAuc)} is close to test {F(testAuc)}, so there is no strong sign of overfitting.");
            }

            if (r.FoldSummary.Length > 0 && r.FoldSummary[0].StandardDeviation.HasValue)
            {
                parts.Add(
                    $"Across folds the ROC AUC averages {F(r.FoldSummary[0].Mean)} with a standard deviation of {F(r.FoldSummary[0].StandardDeviation)}.");
            }

            if (r.Importance.Length > 0)
            {
                parts.Add("Most influential variables by permutation importance: " +
                          string.Join(", ", r.Importance.Take(3).Select(i => $"{i.Variable} ({F(i.Importance)})")) +
                          ".");
            }

            if (r.Warnings.Length > 0)
            {
                parts.Add($"Read the {r.Warnings.Length} warning(s) before trusting this result.");
            }

            return string.Join(" ", parts);
        }
    }
}