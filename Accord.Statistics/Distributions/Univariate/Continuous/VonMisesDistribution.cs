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
using System.ComponentModel;
using Accord.Math;
using Accord.Statistics.Distributions.Fitting;

namespace Accord.Statistics.Distributions.Univariate
{
    /// <summary>
    ///     von-Mises (Circular Normal) distribution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The von Mises distribution (also known as the circular normal distribution
    ///         or Tikhonov distribution) is a continuous probability distribution on the circle.
    ///         It may be thought of as a close approximation to the wrapped normal distribution,
    ///         which is the circular analogue of the normal distribution.
    ///     </para>
    ///     <para>
    ///         The wrapped normal distribution describes the distribution of an angle that
    ///         is the result of the addition of many small independent angular deviations, such as
    ///         target sensing, or grain orientation in a granular material. The von Mises distribution
    ///         is more mathematically tractable than the wrapped normal distribution and is the
    ///         preferred distribution for many applications.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Von_Mises_distribution">
    ///                         Wikipedia, The Free Encyclopedia. Von-Mises distribution. Available on:
    ///                         http://en.wikipedia.org/wiki/Von_Mises_distribution
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://www.kyb.mpg.de/publications/attachments/vmfnote_7045%5B0%5D.pdf">
    ///                         Suvrit Sra, "A short note on parameter approximation for von Mises-Fisher distributions:
    ///                         and a fast implementation of $I_s(x)$". (revision of Apr. 2009). Computational Statistics
    ///                         (2011).
    ///                         Available on: http://www.kyb.mpg.de/publications/attachments/vmfnote_7045%5B0%5D.pdf
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Zheng Sun. M.Sc. Comparing measures of fit for circular distributions. Master thesis, 2006.
    ///                     Available on:
    ///                     https://dspace.library.uvic.ca:8443/bitstream/handle/1828/2698/zhengsun_master_thesis.pdf
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///    // Create a new von-Mises distribution with μ = 0.42 and κ = 1.2
    ///    var vonMises = new VonMisesDistribution(mean: 0.42, concentration: 1.2);
    ///    
    ///    // Common measures
    ///    double mean = vonMises.Mean;     // 0.42
    ///    double median = vonMises.Median; // 0.42
    ///    double var = vonMises.Variance;  // 0.48721760532782921
    ///    
    ///    // Cumulative distribution functions
    ///    double cdf = vonMises.DistributionFunction(x: 1.4); // 0.81326928491589345
    ///    double ccdf = vonMises.ComplementaryDistributionFunction(x: 1.4); // 0.18673071508410655
    ///    double icdf = vonMises.InverseDistributionFunction(p: cdf); // 1.3999999637927665
    ///    
    ///    // Probability density functions
    ///    double pdf = vonMises.ProbabilityDensityFunction(x: 1.4); // 0.2228112141141676
    ///    double lpdf = vonMises.LogProbabilityDensityFunction(x: 1.4); // -1.5014304395467863
    ///    
    ///    // Hazard (failure rate) functions
    ///    double hf = vonMises.HazardFunction(x: 1.4); // 1.1932220899695576
    ///    double chf = vonMises.CumulativeHazardFunction(x: 1.4); // 1.6780877262500649
    ///    
    ///    // String representation
    ///    string str = vonMises.ToString(CultureInfo.InvariantCulture); // VonMises(x; μ = 0.42, κ = 1.2)
    /// </code>
    /// </example>
    /// <seealso cref="Circular" />
    [Serializable]
    public class VonMisesDistribution : UnivariateContinuousDistribution,
        IFittableDistribution<double, VonMisesOptions>
    {
        // Derived measures
        private double constant;
        private double? entropy;

        // Distribution parameters
        private double mean; // μ (mu)

        // Distribution measures
        private double? variance;

        /// <summary>
        ///     Constructs a von-Mises distribution with zero mean and unit concentration.
        /// </summary>
        public VonMisesDistribution()
        {
            initialize(0, 1);
        }

        /// <summary>
        ///     Constructs a von-Mises distribution with zero mean.
        /// </summary>
        /// <param name="concentration">The concentration value κ (kappa). Default is 1.</param>
        public VonMisesDistribution([Positive] double concentration)
        {
            initialize(0, concentration);
        }

        /// <summary>
        ///     Constructs a von-Mises distribution.
        /// </summary>
        /// <param name="mean">The mean value μ (mu). Default is 0.</param>
        /// <param name="concentration">The concentration value κ (kappa). Default is 1.</param>
        public VonMisesDistribution([Real] [DefaultValue(0)] double mean,
            [Positive] [DefaultValue(1)] double concentration)
        {
            initialize(mean, concentration);
        }

        /// <summary>
        ///     Gets the mean value μ (mu) for this distribution.
        /// </summary>
        public override double Mean
        {
            get => mean;
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the median value μ (mu) for this distribution.
        /// </summary>
        public override double Median => mean;

        /// <summary>
        ///     Gets the mode value μ (mu) for this distribution.
        /// </summary>
        public override double Mode
        {
            get => mean;
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the concentration κ (kappa) for this distribution.
        /// </summary>
        public double Concentration { get; private set; }

        /// <summary>
        ///     Gets the variance for this distribution.
        /// </summary>
        /// <remarks>
        ///     The von-Mises Variance is defined in terms of the
        ///     <see cref="Bessel.I(double)">
        ///         Bessel function of the first
        ///         kind In(x)
        ///     </see>
        ///     as <c>var = 1 - I(1, κ) / I(0, κ)</c>
        /// </remarks>
        public override double Variance
        {
            get
            {
                if (!variance.HasValue)
                {
                    var i1 = Bessel.I(1, Concentration);
                    var i0 = Bessel.I0(Concentration);
                    var a = i1 / i0;
                    variance = 1.0 - a;
                }

                return variance.Value;
            }
            set { } //Not Implemented.
        }

        /// <summary>
        ///     Gets the entropy for this distribution.
        /// </summary>
        public override double Entropy
        {
            get
            {
                if (!entropy.HasValue)
                {
                    var i1 = Bessel.I(1, Concentration);
                    var i0 = Bessel.I0(Concentration);
                    var a = i1 / i0;
                    entropy = -Concentration * a + System.Math.Log(2 * System.Math.PI * i0);
                }

                return entropy.Value;
            }
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
            get => new(mean - System.Math.PI, mean + System.Math.PI);
            set { } //Not Implemented.
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
        /// <remarks>
        ///     Although both double[] and double[][] arrays are supported,
        ///     providing a double[] for a multivariate distribution or a
        ///     double[][] for a univariate distribution may have a negative
        ///     impact in performance.
        /// </remarks>
        public void Fit(double[] observations, double[] weights, VonMisesOptions options)
        {
            double m, k;

            if (weights != null)
            {
                m = Circular.WeightedMean(observations, weights);
                k = Circular.WeightedConcentration(observations, weights, m);
            }
            else
            {
                m = Circular.Mean(observations);
                k = Circular.Concentration(observations, m);
            }

            if (options != null)
            {
                // Parse optional estimation options
                if (options.UseBiasCorrection)
                {
                    double N = observations.Length;
                    if (k < 2)
                    {
                        k = System.Math.Max(k - 1.0 / (2.0 * (N * k)), 0);
                    }
                    else
                    {
                        var Nm1 = N - 1;
                        k = Nm1 * Nm1 * Nm1 * k / (N * N * N + N);
                    }
                }
            }

            initialize(m, k);
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            return new VonMisesDistribution(mean, Concentration);
        }

        private void initialize(double m, double k)
        {
            mean = m;
            Concentration = k;

            variance = null;
            constant = 1.0 / (2.0 * System.Math.PI * Bessel.I0(Concentration));
        }

        /// <summary>
        ///     Gets the cumulative distribution function (cdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">A single point in the distribution range.</param>
        protected internal override double InnerDistributionFunction(double x)
        {
            return DistributionFunction(x, mean, Concentration);
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
            var z = x - mean;
            return constant * System.Math.Exp(Concentration * System.Math.Cos(z));
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
            return System.Math.Log(constant) + Concentration * System.Math.Cos(x - mean);
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
        /// <remarks>
        ///     Although both double[] and double[][] arrays are supported,
        ///     providing a double[] for a multivariate distribution or a
        ///     double[][] for a univariate distribution may have a negative
        ///     impact in performance.
        /// </remarks>
        public override void Fit(double[] observations, double[] weights, IFittingOptions options)
        {
            var misesOptions = options as VonMisesOptions;
            if (options != null && misesOptions == null)
            {
                throw new ArgumentException("The specified options' type is invalid.", "options");
            }

            Fit(observations, weights, misesOptions);
        }

        /// <summary>
        ///     Creates a new circular uniform distribution by creating a
        ///     new <see cref="VonMisesDistribution" /> with zero kappa.
        /// </summary>
        /// <param name="mean">The mean value μ (mu).</param>
        /// <returns>
        ///     A <see cref="VonMisesDistribution" /> with zero kappa, which
        ///     is equivalent to creating an uniform circular distribution.
        /// </returns>
        public static VonMisesDistribution CircularUniform(double mean)
        {
            return new VonMisesDistribution(mean, 0);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "VonMises(x; μ = {0}, κ = {1})",
                mean.ToString(format, formatProvider),
                Concentration.ToString(format, formatProvider));
        }

        /// <summary>
        ///     Estimates a new von-Mises distribution from a given set of angles.
        /// </summary>
        public static VonMisesDistribution Estimate(double[] angles)
        {
            return Estimate(angles, null, null);
        }

        /// <summary>
        ///     Estimates a new von-Mises distribution from a given set of angles.
        /// </summary>
        public static VonMisesDistribution Estimate(double[] angles, VonMisesOptions options)
        {
            return Estimate(angles, null, options);
        }

        /// <summary>
        ///     Estimates a new von-Mises distribution from a given set of angles.
        /// </summary>
        public static VonMisesDistribution Estimate(double[] angles, double[] weights, VonMisesOptions options)
        {
            var vonMises = new VonMisesDistribution();
            vonMises.Fit(angles, weights, options);
            return vonMises;
        }

        /// <summary>
        ///     von-Mises cumulative distribution function.
        /// </summary>
        /// <remarks>
        ///     This method implements the Von-Mises CDF calculation code
        ///     as given by Geoffrey Hill on his original FORTRAN code and
        ///     shared under the GNU LGPL license.
        ///     <para>
        ///         References:
        ///         <list type="bullet">
        ///             <item>
        ///                 <description>
        ///                     Geoffrey Hill, ACM TOMS Algorithm 518,
        ///                     Incomplete Bessel Function I0: The von Mises Distribution,
        ///                     ACM Transactions on Mathematical Software, Volume 3, Number 3,
        ///                     September 1977, pages 279-284.
        ///                 </description>
        ///             </item>
        ///         </list>
        ///     </para>
        /// </remarks>
        /// <param name="x">The point where to calculate the CDF.</param>
        /// <param name="mu">The location parameter μ (mu).</param>
        /// <param name="kappa">The concentration parameter κ (kappa).</param>
        /// <returns>The value of the von-Mises CDF at point <paramref name="x" />.</returns>
        public static double DistributionFunction(double x, double mu, double kappa)
        {
            var a1 = 12.0;
            var a2 = 0.8;
            var a3 = 8.0;
            var a4 = 1.0;
            var c1 = 56.0;
            var ck = 10.5;
            double cdf;

            if (x - mu <= -System.Math.PI)
            {
                return 0;
            }

            if (System.Math.PI <= x - mu)
            {
                return 1.0;
            }

            var z = kappa;

            var u = (x - mu + System.Math.PI) % (2.0 * System.Math.PI);

            if (u < 0.0)
            {
                u = u + 2.0 * System.Math.PI;
            }

            var y = u - System.Math.PI;

            if (z <= ck)
            {
                var v = 0.0;

                if (0.0 < z)
                {
                    var ip = System.Math.Floor(z * a2 - a3 / (z + a4) + a1);
                    var p = ip;
                    var s = System.Math.Sin(y);
                    var c = System.Math.Cos(y);
                    y = p * y;
                    var sn = System.Math.Sin(y);
                    var cn = System.Math.Cos(y);
                    var r = 0.0;
                    z = 2.0 / z;

                    for (var n = 2; n <= ip; n++)
                    {
                        p = p - 1.0;
                        y = sn;
                        sn = sn * c - cn * s;
                        cn = cn * c + y * s;
                        r = 1.0 / (p * z + r);
                        v = (sn / p + v) * r;
                    }
                }

                cdf = (u * 0.5 + v) / System.Math.PI;
            }
            else
            {
                var c = 24.0 * z;
                var v = c - c1;
                var r = System.Math.Sqrt((54.0 / (347.0 / v + 26.0 - c) - 6.0 + c) / 12.0);
                z = System.Math.Sin(0.5 * y) * r;
                var s = 2.0 * z * z;
                v = v - s + 3.0;
                y = (c - s - s - 16.0) / 3.0;
                y = ((s + 1.75) * s + 83.5) / v - y;
                var arg = z * (1.0 - s / (y * y));
                var erfx = Special.Erf(arg);
                cdf = 0.5 * erfx + 0.5;
            }

            cdf = System.Math.Max(cdf, 0.0);
            cdf = System.Math.Min(cdf, 1.0);

            return cdf;
        }
    }
}