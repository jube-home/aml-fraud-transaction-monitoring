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
using Accord.Statistics.Distributions.Fitting;
using Accord.Statistics.Testing;

namespace Accord.Statistics.Distributions.Univariate
{
    /// <summary>
    ///     Shapiro-Wilk distribution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The Shapiro-Wilk distribution models the distribution of
    ///         <see cref="ShapiroWilkTest">
    ///             Shapiro-Wilk's
    ///         </see>
    ///         <see cref="IHypothesisTest">test statistic</see>.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://sci2s.ugr.es/keel/pdf/algorithm/articulo/royston1982.pdf">
    ///                         Royston, P. "Algorithm AS 181: The W test for Normality", Applied Statistics (1982),
    ///                         Vol. 31, pp. 176–180.
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://lib.stat.cmu.edu/apstat/R94">
    ///                         Royston, P. "Remark AS R94", Applied Statistics (1995), Vol. 44, No. 4, pp. 547-551.
    ///                         Available at http://lib.stat.cmu.edu/apstat/R94
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Royston, P. "Approximating the Shapiro-Wilk W-test for non-normality",
    ///                     Statistics and Computing (1992), Vol. 2, pp. 117-119.
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Royston, P. "An Extension of Shapiro and Wilk's W Test for Normality to Large
    ///                     Samples", Journal of the Royal Statistical Society Series C (1982a), Vol. 31,
    ///                     No. 2, pp. 115-124.
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Create a new Shapiro-Wilk's W for 5 samples
    /// var sw = new ShapiroWilkDistribution(samples: 5);
    /// 
    /// double mean = sw.Mean;     // 0.81248567196628929
    /// double median = sw.Median; // 0.81248567196628929
    /// double mode = sw.Mode;     // 0.81248567196628929
    /// 
    /// double cdf = sw.DistributionFunction(x: 0.84); // 0.83507812080728383
    /// double pdf = sw.ProbabilityDensityFunction(x: 0.84); // 0.82021062372326459
    /// double lpdf = sw.LogProbabilityDensityFunction(x: 0.84); // -0.1981941135071546
    /// 
    /// double ccdf = sw.ComplementaryDistributionFunction(x: 0.84); // 0.16492187919271617
    /// double icdf = sw.InverseDistributionFunction(p: cdf); // 0.84000000194587177
    /// 
    /// double hf = sw.HazardFunction(x: 0.84); // 4.9733281462602292
    /// double chf = sw.CumulativeHazardFunction(x: 0.84); // 1.8022833766369502
    /// 
    /// string str = sw.ToString(CultureInfo.InvariantCulture); // W(x; n = 12)
    /// </code>
    /// </example>
    [Serializable]
    public class ShapiroWilkDistribution : UnivariateContinuousDistribution, IFormattable
    {
        private Func<double, double> g; // forward transformation
        private Func<double, double> h; // inverse transformation
        private NormalDistribution normal;

        /// <summary>
        ///     Creates a new Shapiro-Wilk distribution.
        /// </summary>
        /// <param name="samples">The number of samples.</param>
        public ShapiroWilkDistribution([PositiveInteger(4)] int samples)
        {
            if (samples < 4)
            {
                throw new ArgumentOutOfRangeException("samples",
                    "The number of samples must be higher than 3.");
            }

            NumberOfSamples = samples;

            if (samples <= 11)
            {
                double n = samples;
                var n2 = n * n;
                var n3 = n2 * n;

                var alpha = 0.459 * n - 2.273;
                g = w => -System.Math.Log(alpha - Special.Log1m(w));
                h = w => System.Math.Exp(-System.Math.Exp(-alpha)) *
                         (System.Math.Exp(System.Math.Exp(-alpha)) - System.Math.Exp(w));
                var mean = -0.0006714 * n3 + 0.0250540 * n2 - 0.39978 * n + 0.54400;
                var sigma = System.Math.Exp(-0.0020322 * n3 + 0.0627670 * n2 - 0.77857 * n + 1.38220);

                normal = new NormalDistribution(mean, sigma);
            }
            else
            {
                var u = System.Math.Log(samples);
                var u2 = u * u;
                var u3 = u2 * u;

                g = w => Special.Log1m(w);
                h = w => 1 - System.Math.Exp(w);
                var mean = 0.00389150 * u3 - 0.083751 * u2 - 0.31082 * u - 1.5861;
                var sigma = System.Math.Exp(0.00303020 * u2 - 0.082676 * u - 0.48030);

                normal = new NormalDistribution(mean, sigma);
            }
        }

        /// <summary>
        ///     Gets the number of samples distribution parameter.
        /// </summary>
        public int NumberOfSamples { get; private set; }

        /// <summary>
        ///     Gets the support interval for this distribution.
        /// </summary>
        /// <value>
        ///     A <see cref="DoubleRange" /> containing
        ///     the support interval for this distribution.
        /// </value>
        public override DoubleRange Support
        {
            get => new(0, 1);
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the mean for this distribution.
        /// </summary>
        /// <value>
        ///     The distribution's mean value.
        /// </value>
        public override double Mean
        {
            get => h(normal.Mean);
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
            get => h(normal.Mode);
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Not supported.
        /// </summary>
        public override double Variance
        {
            get => throw new NotSupportedException();
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the median for this distribution.
        /// </summary>
        /// <value>
        ///     The distribution's median value.
        /// </value>
        public override double Median => h(normal.Median);

        /// <summary>
        ///     Not supported.
        /// </summary>
        public override double Entropy
        {
            get => throw new NotSupportedException();
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
            return string.Format("W(x; n = {0})",
                NumberOfSamples.ToString(format, formatProvider));
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
        ///     See <see cref="ShapiroWilkDistribution" />.
        /// </example>
        protected internal override double InnerDistributionFunction(double x)
        {
            var z = g(x);
            var cdf = normal.ComplementaryDistributionFunction(z);
            return cdf;
        }

        /// <summary>
        ///     Gets the complementary cumulative distribution function
        ///     (ccdf) for this distribution evaluated at point <c>x</c>.
        ///     This function is also known as the Survival function.
        /// </summary>
        /// <param name="x">A single point in the distribution range.</param>
        /// <remarks>
        ///     The Complementary Cumulative Distribution Function (CCDF) is
        ///     the complement of the Cumulative Distribution Function, or 1
        ///     minus the CDF.
        /// </remarks>
        protected internal override double InnerComplementaryDistributionFunction(double x)
        {
            var z = g(x);
            return normal.DistributionFunction(z);
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
        protected internal override double InnerProbabilityDensityFunction(double x)
        {
            var z = g(x);
            return normal.ProbabilityDensityFunction(z);
        }

        /// <summary>
        ///     Gets the log-probability density function (pdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">A single point in the distribution range.</param>
        /// <returns>
        ///     The logarithm of the probability of <c>x</c>
        ///     occurring in the current distribution.
        /// </returns>
        /// <remarks>
        ///     The Probability Density Function (PDF) describes the
        ///     probability that a given value <c>x</c> will occur.
        /// </remarks>
        protected internal override double InnerLogProbabilityDensityFunction(double x)
        {
            var z = g(x);
            return normal.LogProbabilityDensityFunction(z);
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
            return new ShapiroWilkDistribution(NumberOfSamples);
        }
    }
}