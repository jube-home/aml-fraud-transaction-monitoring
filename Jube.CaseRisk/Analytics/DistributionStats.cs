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

using MessagePack;

namespace Jube.CaseRisk.Analytics
{
    [MessagePackObject]
    public sealed record DistributionStats(
        [property: Key(0)] int Count,
        [property: Key(1)] double Mean,
        [property: Key(2)] double Min,
        [property: Key(3)] double P50,
        [property: Key(4)] double P75,
        [property: Key(5)] double P90,
        [property: Key(6)] double P95,
        [property: Key(7)] double Max)
    {
        public static readonly DistributionStats Empty = new(0, 0, 0, 0, 0, 0, 0, 0);

        public static DistributionStats Of(IEnumerable<double> values)
        {
            var sorted = values.Where(v => !double.IsNaN(v)).OrderBy(v => v).ToArray();

            if (sorted.Length == 0)
            {
                return Empty;
            }

            return new DistributionStats(sorted.Length, sorted.Average(), sorted[0], Percentile(sorted, 0.5),
                Percentile(sorted, 0.75), Percentile(sorted, 0.9), Percentile(sorted, 0.95), sorted[^1]);
        }

        public static double Percentile(double[] sorted, double fraction)
        {
            if (sorted.Length == 1)
            {
                return sorted[0];
            }

            var position = fraction * (sorted.Length - 1);
            var lower = (int)Math.Floor(position);
            var upper = (int)Math.Ceiling(position);

            return sorted[lower] + (sorted[upper] - sorted[lower]) * (position - lower);
        }
    }
}
