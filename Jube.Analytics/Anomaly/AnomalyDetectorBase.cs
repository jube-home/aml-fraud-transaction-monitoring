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
    public abstract class AnomalyDetectorBase : IAnomalyDetector
    {
        public abstract string Name { get; }
        public int Dimensions { get; private set; }
        public bool IsFitted { get; private set; }

        public void Fit(double[][] data, int seed, CancellationToken token = default)
        {
            AnomalyInput.Validate(data);
            token.ThrowIfCancellationRequested();
            var d = data[0].Length;
            var medians = new double[d];
            var column = new List<double>(data.Length);

            for (var j = 0; j < d; j++)
            {
                column.Clear();

                foreach (var row in data)
                {
                    if (Numerics.IsFinite(row[j]))
                    {
                        column.Add(row[j]);
                    }
                }

                if (column.Count == 0)
                {
                    medians[j] = 0d;
                }
                else
                {
                    var sorted = column.ToArray();
                    Array.Sort(sorted);
                    medians[j] = Numerics.Median(sorted);
                }
            }

            var clean = new double[data.Length][];

            for (var i = 0; i < data.Length; i++)
            {
                var copy = new double[d];

                for (var j = 0; j < d; j++)
                {
                    copy[j] = Numerics.IsFinite(data[i][j]) ? data[i][j] : medians[j];
                }

                clean[i] = copy;
            }

            AnomalyInput.RequireVariation(clean);
            IsFitted = false;
            FitCore(clean, seed, token);
            fill = medians;
            Dimensions = d;
            IsFitted = true;
        }

        public double Score(double[] row)
        {
            if (!IsFitted)
            {
                throw new InvalidOperationException($"{Name} has not been fitted.");
            }

            ArgumentNullException.ThrowIfNull(row);

            if (row.Length != Dimensions)
            {
                throw new ArgumentException($"Row has {row.Length} values but {Name} was fitted on {Dimensions}.");
            }

            var x = new double[Dimensions];

            for (var j = 0; j < Dimensions; j++)
            {
                x[j] = Numerics.IsFinite(row[j]) ? row[j] : fill[j];
            }

            return ScoreCore(x);
        }

        protected abstract void FitCore(double[][] data, int seed, CancellationToken token);
        protected abstract double ScoreCore(double[] row);
        private double[] fill = [];
    }
}