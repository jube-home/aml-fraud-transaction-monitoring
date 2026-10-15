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
using Accord.Math.Distances;

namespace Accord.Statistics.Kernels
{
    /// <summary>
    ///     Linear Kernel.
    /// </summary>
    [Serializable]
    public struct Linear : IKernel, IDistance, ILinear, ICloneable, IReverseDistance,
        ITransform, IKernel<Sparse<double>>, ILinear<Sparse<double>>, IDistance<Sparse<double>>
    {
        private double constant;

        /// <summary>
        ///     Constructs a new Linear kernel.
        /// </summary>
        /// <param name="constant">A constant intercept term. Default is 0.</param>
        public Linear(double constant)
        {
            this.constant = constant;
        }

        /// <summary>
        ///     Gets or sets the kernel's intercept term. Default is 0.
        /// </summary>
        public double Constant
        {
            get => constant;
            set => constant = value;
        }

        /// <summary>
        ///     Linear kernel function.
        /// </summary>
        /// <param name="x">Vector <c>x</c> in input space.</param>
        /// <param name="y">Vector <c>y</c> in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public double Function(double[] x, double[] y)
        {
            var sum = constant;
            for (var i = 0; i < y.Length; i++)
            {
                sum += x[i] * y[i];
            }

            return sum;
        }

        /// <summary>
        ///     Linear kernel function.
        /// </summary>
        /// <param name="z">Distance <c>z</c> in input space.</param>
        /// <returns>Dot product in feature (kernel) space.</returns>
        public double Function(double z)
        {
            return z + constant;
        }

        /// <summary>
        ///     Computes the squared distance in input space
        ///     between two points given in feature space.
        /// </summary>
        /// <param name="x">Vector <c>x</c> in feature (kernel) space.</param>
        /// <param name="y">Vector <c>y</c> in feature (kernel) space.</param>
        /// <returns>Squared distance between <c>x</c> and <c>y</c> in input space.</returns>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public double Distance(double[] x, double[] y)
        {
            if (x == y)
            {
                return 0.0;
            }

            double sum = 0;
            for (var i = 0; i < x.Length; i++)
            {
                var u = x[i] - y[i];
                sum += u * u;
            }

            return sum;
        }

        /// <summary>
        ///     Computes the squared distance in input space
        ///     between two points given in feature space.
        /// </summary>
        /// <param name="x">Vector <c>x</c> in feature (kernel) space.</param>
        /// <param name="y">Vector <c>y</c> in feature (kernel) space.</param>
        /// <returns>Squared distance between <c>x</c> and <c>y</c> in input space.</returns>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public double Distance(Sparse<double> x, Sparse<double> y)
        {
            if (x == y)
            {
                return 0.0;
            }

            return Math.Distance.SquareEuclidean(x, y);
        }

        /// <summary>
        ///     Computes the squared distance in input space
        ///     between two points given in feature space.
        /// </summary>
        /// <param name="x">Vector <c>x</c> in feature (kernel) space.</param>
        /// <param name="y">Vector <c>y</c> in feature (kernel) space.</param>
        /// <returns>
        ///     Squared distance between <c>x</c> and <c>y</c> in input space.
        /// </returns>
        public double ReverseDistance(double[] x, double[] y)
        {
            var sumx = constant;
            var sumy = constant;
            var sum = constant;

            for (var i = 0; i < x.Length; i++)
            {
                sumx += x[i] * x[i];
                sumy += y[i] * y[i];
                sum += x[i] * y[i];
            }

            return sumx + sumy - 2.0 * sum;
        }

        /// <summary>
        ///     Elementwise addition of a and b, storing in result.
        /// </summary>
        /// <param name="a">The first vector to add.</param>
        /// <param name="b">The second vector to add.</param>
        /// <param name="result">An array to store the result.</param>
        /// <returns>The same vector passed as result.</returns>
        public void Add(double[] a, double[] b, double[] result)
        {
            a.Add(b, result);
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        ///     A new object that is a copy of this instance.
        /// </returns>
        public object Clone()
        {
            return MemberwiseClone();
        }

        /// <summary>
        ///     Elementwise multiplication of scalar a and vector b, storing in result.
        /// </summary>
        /// <param name="a">The scalar to be multiplied.</param>
        /// <param name="b">The vector to be multiplied.</param>
        /// <param name="accumulate">An array to store the result.</param>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void Product(double a, double[] b, double[] accumulate)
        {
            if (a == 0)
            {
                return;
            }

            for (var j = 0; j < b.Length; j++)
            {
                accumulate[j] += a * b[j];
            }
        }

        /// <summary>
        ///     Compress a set of support vectors and weights into a single
        ///     parameter vector.
        /// </summary>
        /// <param name="weights">The weights associated with each support vector.</param>
        /// <param name="supportVectors">The support vectors.</param>
        /// <param name="c">The constant (bias) value.</param>
        /// <returns>A single parameter vector.</returns>
        public double[] Compress(double[] weights, double[][] supportVectors, out double c)
        {
            var result = weights.Dot(supportVectors);
            c = -constant;
            return result;
        }

        /// <summary>
        ///     Projects an input point into feature space.
        /// </summary>
        /// <param name="input">The input point to be projected into feature space.</param>
        /// <returns>
        ///     The feature space representation of the given <paramref name="input" /> point.
        /// </returns>
        public double[] Transform(double[] input)
        {
            return Transform(input, constant);
        }

        /// <summary>
        ///     Projects a set of input points into feature space.
        /// </summary>
        /// <param name="inputs">The input points to be projected into feature space.</param>
        /// <returns>
        ///     The feature space representation of the given <paramref name="inputs" /> points.
        /// </returns>
        public double[][] Transform(double[][] inputs)
        {
            var r = new double[inputs.Length][];
            for (var i = 0; i < inputs.Length; i++)
            {
                r[i] = Transform(inputs[i]);
            }

            return r;
        }

        /// <summary>
        ///     Projects an input point into feature space.
        /// </summary>
        /// <param name="input">The input point to be projected into feature space.</param>
        /// <param name="constant">The <see cref="Constant" /> parameter of the kernel.</param>
        /// <returns>
        ///     The feature space representation of the given <paramref name="input" /> point.
        /// </returns>
        public static double[] Transform(double[] input, double constant)
        {
            if (constant == 0)
            {
                return input;
            }

            var feature = new double[input.Length + 3];
            for (var i = 0; i < input.Length; i++)
            {
                feature[i] = input[i];
            }

            feature[input.Length] = constant;

            return feature;
        }

        /// <summary>
        ///     The kernel function.
        /// </summary>
        /// <param name="x">Vector <c>x</c> in input space.</param>
        /// <param name="y">Vector <c>y</c> in input space.</param>
        /// <returns>
        ///     Dot product in feature (kernel) space.
        /// </returns>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public double Function(Sparse<double> x, Sparse<double> y)
        {
            return x.Dot(y) + constant;
        }

        /// <summary>
        ///     The kernel function.
        /// </summary>
        /// <param name="x">Vector <c>x</c> in input space.</param>
        /// <param name="y">Vector <c>y</c> in input space.</param>
        /// <returns>
        ///     Dot product in feature (kernel) space.
        /// </returns>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public double Function(double[] y, Sparse<double> x)
        {
            return x.Dot(y) + constant;
        }

        /// <summary>
        ///     Elementwise addition of a and b, storing in result.
        /// </summary>
        /// <param name="a">The first vector to add.</param>
        /// <param name="b">The second vector to add.</param>
        /// <param name="result">An array to store the result.</param>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void Add(Sparse<double> a, double[] b, double[] result)
        {
            a.Add(b, result);
        }

        /// <summary>
        ///     Elementwise multiplication of scalar a and vector b, storing in result.
        /// </summary>
        /// <param name="a">The scalar to be multiplied.</param>
        /// <param name="b">The vector to be multiplied.</param>
        /// <param name="accumulate">An array to store the result.</param>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void Product(double a, Sparse<double> b, double[] accumulate)
        {
            if (a == 0)
            {
                return;
            }

            for (var j = 0; j < b.Indices.Length; j++)
            {
                accumulate[b.Indices[j]] += a * b.Values[j];
            }
        }

        ///// <summary>
        ///// Elementwise multiplication of scalar a and vector b, storing in result.
        ///// </summary>
        ///// <param name="a">The scalar to be multiplied.</param>
        ///// <param name="b">The vector to be multiplied.</param>
        ///// <param name="accumulate">An array to store the result.</param>
        //public void Product(double a, Sparse<double> b, Sparse<double> accumulate)
        //{
        //    if (a == 0)
        //        return;

        //    for (int j = 0; j < b.Indices.Length; j++)
        //        accumulate.Values[b.Indices[j]] += a * b.Values[j];
        //}

        /// <summary>
        ///     Compress a set of support vectors and weights into a single
        ///     parameter vector.
        /// </summary>
        /// <param name="weights">The weights associated with each support vector.</param>
        /// <param name="supportVectors">The support vectors.</param>
        /// <param name="c">The constant (bias) value.</param>
        /// <returns>
        ///     A single parameter vector.
        /// </returns>
        public Sparse<double> Compress(double[] weights, Sparse<double>[] supportVectors, out double c)
        {
            return Sparse.FromDense(Compress(weights, supportVectors.ToDense(), out c));
        }

        /// <summary>
        ///     Gets the number of parameters in the input vectors.
        /// </summary>
        public int GetLength(double[][] inputs)
        {
            return inputs.Columns(true);
        }

        /// <summary>
        ///     Gets the number of parameters in the input vectors.
        /// </summary>
        public int GetLength(Sparse<double>[] inputs)
        {
            return inputs.Columns();
        }

        /// <summary>
        ///     Creates an input vector from the given double values.
        /// </summary>
        public double[] CreateVector(double[] values)
        {
            return Vector.Create(values);
        }

        /// <summary>
        ///     Creates an input vector with the given dimensions.
        /// </summary>
        public double[] CreateVector(int dimensions)
        {
            return new double[dimensions];
        }

        Sparse<double> ILinear<Sparse<double>>.CreateVector(double[] values)
        {
            return Sparse.FromDense(values);
        }

        //Sparse<double> ILinear<Sparse<double>>.CreateVector(int dimensions)
        //{
        //    return Accord.Math.Sparse.FromDense(new double[dimensions], removeZeros: false);
        //}

        /// <summary>
        ///     Elementwise multiplication of vector a and vector b, accumulating in result.
        /// </summary>
        /// <param name="a">The vector to be multiplied.</param>
        /// <param name="b">The vector to be multiplied.</param>
        /// <param name="accumulate">An array to store the result.</param>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void Product(double[] a, Sparse<double> b, double[] accumulate)
        {
            for (var j = 0; j < b.Indices.Length; j++)
            {
                accumulate[b.Indices[j]] += a[b.Indices[j]] * b.Values[j];
            }
        }

        /// <summary>
        ///     Elementwise multiplication of vector a and vector b, accumulating in result.
        /// </summary>
        /// <param name="a">The vector to be multiplied.</param>
        /// <param name="b">The vector to be multiplied.</param>
        /// <param name="accumulate">An array to store the result.</param>
        public void Product(double[] a, double[] b, double[] accumulate)
        {
            for (var i = 0; i < a.Length; i++)
            {
                accumulate[i] += a[i] * b[i];
            }
        }

        /// <summary>
        ///     Converts the input vectors to a double-precision representation.
        /// </summary>
        public double[][] ToDouble(double[][] input)
        {
            return input;
        }

        /// <summary>
        ///     Converts the input vectors to a double-precision representation.
        /// </summary>
        public double[][] ToDouble(Sparse<double>[] input)
        {
            return input.ToDense();
        }
    }
}