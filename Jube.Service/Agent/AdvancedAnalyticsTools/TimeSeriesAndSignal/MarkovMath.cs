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

namespace Jube.Service.Agent.AdvancedAnalyticsTools.TimeSeriesAndSignal
{
    internal static class MarkovMath
    {
        public const int MaximumStates = 500;

        public static double[][] Matrix(double[][]? matrix, string name, bool requireStochastic = true)
        {
            if (matrix == null || matrix.Length == 0)
            {
                throw new ArgumentException(
                    $"{name} is required: a square array of arrays whose rows give the probability of moving from each state to every state.",
                    name);
            }

            var n = matrix.Length;

            if (n > MaximumStates)
            {
                throw new ArgumentException($"{name} has {n} states; the limit is {MaximumStates}.", name);
            }

            for (var i = 0; i < n; i++)
            {
                if (matrix[i] == null || matrix[i].Length != n)
                {
                    throw new ArgumentException(
                        $"{name} must be square: row {i} has {(matrix[i] == null ? 0 : matrix[i].Length)} entries but there are {n} rows.",
                        name);
                }

                var sum = 0.0;

                for (var j = 0; j < n; j++)
                {
                    var p = matrix[i][j];

                    if (double.IsNaN(p) || double.IsInfinity(p) || p < 0 || p > 1 + 1e-9)
                    {
                        throw new ArgumentException($"{name}[{i}][{j}] is {p}; probabilities must be between 0 and 1.",
                            name);
                    }

                    sum += p;
                }

                if (requireStochastic && Math.Abs(sum - 1) > 1e-6)
                {
                    throw new ArgumentException(
                        $"Row {i} of {name} sums to {sum}, not 1. Each row must be a probability distribution over the next state.",
                        name);
                }
            }

            return matrix;
        }

        public static bool[,] Reachability(double[][] p)
        {
            var n = p.Length;
            var reach = new bool[n, n];

            for (var i = 0; i < n; i++)
            {
                reach[i, i] = true;

                for (var j = 0; j < n; j++)
                {
                    if (p[i][j] > 0)
                    {
                        reach[i, j] = true;
                    }
                }
            }

            for (var k = 0; k < n; k++)
            {
                for (var i = 0; i < n; i++)
                {
                    if (!reach[i, k])
                    {
                        continue;
                    }

                    for (var j = 0; j < n; j++)
                    {
                        if (reach[k, j])
                        {
                            reach[i, j] = true;
                        }
                    }
                }
            }

            return reach;
        }

        public static (int ClosedClasses, int[] Transient, int[] Absorbing) Classify(double[][] p, bool[,] reach)
        {
            var n = p.Length;
            var inClosed = new bool[n];
            var closedClasses = 0;
            var seen = new bool[n];

            for (var i = 0; i < n; i++)
            {
                if (seen[i])
                {
                    continue;
                }

                var members = Enumerable.Range(0, n).Where(j => reach[i, j] && reach[j, i]).ToArray();

                foreach (var m in members)
                {
                    seen[m] = true;
                }

                var closed = members.All(m => Enumerable.Range(0, n).All(j => p[m][j] <= 0 || members.Contains(j)));

                if (closed)
                {
                    closedClasses++;

                    foreach (var m in members)
                    {
                        inClosed[m] = true;
                    }
                }
            }

            var transient = Enumerable.Range(0, n).Where(i => !inClosed[i]).ToArray();
            var absorbing = Enumerable.Range(0, n).Where(i => p[i][i] >= 1 - 1e-12).ToArray();

            return (closedClasses, transient, absorbing);
        }

        public static double[] Stationary(double[][] p, out bool unique, out int closedClasses, out int[] transient,
            out int[] absorbing, out string note)
        {
            var n = p.Length;
            var reach = Reachability(p);
            (closedClasses, transient, absorbing) = Classify(p, reach);

            note =
                "Unique long-run distribution: the chain has a single recurrent class, so it does not depend on the starting state.";

            unique = closedClasses == 1;

            if (n == 1)
            {
                return new[] { 1.0 };
            }

            if (unique)
            {
                var a = new double[n, n];
                var b = new double[n];

                for (var i = 0; i < n; i++)
                {
                    for (var j = 0; j < n; j++)
                    {
                        a[i, j] = p[j][i] - (i == j ? 1 : 0);
                    }
                }

                for (var j = 0; j < n; j++)
                {
                    a[n - 1, j] = 1;
                }

                b[n - 1] = 1;
                var solution = Accord.Math.Matrix.Solve(a, b, true);
                var clean = solution.Select(x => Math.Abs(x) < 1e-12 ? 0 : x).ToArray();

                if (clean.All(x => x >= -1e-9))
                {
                    var total = clean.Sum(x => Math.Max(0, x));

                    return clean.Select(x => Math.Max(0, x) / total).ToArray();
                }
            }

            note = closedClasses > 1
                ? $"The chain has {closedClasses} separate recurrent classes, so the long-run distribution depends on where it starts; this is the average from a uniform start."
                : "The exact solution was not usable; this is a long-run average from a uniform start.";

            unique = false;
            var current = Enumerable.Repeat(1.0 / n, n).ToArray();
            var average = new double[n];
            const int steps = 20000;

            for (var s = 0; s < steps; s++)
            {
                var next = new double[n];

                for (var i = 0; i < n; i++)
                {
                    if (current[i] == 0)
                    {
                        continue;
                    }

                    for (var j = 0; j < n; j++)
                    {
                        next[j] += current[i] * p[i][j];
                    }
                }

                current = next;

                for (var i = 0; i < n; i++)
                {
                    average[i] += current[i];
                }
            }

            return average.Select(x => x / steps).ToArray();
        }

        public static double Log2(double x)
        {
            return Math.Log(x) / Math.Log(2);
        }
    }
}