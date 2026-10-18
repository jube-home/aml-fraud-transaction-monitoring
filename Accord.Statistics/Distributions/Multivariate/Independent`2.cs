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
using Accord.Math.Random;
using Accord.Statistics.Distributions.Fitting;

namespace Accord.Statistics.Distributions.Multivariate
{
    /// <summary>
    ///     Joint distribution assuming independence between vector components.
    /// </summary>
    /// <typeparam name="TDistribution">The type of the underlying distributions.</typeparam>
    /// <typeparam name="TObservation">The type for the observations being modeled by the distribution (i.e. double).</typeparam>
    /// <remarks>
    ///     <para>
    ///         In probability and statistics, given at least two random variables <c>X</c>,
    ///         <c>Y</c>, ..., that are defined on a probability space, the joint probability
    ///         distribution for <c>X</c>, <c>Y</c>, ... is a probability distribution that
    ///         gives the probability that each of X, Y, ... falls in any particular range or
    ///         discrete set of values specified for that variable. In the case of only two
    ///         random variables, this is called a bivariate distribution, but the concept
    ///         generalizes to any number of random variables, giving a multivariate distribution.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Joint_probability_distribution">
    ///                         Wikipedia, The Free Encyclopedia. Beta distribution.
    ///                         Available from: http://en.wikipedia.org/wiki/Joint_probability_distribution
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The following example shows how to declare and initialize an Independent Joint
    ///         Gaussian Distribution using known means and variances for each component.
    ///     </para>
    ///     <code>
    ///   // Declare two normal distributions
    ///   NormalDistribution pa = new NormalDistribution(4.2, 1); // p(a)
    ///   NormalDistribution pb = new NormalDistribution(7.0, 2); // p(b)
    ///  
    ///   // Now, create a joint distribution combining these two:
    ///   var joint = new Independent&lt;NormalDistribution>(pa, pb);
    ///   
    ///   // This distribution assumes the distributions of the two components are independent,
    ///   // i.e. if we have 2D input vectors on the form {a, b}, then p({a,b}) = p(a) * p(b). 
    ///   
    ///   // Lets check a simple example. Consider a 2D input vector x = { 4.2, 7.0 } as
    ///   //
    ///   double[] x = new double[] { 4.2, 7.0 };
    ///   
    ///   // Those two should be completely equivalent:
    ///   double p1 = joint.ProbabilityDensityFunction(x);
    ///   double p2 = pa.ProbabilityDensityFunction(x[0]) * pb.ProbabilityDensityFunction(x[1]);
    ///   
    ///   bool equal = p1 == p2; // at this point, equal should be true.
    /// </code>
    ///     <para>
    ///         The following example shows how to fit a distribution (estimate
    ///         its parameters) from a given dataset.
    ///     </para>
    ///     <code>
    ///   // Let's consider an input dataset of 2D vectors. We would
    ///   // like to estimate an Independent&lt;NormalDistribution>
    ///   // which best models this data.
    ///   
    ///  double[][] data =
    ///  {
    ///                  // x, y
    ///      new double[] { 1, 8 },
    ///      new double[] { 2, 6 },
    ///      new double[] { 5, 7 },
    ///      new double[] { 3, 9 },
    ///  };
    ///  
    ///  // We start by declaring some initial guesses for the
    ///  // distributions of each random variable (x, and y):
    ///  // 
    ///  var distX = new NormalDistribution(0, 1);
    ///  var distY = new NormalDistribution(0, 1);
    /// 
    ///  // Next, we declare our initial guess Independent distribution
    ///  var joint = new Independent&lt;NormalDistribution>(distX, distY);
    /// 
    ///  // We can now fit the distribution to our data,
    ///  // producing an estimate of its parameters:
    ///  //
    ///  joint.Fit(data);
    /// 
    ///  // At this point, we have estimated our distribution. 
    ///  
    ///  double[] mean = joint.Mean;     // should be { 2.75,  7.50  }
    ///  double[] var  = joint.Variance; // should be { 2.917, 1.667 } 
    /// 
    ///                                    //        | 2.917,  0.000 |
    ///  double[,] cov = joint.Covariance; //  Cov = |               |
    ///                                    //        | 0.000,  1.667 |
    ///                                    
    /// // The covariance matrix is diagonal, as it would be expected
    /// // if is assumed there are no interactions between components.
    /// </code>
    /// </example>
    [Serializable]
    public class Independent<TDistribution, TObservation> : Independent<TDistribution>,
        IMultivariateDistribution<TObservation[]>,
        IFittableDistribution<TObservation[], IndependentOptions>,
        ISampleableDistribution<TObservation[]>
        where TDistribution : IUnivariateDistribution<TObservation>,
        IUnivariateDistribution
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Independent&lt;TDistribution&gt;" /> class.
        /// </summary>
        /// <param name="dimensions">The number of independent component distributions.</param>
        /// <param name="initializer">A function that creates a new distribution for each component index.</param>
        public Independent(int dimensions, Func<int, TDistribution> initializer)
            : base(dimensions, initializer)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Independent&lt;TDistribution&gt;" /> class.
        /// </summary>
        /// <param name="dimensions">The number of independent component distributions.</param>
        /// <param name="initializer">A function that creates a new distribution for each component index.</param>
        public Independent(int dimensions, Func<TDistribution> initializer)
            : base(dimensions, initializer)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Independent&lt;TDistribution&gt;" /> class.
        /// </summary>
        /// <param name="dimensions">The number of independent component distributions.</param>
        /// <param name="component">A base component which will be cloned to all dimensions.</param>
        public Independent(int dimensions, TDistribution component)
            : base(dimensions, component)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Independent&lt;TDistribution&gt;" /> class.
        /// </summary>
        /// <param name="components">The components.</param>
        public Independent(params TDistribution[] components)
            : base(components)
        {
        }

        /// <summary>
        ///     Fits the underlying distribution to a given set of observations.
        /// </summary>
        /// <param name="observations">
        ///     The array of observations to fit the model against. The array
        ///     elements can be either of type double (for univariate data) or
        ///     type double[] (for multivariate data).
        /// </param>
        /// <example>
        ///     For an example on how to fit an independent joint distribution, please
        ///     take a look at the examples section for <see cref="Independent{TDistribution}" />.
        /// </example>
        public void Fit(TObservation[][] observations)
        {
            Fit(observations, null, null);
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
        /// <example>
        ///     For an example on how to fit an independent joint distribution, please
        ///     take a look at the examples section for <see cref="Independent{TDistribution}" />.
        /// </example>
        public void Fit(TObservation[][] observations, double[] weights)
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
        /// <param name="weights">The weight vector containing the weight for each of the samples.</param>
        /// <param name="options">
        ///     Optional arguments which may be used during fitting, such
        ///     as regularization constants and additional parameters.
        /// </param>
        /// <example>
        ///     For an example on how to fit an independent joint distribution, please
        ///     take a look at the examples section for <see cref="Independent{TDistribution}" />.
        /// </example>
        public void Fit(TObservation[][] observations, double[] weights, IndependentOptions options)
        {
            if (options != null)
            {
                if (!options.Transposed)
                {
                    observations = observations.Transpose();
                }

                if (options.InnerOptions != null)
                {
                    for (var i = 0; i < Components.Length; i++)
                    {
                        var c = (IFittableDistribution<TObservation>)Components[i];
                        c.Fit(observations[i], weights, options.InnerOptions[i]);
                    }
                }
                else
                {
                    for (var i = 0; i < Components.Length; i++)
                    {
                        var c = (IFittableDistribution<TObservation>)Components[i];
                        c.Fit(observations[i], weights, options.InnerOption);
                    }
                }
            }
            else
            {
                observations = observations.Transpose();
                for (var i = 0; i < Components.Length; i++)
                {
                    ((IFittableDistribution<TObservation>)Components[i]).Fit(observations[i], weights);
                }
            }

            Reset();
        }

        /// <summary>
        ///     Gets the probability density function (pdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">
        ///     A single point in the distribution range. For a
        ///     univariate distribution, this should be a single
        ///     double value. For a multivariate distribution,
        ///     this should be a double array.
        /// </param>
        /// <returns>
        ///     The probability of <c>x</c> occurring
        ///     in the current distribution.
        /// </returns>
        /// <remarks>
        ///     The Probability Density Function (PDF) describes the
        ///     probability that a given value <c>x</c> will occur.
        /// </remarks>
        public double ProbabilityFunction(TObservation[] x)
        {
            double p = 1;
            for (var i = 0; i < Components.Length; i++)
            {
                p *= Components[i].ProbabilityFunction(x[i]);
            }

            return p;
        }

        /// <summary>
        ///     Gets the cumulative distribution function (cdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        /// <remarks>
        ///     The Cumulative Distribution Function (CDF) describes the cumulative
        ///     probability that a given value or any value smaller than it will occur.
        /// </remarks>
        public double DistributionFunction(TObservation[] x)
        {
            double p = 1;
            for (var i = 0; i < Components.Length; i++)
            {
                p *= Components[i].DistributionFunction(x[i]);
            }

            return p;
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
        public double ComplementaryDistributionFunction(TObservation[] x)
        {
            double p = 1;
            for (var i = 0; i < Components.Length; i++)
            {
                p *= Components[i].ComplementaryDistributionFunction(x[i]);
            }

            return p;
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
        public double LogProbabilityFunction(TObservation[] x)
        {
            double p = 0;
            for (var i = 0; i < Components.Length; i++)
            {
                p += Components[i].LogProbabilityFunction(x[i]);
            }

            //#if DEBUG
//            double expected = Math.Log(ProbabilityFunction(x));
//            if (!expected.IsEqual(p, rtol: 1e-3))
//                throw new Exception();
//#endif
            return p;
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public override object Clone()
        {
            var clone = new TDistribution[Components.Length];
            for (var i = 0; i < clone.Length; i++)
            {
                clone[i] = (TDistribution)Components[i].Clone();
            }

            return new Independent<TDistribution, TObservation>(clone);
        }

        /// <summary>
        ///     Generates a random observation from the current distribution.
        /// </summary>
        /// <param name="result">The location where to store the sample.</param>
        /// <returns>A random observation drawn from this distribution.</returns>
        public TObservation[] Generate(TObservation[] result)
        {
            return Generate(result, Generator.Random);
        }

        /// <summary>
        ///     Generates a random observation from the current distribution.
        /// </summary>
        /// <param name="result">The location where to store the sample.</param>
        /// <param name="source">
        ///     The random number generator to use as a source of randomness.
        ///     Default is to use <see cref="Generator.Random" />.
        /// </param>
        /// <returns>A random observation drawn from this distribution.</returns>
        public TObservation[] Generate(TObservation[] result, Random source)
        {
            for (var i = 0; i < Components.Length; i++)
            {
                result[i] = ((ISampleableDistribution<TObservation>)Components[i]).Generate(source);
            }

            return result;
        }

        /// <summary>
        ///     Generates a random vector of observations from the current distribution.
        /// </summary>
        /// <param name="samples">The number of samples to generate.</param>
        /// <param name="result">The location where to store the samples.</param>
        /// <returns>A random vector of observations drawn from this distribution.</returns>
        public TObservation[][] Generate(int samples, TObservation[][] result)
        {
            return Generate(samples, result, Generator.Random);
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
        public TObservation[][] Generate(int samples, TObservation[][] result, Random source)
        {
            for (var i = 0; i < Components.Length; i++)
            {
                result.SetColumn(i, ((ISampleableDistribution<TObservation>)Components[i]).Generate(samples, source));
            }

            return result;
        }

        TObservation[][] IRandomNumberGenerator<TObservation[]>.Generate(int samples)
        {
            return Generate(samples, Jagged.Zeros<TObservation>(samples, Components.Length));
        }

        TObservation[] IRandomNumberGenerator<TObservation[]>.Generate()
        {
            return Generate(new TObservation[Components.Length]);
        }

        TObservation[][] ISampleableDistribution<TObservation[]>.Generate(int samples, Random source)
        {
            return Generate(samples, Jagged.Zeros<TObservation>(samples, Components.Length), source);
        }

        TObservation[] ISampleableDistribution<TObservation[]>.Generate(Random source)
        {
            return Generate(new TObservation[Components.Length], source);
        }
    }
}