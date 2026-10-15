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
    internal static class ImbalanceHandler
    {
        public static (double[][] X, int[] Y, double[]? W, string? Note) Apply(LearnerConfig config, double[][] x,
            int[] y,
            int seed)
        {
            var positives = y.Count(v => v == 1);
            var negatives = y.Length - positives;

            if (config.Imbalance == ClassImbalanceHandling.None || positives == 0 || negatives == 0)
            {
                return (x, y, null, null);
            }

            if (config.Imbalance == ClassImbalanceHandling.BalancedWeights && config.SupportsWeights)
            {
                var weights = y.Select(v => v == 1 ? y.Length / (2d * positives) : y.Length / (2d * negatives))
                    .ToArray();

                return (x, y, weights, null);
            }

            var random = new Random(SeededLibrary.Derive(seed, 991));
            var minorityLabel = positives <= negatives ? 1 : 0;
            var minority = Enumerable.Range(0, y.Length).Where(i => y[i] == minorityLabel).ToArray();
            var majority = Enumerable.Range(0, y.Length).Where(i => y[i] != minorityLabel).ToArray();
            var rows = new List<int>();

            if (config.Imbalance == ClassImbalanceHandling.Undersample)
            {
                var keep = Math.Max(minority.Length, 10);
                var chosen = majority.OrderBy(_ => random.Next()).Take(keep).ToArray();
                rows.AddRange(minority);
                rows.AddRange(chosen);
                var note = $"Undersampled the training majority class from {majority.Length} to {chosen.Length} rows.";

                return (rows.Select(i => x[i]).ToArray(), rows.Select(i => y[i]).ToArray(), null, note);
            }

            rows.AddRange(Enumerable.Range(0, y.Length));
            var extra = Math.Min(majority.Length - minority.Length, ModelEvaluationLimits.MaximumRows);

            for (var k = 0; k < extra; k++)
            {
                rows.Add(minority[random.Next(minority.Length)]);
            }

            var message = config.Imbalance == ClassImbalanceHandling.BalancedWeights
                ? $"{config.Family} cannot use row weights, so the training minority class was oversampled by {extra} duplicated rows instead."
                : $"Oversampled the training minority class by {extra} duplicated rows.";

            return (rows.Select(i => x[i]).ToArray(), rows.Select(i => y[i]).ToArray(), null, message);
        }
    }
}