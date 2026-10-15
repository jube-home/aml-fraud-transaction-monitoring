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

namespace Jube.Analytics.Anomaly
{
    internal sealed class MemberCalibration
    {
        public MemberCalibration(double[] scores)
        {
            SortedScores = (double[])scores.Clone();
            Array.Sort(SortedScores);
            Mean = scores.Average();
            var variance = scores.Sum(s => (s - Mean) * (s - Mean)) / scores.Length;
            StandardDeviation = Math.Max(Math.Sqrt(variance), 1e-12);
        }

        public double Mean { get; }
        public double StandardDeviation { get; }
        public double[] SortedScores { get; }

        public double Standardise(double score)
        {
            return (score - Mean) / StandardDeviation;
        }

        public double Rank(double score)
        {
            var lower = Array.BinarySearch(SortedScores, score);

            if (lower < 0)
            {
                return ~lower / (double)SortedScores.Length;
            }

            var first = lower;

            while (first > 0 && Math.Abs(SortedScores[first - 1] - score) < 0.0001)
            {
                first--;
            }

            var last = lower;

            while (last + 1 < SortedScores.Length && Math.Abs(SortedScores[last + 1] - score) < 0.0001)
            {
                last++;
            }

            return (first + last + 1) / 2d / SortedScores.Length;
        }
    }
}