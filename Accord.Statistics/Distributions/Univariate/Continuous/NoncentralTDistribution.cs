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
using Accord.Math;
using Accord.Math.Optimization;
using Accord.Statistics.Distributions.Fitting;

namespace Accord.Statistics.Distributions.Univariate
{
    /// <summary>
    ///     Noncentral t-distribution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         As with other noncentrality parameters, the noncentral t-distribution generalizes
    ///         a probability distribution – <see cref="TDistribution">Student's t-distribution</see>
    ///         – using a noncentrality parameter. Whereas the central distribution describes how a
    ///         test statistic is distributed when the difference tested is null, the noncentral
    ///         distribution also describes how <c>t</c> is distributed when the null is false.
    ///         This leads to its use in statistics, especially calculating statistical power. The
    ///         noncentral t-distribution is also known as the singly noncentral t-distribution, and
    ///         in addition to its primary use in statistical inference, is also used in robust
    ///         modeling for data.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Noncentral_t-distribution">
    ///                         Wikipedia, The Free Encyclopedia. Noncentral t-distribution. Available on:
    ///                         http://en.wikipedia.org/wiki/Noncentral_t-distribution
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///   var distribution = new NoncentralTDistribution(
    ///         degreesOfFreedom: 4, noncentrality: 2.42);
    /// 
    ///   double mean = distribution.Mean;     // 3.0330202123035104
    ///   double median = distribution.Median; // 2.6034842414893795
    ///   double var = distribution.Variance;  // 4.5135883917583683
    ///   
    ///   double cdf = distribution.DistributionFunction(x: 1.4); // 0.15955740661144721
    ///   double pdf = distribution.ProbabilityDensityFunction(x: 1.4); // 0.23552141805184526
    ///   double lpdf = distribution.LogProbabilityDensityFunction(x: 1.4); // -1.4459534225195116
    ///   
    ///   double ccdf = distribution.ComplementaryDistributionFunction(x: 1.4); // 0.84044259338855276
    ///   double icdf = distribution.InverseDistributionFunction(p: cdf); // 1.4000000000123853
    ///   
    ///   double hf = distribution.HazardFunction(x: 1.4); // 0.28023498559521387
    ///   double chf = distribution.CumulativeHazardFunction(x: 1.4); // 0.17382662901507062
    ///   
    ///   string str = distribution.ToString(CultureInfo.InvariantCulture); // T(x; df = 4, μ = 2.42)
    /// </code>
    /// </example>
    /// <seealso cref="TDistribution" />
    /// <seealso cref="Testing.TTest" />
    [Serializable]
    public class NoncentralTDistribution : UnivariateContinuousDistribution, IFormattable
    {
        private double? mode;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TDistribution" /> class.
        /// </summary>
        /// <param name="degreesOfFreedom">The degrees of freedom v.</param>
        /// <param name="noncentrality">The noncentrality parameter μ (mu).</param>
        public NoncentralTDistribution([Positive] double degreesOfFreedom, [Real] double noncentrality)
        {
            if (degreesOfFreedom <= 0)
            {
                throw new ArgumentOutOfRangeException("degreesOfFreedom",
                    "The number of degrees of freedom must be positive.");
            }

            DegreesOfFreedom = degreesOfFreedom;
            Noncentrality = noncentrality;
        }

        /// <summary>
        ///     Gets the degrees of freedom (v) for the distribution.
        /// </summary>
        public double DegreesOfFreedom { get; }

        /// <summary>
        ///     Gets the noncentrality parameter μ (mu) for the distribution.
        /// </summary>
        public double Noncentrality { get; }

        /// <summary>
        ///     Gets the mean for this distribution.
        /// </summary>
        /// <remarks>
        ///     The noncentral t-distribution's mean is defined in terms of
        ///     the <see cref="Gamma.Function(double)">Gamma function Γ(x)</see> as
        ///     <c>μ * sqrt(v/2) * Γ((v - 1) / 2) / Γ(v / 2) for v > 1</c>.
        /// </remarks>
        public override double Mean
        {
            get
            {
                if (DegreesOfFreedom > 1)
                {
                    return Noncentrality * System.Math.Sqrt(DegreesOfFreedom / 2.0) *
                        Gamma.Function((DegreesOfFreedom - 1) / 2) / Gamma.Function(DegreesOfFreedom / 2);
                }

                return double.NaN;
            }
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the variance for this distribution.
        /// </summary>
        /// <remarks>
        ///     The noncentral t-distribution's variance is defined in terms of
        ///     the <see cref="Gamma.Function(double)">Gamma function Γ(x)</see> as
        ///     <c>a - b * c²</c> in which
        ///     <c>a = v*(1+μ²) / (v-2)</c>,
        ///     <c>b = (u² * v) / 2</c> and
        ///     <c>c = Γ((v - 1) / 2) / Γ(v / 2)</c> for <c>v > 2</c>.
        /// </remarks>
        public override double Variance
        {
            get
            {
                if (DegreesOfFreedom > 2)
                {
                    var a = DegreesOfFreedom * (1 + Noncentrality * Noncentrality) / (DegreesOfFreedom - 2);
                    var b = Noncentrality * Noncentrality * DegreesOfFreedom / 2;
                    var c = Gamma.Function((DegreesOfFreedom - 1) / 2) / Gamma.Function(DegreesOfFreedom / 2);
                    return a - b * c * c;
                }

                return double.NaN;
            }
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the mode for this distribution.
        /// </summary>
        /// <value>
        ///     The distribution's mode value.
        /// </value>
        public override double Mode
        {
            get
            {
                if (mode == null)
                {
                    if (Noncentrality == 0)
                    {
                        var ratio = Gamma.Function((DegreesOfFreedom + 2) / 2) /
                                    Gamma.Function((DegreesOfFreedom + 3) / 3);
                        mode = System.Math.Sqrt(2 / DegreesOfFreedom) * ratio * Noncentrality;
                    }
                    else if (double.IsInfinity(Noncentrality))
                    {
                        mode = System.Math.Sqrt(DegreesOfFreedom / (DegreesOfFreedom + 1)) * Noncentrality;
                    }
                    else
                    {
                        var upper = System.Math.Sqrt(2 * DegreesOfFreedom / (2 * DegreesOfFreedom + 5)) * Noncentrality;
                        var lower = System.Math.Sqrt(DegreesOfFreedom / (DegreesOfFreedom + 1)) * Noncentrality;

                        if (upper > lower)
                        {
                            mode = BrentSearch.Maximize(ProbabilityDensityFunction, lower, upper);
                        }
                        else
                        {
                            mode = BrentSearch.Maximize(ProbabilityDensityFunction, upper, lower);
                        }
                    }
                }

                return mode.Value;
            }
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Not supported.
        /// </summary>
        public override double Entropy
        {
            get => throw new NotSupportedException();
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the support interval for this distribution.
        /// </summary>
        /// <value>
        ///     A <see cref="DoubleRange" /> containing
        ///     the support interval for this distribution.
        /// </value>
        public override DoubleRange Support
        {
            get => new(double.NegativeInfinity, double.PositiveInfinity);
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "T(x; df = {0}, μ = {1})",
                DegreesOfFreedom.ToString(format, formatProvider),
                Noncentrality.ToString(format, formatProvider));
        }

        /// <summary>
        ///     Gets the cumulative distribution function (cdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">A single point in the distribution range.</param>
        /// <remarks>
        ///     The Cumulative Distribution Function (CDF) describes the cumulative
        ///     probability that a given value or any value smaller than it will occur.
        /// </remarks>
        /// <example>
        ///     See <see cref="NoncentralTDistribution" />.
        /// </example>
        protected internal override double InnerDistributionFunction(double x)
        {
            return distributionFunctionLowerTail(x, DegreesOfFreedom, Noncentrality);
        }

        /// <summary>
        ///     Gets the probability density function (pdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">A single point in the distribution range.</param>
        /// <returns>
        ///     The probability of <c>x</c> occurring
        ///     in the current distribution.
        /// </returns>
        /// <remarks>
        ///     The Probability Density Function (PDF) describes the
        ///     probability that a given value <c>x</c> will occur.
        /// </remarks>
        /// <example>
        ///     See <see cref="NoncentralTDistribution" />.
        /// </example>
        protected internal override double InnerProbabilityDensityFunction(double x)
        {
            var u = Noncentrality; // μ
            var v = DegreesOfFreedom;
            var F = distributionFunctionLowerTail;

            if (x != 0)
            {
                var A = F(x * System.Math.Sqrt(1 + 2 / v), v + 2, u);
                var B = F(x, v, u);
                var C = v / x;
                return C * (A - B);
            }
            else
            {
                var A = Gamma.Function((v + 1) / 2);
                var B = System.Math.Sqrt(System.Math.PI * v) * Gamma.Function(v / 2);
                var C = System.Math.Exp(-(u * u) / 2);
                return A / B * C;
            }
        }

        /// <summary>
        ///     Not supported.
        /// </summary>
        public override void Fit(double[] observations, double[] weights, IFittingOptions options)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            return new NoncentralTDistribution(DegreesOfFreedom, Noncentrality);
        }

        /// <summary>
        ///     Computes the cumulative probability at <c>t</c> of the
        ///     non-central T-distribution with DF degrees of freedom
        ///     and non-centrality parameter.
        /// </summary>
        /// <remarks>
        ///     This function is based on the original work done by
        ///     Russell Lent hand John Burkardt, shared under the
        ///     LGPL license. Original FORTRAN code can be found at:
        ///     http://people.sc.fsu.edu/~jburkardt/f77_src/asa243/asa243.html
        /// </remarks>
        private static double distributionFunctionLowerTail(double t, double df, double delta)
        {
            var alnrpi = 0.57236494292470008707;
            var errmax = 1.0E-10;
            var itrmax = 100;
            var r2pi = 0.79788456080286535588;

            if (df <= 0.0)
            {
                throw new ArgumentOutOfRangeException("df",
                    "Degrees of freedom must be positive.");
            }

            double del;
            bool negdel;

            if (t < 0.0)
            {
                del = -delta;
                negdel = true;
            }
            else
            {
                del = delta;
                negdel = false;
            }

            // Initialize twin series.
            var en = 1.0;
            var x = t * t / (t * t + df);
            if (double.IsNaN(x))
            {
                x = 1;
            }

            double value = 0;

            if (x <= 0.0)
            {
                // upper tail of normal cumulative function
                value += Normal.Complemented(del);

                if (negdel)
                {
                    value = 1.0 - value;
                }

                return value;
            }

            var lambda = del * del;
            var p = 0.5 * System.Math.Exp(-0.5 * lambda);
            var q = r2pi * p * del;
            var s = 0.5 - p;
            var a = 0.5;
            var b = 0.5 * df;
            var rxb = System.Math.Pow(1.0 - x, b);
            var albeta = alnrpi + Gamma.Log(b) - Gamma.Log(a + b);
            var xodd = Beta.Incomplete(a, b, x);
            var godd = 2.0 * rxb * System.Math.Exp(a * System.Math.Log(x) - albeta);
            var xeven = 1.0 - rxb;
            var geven = b * x * rxb;
            value = p * xodd + q * xeven;

            // Repeat until convergence.
            while (true)
            {
                a = a + 1.0;
                xodd = xodd - godd;
                xeven = xeven - geven;
                godd = godd * x * (a + b - 1.0) / a;
                geven = geven * x * (a + b - 0.5) / (a + 0.5);
                p = p * lambda / (2.0 * en);
                q = q * lambda / (2.0 * en + 1.0);
                s = s - p;
                en = en + 1.0;
                value = value + p * xodd + q * xeven;
                var errbd = 2.0 * s * (xodd - godd);

                if (errbd <= errmax || double.IsNaN(errbd))
                {
                    break;
                }

                if (itrmax < en)
                {
                    throw new ConvergenceException("Maximum number of iterations reached.");
                }
            }

            // upper tail of normal cumulative function
            value = value + Normal.Complemented(del);

            if (negdel)
            {
                value = 1.0 - value;
            }

            return value;
        }
    }
}