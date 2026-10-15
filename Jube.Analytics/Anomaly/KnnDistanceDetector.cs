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
    public sealed class KnnDistanceDetector(int k = 10, int rowCap = 3000) : AnomalyDetectorBase
    {
        private int neighbours;
        private double[][] reference = [];
        public override string Name => "KnnDistance";
        public int K { get; } = k > 0 ? k : throw new ArgumentOutOfRangeException(nameof(k));
        public int RowCap { get; } = rowCap > 1 ? rowCap : throw new ArgumentOutOfRangeException(nameof(rowCap));

        protected override void FitCore(double[][] data, int seed, CancellationToken token)
        {
            reference = AnomalyInput.Subsample(data, RowCap, AnomalyInput.DeriveSeed(seed, 21));
            neighbours = Math.Min(K, reference.Length - 1);
        }

        protected override double ScoreCore(double[] row)
        {
            var self = Array.FindIndex(reference, candidate => candidate.AsSpan().SequenceEqual(row));
            var nearest = NeighbourSearch.Nearest(reference, row, neighbours, self);
            var sum = 0d;

            foreach (var (_, distance) in nearest)
            {
                sum += distance;
            }

            return sum / nearest.Length;
        }
    }
}