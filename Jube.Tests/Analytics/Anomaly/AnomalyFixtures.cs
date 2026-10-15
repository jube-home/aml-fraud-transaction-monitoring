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

namespace Jube.Test.Analytics.Anomaly
{
    public static class AnomalyFixtures
    {
        public static double Gaussian(Random rng)
        {
            var u1 = 1d - rng.NextDouble();
            var u2 = rng.NextDouble();

            return Math.Sqrt(-2d * Math.Log(u1)) * Math.Cos(2d * Math.PI * u2);
        }

        public static (double[][] Data, int[] Labels) ClusterWithOutliers(int seed, int rows = 600, int dimensions = 6,
            double shift = 6d, double fraction = 0.03)
        {
            var rng = new Random(seed);
            var outliers = Math.Max(3, (int)(rows * fraction));
            var data = new double[rows][];
            var labels = new int[rows];

            for (var i = 0; i < rows; i++)
            {
                var anomalous = i < outliers;
                labels[i] = anomalous ? 1 : 0;
                data[i] = new double[dimensions];
                var factor = Gaussian(rng);

                for (var j = 0; j < dimensions; j++)
                {
                    data[i][j] = 0.5 * factor + Gaussian(rng);

                    if (anomalous && j < 3)
                    {
                        data[i][j] += shift;
                    }
                }
            }

            var order = Enumerable.Range(0, rows).OrderBy(_ => rng.Next()).ToArray();

            return ([.. order.Select(i => data[i])], [.. order.Select(i => labels[i])]);
        }

        public static (double[][] Data, int[] Labels) CorrelationBreak(int seed, int rows = 800)
        {
            var rng = new Random(seed);
            var outliers = 24;
            var data = new double[rows][];
            var labels = new int[rows];

            for (var i = 0; i < rows; i++)
            {
                var anomalous = i < outliers;
                labels[i] = anomalous ? 1 : 0;
                var x = Gaussian(rng);
                var y = 0.97 * x + 0.05 * Gaussian(rng);

                if (anomalous)
                {
                    y = -y;
                }

                data[i] = [x, y, Gaussian(rng), Gaussian(rng)];

                if (anomalous)
                {
                    data[i][0] = Math.Clamp(data[i][0], -1.2, 1.2);
                }

                if (anomalous)
                {
                    data[i][1] = Math.Sign(x) * -0.9;
                }
            }

            var order = Enumerable.Range(0, rows).OrderBy(_ => rng.Next()).ToArray();

            return ([.. order.Select(i => data[i])], [.. order.Select(i => labels[i])]);
        }

        public static double[][] ReferenceRows()
        {
            var rows = Enumerable.Range(1, 15)
                .Select(i => new[] { Math.Sin(i), Math.Sin(i) * 0.6 + Math.Cos(2 * i) * 0.5, Math.Cos(3 * i) })
                .ToList();
            rows.Add([4.0, 3.5, -3.0]);

            return [.. rows];
        }

        public static double[][] ReferenceProbes()
        {
            return [[0.3, 0.2, -0.5], [3.0, 3.0, 3.0], [-1.0, 0.5, 0.9]];
        }
    }
}