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

namespace Accord.Statistics.Distributions.Multivariate
{
    /// <summary>
    ///     Multinomial probability distribution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The multinomial distribution is a generalization of the binomial
    ///         distribution. The binomial distribution is the probability distribution
    ///         of the number of "successes" in <c>n</c> independent
    ///         <see cref="Univariate.BernoulliDistribution">Bernoulli</see>
    ///         trials, with the same probability of "success" on each trial.
    ///     </para>
    ///     <para>
    ///         In a multinomial distribution, the analog of the
    ///         <see cref="Univariate.BernoulliDistribution">Bernoulli distribution</see> is the
    ///         <see cref="Univariate.GeneralDiscreteDistribution">categorical distribution</see>,
    ///         where each trial results in exactly one of some fixed finite number
    ///         <c>k</c> of possible outcomes, with probabilities <c>p1, ..., pk</c>
    ///         and there are <c>n</c> independent trials.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Multinomial_distribution">
    ///                         Wikipedia, The Free Encyclopedia. Multinomial distribution. Available on:
    ///                         http://en.wikipedia.org/wiki/Multinomial_distribution
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///    // distribution parameters
    ///    int numberOfTrials = 5; 
    ///    double[] probabilities = { 0.25, 0.75 };
    ///    
    ///    // Create a new Multinomial distribution with 5 trials for 2 symbols
    ///    var dist = new MultinomialDistribution(numberOfTrials, probabilities);
    ///    
    ///    int dimensions = dist.Dimension; // 2
    ///    
    ///    double[] mean = dist.Mean;     // {  1.25, 3.75 }
    ///    double[] median = dist.Median; // {  1.25, 3.75 }
    ///    double[] var = dist.Variance;  // { -0.9375, -0.9375 }
    ///    
    ///    double pdf1 = dist.ProbabilityMassFunction(new[] { 2, 3 }); // 0.26367187499999994
    ///    double pdf2 = dist.ProbabilityMassFunction(new[] { 1, 4 }); // 0.3955078125
    ///    double pdf3 = dist.ProbabilityMassFunction(new[] { 5, 0 }); // 0.0009765625
    ///    double lpdf = dist.LogProbabilityMassFunction(new[] { 1, 4 }); // -0.9275847384929139
    ///    
    ///    // output is "Multinomial(x; n = 5, p = { 0.25, 0.75 })"
    ///    string str = dist.ToString(CultureInfo.InvariantCulture); 
    /// </code>
    /// </example>
    /// <seealso cref="Univariate.BernoulliDistribution" />
    /// <seealso cref="Univariate.GeneralDiscreteDistribution" />
    [Serializable]
    public class MultinomialDistribution : MultivariateDiscreteDistribution,
        IFittableDistribution<double[], IFittingOptions>
    {
        // distribution parameters
        private double[,] covariance;

        // derived measures
        private double lnfac;
        private double[] mean;
        private double[] variance;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MultinomialDistribution" /> class.
        /// </summary>
        /// <param name="numberOfTrials">The total number of trials N.</param>
        /// <param name="probabilities">A vector containing the probabilities of seeing each of possible outcomes.</param>
        public MultinomialDistribution(int numberOfTrials, params double[] probabilities)
            : base(probabilities.Length)
        {
            initialize(numberOfTrials, probabilities);
        }

        /// <summary>
        ///     Gets the event probabilities associated with the trials.
        /// </summary>
        public double[] Probabilities { get; private set; }

        /// <summary>
        ///     Gets the number of Bernoulli trials N.
        /// </summary>
        public int NumberOfTrials { get; private set; }

        /// <summary>
        ///     Gets the mean for this distribution.
        /// </summary>
        public override double[] Mean
        {
            get
            {
                if (mean == null)
                {
                    mean = new double[Probabilities.Length];
                    for (var i = 0; i < mean.Length; i++)
                    {
                        mean[i] = NumberOfTrials * Probabilities[i];
                    }
                }

                return mean;
            }
        }

        /// <summary>
        ///     Gets the variance vector for this distribution.
        /// </summary>
        public override double[] Variance
        {
            get
            {
                if (variance == null)
                {
                    variance = new double[Probabilities.Length];
                    for (var i = 0; i < variance.Length; i++)
                    {
                        variance[i] = NumberOfTrials * Probabilities[i] * (1.0 - Probabilities[i]);
                    }
                }

                return variance;
            }
        }

        /// <summary>
        ///     Gets the variance-covariance matrix for this distribution.
        /// </summary>
        public override double[,] Covariance
        {
            get
            {
                if (covariance == null)
                {
                    var k = Probabilities.Length;
                    covariance = new double[k, k];
                    for (var i = 0; i < k; i++)
                    {
                        for (var j = 0; j < k; j++)
                        {
                            covariance[i, j] = i == j
                                ? NumberOfTrials * Probabilities[i] * (1.0 - Probabilities[i])
                                : -NumberOfTrials * Probabilities[j] * Probabilities[i];
                        }
                    }
                }

                return covariance;
            }
        }

        /// <summary>
        ///     Gets the support interval for this distribution.
        /// </summary>
        /// <value>
        ///     A <see cref="IntRange" /> containing
        ///     the support interval for this distribution.
        /// </value>
        public override IntRange[] Support
        {
            get
            {
                var range = new IntRange[Dimension];
                for (var i = 0; i < range.Length; i++)
                {
                    range[i] = new IntRange(0, NumberOfTrials);
                }

                return range;
            }
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
        public override void Fit(double[][] observations, double[] weights, IFittingOptions options)
        {
            if (options != null)
            {
                throw new ArgumentException("This method does not accept fitting options.");
            }

            var pi = new double[Probabilities.Length];

            if (weights == null)
            {
                for (var c = 0; c < Probabilities.Length; c++)
                {
                    for (var i = 0; i < observations.Length; i++)
                    {
                        pi[c] += observations[i][c];
                    }

                    pi[c] /= NumberOfTrials;
                }
            }
            else
            {
                double size = weights.Length;
                for (var c = 0; c < Probabilities.Length; c++)
                {
                    for (var i = 0; i < observations.Length; i++)
                    {
                        pi[c] += observations[i][c] * weights[i] * size;
                    }

                    pi[c] /= NumberOfTrials;
                }
            }

            initialize(NumberOfTrials, pi);
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            return new MultinomialDistribution(NumberOfTrials, Probabilities);
        }

        private void initialize(int n, double[] prob)
        {
            NumberOfTrials = n;
            Probabilities = prob;
            lnfac = Special.LogFactorial(n);

            mean = null;
            variance = null;
            covariance = null;
        }

        /// <summary>
        ///     Not supported.
        /// </summary>
        protected internal override double InnerDistributionFunction(int[] x)
        {
            // TODO: Implement an approximation of the multinomial CDF
            //  "A Representation for Multinomial Cumulative Distribution Functions",  
            //  Bruce Levin, The Annals of Statistics, v.9, n.5, pp.1123-1126, 1981
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Gets the probability mass function (pmf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">A single point in the distribution range.</param>
        /// <returns>
        ///     The probability of <c>x</c> occurring
        ///     in the current distribution.
        /// </returns>
        /// <remarks>
        ///     The Probability Mass Function (PMF) describes the
        ///     probability that a given value <c>x</c> will occur.
        /// </remarks>
        protected internal override double InnerProbabilityMassFunction(int[] x)
        {
            if (x.Length != Dimension)
            {
                throw new DimensionMismatchException("x",
                    "Input length must match the number of dimensions in this distribution.");
            }

            if (x.Sum() != NumberOfTrials)
            {
                throw new ArgumentException("Elements must sum up to the number of trials (" + NumberOfTrials + ")",
                    "x");
            }

            double theta = 0;
            double prod = 0;
            for (var i = 0; i < x.Length; i++)
            {
                theta += Special.LogFactorial(x[i]);
                prod += x[i] * System.Math.Log(Probabilities[i]);
            }

            return System.Math.Exp(lnfac - theta + prod);
        }

        /// <summary>
        ///     Gets the log-probability mass function (pmf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">A single point in the distribution range.</param>
        /// <returns>
        ///     The logarithm of the probability of <c>x</c>
        ///     occurring in the current distribution.
        /// </returns>
        /// <remarks>
        ///     The Probability Mass Function (PMF) describes the
        ///     probability that a given value <c>x</c> will occur.
        /// </remarks>
        protected internal override double InnerLogProbabilityMassFunction(int[] x)
        {
            if (x.Length != Dimension)
            {
                throw new DimensionMismatchException("x",
                    "Input length must match the number of dimensions in this distribution.");
            }

            double theta = 0;
            double prod = 0;
            for (var i = 0; i < x.Length; i++)
            {
                theta += Special.LogFactorial(x[i]);
                prod += x[i] * System.Math.Log(Probabilities[i]);
            }

            return lnfac - theta + prod;
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString(string format, IFormatProvider formatProvider)
        {
            var provider = new CSharpArrayFormatProvider(formatProvider,
                false, false);

            return string.Format(formatProvider, "Multinomial(x; n = {0}, p = {1})",
                NumberOfTrials.ToString(format, provider),
                Probabilities.ToString(format, provider));
        }
    }
}