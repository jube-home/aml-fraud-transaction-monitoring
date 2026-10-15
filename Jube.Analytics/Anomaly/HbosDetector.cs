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
    public sealed class HbosDetector(int bins = 0) : AnomalyDetectorBase
    {
        private double[] floorHeight = [];
        private double[][] height = [];
        private double[] minimum = [];
        private bool[] used = [];
        private double[] width = [];
        public override string Name => "Hbos";
        public int Bins { get; } = bins >= 0 ? bins : throw new ArgumentOutOfRangeException(nameof(bins));

        protected override void FitCore(double[][] data, int seed, CancellationToken token)
        {
            var n = data.Length;
            var d = data[0].Length;
            var k = Bins > 0 ? Bins : Math.Clamp((int)Math.Ceiling(Math.Sqrt(n)), 2, 100);
            minimum = new double[d];
            width = new double[d];
            height = new double[d][];
            used = new bool[d];
            floorHeight = new double[d];

            for (var j = 0; j < d; j++)
            {
                token.ThrowIfCancellationRequested();
                var min = double.PositiveInfinity;
                var max = double.NegativeInfinity;

                for (var i = 0; i < n; i++)
                {
                    if (data[i][j] < min)
                    {
                        min = data[i][j];
                    }

                    if (data[i][j] > max)
                    {
                        max = data[i][j];
                    }
                }

                if (!(max > min))
                {
                    continue;
                }

                used[j] = true;
                minimum[j] = min;
                width[j] = (max - min) / k;
                var counts = new double[k];

                for (var i = 0; i < n; i++)
                {
                    counts[Bin(j, data[i][j], k)]++;
                }

                var densityMax = counts.Max() / width[j];
                height[j] = new double[k];

                for (var b = 0; b < k; b++)
                {
                    height[j][b] = Math.Max(counts[b], 0.5) / width[j] / densityMax;
                }

                floorHeight[j] = 0.5 / width[j] / densityMax;
            }
        }

        private int Bin(int j, double value, int k)
        {
            var b = (int)Math.Floor((value - minimum[j]) / width[j]);

            return Math.Clamp(b, 0, k - 1);
        }

        protected override double ScoreCore(double[] row)
        {
            var score = 0d;

            for (var j = 0; j < row.Length; j++)
            {
                if (!used[j])
                {
                    continue;
                }

                var k = height[j].Length;

                var h = row[j] < minimum[j] || row[j] > minimum[j] + width[j] * k
                    ? floorHeight[j]
                    : height[j][Bin(j, row[j], k)];

                score += -Math.Log(h);
            }

            return score;
        }
    }
}