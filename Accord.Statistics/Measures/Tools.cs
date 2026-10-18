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
using System.Collections.Generic;
using Accord.Math;
using Accord.Math.Decompositions;
using Accord.Statistics.Distributions;
using Accord.Statistics.Distributions.Fitting;

namespace Accord.Statistics
{
    /// <summary>
    ///     Set of statistics functions.
    /// </summary>
    /// <remarks>
    ///     This class represents collection of common functions used in statistics.
    ///     Every Matrix function assumes data is organized in a table-like model,
    ///     where Columns represents variables and Rows represents a observation of
    ///     each variable.
    /// </remarks>
    public static partial class Tools
    {
        #region Determination and performance measures

        /// <summary>
        ///     Gets the coefficient of determination, as known as the R-Squared (R²)
        /// </summary>
        /// <remarks>
        ///     The coefficient of determination is used in the context of statistical models
        ///     whose main purpose is the prediction of future outcomes on the basis of other
        ///     related information. It is the proportion of variability in a data set that
        ///     is accounted for by the statistical model. It provides a measure of how well
        ///     future outcomes are likely to be predicted by the model.
        ///     The R^2 coefficient of determination is a statistical measure of how well the
        ///     regression approximates the real data points. An R^2 of 1.0 indicates that the
        ///     regression perfectly fits the data.
        /// </remarks>
        public static double Determination(double[] actual, double[] expected)
        {
            // R-squared = 100 * SS(regression) / SS(total)

            var SSe = 0.0;
            var SSt = 0.0;
            var avg = 0.0;
            double d;

            // Calculate expected output mean
            for (var i = 0; i < expected.Length; i++)
            {
                avg += expected[i];
            }

            avg /= expected.Length;

            // Calculate SSe and SSt
            for (var i = 0; i < expected.Length; i++)
            {
                d = expected[i] - actual[i];
                SSe += d * d;

                d = expected[i] - avg;
                SSt += d * d;
            }

            // Calculate R-Squared
            return 1.0 - SSe / SSt;
        }

        #endregion

        // ------------------------------------------------------------

        /// <summary>
        ///     Computes the whitening transform for the given data, making
        ///     its covariance matrix equals the identity matrix.
        /// </summary>
        /// <param name="value">
        ///     A matrix where each column represent a
        ///     variable and each row represent a observation.
        /// </param>
        /// <param name="transformMatrix">
        ///     The base matrix used in the
        ///     transformation.
        /// </param>
        /// <returns>
        ///     The transformed source data (which now has unit variance).
        /// </returns>
        public static double[,] Whitening(double[,] value, out double[,] transformMatrix)
        {
            // TODO: Move into PCA and mark as obsolete
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }


            var cols = value.GetLength(1);

            var cov = value.Covariance();

            // Diagonalizes the covariance matrix
            var svd = new SingularValueDecomposition(cov,
                true, // compute left vectors (to become a transformation matrix)
                false, // do not compute right vectors since they aren't necessary
                true, // transpose if necessary to avoid erroneous assumptions in SVD
                true); // perform operation in-place, reducing memory usage


            // Retrieve the transformation matrix
            transformMatrix = svd.LeftSingularVectors;

            // Perform scaling to have unit variance
            var singularValues = svd.Diagonal;
            for (var i = 0; i < cols; i++)
            {
                for (var j = 0; j < singularValues.Length; j++)
                {
                    transformMatrix[i, j] /= System.Math.Sqrt(singularValues[j]);
                }
            }

            // Return the transformed data
            return value.Dot(transformMatrix);
        }

        /// <summary>
        ///     Computes the whitening transform for the given data, making
        ///     its covariance matrix equals the identity matrix.
        /// </summary>
        /// <param name="value">
        ///     A matrix where each column represent a
        ///     variable and each row represent a observation.
        /// </param>
        /// <param name="transformMatrix">
        ///     The base matrix used in the
        ///     transformation.
        /// </param>
        /// <returns>
        ///     The transformed source data (which now has unit variance).
        /// </returns>
        public static double[][] Whitening(double[][] value, out double[][] transformMatrix)
        {
            // TODO: Move into PCA and mark as obsolete
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }


            var cols = value.Columns();

            var cov = value.Covariance();

            // Diagonalizes the covariance matrix
            var svd = new JaggedSingularValueDecomposition(cov,
                true, // compute left vectors (to become a transformation matrix)
                false, // do not compute right vectors since they aren't necessary
                true, // transpose if necessary to avoid erroneous assumptions in SVD
                true); // perform operation in-place, reducing memory usage


            // Retrieve the transformation matrix
            transformMatrix = svd.LeftSingularVectors;

            // Perform scaling to have unit variance
            var singularValues = svd.Diagonal;
            for (var i = 0; i < cols; i++)
            {
                for (var j = 0; j < singularValues.Length; j++)
                {
                    transformMatrix[i][j] /= System.Math.Sqrt(singularValues[j]);
                }
            }

            // Return the transformed data
            return value.Dot(transformMatrix);
        }

        /// <summary>
        ///     Creates a new distribution that has been fit to a given set of observations.
        /// </summary>
        /// <param name="observations">
        ///     The array of observations to fit the model against. The array
        ///     elements can be either of type double (for univariate data) or
        ///     type double[] (for multivariate data).
        /// </param>
        /// <param name="weights">The weight vector containing the weight for each of the samples.</param>
        public static TDistribution Fit<TDistribution>(this double[] observations, double[] weights = null)
            where TDistribution : IFittable<double>, new()
        {
            var dist = new TDistribution();
            dist.Fit(observations, weights);
            return dist;
        }

        /// <summary>
        ///     Creates a new distribution that has been fit to a given set of observations.
        /// </summary>
        /// <param name="observations">
        ///     The array of observations to fit the model against. The array
        ///     elements can be either of type double (for univariate data) or
        ///     type double[] (for multivariate data).
        /// </param>
        /// <param name="weights">The weight vector containing the weight for each of the samples.</param>
        public static TDistribution Fit<TDistribution>(this double[][] observations, double[] weights = null)
            where TDistribution : IFittable<double[]>, new()
        {
            var dist = new TDistribution();
            dist.Fit(observations, weights);
            return dist;
        }

        /// <summary>
        ///     Creates a new distribution that has been fit to a given set of observations.
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
        public static TDistribution Fit<TDistribution, TOptions>(this double[] observations, TOptions options,
            double[] weights = null)
            where TDistribution : IFittable<double, TOptions>, new()
            where TOptions : class, IFittingOptions
        {
            var dist = new TDistribution();
            dist.Fit(observations, weights, options);
            return dist;
        }

        /// <summary>
        ///     Creates a new distribution that has been fit to a given set of observations.
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
        public static TDistribution Fit<TDistribution, TOptions>(this double[][] observations, TOptions options,
            double[] weights = null)
            where TDistribution : IFittable<double[], TOptions>, new()
            where TOptions : class, IFittingOptions
        {
            var dist = new TDistribution();
            dist.Fit(observations, weights, options);
            return dist;
        }

        /// <summary>
        ///     Creates a new distribution that has been fit to a given set of observations.
        /// </summary>
        /// <param name="distribution">The distribution whose parameters should be fitted to the samples.</param>
        /// <param name="observations">
        ///     The array of observations to fit the model against. The array
        ///     elements can be either of type double (for univariate data) or
        ///     type double[] (for multivariate data).
        /// </param>
        /// <param name="weights">The weight vector containing the weight for each of the samples.</param>
        public static TDistribution FitNew<TDistribution, TObservations>(
            this TDistribution distribution, TObservations[] observations, double[] weights = null)
            where TDistribution : IFittable<TObservations>, ICloneable
        {
            var clone = (TDistribution)distribution.Clone();
            clone.Fit(observations, weights);
            return clone;
        }

        /// <summary>
        ///     Creates a new distribution that has been fit to a given set of observations.
        /// </summary>
        /// <param name="distribution">The distribution whose parameters should be fitted to the samples.</param>
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
        public static TDistribution FitNew<TDistribution, TObservations, TOptions>(
            this TDistribution distribution, TObservations[] observations, TOptions options, double[] weights = null)
            where TDistribution : IFittable<TObservations, TOptions>, ICloneable
            where TOptions : class, IFittingOptions
        {
            var clone = (TDistribution)distribution.Clone();
            clone.Fit(observations, weights, options);
            return clone;
        }
        // TODO: Make this class obsolete

        #region Summarizing, grouping and extending operations

        /// <summary>
        ///     Obsolete. Please use <see cref="Classes.GetRatio(int[], int[])" /> instead.
        /// </summary>
        [Obsolete("Please use Classes.GetRatio instead.")]
        public static double[] Proportions(int[] positives, int[] negatives)
        {
            return Classes.GetRatio(positives, negatives);
        }

        /// <summary>
        ///     Obsolete. Please use <see cref="Classes.GetRatio(int[][], int, int)" /> instead.
        /// </summary>
        [Obsolete("Please use Classes.GetRatio instead.")]
        public static double[] Proportions(int[][] data, int positiveColumn, int negativeColumn)
        {
            return data.GetRatio(positiveColumn, negativeColumn);
        }

        /// <summary>
        ///     Obsolete. Please use <see cref="Classes.Summarize(int[][], int, int)" /> instead.
        /// </summary>
        [Obsolete("Please use Classes.Summarize instead.")]
        public static int[][] Group(int[][] data, int labelColumn, int dataColumn)
        {
            return data.Summarize(labelColumn, dataColumn);
        }

        /// <summary>
        ///     Obsolete. Please use <see cref="Classes.Expand(int[], int[], int[])" /> instead.
        /// </summary>
        [Obsolete("Please use Classes.Expand instead.")]
        public static int[][] Expand(int[] data, int[] positives, int[] negatives)
        {
            return data.Expand(positives, negatives);
        }

        /// <summary>
        ///     Obsolete. Please use <see cref="Classes.Expand(int[], int[], int[])" /> instead.
        /// </summary>
        [Obsolete("Please use Classes.Expand instead.")]
        public static int[][] Expand(int[][] data, int labelColumn, int positiveColumn, int negativeColumn)
        {
            return data.Expand(labelColumn, positiveColumn, negativeColumn);
        }

        /// <summary>
        ///     Obsolete. Please use <see cref="Jagged.OneHot(int[])" /> instead.
        /// </summary>
        [Obsolete("Please use Jagged.OneHot instead.")]
        public static double[][] Expand(int[] labels)
        {
            return Jagged.OneHot(labels, labels.DistinctCount());
        }

        /// <summary>
        ///     Obsolete. Please use <see cref="Jagged.OneHot(int[])" /> instead.
        /// </summary>
        [Obsolete("Please use Jagged.OneHot instead.")]
        public static double[][] Expand(int[] labels, double negative, double positive)
        {
            return Jagged.OneHot(labels).Replace(0, negative).Replace(1, positive);
        }

        /// <summary>
        ///     Obsolete. Please use <see cref="Jagged.OneHot(int[])" /> instead.
        /// </summary>
        [Obsolete("Please use Jagged.OneHot instead.")]
        public static double[][] Expand(int[] labels, int classes)
        {
            return Jagged.OneHot(labels, classes);
        }

        /// <summary>
        ///     Obsolete. Please use <see cref="Jagged.OneHot(int[])" /> instead.
        /// </summary>
        [Obsolete("Please use Jagged.OneHot instead.")]
        public static double[][] Expand(int[] labels, int classes, double negative, double positive)
        {
            return Jagged.OneHot(labels, classes).Replace(0, negative).Replace(1, positive);
        }

        #endregion

        #region Permutations and combinatorials

        /// <summary>
        ///     Obsolete. Please use <see cref="Vector.Sample(int, int)" /> instead.
        /// </summary>
        [Obsolete("Please use Vector.Sample instead.")]
        public static int[] RandomSample(int n, int k)
        {
            return Vector.Sample(k, n);
        }

        /// <summary>
        ///     Obsolete. Please use <see cref="Classes.Random(int, int)" /> instead.
        /// </summary>
        [Obsolete("Please use Classes.Random instead.")]
        public static int[] RandomGroups(int size, int groups)
        {
            return Classes.Random(size, groups);
        }

        /// <summary>
        ///     Obsolete. Please use <see cref="Classes.Random(int, double)" /> instead.
        /// </summary>
        [Obsolete("Please use Classes.Random instead.")]
        public static int[] RandomGroups(int size, double proportion)
        {
            return Classes.Random(size, proportion);
        }

        /// <summary>
        ///     Obsolete. Please use <see cref="Classes.Random(int[], int, int)" /> instead.
        /// </summary>
        [Obsolete("Please use Classes.Random instead.")]
        public static int[] RandomGroups(int[] labels, int classes, int groups)
        {
            return Classes.Random(labels, classes, groups);
        }

        /// <summary>
        ///     Obsolete. Please use <see cref="Vector.Sample(int)" /> instead.
        /// </summary>
        [Obsolete("Please use Vector.Sample instead.")]
        public static int[] Random(int n)
        {
            return Vector.Sample(n);
        }

        /// <summary>
        ///     Obsolete. Please use <see cref="Vector.Shuffle{T}(T[])" /> instead.
        /// </summary>
        [Obsolete("Please use Vector.Shuffle instead.")]
        public static void Shuffle<T>(T[] array)
        {
            array.Shuffle();
        }

        /// <summary>
        ///     Obsolete. Please use <see cref="Vector.Shuffle{T}(IList{T})" /> instead.
        /// </summary>
        [Obsolete("Please use Vector.Shuffle instead.")]
        public static void Shuffle<T>(IList<T> array)
        {
            array.Shuffle();
        }

        #endregion
    }
}