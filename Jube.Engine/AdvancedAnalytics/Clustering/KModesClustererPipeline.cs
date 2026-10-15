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

namespace Jube.Engine.AdvancedAnalytics.Clustering
{
    internal sealed class KModesClustererPipeline
    {
        private readonly double[] fill;
        private readonly double[][] modes;

        public KModesClustererPipeline(double[] fill, double[][] modes)
        {
            this.fill = (double[])fill.Clone();
            this.modes = [.. modes.Select(m => (double[])m.Clone())];
        }

        public int InputCount => fill.Length;
        public int ClusterCount => modes.Length;
        public double[] Fill => (double[])fill.Clone();
        public double[][] Modes => [.. modes.Select(m => (double[])m.Clone())];

        public static double Quantise(double value)
        {
            return Math.Round(value, MidpointRounding.AwayFromZero);
        }

        public double[] Prepare(double[] row)
        {
            var count = fill.Length;

            if (row.Length != count)
            {
                throw new ArgumentException($"A row has {row.Length} values but the model expects {count}.");
            }

            var prepared = new double[count];

            for (var j = 0; j < count; j++)
            {
                prepared[j] = double.IsFinite(row[j]) ? Quantise(row[j]) : fill[j];
            }

            return prepared;
        }

        public double[] DistancesByClusterIndex(double[] row)
        {
            var prepared = Prepare(row);
            var distances = new double[modes.Length];

            for (var c = 0; c < modes.Length; c++)
            {
                var mismatches = 0d;
                var mode = modes[c];

                for (var j = 0; j < prepared.Length; j++)
                {
                    if (Math.Abs(prepared[j] - mode[j]) > 0.0001)
                    {
                        mismatches += 1d;
                    }
                }

                distances[c] = mismatches;
            }

            return distances;
        }
    }
}