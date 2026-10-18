// Accord.NET Framework (originally)
// http://accord-framework.net
//
// Copyright © César Souza, 2009-2017
// cesarsouza at gmail.com
//
// Relicensed to MIT per the author's own explicit grant made when the
// upstream project was archived (2020-11-19):
// https://github.com/accord-net/framework
// "I am also willing to change the license of any file where I am the
// single author... to MIT so people can reuse individual pieces of code
// more easily." -- Cesar De Souza
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Diagnostics;

namespace Accord.Math.Integration
{
    /// <summary>
    ///     Status codes for the <see cref="NonAdaptiveGaussKronrod" />
    ///     integration method.
    /// </summary>
    public enum NonAdaptiveGaussKronrodStatus
    {
        /// <summary>
        ///     The integration calculation has been completed with success.
        ///     The obtained result is under the selected convergence criteria.
        /// </summary>
        Success,

        /// <summary>
        ///     Maximum number of steps has been reached.
        /// </summary>
        /// <remarks>
        ///     The maximum number of steps has been executed. The integral
        ///     is probably too difficult to be calculated by dqng.
        /// </remarks>
        MaximumSteps = 1
    }

    /// <summary>
    ///     Non-Adaptive Gauss-Kronrod integration method.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The algorithm implemented by this class has been based on the original FORTRAN
    ///         implementation from QUADPACK. The function implemented the Non-adaptive Gauss-
    ///         Kronrod integration is <c>qng(f,a,b,epsabs,epsrel,result,abserr,neval,ier)</c>.
    ///         The original source code is in the public domain, but this version is under the
    ///         LGPL. The original authors, as long as the original routine description, are
    ///         listed below:
    ///     </para>
    ///     <para>
    ///         Robert Piessens, Elise de Doncker; Applied Mathematics and Programming Division,
    ///         K.U.Leuven, Leuvenappl. This routine calculates an approximation result to a given
    ///         definite integral i = integral of f over (a,b), hopefully satisfying following claim
    ///         for accuracy abs(i-result).le.max(epsabs,epsrel*abs(i)).
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/QUADPACK">
    ///                         Wikipedia, The Free Encyclopedia. QUADPACK. Available on:
    ///                         http://en.wikipedia.org/wiki/QUADPACK
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://www.netlib.no/netlib/quadpack/qng.f">
    ///                         Robert Piessens, Elise de Doncker; Non-adaptive integration standard fortran
    ///                         subroutine (qng.f). Applied Mathematics and Programming Division, K.U.Leuven,
    ///                         Leuvenappl. Available at: http://www.netlib.no/netlib/quadpack/qng.f
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         Let's say we would like to compute the definite integral of the function
    ///         <c>f(x) = cos(x)</c> in the interval -1 to +1 using a variety of integration
    ///         methods, including the <see cref="TrapezoidalRule" />, <see cref="RombergMethod" />
    ///         and <see cref="NonAdaptiveGaussKronrod" />. Those methods can compute definite
    ///         integrals where the integration interval is finite:
    ///     </para>
    ///     <code>
    /// // Declare the function we want to integrate
    /// Func&lt;double, double> f = (x) => Math.Cos(x);
    /// 
    /// // We would like to know its integral from -1 to +1
    /// double a = -1, b = +1;
    /// 
    /// // Integrate!
    /// double trapez  = TrapezoidalRule.Integrate(f, a, b, steps: 1000); // 1.6829414
    /// double romberg = RombergMethod.Integrate(f, a, b);                // 1.6829419
    /// double nagk    = NonAdaptiveGaussKronrod.Integrate(f, a, b);      // 1.6829419
    /// </code>
    ///     <para>
    ///         Moreover, it is also possible to calculate the value of improper integrals
    ///         (it is, integrals with infinite bounds) using <see cref="InfiniteAdaptiveGaussKronrod" />,
    ///         as shown below. Let's say we would like to compute the area under the Gaussian
    ///         curve from -infinite to +infinite. While this function has infinite bounds, this
    ///         function is known to integrate to 1.
    ///     </para>
    ///     <code>
    /// // Declare the Normal distribution's density function (which is the Gaussian's bell curve)
    /// Func&lt;double, double> g = (x) => (1 / Math.Sqrt(2 * Math.PI)) * Math.Exp(-(x * x) / 2);
    /// 
    /// // Integrate!
    /// double iagk = InfiniteAdaptiveGaussKronrod.Integrate(g,
    ///     Double.NegativeInfinity, Double.PositiveInfinity);   // Result should be 0.99999...
    /// </code>
    /// </example>
    /// <seealso cref="TrapezoidalRule" />
    /// <seealso cref="RombergMethod" />
    /// <seealso cref="InfiniteAdaptiveGaussKronrod" />
    /// <seealso cref="MonteCarloIntegration" />
    public class NonAdaptiveGaussKronrod : IUnivariateIntegration,
        INumericalIntegration<NonAdaptiveGaussKronrodStatus>
    {
        private double error;
        private int evaluations;

        private DoubleRange range;
        private double result;

        /// <summary>
        ///     Creates a new <see cref="NonAdaptiveGaussKronrod" /> integration algorithm.
        /// </summary>
        public NonAdaptiveGaussKronrod()
        {
            ToleranceAbsolute = 0;
            ToleranceRelative = 1e-3;
            range = new DoubleRange(0, 1);
        }

        /// <summary>
        ///     Creates a new <see cref="NonAdaptiveGaussKronrod" /> integration algorithm.
        /// </summary>
        /// <param name="function">The function to be integrated.</param>
        public NonAdaptiveGaussKronrod(Func<double, double> function)
            : this()
        {
            if (function == null)
            {
                throw new ArgumentNullException("function");
            }

            Function = function;
            ToleranceAbsolute = 0;
            ToleranceRelative = 1e-3;
        }

        /// <summary>
        ///     Creates a new <see cref="NonAdaptiveGaussKronrod" /> integration algorithm.
        /// </summary>
        /// <param name="function">The function to be integrated.</param>
        /// <param name="a">The lower limit of integration.</param>
        /// <param name="b">The upper limit of integration.</param>
        public NonAdaptiveGaussKronrod(Func<double, double> function, double a, double b)
            : this(function)
        {
            if (double.IsInfinity(a) || double.IsNaN(a))
            {
                throw new ArgumentOutOfRangeException("a");
            }

            if (double.IsInfinity(b) || double.IsNaN(b))
            {
                throw new ArgumentOutOfRangeException("b");
            }

            range = new DoubleRange(a, b);
        }

        /// <summary>
        ///     Desired absolute accuracy. If set to zero, this parameter
        ///     will be ignored and only other requisites will be taken
        ///     into account. Default is zero.
        /// </summary>
        public double ToleranceAbsolute { get; set; }

        /// <summary>
        ///     Desired relative accuracy. If set to zero, this parameter
        ///     will be ignored and only other requisites will be taken
        ///     into account. Default is 1e-3.
        /// </summary>
        public double ToleranceRelative { get; set; }

        /// <summary>
        ///     Gets the integration error for the
        ///     computed <see cref="Area" /> value.
        /// </summary>
        public double Error => error;

        /// <summary>
        ///     Gets the number of function evaluations performed in
        ///     the last call to the <see cref="Compute" /> method.
        /// </summary>
        public int FunctionEvaluations => evaluations;

        /// <summary>
        ///     Get the exit code returned in the last call to the
        ///     <see cref="INumericalIntegration.Compute()" /> method.
        /// </summary>
        public NonAdaptiveGaussKronrodStatus Status { get; private set; }

        /// <summary>
        ///     Gets or sets the function to be differentiated.
        /// </summary>
        public Func<double, double> Function { get; set; }

        /// <summary>
        ///     Gets or sets the input range under
        ///     which the integral must be computed.
        /// </summary>
        public DoubleRange Range
        {
            get => range;
            set
            {
                if (double.IsInfinity(range.Min) || double.IsNaN(range.Min))
                {
                    throw new ArgumentOutOfRangeException("value", "Minimum is out of range.");
                }

                if (double.IsInfinity(range.Max) || double.IsNaN(range.Max))
                {
                    throw new ArgumentOutOfRangeException("value", "Maximum is out of range.");
                }

                range = value;
            }
        }

        /// <summary>
        ///     Gets the numerically computed result of the
        ///     definite integral for the specified function.
        /// </summary>
        public double Area => result;

        /// <summary>
        ///     Computes the area of the function under the selected <see cref="Range" />.
        ///     The computed value will be available at this object's <see cref="Area" />.
        /// </summary>
        /// <remarks>
        ///     If the integration method fails, the reason will be available at <see cref="Status" />.
        /// </remarks>
        /// <returns>
        ///     True if the integration method succeeds, false otherwise.
        /// </returns>
        public bool Compute()
        {
            int errorCode;

            qng_(Function, Range.Min, Range.Max, ToleranceAbsolute, ToleranceRelative,
                out result, out error, out evaluations, out errorCode);

            Status = (NonAdaptiveGaussKronrodStatus)errorCode;

            return Status == NonAdaptiveGaussKronrodStatus.Success;
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public object Clone()
        {
            var clone = new NonAdaptiveGaussKronrod(
                Function, Range.Min, Range.Max);

            clone.error = error;
            clone.result = result;
            clone.ToleranceAbsolute = ToleranceAbsolute;
            clone.ToleranceRelative = ToleranceRelative;

            return clone;
        }

        /// <summary>
        ///     Computes the area under the integral for the given function,
        ///     in the given integration interval, using Gauss-Kronrod method.
        /// </summary>
        /// <param name="f">The unidimensional function whose integral should be computed.</param>
        /// <param name="a">The beginning of the integration interval.</param>
        /// <param name="b">The ending of the integration interval.</param>
        /// <returns>The integral's value in the current interval.</returns>
        public static double Integrate(Func<double, double> f, double a, double b)
        {
            return Integrate(f, a, b, 1e-3);
        }

        /// <summary>
        ///     Computes the area under the integral for the given function, in the given
        ///     integration interval, using the Non-Adaptive Gauss Kronrod algorithm.
        /// </summary>
        /// <param name="f">The unidimensional function whose integral should be computed.</param>
        /// <param name="a">The beginning of the integration interval.</param>
        /// <param name="b">The ending of the integration interval.</param>
        /// <param name="tolerance">
        ///     The relative tolerance under which the solution has to be found. Default is 1e-3.
        /// </param>
        /// <returns>The integral's value in the current interval.</returns>
        public static double Integrate(Func<double, double> f, double a, double b, double tolerance)
        {
            qng_(f, a, b, 0, tolerance, out var result, out _, out _, out _);

            return result;
        }

        #region Quadpack

        internal static int qng_(Func<double, double> f, double a, double b,
            double epsabs, double epsrel, out double result, out double abserr,
            out int neval, out int ier)
        {
            /* Local variables */
            int k;
            var ipx = 0;
            double res21 = 0, res43 = 0, resabs = 0, resasc = 0;
            var fv1 = new double[5];
            var fv2 = new double[5];
            var fv3 = new double[5];
            var fv4 = new double[5];

            double absc, fval;
            double fval1, fval2;

            var savfun = new double[21];

            /*           machine dependent constants               */
            /*           ---------------------------               */
            /*           epmach is the largest relative spacing.   */
            /*           uflow is the smallest positive magnitude. */

            var epmach = Constants.DoubleEpsilon;
            var uflow = double.Epsilon;

            /*           test on validity of parameters */
            /*           ------------------------------ */

            result = 0.0;
            abserr = 0.0;
            neval = 0;
            ier = 6;

            if (epsabs <= 0.0 && epsrel < System.Math.Max(5e-15, epmach * 50))
            {
                goto L80;
            }

            var hlgth = (b - a) * .5;
            var dhlgth = System.Math.Abs(hlgth);

            var centr = (b + a) * .5;
            var fcentr = f(centr);
            neval = 21;
            ier = 1;

            /*          compute the integral using the 10- and 21-point formula. */
            for (var l = 1; l <= 3; ++l)
            {
                switch (l)
                {
                    case 1: goto L5;
                    case 2: goto L25;
                    case 3: goto L45;
                }

                L5:
                var res10 = 0.0;
                res21 = w21b[5] * fcentr;
                resabs = w21b[5] * System.Math.Abs(fcentr);
                for (k = 1; k <= 5; ++k)
                {
                    absc = hlgth * x1[k - 1];
                    fval1 = f(centr + absc);
                    fval2 = f(centr - absc);
                    fval = fval1 + fval2;
                    res10 += w10[k - 1] * fval;
                    res21 += w21a[k - 1] * fval;
                    resabs += w21a[k - 1] * (System.Math.Abs(fval1) + System.Math.Abs(fval2));
                    savfun[k - 1] = fval;
                    fv1[k - 1] = fval1;
                    fv2[k - 1] = fval2;
                }

                ipx = 5;
                for (k = 1; k <= 5; ++k)
                {
                    ++ipx;
                    absc = hlgth * x2[k - 1];
                    fval1 = f(centr + absc);
                    fval2 = f(centr - absc);
                    fval = fval1 + fval2;
                    res21 += w21b[k - 1] * fval;
                    resabs += w21b[k - 1] * (System.Math.Abs(fval1) + System.Math.Abs(fval2));
                    savfun[ipx - 1] = fval;
                    fv3[k - 1] = fval1;
                    fv4[k - 1] = fval2;
                }

                /*          test for convergence. */
                result = res21 * hlgth;
                resabs *= dhlgth;
                var reskh = res21 * .5;
                resasc = w21b[5] * System.Math.Abs(fcentr - reskh);

                for (k = 1; k <= 5; ++k)
                {
                    resasc = resasc
                             + w21a[k - 1] * (System.Math.Abs(fv1[k - 1] - reskh) + System.Math.Abs(fv2[k - 1] - reskh))
                             + w21b[k - 1] * (System.Math.Abs(fv3[k - 1] - reskh) +
                                              System.Math.Abs(fv4[k - 1] - reskh));
                }

                abserr = System.Math.Abs((res21 - res10) * hlgth);
                resasc *= dhlgth;
                goto L65;

                /*          compute the integral using the 43-point formula. */
                L25:
                res43 = w43b[11] * fcentr;
                neval = 43;

                for (k = 1; k <= 10; ++k)
                {
                    res43 += savfun[k - 1] * w43a[k - 1];
                }

                for (k = 1; k <= 11; ++k)
                {
                    ++ipx;
                    absc = hlgth * x3[k - 1];
                    fval = f(absc + centr) + f(centr - absc);
                    res43 += fval * w43b[k - 1];
                    savfun[ipx - 1] = fval;
                }

                /*          test for convergence. */
                result = res43 * hlgth;
                abserr = System.Math.Abs((res43 - res21) * hlgth);
                goto L65;

                /*          compute the integral using the 87-point formula. */
                L45:
                var res87 = w87b[22] * fcentr;
                neval = 87;

                for (k = 1; k <= 21; ++k)
                {
                    res87 += savfun[k - 1] * w87a[k - 1];
                }

                for (k = 1; k <= 22; ++k)
                {
                    absc = hlgth * x4[k - 1];
                    res87 += w87b[k - 1] * (f(absc + centr) + f(centr - absc));
                }

                result = res87 * hlgth;
                abserr = System.Math.Abs(res87 - res43) * hlgth;

                L65:
                if (resasc != 0.0 && abserr != 0.0)
                {
                    abserr = resasc * System.Math.Min(1.0, System.Math.Pow(abserr * 200.0 / resasc, 1.5));
                }

                if (resabs > uflow / (epmach * 50.0))
                {
                    abserr = System.Math.Max(epmach * 50.0 * resabs, abserr);
                }

                if (abserr <= System.Math.Max(epsabs, epsrel * System.Math.Abs(result)))
                {
                    ier = 0;
                }

                /* ***jump out of do-loop */
                if (ier == 0)
                {
                    goto L999;
                }
            }

            L80:
            //throw new Exception("abnormal return from  qng ");
            Trace.Write("abnormal return from  qng");

            L999:
            return 0;
        } /* qng_ */

        private static readonly double[] x1 =
        {
            0.9739065285171717, 0.8650633666889845,
            .6794095682990244, .4333953941292472, .1488743389816312
        };

        private static readonly double[] w87a =
        {
            0.008148377384149173, 0.01876143820156282, 0.02734745105005229,
            0.03367770731163793, 0.03693509982042791, 0.002884872430211531,
            0.0136859460227127, 0.02328041350288831, 0.03087249761171336,
            0.03569363363941877, 9.152833452022414e-4, 0.005399280219300471,
            0.01094767960111893, 0.01629873169678734, 0.02108156888920384,
            0.02537096976925383, 0.02918969775647575, 0.03237320246720279,
            0.03478309895036514, 0.03641222073135179, 0.03725387550304771
        };

        private static readonly double[] w87b =
        {
            2.741455637620724e-4, .001807124155057943, 0.004096869282759165,
            0.006758290051847379, .009549957672201647, .01232944765224485,
            0.01501044734638895, .01754896798624319, 0.01993803778644089,
            0.02219493596101229, .02433914712600081, 0.02637450541483921,
            0.0282869107887712, .0300525811280927, 0.03164675137143993,
            0.0330504134199785, .03425509970422606, 0.03526241266015668,
            0.0360769896228887, .03669860449845609, 0.03712054926983258,
            0.03733422875193504, .03736107376267902
        };

        private static readonly double[] x2 =
        {
            0.9956571630258081, 0.9301574913557082,
            0.7808177265864169, 0.5627571346686047, 0.2943928627014602
        };

        private static readonly double[] x3 =
        {
            .9993333609019321, .9874334029080889,
            .9548079348142663, .9001486957483283, .8251983149831142,
            .732148388989305, .6228479705377252, .4994795740710565,
            .3649016613465808, .2222549197766013, .07465061746138332
        };

        private static readonly double[] x4 =
        {
            .9999029772627292, .9979898959866787,
            .9921754978606872, .9813581635727128, .9650576238583846,
            .9431676131336706, .9158064146855072, .8832216577713165,
            .8457107484624157, .803557658035231, .7570057306854956,
            .7062732097873218, .6515894665011779, .5932233740579611,
            .5314936059708319, .4667636230420228, .3994248478592188,
            .3298748771061883, .2585035592021616, .1856953965683467,
            .1118422131799075, .03735212339461987
        };

        private static readonly double[] w10 =
        {
            .06667134430868814, .1494513491505806,
            .219086362515982, .2692667193099964, .2955242247147529
        };

        private static readonly double[] w21a =
        {
            .03255816230796473, .07503967481091995,
            .1093871588022976, .1347092173114733, .1477391049013385
        };

        private static readonly double[] w21b =
        {
            .01169463886737187, .054755896574352,
            .09312545458369761, .1234919762620659, .1427759385770601,
            .1494455540029169
        };

        private static readonly double[] w43a =
        {
            .01629673428966656, .0375228761208695,
            .05469490205825544, .06735541460947809, .07387019963239395,
            .005768556059769796, .02737189059324884, .04656082691042883,
            .06174499520144256, .0713872672686934
        };

        private static readonly double[] w43b =
        {
            .001844477640212414, .01079868958589165,
            .02189536386779543, .03259746397534569, .04216313793519181,
            .05074193960018458, .05837939554261925, .06474640495144589,
            .06956619791235648, .07282444147183321, .07450775101417512,
            .07472214751740301
        };

        #endregion
    }
}