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

namespace Jube.Graph.Algorithms
{
    public sealed record Moments(int Count, double Sum, double Mean, double Min, double Max, double Median,
        double Variance, double Skewness, double Kurtosis)
    {
        public static readonly Moments Empty = new(0, 0d, 0d, 0d, 0d, 0d, 0d, 0d, 0d);

        public static Moments Of(IReadOnlyList<double> values)
        {
            ArgumentNullException.ThrowIfNull(values);

            var n = values.Count;

            if (n == 0)
            {
                return Empty;
            }

            var sorted = values.ToArray();
            Array.Sort(sorted);
            var sum = 0d;

            foreach (var v in sorted)
            {
                sum += v;
            }

            var mean = sum / n;
            var m2 = 0d;
            var m3 = 0d;
            var m4 = 0d;

            foreach (var v in sorted)
            {
                var d = v - mean;
                var d2 = d * d;
                m2 += d2;
                m3 += d2 * d;
                m4 += d2 * d2;
            }

            var variance = n < 2 ? 0d : m2 / (n - 1);
            var populationVariance = m2 / n;
            var skewness = populationVariance <= 0 ? 0d : m3 / n / Math.Pow(populationVariance, 1.5);
            var kurtosis = populationVariance <= 0 ? 0d : m4 / n / (populationVariance * populationVariance) - 3d;
            var median = n % 2 == 1 ? sorted[n / 2] : (sorted[n / 2 - 1] + sorted[n / 2]) / 2d;

            return new Moments(n, sum, mean, sorted[0], sorted[^1], median, variance, skewness, kurtosis);
        }
    }
}
