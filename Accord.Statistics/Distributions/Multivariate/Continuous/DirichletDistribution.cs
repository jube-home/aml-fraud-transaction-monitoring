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
    ///     Dirichlet distribution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The Dirichlet distribution, often denoted Dir(α), is a family of continuous
    ///         multivariate probability distributions parameterized by a vector α of positive
    ///         real numbers. It is the multivariate generalization of the beta distribution.
    ///     </para>
    ///     <para>
    ///         Dirichlet distributions are very often used as prior distributions in Bayesian
    ///         statistics, and in fact the Dirichlet distribution is the conjugate prior of the
    ///         categorical distribution and multinomial distribution. That is, its probability
    ///         density function returns the belief that the probabilities of K rival events are
    ///         x<sub>i</sub> given that each event has been observed α<sub>i</sub>−1 times.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Dirichlet_distribution">
    ///                         Wikipedia, The Free Encyclopedia. Dirichlet distribution.
    ///                         Available from: http://en.wikipedia.org/wiki/Dirichlet_distribution
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///   // Create a Dirichlet with the following concentrations
    ///   var dirich = new DirichletDistribution(0.42, 0.57, 1.2);
    ///   
    ///   // Common measures
    ///   double[] mean = dirich.Mean;     // { 0.19, 0.26, 0.54 }
    ///   double[] median = dirich.Median; // { 0.19, 0.26, 0.54 }
    ///   double[] var = dirich.Variance;  // { 0.048, 0.060, 0.077 }
    ///   double[,] cov = dirich.Covariance; // see below
    ///   
    ///   
    ///   //       0.0115297440926238 0.0156475098399895 0.0329421259789253 
    ///   // cov = 0.0156475098399895 0.0212359062114143 0.0447071709713986 
    ///   //       0.0329421259789253 0.0447071709713986 0.0941203599397865
    ///   
    ///   // (the above matrix representation has been transcribed to text using)
    ///   string str = cov.ToString(DefaultMatrixFormatProvider.InvariantCulture);
    ///   
    ///   
    ///   // Probability mass functions
    ///   double pdf1 = dirich.ProbabilityDensityFunction(new double[] { 2, 5 }); // 0.12121671541846207
    ///   double pdf2 = dirich.ProbabilityDensityFunction(new double[] { 4, 2 }); // 0.12024840322466089
    ///   double pdf3 = dirich.ProbabilityDensityFunction(new double[] { 3, 7 }); // 0.082907634905068528
    ///   double lpdf = dirich.LogProbabilityDensityFunction(new double[] { 3, 7 }); // -2.4900281233124044
    /// </code>
    /// </example>
    [Serializable]
    public class DirichletDistribution : MultivariateContinuousDistribution
    {
        private double[] alphas;

        private double constant;
        private double[,] covariance;

        private double[] mean;
        private double[] variance;

        /// <summary>
        ///     Creates a new symmetric Dirichlet distribution.
        /// </summary>
        /// <param name="dimension">The number <c>k</c> of categories.</param>
        /// <param name="concentration">The common concentration parameter α (alpha).</param>
        public DirichletDistribution(int dimension, double concentration)
            : base(dimension)
        {
            if (concentration <= 0)
            {
                throw new ArgumentOutOfRangeException("concentration",
                    "Concentration parameter α must be higher than 0.");
            }

            alphas = new double[dimension];
            for (var i = 0; i < alphas.Length; i++)
            {
                alphas[i] = concentration;
            }

            constant = 1.0 / Beta.Multinomial(alphas);
        }

        /// <summary>
        ///     Creates a new Dirichlet distribution.
        /// </summary>
        /// <param name="concentrations">The concentration parameters α<sub>i</sub> (alpha_i).</param>
        public DirichletDistribution(params double[] concentrations)
            : base(concentrations.Length)
        {
            if (concentrations.Length < 2)
            {
                throw new DimensionMismatchException("concentrations",
                    "Concentration length must be greater than 2.");
            }

            for (var i = 0; i < concentrations.Length; i++)
            {
                if (concentrations[i] <= 0)
                {
                    throw new ArgumentOutOfRangeException("concentrations",
                        "Concentration parameters must be higher than 0.");
                }
            }

            alphas = concentrations;
            constant = 1.0 / Beta.Multinomial(concentrations);
        }

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
                    mean = alphas.Divide(alphas.Sum());
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
                    var sum = alphas.Sum();
                    var den = sum * sum * (sum + 1);

                    variance = new double[alphas.Length];
                    for (var i = 0; i < variance.Length; i++)
                    {
                        variance[i] = alphas[i] * (sum - alphas[i]) / den;
                    }
                }

                return variance;
            }
        }

        /// <summary>
        ///     Gets the variance-covariance matrix for this distribution.
        /// </summary>
        /// <value>A matrix containing the covariance values for the distribution.</value>
        public override double[,] Covariance
        {
            get
            {
                if (covariance == null)
                {
                    var sum = alphas.Sum();
                    var den = sum * sum * (sum + 1);

                    var k = alphas.Length;
                    covariance = new double[k, k];

                    for (var i = 0; i < alphas.Length; i++)
                    {
                        for (var j = 0; j < alphas.Length; j++)
                        {
                            covariance[i, j] = i == j
                                ? alphas[i] * (sum - alphas[i]) / den
                                : -(alphas[i] * alphas[j]) / den;
                        }
                    }
                }

                return covariance;
            }
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
            double product = 1;

            for (var i = 0; i < x.Length; i++)
            {
                product *= System.Math.Pow(x[i], alphas[i] - 1);
            }

            return constant * product;
        }

        /// <summary>
        ///     Not supported.
        /// </summary>
        protected internal override double InnerDistributionFunction(double[] x)
        {
            throw new NotSupportedException();
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
            double sum = 0;

            for (var i = 0; i < x.Length; i++)
            {
                sum += (alphas[i] - 1) * System.Math.Log(x[i]);
            }

            return System.Math.Log(constant) + sum;
        }

        /// <summary>
        ///     Not supported.
        /// </summary>
        public override void Fit(double[][] observations, double[] weights, IFittingOptions options)
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
            return new DirichletDistribution(alphas);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="formatProvider">The format provider.</param>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "Dirichlet(X)");
        }
    }
}