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

namespace Jube.Analytics.Anomaly
{
    internal static class DenseLinearAlgebra
    {
        public static double[] Mean(double[][] data)
        {
            var d = data[0].Length;
            var mean = new double[d];

            foreach (var row in data)
            {
                for (var j = 0; j < d; j++)
                {
                    mean[j] += row[j];
                }
            }

            for (var j = 0; j < d; j++)
            {
                mean[j] /= data.Length;
            }

            return mean;
        }

        public static double[,] Covariance(double[][] data, double[] mean)
        {
            var d = mean.Length;
            var n = data.Length;
            var s = new double[d, d];
            var centred = new double[d];

            foreach (var row in data)
            {
                for (var j = 0; j < d; j++)
                {
                    centred[j] = row[j] - mean[j];
                }

                for (var a = 0; a < d; a++)
                {
                    var ca = centred[a];

                    if (ca == 0d)
                    {
                        continue;
                    }

                    for (var b = a; b < d; b++)
                    {
                        s[a, b] += ca * centred[b];
                    }
                }
            }

            for (var a = 0; a < d; a++)
            {
                for (var b = a; b < d; b++)
                {
                    s[a, b] /= n;
                    s[b, a] = s[a, b];
                }
            }

            return s;
        }

        public static double[,]? Cholesky(double[,] a)
        {
            var d = a.GetLength(0);
            var l = new double[d, d];

            for (var i = 0; i < d; i++)
            {
                for (var j = 0; j <= i; j++)
                {
                    var sum = a[i, j];

                    for (var k = 0; k < j; k++)
                    {
                        sum -= l[i, k] * l[j, k];
                    }

                    if (i == j)
                    {
                        if (!(sum > 1e-12))
                        {
                            return null;
                        }

                        l[i, i] = Math.Sqrt(sum);
                    }
                    else
                    {
                        l[i, j] = sum / l[j, j];
                    }
                }
            }

            return l;
        }

        public static double LogDeterminant(double[,] l)
        {
            var sum = 0d;

            for (var i = 0; i < l.GetLength(0); i++)
            {
                sum += 2d * Math.Log(l[i, i]);
            }

            return sum;
        }

        public static double SquaredMahalanobis(double[,] l, double[] x, double[] mean)
        {
            var d = mean.Length;
            var y = new double[d];
            var total = 0d;

            for (var i = 0; i < d; i++)
            {
                var sum = x[i] - mean[i];

                for (var k = 0; k < i; k++)
                {
                    sum -= l[i, k] * y[k];
                }

                y[i] = sum / l[i, i];
                total += y[i] * y[i];
            }

            return total;
        }

        public static (double[] Values, double[][] Vectors) SymmetricEigen(double[,] input)
        {
            var d = input.GetLength(0);
            var a = (double[,])input.Clone();
            var v = new double[d, d];

            for (var i = 0; i < d; i++)
            {
                v[i, i] = 1d;
            }

            for (var sweep = 0; sweep < 60; sweep++)
            {
                var off = 0d;

                for (var p = 0; p < d - 1; p++)
                {
                    for (var q = p + 1; q < d; q++)
                    {
                        off += a[p, q] * a[p, q];
                    }
                }

                if (off < 1e-22)
                {
                    break;
                }

                for (var p = 0; p < d - 1; p++)
                {
                    for (var q = p + 1; q < d; q++)
                    {
                        if (Math.Abs(a[p, q]) < 1e-300)
                        {
                            continue;
                        }

                        var theta = (a[q, q] - a[p, p]) / (2d * a[p, q]);
                        var t = Math.Sign(theta == 0 ? 1 : theta) / (Math.Abs(theta) + Math.Sqrt(theta * theta + 1d));
                        var c = 1d / Math.Sqrt(t * t + 1d);
                        var s = t * c;

                        for (var k = 0; k < d; k++)
                        {
                            var akp = a[k, p];
                            var akq = a[k, q];
                            a[k, p] = c * akp - s * akq;
                            a[k, q] = s * akp + c * akq;
                        }

                        for (var k = 0; k < d; k++)
                        {
                            var apk = a[p, k];
                            var aqk = a[q, k];
                            a[p, k] = c * apk - s * aqk;
                            a[q, k] = s * apk + c * aqk;
                        }

                        for (var k = 0; k < d; k++)
                        {
                            var vkp = v[k, p];
                            var vkq = v[k, q];
                            v[k, p] = c * vkp - s * vkq;
                            v[k, q] = s * vkp + c * vkq;
                        }
                    }
                }
            }

            var order = Enumerable.Range(0, d).OrderByDescending(i => a[i, i]).ToArray();
            var values = new double[d];
            var vectors = new double[d][];

            for (var r = 0; r < d; r++)
            {
                values[r] = a[order[r], order[r]];
                vectors[r] = new double[d];

                for (var k = 0; k < d; k++)
                {
                    vectors[r][k] = v[k, order[r]];
                }
            }

            return (values, vectors);
        }

        public static double SquaredDistance(double[] a, double[] b)
        {
            var sum = 0d;

            for (var i = 0; i < a.Length; i++)
            {
                var diff = a[i] - b[i];
                sum += diff * diff;
            }

            return sum;
        }
    }
}