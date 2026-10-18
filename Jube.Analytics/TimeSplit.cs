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

using Jube.Analytics.Models;

namespace Jube.Analytics
{
    public static class TimeSplit
    {
        public static SplitFold[] ForwardChaining(double[] times, int folds, int gap = 0)
        {
            ArgumentNullException.ThrowIfNull(times);

            if (folds < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(folds));
            }

            if (gap < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gap));
            }

            if (times.Any(double.IsNaN))
            {
                throw new ArgumentException("Times must not contain NaN.");
            }

            var testSize = times.Length / (folds + 1);

            if (testSize < 1)
            {
                throw new ArgumentException("There are too few rows for the number of folds.");
            }

            var order = Enumerable.Range(0, times.Length).OrderBy(i => times[i]).ThenBy(i => i).ToArray();
            var result = new List<SplitFold>();

            for (var fold = 0; fold < folds; fold++)
            {
                var testStart = order.Length - (folds - fold) * testSize;
                var trainEnd = testStart - gap;

                if (trainEnd < 1)
                {
                    throw new ArgumentException("The gap leaves no training rows.");
                }

                result.Add(
                    new SplitFold(order.Take(trainEnd).ToArray(), order.Skip(testStart).Take(testSize).ToArray()));
            }

            return result.ToArray();
        }
    }
}