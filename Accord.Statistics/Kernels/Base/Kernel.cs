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
using Accord.Diagnostics;
using Accord.Math;
using Accord.Math.Distances;

namespace Accord.Statistics.Kernels
{
    /// <summary>
    ///     Extension methods for <see cref="IKernel">kernel functions</see>.
    /// </summary>
    public static class Kernel
    {
        /// <summary>
        ///     Creates the Gram matrix from the given vectors.
        /// </summary>
        /// <param name="kernel">The kernel function.</param>
        /// <param name="x">The vectors.</param>
        /// <param name="result">An optional matrix where the result should be stored in.</param>
        /// <returns>
        ///     A symmetric matrix containing the dot-products in
        ///     feature (kernel) space between all vectors in <paramref name="x" />.
        /// </returns>
        public static double[,] ToMatrix<TKernel, TInput>(this TKernel kernel, TInput[] x, double[,] result = null)
            where TKernel : IKernel<TInput>
        {
            if (result == null)
            {
                result = new double[x.Length, x.Length];
            }

            for (var i = 0; i < x.Length; i++)
            {
                for (var j = i; j < x.Length; j++)
                {
                    result[j, i] = result[i, j] = kernel.Function(x[i], x[j]);
                }
            }

            return result;
        }

        /// <summary>
        ///     Creates the Gram matrix containing all dot products in feature
        ///     (kernel) space between each vector in <paramref name="x">x</paramref>
        ///     and the ones in <paramref name="y">y</paramref>.
        /// </summary>
        /// <param name="x">The first vectors.</param>
        /// <param name="y">The second vectors.</param>
        /// <param name="kernel">The kernel function.</param>
        /// <param name="result">An optional matrix where the result should be stored in.</param>
        /// <returns>
        ///     A symmetric matrix containing the dot-products in
        ///     feature (kernel) space between each vector in <paramref name="x" />
        ///     and the ones in <paramref name="y" />.
        /// </returns>
        public static double[,] ToMatrix2<TKernel, TInput>(this TKernel kernel, TInput[] x, TInput[] y,
            double[,] result = null)
            where TKernel : IKernel<TInput>
        {
            if (result == null)
            {
                result = new double[x.Length, y.Length];
            }

            for (var i = 0; i < x.Length; i++)
            {
                for (var j = 0; j < y.Length; j++)
                {
                    result[i, j] = kernel.Function(x[i], y[j]);
                }
            }

            return result;
        }

        /// <summary>
        ///     Creates the Gram matrix from the given vectors.
        /// </summary>
        /// <param name="kernel">The kernel function.</param>
        /// <param name="x">The vectors.</param>
        /// <param name="result">An optional matrix where the result should be stored in.</param>
        /// <returns>
        ///     A symmetric matrix containing the dot-products in
        ///     feature (kernel) space between all vectors in <paramref name="x" />.
        /// </returns>
        public static double[][] ToJagged<TKernel, TInput>(this TKernel kernel, TInput[] x, double[][] result = null)
            where TKernel : IKernel<TInput>
        {
            if (result == null)
            {
                result = Jagged.Create<double>(x.Length, x.Length);
            }

            for (var i = 0; i < x.Length; i++)
            {
                for (var j = i; j < x.Length; j++)
                {
                    result[j][i] = result[i][j] = kernel.Function(x[i], x[j]);
                }
            }

            return result;
        }

        /// <summary>
        ///     Creates the Gram matrix containing all dot products in feature
        ///     (kernel) space between each vector in <paramref name="x">x</paramref>
        ///     and the ones in <paramref name="y">y</paramref>.
        /// </summary>
        /// <param name="kernel">The kernel function.</param>
        /// <param name="x">The first vectors.</param>
        /// <param name="y">The second vectors.</param>
        /// <param name="result">An optional matrix where the result should be stored in.</param>
        /// <returns>
        ///     A symmetric matrix containing the dot-products in
        ///     feature (kernel) space between each vector in <paramref name="x" />
        ///     and the ones in <paramref name="y" />.
        /// </returns>
        public static double[][] ToJagged2<TKernel, TInput>(this TKernel kernel, TInput[] x, TInput[] y,
            double[][] result = null)
            where TKernel : IKernel<TInput>
        {
            if (result == null)
            {
                result = Jagged.Create<double>(x.Length, y.Length);
            }

            for (var i = 0; i < x.Length; i++)
            {
                for (var j = 0; j < y.Length; j++)
                {
                    result[i][j] = kernel.Function(x[i], y[j]);
                }
            }

            return result;
        }

        /// <summary>
        ///     Estimates the complexity parameter C, present in many SVM algorithms,
        ///     for a given kernel and a given data set by summing every element
        ///     on the diagonal of the kernel matrix and using an heuristic based
        ///     on it.
        /// </summary>
        /// <param name="kernel">The kernel function.</param>
        /// <param name="inputs">The input samples.</param>
        /// <returns>A suitable value for C.</returns>
        public static double EstimateComplexity<TKernel, TInput>(this TKernel kernel, TInput[] inputs)
            where TKernel : IKernel<TInput>
        {
            // Compute initial value for C as the number of examples
            // divided by the trace of the input sample kernel matrix.

            var sum = 0.0;
            for (var i = 0; i < inputs.Length; i++)
            {
                sum += kernel.Function(inputs[i], inputs[i]);

                if (double.IsNaN(sum))
                {
                    throw new OverflowException();
                }
            }

            return inputs.Length / sum;
        }

        /// <summary>
        ///     Estimates the complexity parameter C, present in many SVM algorithms,
        ///     for a given kernel and an unbalanced data set by summing every element
        ///     on the diagonal of the kernel matrix and using an heuristic based on it.
        /// </summary>
        /// <param name="kernel">The kernel function.</param>
        /// <param name="inputs">The input samples.</param>
        /// <param name="outputs">The output samples.</param>
        /// <returns>A suitable value for positive C and negative C, respectively.</returns>
        public static Tuple<double, double> EstimateComplexity<TKernel, TInput>(this TKernel kernel, TInput[] inputs,
            int[] outputs)
            where TKernel : IKernel<TInput>
        {
            // Compute initial value for C as the number of examples
            // divided by the trace of the input sample kernel matrix.

            var negativeSum = 0.0;
            var positiveSum = 0.0;

            var negativeCount = 0;
            var positiveCount = 0;

            for (var i = 0; i < inputs.Length; i++)
            {
                if (outputs[i] == -1)
                {
                    negativeSum += kernel.Function(inputs[i], inputs[i]);
                    negativeCount++;
                }
                else // outputs[i] == +1
                {
                    positiveSum += kernel.Function(inputs[i], inputs[i]);
                    positiveCount++;
                }

                if (double.IsNaN(positiveSum) || double.IsNaN(negativeSum))
                {
                    throw new OverflowException();
                }
            }

            return Tuple.Create
            (
                positiveCount / positiveSum,
                negativeCount / negativeSum
            );
        }

        /// <summary>
        ///     Estimates the complexity parameter C, present in many SVM algorithms,
        ///     for a given kernel and a given data set by summing every element
        ///     on the diagonal of the kernel matrix and using an heuristic based
        ///     on it.
        /// </summary>
        /// <param name="inputs">The input samples.</param>
        /// <returns>A suitable value for C.</returns>
        public static double EstimateComplexity(double[][] inputs)
        {
            return EstimateComplexity(new Linear(), inputs);
        }

        /// <summary>
        ///     Estimates the complexity parameter C, present in many SVM algorithms,
        ///     for a given kernel and an unbalanced data set by summing every element
        ///     on the diagonal of the kernel matrix and using an heuristic based on it.
        /// </summary>
        /// <param name="inputs">The input samples.</param>
        /// <param name="outputs">The output samples.</param>
        /// <returns>A suitable value for positive C and negative C, respectively.</returns>
        public static Tuple<double, double> EstimateComplexity(double[][] inputs, int[] outputs)
        {
            return EstimateComplexity(new Linear(), inputs, outputs);
        }

        /// <summary>
        ///     Computes the set of all distances between
        ///     all points in a random subset of the data.
        /// </summary>
        /// <param name="kernel">The inner kernel.</param>
        /// <param name="inputs">The inputs points.</param>
        /// <param name="samples">The number of samples.</param>
        public static double[] Distances<T>(this T kernel, double[][] inputs, int samples)
            where T : IDistance<double[]>, ICloneable
        {
            var idx = Vector.Sample(samples, inputs.Length);
            var idy = Vector.Sample(samples, inputs.Length);

            var distances = new double[samples * samples];

            for (var i = 0; i < idx.Length; i++)
            {
                var x = inputs[idx[i]];

                for (var j = 0; j < idy.Length; j++)
                {
                    var y = inputs[idy[j]];

                    distances[i * samples + j] = kernel.Distance(x, y);
                }
            }

            Array.Sort(distances);

            return distances;
        }

        /// <summary>
        ///     Computes the set of all distances between
        ///     all points in a random subset of the data.
        /// </summary>
        /// <param name="kernel">The inner kernel.</param>
        /// <param name="inputs">The inputs points.</param>
        /// <param name="samples">The number of samples.</param>
        public static double[] Distances<TKernel, TData>(this TKernel kernel, TData[] inputs, int samples)
            where TKernel : IDistance<TData>, ICloneable
        {
            var idx = Vector.Sample(samples, inputs.Length);
            var idy = Vector.Sample(samples, inputs.Length);

            var distances = new double[samples * samples];

            for (var i = 0; i < idx.Length; i++)
            {
                var x = inputs[idx[i]];

                for (var j = 0; j < idy.Length; j++)
                {
                    var y = inputs[idy[j]];

                    distances[i * samples + j] = kernel.Distance(x, y);
                }
            }

            Array.Sort(distances);

            return distances;
        }

        /// <summary>
        ///     Centers the given kernel matrix K.
        /// </summary>
        /// <param name="kernelMatrix">The kernel matrix to be centered.</param>
        /// <param name="result">The array where to store results.</param>
        public static double[,] Center(double[,] kernelMatrix, double[,] result = null)
        {
            if (result == null)
            {
                result = Matrix.CreateAs(kernelMatrix);
            }

            var rowMean = kernelMatrix.Mean(1);
#if DEBUG
            // row mean and column means should be equal on a symmetric matrix
            var colMean = kernelMatrix.Mean(0);
            for (var i = 0; i < colMean.Length; i++)
            {
                Debug.Assert(colMean[i] == rowMean[i]);
            }
#endif
            var mean = kernelMatrix.Mean();

            for (var i = 0; i < rowMean.Length; i++)
            {
                for (var j = i; j < rowMean.Length; j++)
                {
                    result[i, j] = result[j, i] = kernelMatrix[i, j] - rowMean[i] - rowMean[j] + mean;
                }
            }

            return result;
        }

        /// <summary>
        ///     Centers the given kernel matrix K.
        /// </summary>
        /// <param name="kernelMatrix">The kernel matrix to be centered.</param>
        /// <param name="rowMean">The row-wise mean vector.</param>
        /// <param name="mean">The total mean (across all values in the matrix).</param>
        /// <param name="result">The array where to store results.</param>
        public static double[][] Center(double[][] kernelMatrix, out double[] rowMean, out double mean,
            double[][] result = null)
        {
            if (result == null)
            {
                result = Jagged.CreateAs(kernelMatrix);
            }

            rowMean = kernelMatrix.Mean(1);
#if DEBUG
            // row mean and column means should be equal on a symmetric matrix
            var colMean = kernelMatrix.Mean(0);
            for (var i = 0; i < colMean.Length; i++)
            {
                Debug.Assert(colMean[i] == rowMean[i]);
            }
#endif
            mean = kernelMatrix.Mean(); // TODO: This should become simply mean = K.Mean()

            for (var i = 0; i < rowMean.Length; i++)
            {
                for (var j = i; j < rowMean.Length; j++)
                {
                    result[i][j] = result[j][i] = kernelMatrix[i][j] - rowMean[i] - rowMean[j] + mean;
                }
            }

            return result;
        }

        /// <summary>
        ///     Centers the given kernel matrix K.
        /// </summary>
        /// <param name="kernelMatrix">The kernel matrix to be centered.</param>
        /// <param name="rowMean">The row-wise mean vector.</param>
        /// <param name="mean">The total mean (across all values in the matrix).</param>
        /// <param name="result">The array where to store results.</param>
        public static double[,] Center(double[,] kernelMatrix, double[] rowMean, double mean, double[,] result = null)
        {
            if (result == null)
            {
                result = Matrix.CreateAs(kernelMatrix);
            }

            var rows = kernelMatrix.Rows();
            var cols = kernelMatrix.Columns();

            var rowMean1 = kernelMatrix.Mean(1);
            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < cols; j++)
                {
                    result[i, j] = kernelMatrix[i, j] - rowMean1[i] - rowMean[j] + mean;
                }
            }

            return result;
        }

        /// <summary>
        ///     Centers the given kernel matrix K.
        /// </summary>
        /// <param name="kernelMatrix">The kernel matrix to be centered.</param>
        /// <param name="rowMean">The row-wise mean vector.</param>
        /// <param name="mean">The total mean (across all values in the matrix).</param>
        /// <param name="result">The array where to store results.</param>
        public static double[][] Center(double[][] kernelMatrix, double[] rowMean, double mean,
            double[][] result = null)
        {
            if (result == null)
            {
                result = Jagged.CreateAs(kernelMatrix);
            }

#if DEBUG
            var r = Center(kernelMatrix.ToMatrix(), rowMean, mean);
#endif

            //int cols = kernelMatrix.Columns();

            var rowMean1 = kernelMatrix.Mean(1);
            for (var i = 0; i < result.Length; i++)
            {
                for (var j = 0; j < kernelMatrix[i].Length; j++)
                {
                    result[i][j] = kernelMatrix[i][j] - rowMean1[i] - rowMean[j] + mean;
                }
            }

#if DEBUG
            if (!r.IsEqual(result, 1e-8))
            {
                throw new Exception();
            }
#endif

            return result;
        }
    }
}