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

using Jube.Analytics.Anomaly.Models;

namespace Jube.Analytics.Anomaly
{
    public sealed class ReducerModel
    {
        private readonly double[] centre;
        private readonly double[] fill;
        private readonly int[] kept;
        private readonly double[][] projection;
        private readonly double[] projectionMean;
        private readonly double[] scale;

        internal ReducerModel(int inputDimensions, int[] kept, double[] fill, double[] centre, double[] scale,
            double[][] projection, double[] projectionMean, IReadOnlyList<VariableDecision> decisions,
            ReductionOptions options)
        {
            InputDimensions = inputDimensions;
            this.kept = kept;
            this.fill = fill;
            this.centre = centre;
            this.scale = scale;
            this.projection = projection;
            this.projectionMean = projectionMean;
            Decisions = decisions;
            Options = options;
        }

        public int InputDimensions { get; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public ReductionOptions Options { get; }
        public IReadOnlyList<int> KeptIndices => kept;
        public IReadOnlyList<VariableDecision> Decisions { get; }
        public int Components => projection.Length;
        public int OutputDimensions => projection.Length > 0 ? projection.Length : kept.Length;

        public double[] Transform(double[] row)
        {
            ArgumentNullException.ThrowIfNull(row);

            if (row.Length != InputDimensions)
            {
                throw new ArgumentException(
                    $"Row has {row.Length} values but the reducer was fitted on {InputDimensions}.");
            }

            var scaled = new double[kept.Length];

            for (var c = 0; c < kept.Length; c++)
            {
                var v = row[kept[c]];

                if (!Numerics.IsFinite(v))
                {
                    v = fill[c];
                }

                scaled[c] = (v - centre[c]) / scale[c];
            }

            if (projection.Length == 0)
            {
                return scaled;
            }

            var result = new double[projection.Length];

            for (var p = 0; p < projection.Length; p++)
            {
                var sum = 0d;

                for (var c = 0; c < scaled.Length; c++)
                {
                    sum += (scaled[c] - projectionMean[c]) * projection[p][c];
                }

                result[p] = sum;
            }

            return result;
        }

        public double[][] Transform(double[][] rows)
        {
            return rows.Select(Transform).ToArray();
        }
    }
}