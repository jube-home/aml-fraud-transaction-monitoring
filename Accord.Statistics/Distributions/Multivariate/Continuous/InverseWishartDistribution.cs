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
using Accord.Math.Decompositions;
using Accord.Statistics.Distributions.Fitting;

namespace Accord.Statistics.Distributions.Multivariate
{
    /// <summary>
    ///     Inverse Wishart Distribution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The inverse Wishart distribution, also called the inverted Wishart distribution,
    ///         is a probability distribution defined on real-valued positive-definite matrices.
    ///         In Bayesian statistics it is used as the conjugate prior for the covariance matrix
    ///         of a multivariate normal distribution.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Inverse-Wishart_distribution">
    ///                         Wikipedia, The Free Encyclopedia. Inverse Wishart distribution.
    ///                         Available from: http://en.wikipedia.org/wiki/Inverse-Wishart_distribution
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///   // Create a Inverse Wishart with the parameters
    ///   var invWishart = new InverseWishartDistribution(
    ///   
    ///       // Degrees of freedom
    ///       degreesOfFreedom: 4,
    ///   
    ///       // Scale parameter
    ///       inverseScale: new double[,] 
    ///       {
    ///            {  1.7, -0.2 },
    ///            { -0.2,  5.3 },
    ///       }
    ///   );
    ///   
    ///   // Common measures
    ///   double[] var = invWishart.Variance;  // { -3.4, -10.6 }
    ///   double[,] cov = invWishart.Covariance;  // see below
    ///   double[,] mmean = invWishart.MeanMatrix; // see below
    ///   
    ///   //        cov                mean
    ///   //   -5.78   -4.56        1.7  -0.2 
    ///   //   -4.56  -56.18       -0.2   5.3 
    ///   
    ///   // (the above matrix representations have been transcribed to text using)
    ///   string scov = cov.ToString(DefaultMatrixFormatProvider.InvariantCulture);
    ///   string smean = mmean.ToString(DefaultMatrixFormatProvider.InvariantCulture);
    ///   
    ///   // For compatibility reasons, .Mean stores a flattened mean matrix
    ///   double[] mean = invWishart.Mean; // { 1.7, -0.2, -0.2, 5.3 }
    ///   
    ///   
    ///   // Probability density functions
    ///   double pdf = invWishart.ProbabilityDensityFunction(new double[,] 
    ///   {
    ///       {  5.2,  0.2 }, // 0.000029806281690351203
    ///       {  0.2,  4.2 },
    ///   });
    ///   
    ///   double lpdf = invWishart.LogProbabilityDensityFunction(new double[,] 
    ///   {
    ///       {  5.2,  0.2 }, // -10.420791391688828
    ///       {  0.2,  4.2 },
    ///   });
    /// </code>
    /// </example>
    /// <seealso cref="WishartDistribution" />
    [Serializable]
    public class InverseWishartDistribution : MatrixContinuousDistribution
    {
        private double constant;
        private double[,] covariance;
        private double[,] inverseScaleMatrix; // Ψ (psi)

        private double[,] mean;
        private double power;
        private int size;
        private double v; // degrees of freedom
        private double[] variance;

        /// <summary>
        ///     Creates a new Inverse Wishart distribution.
        /// </summary>
        /// <param name="degreesOfFreedom">The degrees of freedom v.</param>
        /// <param name="inverseScale">The inverse scale matrix Ψ (psi).</param>
        public InverseWishartDistribution(double degreesOfFreedom, double[,] inverseScale)
            : base(inverseScale.Rows(), inverseScale.Columns())
        {
            if (inverseScale.GetLength(0) != inverseScale.GetLength(1))
            {
                throw new DimensionMismatchException("inverseScale", "Matrix must be square.");
            }

            inverseScaleMatrix = inverseScale;
            size = inverseScale.GetLength(0);
            v = degreesOfFreedom;

            if (degreesOfFreedom <= size - 1)
            {
                throw new ArgumentOutOfRangeException("degreesOfFreedom", "Degrees of freedom must be greater "
                                                                          + "than or equal to the number of rows in the inverse scale matrix.");
            }

            var chol = new CholeskyDecomposition(inverseScale);

            if (!chol.IsPositiveDefinite)
            {
                throw new NonPositiveDefiniteMatrixException("scale");
            }
            //if (!chol.Symmetric)
            //    throw new NonSymmetricMatrixException("scale");

            var a = System.Math.Pow(chol.Determinant, v / 2.0);
            var b = System.Math.Pow(2, v * size / 2.0);
            var c = Gamma.Multivariate(v / 2.0, size);

            constant = a / (b * c);
            power = -(v + size + 1) / 2.0;
        }

        /// <summary>
        ///     Gets the mean for this distribution.
        /// </summary>
        /// <value>A vector containing the mean values for the distribution.</value>
        public override double[,] Mean
        {
            get
            {
                if (mean == null)
                {
                    mean = inverseScaleMatrix.Divide(v - size - 1);
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
                    variance = new double[size];
                    for (var i = 0; i < variance.Length; i++)
                    {
                        var num = 2 * inverseScaleMatrix[i, i] * inverseScaleMatrix[i, i];
                        var den = (v - size - 1) * (v - size - 1) * (v - size - 3);
                        variance[i] = num / den;
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
                    covariance = new double[size, size];
                    for (var i = 0; i < size; i++)
                    {
                        var pii = inverseScaleMatrix[i, i];

                        for (var j = 0; j < size; j++)
                        {
                            var pij = inverseScaleMatrix[i, j];
                            var pjj = inverseScaleMatrix[j, j];

                            var num = (v - size + 1) * (pij * pij) + (v - size - 1) * (pii * pjj);
                            var den = (v - size) * (v - size - 1) * (v - size - 1) * (v - size - 3);

                            covariance[i, j] = num / den;
                        }
                    }
                }

                return covariance;
            }
        }

        /// <summary>
        ///     Not supported.
        /// </summary>
        protected internal override double InnerDistributionFunction(double[,] x)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Gets the probability density function (pdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">
        ///     A single point in the distribution range.
        ///     For a matrix distribution, such as the Wishart's, this
        ///     should be a positive-definite matrix or a matrix written
        ///     in flat vector form.
        /// </param>
        /// <returns>
        ///     The probability of <c>x</c> occurring
        ///     in the current distribution.
        /// </returns>
        /// <remarks>
        ///     The Probability Density Function (PDF) describes the
        ///     probability that a given value <c>x</c> will occur.
        /// </remarks>
        protected internal override double InnerProbabilityDensityFunction(double[,] x)
        {
            var chol = new CholeskyDecomposition(x);

            var det = chol.Determinant;
            var Vx = chol.Solve(inverseScaleMatrix);

            var z = -0.5 * Vx.Trace();
            var a = System.Math.Pow(det, power);
            var b = System.Math.Exp(z);

            return constant * a * b;
        }

        /// <summary>
        ///     Gets the probability density function (pdf) for
        ///     this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">
        ///     A single point in the distribution range.
        ///     For a matrix distribution, such as the Wishart's, this
        ///     should be a positive-definite matrix or a matrix written
        ///     in flat vector form.
        /// </param>
        /// <returns>
        ///     The probability of <c>x</c> occurring
        ///     in the current distribution.
        /// </returns>
        /// <remarks>
        ///     The Probability Density Function (PDF) describes the
        ///     probability that a given value <c>x</c> will occur.
        /// </remarks>
        protected internal override double InnerLogProbabilityDensityFunction(double[,] x)
        {
            var chol = new CholeskyDecomposition(x);

            var det = chol.Determinant;
            var Vx = chol.Solve(inverseScaleMatrix);

            var z = -0.5 * Vx.Trace();

            return System.Math.Log(constant) + power * System.Math.Log(det) + z;
        }

        /// <summary>
        ///     Not supported.
        /// </summary>
        public override void Fit(double[][,] observations, double[] weights, IFittingOptions options)
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
            return new InverseWishartDistribution(v, inverseScaleMatrix);
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
            return string.Format(formatProvider, "Wishart^-1(X)");
        }
    }
}