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

namespace Jube.Analytics
{
    public static class RegressionMetrics
    {
        public static double MeanAbsoluteError(double[] actual, double[] predicted)
        {
            var (a, p) = Numerics.CleanRegressionPairs(actual, predicted);

            return a.Select((v, i) => Math.Abs(v - p[i])).Average();
        }

        public static double RootMeanSquaredError(double[] actual, double[] predicted)
        {
            var (a, p) = Numerics.CleanRegressionPairs(actual, predicted);

            return Math.Sqrt(a.Select((v, i) => (v - p[i]) * (v - p[i])).Average());
        }

        public static double MeanAbsolutePercentageError(double[] actual, double[] predicted)
        {
            var (a, p) = Numerics.CleanRegressionPairs(actual, predicted);

            var terms = a.Select((v, i) => (v, i)).Where(t => t.v != 0).Select(t => Math.Abs((t.v - p[t.i]) / t.v))
                .ToArray();

            return terms.Length == 0 ? double.NaN : terms.Average();
        }

        public static double SymmetricMeanAbsolutePercentageError(double[] actual, double[] predicted)
        {
            var (a, p) = Numerics.CleanRegressionPairs(actual, predicted);

            return a.Select((v, i) =>
            {
                var denominator = Math.Abs(v) + Math.Abs(p[i]);

                return denominator == 0 ? 0d : 2d * Math.Abs(v - p[i]) / denominator;
            }).Average();
        }

        public static double MeanAbsoluteScaledError(double[] actual, double[] predicted, double[] training,
            int seasonality = 1)
        {
            ArgumentNullException.ThrowIfNull(training);

            if (seasonality < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(seasonality));
            }

            var (a, p) = Numerics.CleanRegressionPairs(actual, predicted);
            var differences = new List<double>();

            for (var i = seasonality; i < training.Length; i++)
            {
                if (!double.IsNaN(training[i]) && !double.IsNaN(training[i - seasonality]))
                {
                    differences.Add(Math.Abs(training[i] - training[i - seasonality]));
                }
            }

            if (differences.Count == 0)
            {
                throw new ArgumentException("The training series is too short for the seasonality.");
            }

            var scale = differences.Average();

            return scale == 0 ? double.NaN : a.Select((v, i) => Math.Abs(v - p[i])).Average() / scale;
        }

        public static double RSquared(double[] actual, double[] predicted)
        {
            var (a, p) = Numerics.CleanRegressionPairs(actual, predicted);
            var mean = a.Average();
            var residual = a.Select((v, i) => (v - p[i]) * (v - p[i])).Sum();
            var total = a.Select(v => (v - mean) * (v - mean)).Sum();

            return total == 0 ? double.NaN : 1d - residual / total;
        }
    }
}