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
    internal static class ModelMath
    {
        public static double Sigmoid(double z)
        {
            if (z >= 0)
            {
                var e = Math.Exp(-z);

                return 1d / (1d + e);
            }

            var f = Math.Exp(z);

            return f / (1d + f);
        }

        public static double Softplus(double z)
        {
            return z > 30 ? z : z < -30 ? Math.Exp(z) : Math.Log(1d + Math.Exp(z));
        }

        public static double? Finite(double value)
        {
            return double.IsFinite(value) ? value : null;
        }

        public static double Mean(IReadOnlyList<double> values)
        {
            return values.Count == 0 ? double.NaN : values.Average();
        }

        public static double StandardDeviation(IReadOnlyList<double> values)
        {
            if (values.Count < 2)
            {
                return double.NaN;
            }

            var mean = values.Average();

            return Math.Sqrt(values.Sum(v => (v - mean) * (v - mean)) / (values.Count - 1));
        }

        public static double Quantile(double[] sorted, double q)
        {
            if (sorted.Length == 0)
            {
                return double.NaN;
            }

            var position = q * (sorted.Length - 1);
            var lower = (int)Math.Floor(position);
            var upper = (int)Math.Ceiling(position);

            return sorted[lower] + (sorted[upper] - sorted[lower]) * (position - lower);
        }

        public static double Median(double[] values)
        {
            var sorted = values.OrderBy(v => v).ToArray();

            return Quantile(sorted, 0.5);
        }

        public static EvaluationMetricSummary Summarise(string metric, IReadOnlyList<double> values)
        {
            var finite = values.Where(double.IsFinite).ToList();

            if (finite.Count == 0)
            {
                return new EvaluationMetricSummary(metric, null, null, null, null);
            }

            return new EvaluationMetricSummary(metric, finite.Average(), Finite(StandardDeviation(finite)),
                finite.Min(),
                finite.Max());
        }
    }
}