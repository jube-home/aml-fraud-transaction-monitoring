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
    public sealed class MarginalLikelihoodDetector(int discreteLimit = 10) : AnomalyDetectorBase
    {
        private Dictionary<double, double>?[] categorical = [];
        private double[] mean = [];
        private double[] unseen = [];
        private double[] variance = [];
        public override string Name => "MarginalLikelihood";
        public int DiscreteLimit { get; } = discreteLimit;

        protected override void FitCore(double[][] data, int seed, CancellationToken token)
        {
            var n = data.Length;
            var d = data[0].Length;
            mean = new double[d];
            variance = new double[d];
            categorical = new Dictionary<double, double>?[d];
            unseen = new double[d];

            for (var j = 0; j < d; j++)
            {
                token.ThrowIfCancellationRequested();
                var counts = new Dictionary<double, int>();
                var sum = 0d;

                for (var i = 0; i < n; i++)
                {
                    sum += data[i][j];

                    if (counts.Count <= DiscreteLimit)
                    {
                        counts[data[i][j]] = counts.GetValueOrDefault(data[i][j]) + 1;
                    }
                }

                mean[j] = sum / n;
                var ss = 0d;

                for (var i = 0; i < n; i++)
                {
                    ss += (data[i][j] - mean[j]) * (data[i][j] - mean[j]);
                }

                variance[j] = Math.Max(ss / n, 1e-12);

                if (counts.Count <= DiscreteLimit)
                {
                    var categories = counts.Count + 1;
                    categorical[j] = counts.ToDictionary(p => p.Key, p => (p.Value + 1d) / (n + categories));
                    unseen[j] = 1d / (n + categories);
                }
            }
        }

        protected override double ScoreCore(double[] row)
        {
            var score = 0d;

            for (var j = 0; j < row.Length; j++)
            {
                var table = categorical[j];

                if (table != null)
                {
                    score += -Math.Log(table.TryGetValue(row[j], out var p) ? p : unseen[j]);
                }
                else
                {
                    var z = row[j] - mean[j];
                    score += 0.5 * (Math.Log(2d * Math.PI * variance[j]) + z * z / variance[j]);
                }
            }

            return score;
        }
    }
}