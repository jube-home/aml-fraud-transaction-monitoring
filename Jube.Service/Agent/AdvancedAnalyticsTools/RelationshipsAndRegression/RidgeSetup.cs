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

using Accord.Statistics;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression
{
    internal sealed class RidgeSetup
    {
        public int N { get; init; }
        public int P { get; init; }
        public double[] Means { get; init; } = Array.Empty<double>();
        public double[] Deviations { get; init; } = Array.Empty<double>();
        public double[][] Z { get; init; } = Array.Empty<double[]>();
        public double[] CentredY { get; init; } = Array.Empty<double>();
        public double YMean { get; init; }
        public double[][] ZtZ { get; init; } = Array.Empty<double[]>();
        public double[] ZtY { get; init; } = Array.Empty<double>();
        public double Tss { get; init; }

        public static RidgeSetup Create(double[][] x, double[] y)
        {
            var n = x.Length;
            var p = x[0].Length;
            var means = x.Mean(0);
            var deviations = x.StandardDeviation(means);

            for (var j = 0; j < p; j++)
            {
                if (!(deviations[j] > 0))
                {
                    throw new ArgumentException(
                        $"Predictor column {j} has no variation, so it cannot be standardised for ridge regression. Remove it.",
                        "inputs");
                }
            }

            var z = x.Select(r => r.Select((v, j) => (v - means[j]) / deviations[j]).ToArray()).ToArray();
            var yMean = y.Average();
            var centred = y.Select(v => v - yMean).ToArray();
            var ztz = new double[p][];

            for (var a = 0; a < p; a++)
            {
                ztz[a] = new double[p];
            }

            var zty = new double[p];

            for (var i = 0; i < n; i++)
            {
                for (var a = 0; a < p; a++)
                {
                    zty[a] += z[i][a] * centred[i];

                    for (var b = a; b < p; b++)
                    {
                        ztz[a][b] += z[i][a] * z[i][b];
                    }
                }
            }

            for (var a = 0; a < p; a++)
            {
                for (var b = 0; b < a; b++)
                {
                    ztz[a][b] = ztz[b][a];
                }
            }

            return new RidgeSetup
            {
                N = n,
                P = p,
                Means = means,
                Deviations = deviations,
                Z = z,
                CentredY = centred,
                YMean = yMean,
                ZtZ = ztz,
                ZtY = zty,
                Tss = centred.Sum(v => v * v)
            };
        }
    }
}