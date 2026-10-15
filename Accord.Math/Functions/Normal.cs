// Accord Math Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © César Souza, 2009-2017
// cesarsouza at gmail.com
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

// Contains functions from the Cephes Math Library Release 2.8:
// June, 2000 Copyright 1984, 1987, 1988, 2000 by Stephen L. Moshier
//
// Original license is listed below:
//
//   Some software in this archive may be from the book _Methods and
// Programs for Mathematical Functions_ (Prentice-Hall or Simon & Schuster
// International, 1989) or from the Cephes Mathematical Library, a
// commercial product. In either event, it is copyrighted by the author.
// What you see here may be used freely but it comes with no support or
// guarantee.
//
//   The two known misprints in the book are repaired here in the
// source listings for the gamma function and the incomplete beta
// integral.
//
//
//   Stephen L. Moshier
//   moshier@na-net.ornl.gov
//

// Contains functions from the TVPACK Fortran routines,
// Copyright (C) 2013, Alan Genz, under the BSD license.
// See functions below for more details.

using System;

namespace Accord.Math
{
    /// <summary>
    ///     Normal distribution functions.
    /// </summary>
    /// <remarks>
    ///     References:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 Cephes Math Library, http://www.netlib.org/cephes/
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 George Marsaglia, Evaluating the Normal Distribution, 2004.
    ///                 Available in: http://www.jstatsoft.org/v11/a05/paper
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The following example shows the normal usages for the Normal functions:
    ///     </para>
    ///     <code>
    ///   // Compute standard precision functions
    ///   double phi  = Normal.Function(0.42);     //  0.66275727315175048
    ///   double phic = Normal.Complemented(0.42); //  0.33724272684824952
    ///   double inv  = Normal.Inverse(0.42);      // -0.20189347914185085
    ///   
    ///   // Compute at the limits
    ///   double phi  = Normal.Function(16.6);     //  1.0
    ///   double phic = Normal.Complemented(16.6); //  3.4845465199504055E-62
    /// </code>
    /// </example>
    public static class Normal
    {
        private static readonly double[] inverse_P0 =
        {
            -59.963350101410789,
            98.001075418599967,
            -56.676285746907027,
            13.931260938727968,
            -1.2391658386738125
        };

        private static readonly double[] inverse_Q0 =
        {
            1.9544885833814176,
            4.6762791289888153,
            86.360242139089053,
            -225.46268785411937,
            200.26021238006066,
            -82.037225616833339,
            15.90562251262117,
            -1.1833162112133
        };

        private static readonly double[] inverse_P1 =
        {
            4.0554489230596245,
            31.525109459989388,
            57.162819224642128,
            44.080507389320083,
            14.684956192885803,
            2.1866330685079025,
            -0.14025607917135449,
            -0.035042462682784818,
            -0.00085745678515468545
        };

        private static readonly double[] inverse_Q1 =
        {
            15.779988325646675,
            45.390763512887922,
            41.317203825467203,
            15.04253856929075,
            2.5046494620830941,
            -0.14218292285478779,
            -0.038080640769157827,
            -0.00093325948089545744
        };

        private static readonly double[] inverse_P2 =
        {
            3.2377489177694603,
            6.9152288906898418,
            3.9388102529247444,
            1.3330346081580755,
            0.20148538954917908,
            0.012371663481782003,
            0.00030158155350823543,
            2.6580697468673755E-06,
            6.2397453918498331E-09
        };

        private static readonly double[] inverse_Q2 =
        {
            6.02427039364742,
            3.6798356385616087,
            1.3770209948908132,
            0.21623699359449663,
            0.013420400608854318,
            0.00032801446468212774,
            2.8924786474538068E-06,
            6.7901940800998127E-09
        };

        private static readonly double[] high_R =
        {
            1.25331413731550025, 0.421369229288054473, 0.236652382913560671,
            0.162377660896867462, 0.123131963257932296, 0.0990285964717319214,
            0.0827662865013691773, 0.0710695805388521071, 0.0622586659950261958
        };

        private static readonly double[] BVND_WN20 =
        {
            0.01761400713915212, 0.04060142980038694,
            0.06267204833410906, 0.08327674157670475,
            0.1019301198172404, 0.1181945319615184,
            0.1316886384491766, 0.1420961093183821,
            0.1491729864726037, 0.1527533871307259
        };

        private static readonly double[] BVND_XN20 =
        {
            -0.9931285991850949, -0.9639719272779138,
            -0.9122344282513259, -0.8391169718222188,
            -0.7463319064601508, -0.6360536807265150,
            -0.5108670019508271, -0.3737060887154196,
            -0.2277858511416451, -0.07652652113349733
        };

        private static readonly double[] BVND_WN12 =
        {
            0.04717533638651177, 0.1069393259953183, 0.1600783285433464,
            0.2031674267230659, 0.2334925365383547, 0.2491470458134029
        };

        private static readonly double[] BVND_XN12 =
        {
            -0.9815606342467191, -0.9041172563704750, -0.7699026741943050,
            -0.5873179542866171, -0.3678314989981802, -0.1252334085114692
        };

        private static readonly double[] BVND_WN6 =
        {
            0.1713244923791705,
            0.3607615730481384,
            0.4679139345726904
        };

        private static readonly double[] BVND_XN6 =
        {
            -0.9324695142031522,
            -0.6612093864662647,
            -0.2386191860831970
        };

        /// <summary>
        ///     Normal cumulative distribution function.
        /// </summary>
        /// <returns>
        ///     The area under the Gaussian p.d.f. integrated
        ///     from minus infinity to the given value.
        /// </returns>
        public static double Function(double value)
        {
            return 0.5 + 0.5 * Special.Erf(value / Constants.Sqrt2);
        }

        /// <summary>
        ///     Normal cumulative distribution function.
        /// </summary>
        /// <returns>
        ///     The area under the Gaussian p.d.f. integrated
        ///     from minus infinity to the given value.
        /// </returns>
        public static double Log(double value)
        {
            return System.Math.Log(0.5 * Special.Erfc(-value / Constants.Sqrt2));
        }

        /// <summary>
        ///     Complemented cumulative distribution function.
        /// </summary>
        /// <returns>
        ///     The area under the Gaussian p.d.f. integrated
        ///     from the given value to positive infinity.
        /// </returns>
        public static double Complemented(double value)
        {
            return 0.5 * Special.Erfc(value / Constants.Sqrt2);
        }

        /// <summary>
        ///     Normal (Gaussian) inverse cumulative distribution function.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         For small arguments <c>0 &lt; y &lt; exp(-2)</c>, the program computes
        ///         <c>
        ///             z =
        ///             sqrt( -2.0 * log(y) )
        ///         </c>
        ///         ;  then the approximation is
        ///         <c>
        ///             x = z - log(z)/z  -
        ///             (1/z) P(1/z) / Q(1/z)
        ///         </c>
        ///         .
        ///     </para>
        ///     <para>
        ///         There are two rational functions P/Q, one for <c>0 &lt; y &lt; exp(-32)</c> and
        ///         the other for <c>y</c> up to <c>exp(-2)</c>. For larger arguments, <c>w = y - 0.5</c>,
        ///         and  <c>x/sqrt(2pi) = w + w^3 * R(w^2)/S(w^2))</c>.
        ///     </para>
        /// </remarks>
        /// <returns>
        ///     Returns the value, <c>x</c>, for which the area under the Normal (Gaussian)
        ///     probability density function (integrated from minus infinity to <c>x</c>) is
        ///     equal to the argument <c>y</c> (assumes mean is zero, variance is one).
        /// </returns>
        public static double Inverse(double y0)
        {
            if (y0 <= 0.0)
            {
                if (y0 == 0)
                {
                    return double.NegativeInfinity;
                }

                throw new ArgumentOutOfRangeException("y0");
            }

            if (y0 >= 1.0)
            {
                if (y0 == 1)
                {
                    return double.PositiveInfinity;
                }

                throw new ArgumentOutOfRangeException("y0");
            }


            var s2pi = System.Math.Sqrt(2.0 * System.Math.PI);
            var code = 1;
            var y = y0;
            double x;


            if (y > 0.8646647167633873)
            {
                y = 1.0 - y;
                code = 0;
            }

            if (y > 0.1353352832366127)
            {
                y -= 0.5;
                var y2 = y * y;
                x = y + y * (y2 * Special.Polevl(y2, inverse_P0, 4) / Special.P1evl(y2, inverse_Q0, 8));
                x *= s2pi;
                return x;
            }

            x = System.Math.Sqrt(-2.0 * System.Math.Log(y));
            var x0 = x - System.Math.Log(x) / x;
            var z = 1.0 / x;
            double x1;

            if (x < 8.0)
            {
                x1 = z * Special.Polevl(z, inverse_P1, 8) / Special.P1evl(z, inverse_Q1, 8);
            }
            else
            {
                x1 = z * Special.Polevl(z, inverse_P2, 8) / Special.P1evl(z, inverse_Q2, 8);
            }

            x = x0 - x1;

            if (code != 0)
            {
                x = -x;
            }

            return x;
        }

        /// <summary>
        ///     High-accuracy Normal cumulative distribution function.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The following formula provide probabilities with an absolute error
        ///         less than 8e-16.
        ///     </para>
        ///     <para>
        ///         References:
        ///         - George Marsaglia, Evaluating the Normal Distribution, 2004.
        ///         Available in: http://www.jstatsoft.org/v11/a05/paper
        ///     </para>
        /// </remarks>
        public static double HighAccuracyFunction(double x)
        {
            var sum = x;
            double term = 0;

            var nextTerm = x;
            var pwr = x * x;
            double i = 1;

            // Iterate until adding next terms doesn't produce
            // any change within the current numerical accuracy.

            while (sum != term)
            {
                term = sum;

                // Next term
                nextTerm *= pwr / (i += 2);

                sum += nextTerm;
            }

            return 0.5 + sum * System.Math.Exp(-0.5 * pwr - 0.5 * Constants.Log2PI);
        }

        /// <summary>
        ///     High-accuracy Complementary normal distribution function.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This function uses 9 tabled values to provide tail values of the
        ///         normal distribution, also known as complementary Phi, with an
        ///         absolute error of 1e-14 ~ 1e-16.
        ///     </para>
        ///     References:
        ///     - George Marsaglia, Evaluating the Normal Distribution, 2004.
        ///     Available in: http://www.jstatsoft.org/v11/a05/paper
        /// </remarks>
        /// <returns>
        ///     The area under the Gaussian p.d.f. integrated
        ///     from the given value to positive infinity.
        /// </returns>
        public static double HighAccuracyComplemented(double x)
        {
            var j = (int)(0.5 * (System.Math.Abs(x) + 1));

            if (j >= high_R.Length)
            {
                return x > 0 ? 0 : 1;
            }

            var a = high_R[j];
            double z = 2 * j;
            var b = a * z - 1.0;

            var h = System.Math.Abs(x) - z;
            var q = h * h;
            double pwr = 1;

            var sum = a + h * b;
            var term = a;


            for (var i = 2; sum != term; i += 2)
            {
                term = sum;

                a = (a + z * b) / i;
                b = (b + z * a) / (i + 1);
                pwr *= q;

                sum = term + pwr * (a + h * b);
            }

            sum *= System.Math.Exp(-0.5 * x * x - 0.91893853320467274178);

            return x >= 0 ? sum : 1.0 - sum;
        }

        /// <summary>
        ///     Bivariate normal cumulative distribution function.
        /// </summary>
        /// <param name="x">The value of the first variate.</param>
        /// <param name="y">The value of the second variate.</param>
        /// <param name="rho">
        ///     The correlation coefficient between x and y. This can be computed
        ///     from a covariance matrix C as  <code>rho = C_12 / (sqrt(C_11) * sqrt(C_22))</code>.
        /// </param>
        /// <returns></returns>
        public static double Bivariate(double x, double y, double rho)
        {
            return BVND(-x, -y, rho);
        }

        /// <summary>
        ///     Complemented bivariate normal cumulative distribution function.
        /// </summary>
        /// <param name="x">The value of the first variate.</param>
        /// <param name="y">The value of the second variate.</param>
        /// <param name="rho">
        ///     The correlation coefficient between x and y. This can be computed
        ///     from a covariance matrix C as  <code>rho = C_12 / (sqrt(C_11) * sqrt(C_22))</code>.
        /// </param>
        /// <returns></returns>
        public static double BivariateComplemented(double x, double y, double rho)
        {
            return BVND(x, y, rho);
        }

        /// <summary>
        ///     A function for computing bivariate normal probabilities.
        ///     BVND calculates the probability that X > DH and Y > DK.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This method is based on the work done by Alan Genz, Department of
        ///         Mathematics, Washington State University. Pullman, WA 99164-3113
        ///         Email: alangenz@wsu.edu. This work was shared under a 3-clause BSD
        ///         license. Please see source file for more details and the actual
        ///         license text.
        ///     </para>
        ///     <para>
        ///         This function is based on the method described by Drezner, Z and G.O.
        ///         Wesolowsky, (1989), On the computation of the bivariate normal integral,
        ///         Journal of Statist. Comput. Simul. 35, pp. 101-107, with major modifications
        ///         for double precision, and for |R| close to 1.
        ///     </para>
        /// </remarks>
        private static double BVND(double dh, double dk, double r)
        {
            // Copyright (C) 2013, Alan Genz,  All rights reserved.               
            // 
            //  Redistribution and use in source and binary forms, with or without
            //  modification, are permitted provided the following conditions are met:
            //    1. Redistributions of source code must retain the above copyright
            //       notice, this list of conditions and the following disclaimer.
            //    2. Redistributions in binary form must reproduce the above copyright
            //       notice, this list of conditions and the following disclaimer in 
            //       the documentation and/or other materials provided with the 
            //       distribution.
            //    3. The contributor name(s) may not be used to endorse or promote 
            //       products derived from this software without specific prior 
            //       written permission.
            //  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
            //  "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT 
            //  LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS 
            //  FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE 
            //  COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, 
            //  INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, 
            //  BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS 
            //  OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND 
            //  ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR 
            //  TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF USE
            //  OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

            const double TWOPI = 2.0 * System.Math.PI;

            double[] x;
            double[] w;

            if (System.Math.Abs(r) < 0.3)
            {
                // Gauss Legendre Points and Weights N =  6
                x = BVND_XN6;
                w = BVND_WN6;
            }
            else if (System.Math.Abs(r) < 0.75)
            {
                // Gauss Legendre Points and Weights N =  12
                x = BVND_XN12;
                w = BVND_WN12;
            }
            else
            {
                // Gauss Legendre Points and Weights N =  20
                x = BVND_XN20;
                w = BVND_WN20;
            }

            var h = dh;
            var k = dk;
            var hk = h * k;
            double bvn = 0;

            if (System.Math.Abs(r) < 0.925)
            {
                if (System.Math.Abs(r) > 0)
                {
                    var sh = (h * h + k * k) / 2;
                    var asr = System.Math.Asin(r);

                    for (var i = 0; i < x.Length; i++)
                    {
                        for (var j = -1; j <= 1; j += 2)
                        {
                            var sn = System.Math.Sin(asr * (j * x[i] + 1) / 2);
                            bvn = bvn + w[i] * System.Math.Exp((sn * hk - sh) / (1 - sn * sn));
                        }
                    }

                    bvn = bvn * asr / (2 * TWOPI);
                }

                return bvn + Function(-h) * Function(-k);
            }


            if (r < 0)
            {
                k = -k;
                hk = -hk;
            }

            if (System.Math.Abs(r) < 1)
            {
                var sa = (1 - r) * (1 + r);
                var A = System.Math.Sqrt(sa);
                var sb = h - k;
                sb = sb * sb;
                var c = (4 - hk) / 8;
                var d = (12 - hk) / 16;
                var asr = -(sb / sa + hk) / 2;

                if (asr > -100)
                {
                    bvn = A * System.Math.Exp(asr) * (1 - c * (sb - sa) * (1 - d * sb / 5) / 3 + c * d * sa * sa / 5);
                }

                if (-hk < 100)
                {
                    var B = System.Math.Sqrt(sb);
                    bvn = bvn - System.Math.Exp(-hk / 2) * System.Math.Sqrt(TWOPI) * Function(-B / A) * B
                        * (1 - c * sb * (1 - d * sb / 5) / 3);
                }

                A = A / 2;

                for (var i = 0; i < x.Length; i++)
                {
                    for (var j = -1; j <= 1; j += 2)
                    {
                        var xs = A * (j * x[i] + 1);
                        xs = xs * xs;
                        var rs = System.Math.Sqrt(1 - xs);
                        asr = -(sb / xs + hk) / 2;

                        if (asr > -100)
                        {
                            bvn = bvn + A * w[i] * System.Math.Exp(asr)
                                * (System.Math.Exp(-hk * xs / (2 * (1 + rs) * (1 + rs))) / rs
                                   - (1 + c * xs * (1 + d * xs)));
                        }
                    }
                }

                bvn = -bvn / TWOPI;
            }

            if (r > 0)
            {
                return bvn + Function(-System.Math.Max(h, k));
            }

            bvn = -bvn;

            if (k <= h)
            {
                return bvn;
            }

            if (h < 0)
            {
                return bvn + Function(k) - Function(h);
            }

            return bvn + Function(-h) - Function(-k);
        }

        /// <summary>
        ///     First derivative of
        ///     <see cref="Function">
        ///         Normal cumulative
        ///         distribution function
        ///     </see>
        ///     , also known as the Normal density
        ///     function.
        /// </summary>
        public static double Derivative(double value)
        {
            return System.Math.Exp(-Constants.LogSqrt2PI - value * value * 0.5);
        }

        /// <summary>
        ///     Log of the first derivative of
        ///     <see cref="Function">
        ///         Normal cumulative
        ///         distribution function
        ///     </see>
        ///     , also known as the Normal density function.
        /// </summary>
        public static double LogDerivative(double value)
        {
            return -Constants.LogSqrt2PI - value * value * 0.5;
        }

        /// <summary>
        ///     1-D Gaussian function.
        /// </summary>
        /// <param name="sigmaSquared">The variance parameter σ² (sigma squared).</param>
        /// <param name="x">x value.</param>
        /// <returns>Returns function's value at point <paramref name="x" />.</returns>
        /// <remarks>
        ///     <para>The function calculates 1-D Gaussian function:</para>
        ///     <code lang="none">
        /// f(x) = exp( x * x / ( -2 * s * s ) ) / ( s * sqrt( 2 * PI ) )
        /// </code>
        /// </remarks>
        public static double Gaussian(double sigmaSquared, double x)
        {
            return System.Math.Exp(x * x / (-2 * sigmaSquared)) / System.Math.Sqrt(2 * System.Math.PI * sigmaSquared);
        }

        /// <summary>
        ///     2-D Gaussian function.
        /// </summary>
        /// <param name="sigmaSquared">The variance parameter σ² (sigma squared).</param>
        /// <param name="x">x value.</param>
        /// <param name="y">y value.</param>
        /// <returns>Returns function's value at point (<paramref name="x" />, <paramref name="y" />).</returns>
        /// <remarks>
        ///     <para>The function calculates 2-D Gaussian function:</para>
        ///     <code lang="none">
        /// f(x, y) = exp( x * x + y * y / ( -2 * s * s ) ) / ( s * s * 2 * PI )
        /// </code>
        /// </remarks>
        public static double Gaussian2D(double sigmaSquared, double x, double y)
        {
            return System.Math.Exp((x * x + y * y) / (-2 * sigmaSquared)) / (2 * System.Math.PI * sigmaSquared);
        }

        /// <summary>
        ///     1-D Gaussian kernel.
        /// </summary>
        /// <param name="sigmaSquared">The variance parameter σ² (sigma squared).</param>
        /// <param name="size">Kernel size (should be odd), [3, 101].</param>
        /// <returns>Returns 1-D Gaussian kernel of the specified size.</returns>
        /// <remarks>
        ///     <para>
        ///         The function calculates 1-D Gaussian kernel, which is array
        ///         of Gaussian function's values in the [-r, r] range of x value, where
        ///         r=floor(<paramref name="size" />/2).
        ///     </para>
        /// </remarks>
        /// <exception cref="ArgumentException">Wrong kernel size.</exception>
        public static double[] Kernel(double sigmaSquared, int size)
        {
            // check for evem size and for out of range
            if (size % 2 == 0 || size < 3)
            {
                throw new ArgumentOutOfRangeException("size", "Kernel size must be odd and higher than 2.");
            }

            var r = size / 2;

            var kernel = new double[size];
            for (int x = -r, i = 0; i < size; x++, i++)
            {
                kernel[i] = Gaussian(sigmaSquared, x);
            }

            return kernel;
        }

        /// <summary>
        ///     2-D Gaussian kernel.
        /// </summary>
        /// <param name="sigmaSquared">The variance parameter σ² (sigma squared).</param>
        /// <param name="size">Kernel size (should be odd), [3, 101].</param>
        /// <returns>Returns 2-D Gaussian kernel of specified size.</returns>
        /// <remarks>
        ///     <para>
        ///         The function calculates 2-D Gaussian kernel, which is array
        ///         of Gaussian function's values in the [-r, r] range of x,y values, where
        ///         r=floor(<paramref name="size" />/2).
        ///     </para>
        /// </remarks>
        /// <exception cref="ArgumentException">Wrong kernel size.</exception>
        public static double[,] Kernel2D(double sigmaSquared, int size)
        {
            // check for evem size and for out of range
            if (size % 2 == 0 || size < 3)
            {
                throw new ArgumentOutOfRangeException("size", "Kernel size must be odd and higher than 2.");
            }

            var r = size / 2;

            var kernel = new double[size, size];
            for (int y = -r, i = 0; i < size; y++, i++)
            {
                for (int x = -r, j = 0; j < size; x++, j++)
                {
                    kernel[i, j] = Gaussian2D(sigmaSquared, x, y);
                }
            }

            return kernel;
        }
    }
}