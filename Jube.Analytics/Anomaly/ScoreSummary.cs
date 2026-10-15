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

// ReSharper disable NotAccessedPositionalProperty.Global

namespace Jube.Analytics.Anomaly
{
    public sealed record ScoreSummary(
        double Minimum,
        double Maximum,
        double Mean,
        double StandardDeviation,
        double P50,
        double P90,
        double P95,
        double P99)
    {
        public static ScoreSummary From(double[] scores)
        {
            var finite = scores.Where(Numerics.IsFinite).OrderBy(v => v).ToArray();

            if (finite.Length == 0)
            {
                return new ScoreSummary(0, 0, 0, 0, 0, 0, 0, 0);
            }

            var mean = finite.Average();
            var sd = Math.Sqrt(finite.Sum(v => (v - mean) * (v - mean)) / finite.Length);

            return new ScoreSummary(finite[0], finite[^1], mean, sd, Numerics.Quantile(finite, 0.5),
                Numerics.Quantile(finite, 0.9), Numerics.Quantile(finite, 0.95), Numerics.Quantile(finite, 0.99));
        }
    }
}