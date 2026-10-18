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
using System.Text;
using Accord.Math;
using Accord.Statistics.Distributions.Fitting;

namespace Accord.Statistics.Distributions.Multivariate
{
    /// <summary>
    ///     Joint distribution assuming independence between vector components.
    /// </summary>
    /// <typeparam name="TDistribution">The type of the underlying distributions.</typeparam>
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
    public class Independent<TDistribution> : MultivariateContinuousDistribution,
        ISampleableDistribution<double[]>,
        IFittableDistribution<double[], IndependentOptions>
        where TDistribution : IUnivariateDistribution
    {
        private double[,] covariance;

        private double[] mean;
        private double[] variance;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Independent&lt;TDistribution&gt;" /> class.
        /// </summary>
        /// <param name="dimensions">The number of independent component distributions.</param>
        public Independent(int dimensions)
            : base(dimensions)
        {
            try
            {
                Components = new TDistribution[dimensions];
                for (var i = 0; i < Components.Length; i++)
                {
                    Components[i] = Activator.CreateInstance<TDistribution>();
                }
            }
            catch
            {
                throw new ArgumentException("The component distribution needs specific parameters that need to be" +
                                            "given to its constructor. Please specify in the 'initializer' argument of this constructor" +
                                            "how the component distributions should be created.");
            }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Independent&lt;TDistribution&gt;" /> class.
        /// </summary>
        /// <param name="dimensions">The number of independent component distributions.</param>
        /// <param name="initializer">A function that creates a new distribution for each component index.</param>
        public Independent(int dimensions, Func<TDistribution> initializer)
            : base(dimensions)
        {
            Components = new TDistribution[dimensions];
            for (var i = 0; i < Components.Length; i++)
            {
                Components[i] = initializer();
            }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Independent&lt;TDistribution&gt;" /> class.
        /// </summary>
        /// <param name="dimensions">The number of independent component distributions.</param>
        /// <param name="initializer">A function that creates a new distribution for each component index.</param>
        public Independent(int dimensions, Func<int, TDistribution> initializer)
            : base(dimensions)
        {
            Components = new TDistribution[dimensions];
            for (var i = 0; i < Components.Length; i++)
            {
                Components[i] = initializer(i);
            }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Independent&lt;TDistribution&gt;" /> class.
        /// </summary>
        /// <param name="dimensions">The number of independent component distributions.</param>
        /// <param name="component">A base component which will be cloned to all dimensions.</param>
        public Independent(int dimensions, TDistribution component)
            : base(dimensions)
        {
            Components = new TDistribution[dimensions];
            for (var i = 0; i < Components.Length; i++)
            {
                Components[i] = (TDistribution)component.Clone();
            }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Independent&lt;TDistribution&gt;" /> class.
        /// </summary>
        /// <param name="components">The components.</param>
        public Independent(params TDistribution[] components)
            : base(components.Length)
        {
            this.Components = components;
        }

        /// <summary>
        ///     Gets or sets the components of this joint distribution.
        /// </summary>
        public TDistribution this[int i]
        {
            get => Components[i];
            set => Components[i] = value;
        }

        /// <summary>
        ///     Gets the components of this joint distribution.
        /// </summary>
        public TDistribution[] Components { get; }

        /// <summary>
        ///     Gets the mean for this distribution.
        /// </summary>
        /// <value>A vector containing the mean values for the distribution.</value>
        public override double[] Mean
        {
            get
            {
                if (mean == null)
                {
                    mean = new double[Components.Length];
                    for (var i = 0; i < Components.Length; i++)
                    {
                        mean[i] = Components[i].Mean;
                    }
                }

                return mean;
            }
        }

        /// <summary>
        ///     Gets the variance for this distribution.
        /// </summary>
        /// <value>A vector containing the variance values for the distribution.</value>
        public override double[] Variance
        {
            get
            {
                if (variance == null)
                {
                    variance = new double[Components.Length];
                    for (var i = 0; i < Components.Length; i++)
                    {
                        variance[i] = Components[i].Variance;
                    }
                }

                return variance;
            }
        }

        /// <summary>
        ///     Gets the variance-covariance matrix for this distribution.
        /// </summary>
        /// <value>A matrix containing the covariance values for the distribution.</value>
        /// <remarks>
        ///     For an independent distribution, this matrix will always be diagonal.
        /// </remarks>
        public override double[,] Covariance
        {
            get
            {
                if (covariance == null)
                {
                    covariance = Matrix.Diagonal(Variance);
                }

                return covariance;
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
        /// <example>
        ///     For an example on how to fit an independent joint distribution, please
        ///     take a look at the examples section for <see cref="Independent{TDistribution}" />.
        /// </example>
        public void Fit(double[][] observations, double[] weights, IndependentOptions options)
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
                        Components[i].Fit(observations[i], weights, options.InnerOptions[i]);
                    }
                }
                else
                {
                    for (var i = 0; i < Components.Length; i++)
                    {
                        Components[i].Fit(observations[i], weights, options.InnerOption);
                    }
                }
            }
            else
            {
                observations = observations.Transpose();
                for (var i = 0; i < Components.Length; i++)
                {
                    Components[i].Fit(observations[i], weights, null);
                }
            }

            Reset();
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
        /// <returns>
        ///     A random vector of observations drawn from this distribution.
        /// </returns>
        public override double[][] Generate(int samples, double[][] result, Random source)
        {
            var gen = Components.Apply(x => (ISampleableDistribution<double>)x);

            for (var i = 0; i < result.Length; i++)
            {
                for (var j = 0; j < gen.Length; j++)
                {
                    result[i][j] = gen[j].Generate(source);
                }
            }

            return result;
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

            return new Independent<TDistribution>(clone);
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
        protected internal override double InnerDistributionFunction(double[] x)
        {
            double p = 1;
            for (var i = 0; i < Components.Length; i++)
            {
                p *= Components[i].DistributionFunction(x[i]);
            }

            return p;
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
        protected internal override double InnerProbabilityDensityFunction(double[] x)
        {
            return System.Math.Exp(LogProbabilityDensityFunction(x));
        }

        /// <summary>
        ///     Gets the log-probability density function (pdf)
        ///     for this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">
        ///     A single point in the distribution range. For a
        ///     univariate distribution, this should be a single
        ///     double value. For a multivariate distribution,
        ///     this should be a double array.
        /// </param>
        /// <returns>
        ///     The logarithm of the probability of <c>x</c>
        ///     occurring in the current distribution.
        /// </returns>
        protected internal override double InnerLogProbabilityDensityFunction(params double[] x)
        {
            double p = 0;
            for (var i = 0; i < Components.Length; i++)
            {
                p += Components[i].LogProbabilityFunction(x[i]);
            }

            return p;
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
        /// <example>
        ///     For an example on how to fit an independent joint distribution, please
        ///     take a look at the examples section for <see cref="Independent{TDistribution}" />.
        /// </example>
        public override void Fit(double[][] observations, double[] weights, IFittingOptions options)
        {
            var independentOptions = options as IndependentOptions;
            if (options != null && independentOptions == null)
            {
                throw new ArgumentException("The specified options' type is invalid.", "options");
            }

            Fit(observations, weights, independentOptions);
        }

        /// <summary>
        ///     Resets cached values (should be called after re-estimation).
        /// </summary>
        protected void Reset()
        {
            mean = null;
            variance = null;
            covariance = null;
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString(string format, IFormatProvider formatProvider)
        {
            var sb = new StringBuilder();
            sb.Append("Independent(");
            for (var i = 0; i < Components.Length; i++)
            {
                sb.Append("x" + i);

                if (i < Components.Length - 1)
                {
                    sb.Append(", ");
                }
            }

            sb.Append("; ");

            for (var i = 0; i < Components.Length; i++)
            {
                var fmt = Components[i] as IFormattable;

                var componentText = fmt
                    .ToString(format, formatProvider)
                    .Replace("(x", "(x" + i);

                sb.AppendFormat(componentText);

                if (i < Components.Length - 1)
                {
                    sb.Append(" + ");
                }
            }

            sb.Append(")");

            return sb.ToString();
        }
    }
}