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

namespace Jube.Service.ModelDiagrams.Adapters
{
    internal static class SnapshotMatrix
    {
        public static double[][] Square(double[][]? matrix, int size, string what)
        {
            if (matrix is null || !ModelJson.IsRectangular(matrix, size, size) ||
                matrix.Any(r => r.Any(v => !double.IsFinite(v))))
            {
                throw ModelJson.Inconsistent(what + " must be a finite " + size + " by " + size + " matrix");
            }

            return matrix;
        }

        public static double[][]? Invert(double[][] matrix)
        {
            var n = matrix.Length;
            var a = matrix.Select(r => r.ToArray()).ToArray();

            var inverse = Enumerable.Range(0, n).Select(i =>
            {
                var row = new double[n];
                row[i] = 1;

                return row;
            }).ToArray();

            var scale = Math.Max(matrix.Max(r => r.Max(Math.Abs)), 1e-300);

            for (var c = 0; c < n; c++)
            {
                var pivot = c;

                for (var r = c + 1; r < n; r++)
                {
                    if (Math.Abs(a[r][c]) > Math.Abs(a[pivot][c]))
                    {
                        pivot = r;
                    }
                }

                if (Math.Abs(a[pivot][c]) <= scale * 1e-13)
                {
                    return null;
                }

                (a[c], a[pivot]) = (a[pivot], a[c]);
                (inverse[c], inverse[pivot]) = (inverse[pivot], inverse[c]);
                var divisor = a[c][c];

                for (var j = 0; j < n; j++)
                {
                    a[c][j] /= divisor;
                    inverse[c][j] /= divisor;
                }

                for (var r = 0; r < n; r++)
                {
                    if (r == c || a[r][c] == 0)
                    {
                        continue;
                    }

                    var factor = a[r][c];

                    for (var j = 0; j < n; j++)
                    {
                        a[r][j] -= factor * a[c][j];
                        inverse[r][j] -= factor * inverse[c][j];
                    }
                }
            }

            return inverse.Any(r => r.Any(v => !double.IsFinite(v))) ? null : inverse;
        }

        public static double[][] Correlation(double[][] covariance)
        {
            var n = covariance.Length;
            var result = new double[n][];

            for (var i = 0; i < n; i++)
            {
                result[i] = new double[n];

                for (var j = 0; j < n; j++)
                {
                    var denominator = Math.Sqrt(covariance[i][i] * covariance[j][j]);
                    result[i][j] = i == j ? 1 : denominator > 0 ? Math.Clamp(covariance[i][j] / denominator, -1, 1) : 0;
                }
            }

            return result;
        }

        public static double[][] PartialCorrelation(double[][] precision)
        {
            var n = precision.Length;
            var result = new double[n][];

            for (var i = 0; i < n; i++)
            {
                result[i] = new double[n];

                for (var j = 0; j < n; j++)
                {
                    var denominator = Math.Sqrt(precision[i][i] * precision[j][j]);
                    result[i][j] = i == j ? 1 : denominator > 0 ? Math.Clamp(-precision[i][j] / denominator, -1, 1) : 0;
                }
            }

            return result;
        }

        public static int[] ActiveInputs(int[]? active, int inputCount, int expected)
        {
            var result = active ?? Enumerable.Range(0, inputCount).ToArray();

            if (result.Length != expected || result.Any(i => i < 0 || i >= inputCount) ||
                result.Distinct().Count() != result.Length)
            {
                throw ModelJson.Inconsistent("the active inputs must be " + expected + " distinct variable indexes");
            }

            return result;
        }
    }
}