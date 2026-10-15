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
using System.Numerics;
using Accord.Statistics.Distributions.Fitting;

namespace Accord.Statistics.Distributions.Univariate
{
    /// <summary>
    ///     Wrapped Cauchy Distribution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         In probability theory and directional statistics, a wrapped Cauchy distribution
    ///         is a wrapped probability distribution that results from the "wrapping" of the
    ///         Cauchy distribution around the unit circle. The Cauchy distribution is sometimes
    ///         known as a Lorentzian distribution, and the wrapped Cauchy distribution may
    ///         sometimes be referred to as a wrapped Lorentzian distribution.
    ///     </para>
    ///     <para>
    ///         The wrapped Cauchy distribution is often found in the field of spectroscopy where
    ///         it is used to analyze diffraction patterns (e.g. see Fabry–Pérot interferometer)
    ///     </para>
    ///     .
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Directional_statistics">
    ///                         Wikipedia, The Free Encyclopedia. Directional statistics. Available on:
    ///                         http://en.wikipedia.org/wiki/Directional_statistics
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Wrapped_Cauchy_distribution">
    ///                         Wikipedia, The Free Encyclopedia. Wrapped Cauchy distribution. Available on:
    ///                         http://en.wikipedia.org/wiki/Wrapped_Cauchy_distribution
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///   // Create a Wrapped Cauchy distribution with μ = 0.42, γ = 3
    ///   var dist = new WrappedCauchyDistribution(mu: 0.42, gamma: 3);
    ///   
    ///   // Common measures
    ///   double mean = dist.Mean;     // 0.42
    ///   double var = dist.Variance;  // 0.950212931632136
    ///    
    ///   // Probability density functions
    ///   double pdf = dist.ProbabilityDensityFunction(x: 0.42); // 0.1758330112785475
    ///   double lpdf = dist.LogProbabilityDensityFunction(x: 0.42); // -1.7382205338929015
    ///   
    ///   // String representation
    ///   string str = dist.ToString(); // "WrappedCauchy(x; μ = 0,42, γ = 3)"
    /// </code>
    /// </example>
    /// <seealso cref="CauchyDistribution" />
    public class WrappedCauchyDistribution : UnivariateContinuousDistribution,
        IFittableDistribution<double, CauchyOptions>
    {
        private double gamma;
        private double mu;

        /// <summary>
        ///     Initializes a new instance of the <see cref="WrappedCauchyDistribution" /> class.
        /// </summary>
        /// <param name="mu">The mean resultant parameter μ.</param>
        /// <param name="gamma">The gamma parameter γ.</param>
        public WrappedCauchyDistribution([Real] double mu, [Positive] double gamma)
        {
            if (gamma <= 0)
            {
                throw new ArgumentOutOfRangeException("gamma", "Gamma must be positive.");
            }

            this.mu = mu;
            this.gamma = gamma;
        }

        /// <summary>
        ///     Gets the mean for this distribution.
        /// </summary>
        /// <value>
        ///     The distribution's mean value.
        /// </value>
        public override double Mean
        {
            get => mu;
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the variance for this distribution.
        /// </summary>
        /// <value>
        ///     The distribution's variance.
        /// </value>
        public override double Variance
        {
            get => 1 - System.Math.Exp(-gamma);
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Not supported.
        /// </summary>
        public override double Median => throw new NotSupportedException();

        /// <summary>
        ///     Not supported.
        /// </summary>
        public override double Mode
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
            get => new(-System.Math.PI, System.Math.PI);
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the entropy for this distribution.
        /// </summary>
        /// <value>
        ///     The distribution's entropy.
        /// </value>
        public override double Entropy
        {
            get => System.Math.Log(2 * System.Math.PI * (1 - System.Math.Exp(-2 * gamma)));
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            return new WrappedCauchyDistribution(mu, gamma);
        }

        /// <summary>
        ///     Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">
        ///     The array of observations to fit the model against. The array
        ///     elements can be either of type double (for univariate data) or
        ///     type double[] (for multivariate data).
        /// </param>
        /// <param name="weights">The weight vector containing the weight for each of the samples.</param>
        /// <param name="options">
        ///     Optional arguments which may be used during fitting, such
        ///     as regularization constants and additional parameters.
        /// </param>
        public void Fit(double[] observations, double[] weights, CauchyOptions options)
        {
            if (weights != null)
            {
                throw new ArgumentException("This distribution does not support weighted samples.", "weights");
            }

            double sin = 0, cos = 0;
            for (var i = 0; i < observations.Length; i++)
            {
                sin += System.Math.Sin(observations[i]);
                cos += System.Math.Cos(observations[i]);
            }

            mu = new Complex(sin, cos).Phase;

            double N = observations.Length;
            var R2 = cos / N * (cos / N) + sin / N * (sin / N);
            var R2e = N / (N - 1) * (R2 - 1.0 / N);

            gamma = System.Math.Log(1.0 / R2e) / 2.0;
        }

        /// <summary>
        ///     Not supported.
        /// </summary>
        protected internal override double InnerDistributionFunction(double x)
        {
            throw new NotSupportedException();
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
        protected internal override double InnerProbabilityDensityFunction(double x)
        {
            var constant = 1.0 / (2 * System.Math.PI);
            return constant * System.Math.Sinh(gamma) / (System.Math.Cosh(gamma) - System.Math.Cos(x - mu));
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
        protected internal override double InnerLogProbabilityDensityFunction(double x)
        {
            return System.Math.Log(ProbabilityDensityFunction(x));
        }

        /// <summary>
        ///     Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">
        ///     The array of observations to fit the model against. The array
        ///     elements can be either of type double (for univariate data) or
        ///     type double[] (for multivariate data).
        /// </param>
        /// <param name="weights">The weight vector containing the weight for each of the samples.</param>
        /// <param name="options">
        ///     Optional arguments which may be used during fitting, such
        ///     as regularization constants and additional parameters.
        /// </param>
        public void Fit(double[] observations, int[] weights, CauchyOptions options)
        {
            if (weights != null)
            {
                throw new ArgumentException("This distribution does not support weighted samples.", "weights");
            }

            Fit(observations, (double[])null, options);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "WrappedCauchy(x; μ = {0}, γ = {1})",
                mu.ToString(format, formatProvider),
                gamma.ToString(format, formatProvider));
        }
    }
}