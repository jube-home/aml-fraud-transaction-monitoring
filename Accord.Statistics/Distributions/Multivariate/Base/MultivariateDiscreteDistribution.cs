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

// A note on compatibility: Up to version 3.5, users were supposed to implement their own probability
// distributions by inheriting from this class and overriding the public members ProbabilityDensityFunction,
// DistributionFunction, etc. However, since those were public methods this meant that users (and I) had to
// write validation checks on every method override, resulting in lots of duplicated code. Starting from version
// 3.6, users should override methods that start with "Inner" in their name, such as InnerProbabilityDensityFunction 
// and InnerDistributionFunction. The framework will have already validated the inputs of those functions, and
// will also take care to check whether the implementation of those functions is correct.

// For now, compatibility mode is enabled for release builds, meaning that old code that has been written
// using the old way will keep working. However, debug (development) builds will have this feature turned
// off to force new classes to be implemented using this new way.

# if !DEBUG
#define COMPATIBILITY
#endif

using System;
using Accord.Math;
using Accord.Math.Random;
using Accord.Statistics.Distributions.Fitting;
using Accord.Statistics.Distributions.Sampling;

namespace Accord.Statistics.Distributions.Multivariate
{
    /// <summary>
    ///     Abstract class for multivariate discrete probability distributions.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A probability distribution identifies either the probability of each value of an
    ///         unidentified random variable (when the variable is discrete), or the probability
    ///         of the value falling within a particular interval (when the variable is continuous).
    ///     </para>
    ///     <para>
    ///         The probability distribution describes the range of possible values that a random
    ///         variable can attain and the probability that the value of the random variable is
    ///         within any (measurable) subset of that range.
    ///     </para>
    ///     <para>
    ///         The function describing the probability that a given discrete value will
    ///         occur is called the probability function (or probability mass function,
    ///         abbreviated PMF), and the function describing the cumulative probability
    ///         that a given value or any value smaller than it will occur is called the
    ///         distribution function (or cumulative distribution function, abbreviated CDF).
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Probability_distribution">
    ///                         Wikipedia, The Free Encyclopedia. Probability distribution. Available on:
    ///                         http://en.wikipedia.org/wiki/Probability_distribution
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://mathworld.wolfram.com/StatisticalDistribution.html">
    ///                         Weisstein, Eric W. "Statistical Distribution." From MathWorld--A Wolfram Web Resource.
    ///                         http://mathworld.wolfram.com/StatisticalDistribution.html
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    [Serializable]
    public abstract class MultivariateDiscreteDistribution : DistributionBase,
        IMultivariateDistribution, IMultivariateDistribution<int[]>,
        IMultivariateDistribution<double[]>,
        ISampleableDistribution<int[]>, ISampleableDistribution<double[]>,
        IDistribution<double[]>, IFormattable
    {
        [NonSerialized] private MetropolisHasting<int> generator;

        /// <summary>
        ///     Constructs a new MultivariateDiscreteDistribution class.
        /// </summary>
        protected MultivariateDiscreteDistribution(int dimension)
        {
            this.Dimension = dimension;
        }

        /// <summary>
        ///     Gets the support interval for this distribution.
        /// </summary>
        /// <value>
        ///     A <see cref="IntRange" /> containing
        ///     the support interval for this distribution.
        /// </value>
        public abstract IntRange[] Support { get; }

        /// <summary>
        ///     Gets the number of variables for this distribution.
        /// </summary>
        public int Dimension { get; }

        /// <summary>
        ///     Gets the mean for this distribution.
        /// </summary>
        /// <value>
        ///     An array of double-precision values containing
        ///     the mean values for this distribution.
        /// </value>
        public abstract double[] Mean { get; }

        /// <summary>
        ///     Gets the mean for this distribution.
        /// </summary>
        /// <value>
        ///     An array of double-precision values containing
        ///     the variance values for this distribution.
        /// </value>
        public abstract double[] Variance { get; }

        /// <summary>
        ///     Gets the variance for this distribution.
        /// </summary>
        /// <value>
        ///     An multidimensional array of double-precision values
        ///     containing the covariance values for this distribution.
        /// </value>
        public abstract double[,] Covariance { get; }

        /// <summary>
        ///     Gets the mode for this distribution.
        /// </summary>
        /// <value>
        ///     An array of double-precision values containing
        ///     the mode values for this distribution.
        /// </value>
        public virtual double[] Mode => Mean;

        /// <summary>
        ///     Gets the median for this distribution.
        /// </summary>
        /// <value>
        ///     An array of double-precision values containing
        ///     the median values for this distribution.
        /// </value>
        public virtual double[] Median => Mean;

        double IDistribution<double[]>.DistributionFunction(double[] x)
        {
            return (this as IDistribution).DistributionFunction(x);
        }

        double IDistribution<double[]>.ProbabilityFunction(double[] x)
        {
            return (this as IDistribution).ProbabilityFunction(x);
        }

        double IDistribution<double[]>.LogProbabilityFunction(double[] x)
        {
            return (this as IDistribution).LogProbabilityFunction(x);
        }

        double IDistribution<double[]>.ComplementaryDistributionFunction(double[] x)
        {
            return (this as IDistribution).ComplementaryDistributionFunction(x);
        }

        /// <summary>
        ///     Gets the cumulative distribution function (cdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">
        ///     A single point in the distribution range.
        /// </param>
        /// <remarks>
        ///     The Cumulative Distribution Function (CDF) describes the cumulative
        ///     probability that a given value or any value smaller than it will occur.
        /// </remarks>
        public
#if COMPATIBILITY
        virtual
#endif
            double DistributionFunction(int[] x)
        {
            if (x == null)
            {
                throw new ArgumentNullException("x");
            }

            return InnerDistributionFunction(x);
        }

        /// <summary>
        ///     Gets the complementary cumulative distribution function
        ///     (ccdf) for this distribution evaluated at point <c>x</c>.
        ///     This function is also known as the Survival function.
        /// </summary>
        /// <remarks>
        ///     The Complementary Cumulative Distribution Function (CCDF) is
        ///     the complement of the Cumulative Distribution Function, or 1
        ///     minus the CDF.
        /// </remarks>
        public
#if COMPATIBILITY
        virtual
#endif
            double ComplementaryDistributionFunction(int[] x)
        {
            if (x == null)
            {
                throw new ArgumentNullException("x");
            }

            return InnerComplementaryDistributionFunction(x);
        }

        double IDistribution<int[]>.ProbabilityFunction(int[] x)
        {
            return ProbabilityMassFunction(x);
        }

        double IDistribution<int[]>.LogProbabilityFunction(int[] x)
        {
            return LogProbabilityMassFunction(x);
        }

        /// <summary>
        ///     Generates a random vector of observations from the current distribution.
        /// </summary>
        /// <param name="samples">The number of samples to generate.</param>
        /// <param name="result">The location where to store the samples.</param>
        /// <returns>A random vector of observations drawn from this distribution.</returns>
        public double[][] Generate(int samples, double[][] result)
        {
            return Generate(samples, result, Generator.Random);
        }

        /// <summary>
        ///     Generates a random observation from the current distribution.
        /// </summary>
        /// <returns>A random observations drawn from this distribution.</returns>
        public double[] Generate(double[] result)
        {
            return Generate(result, Generator.Random);
        }

        /// <summary>
        ///     Generates a random vector of observations from the current distribution.
        /// </summary>
        /// <param name="samples">The number of samples to generate.</param>
        /// <param name="result">The location where to store the samples.</param>
        /// <param name="source">
        ///     The random number generator to use as a source of randomness.
        ///     Default is to use <see cref="Generator.Random" />.
        /// </param>
        /// <returns>A random vector of observations drawn from this distribution.</returns>
        public virtual double[][] Generate(int samples, double[][] result, Random source)
        {
            return Generate(samples, source).ToDouble(result);
        }

        /// <summary>
        ///     Generates a random observation from the current distribution.
        /// </summary>
        /// <returns>A random observations drawn from this distribution.</returns>
        public double[] Generate(double[] result, Random source)
        {
            return Generate(1, new[] { result }, source)[0];
        }

        double[] IRandomNumberGenerator<double[]>.Generate()
        {
            return Generate(new double[Dimension]);
        }

        double[][] IRandomNumberGenerator<double[]>.Generate(int samples)
        {
            return Generate(samples, Jagged.Create<double>(samples, Dimension));
        }

        double[][] ISampleableDistribution<double[]>.Generate(int samples, Random source)
        {
            return Generate(samples, Jagged.Create<double>(samples, Dimension), source);
        }

        double[] ISampleableDistribution<double[]>.Generate(Random source)
        {
            return Generate(new double[Dimension], source);
        }

        /// <summary>
        ///     Generates a random vector of observations from the current distribution.
        /// </summary>
        /// <param name="samples">The number of samples to generate.</param>
        /// <returns>A random vector of observations drawn from this distribution.</returns>
        public int[][] Generate(int samples)
        {
            return Generate(samples, Generator.Random);
        }

        /// <summary>
        ///     Generates a random vector of observations from the current distribution.
        /// </summary>
        /// <param name="samples">The number of samples to generate.</param>
        /// <param name="result">The location where to store the samples.</param>
        /// <returns>A random vector of observations drawn from this distribution.</returns>
        public int[][] Generate(int samples, int[][] result)
        {
            return Generate(samples, result, Generator.Random);
        }

        /// <summary>
        ///     Generates a random observation from the current distribution.
        /// </summary>
        /// <returns>A random observations drawn from this distribution.</returns>
        public int[] Generate()
        {
            return Generate(Generator.Random);
        }

        /// <summary>
        ///     Generates a random observation from the current distribution.
        /// </summary>
        /// <returns>A random observations drawn from this distribution.</returns>
        public int[] Generate(int[] result)
        {
            return Generate(result, Generator.Random);
        }

        /// <summary>
        ///     Generates a random vector of observations from the current distribution.
        /// </summary>
        /// <param name="samples">The number of samples to generate.</param>
        /// <param name="source">
        ///     The random number generator to use as a source of randomness.
        ///     Default is to use <see cref="Generator.Random" />.
        /// </param>
        /// <returns>A random vector of observations drawn from this distribution.</returns>
        public int[][] Generate(int samples, Random source)
        {
            return Generate(samples, Jagged.Create<int>(samples, Dimension), source);
        }

        /// <summary>
        ///     Generates a random vector of observations from the current distribution.
        /// </summary>
        /// <param name="samples">The number of samples to generate.</param>
        /// <param name="result">The location where to store the samples.</param>
        /// <param name="source">
        ///     The random number generator to use as a source of randomness.
        ///     Default is to use <see cref="Generator.Random" />.
        /// </param>
        /// <returns>A random vector of observations drawn from this distribution.</returns>
        public virtual int[][] Generate(int samples, int[][] result, Random source)
        {
            if (generator == null)
            {
                generator = MetropolisHasting.Discrete(Dimension, this);
            }

            if (generator.RandomSource != source)
            {
                generator.RandomSource = source;
            }

            return generator.Generate(samples, result);
        }

        /// <summary>
        ///     Generates a random observation from the current distribution.
        /// </summary>
        /// <returns>A random observations drawn from this distribution.</returns>
        public int[] Generate(Random source)
        {
            return Generate(1, source)[0];
        }

        /// <summary>
        ///     Generates a random observation from the current distribution.
        /// </summary>
        /// <returns>A random observations drawn from this distribution.</returns>
        public int[] Generate(int[] result, Random source)
        {
            return Generate(1, new[] { result }, source)[0];
        }

        /// <summary>
        ///     Gets the cumulative distribution function (cdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">
        ///     A single point in the distribution range.
        /// </param>
        /// <remarks>
        ///     The Cumulative Distribution Function (CDF) describes the cumulative
        ///     probability that a given value or any value smaller than it will occur.
        /// </remarks>
#if COMPATIBILITY
        protected internal virtual double InnerDistributionFunction(int[] x)
        {
            throw new NotImplementedException();
        }
#else
        protected internal abstract double InnerDistributionFunction(int[] x);
#endif

        /// <summary>
        ///     Gets the probability mass function (pmf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">
        ///     A single point in the distribution range.
        /// </param>
        /// <remarks>
        ///     The Probability Mass Function (PMF) describes the
        ///     probability that a given value <c>x</c> will occur.
        /// </remarks>
        /// <returns>
        ///     The probability of <c>x</c> occurring
        ///     in the current distribution.
        /// </returns>
        public
#if COMPATIBILITY
        virtual
#endif
            double ProbabilityMassFunction(int[] x)
        {
            if (x == null)
            {
                throw new ArgumentNullException("x");
            }

            return InnerProbabilityMassFunction(x);
        }

        /// <summary>
        ///     Gets the probability mass function (pmf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">
        ///     A single point in the distribution range.
        /// </param>
        /// <remarks>
        ///     The Probability Mass Function (PMF) describes the
        ///     probability that a given value <c>x</c> will occur.
        /// </remarks>
        /// <returns>
        ///     The probability of <c>x</c> occurring
        ///     in the current distribution.
        /// </returns>
#if COMPATIBILITY
        protected internal virtual double InnerProbabilityMassFunction(int[] x)
        {
            throw new NotImplementedException();
        }
#else
        protected internal abstract double InnerProbabilityMassFunction(int[] x);
#endif

        /// <summary>
        ///     Gets the log-probability mass function (pmf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">
        ///     A single point in the distribution range.
        /// </param>
        /// <remarks>
        ///     The Probability Mass Function (PMF) describes the
        ///     probability that a given value <c>x</c> will occur.
        /// </remarks>
        /// <returns>
        ///     The logarithm of the probability of <c>x</c>
        ///     occurring in the current distribution.
        /// </returns>
        public
#if COMPATIBILITY
        virtual
#endif
            double LogProbabilityMassFunction(int[] x)
        {
            if (x == null)
            {
                throw new ArgumentNullException("x");
            }

            return InnerLogProbabilityMassFunction(x);
        }

        /// <summary>
        ///     Gets the log-probability mass function (pmf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">
        ///     A single point in the distribution range.
        /// </param>
        /// <remarks>
        ///     The Probability Mass Function (PMF) describes the
        ///     probability that a given value <c>x</c> will occur.
        /// </remarks>
        /// <returns>
        ///     The logarithm of the probability of <c>x</c>
        ///     occurring in the current distribution.
        /// </returns>
#if COMPATIBILITY
        protected internal virtual double InnerLogProbabilityMassFunction(int[] x)
        {
            throw new NotImplementedException();
        }
#else
        protected internal virtual double InnerLogProbabilityMassFunction(int[] x)
        {
            return System.Math.Log(ProbabilityMassFunction(x));
        }
#endif

        /// <summary>
        ///     Not supported.
        /// </summary>
        public
#if COMPATIBILITY
        virtual
#endif
            int[] InverseDistributionFunction(double p)
        {
            return InnerInverseDistributionFunction(p);
        }

        /// <summary>
        ///     Not supported.
        /// </summary>
        protected internal int[] InnerInverseDistributionFunction(double p)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Gets the complementary cumulative distribution function
        ///     (ccdf) for this distribution evaluated at point <c>x</c>.
        ///     This function is also known as the Survival function.
        /// </summary>
        /// <remarks>
        ///     The Complementary Cumulative Distribution Function (CCDF) is
        ///     the complement of the Cumulative Distribution Function, or 1
        ///     minus the CDF.
        /// </remarks>
        protected internal double InnerComplementaryDistributionFunction(int[] x)
        {
            return 1.0 - DistributionFunction(x);
        }

        /// <summary>
        ///     Gets the marginal distribution of a given variable.
        /// </summary>
        /// <param name="index">The variable index.</param>
        public double[] MarginalDistributionFunction(int index)
        {
            var num = new double[Support[index].Length];
            double den = 0;

            var symbols = Support.Apply(x => x.Length);
            var probe = new int[Dimension];

            foreach (var input in symbols.Sequences(true))
            {
                for (var i = 0; i < probe.Length; i++)
                {
                    probe[i] = input[i] + Support[i].Min;
                }

                var p = ProbabilityMassFunction(probe);

                for (var k = 0; k < probe.Length; k++)
                {
                    if (k == index)
                    {
                        continue;
                    }

                    for (var j = 0; j < Support[k].Length; j++)
                    {
                        if (probe[k] == Support[k].Min + j)
                        {
                            num[j] += p;
                        }
                    }
                }

                den += p;
            }

            return num.Apply(v => v == 0 ? 0 : v / den);
        }

        /// <summary>
        ///     Gets the marginal distribution of a given variable evaluated at a given value.
        /// </summary>
        /// <param name="index">The variable index.</param>
        /// <param name="value">The variable value.</param>
        public double MarginalDistributionFunction(int index, int value)
        {
            double num = 0;
            double den = 0;

            var symbols = Support.Apply(x => x.Length);
            var probe = new int[Dimension];

            foreach (var input in symbols.Sequences(true))
            {
                for (var i = 0; i < probe.Length; i++)
                {
                    probe[i] = input[i] + Support[i].Min;
                }

                var p = ProbabilityMassFunction(probe);

                for (var k = 0; k < probe.Length; k++)
                {
                    if (k == index)
                    {
                        continue;
                    }

                    if (probe[k] == Support[k].Min + value)
                    {
                        num += p;
                    }
                }

                den += p;
            }

            return num == 0 ? 0 : num / den;
        }

        /// <summary>
        ///     Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">
        ///     The array of observations to fit the model against. The array
        ///     elements can be either of type double (for univariate data) or
        ///     type double[] (for multivariate data).
        /// </param>
        /// <remarks>
        ///     Although both double[] and double[][] arrays are supported,
        ///     providing a double[] for a multivariate distribution or a
        ///     double[][] for a univariate distribution may have a negative
        ///     impact in performance.
        /// </remarks>
        public virtual void Fit(double[][] observations)
        {
            Fit(observations, (IFittingOptions)null);
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
        /// <remarks>
        ///     Although both double[] and double[][] arrays are supported,
        ///     providing a double[] for a multivariate distribution or a
        ///     double[][] for a univariate distribution may have a negative
        ///     impact in performance.
        /// </remarks>
        public virtual void Fit(double[][] observations, double[] weights)
        {
            Fit(observations, weights, null);
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
        /// <remarks>
        ///     Although both double[] and double[][] arrays are supported,
        ///     providing a double[] for a multivariate distribution or a
        ///     double[][] for a univariate distribution may have a negative
        ///     impact in performance.
        /// </remarks>
        public virtual void Fit(double[][] observations, int[] weights)
        {
            Fit(observations, weights, null);
        }

        /// <summary>
        ///     Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">
        ///     The array of observations to fit the model against. The array
        ///     elements can be either of type double (for univariate data) or
        ///     type double[] (for multivariate data).
        /// </param>
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
        public virtual void Fit(double[][] observations, IFittingOptions options)
        {
            Fit(observations, (double[])null, options);
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
        /// <remarks>
        ///     Although both double[] and double[][] arrays are supported,
        ///     providing a double[] for a multivariate distribution or a
        ///     double[][] for a univariate distribution may have a negative
        ///     impact in performance.
        /// </remarks>
        public virtual void Fit(double[][] observations, double[] weights, IFittingOptions options)
        {
            throw new NotSupportedException();
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
        /// <remarks>
        ///     Although both double[] and double[][] arrays are supported,
        ///     providing a double[] for a multivariate distribution or a
        ///     double[][] for a univariate distribution may have a negative
        ///     impact in performance.
        /// </remarks>
        public virtual void Fit(double[][] observations, int[] weights, IFittingOptions options)
        {
            if (weights != null)
            {
                throw new NotSupportedException();
            }

            Fit(observations, (double[])null, options);
        }

        #region IDistribution explicit members

        double IDistribution.DistributionFunction(double[] x)
        {
            return DistributionFunction(x.Apply(Convert.ToInt32));
        }

        double IDistribution.ProbabilityFunction(double[] x)
        {
            return ProbabilityMassFunction(x.Apply(Convert.ToInt32));
        }

        double IDistribution.LogProbabilityFunction(double[] x)
        {
            return LogProbabilityMassFunction(x.Apply(Convert.ToInt32));
        }

        double IDistribution.ComplementaryDistributionFunction(double[] x)
        {
            return ComplementaryDistributionFunction(x.Apply(Convert.ToInt32));
        }

        void IDistribution.Fit(Array observations)
        {
            (this as IDistribution).Fit(observations, (IFittingOptions)null);
        }

        void IDistribution.Fit(Array observations, double[] weights)
        {
            (this as IDistribution).Fit(observations, weights, null);
        }

        void IDistribution.Fit(Array observations, int[] weights)
        {
            (this as IDistribution).Fit(observations, weights, null);
        }

        void IDistribution.Fit(Array observations, IFittingOptions options)
        {
            (this as IDistribution).Fit(observations, (double[])null, options);
        }

        void IDistribution.Fit(Array observations, double[] weights, IFittingOptions options)
        {
            var multivariate = observations as double[][];
            if (multivariate != null)
            {
                Fit(multivariate, weights, options);
                return;
            }

            var univariate = observations as double[];
            if (univariate != null)
            {
                Fit(univariate.Split(Dimension), weights, options);
                return;
            }

            throw new ArgumentException("Unsupported parameter type.", "observations");
        }

        void IDistribution.Fit(Array observations, int[] weights, IFittingOptions options)
        {
            var multivariate = observations as double[][];
            if (multivariate != null)
            {
                Fit(multivariate, weights, options);
                return;
            }

            var univariate = observations as double[];
            if (univariate != null)
            {
                Fit(univariate.Split(Dimension), weights, options);
                return;
            }

            throw new ArgumentException("Unsupported parameter type.", "observations");
        }

        #endregion
    }
}