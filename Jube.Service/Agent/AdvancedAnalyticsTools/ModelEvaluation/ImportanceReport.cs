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

using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal static class ImportanceReport
    {
        public static PermutationImportanceResult Build(string family, string metric, int seed, int repeats, int folds,
            List<double> baselines, string[] names, List<double>[] drops, List<EvaluationWarning> warnings,
            string metricLabel, string dropLabel)
        {
            var rows = baselines.Count == 0 ? [] : PermutationImportance.Rows(names, drops);

            var negligible = rows.Where(r => r.Importance <= Math.Max(0d, r.StandardDeviation ?? 0d))
                .Select(r => r.Variable)
                .ToArray();

            var baseline = baselines.Count == 0 ? (double?)null : baselines.Average();
            var distinct = new List<EvaluationWarning>();

            foreach (var warning in warnings)
            {
                if (!distinct.Any(w => w.Code == warning.Code))
                {
                    distinct.Add(warning);
                }
            }

            var text = baselines.Count == 0
                ? "The held-out rows do not allow importance to be measured (a single class or a constant target)."
                : $"Baseline held-out {metricLabel} is {baseline:0.####}. The most important variables, by how much the {dropLabel} when they are shuffled, are " +
                  string.Join(", ", rows.Take(3).Select(r => $"{r.Variable} ({r.Importance:0.####})")) + ". " +
                  (negligible.Length > 0
                      ? $"{negligible.Length} variable(s) have an importance no larger than their own shuffling noise and can probably be dropped: {string.Join(", ", negligible.Take(10))}."
                      : "Every variable has an importance above its shuffling noise.") +
                  " Correlated variables share importance, so a low value for one of a correlated pair does not prove it is useless.";

            return new PermutationImportanceResult
            {
                Family = family,
                Metric = metric,
                Seed = seed,
                Repeats = repeats,
                Folds = folds,
                BaselineScore = baseline,
                Importance = rows,
                NegligibleVariables = negligible,
                Warnings = distinct.ToArray(),
                Interpretation = text
            };
        }
    }
}