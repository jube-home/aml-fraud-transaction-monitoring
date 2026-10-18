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
using System.Collections.Generic;
using System.Linq;

namespace Jube.Test.AdvancedAnalytics.Slice
{
    public sealed record SliceDataset(double[][] Inputs, int[] Labels, string[] Names);

    public static class SliceData
    {
        public static double Sigmoid(double z)
        {
            return 1d / (1d + Math.Exp(-z));
        }

        public static double Gaussian(Random random)
        {
            var u1 = 1d - random.NextDouble();
            var u2 = random.NextDouble();

            return Math.Sqrt(-2d * Math.Log(u1)) * Math.Cos(2d * Math.PI * u2);
        }

        public static SliceDataset Logistic(int n, int seed, double intercept, double[] slopes, int noiseColumns = 0,
            double missingRate = 0d)
        {
            var random = new Random(seed);
            var columns = slopes.Length + noiseColumns;
            var inputs = new double[n][];
            var labels = new int[n];

            for (var i = 0; i < n; i++)
            {
                var row = new double[columns];
                var eta = intercept;

                for (var j = 0; j < columns; j++)
                {
                    row[j] = Gaussian(random);

                    if (j < slopes.Length)
                    {
                        eta += slopes[j] * row[j];
                    }
                }

                labels[i] = random.NextDouble() < Sigmoid(eta) ? 1 : 0;

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

            return new SliceDataset(inputs, labels, [.. Enumerable.Range(0, columns).Select(j => "x" + j)]);
        }

        public static SliceDataset Reference(int n, uint seed, double missingRate)
        {
            var state = seed;

            double Next()
            {
                state = unchecked(1664525u * state + 1013904223u);

                return state / 4294967296.0;
            }

            var inputs = new double[n][];
            var labels = new int[n];

            for (var i = 0; i < n; i++)
            {
                var x = new double[3];

                for (var j = 0; j < 3; j++)
                {
                    var sum = 0d;

                    for (var k = 0; k < 4; k++)
                    {
                        sum += Next();
                    }

                    x[j] = sum - 2.0;
                }

                var uMissing = Next();
                var uLabel = Next();
                var eta = -0.5 + 1.2 * x[0] + 0.0 * x[1] - 0.8 * x[2];
                labels[i] = uLabel < Sigmoid(eta) ? 1 : 0;

                if (missingRate > 0 && uMissing < missingRate)
                {
                    x[1] = double.NaN;
                }

                inputs[i] = x;
            }

            return new SliceDataset(inputs, labels, ["x0", "x1", "x2"]);
        }

        public static double[][] Copy(double[][] source)
        {
            return [.. source.Select(r => (double[])r.Clone())];
        }

        public static long Bits(double value)
        {
            return BitConverter.DoubleToInt64Bits(value);
        }

        public static IEnumerable<T> Repeat<T>(int count, Func<int, T> factory)
        {
            return Enumerable.Range(0, count).Select(factory);
        }
    }
}