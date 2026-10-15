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
using System.Linq;

namespace Jube.Test.AdvancedAnalytics.Slice
{
    public sealed record LinearSliceDataset(double[][] Inputs, double[] Outputs, string[] Names);

    public static class LinearSliceData
    {
        public static LinearSliceDataset Continuous(int n, int seed, double intercept, double[] slopes,
            int noiseColumns = 0, double missingRate = 0d, double noiseStandardDeviation = 0.3)
        {
            var random = new Random(seed);
            var columns = slopes.Length + noiseColumns;
            var inputs = new double[n][];
            var outputs = new double[n];

            for (var i = 0; i < n; i++)
            {
                var row = new double[columns];
                var y = intercept;

                for (var j = 0; j < columns; j++)
                {
                    row[j] = SliceData.Gaussian(random);

                    if (j < slopes.Length)
                    {
                        y += slopes[j] * row[j];
                    }
                }

                y += noiseStandardDeviation * SliceData.Gaussian(random);
                outputs[i] = y;

                if (missingRate > 0)
                {
                    for (var j = 0; j < columns; j++)
                    {
                        if (random.NextDouble() < missingRate)
                        {
                            row[j] = double.NaN;
                        }
                    }
                }

                inputs[i] = row;
            }

            return new LinearSliceDataset(inputs, outputs,
                [.. Enumerable.Range(0, columns).Select(j => "x" + j)]);
        }
    }
}