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

using Jube.Graph.Model;

namespace Jube.Graph.Algorithms
{
    public static class Hits
    {
        public static (double[] Hub, double[] Authority) Compute(CompressedGraph graph, double tolerance = 1e-9,
            int maxIterations = 100)
        {
            ArgumentNullException.ThrowIfNull(graph);

            var n = graph.NodeCount;
            var hub = new double[n];
            var authority = new double[n];

            if (n == 0)
            {
                return (hub, authority);
            }

            Array.Fill(hub, 1d);
            var nextHub = new double[n];
            var nextAuthority = new double[n];

            for (var iteration = 0; iteration < maxIterations; iteration++)
            {
                Array.Clear(nextAuthority);

                for (var u = 0; u < n; u++)
                {
                    for (var e = graph.Offsets[u]; e < graph.Offsets[u + 1]; e++)
                    {
                        nextAuthority[graph.Neighbours[e]] += hub[u] * graph.Weights[e];
                    }
                }

                NormaliseToUnitMaximum(nextAuthority);

                for (var u = 0; u < n; u++)
                {
                    var sum = 0d;

                    for (var e = graph.Offsets[u]; e < graph.Offsets[u + 1]; e++)
                    {
                        sum += nextAuthority[graph.Neighbours[e]] * graph.Weights[e];
                    }

                    nextHub[u] = sum;
                }

                NormaliseToUnitMaximum(nextHub);
                var delta = 0d;

                for (var u = 0; u < n; u++)
                {
                    delta += Math.Abs(nextHub[u] - hub[u]) + Math.Abs(nextAuthority[u] - authority[u]);
                }

                (hub, nextHub) = (nextHub, hub);
                (authority, nextAuthority) = (nextAuthority, authority);

                if (delta < tolerance)
                {
                    break;
                }
            }

            return (hub, authority);
        }

        private static void NormaliseToUnitMaximum(double[] values)
        {
            var maximum = 0d;

            foreach (var v in values)
            {
                maximum = Math.Max(maximum, v);
            }

            if (maximum <= 0)
            {
                return;
            }

            for (var i = 0; i < values.Length; i++)
            {
                values[i] /= maximum;
            }
        }
    }
}
