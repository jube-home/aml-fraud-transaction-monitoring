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
    internal static class Combiner
    {
        public static double Combine(ScoreCombination combination, MemberCalibration[] calibration, double[] raw)
        {
            switch (combination)
            {
                case ScoreCombination.Maximum:
                {
                    var max = double.NegativeInfinity;

                    for (var i = 0; i < raw.Length; i++)
                    {
                        max = Math.Max(max, calibration[i].Standardise(raw[i]));
                    }

                    return max;
                }
                case ScoreCombination.RankAverage:
                {
                    var sum = 0d;

                    for (var i = 0; i < raw.Length; i++)
                    {
                        sum += calibration[i].Rank(raw[i]);
                    }

                    return sum / raw.Length;
                }
                default:
                {
                    var sum = 0d;

                    for (var i = 0; i < raw.Length; i++)
                    {
                        sum += calibration[i].Standardise(raw[i]);
                    }

                    return sum / raw.Length;
                }
            }
        }
    }
}