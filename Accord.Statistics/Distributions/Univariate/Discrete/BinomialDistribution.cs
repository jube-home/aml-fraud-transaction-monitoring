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
    ///     Binomial probability distribution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The binomial distribution is the discrete probability distribution of the number of
    ///         successes in a sequence of <c>>n</c> independent yes/no experiments, each of which
    ///         yields success with probability <c>p</c>. Such a success/failure experiment is also
    ///         called a Bernoulli experiment or Bernoulli trial; when <c>n = 1</c>, the binomial
    ///         distribution is a <see cref="BernoulliDistribution">Bernoulli distribution</see>.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Binomial_distribution">
    ///                         Wikipedia, The Free Encyclopedia. Binomial distribution. Available on:
    ///                         http://en.wikipedia.org/wiki/Binomial_distribution
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     C. Bishop. “Pattern Recognition and Machine Learning”. Springer. 2006.
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///   // Creates a distribution with n = 16 and success probability 0.12
    ///   var bin = new BinomialDistribution(trials: 16, probability: 0.12);
    ///   
    ///   // Common measures
    ///   double mean = bin.Mean;     // 1.92
    ///   double median = bin.Median; // 2
    ///   double var = bin.Variance;  // 1.6896
    ///   double mode = bin.Mode;     // 2
    ///   
    ///   // Probability mass functions
    ///   double pdf = bin.ProbabilityMassFunction(k: 1); // 0.28218979948821621
    ///   double lpdf = bin.LogProbabilityMassFunction(k: 0); // -2.0453339441581582
    ///   
    ///   // Cumulative distribution functions
    ///   double cdf = bin.DistributionFunction(k: 0);    // 0.12933699143209909
    ///   double ccdf = bin.ComplementaryDistributionFunction(k: 0); // 0.87066300856790091
    ///   
    ///   // Quantile functions
    ///   int icdf0 = bin.InverseDistributionFunction(p: 0.37); // 1
    ///   int icdf1 = bin.InverseDistributionFunction(p: 0.50); // 2
    ///   int icdf2 = bin.InverseDistributionFunction(p: 0.99); // 5
    ///   int icdf3 = bin.InverseDistributionFunction(p: 0.999); // 7
    ///   
    ///   // Hazard (failure rate) functions
    ///   double hf = bin.HazardFunction(x: 0); // 1.3809523809523814
    ///   double chf = bin.CumulativeHazardFunction(x: 0); // 0.86750056770472328
    ///   
    ///   // String representation
    ///   string str = bin.ToString(CultureInfo.InvariantCulture); // "Binomial(x; n = 16, p = 0.12)"
    /// </code>
    /// </example>
    /// <seealso cref="BernoulliDistribution" />
    [Serializable]
    public class BinomialDistribution : UnivariateDiscreteDistribution,
        IFittableDistribution<double>, IFittableDistribution<int>
    {
        // Distribution parameters

        /// <summary>
        ///     Constructs a new <see cref="BinomialDistribution">binomial distribution</see>.
        /// </summary>
        public BinomialDistribution()
            : this(1, 0.5)
        {
        }

        /// <summary>
        ///     Constructs a new <see cref="BinomialDistribution">binomial distribution</see>.
        /// </summary>
        /// <param name="trials">The number of trials <c>n</c>.</param>
        public BinomialDistribution([PositiveInteger] int trials)
            : this(trials, 0.5)
        {
        }

        /// <summary>
        ///     Constructs a new <see cref="BinomialDistribution">binomial distribution</see>.
        /// </summary>
        /// <param name="trials">The number of trials <c>n</c>.</param>
        /// <param name="probability">The success probability <c>p</c> in each trial.</param>
        public BinomialDistribution([PositiveInteger] int trials, [Unit] double probability)
        {
            if (trials <= 0)
            {
                throw new ArgumentOutOfRangeException("trials", "The number of trials should be greater than zero.");
            }

            if (probability < 0 || probability > 1)
            {
                throw new ArgumentOutOfRangeException("probability", "A probability must be between 0 and 1.");
            }

            NumberOfTrials = trials;
            this.ProbabilityOfSuccess = probability;
        }

        /// <summary>
        ///     Gets the number of trials <c>n</c> for the distribution.
        /// </summary>
        public int NumberOfTrials { get; }

        /// <summary>
        ///     Gets the success probability <c>p</c> for the distribution.
        /// </summary>
        public double ProbabilityOfSuccess { get; private set; }

        /// <summary>
        ///     Gets the mean for this distribution.
        /// </summary>
        /// <value>The distribution's mean value.</value>
        public override double Mean
        {
            get => NumberOfTrials * ProbabilityOfSuccess;
            set => throw new NotImplementedException();
        }

        /// <summary>
        ///     Gets the variance for this distribution.
        /// </summary>
        /// <value>The distribution's variance.</value>
        public override double Variance
        {
            get => NumberOfTrials * ProbabilityOfSuccess * (1 - ProbabilityOfSuccess);
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
                var test = (NumberOfTrials + 1) * ProbabilityOfSuccess;

                if (test <= 0 || (int)test != test)
                {
                    return System.Math.Floor(test);
                }

                if (test <= NumberOfTrials)
                {
                    return test; // TODO: should return test and test - 1 (multimodal)
                }

                if (test == NumberOfTrials + 1)
                {
                    return NumberOfTrials;
                }

                return double.NaN;
            }
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
        ///     Gets the support interval for this distribution.
        /// </summary>
        /// <value>
        ///     A <see cref="IntRange" /> containing
        ///     the support interval for this distribution.
        /// </value>
        public override IntRange Support => new(0, NumberOfTrials);

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            return new BinomialDistribution(NumberOfTrials, ProbabilityOfSuccess);
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
        /// <remarks>
        ///     Although both double[] and double[][] arrays are supported,
        ///     providing a double[] for a multivariate distribution or a
        ///     double[][] for a univariate distribution may have a negative
        ///     impact in performance.
        /// </remarks>
        public void Fit(int[] observations, double[] weights)
        {
            Fit(observations, weights, null);
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
            if (k < 0)
            {
                return 0;
            }

            if (k >= NumberOfTrials)
            {
                return 1;
            }

            var x = 1.0 - ProbabilityOfSuccess;
            double a = NumberOfTrials - k;
            double b = k + 1;
            return Beta.Incomplete(a, b, x);
        }

        /// <summary>
        ///     Gets the inverse of the cumulative distribution function (icdf) for
        ///     this distribution evaluated at probability <c>p</c>. This function
        ///     is also known as the Quantile function.
        /// </summary>
        /// <remarks>
        ///     The Inverse Cumulative Distribution Function (ICDF) specifies, for
        ///     a given probability, the value which the random variable will be at,
        ///     or below, with that probability.
        /// </remarks>
        protected override int InnerInverseDistributionFunction(double p)
        {
            var result = NumberOfTrials;

            for (var i = 0; i < NumberOfTrials; i++)
            {
                if (DistributionFunction(i) >= p)
                {
                    result = i;
                    break;
                }
            }

#if DEBUG
            double expected = base.InnerInverseDistributionFunction(p);
            if (result != expected)
            {
                throw new Exception();
            }
#endif
            return result;
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
            var a = Special.LogBinomial(NumberOfTrials, k);
            var b = k == 0 ? 0 : k * System.Math.Log(ProbabilityOfSuccess);
            double c = NumberOfTrials - k;
            var d = System.Math.Log(1 - ProbabilityOfSuccess);
            var log = a + b + c * d;

            return System.Math.Exp(log);
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
            var a = Special.LogBinomial(NumberOfTrials, k);
            var b = k == 0 ? 0 : k * System.Math.Log(ProbabilityOfSuccess);
            double c = NumberOfTrials - k;
            var d = System.Math.Log(1 - ProbabilityOfSuccess);
            var log = a + b + c * d;

            return log;
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
            if (weights != null)
            {
                throw new NotSupportedException("Weighted estimation is not supported.");
            }

            if (options != null)
            {
                throw new ArgumentException("No options may be specified.");
            }

            double sum = 0;
            for (var i = 0; i < observations.Length; i++)
            {
                sum += observations[i] / NumberOfTrials;
            }

            ProbabilityOfSuccess = sum / observations.Length;
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
        public override void Fit(int[] observations, double[] weights, IFittingOptions options)
        {
            if (weights != null)
            {
                throw new NotSupportedException("Weighted estimation is not supported.");
            }

            if (options != null)
            {
                throw new ArgumentException("No options may be specified.");
            }

            double sum = 0;
            for (var i = 0; i < observations.Length; i++)
            {
                sum += observations[i] / (double)NumberOfTrials;
            }

            ProbabilityOfSuccess = sum / observations.Length;
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "Binomial(x; n = {0}, p = {1})",
                NumberOfTrials.ToString(format, formatProvider),
                ProbabilityOfSuccess.ToString(format, formatProvider));
        }
    }
}