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


namespace Jube.CaseRisk.Journal
{
    public static class PeerDistributionCalculator
    {
        public const int MinimumKeys = 20;

        public static PeerDistribution? Build(string field, string? segment,
            IReadOnlyList<IReadOnlyList<double>> valuesByKey)
        {
            var medians = new List<double>();
            var rows = 0;

            foreach (var values in valuesByKey)
            {
                var sorted = RobustStatistics.Sorted(values);

                if (sorted.Length == 0)
                {
                    continue;
                }

                medians.Add(RobustStatistics.Quantile(sorted, 0.5));
                rows += sorted.Length;
            }

            if (medians.Count < MinimumKeys)
            {
                return null;
            }

            var ordered = RobustStatistics.Sorted(medians);
            var fit = RobustStatistics.Fit(ordered);

            return new PeerDistribution(field, segment, ordered.Length, rows, fit.Median, fit.Scale, fit.Q1, fit.Q3,
                ordered[0], ordered[^1], ordered.Average(), DistributionCalculator.Bins(ordered, fit.Q3 - fit.Q1));
        }
    }
}
