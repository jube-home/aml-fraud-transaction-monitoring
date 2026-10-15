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

using System;

namespace Jube.Engine.Exhaustive.Statistics
{
    internal static class ClassHistogramComputer
    {
        internal static ClassHistogramResult Compute(double[] binStart, double[] binEnd, double[] labelledValues,
            double[] labels)
        {
            var bins = binStart.Length;

            if (bins == 0)
            {
                return null;
            }

            var lower = binStart[0];
            var span = binEnd[bins - 1] - lower;
            var negative = new int[bins];
            var positive = new int[bins];

            for (var row = 0; row < labelledValues.Length; row++)
            {
                var index = 0;

                if (span > 0)
                {
                    var scaled = bins * (labelledValues[row] - lower) / span;
                    index = double.IsNaN(scaled) ? 0 : (int)Math.Clamp(scaled, 0, bins - 1);
                }

                if (labels[row] > 0.5)
                {
                    positive[index]++;
                }
                else
                {
                    negative[index]++;
                }
            }

            return new ClassHistogramResult
                { BinStart = binStart, BinEnd = binEnd, Negative = negative, Positive = positive };
        }
    }
}