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
using System.Globalization;
using System.Linq;

namespace Accord.Math
{
    /// <summary>
    ///     Extension methods for <see cref="Sparse{T}">sparse vectors</see>.
    /// </summary>
    public static class Sparse
    {
        /// <summary>
        ///     Parses a string containing a sparse array in LibSVM
        ///     format into a <see cref="Sparse{T}" /> vector.
        /// </summary>
        /// <param name="values">
        ///     An array of "index:value" strings indicating
        ///     where each value belongs in the sparse vector.
        /// </param>
        /// <param name="insertValueAtBeginning">
        ///     Whether an intercept term should be added
        ///     at the beginning of the vector.
        /// </param>
        public static Sparse<double> Parse(string values, double? insertValueAtBeginning = null)
        {
            return Parse(values.Split(' '), insertValueAtBeginning);
        }

        /// <summary>
        ///     Parses a string containing a sparse array in LibSVM
        ///     format into a <see cref="Sparse{T}" /> vector.
        /// </summary>
        /// <param name="values">
        ///     An array of "index:value" strings indicating
        ///     where each value belongs in the sparse vector.
        /// </param>
        /// <param name="insertValueAtBeginning">
        ///     Whether an intercept term should be added
        ///     at the beginning of the vector.
        /// </param>
        public static Sparse<double> Parse(string[] values, double? insertValueAtBeginning = null)
        {
            var addIntercept = insertValueAtBeginning.HasValue && insertValueAtBeginning.Value != 0;

            var offset = addIntercept ? 1 : 0;

            var result = new Sparse<double>(values.Length + offset);

            for (var i = 0; i < values.Length; i++)
            {
                var element = values[i].Split(':');
                var oneBasedindex = int.Parse(element[0], CultureInfo.InvariantCulture);
                if (oneBasedindex <= 0)
                {
                    throw new FormatException(
                        "The given string contains 0 or negative indices (indices of sparse vectors in LibSVM format should begin at 1).");
                }

                var zeroBasedIndex = oneBasedindex - 1; // LibSVM uses 1-based array format
                var value = double.Parse(element[1], CultureInfo.InvariantCulture);

                result.Indices[i + offset] = zeroBasedIndex + offset;
                result.Values[i + offset] = value;
            }

            if (addIntercept)
            {
                result.Values[0] = insertValueAtBeginning.Value;
            }

            return result;
        }

        /// <summary>
        ///     Converts an array of sparse vectors into a jagged matrix.
        /// </summary>
        public static T[][] ToDense<T>(this Sparse<T>[] vectors)
            where T : IEquatable<T>
        {
            return ToDense(vectors, vectors.Columns());
        }

        /// <summary>
        ///     Converts an array of sparse vectors into a jagged matrix.
        /// </summary>
        public static T[][] ToDense<T>(this Sparse<T>[] vectors, int length)
            where T : IEquatable<T>
        {
            var dense = new T[vectors.Length][];
            for (var i = 0; i < dense.Length; i++)
            {
                dense[i] = vectors[i].ToDense(length);
            }

            return dense;
        }

        /// <summary>
        ///     Creates a sparse vector from a dense array.
        /// </summary>
        public static Sparse<T> FromDense<T>(T[] dense, bool removeZeros = true)
            where T : IEquatable<T>
        {
            if (removeZeros)
            {
                var zero = default(T);

                var nonZeros = 0;
                for (var i = 0; i < dense.Length; i++)
                {
                    if (!zero.Equals(dense[i]))
                    {
                        nonZeros++;
                    }
                }

                var idx = new int[nonZeros];
                var values = new T[nonZeros];
                for (int i = 0, c = 0; i < dense.Length; i++)
                {
                    if (!zero.Equals(dense[i]))
                    {
                        idx[c] = i;
                        values[c] = dense[i];
                        c++;
                    }
                }

                return new Sparse<T>(idx, values);
            }

            var denseIdx = new int[dense.Length];
            for (var i = 0; i < denseIdx.Length; i++)
            {
                denseIdx[i] = i;
            }

            return new Sparse<T>(denseIdx, dense);
        }

        /// <summary>
        ///     Creates sparse vectors from dense arrays.
        /// </summary>
        public static Sparse<T>[] FromDense<T>(T[][] dense, bool removeZeros = true)
            where T : IEquatable<T>
        {
            var result = new Sparse<T>[dense.Length];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = FromDense(dense[i], removeZeros);
            }

            return result;
        }

        /// <summary>
        ///     Gets the maximum number of columns (dimensions)
        ///     that can be inferred from the given sparse vectors.
        /// </summary>
        public static int Columns<T>(this Sparse<T>[] inputs)
            where T : IEquatable<T>
        {
            var max = 0;
            for (var i = 0; i < inputs.Length; i++)
            {
                var c = inputs[i].Length;
                if (c > max)
                {
                    max = c;
                }
            }

            return max;
        }

        /// <summary>
        ///     Creates a sparse vector from a dictionary mapping indices to values.
        /// </summary>
        public static Sparse<double> FromDictionary(IDictionary<int, int> dictionary)
        {
            var indices = dictionary.Keys.ToArray();

            Array.Sort(indices);

            var values = new double[indices.Length];
            for (var i = 0; i < indices.Length; i++)
            {
                values[i] = dictionary[indices[i]];
            }

            return new Sparse<double>(indices, values);
        }
    }
}