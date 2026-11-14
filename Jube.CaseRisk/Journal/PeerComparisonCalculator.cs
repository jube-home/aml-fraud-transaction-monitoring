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
    public static class PeerComparisonCalculator
    {
        public const string Above = "above";
        public const string Below = "below";
        public const string Typical = "typical";

        public static PeerComparison? Compare(PeerDistribution peer, IReadOnlyList<double> keyValues,
            double threshold = NotableCalculator.DefaultThreshold)
        {
            var sorted = RobustStatistics.Sorted(keyValues);

            if (sorted.Length == 0)
            {
                return null;
            }

            var median = RobustStatistics.Quantile(sorted, 0.5);
            var fit = new RobustFit(peer.KeyCount, peer.Median, peer.Scale, peer.Q1, peer.Q3);
            var z = RobustStatistics.ZScore(fit, median);
            var direction = z is null || Math.Abs(z.Value) < threshold ? Typical : z.Value > 0 ? Above : Below;

            return new PeerComparison(peer.Field, median, sorted.Length, peer.Median, peer.Scale, peer.Q1, peer.Q3,
                peer.KeyCount, z, Percentile(peer, median), direction, sorted.Length < RobustStatistics.ConfidentRows);
        }

        public static double Percentile(PeerDistribution peer, double value)
        {
            var total = peer.Bins.Sum(bin => bin.Count);

            if (total == 0 || value <= peer.Minimum)
            {
                return 0;
            }

            if (value >= peer.Maximum)
            {
                return 1;
            }

            double below = 0;

            foreach (var bin in peer.Bins)
            {
                if (value >= bin.To)
                {
                    below += bin.Count;
                    continue;
                }

                var width = bin.To - bin.From;

                if (width > 0 && value > bin.From)
                {
                    below += bin.Count * (value - bin.From) / width;
                }

                break;
            }

            return below / total;
        }
    }
}
