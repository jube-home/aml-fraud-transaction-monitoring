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
    public static class RobustStatistics
    {
        public const double MaxZ = 5;
        public const int MinimumForJudgement = 5;
        public const int ConfidentRows = 20;
        private const double NormalConsistency = 1.4826;
        private const double InterquartileToSigma = 1.349;

        public static double[] Sorted(IEnumerable<double> values)
        {
            return values.Where(double.IsFinite).OrderBy(value => value).ToArray();
        }

        public static double Quantile(double[] sorted, double fraction)
        {
            if (sorted.Length == 0)
            {
                return double.NaN;
            }

            if (sorted.Length == 1)
            {
                return sorted[0];
            }

            var position = fraction * (sorted.Length - 1);
            var lower = (int)Math.Floor(position);
            var upper = (int)Math.Ceiling(position);

            return sorted[lower] + (sorted[upper] - sorted[lower]) * (position - lower);
        }

        public static RobustFit Fit(IEnumerable<double> values)
        {
            var sorted = Sorted(values);

            if (sorted.Length == 0)
            {
                return new RobustFit(0, double.NaN, 0, double.NaN, double.NaN);
            }

            var median = Quantile(sorted, 0.5);
            var q1 = Quantile(sorted, 0.25);
            var q3 = Quantile(sorted, 0.75);
            var deviations = sorted.Select(value => Math.Abs(value - median)).OrderBy(value => value).ToArray();
            var scale = NormalConsistency * Quantile(deviations, 0.5);

            if (scale == 0)
            {
                scale = (q3 - q1) / InterquartileToSigma;
            }

            if (scale == 0 && sorted.Length > 1)
            {
                var mean = sorted.Average();
                scale = Math.Sqrt(sorted.Sum(value => (value - mean) * (value - mean)) / (sorted.Length - 1));
            }

            return new RobustFit(sorted.Length, median, scale, q1, q3);
        }

        public static double? ZScore(RobustFit fit, double value)
        {
            if (fit.Count < MinimumForJudgement || !double.IsFinite(value))
            {
                return null;
            }

            if (fit.Scale == 0)
            {
                return value == fit.Median ? 0 : Math.Sign(value - fit.Median) * MaxZ;
            }

            return Math.Clamp((value - fit.Median) / fit.Scale, -MaxZ, MaxZ);
        }
    }
}
