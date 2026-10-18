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

using Jube.Analytics;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal static class RegressionMetricsBuilder
    {
        public static RegressionMetricsResult? Build(double[] actual, double[] predicted)
        {
            if (actual.Length == 0)
            {
                return null;
            }

            var residuals = actual.Select((a, i) => a - predicted[i]).ToArray();
            var mean = residuals.Average();

            var deviation = residuals.Length > 1
                ? Math.Sqrt(residuals.Sum(r => (r - mean) * (r - mean)) / (residuals.Length - 1))
                : double.NaN;

            var skew = double.NaN;

            if (residuals.Length > 2 && double.IsFinite(deviation) && deviation > 0)
            {
                var population = Math.Sqrt(residuals.Sum(r => (r - mean) * (r - mean)) / residuals.Length);
                skew = residuals.Sum(r => Math.Pow((r - mean) / population, 3)) / residuals.Length;
            }

            var absolute = residuals.Select(Math.Abs).OrderBy(v => v).ToArray();
            var correlation = Correlation(actual, predicted);

            return new RegressionMetricsResult(
                actual.Length,
                ModelMath.Finite(actual.Length > 1 ? RegressionMetrics.RSquared(actual, predicted) : double.NaN),
                ModelMath.Finite(RegressionMetrics.MeanAbsoluteError(actual, predicted)),
                ModelMath.Finite(RegressionMetrics.RootMeanSquaredError(actual, predicted)),
                ModelMath.Finite(RegressionMetrics.MeanAbsolutePercentageError(actual, predicted)),
                ModelMath.Finite(RegressionMetrics.SymmetricMeanAbsolutePercentageError(actual, predicted)),
                ModelMath.Finite(ModelMath.Quantile(absolute, 0.5)),
                ModelMath.Finite(absolute[^1]),
                ModelMath.Finite(correlation),
                ModelMath.Finite(mean),
                ModelMath.Finite(deviation),
                ModelMath.Finite(skew));
        }

        public static double Score(RegressionMetricsResult? metrics, RegressionSelectionMetric metric)
        {
            if (metrics == null)
            {
                return double.NaN;
            }

            return metric switch
            {
                RegressionSelectionMetric.RSquared => metrics.RSquared ?? double.NaN,
                RegressionSelectionMetric.MeanAbsoluteError => metrics.MeanAbsoluteError ?? double.NaN,
                RegressionSelectionMetric.RootMeanSquaredError => metrics.RootMeanSquaredError ?? double.NaN,
                RegressionSelectionMetric.MeanAbsolutePercentageError => metrics.MeanAbsolutePercentageError ??
                                                                         double.NaN,
                _ => double.NaN
            };
        }

        public static bool HigherIsBetter(RegressionSelectionMetric metric)
        {
            return metric == RegressionSelectionMetric.RSquared;
        }

        private static double Correlation(double[] a, double[] b)
        {
            if (a.Length < 2)
            {
                return double.NaN;
            }

            var ma = a.Average();
            var mb = b.Average();
            double sab = 0, saa = 0, sbb = 0;

            for (var i = 0; i < a.Length; i++)
            {
                sab += (a[i] - ma) * (b[i] - mb);
                saa += (a[i] - ma) * (a[i] - ma);
                sbb += (b[i] - mb) * (b[i] - mb);
            }

            return saa > 0 && sbb > 0 ? sab / Math.Sqrt(saa * sbb) : double.NaN;
        }
    }
}