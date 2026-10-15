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
using Jube.Analytics;

namespace Jube.Engine.AdvancedAnalytics.Clustering
{
    internal sealed class KMedoidsClustererPipeline
    {
        private readonly double[] fill;
        private readonly double[] means;
        private readonly double[][] medoids;
        private readonly double[] standardDeviations;

        public KMedoidsClustererPipeline(bool standardise, double[] fill, double[] means, double[] standardDeviations,
            double[][] medoids)
        {
            Standardise = standardise;
            this.fill = (double[])fill.Clone();
            this.means = (double[])means.Clone();
            this.standardDeviations = (double[])standardDeviations.Clone();
            this.medoids = [.. medoids.Select(c => (double[])c.Clone())];
        }

        public int InputCount => fill.Length;
        public int ClusterCount => medoids.Length;
        public bool Standardise { get; }
        public double[] Fill => (double[])fill.Clone();
        public double[] Means => (double[])means.Clone();
        public double[] StandardDeviations => (double[])standardDeviations.Clone();
        public double[][] Medoids => [.. medoids.Select(c => (double[])c.Clone())];

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
                var value = double.IsFinite(row[j]) ? row[j] : fill[j];
                prepared[j] = Standardise ? Standardisation.ZScore(value, means[j], standardDeviations[j]) : value;
            }

            return prepared;
        }

        public double[] DistancesByClusterIndex(double[] row)
        {
            var prepared = Prepare(row);
            var distances = new double[medoids.Length];

            for (var c = 0; c < medoids.Length; c++)
            {
                var sum = 0d;

                for (var j = 0; j < prepared.Length; j++)
                {
                    var d = prepared[j] - medoids[c][j];
                    sum += d * d;
                }

                distances[c] = Math.Sqrt(sum);
            }

            return distances;
        }
    }
}