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
    ///     Hypergeometric probability distribution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The hypergeometric distribution is a discrete probability distribution that
    ///         describes the probability of <c>k</c> successes in <c>n</c> draws from a finite population
    ///         without replacement. This is in contrast to the
    ///         <see cref="BinomialDistribution">
    ///             binomial distribution
    ///         </see>
    ///         , which describes the probability of <c>k</c> successes
    ///         in <c>n</c> draws with replacement.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Hypergeometric_distribution">
    ///                         Wikipedia, The Free Encyclopedia. Hypergeometric distribution. Available on:
    ///                         http://en.wikipedia.org/wiki/Hypergeometric_distribution
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///    // Distribution parameters
    ///    int populationSize = 15; // population size N
    ///    int success = 7;         // number of successes in the sample  
    ///    int samples = 8;         // number of samples drawn from N
    ///    
    ///    // Create a new Hypergeometric distribution with N = 15, n = 8, and s = 7
    ///    var dist = new HypergeometricDistribution(populationSize, success, samples);
    ///    
    ///    // Common measures
    ///    double mean   = dist.Mean;     // 1.3809523809523812
    ///    double median = dist.Median;   // 4.0
    ///    double var    = dist.Variance; // 3.2879818594104315
    ///    double mode   = dist.Mode;     // 4.0
    ///    
    ///    // Cumulative distribution functions
    ///    double cdf = dist.DistributionFunction(k: 2);               // 0.80488799999999994
    ///    double ccdf = dist.ComplementaryDistributionFunction(k: 2); // 0.19511200000000006
    ///    
    ///    // Probability mass functions
    ///    double pdf1 = dist.ProbabilityMassFunction(k: 4); // 0.38073038073038074
    ///    double pdf2 = dist.ProbabilityMassFunction(k: 5); // 0.18275058275058276
    ///    double pdf3 = dist.ProbabilityMassFunction(k: 6); // 0.030458430458430458
    ///    double lpdf = dist.LogProbabilityMassFunction(k: 2); // -2.3927801721315989
    ///    
    ///    // Quantile function
    ///    int icdf1 = dist.InverseDistributionFunction(p: 0.17); // 3
    ///    int icdf2 = dist.InverseDistributionFunction(p: 0.46); // 4
    ///    int icdf3 = dist.InverseDistributionFunction(p: 0.87); // 5
    ///    
    ///    // Hazard (failure rate) functions
    ///    double hf = dist.HazardFunction(x: 4); // 1.7753623188405792
    ///    double chf = dist.CumulativeHazardFunction(x: 4); // 1.5396683418789763
    ///    
    ///    // String representation
    ///    string str = dist.ToString(CultureInfo.InvariantCulture); // "HyperGeometric(x; N = 15, m = 7, n = 8)"
    /// </code>
    /// </example>
    /// <seealso cref="BinomialDistribution" />
    /// <seealso cref="GeometricDistribution" />
    [Serializable]
    public class HypergeometricDistribution : UnivariateDiscreteDistribution,
        IFittableDistribution<double, HypergeometricOptions>
    {
        // Distribution parameters

        /// <summary>
        ///     Constructs a new <see cref="HypergeometricDistribution">Hypergeometric distribution</see>. To build from
        ///     the number of failures and succcesses like in R, please see <see cref="FromSuccessCounts(int, int, int)" />.
        /// </summary>
        /// <param name="populationSize">Size <c>N</c> of the population.</param>
        /// <param name="successes">The number <c>m</c> of successes in the population.</param>
        /// <param name="samples">The number <c>n</c> of samples drawn from the population.</param>
        public HypergeometricDistribution([PositiveInteger] int populationSize,
            [NonnegativeInteger] int successes, [PositiveInteger] int samples)
        {
            if (populationSize <= 0)
            {
                throw new ArgumentOutOfRangeException("populationSize",
                    "Size of the population should be greater than zero.");
            }

            if (samples <= 0)
            {
                throw new ArgumentOutOfRangeException("samples",
                    "The number of samples drawn from the population should be greater than zero.");
            }

            if (samples > populationSize)
            {
                throw new ArgumentOutOfRangeException("samples",
                    "The number of samples can not exceed the total size of the population.");
            }

            if (successes < 0 || successes > populationSize)
            {
                throw new ArgumentOutOfRangeException("successes",
                    "The number of successes in the population should range from zero to the size of the population.");
            }

            PopulationSize = populationSize;
            SampleSize = samples;
            PopulationSuccess = successes;
        }

        /// <summary>
        ///     Gets the size <c>N</c> of the
        ///     population for this distribution.
        /// </summary>
        public int PopulationSize { get; private set; }

        /// <summary>
        ///     Gets the size <c>n</c> of the sample drawn
        ///     from <see cref="PopulationSize">N</see>.
        /// </summary>
        public int SampleSize { get; }

        /// <summary>
        ///     Gets the count of success trials in the
        ///     population for this distribution. This
        ///     is often referred as <c>m</c>.
        /// </summary>
        public int PopulationSuccess { get; private set; }

        /// <summary>
        ///     Gets the mean for this distribution.
        /// </summary>
        /// <value>The distribution's mean value.</value>
        public override double Mean
        {
            get => SampleSize * (PopulationSuccess / (double)PopulationSize);
            set => throw new NotImplementedException();
        }

        /// <summary>
        ///     Gets the variance for this distribution.
        /// </summary>
        /// <value>The distribution's variance.</value>
        public override double Variance
        {
            get => SampleSize * (PopulationSuccess / (double)PopulationSize) *
                   ((PopulationSize - PopulationSuccess) / (double)PopulationSize) *
                   ((PopulationSize - SampleSize) / (PopulationSize - 1.0));
            set => throw new NotImplementedException();
        }

        /// <summary>
        ///     Gets the entropy for this distribution.
        /// </summary>
        /// <value>The distribution's entropy.</value>
        public override double Entropy
        {
            get => throw new NotSupportedException();
            set => throw new NotImplementedException();
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
                double num = (SampleSize + 1) * (PopulationSuccess + 1);
                double den = PopulationSize + 2;
                return System.Math.Floor(num / den);
            }
            set => throw new NotImplementedException();
        }

        /// <summary>
        ///     Gets the support interval for this distribution.
        /// </summary>
        /// <value>
        ///     A <see cref="IntRange" /> containing
        ///     the support interval for this distribution.
        /// </value>
        public override IntRange Support => new(System.Math.Max(0, SampleSize + PopulationSuccess - PopulationSize),
            System.Math.Min(PopulationSuccess, SampleSize));

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
        public void Fit(double[] observations, double[] weights, HypergeometricOptions options)
        {
            if (weights != null)
            {
                throw new NotSupportedException("Weighted estimation is not supported.");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options",
                    "Please specify which parameter to estimate.");
            }

            var successes = 0;
            for (var i = 0; i < observations.Length; i++)
            {
                if (observations[i] == 1)
                {
                    successes++;
                }
            }

            if (options.Parameter == HypergeometricParameter.PopulationSize)
            {
                // Estimate N (population size)
                var newN = (int)System.Math.Truncate(SampleSize * PopulationSuccess / (double)successes);
                PopulationSize = newN;
            }
            else if (options.Parameter == HypergeometricParameter.PopulationSuccesses)
            {
                // Estimate m (number of successes in population)
                var newM = (int)System.Math.Truncate(successes * (PopulationSize + 1.0) / SampleSize);
                PopulationSuccess = newM;
            }
            else
            {
                throw new InvalidOperationException("Unexpected parameter.");
            }
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            return new HypergeometricDistribution(PopulationSize, PopulationSuccess, SampleSize);
        }

        /// <summary>
        ///     Creates a new <see cref="HypergeometricDistribution" /> from the number of successes, failures,
        ///     and the number of samples drawn. This is the same parameterization used in R.
        /// </summary>
        /// <param name="successes">The number <c>m</c> of successes in the population.</param>
        /// <param name="failures">The number <c>m</c> of failures in the population.</param>
        /// <param name="samples">The number <c>n</c> of samples drawn from the population.</param>
        public static HypergeometricDistribution FromSuccessCounts(int successes, int failures, int samples)
        {
            return new HypergeometricDistribution(failures + successes, successes, samples);
        }

        /// <summary>
        ///     Gets the cumulative distribution function (cdf) for
        ///     this distribution evaluated at point <c>k</c>.
        /// </summary>
        /// <param name="k">A single point in the distribution range.</param>
        /// <remarks>
        ///     The Cumulative Distribution Function (CDF) describes the cumulative
        ///     probability that a given value or any value smaller than it will occur.
        /// </remarks>
        protected internal override double InnerDistributionFunction(int k)
        {
            return BaseDistributionFunction(k);
        }

        /// <summary>
        ///     Gets the probability mass function (pmf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="k">A single point in the distribution range.</param>
        /// <returns>
        ///     The probability of <c>k</c> occurring
        ///     in the current distribution.
        /// </returns>
        /// <remarks>
        ///     The Probability Mass Function (PMF) describes the
        ///     probability that a given value <c>x</c> will occur.
        /// </remarks>
        protected internal override double InnerProbabilityMassFunction(int k)
        {
            var a = Special.Binomial(PopulationSuccess, k);
            var b = Special.Binomial(PopulationSize - PopulationSuccess, SampleSize - k);
            var c = Special.Binomial(PopulationSize, SampleSize);

            return a * b / c;
        }

        /// <summary>
        ///     Gets the log-probability mass function (pmf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="k">A single point in the distribution range.</param>
        /// <returns>
        ///     The logarithm of the probability of <c>x</c>
        ///     occurring in the current distribution.
        /// </returns>
        /// <remarks>
        ///     The Probability Mass Function (PMF) describes the
        ///     probability that a given value <c>k</c> will occur.
        /// </remarks>
        protected internal override double InnerLogProbabilityMassFunction(int k)
        {
            var a = Special.LogBinomial(PopulationSuccess, k);
            var b = Special.LogBinomial(PopulationSize - PopulationSuccess, SampleSize - k);
            var c = Special.LogBinomial(PopulationSize, SampleSize);

            return a + b - c;
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
            var geometricOptions = options as HypergeometricOptions;
            if (options != null && geometricOptions == null)
            {
                throw new ArgumentException("The specified options' type is invalid.", "options");
            }

            Fit(observations, weights, geometricOptions);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "HyperGeometric(x; N = {0}, m = {1}, n = {2})",
                PopulationSize.ToString(format, formatProvider),
                PopulationSuccess.ToString(format, formatProvider),
                SampleSize.ToString(format, formatProvider));
        }
    }
}