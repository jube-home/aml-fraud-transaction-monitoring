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

namespace Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms
{
    internal sealed class SampleProfile
    {
        public SampleProfile(double[] values)
        {
            var sorted = (double[])values.Clone();
            Array.Sort(sorted);
            Values = values;
            Count = values.Length;
            Minimum = sorted[0];
            Maximum = sorted[^1];
            var sum = 0.0;

            foreach (var v in sorted)
            {
                sum += v;
            }

            Mean = sum / Count;
            var ss = 0.0;

            foreach (var v in sorted)
            {
                ss += (v - Mean) * (v - Mean);
            }

            StandardDeviation = Count > 1 ? Math.Sqrt(ss / (Count - 1)) : 0.0;
            Median = Count % 2 == 1 ? sorted[Count / 2] : (sorted[Count / 2 - 1] + sorted[Count / 2]) / 2.0;
            var mad = 0.0;

            foreach (var v in sorted)
            {
                mad += Math.Abs(v - Median);
            }

            MeanAbsoluteDeviationFromMedian = mad / Count;
            var skew = 0.0;

            if (StandardDeviation > 0)
            {
                foreach (var v in sorted)
                {
                    var z = (v - Mean) / StandardDeviation;
                    skew += z * z * z;
                }

                skew /= Count;
            }

            Skewness = skew;
            InterquartileRange = Quantile(sorted, 0.75) - Quantile(sorted, 0.25);
        }

        public double InterquartileRange { get; }
        public double[] Values { get; }
        public int Count { get; }
        public double Minimum { get; }
        public double Maximum { get; }
        public double Mean { get; }
        public double StandardDeviation { get; }
        public double Median { get; }
        public double MeanAbsoluteDeviationFromMedian { get; }
        public double Skewness { get; }

        private static double Quantile(double[] sorted, double p)
        {
            var h = (sorted.Length - 1) * p;
            var lower = (int)Math.Floor(h);
            var upper = Math.Min(lower + 1, sorted.Length - 1);

            return sorted[lower] + (h - lower) * (sorted[upper] - sorted[lower]);
        }
    }
}