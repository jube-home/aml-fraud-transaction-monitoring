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

using Jube.Charting.Exceptions;

namespace Jube.Charting.More
{
    public static class HierarchicalOrder
    {
        public const int MaxItems = 500;

        public static int[] Order(double[,] distance)
        {
            var n = distance.GetLength(0);

            if (distance.GetLength(1) != n)
            {
                throw new ChartSpecificationException("The distance matrix must be square.");
            }

            if (n > MaxItems)
            {
                throw new ChartLimitException("clustered item count", n, MaxItems);
            }

            if (n <= 2)
            {
                return [.. Enumerable.Range(0, n)];
            }

            var worst = 0.0;

            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < n; j++)
                {
                    if (double.IsFinite(distance[i, j]))
                    {
                        worst = Math.Max(worst, distance[i, j]);
                    }
                }
            }

            double D(int i, int j)
            {
                return double.IsFinite(distance[i, j]) ? distance[i, j] : worst;
            }

            var leaves = Enumerable.Range(0, n).Select(i => new List<int> { i }).ToList();
            var active = Enumerable.Range(0, n).ToList();
            var cluster = new double[n, n];

            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < n; j++)
                {
                    cluster[i, j] = i == j ? 0 : (D(i, j) + D(j, i)) / 2;
                }
            }

            var size = Enumerable.Repeat(1, n).ToArray();

            while (active.Count > 1)
            {
                var bestA = -1;
                var bestB = -1;
                var best = double.PositiveInfinity;

                for (var x = 0; x < active.Count; x++)
                {
                    for (var y = x + 1; y < active.Count; y++)
                    {
                        var d = cluster[active[x], active[y]];

                        if (d < best)
                        {
                            best = d;
                            bestA = active[x];
                            bestB = active[y];
                        }
                    }
                }

                var a = leaves[bestA];
                var b = leaves[bestB];
                var ra = a.AsEnumerable().Reverse().ToList();
                var rb = b.AsEnumerable().Reverse().ToList();
                var options = new[] { (a, b), (a, rb), (ra, b), (ra, rb) };
                var chosen = options[0];
                var chosenDistance = double.PositiveInfinity;

                foreach (var option in options)
                {
                    var d = (D(option.Item1[^1], option.Item2[0]) + D(option.Item2[0], option.Item1[^1])) / 2;

                    if (d < chosenDistance - 1e-12)
                    {
                        chosenDistance = d;
                        chosen = option;
                    }
                }

                var merged = chosen.Item1.Concat(chosen.Item2).ToList();

                foreach (var k in active)
                {
                    if (k == bestA || k == bestB)
                    {
                        continue;
                    }

                    var d = (size[bestA] * cluster[bestA, k] + size[bestB] * cluster[bestB, k]) /
                            (size[bestA] + size[bestB]);

                    cluster[bestA, k] = d;
                    cluster[k, bestA] = d;
                }

                leaves[bestA] = merged;
                size[bestA] += size[bestB];
                active.Remove(bestB);
            }

            return [.. leaves[active[0]]];
        }

        public static int[] OrderRows(IReadOnlyList<IReadOnlyList<double>> rows)
        {
            var n = rows.Count;

            if (n > MaxItems)
            {
                throw new ChartLimitException("clustered item count", n, MaxItems);
            }

            var d = new double[n, n];

            for (var i = 0; i < n; i++)
            {
                for (var j = i + 1; j < n; j++)
                {
                    var sum = 0.0;
                    var count = 0;

                    for (var k = 0; k < Math.Min(rows[i].Count, rows[j].Count); k++)
                    {
                        if (double.IsFinite(rows[i][k]) && double.IsFinite(rows[j][k]))
                        {
                            sum += (rows[i][k] - rows[j][k]) * (rows[i][k] - rows[j][k]);
                            count++;
                        }
                    }

                    d[i, j] = d[j, i] = count == 0 ? double.NaN : Math.Sqrt(sum / count);
                }
            }

            return Order(d);
        }
    }
}