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

using Jube.Analytics.Bootstrap.Interfaces;

namespace Jube.Analytics.Bootstrap
{
    public sealed class DVineCopula
    {
        private readonly EmpiricalMargin[] margins;
        private readonly int[] order;
        private readonly IBivariateCopula[][] treeCopulas;

        private DVineCopula(int[] order, EmpiricalMargin[] margins, IBivariateCopula[][] treeCopulas)
        {
            this.order = order;
            this.margins = margins;
            this.treeCopulas = treeCopulas;
            Dimension = order.Length;
        }

        public int Dimension { get; }

        public static DVineCopula Fit(double[][] columns, int truncationLevel = int.MaxValue)
        {
            ArgumentNullException.ThrowIfNull(columns);
            var d = columns.Length;

            if (d < 1)
            {
                throw new ArgumentException("At least one column is required.");
            }

            var n = columns[0].Length;

            if (columns.Any(c => c.Length != n))
            {
                throw new ArgumentException("Every column must have the same number of rows.");
            }

            var order = VineVariableOrder.GreedyPathByAbsoluteTau(columns);

            var margins = new EmpiricalMargin[d];
            var pseudo = new double[d][];

            for (var pos = 0; pos < d; pos++)
            {
                margins[pos] = EmpiricalMargin.Fit(columns[order[pos]]);
                pseudo[pos] = margins[pos].PseudoObservations;
            }

            var rightWin = new double[d][][];
            var leftWin = new double[d][][];
            rightWin[0] = new double[d][];
            leftWin[0] = new double[d][];

            for (var p = 0; p < d; p++)
            {
                rightWin[0][p] = pseudo[p];
                leftWin[0][p] = pseudo[p];
            }

            var treeCopulas = new IBivariateCopula[d][];

            for (var t = 1; t < d; t++)
            {
                var width = d - t;
                rightWin[t] = new double[width][];
                leftWin[t] = new double[width][];
                treeCopulas[t] = new IBivariateCopula[width];

                for (var p = 0; p < width; p++)
                {
                    var u = rightWin[t - 1][p];
                    var v = leftWin[t - 1][p + 1];

                    var copula = t > truncationLevel
                        ? IndependenceCopula.Instance
                        : BivariateCopulaFitter.FitBest(u, v);

                    treeCopulas[t][p] = copula;

                    var newRight = new double[n];
                    var newLeft = new double[n];

                    for (var i = 0; i < n; i++)
                    {
                        newRight[i] = copula.ConditionalCdf(v[i], u[i]);
                        newLeft[i] = copula.ConditionalCdf(u[i], v[i]);
                    }

                    rightWin[t][p] = newRight;
                    leftWin[t][p] = newLeft;
                }
            }

            return new DVineCopula(order, margins, treeCopulas);
        }

        public double[][] Sample(int count, Random random)
        {
            ArgumentNullException.ThrowIfNull(random);
            var d = Dimension;
            var result = new double[count][];

            for (var row = 0; row < count; row++)
            {
                var rightWin = new double[Math.Max(d, 1), Math.Max(d, 1)];
                var leftWin = new double[Math.Max(d, 1), Math.Max(d, 1)];

                var x = new double[d];
                x[0] = random.NextDouble();
                rightWin[0, 0] = x[0];
                leftWin[0, 0] = x[0];

                for (var i = 1; i < d; i++)
                {
                    leftWin[i, 0] = random.NextDouble();

                    for (var t = i; t >= 1; t--)
                    {
                        var p = i - t;
                        var conditioning = rightWin[t - 1, p];
                        var target = leftWin[t, p];
                        var copula = treeCopulas[t][p];
                        leftWin[t - 1, p + 1] = copula.InverseConditionalCdf(conditioning, target);
                    }

                    x[i] = leftWin[0, i];
                    rightWin[0, i] = x[i];

                    if (i == d - 1)
                    {
                        break;
                    }

                    for (var k = 0; k < i; k++)
                    {
                        var window = i - k;
                        var previousRight = rightWin[window - 1, k];
                        var leftValue = leftWin[window - 1, k + 1];
                        var copula = treeCopulas[window][k];
                        rightWin[window, k] = copula.ConditionalCdf(leftValue, previousRight);
                    }
                }

                var sampled = new double[d];

                for (var pos = 0; pos < d; pos++)
                {
                    sampled[order[pos]] = margins[pos].Inverse(x[pos]);
                }

                result[row] = sampled;
            }

            return result;
        }
    }
}