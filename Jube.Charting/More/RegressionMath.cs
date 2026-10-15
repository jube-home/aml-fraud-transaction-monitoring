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
using Jube.Charting.More.Models;

namespace Jube.Charting.More
{
    public static class RegressionMath
    {
        public static RegressionFit Fit(IReadOnlyList<double> xs, IReadOnlyList<double> ys, int degree = 1,
            double confidence = 0.95)
        {
            if (xs.Count != ys.Count)
            {
                throw new ChartSpecificationException("X and Y values must have the same length.");
            }

            if (degree is < 1 or > 3)
            {
                throw new ChartSpecificationException("Regression degree must be 1, 2 or 3.");
            }

            if (!(confidence is > 0 and < 1))
            {
                throw new ChartSpecificationException("The confidence level must be strictly between 0 and 1.");
            }

            var px = new List<double>();
            var py = new List<double>();

            for (var i = 0; i < xs.Count; i++)
            {
                if (double.IsFinite(xs[i]) && double.IsFinite(ys[i]))
                {
                    px.Add(xs[i]);
                    py.Add(ys[i]);
                }
            }

            var n = px.Count;
            var p = degree + 1;

            if (n <= p)
            {
                throw new ChartSpecificationException("A degree " + degree + " regression needs more than " + p +
                                                      " finite points.");
            }

            var center = px.Average();
            var scale = Math.Sqrt(px.Sum(x => (x - center) * (x - center)) / n);

            if (!(scale > 0))
            {
                throw new ChartSpecificationException("Regression needs at least two distinct x values.");
            }

            var gram = new double[p, p];
            var moment = new double[p];

            for (var k = 0; k < n; k++)
            {
                var u = (px[k] - center) / scale;
                var basis = new double[p];
                basis[0] = 1;

                for (var i = 1; i < p; i++)
                {
                    basis[i] = basis[i - 1] * u;
                }

                for (var i = 0; i < p; i++)
                {
                    moment[i] += basis[i] * py[k];

                    for (var j = 0; j < p; j++)
                    {
                        gram[i, j] += basis[i] * basis[j];
                    }
                }
            }

            var inverse = Invert(gram, p);
            var beta = new double[p];

            for (var i = 0; i < p; i++)
            {
                for (var j = 0; j < p; j++)
                {
                    beta[i] += inverse[i, j] * moment[j];
                }
            }

            var mean = py.Average();
            var rss = 0.0;
            var tss = 0.0;

            for (var k = 0; k < n; k++)
            {
                var u = (px[k] - center) / scale;
                var fitted = 0.0;

                for (var i = p - 1; i >= 0; i--)
                {
                    fitted = fitted * u + beta[i];
                }

                rss += (py[k] - fitted) * (py[k] - fitted);
                tss += (py[k] - mean) * (py[k] - mean);
            }

            var df = n - p;

            return new RegressionFit(beta, tss > 0 ? 1 - rss / tss : 1, Math.Sqrt(rss / df), df, n, confidence, center,
                scale, inverse);
        }

        private static double[,] Invert(double[,] matrix, int n)
        {
            var a = new double[n, 2 * n];

            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < n; j++)
                {
                    a[i, j] = matrix[i, j];
                }

                a[i, n + i] = 1;
            }

            for (var col = 0; col < n; col++)
            {
                var pivot = col;

                for (var r = col + 1; r < n; r++)
                {
                    if (Math.Abs(a[r, col]) > Math.Abs(a[pivot, col]))
                    {
                        pivot = r;
                    }
                }

                if (Math.Abs(a[pivot, col]) < 1e-12)
                {
                    throw new ChartSpecificationException("The regression design is singular.");
                }

                if (pivot != col)
                {
                    for (var j = 0; j < 2 * n; j++)
                    {
                        (a[col, j], a[pivot, j]) = (a[pivot, j], a[col, j]);
                    }
                }

                var d = a[col, col];

                for (var j = 0; j < 2 * n; j++)
                {
                    a[col, j] /= d;
                }

                for (var r = 0; r < n; r++)
                {
                    if (r == col)
                    {
                        continue;
                    }

                    var f = a[r, col];

                    for (var j = 0; j < 2 * n; j++)
                    {
                        a[r, j] -= f * a[col, j];
                    }
                }
            }

            var result = new double[n, n];

            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < n; j++)
                {
                    result[i, j] = a[i, n + j];
                }
            }

            return result;
        }
    }
}