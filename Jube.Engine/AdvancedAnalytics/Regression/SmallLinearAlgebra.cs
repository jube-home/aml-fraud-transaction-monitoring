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

namespace Jube.Engine.AdvancedAnalytics.Regression
{
    internal static class SmallLinearAlgebra
    {
        public static double[][] Cholesky(double[][] matrix)
        {
            var n = matrix.Length;
            var l = new double[n][];

            for (var i = 0; i < n; i++)
            {
                l[i] = new double[n];
            }

            var scale = 0d;

            for (var i = 0; i < n; i++)
            {
                scale = Math.Max(scale, Math.Abs(matrix[i][i]));
            }

            var floor = Math.Max(scale, 1d) * 1e-13;

            for (var i = 0; i < n; i++)
            for (var j = 0; j <= i; j++)
            {
                var sum = matrix[i][j];

                for (var k = 0; k < j; k++)
                {
                    sum -= l[i][k] * l[j][k];
                }

                if (i == j)
                {
                    if (!(sum > floor))
                    {
                        return null;
                    }

                    l[i][j] = Math.Sqrt(sum);
                }
                else
                {
                    l[i][j] = sum / l[j][j];
                }
            }

            return l;
        }

        public static double[] Solve(double[][] matrix, double[] rightHandSide)
        {
            var l = Cholesky(matrix);

            return l == null ? null : SolveWith(l, rightHandSide);
        }

        public static double[][] Inverse(double[][] matrix)
        {
            var l = Cholesky(matrix);

            if (l == null)
            {
                return null;
            }

            var n = matrix.Length;
            var inverse = new double[n][];

            for (var i = 0; i < n; i++)
            {
                inverse[i] = new double[n];
            }

            for (var column = 0; column < n; column++)
            {
                var unit = new double[n];
                unit[column] = 1d;
                var solved = SolveWith(l, unit);

                for (var row = 0; row < n; row++)
                {
                    inverse[row][column] = solved[row];
                }
            }

            for (var i = 0; i < n; i++)
            for (var j = 0; j < i; j++)
            {
                var mean = (inverse[i][j] + inverse[j][i]) / 2d;
                inverse[i][j] = mean;
                inverse[j][i] = mean;
            }

            return inverse;
        }

        private static double[] SolveWith(double[][] l, double[] rightHandSide)
        {
            var n = l.Length;
            var y = new double[n];

            for (var i = 0; i < n; i++)
            {
                var sum = rightHandSide[i];

                for (var k = 0; k < i; k++)
                {
                    sum -= l[i][k] * y[k];
                }

                y[i] = sum / l[i][i];
            }

            var x = new double[n];

            for (var i = n - 1; i >= 0; i--)
            {
                var sum = y[i];

                for (var k = i + 1; k < n; k++)
                {
                    sum -= l[k][i] * x[k];
                }

                x[i] = sum / l[i][i];
            }

            return x;
        }
    }
}