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
using System.Linq;
using System.Runtime.InteropServices;

namespace Accord.Math
{
    public static partial class Matrix
    {
        /// <summary>
        ///     Adds a new dimension to an array with length 1.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="dimension">The index where the dimension should be added.</param>
        public static Array ExpandDimensions(this Array array, int dimension)
        {
            var dimensions = array.GetLength().ToList();
            dimensions.Insert(dimension, 1);
            var res = Array.CreateInstance(array.GetInnerMostType(), dimensions.ToArray());
            Buffer.BlockCopy(array, 0, res, 0, res.GetNumberOfBytes());
            return res;
        }

        /// <summary>
        ///     Removes dimensions of length 1 from the array.
        /// </summary>
        /// <param name="array">The array.</param>
        public static Array Squeeze(this Array array)
        {
            var dimensions = array.GetLength().Where(x => x != 1).ToArray();
            if (dimensions.Length == 0)
            {
                dimensions = new[] { 1 };
            }

            Array res;
            if (array.IsJagged())
            {
#if NETSTANDARD1_4
                throw new NotSupportedException("Squeeze with jagged arrays is not supported in .NET Standard 1.4.");
#else
                res = Jagged.Zeros(array.GetInnerMostType(), dimensions);
                Copy(array, res);
#endif
            }
            else
            {
                res = Zeros(array.GetInnerMostType(), dimensions);
                Buffer.BlockCopy(array, 0, res, 0, res.GetNumberOfBytes());
            }

            return res;
        }

        /// <summary>
        ///     Transforms a tensor into a single vector.
        /// </summary>
        /// <param name="array">An array.</param>
        /// <param name="order">
        ///     The direction to perform copying. Pass
        ///     1 to perform a copy by reading the matrix in row-major order.
        ///     Pass 0 to perform a copy in column-major copy. Default is 1
        ///     (row-major, c-style order).
        /// </param>
        public static Array Flatten(this Array array, MatrixOrder order = MatrixOrder.CRowMajor)
        {
            var t = array.GetInnerMostType();

            if (order == MatrixOrder.CRowMajor)
            {
                var dst = Array.CreateInstance(t, array.Length);
#pragma warning disable CS0618 // Type or member is obsolete
                Buffer.BlockCopy(array, 0, dst, 0, dst.Length * Marshal.SizeOf(t));
#pragma warning restore CS0618 // Type or member is obsolete
                return dst;
            }

            var r = Array.CreateInstance(t, array.Length);

            var c = 0;
            foreach (var idx in array.GetIndices(order: order))
            {
                r.SetValue(array.GetValue(idx), c++);
            }

            return r;
        }

        /// <summary>
        ///     Changes the length of individual dimensions in an array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="shape">The new shape.</param>
        /// <param name="order">
        ///     The direction to perform copying. Pass
        ///     1 to perform a copy by reading the matrix in row-major order.
        ///     Pass 0 to perform a copy in column-major copy. Default is 1
        ///     (row-major, c-style order).
        /// </param>
        public static Array Reshape(this Array array, int[] shape, MatrixOrder order = MatrixOrder.CRowMajor)
        {
            var t = array.GetInnerMostType();

            var r = Array.CreateInstance(t, shape);

            var c = array.GetIndices().GetEnumerator();
            foreach (var idx in r.GetIndices(order: order))
            {
                c.MoveNext();
                r.SetValue(array.GetValue(c.Current), idx);
            }

            return r;
        }

        /// <summary>
        ///     Converts the values of a tensor.
        /// </summary>
        /// <typeparam name="TOutput">The type of the output.</typeparam>
        /// <param name="array">The tensor to be converted.</param>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Math\Matrix\Matrix.Conversion.cs" region="doc_convert_matrix" />
        ///     <code source="Unit Tests\Accord.Tests.Math\Matrix\Matrix.Conversion.cs" region="doc_convert_jagged" />
        /// </example>
        public static Array Convert<TOutput>(this Array array)
        {
            return Convert(array, typeof(TOutput));
        }

        /// <summary>
        ///     Converts the values of a tensor.
        /// </summary>
        /// <param name="type">The type of the output.</param>
        /// <param name="array">The tensor to be converted.</param>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Math\Matrix\Matrix.Conversion.cs" region="doc_convert_matrix" />
        ///     <code source="Unit Tests\Accord.Tests.Math\Matrix\Matrix.Conversion.cs" region="doc_convert_jagged" />
        /// </example>
        public static Array Convert(this Array array, Type type)
        {
            var r = Zeros(type, array.GetLength());

            foreach (var idx in r.GetIndices(true))
            {
                var value = array.GetValue(true, idx).To(type);
                r.SetValue(value, true, idx);
            }

            return r;
        }

        /// <summary>
        ///     Returns a subtensor extracted from the current tensor.
        /// </summary>
        /// <param name="source">The tensor to return the subvector from.</param>
        /// <param name="dimension">The dimension from which the indices should be extracted.</param>
        /// <param name="indices">Array of indices.</param>
        public static Array Get(this Array source, int dimension, int[] indices)
        {
            var lengths = source.GetLength();
            lengths[dimension] = indices.Length;

            var type = source.GetInnerMostType();
            var r = Array.CreateInstance(type, lengths);

            for (var i = 0; i < indices.Length; i++)
            {
                Set(r, dimension, i, Get(source, dimension, indices[i]));
            }

            return r;
        }

        /// <summary>
        ///     Returns a subtensor extracted from the current tensor.
        /// </summary>
        /// <param name="source">The tensor to return the subvector from.</param>
        /// <param name="dimension">The dimension from which the indices should be extracted.</param>
        /// <param name="index">The index.</param>
        public static Array Get(this Array source, int dimension, int index)
        {
            return Get(source, dimension, index, index + 1);
        }

        /// <summary>
        ///     Returns a subtensor extracted from the current tensor.
        /// </summary>
        /// <param name="source">The tensor to return the subvector from.</param>
        /// <param name="dimension">The dimension from which the indices should be extracted.</param>
        /// <param name="start">The start index.</param>
        /// <param name="end">The end index.</param>
        public static Array Get(this Array source, int dimension, int start, int end)
        {
            if (dimension != 0)
            {
                throw new NotImplementedException("Retrieving dimensions higher than zero has not been implemented" +
                                                  " yet. Please open a new issue at the issue tracker if you need such functionality.");
            }

            var length = source.GetLength();
            length = length.RemoveAt(dimension);
            var rows = end - start;
            if (length.Length == 0)
            {
                length = new[]
                {
                    rows
                };
            }

            var type = source.GetInnerMostType();
            var r = Array.CreateInstance(type, length);
            var rowSize = source.Length / source.GetLength(dimension);
#pragma warning disable CS0618 // Type or member is obsolete
            Buffer.BlockCopy(source, start * rowSize * Marshal.SizeOf(type), r, 0,
                rows * rowSize * Marshal.SizeOf(type));
#pragma warning restore CS0618 // Type or member is obsolete
            return r;
        }

        /// <summary>
        ///     Sets a region of a matrix to the given values.
        /// </summary>
        /// <param name="destination">The matrix where elements will be set.</param>
        /// <param name="dimension">The dimension where indices refer to.</param>
        /// <param name="value">The matrix of values to which matrix elements will be set.</param>
        /// <param name="index">The index.</param>
        public static void Set(this Array destination, int dimension, int index, Array value)
        {
            Set(destination, dimension, index, index + 1, value);
        }

        /// <summary>
        ///     Sets a region of a matrix to the given values.
        /// </summary>
        /// <param name="destination">The matrix where elements will be set.</param>
        /// <param name="dimension">The dimension where indices refer to.</param>
        /// <param name="value">The matrix of values to which matrix elements will be set.</param>
        /// <param name="start">The start index.</param>
        /// <param name="end">The end index.</param>
        public static void Set(this Array destination, int dimension, int start, int end, Array value)
        {
            if (dimension != 0)
            {
                throw new NotImplementedException("Retrieving dimensions higher than zero has not been implemented" +
                                                  " yet. Please open a new issue at the issue tracker if you need such functionality.");
            }

            var type = destination.GetInnerMostType();
            var rowSize = destination.Length / destination.GetLength(0);
            var length = end - start;
#pragma warning disable CS0618 // Type or member is obsolete
            Buffer.BlockCopy(value, 0, destination, start * rowSize * Marshal.SizeOf(type),
                length * rowSize * Marshal.SizeOf(type));
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <summary>
        ///     Returns true if a tensor is square.
        /// </summary>
        public static bool IsSquare(this Array array)
        {
            var first = array.GetLength(0);
            for (var i = 1; i < array.Rank; i++)
            {
                if (array.GetLength(i) != first)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Creates a zero-valued tensor.
        /// </summary>
        /// <param name="shape">The number of dimensions that the tensor should have.</param>
        /// <returns>A tensor of the specified shape.</returns>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static Array Zeros<T>(params int[] shape)
        {
            return Array.CreateInstance(typeof(T), shape);
        }

        /// <summary>
        ///     Creates a zero-valued tensor.
        /// </summary>
        /// <param name="type">The type of the elements to be contained in the tensor.</param>
        /// <param name="shape">The number of dimensions that the tensor should have.</param>
        /// <returns>A tensor of the specified shape.</returns>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static Array Zeros(Type type, params int[] shape)
        {
            return Array.CreateInstance(type, shape);
        }

        /// <summary>
        ///     Creates a tensor with all values set to a given value.
        /// </summary>
        /// <param name="shape">The number of dimensions that the matrix should have.</param>
        /// <param name="value">The initial values for the vector.</param>
        /// <returns>A matrix of the specified size.</returns>
        public static Array Create<T>(int[] shape, T value)
        {
            return Create(typeof(T), shape, value);
        }

        /// <summary>
        ///     Creates a tensor with all values set to a given value.
        /// </summary>
        /// <param name="elementType">The type of the elements to be contained in the matrix.</param>
        /// <param name="shape">The number of dimensions that the matrix should have.</param>
        /// <param name="value">The initial values for the vector.</param>
        /// <returns>A matrix of the specified size.</returns>
        public static Array Create(Type elementType, int[] shape, object value)
        {
            var arr = Array.CreateInstance(elementType, shape);
            foreach (var idx in arr.GetIndices())
            {
                arr.SetValue(value, idx);
            }

            return arr;
        }
    }
}