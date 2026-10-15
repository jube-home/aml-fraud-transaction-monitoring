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

namespace Accord.Statistics
{
    public static partial class Measures
    {
        /// <summary>
        ///     Computes the mean value across all dimensions of the given matrix.
        /// </summary>
        public static double Mean(this double[][] matrix)
        {
            return matrix.Sum() / matrix.GetTotalLength();
        }

        /// <summary>
        ///     Computes the mean value across all dimensions of the given matrix.
        /// </summary>
        public static double Mean(this double[,] matrix)
        {
            return matrix.Sum() / matrix.GetTotalLength();
        }

        /// <summary>
        ///     Calculates the matrix Mean vector.
        /// </summary>
        /// <param name="matrix">A matrix whose means will be calculated.</param>
        /// <param name="dimension">
        ///     The dimension along which the means will be calculated. Pass
        ///     0 to compute a row vector containing the mean of each column,
        ///     or 1 to compute a column vector containing the mean of each row.
        ///     Default value is 0.
        /// </param>
        /// <returns>Returns a vector containing the means of the given matrix.</returns>
        /// <example>
        ///     <code>
        ///   double[,] matrix = 
        ///   {
        ///      { 2, -1.0, 5 },
        ///      { 7,  0.5, 9 },
        ///   };
        ///   
        ///   // column means are equal to (4.5, -0.25, 7.0)
        ///   double[] colMeans = Stats.Mean(matrix, 0);
        ///     
        ///   // row means are equal to (2.0, 5.5)
        ///   double[] rowMeans = Stats.Mean(matrix, 1);
        ///   </code>
        /// </example>
        public static double[] Mean(this double[,] matrix, int dimension)
        {
            var rows = matrix.GetLength(0);
            var cols = matrix.GetLength(1);
            double[] mean;

            if (dimension == 0)
            {
                mean = new double[cols];
                double N = rows;

                // for each column
                for (var j = 0; j < cols; j++)
                {
                    // for each row
                    for (var i = 0; i < rows; i++)
                    {
                        mean[j] += matrix[i, j];
                    }

                    mean[j] /= N;
                }
            }
            else if (dimension == 1)
            {
                mean = new double[rows];
                double N = cols;

                // for each row
                for (var j = 0; j < rows; j++)
                {
                    // for each column
                    for (var i = 0; i < cols; i++)
                    {
                        mean[j] += matrix[j, i];
                    }

                    mean[j] /= N;
                }
            }
            else
            {
                throw new ArgumentException("Invalid dimension.", "dimension");
            }

            return mean;
        }

        /// <summary>
        ///     Calculates the matrix Mean vector.
        /// </summary>
        /// <param name="matrix">A matrix whose means will be calculated.</param>
        /// <param name="dimension">
        ///     The dimension along which the means will be calculated. Pass
        ///     0 to compute a row vector containing the mean of each column,
        ///     or 1 to compute a column vector containing the mean of each row.
        ///     Default value is 0.
        /// </param>
        /// <returns>Returns a vector containing the means of the given matrix.</returns>
        /// <example>
        ///     <code>
        ///   double[][] matrix = 
        ///   {
        ///       new double[] { 2, -1.0, 5 },
        ///       new double[] { 7,  0.5, 9 },
        ///   };
        ///   
        ///   // column means are equal to (4.5, -0.25, 7.0)
        ///   double[] colMeans = Stats.Mean(matrix, 0);
        ///     
        ///   // row means are equal to (2.0, 5.5)
        ///   double[] rowMeans = Stats.Mean(matrix, 1);
        ///   </code>
        /// </example>
        public static double[] Mean(this double[][] matrix, int dimension)
        {
            var rows = matrix.Length;
            if (rows == 0)
            {
                return new double[0];
            }

            double[] mean;

            if (dimension == 0)
            {
                var cols = matrix[0].Length;
                mean = new double[cols];
                double N = rows;

                // for each column
                for (var j = 0; j < cols; j++)
                {
                    // for each row
                    for (var i = 0; i < rows; i++)
                    {
                        mean[j] += matrix[i][j];
                    }

                    mean[j] = mean[j] / N;
                }
            }
            else if (dimension == 1)
            {
                mean = new double[rows];

                // for each row
                for (var j = 0; j < rows; j++)
                {
                    // for each column
                    for (var i = 0; i < matrix[j].Length; i++)
                    {
                        mean[j] += matrix[j][i];
                    }

                    mean[j] = mean[j] / matrix[j].Length;
                }
            }
            else
            {
                throw new ArgumentException("Invalid dimension.", "dimension");
            }

            return mean;
        }

        /// <summary>
        ///     Calculates the matrix Mean vector.
        /// </summary>
        /// <param name="matrix">A matrix whose means will be calculated.</param>
        /// <param name="sums">The sum vector containing already calculated sums for each column of the matrix.</param>
        /// <returns>Returns a vector containing the means of the given matrix.</returns>
        public static double[] Mean(this double[,] matrix, double[] sums)
        {
            var rows = matrix.GetLength(0);
            var cols = matrix.GetLength(1);

            var mean = new double[cols];
            double N = rows;

            for (var j = 0; j < cols; j++)
            {
                mean[j] = sums[j] / N;
            }

            return mean;
        }

        /// <summary>
        ///     Calculates the matrix Mean vector.
        /// </summary>
        /// <param name="matrix">A matrix whose means will be calculated.</param>
        /// <param name="sums">The sum vector containing already calculated sums for each column of the matrix.</param>
        /// <returns>Returns a vector containing the means of the given matrix.</returns>
        public static double[] Mean(this double[][] matrix, double[] sums)
        {
            var rows = matrix.Length;
            var cols = matrix[0].Length;

            var mean = new double[cols];
            double N = rows;

            for (var j = 0; j < sums.Length; j++)
            {
                mean[j] = sums[j] / N;
            }

            return mean;
        }

        /// <summary>
        ///     Calculates the matrix Standard Deviations vector.
        /// </summary>
        /// <param name="matrix">A matrix whose deviations will be calculated.</param>
        /// <returns>Returns a vector containing the standard deviations of the given matrix.</returns>
        public static double[] StandardDeviation(this double[,] matrix)
        {
            return StandardDeviation(matrix, Mean(matrix, 0));
        }

        /// <summary>
        ///     Calculates the matrix Standard Deviations vector.
        /// </summary>
        /// <param name="matrix">A matrix whose deviations will be calculated.</param>
        /// <param name="means">The mean vector containing already calculated means for each column of the matrix.</param>
        /// <returns>Returns a vector containing the standard deviations of the given matrix.</returns>
        public static double[] StandardDeviation(this double[,] matrix, double[] means)
        {
            return Variance(matrix, means).Sqrt();
        }

        /// <summary>
        ///     Calculates the matrix Standard Deviations vector.
        /// </summary>
        /// <param name="matrix">A matrix whose deviations will be calculated.</param>
        /// <param name="means">The mean vector containing already calculated means for each column of the matrix.</param>
        /// <param name="unbiased">
        ///     Pass true to compute the standard deviation using the sample variance.
        ///     Pass false to compute it using the population variance. See remarks
        ///     for more details.
        /// </param>
        /// <remarks>
        ///     <para>
        ///         Setting <paramref name="unbiased" /> to <c>true</c> will make this method
        ///         compute the standard deviation σ using the sample variance, which is an unbiased
        ///         estimator of the true population variance. Setting this parameter to true will
        ///         thus compute σ using the following formula:
        ///     </para>
        ///     <code>
        ///                           N
        ///        σ² = 1 / (N - 1)  ∑   (x_i − μ)²
        ///                           i=1
        ///     </code>
        ///     <para>
        ///         Setting <paramref name="unbiased" /> to <c>false</c> will assume the given values
        ///         already represent the whole population, and will compute the population variance
        ///         using the formula:
        ///     </para>
        ///     <code>
        ///                           N
        ///        σ² =   (1 / N)    ∑   (x_i − μ)²
        ///                           i=1
        ///     </code>
        /// </remarks>
        /// <returns>Returns a vector containing the standard deviations of the given matrix.</returns>
        public static double[] StandardDeviation(this double[][] matrix, double[] means, bool unbiased = true)
        {
            return Variance(matrix, means, unbiased).Sqrt();
        }

        /// <summary>
        ///     Calculates the matrix Standard Deviations vector.
        /// </summary>
        /// <param name="matrix">A matrix whose deviations will be calculated.</param>
        /// <param name="unbiased">
        ///     Pass true to compute the standard deviation using the sample variance.
        ///     Pass false to compute it using the population variance. See remarks
        ///     for more details.
        /// </param>
        /// <remarks>
        ///     <para>
        ///         Setting <paramref name="unbiased" /> to <c>true</c> will make this method
        ///         compute the standard deviation σ using the sample variance, which is an unbiased
        ///         estimator of the true population variance. Setting this parameter to true will
        ///         thus compute σ using the following formula:
        ///     </para>
        ///     <code>
        ///                           N
        ///        σ² = 1 / (N - 1)  ∑   (x_i − μ)²
        ///                           i=1
        ///     </code>
        ///     <para>
        ///         Setting <paramref name="unbiased" /> to <c>false</c> will assume the given values
        ///         already represent the whole population, and will compute the population variance
        ///         using the formula:
        ///     </para>
        ///     <code>
        ///                           N
        ///        σ² =   (1 / N)    ∑   (x_i − μ)²
        ///                           i=1
        ///     </code>
        /// </remarks>
        /// <returns>Returns a vector containing the standard deviations of the given matrix.</returns>
        public static double[] StandardDeviation(this double[][] matrix, bool unbiased = true)
        {
            return StandardDeviation(matrix, Mean(matrix, 0), unbiased);
        }

        /// <summary>
        ///     Calculates the matrix Variance vector.
        /// </summary>
        /// <param name="matrix">A matrix whose variances will be calculated.</param>
        /// <returns>Returns a vector containing the variances of the given matrix.</returns>
        public static double[] Variance(this double[,] matrix)
        {
            return Variance(matrix, Mean(matrix, 0));
        }

        /// <summary>
        ///     Calculates the matrix Variance vector.
        /// </summary>
        /// <param name="matrix">A matrix whose variances will be calculated.</param>
        /// <param name="means">
        ///     The mean vector containing already
        ///     calculated means for each column of the matrix.
        /// </param>
        /// <returns>Returns a vector containing the variances of the given matrix.</returns>
        public static double[] Variance(this double[,] matrix, double[] means)
        {
            var rows = matrix.GetLength(0);
            var cols = matrix.GetLength(1);
            double N = rows;

            var variance = new double[cols];

            // for each column (for each variable)
            for (var j = 0; j < cols; j++)
            {
                var sum1 = 0.0;
                var sum2 = 0.0;

                // for each row (observation of the variable)
                for (var i = 0; i < rows; i++)
                {
                    var x = matrix[i, j] - means[j];
                    sum1 += x;
                    sum2 += x * x;
                }

                // calculate the variance
                variance[j] = (sum2 - sum1 * sum1 / N) / (N - 1);
            }

            return variance;
        }

        /// <summary>
        ///     Calculates the matrix Variance vector.
        /// </summary>
        /// <param name="matrix">A matrix whose variances will be calculated.</param>
        /// <returns>Returns a vector containing the variances of the given matrix.</returns>
        public static double[] Variance(this double[][] matrix)
        {
            return Variance(matrix, Mean(matrix, 0));
        }

        /// <summary>
        ///     Calculates the matrix Variance vector.
        /// </summary>
        /// <param name="matrix">A matrix whose variances will be calculated.</param>
        /// <param name="means">The mean vector containing already calculated means for each column of the matrix.</param>
        /// <param name="unbiased">
        ///     Pass true to compute the sample variance; or pass false to compute
        ///     the population variance. See remarks for more details.
        /// </param>
        /// <remarks>
        ///     <para>
        ///         Setting <paramref name="unbiased" /> to <c>true</c> will make this method
        ///         compute the variance σ² using the sample variance, which is an unbiased
        ///         estimator of the true population variance. Setting this parameter to true
        ///         will thus compute σ² using the following formula:
        ///     </para>
        ///     <code>
        ///                           N
        ///        σ² = 1 / (N - 1)  ∑   (x_i − μ)²
        ///                           i=1
        ///     </code>
        ///     <para>
        ///         Setting <paramref name="unbiased" /> to <c>false</c> will assume the given values
        ///         already represent the whole population, and will compute the population variance
        ///         using the formula:
        ///     </para>
        ///     <code>
        ///                           N
        ///        σ² =   (1 / N)    ∑   (x_i − μ)²
        ///                           i=1
        ///     </code>
        /// </remarks>
        /// <returns>Returns a vector containing the variances of the given matrix.</returns>
        public static double[] Variance(this double[][] matrix, double[] means, bool unbiased = true)
        {
            var rows = matrix.Length;
            if (rows == 0)
            {
                return new double[0];
            }

            var cols = matrix[0].Length;
            double N = rows;

            var variance = new double[cols];

            // for each column (for each variable)
            for (var j = 0; j < cols; j++)
            {
                var sum1 = 0.0;
                var sum2 = 0.0;

                // for each row (observation of the variable)
                for (var i = 0; i < rows; i++)
                {
                    var x = matrix[i][j] - means[j];
                    sum1 += x;
                    sum2 += x * x;
                }

                if (unbiased)
                {
                    // calculate the population variance
                    variance[j] = (sum2 - sum1 * sum1 / N) / (N - 1);
                }
                else
                {
                    // calculate the sample variance
                    variance[j] = (sum2 - sum1 * sum1 / N) / N;
                }
            }

            return variance;
        }

        /// <summary>
        ///     Calculates the matrix Medians vector.
        /// </summary>
        /// <param name="matrix">A matrix whose medians will be calculated.</param>
        /// <param name="type">The quartile definition that should be used. See <see cref="QuantileMethod" /> for datails.</param>
        /// <returns>Returns a vector containing the medians of the given matrix.</returns>
        public static double[] Median(this double[,] matrix, QuantileMethod type = QuantileMethod.Default)
        {
            var rows = matrix.Rows();
            var cols = matrix.Columns();

            var medians = new double[cols];
            var data = new double[rows];

            for (var i = 0; i < cols; i++)
            {
                medians[i] = matrix.GetColumn(i, data).Median(type: type, inPlace: true);
            }

            return medians;
        }

        /// <summary>
        ///     Calculates the matrix Medians vector.
        /// </summary>
        /// <param name="matrix">A matrix whose medians will be calculated.</param>
        /// <param name="type">The quartile definition that should be used. See <see cref="QuantileMethod" /> for datails.</param>
        /// <returns>Returns a vector containing the medians of the given matrix.</returns>
        public static double[] Median(this double[][] matrix, QuantileMethod type = QuantileMethod.Default)
        {
            var rows = matrix.Rows();
            var cols = matrix.Columns();

            var medians = new double[cols];
            var data = new double[rows];

            for (var i = 0; i < cols; i++)
            {
                medians[i] = matrix.GetColumn(i, data).Median(type: type, inPlace: true);
            }

            return medians;
        }

        /// <summary>
        ///     Computes the Quartiles of the given values.
        /// </summary>
        /// <param name="matrix">A matrix whose medians and quartiles will be calculated.</param>
        /// <param name="range">The inter-quartile range for the values.</param>
        /// <param name="type">The quartile definition that should be used. See <see cref="QuantileMethod" /> for datails.</param>
        /// <returns>The second quartile, the median of the given data.</returns>
        public static double[] Quartiles(this double[,] matrix, out DoubleRange[] range,
            QuantileMethod type = QuantileMethod.Default)
        {
            double[] q1, q3;
            var median = Quartiles(matrix, out q1, out q3, type);

            range = new DoubleRange[median.Length];
            for (var i = 0; i < range.Length; i++)
            {
                range[i] = new DoubleRange(q1[i], q3[i]);
            }

            return median;
        }

        /// <summary>
        ///     Computes the Quartiles of the given values.
        /// </summary>
        /// <param name="matrix">A matrix whose medians and quartiles will be calculated.</param>
        /// <param name="range">The inter-quartile range for the values.</param>
        /// <param name="type">The quartile definition that should be used. See <see cref="QuantileMethod" /> for datails.</param>
        /// <returns>The second quartile, the median of the given data.</returns>
        public static double[] Quartiles(this double[][] matrix, out DoubleRange[] range,
            QuantileMethod type = QuantileMethod.Default)
        {
            double[] q1, q3;
            var median = Quartiles(matrix, out q1, out q3, type);

            range = new DoubleRange[median.Length];
            for (var i = 0; i < range.Length; i++)
            {
                range[i] = new DoubleRange(q1[i], q3[i]);
            }

            return median;
        }

        /// <summary>
        ///     Computes the Quartiles of the given values.
        /// </summary>
        /// <param name="matrix">A matrix whose medians and quartiles will be calculated.</param>
        /// <param name="q1">The first quartile for each column.</param>
        /// <param name="q3">The third quartile for each column.</param>
        /// <param name="type">The quartile definition that should be used. See <see cref="QuantileMethod" /> for datails.</param>
        /// <returns>The second quartile, the median of the given data.</returns>
        public static double[] Quartiles(double[][] matrix, out double[] q1, out double[] q3,
            QuantileMethod type = QuantileMethod.Default)
        {
            if (matrix == null)
            {
                throw new ArgumentNullException("matrix");
            }

            var rows = matrix.Rows();
            var cols = matrix.Columns();

            var medians = new double[cols];
            q1 = new double[cols];
            q3 = new double[cols];

            var data = new double[rows];
            for (var i = 0; i < cols; i++)
            {
                medians[i] = matrix.GetColumn(i, data).Quartiles(out q1[i], out q3[i], false,
                    type, true);
            }

            return medians;
        }

        /// <summary>
        ///     Computes the Quartiles of the given values.
        /// </summary>
        /// <param name="matrix">A matrix whose medians and quartiles will be calculated.</param>
        /// <param name="q1">The first quartile for each column.</param>
        /// <param name="q3">The third quartile for each column.</param>
        /// <param name="type">The quartile definition that should be used. See <see cref="QuantileMethod" /> for datails.</param>
        /// <returns>The second quartile, the median of the given data.</returns>
        public static double[] Quartiles(double[,] matrix, out double[] q1, out double[] q3,
            QuantileMethod type = QuantileMethod.Default)
        {
            if (matrix == null)
            {
                throw new ArgumentNullException("matrix");
            }

            var rows = matrix.Rows();
            var cols = matrix.Columns();

            var medians = new double[cols];
            q1 = new double[cols];
            q3 = new double[cols];

            var data = new double[rows];
            for (var i = 0; i < cols; i++)
            {
                medians[i] = matrix.GetColumn(i, data).Quartiles(out q1[i], out q3[i], false,
                    type, true);
            }

            return medians;
        }

        /// <summary>
        ///     Calculates the matrix Modes vector.
        /// </summary>
        /// <param name="matrix">A matrix whose modes will be calculated.</param>
        /// <returns>Returns a vector containing the modes of the given matrix.</returns>
        public static T[] Mode<T>(this T[,] matrix)
        {
            var cols = matrix.GetLength(1);

            var mode = new T[cols];

            for (var i = 0; i < cols; i++)
            {
                var col = matrix.GetColumn(i);

                mode[i] = Mode(col, out _, true);
            }

            return mode;
        }

        /// <summary>
        ///     Calculates the matrix Modes vector.
        /// </summary>
        /// <param name="matrix">A matrix whose modes will be calculated.</param>
        /// <returns>Returns a vector containing the modes of the given matrix.</returns>
        public static T[] Mode<T>(this T[][] matrix)
        {
            var cols = matrix[0].Length;

            var mode = new T[cols];

            for (var i = 0; i < cols; i++)
            {
                var col = matrix.GetColumn(i);

                mode[i] = Mode(col, out _, true);
            }

            return mode;
        }

        /// <summary>
        ///     Computes the Skewness for the given values.
        /// </summary>
        /// <remarks>
        ///     Skewness characterizes the degree of asymmetry of a distribution
        ///     around its mean. Positive skewness indicates a distribution with
        ///     an asymmetric tail extending towards more positive values. Negative
        ///     skewness indicates a distribution with an asymmetric tail extending
        ///     towards more negative values.
        /// </remarks>
        /// <param name="matrix">A number matrix containing the matrix values.</param>
        /// <param name="unbiased">
        ///     True to compute the unbiased estimate of the population
        ///     skewness, false otherwise. Default is true (compute the
        ///     unbiased estimator).
        /// </param>
        /// <returns>The skewness of the given data.</returns>
        public static double[] Skewness(this double[,] matrix, bool unbiased = true)
        {
            var means = Mean(matrix, 0);
            return Skewness(matrix, means, unbiased);
        }

        /// <summary>
        ///     Computes the Skewness vector for the given matrix.
        /// </summary>
        /// <remarks>
        ///     Skewness characterizes the degree of asymmetry of a distribution
        ///     around its mean. Positive skewness indicates a distribution with
        ///     an asymmetric tail extending towards more positive values. Negative
        ///     skewness indicates a distribution with an asymmetric tail extending
        ///     towards more negative values.
        /// </remarks>
        /// <param name="matrix">A number array containing the vector values.</param>
        /// <param name="means">The mean value for the given values, if already known.</param>
        /// <param name="unbiased">
        ///     True to compute the unbiased estimate of the population
        ///     skewness, false otherwise. Default is true (compute the
        ///     unbiased estimator).
        /// </param>
        /// <returns>The skewness of the given data.</returns>
        public static double[] Skewness(this double[,] matrix, double[] means, bool unbiased = true)
        {
            var n = matrix.GetLength(0);
            var skewness = new double[means.Length];

            for (var j = 0; j < means.Length; j++)
            {
                double s2 = 0;
                double s3 = 0;

                for (var i = 0; i < n; i++)
                {
                    var dev = matrix[i, j] - means[j];

                    s2 += dev * dev;
                    s3 += dev * dev * dev;
                }

                var m2 = s2 / n;
                var m3 = s3 / n;

                var g = m3 / System.Math.Pow(m2, 3 / 2.0);

                if (unbiased)
                {
                    var a = System.Math.Sqrt(n * (n - 1));
                    double b = n - 2;
                    skewness[j] = a / b * g;
                }
                else
                {
                    skewness[j] = g;
                }
            }

            return skewness;
        }

        /// <summary>
        ///     Computes the Skewness for the given values.
        /// </summary>
        /// <remarks>
        ///     Skewness characterizes the degree of asymmetry of a distribution
        ///     around its mean. Positive skewness indicates a distribution with
        ///     an asymmetric tail extending towards more positive values. Negative
        ///     skewness indicates a distribution with an asymmetric tail extending
        ///     towards more negative values.
        /// </remarks>
        /// <param name="matrix">A number matrix containing the matrix values.</param>
        /// <param name="unbiased">
        ///     True to compute the unbiased estimate of the population
        ///     skewness, false otherwise. Default is true (compute the
        ///     unbiased estimator).
        /// </param>
        /// <returns>The skewness of the given data.</returns>
        public static double[] Skewness(this double[][] matrix, bool unbiased = true)
        {
            var means = Mean(matrix, 0);
            return Skewness(matrix, means, unbiased);
        }

        /// <summary>
        ///     Computes the Skewness vector for the given matrix.
        /// </summary>
        /// <remarks>
        ///     Skewness characterizes the degree of asymmetry of a distribution
        ///     around its mean. Positive skewness indicates a distribution with
        ///     an asymmetric tail extending towards more positive values. Negative
        ///     skewness indicates a distribution with an asymmetric tail extending
        ///     towards more negative values.
        /// </remarks>
        /// <param name="matrix">A number array containing the vector values.</param>
        /// <param name="means">The column means, if known.</param>
        /// <param name="unbiased">
        ///     True to compute the unbiased estimate of the population
        ///     skewness, false otherwise. Default is true (compute the
        ///     unbiased estimator).
        /// </param>
        /// <returns>The skewness of the given data.</returns>
        public static double[] Skewness(this double[][] matrix, double[] means, bool unbiased = true)
        {
            var n = matrix.Length;
            var skewness = new double[means.Length];

            for (var j = 0; j < means.Length; j++)
            {
                double s2 = 0;
                double s3 = 0;

                for (var i = 0; i < matrix.Length; i++)
                {
                    var dev = matrix[i][j] - means[j];

                    s2 += dev * dev;
                    s3 += dev * dev * dev;
                }

                var m2 = s2 / n;
                var m3 = s3 / n;

                var g = m3 / System.Math.Pow(m2, 3 / 2.0);

                if (unbiased)
                {
                    var a = System.Math.Sqrt(n * (n - 1));
                    double b = n - 2;
                    skewness[j] = a / b * g;
                }
                else
                {
                    skewness[j] = g;
                }
            }

            return skewness;
        }

        /// <summary>
        ///     Computes the Kurtosis vector for the given matrix.
        /// </summary>
        /// <remarks>
        ///     The framework uses the same definition used by default in SAS and SPSS.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="unbiased">
        ///     True to compute the unbiased estimate of the population
        ///     kurtosis, false otherwise. Default is true (compute the
        ///     unbiased estimator).
        /// </param>
        /// <returns>The kurtosis vector of the given data.</returns>
        public static double[] Kurtosis(this double[,] matrix, bool unbiased = true)
        {
            var means = Mean(matrix, 0);
            return Kurtosis(matrix, means, unbiased);
        }

        /// <summary>
        ///     Computes the sample Kurtosis vector for the given matrix.
        /// </summary>
        /// <remarks>
        ///     The framework uses the same definition used by default in SAS and SPSS.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="means">The mean value of the given values, if already known.</param>
        /// <param name="unbiased">
        ///     True to compute the unbiased estimate of the population
        ///     kurtosis, false otherwise. Default is true (compute the
        ///     unbiased estimator).
        /// </param>
        /// <returns>The sample kurtosis vector of the given data.</returns>
        public static double[] Kurtosis(this double[,] matrix, double[] means, bool unbiased = true)
        {
            var n = matrix.GetLength(0);
            var m = matrix.GetLength(1);

            var kurtosis = new double[m];

            for (var j = 0; j < kurtosis.Length; j++)
            {
                double s2 = 0;
                double s4 = 0;

                for (var i = 0; i < n; i++)
                {
                    var dev = matrix[i, j] - means[j];

                    s2 += dev * dev;
                    s4 += dev * dev * dev * dev;
                }

                double dn = n;
                var m2 = s2 / n;
                var m4 = s4 / n;

                if (unbiased)
                {
                    var v = s2 / (dn - 1);


                    var a = dn * (dn + 1) / ((dn - 1) * (dn - 2) * (dn - 3));
                    var b = s4 / (v * v);
                    var c = (dn - 1) * (dn - 1) / ((dn - 2) * (dn - 3));

                    kurtosis[j] = a * b - 3 * c;
                }
                else
                {
                    kurtosis[j] = m4 / (m2 * m2) - 3;
                }
            }

            return kurtosis;
        }

        /// <summary>
        ///     Computes the Kurtosis vector for the given matrix.
        /// </summary>
        /// <remarks>
        ///     The framework uses the same definition used by default in SAS and SPSS.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="unbiased">
        ///     True to compute the unbiased estimate of the population
        ///     kurtosis, false otherwise. Default is true (compute the
        ///     unbiased estimator).
        /// </param>
        /// <returns>The kurtosis vector of the given data.</returns>
        public static double[] Kurtosis(this double[][] matrix, bool unbiased = true)
        {
            var means = Mean(matrix, 0);
            return Kurtosis(matrix, means, unbiased);
        }

        /// <summary>
        ///     Computes the Kurtosis vector for the given matrix.
        /// </summary>
        /// <remarks>
        ///     The framework uses the same definition used by default in SAS and SPSS.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="means">The mean value of the given values, if already known.</param>
        /// <param name="unbiased">
        ///     True to compute the unbiased estimate of the population
        ///     kurtosis, false otherwise. Default is true (compute the
        ///     unbiased estimator).
        /// </param>
        /// <returns>The kurtosis vector of the given data.</returns>
        public static double[] Kurtosis(this double[][] matrix, double[] means, bool unbiased = true)
        {
            var n = matrix.Length;
            var m = matrix[0].Length;

            var kurtosis = new double[m];

            for (var j = 0; j < kurtosis.Length; j++)
            {
                double s2 = 0;
                double s4 = 0;

                for (var i = 0; i < matrix.Length; i++)
                {
                    var dev = matrix[i][j] - means[j];

                    s2 += dev * dev;
                    s4 += dev * dev * dev * dev;
                }

                double dn = n;
                var m2 = s2 / n;
                var m4 = s4 / n;

                if (unbiased)
                {
                    var v = s2 / (dn - 1);

                    var a = dn * (dn + 1) / ((dn - 1) * (dn - 2) * (dn - 3));
                    var b = s4 / (v * v);
                    var c = (dn - 1) * (dn - 1) / ((dn - 2) * (dn - 3));

                    kurtosis[j] = a * b - 3 * c;
                }
                else
                {
                    kurtosis[j] = m4 / (m2 * m2) - 3;
                }
            }

            return kurtosis;
        }

        /// <summary>
        ///     Computes the Standard Error vector for a given matrix.
        /// </summary>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <returns>Returns the standard error vector for the matrix.</returns>
        public static double[] StandardError(double[,] matrix)
        {
            return StandardError(matrix.GetLength(0), StandardDeviation(matrix));
        }

        /// <summary>
        ///     Computes the Standard Error vector for a given matrix.
        /// </summary>
        /// <param name="samples">The number of samples in the matrix.</param>
        /// <param name="standardDeviations">The values' standard deviation vector, if already known.</param>
        /// <returns>Returns the standard error vector for the matrix.</returns>
        public static double[] StandardError(int samples, double[] standardDeviations)
        {
            var standardErrors = new double[standardDeviations.Length];

            var sqrtN = System.Math.Sqrt(samples);
            for (var i = 0; i < standardDeviations.Length; i++)
            {
                standardErrors[i] = standardDeviations[i] / sqrtN;
            }

            return standardErrors;
        }

        /// <summary>
        ///     Calculates the covariance matrix of a sample matrix.
        /// </summary>
        /// <remarks>
        ///     In statistics and probability theory, the covariance matrix is a matrix of
        ///     covariances between elements of a vector. It is the natural generalization
        ///     to higher dimensions of the concept of the variance of a scalar-valued
        ///     random variable.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <returns>The covariance matrix.</returns>
        public static double[,] Covariance(this double[,] matrix)
        {
            return Covariance(matrix, Mean(matrix, 0));
        }

        /// <summary>
        ///     Calculates the covariance matrix of a sample matrix.
        /// </summary>
        /// <remarks>
        ///     In statistics and probability theory, the covariance matrix is a matrix of
        ///     covariances between elements of a vector. It is the natural generalization
        ///     to higher dimensions of the concept of the variance of a scalar-valued
        ///     random variable.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="dimension">
        ///     The dimension of the matrix to consider as observations. Pass 0 if the matrix has
        ///     observations as rows and variables as columns, pass 1 otherwise. Default is 0.
        /// </param>
        /// <returns>The covariance matrix.</returns>
        public static double[,] Covariance(this double[,] matrix, int dimension)
        {
            return Scatter(matrix, Mean(matrix, dimension), matrix.GetLength(dimension) - 1, dimension);
        }

        /// <summary>
        ///     Calculates the covariance matrix of a sample matrix.
        /// </summary>
        /// <remarks>
        ///     In statistics and probability theory, the covariance matrix is a matrix of
        ///     covariances between elements of a vector. It is the natural generalization
        ///     to higher dimensions of the concept of the variance of a scalar-valued
        ///     random variable.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="means">The mean value of the given values, if already known.</param>
        /// <returns>The covariance matrix.</returns>
        public static double[,] Covariance(this double[,] matrix, double[] means)
        {
            return Scatter(matrix, means, matrix.GetLength(0) - 1, 0);
        }

        /// <summary>
        ///     Calculates the scatter matrix of a sample matrix.
        /// </summary>
        /// <remarks>
        ///     By dividing the Scatter matrix by the sample size, we get the population
        ///     Covariance matrix. By dividing by the sample size minus one, we get the
        ///     sample Covariance matrix.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="means">The mean value of the given values, if already known.</param>
        /// <returns>The covariance matrix.</returns>
        public static double[,] Scatter(this double[,] matrix, double[] means)
        {
            return Scatter(matrix, means, 1.0, 0);
        }

        /// <summary>
        ///     Calculates the scatter matrix of a sample matrix.
        /// </summary>
        /// <remarks>
        ///     By dividing the Scatter matrix by the sample size, we get the population
        ///     Covariance matrix. By dividing by the sample size minus one, we get the
        ///     sample Covariance matrix.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="means">The mean value of the given values, if already known.</param>
        /// <param name="divisor">A real number to divide each member of the matrix.</param>
        /// <returns>The covariance matrix.</returns>
        public static double[,] Scatter(this double[,] matrix, double[] means, double divisor)
        {
            return Scatter(matrix, means, divisor, 0);
        }

        /// <summary>
        ///     Calculates the scatter matrix of a sample matrix.
        /// </summary>
        /// <remarks>
        ///     By dividing the Scatter matrix by the sample size, we get the population
        ///     Covariance matrix. By dividing by the sample size minus one, we get the
        ///     sample Covariance matrix.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="means">The mean value of the given values, if already known.</param>
        /// <param name="dimension">
        ///     Pass 0 to if mean vector is a row vector, 1 otherwise. Default value is 0.
        /// </param>
        /// <returns>The covariance matrix.</returns>
        public static double[,] Scatter(this double[,] matrix, double[] means, int dimension)
        {
            return Scatter(matrix, means, 1.0, dimension);
        }

        /// <summary>
        ///     Calculates the scatter matrix of a sample matrix.
        /// </summary>
        /// <remarks>
        ///     By dividing the Scatter matrix by the sample size, we get the population
        ///     Covariance matrix. By dividing by the sample size minus one, we get the
        ///     sample Covariance matrix.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="means">The mean value of the given values, if already known.</param>
        /// <param name="divisor">A real number to divide each member of the matrix.</param>
        /// <param name="dimension">
        ///     Pass 0 if the mean vector is a row vector, 1 otherwise. Default value is 0.
        /// </param>
        /// <returns>The covariance matrix.</returns>
        public static double[,] Scatter(this double[,] matrix, double[] means, double divisor, int dimension)
        {
            var rows = matrix.GetLength(0);
            var cols = matrix.GetLength(1);

            double[,] cov;

            if (dimension == 0)
            {
                if (means.Length != cols)
                {
                    throw new ArgumentException("Length of the mean vector should equal the number of columns",
                        "means");
                }

                cov = new double[cols, cols];
                for (var i = 0; i < cols; i++)
                {
                    for (var j = i; j < cols; j++)
                    {
                        var s = 0.0;
                        for (var k = 0; k < rows; k++)
                        {
                            s += (matrix[k, j] - means[j]) * (matrix[k, i] - means[i]);
                        }

                        s /= divisor;
                        cov[i, j] = s;
                        cov[j, i] = s;
                    }
                }
            }
            else if (dimension == 1)
            {
                if (means.Length != rows)
                {
                    throw new ArgumentException(
                        "Length of the mean vector should equal the number of rows", "means");
                }

                cov = new double[rows, rows];
                for (var i = 0; i < rows; i++)
                {
                    for (var j = i; j < rows; j++)
                    {
                        var s = 0.0;
                        for (var k = 0; k < cols; k++)
                        {
                            s += (matrix[j, k] - means[j]) * (matrix[i, k] - means[i]);
                        }

                        s /= divisor;
                        cov[i, j] = s;
                        cov[j, i] = s;
                    }
                }
            }
            else
            {
                throw new ArgumentException("Invalid dimension.", "dimension");
            }

            return cov;
        }

        /// <summary>
        ///     Calculates the covariance matrix of a sample matrix.
        /// </summary>
        /// <remarks>
        ///     In statistics and probability theory, the covariance matrix is a matrix of
        ///     covariances between elements of a vector. It is the natural generalization
        ///     to higher dimensions of the concept of the variance of a scalar-valued
        ///     random variable.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <returns>The covariance matrix.</returns>
        public static double[][] Covariance(this double[][] matrix)
        {
            return Covariance(matrix, Mean(matrix, 0));
        }

        /// <summary>
        ///     Calculates the covariance matrix of a sample matrix.
        /// </summary>
        /// <remarks>
        ///     In statistics and probability theory, the covariance matrix is a matrix of
        ///     covariances between elements of a vector. It is the natural generalization
        ///     to higher dimensions of the concept of the variance of a scalar-valued
        ///     random variable.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="dimension">
        ///     The dimension of the matrix to consider as observations. Pass 0 if the matrix has
        ///     observations as rows and variables as columns, pass 1 otherwise. Default is 0.
        /// </param>
        /// <returns>The covariance matrix.</returns>
        public static double[][] Covariance(this double[][] matrix, int dimension)
        {
            var size = dimension == 0 ? matrix.Length : matrix[0].Length;
            return Scatter(matrix, Mean(matrix, dimension), size - 1, dimension);
        }

        /// <summary>
        ///     Calculates the covariance matrix of a sample matrix.
        /// </summary>
        /// <remarks>
        ///     In statistics and probability theory, the covariance matrix is a matrix of
        ///     covariances between elements of a vector. It is the natural generalization
        ///     to higher dimensions of the concept of the variance of a scalar-valued
        ///     random variable.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="means">The mean value of the given values, if already known.</param>
        /// <returns>The covariance matrix.</returns>
        public static double[][] Covariance(this double[][] matrix, double[] means)
        {
            return Scatter(matrix, means, matrix.Length - 1, 0);
        }

        /// <summary>
        ///     Calculates the scatter matrix of a sample matrix.
        /// </summary>
        /// <remarks>
        ///     By dividing the Scatter matrix by the sample size, we get the population
        ///     Covariance matrix. By dividing by the sample size minus one, we get the
        ///     sample Covariance matrix.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="means">The mean value of the given values, if already known.</param>
        /// <returns>The covariance matrix.</returns>
        public static double[][] Scatter(this double[][] matrix, double[] means)
        {
            return Scatter(matrix, means, 1.0, 0);
        }

        /// <summary>
        ///     Calculates the scatter matrix of a sample matrix.
        /// </summary>
        /// <remarks>
        ///     By dividing the Scatter matrix by the sample size, we get the population
        ///     Covariance matrix. By dividing by the sample size minus one, we get the
        ///     sample Covariance matrix.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="means">The mean value of the given values, if already known.</param>
        /// <param name="divisor">A real number to divide each member of the matrix.</param>
        /// <returns>The covariance matrix.</returns>
        public static double[][] Scatter(this double[][] matrix, double[] means, double divisor)
        {
            return Scatter(matrix, means, divisor, 0);
        }

        /// <summary>
        ///     Calculates the scatter matrix of a sample matrix.
        /// </summary>
        /// <remarks>
        ///     By dividing the Scatter matrix by the sample size, we get the population
        ///     Covariance matrix. By dividing by the sample size minus one, we get the
        ///     sample Covariance matrix.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="divisor">A real number to divide each member of the matrix.</param>
        /// <param name="dimension">
        ///     Pass 0 to if mean vector is a row vector, 1 otherwise. Default value is 0.
        /// </param>
        /// <returns>The covariance matrix.</returns>
        public static double[][] Scatter(this double[][] matrix, double divisor, int dimension = 0)
        {
            return Scatter(matrix, Mean(matrix, 0), divisor, dimension);
        }

        /// <summary>
        ///     Calculates the scatter matrix of a sample matrix.
        /// </summary>
        /// <remarks>
        ///     By dividing the Scatter matrix by the sample size, we get the population
        ///     Covariance matrix. By dividing by the sample size minus one, we get the
        ///     sample Covariance matrix.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="means">The mean value of the given values, if already known.</param>
        /// <param name="dimension">
        ///     Pass 0 to if mean vector is a row vector, 1 otherwise. Default value is 0.
        /// </param>
        /// <returns>The covariance matrix.</returns>
        public static double[][] Scatter(this double[][] matrix, double[] means, int dimension)
        {
            return Scatter(matrix, means, 1.0, dimension);
        }

        /// <summary>
        ///     Calculates the scatter matrix of a sample matrix.
        /// </summary>
        /// <remarks>
        ///     By dividing the Scatter matrix by the sample size, we get the population
        ///     Covariance matrix. By dividing by the sample size minus one, we get the
        ///     sample Covariance matrix.
        /// </remarks>
        /// <param name="matrix">A number multi-dimensional array containing the matrix values.</param>
        /// <param name="means">The mean value of the given values, if already known.</param>
        /// <param name="divisor">A real number to divide each member of the matrix.</param>
        /// <param name="dimension">
        ///     Pass 0 to if mean vector is a row vector, 1 otherwise. Default value is 0.
        /// </param>
        /// <returns>The covariance matrix.</returns>
        public static double[][] Scatter(double[][] matrix, double[] means, double divisor, int dimension)
        {
            var rows = matrix.Length;
            if (rows == 0)
            {
                return new double[0][];
            }

            var cols = matrix[0].Length;

            double[][] cov;

            if (dimension == 0)
            {
                if (means.Length != cols)
                {
                    throw new ArgumentException("Length of the mean vector should equal the number of columns",
                        "means");
                }

                cov = Jagged.Zeros(cols, cols);
                for (var i = 0; i < cols; i++)
                {
                    for (var j = i; j < cols; j++)
                    {
                        var s = 0.0;
                        for (var k = 0; k < rows; k++)
                        {
                            s += (matrix[k][j] - means[j]) * (matrix[k][i] - means[i]);
                        }

                        s /= divisor;
                        cov[i][j] = s;
                        cov[j][i] = s;
                    }
                }
            }
            else if (dimension == 1)
            {
                if (means.Length != rows)
                {
                    throw new ArgumentException("Length of the mean vector should equal the number of rows", "means");
                }

                cov = Jagged.Zeros(rows, rows);
                for (var i = 0; i < rows; i++)
                {
                    for (var j = i; j < rows; j++)
                    {
                        var s = 0.0;
                        for (var k = 0; k < cols; k++)
                        {
                            s += (matrix[j][k] - means[j]) * (matrix[i][k] - means[i]);
                        }

                        s /= divisor;
                        cov[i][j] = s;
                        cov[j][i] = s;
                    }
                }
            }
            else
            {
                throw new ArgumentException("Invalid dimension.", "dimension");
            }

            return cov;
        }

        /// <summary>
        ///     Calculates the weighted pooled covariance matrix from a set of covariance matrices.
        /// </summary>
        public static double[,] PooledCovariance(double[][,] covariances, double[] weights)
        {
            var pooledCov = Matrix.CreateAs(covariances[0]);

            var weightSum = weights.Sum();

            for (var k = 0; k < covariances.Length; k++)
            {
                var w = weights[k];
                if (weightSum != 0)
                {
                    w /= weightSum;
                }

                pooledCov.MultiplyAndAdd(w, covariances[k], pooledCov);
            }

            return pooledCov;
        }

        /// <summary>
        ///     Calculates the correlation matrix for a matrix of samples.
        /// </summary>
        /// <remarks>
        ///     In statistics and probability theory, the correlation matrix is the same
        ///     as the covariance matrix of the standardized random variables.
        /// </remarks>
        /// <param name="matrix">A multi-dimensional array containing the matrix values.</param>
        /// <returns>The correlation matrix.</returns>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Statistics\ToolsTest.cs" region="doc_correlation" />
        /// </example>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Statistics\ToolsTest.cs" region="doc_correlation" />
        /// </example>
        public static double[,] Correlation(this double[,] matrix)
        {
            var means = Mean(matrix, 0);
            return Correlation(matrix, means, StandardDeviation(matrix, means));
        }

        /// <summary>
        ///     Calculates the correlation matrix for a matrix of samples.
        /// </summary>
        /// <remarks>
        ///     In statistics and probability theory, the correlation matrix is the same
        ///     as the covariance matrix of the standardized random variables.
        /// </remarks>
        /// <param name="matrix">A multi-dimensional array containing the matrix values.</param>
        /// <returns>The correlation matrix.</returns>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Statistics\ToolsTest.cs" region="doc_correlation" />
        /// </example>
        public static double[][] Correlation(this double[][] matrix)
        {
            var means = Mean(matrix, 0);
            return Correlation(matrix, means, StandardDeviation(matrix, means));
        }

        /// <summary>
        ///     Calculates the correlation matrix for a matrix of samples.
        /// </summary>
        /// <remarks>
        ///     In statistics and probability theory, the correlation matrix is the same
        ///     as the covariance matrix of the standardized random variables.
        /// </remarks>
        /// <param name="matrix">A multi-dimensional array containing the matrix values.</param>
        /// <param name="means">The mean value of the given values, if already known.</param>
        /// <param name="standardDeviations">The values' standard deviation vector, if already known.</param>
        /// <returns>The correlation matrix.</returns>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Statistics\ToolsTest.cs" region="doc_correlation" />
        /// </example>
        public static double[,] Correlation(this double[,] matrix, double[] means, double[] standardDeviations)
        {
            var rows = matrix.Rows();
            var cols = matrix.Columns();

            double N = rows;
            var cor = new double[cols, cols];
            for (var i = 0; i < cols; i++)
            {
                for (var j = i; j < cols; j++)
                {
                    var c = 0.0;
                    for (var k = 0; k < rows; k++)
                    {
                        var a = z(matrix[k, j], means[j], standardDeviations[j]);
                        var b = z(matrix[k, i], means[i], standardDeviations[i]);
                        c += a * b;
                    }

                    c /= N - 1.0;
                    cor[i, j] = c;
                    cor[j, i] = c;
                }
            }

            return cor;
        }

        /// <summary>
        ///     Calculates the correlation matrix for a matrix of samples.
        /// </summary>
        /// <remarks>
        ///     In statistics and probability theory, the correlation matrix is the same
        ///     as the covariance matrix of the standardized random variables.
        /// </remarks>
        /// <param name="matrix">A multi-dimensional array containing the matrix values.</param>
        /// <param name="means">The mean value of the given values, if already known.</param>
        /// <param name="standardDeviations">The values' standard deviation vector, if already known.</param>
        /// <returns>The correlation matrix.</returns>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Statistics\ToolsTest.cs" region="doc_correlation" />
        /// </example>
        public static double[][] Correlation(this double[][] matrix, double[] means, double[] standardDeviations)
        {
            var rows = matrix.Rows();
            var cols = matrix.Columns();

            double N = rows;
            var cor = Jagged.Zeros(cols, cols);
            for (var i = 0; i < cols; i++)
            {
                for (var j = i; j < cols; j++)
                {
                    var c = 0.0;
                    for (var k = 0; k < matrix.Length; k++)
                    {
                        var a = z(matrix[k][j], means[j], standardDeviations[j]);
                        var b = z(matrix[k][i], means[i], standardDeviations[i]);
                        c += a * b;
                    }

                    c /= N - 1.0;
                    cor[i][j] = c;
                    cor[j][i] = c;
                }
            }

            return cor;
        }

        private static double z(double v, double mean, double sdev)
        {
            if (sdev == 0)
            {
                sdev = 1e-12;
            }

            return (v - mean) / sdev;
        }
    }
}