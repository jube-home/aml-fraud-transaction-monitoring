// Accord Math Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © César Souza, 2009-2017
// cesarsouza at gmail.com
//
// Copyright © John Burkardt, 2012
// http://people.sc.fsu.edu/~jburkardt/
//
//    This library is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 2.1 of the License, or (at your option) any later version.
//
//    This library is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public
//    License along with this library; if not, write to the Free Software
//    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//

namespace Accord.Math
{
    public static partial class OwensT
    {
        private const double rrtpi = 0.39894228040143267794;
        private const double rtwopi = 0.15915494309189533577;

        private static int SelectCode(double h, double a)
        {
            int i;

            var ihint =
                /*
              Determine appropriate method from t1...t6
            */
                15;

            for (i = 1; i <= 14; i++)
            {
                if (h <= hrange[i - 1])
                {
                    ihint = i;
                    break;
                }
            }

            var iaint = 8;

            for (i = 1; i <= 7; i++)
            {
                if (a <= arange[i - 1])
                {
                    iaint = i;
                    break;
                }
            }

            var icode = select[ihint - 1 + (iaint - 1) * 15];
            return icode;
        }

        private static double SeriesT1(double h, double a, int m)
        {
            /*
              t1(h, a, m) ; m = 2, 3, 4, 5, 7, 10, 12 or 18
              jj = 2j - 1 ; gj = exp(-h*h/2) * (-h*h/2)**j / j
              aj = a**(2j-1) / (2*pi)
            */
            var hs = -0.5 * h * h;
            var dhs = System.Math.Exp(hs);
            var AS = a * a;
            var j = 1;
            var jj = 1;
            var aj = rtwopi * a;
            var value = rtwopi * System.Math.Atan(a);
            var dj = dhs - 1.0;
            var gj = hs * dhs;

            for (;;)
            {
                value = value + dj * aj / jj;

                if (m <= j)
                {
                    return value;
                }

                j = j + 1;
                jj = jj + 2;
                aj = aj * AS;
                dj = gj - dj;
                gj = gj * hs / j;
            }
        }

        private static double SeriesT2(double h, double a, double ah, int m)
        {
            /*
            t2(h, a, m) ; m = 10, 20 or 30
            z = (-1)^(i-1) * zi ; ii = 2i - 1
            vi = (-1)^(i-1) * a^(2i-1) * exp[-(a*h)^2/2] / sqrt(2*pi)
          */
            var maxii = m + m + 1;
            var ii = 1;
            var value = 0.0;
            var hs = h * h;
            var AS = -a * a;
            var vi = rrtpi * a * System.Math.Exp(-0.5 * ah * ah);
            var z = 0.5 * (-0.5 + Normal.Function(ah)) / h;
            var y = 1.0 / hs;

            for (;;)
            {
                value = value + z;

                if (maxii <= ii)
                {
                    value = value * rrtpi * System.Math.Exp(-0.5 * hs);
                    return value;
                }

                z = y * (vi - ii * z);
                vi = AS * vi;
                ii = ii + 2;
            }
        }

        private static double SeriesT3(double h, double a, double ah, int m)
        {
            /*
              t3(h, a, m) ; m = 20
              ii = 2i - 1
              vi = a**(2i-1) * exp[-(a*h)**2/2] / sqrt(2*pi)
            */
            var i = 1;
            var ii = 1;
            var value = 0.0;
            var hs = h * h;
            var AS = a * a;
            var vi = rrtpi * a * System.Math.Exp(-0.5 * ah * ah);
            var zi = 0.5 * (-0.5 + Normal.Function(ah)) / h;
            var y = 1.0 / hs;

            for (;;)
            {
                value = value + zi * c2[i - 1];

                if (m < i)
                {
                    value = value * rrtpi * System.Math.Exp(-0.5 * hs);
                    return value;
                }

                zi = y * (ii * zi - vi);
                vi = AS * vi;
                i = i + 1;
                ii = ii + 2;
            }
        }

        private static double SeriesT4(double h, double a, int m)
        {
            /*
              t4(h, a, m) ; m = 4, 7, 8 or 20;  ii = 2i + 1
              ai = a * exp[-h*h*(1+a*a)/2] * (-a*a)**i / (2*pi)
            */
            var maxii = m + m + 1;
            var ii = 1;
            var hs = h * h;
            var AS = -a * a;
            var value = 0.0;
            var ai = rtwopi * a * System.Math.Exp(-0.5 * hs * (1.0 - AS));
            var yi = 1.0;

            for (;;)
            {
                value = value + ai * yi;

                if (maxii <= ii)
                {
                    return value;
                }

                ii = ii + 2;
                yi = (1.0 - hs * yi) / ii;
                ai = ai * AS;
            }
        }

        private static double QuadratureT5(double h, double a, int m)
        {
            var value =
                /*
              t5(h, a, m) ; m = 13
              2m - point gaussian quadrature
            */
                0.0;
            var AS = a * a;
            var hs = -0.5 * h * h;
            for (var i = 1; i <= m; i++)
            {
                var r = 1.0 + AS * pts[i - 1];
                value = value + wts[i - 1] * System.Math.Exp(hs * r) / r;
            }

            value = a * value;

            return value;
        }

        private static double ApproximationT6(double h, double a)
        {
            /*
              t6(h, a);  approximation for a near 1, (a<=1)
            */
            var normh = Normal.Complemented(h);
            var value = 0.5 * normh * (1.0 - normh);
            var y = 1.0 - a;
            var r = System.Math.Atan(y / (1.0 + a));

            if (r != 0.0)
            {
                value = value - rtwopi * r * System.Math.Exp(-0.5 * y * h * h / r);
            }

            return value;
        }
    }
}