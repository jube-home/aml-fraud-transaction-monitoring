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

using Jube.Analytics.Anomaly.Models;

namespace Jube.Analytics.Anomaly
{
    public static class AnomalyLabeller
    {
        public static ThresholdApplied Contamination(double[] scores, double fraction)
        {
            if (!(fraction > 0 && fraction <= 0.5))
            {
                throw new ArgumentOutOfRangeException(nameof(fraction), "Contamination must be in (0, 0.5].");
            }

            var n = scores.Length;
            var k = Math.Clamp((int)Math.Round(fraction * n), 1, n);
            var descending = scores.OrderByDescending(v => v).ToArray();
            var threshold = k >= n ? double.NegativeInfinity : descending[k];
            var flagged = scores.Count(v => v > threshold);

            return new ThresholdApplied("Contamination", threshold, fraction, flagged, n);
        }

        public static double[] Label(double[] scores, double threshold)
        {
            var labels = new double[scores.Length];

            for (var i = 0; i < scores.Length; i++)
            {
                labels[i] = scores[i] > threshold ? 1d : 0d;
            }

            return labels;
        }
    }
}