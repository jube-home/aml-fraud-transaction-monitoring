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
using Accord.Math.Random;
using Accord.Statistics.Distributions.Fitting;
using Accord.Statistics.Distributions.Univariate;

namespace Accord.Statistics.Distributions.Multivariate
{
    /// <summary>
    ///     Wishart Distribution.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The Wishart distribution is a generalization to multiple dimensions of
    ///         the
    ///         <see cref="ChiSquareDistribution">
    ///             Chi-Squared distribution, or, in
    ///             the case of non-integer <see cref="DegreesOfFreedom" />degrees of
    ///             freedom
    ///         </see>
    ///         , of the
    ///         <see cref="GammaDistribution">
    ///             Gamma distribution
    ///         </see>
    ///         .
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Wishart_distribution">
    ///                         Wikipedia, The Free Encyclopedia. Wishart distribution.
    ///                         Available from: http://en.wikipedia.org/wiki/Wishart_distribution
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///   // Create a Wishart distribution with the parameters:
    ///   WishartDistribution wishart = new WishartDistribution(
    ///   
    ///       // Degrees of freedom
    ///       degreesOfFreedom: 7,
    ///   
    ///       // Scale parameter
    ///       scale: new double[,] 
    ///       {
    ///           { 4, 1, 1 },  
    ///           { 1, 2, 2 },  // (must be symmetric and positive definite)
    ///           { 1, 2, 6 },
    ///       }
    ///   );
    ///   
    ///   // Common measures
    ///   double[] var = wishart.Variance;  // { 224, 56, 504 }
    ///   double[,] cov = wishart.Covariance;   // see below
    ///   double[,] meanm = wishart.MeanMatrix; // see below
    ///               
    ///   //         224  63  175             28  7   7 
    ///   //   cov =  63  56  112     mean =   7  14  14
    ///   //         175 112  504              7  14  42
    ///   
    ///   // (the above matrix representations have been transcribed to text using)
    ///   string scov = cov.ToString(DefaultMatrixFormatProvider.InvariantCulture);
    ///   string smean = meanm.ToString(DefaultMatrixFormatProvider.InvariantCulture);
    ///   
    ///   // For compatibility reasons, .Mean stores a flattened mean matrix
    ///   double[] mean = wishart.Mean; // { 28, 7, 7, 7, 14, 14, 7, 14, 42 }
    ///   
    ///   
    ///   // Probability density functions
    ///   double pdf = wishart.ProbabilityDensityFunction(new double[,] 
    ///   {
    ///       { 8, 3, 1 },
    ///       { 3, 7, 1 },   //   0.000000011082455043473361
    ///       { 1, 1, 8 },
    ///   });
    ///   
    ///   double lpdf = wishart.LogProbabilityDensityFunction(new double[,] 
    ///   {
    ///       { 8, 3, 1 },
    ///       { 3, 7, 1 },   // -18.317902605850534
    ///       { 1, 1, 8 },
    ///   });
    /// </code>
    /// </example>
    /// <seealso cref="InverseWishartDistribution" />
    [Serializable]
    public class WishartDistribution : MatrixContinuousDistribution
    {
        private CholeskyDecomposition chol;

        private double constant;
        private double[,] covariance;
        private double lnconstant;

        private double[,] mean;
        private int n;
        private double power;
        private double[,] scaleMatrix;
        private int size;
        private double[] variance;

        /// <summary>
        ///     Creates a new Wishart distribution.
        /// </summary>
        /// <param name="dimension">The number of rows in the covariance matrices.</param>
        /// <param name="degreesOfFreedom">The degrees of freedom n.</param>
        public WishartDistribution(int dimension, int degreesOfFreedom)
            : this(degreesOfFreedom, Matrix.Identity(dimension))
        {
        }

        /// <summary>
        ///     Creates a new Wishart distribution.
        /// </summary>
        /// <param name="degreesOfFreedom">The degrees of freedom <c>n</c>.</param>
        /// <param name="scale">The positive-definite matrix scale matrix <c>V</c>.</param>
        public WishartDistribution(int degreesOfFreedom, double[,] scale)
            : base(scale.Rows(), scale.Columns())
        {
            if (scale.GetLength(0) != scale.GetLength(1))
            {
                throw new DimensionMismatchException("scale", "Matrix must be square.");
            }

            scaleMatrix = scale;
            n = degreesOfFreedom;
            size = scale.GetLength(0);

            if (degreesOfFreedom <= size - 1)
            {
                throw new ArgumentOutOfRangeException("degreesOfFreedom", "Degrees of freedom must be greater "
                                                                          + "than or equal to the number of rows in the scale matrix.");
            }

            chol = new CholeskyDecomposition(scale);

            if (!chol.IsPositiveDefinite)
            {
                throw new NonPositiveDefiniteMatrixException("scale");
            }
            //if (!chol.Symmetric)
            //    throw new NonSymmetricMatrixException("scale");

            var a = System.Math.Pow(chol.Determinant, n / 2.0);
            var b = System.Math.Pow(2, n * size / 2.0);
            var c = Gamma.Multivariate(n / 2.0, size);

            constant = 1.0 / (a * b * c);
            lnconstant = System.Math.Log(constant);

            power = (n - size - 1) / 2.0;
        }

        /// <summary>
        ///     Gets the degrees of freedom for this Wishart distribution.
        /// </summary>
        public double DegreesOfFreedom => n;

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
                    mean = n.Multiply(scaleMatrix);
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
                    for (var i = 0; i < size; i++)
                    {
                        var vii = scaleMatrix[i, i];
                        variance[i] = 2 * n * (vii * vii);
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
                        var vii = scaleMatrix[i, i];

                        for (var j = 0; j < size; j++)
                        {
                            var vij = scaleMatrix[i, j];
                            var vjj = scaleMatrix[j, j];

                            covariance[i, j] = n * (vij * vij + vii * vjj);
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
            var det = x.Determinant();
            var Vx = chol.Solve(x);

            var z = -0.5 * Vx.Trace();
            var a = System.Math.Pow(det, power);
            var b = System.Math.Exp(z);

            return constant * a * b;
        }

        /// <summary>
        ///     Gets the log-probability density function (pdf)
        ///     for this distribution evaluated at point <c>x</c>.
        /// </summary>
        /// <param name="x">
        ///     A single point in the distribution range.
        ///     For a matrix distribution, such as the Wishart's, this
        ///     should be a positive-definite matrix or a matrix written
        ///     in flat vector form.
        /// </param>
        /// <returns>
        ///     The logarithm of the probability of <c>x</c>
        ///     occurring in the current distribution.
        /// </returns>
        protected internal override double InnerLogProbabilityDensityFunction(double[,] x)
        {
            var det = x.Determinant();
            var Vx = chol.Solve(x);

            var z = -0.5 * Vx.Trace();
            var a = power * System.Math.Log(det);

            return lnconstant + a + z;
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
        public override double[][,] Generate(int samples, double[][,] result, Random source)
        {
            return Random(samples, n, scaleMatrix, result, source);
        }

        /// <summary>
        ///     Unsupported.
        /// </summary>
        protected internal override double InnerDistributionFunction(double[,] x)
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
        /// <param name="weights">The weight vector containing the weight for each of the samples.</param>
        /// <param name="options">
        ///     Optional arguments which may be used during fitting, such
        ///     as regularization constants and additional parameters.
        /// </param>
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
            return new WishartDistribution(n, scaleMatrix);
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
            return string.Format(formatProvider, "Wishart(X)");
        }

        /// <summary>
        ///     Generates a random vector of observations from the current distribution.
        /// </summary>
        /// <param name="degreesOfFreedom">The degrees of freedom <c>n</c>.</param>
        /// <param name="scale">The positive-definite matrix scale matrix <c>V</c>.</param>
        /// <returns>A random vector of observations drawn from this distribution.</returns>
        public static double[,] Random(int degreesOfFreedom, double[,] scale)
        {
            return Random(1, degreesOfFreedom, scale)[0];
        }

        /// <summary>
        ///     Generates a random vector of observations from the current distribution.
        /// </summary>
        /// <param name="degreesOfFreedom">The degrees of freedom <c>n</c>.</param>
        /// <param name="scale">The positive-definite matrix scale matrix <c>V</c>.</param>
        /// <param name="source">
        ///     The random number generator to use as a source of randomness.
        ///     Default is to use <see cref="Accord.Math.Random.Generator.Random" />.
        /// </param>
        /// <returns>A random vector of observations drawn from this distribution.</returns>
        public static double[,] Random(int degreesOfFreedom, double[,] scale, Random source)
        {
            return Random(1, degreesOfFreedom, scale, source)[0];
        }

        /// <summary>
        ///     Generates a random vector of observations from the current distribution.
        /// </summary>
        /// <param name="samples">The number of samples to generate.</param>
        /// <param name="degreesOfFreedom">The degrees of freedom <c>n</c>.</param>
        /// <param name="scale">The positive-definite matrix scale matrix <c>V</c>.</param>
        /// <returns>A random vector of observations drawn from this distribution.</returns>
        public static double[][,] Random(int samples, int degreesOfFreedom, double[,] scale)
        {
            return Random(samples, degreesOfFreedom, scale, Generator.Random);
        }

        /// <summary>
        ///     Generates a random vector of observations from the current distribution.
        /// </summary>
        /// <param name="samples">The number of samples to generate.</param>
        /// <param name="degreesOfFreedom">The degrees of freedom <c>n</c>.</param>
        /// <param name="scale">The positive-definite matrix scale matrix <c>V</c>.</param>
        /// <param name="source">
        ///     The random number generator to use as a source of randomness.
        ///     Default is to use <see cref="Accord.Math.Random.Generator.Random" />.
        /// </param>
        /// <returns>A random vector of observations drawn from this distribution.</returns>
        public static double[][,] Random(int samples, int degreesOfFreedom, double[,] scale, Random source)
        {
            var np = scale.GetLength(0);
            var result = new double[samples].Apply(x => new double[np, np]);
            return Random(samples, degreesOfFreedom, scale, result, source);
        }

        /// <summary>
        ///     Generates a random vector of observations from the current distribution.
        /// </summary>
        /// <param name="samples">The number of samples to generate.</param>
        /// <param name="result">The location where to store the samples.</param>
        /// <param name="degreesOfFreedom">The degrees of freedom <c>n</c>.</param>
        /// <param name="scale">The positive-definite matrix scale matrix <c>V</c>.</param>
        /// <param name="source">
        ///     The random number generator to use as a source of randomness.
        ///     Default is to use <see cref="Accord.Math.Random.Generator.Random" />.
        /// </param>
        /// <returns>A random vector of observations drawn from this distribution.</returns>
        public static double[][,] Random(int samples, int degreesOfFreedom, double[,] scale, double[][,] result,
            Random source)
        {
            var np = scale.GetLength(0);
            var chol = new CholeskyDecomposition(scale);
            var d = new double[np * (np + 1) / 2];
            for (int j = 0, l = 0; j < np; j++)
            {
                for (var k = 0; k <= j; k++, l++)
                {
                    d[l] = chol.LeftTriangularFactor[j, k];
                }
            }

            var r = new double[np * (np + 1) / 2];

            for (var i = 0; i < samples; i++)
            {
                wshrt(d, degreesOfFreedom, np, source, r);

                var ret = result[i];
                for (int j = 0, l = 0; j < np; j++)
                {
                    for (var k = 0; k <= j; k++, l++)
                    {
                        ret[j, k] = ret[k, j] = r[l];
                    }
                }
            }

            return result;
        }

        /******************************************************************************/
        /*
          Purpose:

            RNORM returns two independent standard random normal deviates.

          Discussion:

            This routine sets U1 and U2 to two independent standardized
            random normal deviates.   This is a version of the
            method given in Knuth.

          Licensing:

            This code is distributed under the GNU LGPL license.

          Modified:

            16 April 2014

          Author:

            Original FORTRAN77 version by William Smith, Ronald Hocking.
            This C version by John Burkardt.

          Reference:

            Donald Knuth,
            The Art of Computer Programming,
            Volume 2, Seminumerical Algorithms,
            Third Edition,
            Addison Wesley, 1997,
            ISBN: 0201896842,
            LC: QA76.6.K64.

          Parameters:

            Input/output, int *SEED, a seed for the random
            number generator.

            Output, double *U1, *U2, two standard random normal deviates.
        */
        private static void rnorm(Random random, out double u1, out double u2)
        {
            for (;;)
            {
                var x = random.NextDouble();
                var y = random.NextDouble();
                x = 2.0 * x - 1.0;
                y = 2.0 * y - 1.0;
                var s = x * x + y * y;

                if (s <= 1.0)
                {
                    s = System.Math.Sqrt(-2.0 * System.Math.Log(s) / s);
                    u1 = x * s;
                    u2 = y * s;
                    break;
                }
            }
        }

        /******************************************************************************/
        /*
          Purpose:

            WSHRT returns a random Wishart variate.

          Discussion:

            This routine is a Wishart variate generator.

            On output, SA is an upper-triangular matrix of size NP * NP,
            written in linear form, column ordered, whose elements have a
            Wishart(N, SIGMA) distribution.

          Licensing:

            This code is distributed under the GNU LGPL license.

          Modified:

            16 April 2014

          Author:

            Original FORTRAN77 version by William Smith, Ronald Hocking.
            This C version by John Burkardt.

          Reference:

            William Smith, Ronald Hocking,
            Algorithm AS 53, Wishart Variate Generator,
            Applied Statistics,
            Volume 21, Number 3, pages 341-345, 1972.

          Parameters:

            Input, double D[NP*(NP+1)/2], the upper triangular array that
            represents the Cholesky factor of the correlation matrix SIGMA.
            D is stored in column-major form.

            Input, int N, the number of degrees of freedom.
            1 <= N <= NP.

            Input, int NP, the size of variables.

            Input/output, int *SEED, a seed for the random
            number generator.

            Output, double WSHART[NP*(NP+1)/2], a sample from the
            Wishart distribution.
        */
        private static void wshrt(double[] d, int n, int np, Random seed, double[] sa)
        {
            var k = 0;
            var nnp = np * (np + 1) / 2;
            /*
              Load SB with independent normal (0, 1) variates.
            */
            var sb = new double[nnp];

            while (k < nnp)
            {
                rnorm(seed, out var u1, out var u2);

                sb[k] = u1;
                k = k + 1;

                if (k < nnp)
                {
                    sb[k] = u2;
                    k = k + 1;
                }
            }

            /*
              Load diagonal elements with square root of chi-square variates.
            */
            var ns = 0;

            for (var i = 1; i <= np; i++)
            {
                double df = n - i + 1;
                ns = ns + i;
                sb[ns - 1] = System.Math.Sqrt(2.0 *
                                              GammaDistribution.Random(df / 2.0,
                                                  1.0, seed));
            }

            var nr = 1;

            for (var i = 1; i <= np; i++)
            {
                nr = nr + i - 1;
                for (var j = i; j <= np; j++)
                {
                    var ip = nr;
                    var nq = j * (j - 1) / 2 + i - 1;
                    var c = 0.0;
                    for (k = i; k <= j; k++)
                    {
                        ip = ip + k - 1;
                        nq = nq + 1;
                        c = c + sb[ip - 1] * d[nq - 1];
                    }

                    sa[ip - 1] = c;
                }
            }

            for (var i = 1; i <= np; i++)
            {
                var ii = np - i + 1;
                var nq = nnp - np;
                for (var j = 1; j <= i; j++)
                {
                    var ip = ii * (ii - 1) / 2;
                    var c = 0.0;
                    for (k = i; k <= np; k++)
                    {
                        ip = ip + 1;
                        nq = nq + 1;
                        c = c + sa[ip - 1] * sa[nq - 1];
                    }

                    sa[nq - 1] = c;
                    nq = nq - 2 * np + i + j - 1;
                }
            }
        }
    }
}