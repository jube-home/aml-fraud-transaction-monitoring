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

namespace Jube.Charting.Ticks
{
    public static class ExtendedTicks
    {
        private const double Eps = 1e-10;
        private static readonly double[] q = [1, 5, 2, 2.5, 4, 3];
        private static readonly double[] w = [0.25, 0.2, 0.5, 0.05];

        public static IReadOnlyList<double> Generate(double min, double max, int target = 5, bool loose = false)
        {
            if (!double.IsFinite(min) || !double.IsFinite(max))
            {
                throw new ChartSpecificationException("Tick generation needs finite bounds.");
            }

            if (min > max)
            {
                (min, max) = (max, min);
            }

            target = Math.Clamp(target, 2, 40);

            if (max - min < Eps * Math.Max(1, Math.Abs(max)))
            {
                return [min];
            }

            var bestScore = -2.0;
            double bestMin = min, bestMax = max, bestStep = max - min;
            var found = false;
            var j = 1;
            var stop = false;

            while (!stop && j < 50)
            {
                for (var qi = 0; qi < q.Length; qi++)
                {
                    var d = ExtendedTicks.q[qi];
                    var sm = SimplicityMax(qi, j);

                    if (w[0] * sm + w[1] + w[2] + w[3] < bestScore)
                    {
                        stop = true;
                        break;
                    }

                    var k = 2;

                    while (k < 200)
                    {
                        var dm = DensityMax(k, target);

                        if (w[0] * sm + w[1] + w[2] * dm + w[3] < bestScore)
                        {
                            break;
                        }

                        var delta = (max - min) / (k + 1) / j / d;
                        var z = Math.Ceiling(Math.Log10(delta));

                        while (z < 300)
                        {
                            var step = j * d * Math.Pow(10, z);
                            var cm = CoverageMax(min, max, step * (k - 1));

                            if (w[0] * sm + w[1] * cm + w[2] * dm + w[3] < bestScore)
                            {
                                break;
                            }

                            var minStart = Math.Floor(max / step) * j - (k - 1) * j;
                            var maxStart = Math.Ceiling(min / step) * j;

                            if (minStart <= maxStart)
                            {
                                for (var start = minStart; start <= maxStart; start++)
                                {
                                    var lmin = start * (step / j);
                                    var lmax = lmin + step * (k - 1);

                                    var score = w[0] * Simplicity(qi, j, lmin, lmax, step) +
                                                w[1] * Coverage(min, max, lmin, lmax) +
                                                w[2] * Density(k, target, min, max, lmin, lmax) + w[3];

                                    if (score > bestScore && (!loose || (lmin <= min + Eps && lmax >= max - Eps)))
                                    {
                                        bestScore = score;
                                        bestMin = lmin;
                                        bestMax = lmax;
                                        bestStep = step;
                                        found = true;
                                    }
                                }
                            }

                            z++;
                        }

                        k++;
                    }
                }

                j++;
            }

            if (!found)
            {
                return [min, max];
            }

            var count = (int)Math.Round((bestMax - bestMin) / bestStep) + 1;
            var result = new double[count];

            for (var i = 0; i < count; i++)
            {
                result[i] = Clean(bestMin + i * bestStep, bestStep);
            }

            return result;
        }

        private static double Clean(double value, double step)
        {
            var digits = Math.Clamp((int)Math.Ceiling(-Math.Log10(step)) + 6, 0, 12);
            var rounded = Math.Round(value, digits);

            return rounded == 0 ? 0 : rounded;
        }

        private static double Simplicity(int qi, int j, double lmin, double lmax, double lstep)
        {
            var n = q.Length;
            var v = 0.0;
            var remainder = lmin - Math.Floor(lmin / lstep) * lstep;

            if ((remainder < Eps || lstep - remainder < Eps) && lmin <= 0 && lmax >= 0)
            {
                v = 1;
            }

            return 1 - (double)qi / (n - 1) - j + v;
        }

        private static double SimplicityMax(int qi, int j)
        {
            return 1 - (double)qi / (q.Length - 1) - j + 1;
        }

        private static double Coverage(double dmin, double dmax, double lmin, double lmax)
        {
            var range = dmax - dmin;

            return 1 - 0.5 * (Math.Pow(dmax - lmax, 2) + Math.Pow(dmin - lmin, 2)) / Math.Pow(0.1 * range, 2);
        }

        private static double CoverageMax(double dmin, double dmax, double span)
        {
            var range = dmax - dmin;

            if (span > range)
            {
                var half = (span - range) / 2;

                return 1 - 0.5 * (half * half + half * half) / Math.Pow(0.1 * range, 2);
            }

            return 1;
        }

        private static double Density(int k, int m, double dmin, double dmax, double lmin, double lmax)
        {
            var r = (k - 1) / (lmax - lmin);
            var rt = (m - 1) / (Math.Max(lmax, dmax) - Math.Min(dmin, lmin));

            return 2 - Math.Max(r / rt, rt / r);
        }

        private static double DensityMax(int k, int m)
        {
            return k >= m ? 2 - (double)(k - 1) / (m - 1) : 1;
        }
    }
}