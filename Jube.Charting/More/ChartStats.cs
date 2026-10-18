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
    public static class ChartStats
    {
        private static readonly double[] lanczosCoefficients =
        [
            676.5203681218851, -1259.1392167224028, 771.32342877765313, -176.61502916214059,
            12.507343278686905, -0.13857109526572012, 9.9843695780195716e-6, 1.5056327351493116e-7
        ];

        public static double NormalQuantile(double p)
        {
            if (!(p is > 0 and < 1))
            {
                throw new ChartSpecificationException("A probability must be strictly between 0 and 1.");
            }

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

            const double low = 0.02425;
            double x;

            if (p < low)
            {
                var q = Math.Sqrt(-2 * Math.Log(p));

                x = (((((c[0] * q + c[1]) * q + c[2]) * q + c[3]) * q + c[4]) * q + c[5]) /
                    ((((d[0] * q + d[1]) * q + d[2]) * q + d[3]) * q + 1);
            }
            else if (p > 1 - low)
            {
                var q = Math.Sqrt(-2 * Math.Log(1 - p));

                x = -(((((c[0] * q + c[1]) * q + c[2]) * q + c[3]) * q + c[4]) * q + c[5]) /
                    ((((d[0] * q + d[1]) * q + d[2]) * q + d[3]) * q + 1);
            }
            else
            {
                var q = p - 0.5;
                var r = q * q;

                x = (((((a[0] * r + a[1]) * r + a[2]) * r + a[3]) * r + a[4]) * r + a[5]) * q /
                    (((((b[0] * r + b[1]) * r + b[2]) * r + b[3]) * r + b[4]) * r + 1);
            }

            return x;
        }

        public static double Pearson(IReadOnlyList<double> xs, IReadOnlyList<double> ys, out int count)
        {
            var n = 0;
            var mx = 0.0;
            var my = 0.0;

            for (var i = 0; i < Math.Min(xs.Count, ys.Count); i++)
            {
                if (double.IsFinite(xs[i]) && double.IsFinite(ys[i]))
                {
                    n++;
                    mx += xs[i];
                    my += ys[i];
                }
            }

            count = n;

            if (n < 2)
            {
                return double.NaN;
            }

            mx /= n;
            my /= n;
            double sxx = 0, syy = 0, sxy = 0;

            for (var i = 0; i < Math.Min(xs.Count, ys.Count); i++)
            {
                if (double.IsFinite(xs[i]) && double.IsFinite(ys[i]))
                {
                    sxx += (xs[i] - mx) * (xs[i] - mx);
                    syy += (ys[i] - my) * (ys[i] - my);
                    sxy += (xs[i] - mx) * (ys[i] - my);
                }
            }

            return sxx > 0 && syy > 0 ? Math.Clamp(sxy / Math.Sqrt(sxx * syy), -1, 1) : double.NaN;
        }

        public static double LogGamma(double x)
        {
            if (x < 0.5)
            {
                return Math.Log(Math.PI / Math.Abs(Math.Sin(Math.PI * x))) - LogGamma(1 - x);
            }

            x -= 1;
            var sum = 0.99999999999980993;

            for (var i = 0; i < lanczosCoefficients.Length; i++)
            {
                sum += lanczosCoefficients[i] / (x + i + 1);
            }

            var t = x + lanczosCoefficients.Length - 0.5;

            return 0.5 * Math.Log(2 * Math.PI) + (x + 0.5) * Math.Log(t) - t + Math.Log(sum);
        }

        public static double RegularizedGammaQ(double a, double x)
        {
            if (!(a > 0) || x < 0)
            {
                throw new ChartSpecificationException(
                    "Gamma arguments must be a positive shape and a non-negative value.");
            }

            if (x == 0)
            {
                return 1;
            }

            if (x < a + 1)
            {
                var ap = a;
                var sum = 1 / a;
                var del = sum;

                for (var n = 0; n < 500; n++)
                {
                    ap += 1;
                    del *= x / ap;
                    sum += del;

                    if (Math.Abs(del) < Math.Abs(sum) * 1e-15)
                    {
                        break;
                    }
                }

                return 1 - sum * Math.Exp(-x + a * Math.Log(x) - LogGamma(a));
            }

            const double tiny = 1e-300;
            var bb = x + 1 - a;
            var c = 1 / tiny;
            var dd = 1 / bb;
            var h = dd;

            for (var i = 1; i < 500; i++)
            {
                var an = -i * (i - a);
                bb += 2;
                dd = an * dd + bb;

                if (Math.Abs(dd) < tiny)
                {
                    dd = tiny;
                }

                c = bb + an / c;

                if (Math.Abs(c) < tiny)
                {
                    c = tiny;
                }

                dd = 1 / dd;
                var del = dd * c;
                h *= del;

                if (Math.Abs(del - 1) < 1e-15)
                {
                    break;
                }
            }

            return Math.Exp(-x + a * Math.Log(x) - LogGamma(a)) * h;
        }

        public static double ChiSquareSurvival(double statistic, double degreesOfFreedom)
        {
            return RegularizedGammaQ(degreesOfFreedom / 2, Math.Max(statistic, 0) / 2);
        }

        public static double RegularizedBeta(double x, double a, double b)
        {
            if (x <= 0)
            {
                return 0;
            }

            if (x >= 1)
            {
                return 1;
            }

            var front = Math.Exp(LogGamma(a + b) - LogGamma(a) - LogGamma(b) + a * Math.Log(x) + b * Math.Log(1 - x));

            return x < (a + 1) / (a + b + 2)
                ? front * BetaContinuedFraction(x, a, b) / a
                : 1 - front * BetaContinuedFraction(1 - x, b, a) / b;
        }

        private static double BetaContinuedFraction(double x, double a, double b)
        {
            const double tiny = 1e-300;
            var qab = a + b;
            var qap = a + 1;
            var qam = a - 1;
            var c = 1.0;
            var d = 1 - qab * x / qap;

            if (Math.Abs(d) < tiny)
            {
                d = tiny;
            }

            d = 1 / d;
            var h = d;

            for (var m = 1; m <= 500; m++)
            {
                var m2 = 2 * m;
                var aa = m * (b - m) * x / ((qam + m2) * (a + m2));
                d = 1 + aa * d;

                if (Math.Abs(d) < tiny)
                {
                    d = tiny;
                }

                c = 1 + aa / c;

                if (Math.Abs(c) < tiny)
                {
                    c = tiny;
                }

                d = 1 / d;
                h *= d * c;
                aa = -(a + m) * (qab + m) * x / ((a + m2) * (qap + m2));
                d = 1 + aa * d;

                if (Math.Abs(d) < tiny)
                {
                    d = tiny;
                }

                c = 1 + aa / c;

                if (Math.Abs(c) < tiny)
                {
                    c = tiny;
                }

                d = 1 / d;
                var del = d * c;
                h *= del;

                if (Math.Abs(del - 1) < 1e-15)
                {
                    break;
                }
            }

            return h;
        }

        public static double StudentTCdf(double t, double degreesOfFreedom)
        {
            var x = degreesOfFreedom / (degreesOfFreedom + t * t);
            var tail = 0.5 * RegularizedBeta(x, degreesOfFreedom / 2, 0.5);

            return t > 0 ? 1 - tail : tail;
        }

        public static double StudentTQuantile(double p, double degreesOfFreedom)
        {
            if (!(p is > 0 and < 1) || !(degreesOfFreedom > 0))
            {
                throw new ChartSpecificationException(
                    "Student t quantile needs a probability in (0,1) and positive degrees of freedom.");
            }

            if (degreesOfFreedom > 1e7)
            {
                return NormalQuantile(p);
            }

            var lo = -1.0;
            var hi = 1.0;

            while (StudentTCdf(lo, degreesOfFreedom) > p)
            {
                lo *= 2;
            }

            while (StudentTCdf(hi, degreesOfFreedom) < p)
            {
                hi *= 2;
            }

            for (var i = 0; i < 200; i++)
            {
                var mid = (lo + hi) / 2;

                if (StudentTCdf(mid, degreesOfFreedom) < p)
                {
                    lo = mid;
                }
                else
                {
                    hi = mid;
                }

                if (hi - lo < 1e-13 * Math.Max(1, Math.Abs(mid)))
                {
                    break;
                }
            }

            return (lo + hi) / 2;
        }
    }
}