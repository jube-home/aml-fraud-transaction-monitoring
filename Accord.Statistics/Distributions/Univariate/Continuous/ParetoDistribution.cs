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

namespace Accord.Statistics.Distributions.Univariate
{
    /// <summary>
    ///     Pareto's Distribution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The Pareto distribution, named after the Italian economist Vilfredo Pareto, is a power law
    ///         probability distribution that coincides with social, scientific, geophysical, actuarial,
    ///         and many other types of observable phenomena. Outside the field of economics it is sometimes
    ///         referred to as the Bradford distribution.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Pareto_distribution">
    ///                         Wikipedia, The Free Encyclopedia. Pareto distribution.
    ///                         Available from: http://en.wikipedia.org/wiki/Pareto_distribution
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///    // Creates a new Pareto's distribution with xm = 0.42, α = 3
    ///    var pareto = new ParetoDistribution(scale: 0.42, shape: 3);
    ///    
    ///    // Common measures
    ///    double mean   = pareto.Mean;     // 0.63
    ///    double median = pareto.Median;   // 0.52916684095584676
    ///    double var    = pareto.Variance; // 0.13229999999999997
    ///    
    ///    // Cumulative distribution functions
    ///    double cdf = pareto.DistributionFunction(x: 1.4);           // 0.973
    ///    double ccdf = pareto.ComplementaryDistributionFunction(x: 1.4); // 0.027000000000000024
    ///    double icdf = pareto.InverseDistributionFunction(p: cdf);       // 1.4000000446580794
    ///    
    ///    // Probability density functions
    ///    double pdf = pareto.ProbabilityDensityFunction(x: 1.4);     // 0.057857142857142857
    ///    double lpdf = pareto.LogProbabilityDensityFunction(x: 1.4); // -2.8497783609309111
    ///    
    ///    // Hazard (failure rate) functions
    ///    double hf = pareto.HazardFunction(x: 1.4);            // 2.142857142857141
    ///    double chf = pareto.CumulativeHazardFunction(x: 1.4); // 3.6119184129778072
    ///    
    ///    // String representation
    ///    string str = pareto.ToString(CultureInfo.InvariantCulture); // Pareto(x; xm = 0.42, α = 3)
    /// </code>
    /// </example>
    [Serializable]
    public class ParetoDistribution : UnivariateContinuousDistribution,
        IFormattable, ISampleableDistribution<double>
    {
        /// <summary>
        ///     Creates new Pareto distribution.
        /// </summary>
        public ParetoDistribution()
            : this(1, 1)
        {
        }

        /// <summary>
        ///     Creates new Pareto distribution.
        /// </summary>
        /// <param name="scale">The scale parameter x<sub>m</sub>. Default is 1.</param>
        /// <param name="shape">The shape parameter α (alpha). Default is 1.</param>
        public ParetoDistribution([Positive] double scale, [Positive] double shape)
        {
            if (scale <= 0)
            {
                throw new ArgumentOutOfRangeException("scale",
                    "Scale must be positive.");
            }

            if (shape <= 0)
            {
                throw new ArgumentOutOfRangeException("shape",
                    "Shape must be positive.");
            }

            init(scale, shape);
        }

        /// <summary>
        ///     Gets the scale parameter x<sub>m</sub> for this distribution.
        /// </summary>
        public double Scale { get; private set; }

        /// <summary>
        ///     Gets the shape parameter α (alpha) for this distribution.
        /// </summary>
        public double Alpha { get; private set; }

        /// <summary>
        ///     Gets the mean for this distribution.
        /// </summary>
        /// <value>The distribution's mean value.</value>
        /// <remarks>
        ///     The Pareto distribution's mean is defined as
        ///     <c>α * x<sub>m</sub> / (α - 1)</c>.
        /// </remarks>
        public override double Mean
        {
            get => Alpha * Scale / (Alpha - 1);
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the variance for this distribution.
        /// </summary>
        /// <value>The distribution's variance.</value>
        /// <remarks>
        ///     The Pareto distribution's mean is defined as
        ///     <c>α * x<sub>m</sub>² / ((α - 1)² * (α - 2)</c>.
        /// </remarks>
        public override double Variance
        {
            get => Scale * Scale * Alpha / ((Alpha - 1) * (Alpha - 1) * (Alpha - 2));
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the entropy for this distribution.
        /// </summary>
        /// <value>The distribution's entropy.</value>
        /// <remarks>
        ///     The Pareto distribution's Entropy is defined as
        ///     <c>ln(x<sub>m</sub> / α) + 1 / α + 1</c>.
        /// </remarks>
        public override double Entropy
        {
            get => System.Math.Log(Scale / Alpha) + 1.0 / Alpha + 1.0;
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
            get => new(Scale, double.PositiveInfinity);
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the mode for this distribution.
        /// </summary>
        /// <value>The distribution's mode value.</value>
        public override double Mode
        {
            get => Scale;
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the median for this distribution.
        /// </summary>
        /// <value>The distribution's median value.</value>
        /// <remarks>
        ///     The Pareto distribution's median is defined
        ///     as <c>x<sub>m</sub> * 2^(1 / α)</c>.
        /// </remarks>
        public override double Median => Scale * System.Math.Pow(2, 1.0 / Alpha);

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "Pareto(x; xm = {0}, α = {1})",
                Scale.ToString(format, formatProvider),
                Alpha.ToString(format, formatProvider));
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            return new ParetoDistribution(Scale, Alpha);
        }

        /// <summary>
        ///     Generates a random vector of observations from the current distribution.
        /// </summary>
        /// <param name="samples">The number of samples to generate.</param>
        /// <param name="result">The location where to store the samples.</param>
        /// <param name="source">
        ///     The random number generator to use as a source of randomness.
        ///     Default is to use <see cref="Accord.Math.Random.Generator.Random" />.
        /// </param>
        /// <returns>A random vector of observations drawn from this distribution.</returns>
        public override double[] Generate(int samples, double[] result, Random source)
        {
            UniformContinuousDistribution.Random(samples, result, source);
            for (var i = 0; i < samples; i++)
            {
                result[i] = Scale / System.Math.Pow(result[i], 1.0 / Alpha);
            }

            return result;
        }

        /// <summary>
        ///     Generates a random observation from the current distribution.
        /// </summary>
        /// <returns>A random observations drawn from this distribution.</returns>
        public override double Generate(Random source)
        {
            var u = UniformContinuousDistribution.Random(source);
            return Scale / System.Math.Pow(u, 1.0 / Alpha);
        }

        private void init(double scale, double shape)
        {
            Scale = scale;
            Alpha = shape;
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
        ///     See <see cref="ParetoDistribution" />.
        /// </example>
        protected internal override double InnerDistributionFunction(double x)
        {
            return 1 - System.Math.Pow(Scale / x, Alpha);
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
        ///     See <see cref="ParetoDistribution" />.
        /// </example>
        protected internal override double InnerProbabilityDensityFunction(double x)
        {
            return Alpha * System.Math.Pow(Scale, Alpha) / System.Math.Pow(x, Alpha + 1);
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
        /// <example>
        ///     See <see cref="ParetoDistribution" />.
        /// </example>
        protected internal override double InnerLogProbabilityDensityFunction(double x)
        {
            return System.Math.Log(Alpha) + Alpha * System.Math.Log(Scale) - (Alpha + 1) * System.Math.Log(x);
        }

        /// <summary>
        ///     Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">
        ///     The array of observations to fit the model against. The array
        ///     elements can be either of type double (for univariate data) or
        ///     type double[] (for multivariate data).
        /// </param>
        /// <param name="weights">
        ///     The weight vector containing the weight for each of the samples.
        /// </param>
        /// <param name="options">
        ///     Optional arguments which may be used during fitting, such
        ///     as regularization constants and additional parameters.
        /// </param>
        public override void Fit(double[] observations, double[] weights, IFittingOptions options)
        {
            if (options != null)
            {
                throw new ArgumentException("This method does not accept fitting options.");
            }

            var xm = observations.Min();

            var lnx = System.Math.Log(xm);

            double alpha;

            if (weights == null)
            {
                double den = 0;
                for (var i = 0; i < observations.Length; i++)
                {
                    den += System.Math.Log(observations[i]) - lnx;
                }

                alpha = observations.Length / den;
            }
            else
            {
                double den = 0;
                for (var i = 0; i < observations.Length; i++)
                {
                    den += (System.Math.Log(observations[i]) - lnx) * weights[i];
                }

                alpha = weights.Sum() / den;
            }

            init(xm, alpha);
        }
    }
}