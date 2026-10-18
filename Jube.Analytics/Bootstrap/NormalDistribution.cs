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

namespace Jube.Analytics.Bootstrap
{
    internal static class NormalDistribution
    {
        public static double Cdf(double x)
        {
            return 0.5 * Erfc(-x / Math.Sqrt(2d));
        }

        public static double InverseCdf(double p)
        {
            if (p <= 0d)
            {
                return double.NegativeInfinity;
            }

            if (p >= 1d)
            {
                return double.PositiveInfinity;
            }

            const double lowBreak = 0.02425;
            const double highBreak = 1d - lowBreak;

            double[] a =
            [
                -3.969683028665376e+01, 2.209460984245205e+02, -2.759285104469687e+02, 1.383577518672690e+02,
                -3.066479806614716e+01, 2.506628277459239e+00
            ];

            double[] b =
            [
                -5.447609879822406e+01, 1.615858368580409e+02, -1.556989798598866e+02, 6.680131188771972e+01,
                -1.328068155288572e+01
            ];

            double[] c =
            [
                -7.784894002430293e-03, -3.223964580411365e-01, -2.400758277161838e+00, -2.549732539343734e+00,
                4.374664141464968e+00, 2.938163982698783e+00
            ];

            double[] d =
            [
                7.784695709041462e-03, 3.224671290700398e-01, 2.445134137142996e+00, 3.754408661907416e+00
            ];

            double x;

            if (p < lowBreak)
            {
                var q = Math.Sqrt(-2d * Math.Log(p));

                x = (((((c[0] * q + c[1]) * q + c[2]) * q + c[3]) * q + c[4]) * q + c[5]) /
                    ((((d[0] * q + d[1]) * q + d[2]) * q + d[3]) * q + 1d);
            }
            else if (p <= highBreak)
            {
                var q = p - 0.5;
                var r = q * q;

                x = (((((a[0] * r + a[1]) * r + a[2]) * r + a[3]) * r + a[4]) * r + a[5]) * q /
                    (((((b[0] * r + b[1]) * r + b[2]) * r + b[3]) * r + b[4]) * r + 1d);
            }
            else
            {
                var q = Math.Sqrt(-2d * Math.Log(1d - p));

                x = -(((((c[0] * q + c[1]) * q + c[2]) * q + c[3]) * q + c[4]) * q + c[5]) /
                    ((((d[0] * q + d[1]) * q + d[2]) * q + d[3]) * q + 1d);
            }

            var e = 0.5 * Erfc(-x / Math.Sqrt(2d)) - p;
            var u = e * Math.Sqrt(2d * Math.PI) * Math.Exp(x * x / 2d);

            return x - u / (1d + x * u / 2d);
        }

        private static double Erfc(double x)
        {
            var z = Math.Abs(x);
            var t = 1d / (1d + z / 2d);

            var result = t * Math.Exp(-z * z - 1.26551223 + t * (1.00002368 + t * (0.37409196 + t * (0.09678418 +
                t * (-0.18628806 + t * (0.27886807 + t * (-1.13520398 + t * (1.48851587 + t * (-0.82215223 +
                    t * 0.17087277)))))))));

            return x >= 0 ? result : 2d - result;
        }
    }
}