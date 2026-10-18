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

using Jube.Analytics.Anomaly.Interfaces;

namespace Jube.Analytics.Anomaly
{
    public static class AnomalyInput
    {
        public static void Validate(double[][]? data)
        {
            if (data == null || data.Length == 0)
            {
                throw new ArgumentException("There is no data to fit on.");
            }

            if (data.Length < 2)
            {
                throw new ArgumentException("At least 2 rows are required to fit an anomaly detector.");
            }

            var d = data[0].Length;

            if (d == 0)
            {
                throw new ArgumentException("Rows must have at least one variable.");
            }

            for (var i = 0; i < data.Length; i++)
            {
                if (data[i] == null || data[i].Length != d)
                {
                    throw new ArgumentException($"Row {i} does not have the same number of variables as row 0.");
                }
            }
        }

        public static double[] ScoreAll(IAnomalyDetector detector, double[][] rows, CancellationToken token = default,
            int maxDegreeOfParallelism = 0)
        {
            ArgumentNullException.ThrowIfNull(detector);
            ArgumentNullException.ThrowIfNull(rows);
            var scores = new double[rows.Length];

            var options = new ParallelOptions
            {
                CancellationToken = token,
                MaxDegreeOfParallelism = maxDegreeOfParallelism > 0
                    ? maxDegreeOfParallelism
                    : Math.Max(1, Environment.ProcessorCount / 2)
            };

            Parallel.For(0, rows.Length, options, i => scores[i] = detector.Score(rows[i]));

            return scores;
        }

        internal static void RequireVariation(double[][] data)
        {
            var d = data[0].Length;

            for (var j = 0; j < d; j++)
            {
                var first = data[0][j];

                for (var i = 1; i < data.Length; i++)
                {
                    if (Math.Abs(data[i][j] - first) > 0.0001)
                    {
                        return;
                    }
                }
            }

            throw new ArgumentException("Every variable is constant so there is nothing for a detector to learn.");
        }

        internal static ParallelOptions Options(CancellationToken token)
        {
            return new ParallelOptions
            {
                CancellationToken = token,
                MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2)
            };
        }

        internal static int DeriveSeed(int seed, int index)
        {
            unchecked
            {
                var h = (uint)seed * 2654435761u + (uint)(index + 1) * 40503u;
                h ^= h >> 15;
                h *= 2246822519u;
                h ^= h >> 13;

                return (int)(h & 0x7fffffff);
            }
        }

        internal static double[][] Subsample(double[][] data, int cap, int seed)
        {
            if (data.Length <= cap)
            {
                return data;
            }

            var rng = new Random(seed);
            var index = Enumerable.Range(0, data.Length).ToArray();

            for (var i = 0; i < cap; i++)
            {
                var j = i + rng.Next(data.Length - i);
                (index[i], index[j]) = (index[j], index[i]);
            }

            var result = new double[cap][];

            for (var i = 0; i < cap; i++)
            {
                result[i] = data[index[i]];
            }

            return result;
        }

        internal static double NextGaussian(Random rng)
        {
            var u1 = 1d - rng.NextDouble();
            var u2 = rng.NextDouble();

            return Math.Sqrt(-2d * Math.Log(u1)) * Math.Cos(2d * Math.PI * u2);
        }
    }
}