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
    public sealed class LocalOutlierFactorDetector(int k = 20, int rowCap = 3000) : AnomalyDetectorBase
    {
        private double[] density = [];
        private double[] kDistance = [];
        private int neighbours;
        private double[][] reference = [];
        public override string Name => "LocalOutlierFactor";
        public int K { get; } = k > 0 ? k : throw new ArgumentOutOfRangeException(nameof(k));
        public int RowCap { get; } = rowCap > 1 ? rowCap : throw new ArgumentOutOfRangeException(nameof(rowCap));

        protected override void FitCore(double[][] data, int seed, CancellationToken token)
        {
            reference = AnomalyInput.Subsample(data, RowCap, AnomalyInput.DeriveSeed(seed, 22));
            neighbours = Math.Min(K, reference.Length - 1);
            var m = reference.Length;
            var found = new (int Index, double Distance)[m][];
            kDistance = new double[m];

            Parallel.For(0, m, AnomalyInput.Options(token), i =>
            {
                found[i] = NeighbourSearch.Nearest(reference, reference[i], neighbours, i);
                kDistance[i] = found[i][^1].Distance;
            });

            density = new double[m];

            Parallel.For(0, m, AnomalyInput.Options(token), i =>
            {
                var reach = 0d;

                foreach (var (index, distance) in found[i])
                {
                    reach += Math.Max(kDistance[index], distance);
                }

                density[i] = 1d / (reach / found[i].Length + 1e-12);
            });
        }

        protected override double ScoreCore(double[] row)
        {
            var self = Array.FindIndex(reference, candidate => candidate.AsSpan().SequenceEqual(row));
            var nearest = NeighbourSearch.Nearest(reference, row, neighbours, self);
            var reach = 0d;
            var neighbourDensity = 0d;

            foreach (var (index, distance) in nearest)
            {
                reach += Math.Max(kDistance[index], distance);
                neighbourDensity += density[index];
            }

            var lrd = 1d / (reach / nearest.Length + 1e-12);

            return neighbourDensity / nearest.Length / lrd;
        }
    }
}