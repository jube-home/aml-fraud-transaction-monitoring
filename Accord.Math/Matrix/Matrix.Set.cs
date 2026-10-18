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

namespace Accord.Math
{
    public static partial class Matrix
    {
        /// <summary>
        ///     Sets a region of a matrix to the given values.
        /// </summary>
        /// <param name="destination">The matrix where elements will be set.</param>
        /// <param name="value">The value to which matrix elements will be set.</param>
        public static void Set<T>(this T[][] destination, T value)
        {
            Set(destination, Jagged.Create(destination.Rows(), destination.Columns(), value));
        }

        /// <summary>
        ///     Sets a region of a matrix to the given values.
        /// </summary>
        /// <param name="destination">The matrix where elements will be set.</param>
        /// <param name="value">The value to which matrix elements will be set.</param>
        public static void Set<T>(this T[,] destination, T value)
        {
            Set(destination, Create(destination.Rows(), destination.Columns(), value));
        }

        /// <summary>
        ///     Sets a region of a matrix to the given values.
        /// </summary>
        /// <param name="destination">The matrix where elements will be set.</param>
        /// <param name="value">The matrix of values to which matrix elements will be set.</param>
        public static void Set<T>(this T[][] destination, T[][] value)
        {
            set(destination, null, null, value, null, null);
        }

        /// <summary>
        ///     Sets a region of a matrix to the given values.
        /// </summary>
        /// <param name="destination">The matrix where elements will be set.</param>
        /// <param name="value">The matrix of values to which matrix elements will be set.</param>
        public static void Set<T>(this T[,] destination, T[,] value)
        {
            set(destination, null, null, value, null, null);
        }

        /// <summary>
        ///     Sets a region of a matrix to the given values.
        /// </summary>
        /// <param name="destination">The matrix where elements will be set.</param>
        /// <param name="value">The matrix of values to which matrix elements will be set.</param>
        /// <param name="rowIndices">Array of row indices</param>
        /// <param name="startColumn">Start column index</param>
        /// <param name="endColumn">End column index</param>
        public static void Set<T>(this T[][] destination, T[][] value, int[] rowIndices, int startColumn, int endColumn)
        {
            var endIndex = end(endColumn, destination.Columns());
            var columnIndices = Math.Vector.Range(startColumn, endIndex);
            set(destination, rowIndices, columnIndices, value, rowIndices, columnIndices);
        }

        /// <summary>
        ///     Sets a region of a matrix to the given values.
        /// </summary>
        /// <param name="destination">The matrix where elements will be set.</param>
        /// <param name="value">The matrix of values to which matrix elements will be set.</param>
        /// <param name="rowIndices">Array of row indices</param>
        /// <param name="startColumn">Start column index</param>
        /// <param name="endColumn">End column index</param>
        public static void Set<T>(this T[,] destination, T[,] value, int[] rowIndices, int startColumn, int endColumn)
        {
            var endIndex = end(endColumn, destination.Columns());
            var columnIndices = Math.Vector.Range(startColumn, endIndex);
            set(destination, rowIndices, columnIndices, value, rowIndices, columnIndices);
        }

        /// <summary>
        ///     Sets a region of a matrix to the given values.
        /// </summary>
        /// <param name="destination">The matrix where elements will be set.</param>
        /// <param name="value">The matrix of values to which matrix elements will be set.</param>
        /// <param name="rowIndices">Array of row indices</param>
        /// <param name="startColumn">Start column index</param>
        /// <param name="endColumn">End column index</param>
        public static void Set<T>(this T[][] destination, T value, int[] rowIndices, int startColumn, int endColumn)
        {
            Set(destination, Jagged.Create(destination.Rows(), destination.Columns(), value), rowIndices, startColumn,
                endColumn);
        }

        /// <summary>
        ///     Sets a region of a matrix to the given values.
        /// </summary>
        /// <param name="destination">The matrix where elements will be set.</param>
        /// <param name="value">The matrix of values to which matrix elements will be set.</param>
        /// <param name="columnIndices">Array of column indices.</param>
        /// <param name="startRow">Start row index</param>
        /// <param name="endRow">End row index</param>
        public static void Set<T>(this T[,] destination, T[,] value, int startRow, int endRow, int[] columnIndices)
        {
            var endIndex = end(endRow, destination.Rows());
            var rowIndices = Math.Vector.Range(startRow, endIndex);
            set(destination, rowIndices, columnIndices, value, rowIndices, columnIndices);
        }

        /// <summary>
        ///     Returns a sub matrix extracted from the current matrix.
        /// </summary>
        /// <param name="destination">The matrix where elements will be set.</param>
        /// <param name="value">The matrix of values to which matrix elements will be set.</param>
        /// <param name="columnIndices">Array of column indices.</param>
        /// <param name="startRow">Start row index</param>
        /// <param name="endRow">End row index</param>
        public static void Set<T>(this T[][] destination, T[][] value, int startRow, int endRow, int[] columnIndices)
        {
            var endIndex = end(endRow, destination.Rows());
            var rowIndices = Math.Vector.Range(startRow, endIndex);
            set(destination, rowIndices, columnIndices, value, rowIndices, columnIndices);
        }

        /// <summary>
        ///     Sets a region of a matrix to the given values.
        /// </summary>
        /// <param name="destination">The matrix where elements will be set.</param>
        /// <param name="value">The matrix of values to which matrix elements will be set.</param>
        /// <param name="rowIndices">Array of row indices</param>
        /// <param name="startColumn">Start column index</param>
        /// <param name="endColumn">End column index</param>
        public static void Set<T>(this T[,] destination, T value, int[] rowIndices, int startColumn, int endColumn)
        {
            var source = Create(destination.Rows(), destination.Columns(), value);
            Set(destination, source, rowIndices, startColumn, endColumn);
        }

        /// <summary>
        ///     Sets a region of a matrix to the given values.
        /// </summary>
        /// <param name="destination">The matrix where elements will be set.</param>
        /// <param name="value">The matrix of values to which matrix elements will be set.</param>
        /// <param name="startRow">Starting row index</param>
        /// <param name="endRow">End row index</param>
        /// <param name="columnIndices">Array of column indices</param>
        public static void Set<T>(this T[][] destination, T value, int startRow, int endRow, int[] columnIndices)
        {
            var source = Jagged.Create(destination.Rows(), destination.Columns(), value);
            Set(destination, source, startRow, endRow, columnIndices);
        }

        /// <summary>
        ///     Sets a region of a matrix to the given values.
        /// </summary>
        /// <param name="destination">The matrix where elements will be set.</param>
        /// <param name="value">The matrix of values to which matrix elements will be set.</param>
        /// <param name="startRow">Starting row index</param>
        /// <param name="endRow">End row index</param>
        /// <param name="columnIndices">Array of column indices</param>
        public static void Set<T>(this T[,] destination, T value, int startRow, int endRow, int[] columnIndices)
        {
            var source = Create(destination.Rows(), destination.Columns(), value);
            Set(destination, source, startRow, endRow, columnIndices);
        }

        /// <summary>
        ///     Sets a region of a matrix to the given values.
        /// </summary>
        /// <param name="destination">The matrix where elements will be set.</param>
        /// <param name="value">The matrix of values to which matrix elements will be set.</param>
        /// <param name="startRow">Starting row index</param>
        /// <param name="endRow">End row index</param>
        /// <param name="startCol">Starting column index</param>
        /// <param name="endCol">End column index</param>
        public static void Set<T>(this T[][] destination, T value, int startRow, int endRow, int startCol, int endCol)
        {
            var values = Jagged.Create(destination.Rows(), destination.Columns(), value);
            var rowIndex = end(endRow, destination.Rows());
            var rowIndices = Math.Vector.Range(startRow, rowIndex);
            var colIndex = end(endCol, destination.Columns());
            var columnIndices = Math.Vector.Range(startCol, colIndex);
            set(destination, rowIndices, columnIndices, values, rowIndices, columnIndices);
        }

        /// <summary>
        ///     Sets a region of a matrix to the given values.
        /// </summary>
        /// <param name="destination">The matrix where elements will be set.</param>
        /// <param name="value">The matrix of values to which matrix elements will be set.</param>
        /// <param name="startRow">Starting row index</param>
        /// <param name="endRow">End row index</param>
        /// <param name="startCol">Starting column index</param>
        /// <param name="endCol">End column index</param>
        public static void Set<T>(this T[,] destination, T value, int startRow, int endRow, int startCol, int endCol)
        {
            var values = Create(destination.Rows(), destination.Columns(), value);
            var rowIndex = end(endRow, destination.Rows());
            var rowIndices = Math.Vector.Range(startRow, rowIndex);
            var colIndex = end(endCol, destination.Columns());
            var columnIndices = Math.Vector.Range(startCol, colIndex);
            set(destination, rowIndices, columnIndices, values, rowIndices, columnIndices);
        }

        /// <summary>
        ///     Sets a region of a matrix to the given values.
        /// </summary>
        /// <param name="destination">The matrix where elements will be set.</param>
        /// <param name="value">The matrix of values to which matrix elements will be set.</param>
        /// <param name="rowIndices">Array of row indices</param>
        /// <param name="columnIndices">Array of column indices</param>
        public static void Set<T>(this T[][] destination, T value, int[] rowIndices, int[] columnIndices)
        {
            var values = Jagged.Create(destination.Rows(), destination.Columns(), value);
            set(destination, rowIndices, columnIndices, values, rowIndices, columnIndices);
        }

        /// <summary>
        ///     Sets a region of a matrix to the given values.
        /// </summary>
        /// <param name="destination">The matrix where elements will be set.</param>
        /// <param name="value">The matrix of values to which matrix elements will be set.</param>
        /// <param name="rowIndices">Array of row indices</param>
        /// <param name="columnIndices">Array of column indices</param>
        public static void Set<T>(this T[,] destination, T value, int[] rowIndices, int[] columnIndices)
        {
            var values = Create(destination.Rows(), destination.Columns(), value);
            set(destination, rowIndices, columnIndices, values, rowIndices, columnIndices);
        }

        /// <summary>
        ///     Sets a region of a matrix to the given values.
        /// </summary>
        /// <param name="destination">The matrix where elements will be set.</param>
        /// <param name="value">The matrix of values to which matrix elements will be set.</param>
        /// <param name="rowIndices">Array of row indices</param>
        /// <param name="columnIndices">Array of column indices</param>
        public static void Set<T>(this T[][] destination, T[][] value, int[] rowIndices, int[] columnIndices)
        {
            set(destination, rowIndices, columnIndices, value, rowIndices, columnIndices);
        }

        /// <summary>
        ///     Sets a region of a matrix to the given values.
        /// </summary>
        /// <param name="destination">The matrix where elements will be set.</param>
        /// <param name="value">The matrix of values to which matrix elements will be set.</param>
        /// <param name="rowIndices">Array of row indices</param>
        /// <param name="columnIndices">Array of column indices</param>
        public static void Set<T>(this T[,] destination, T[,] value, int[] rowIndices, int[] columnIndices)
        {
            set(destination, rowIndices, columnIndices, value, rowIndices, columnIndices);
        }

        /// <summary>
        ///     Sets a region of a matrix to the given values.
        /// </summary>
        /// <param name="destination">The matrix where elements will be set.</param>
        /// <param name="value">The matrix of values to which matrix elements will be set.</param>
        /// <param name="indices">Array of indices.</param>
        public static void Set<T>(this T[] destination, T value, int[] indices)
        {
            if (destination == null)
            {
                throw new ArgumentNullException("destination");
            }

            if (indices == null)
            {
                throw new ArgumentNullException("indices");
            }

            for (var i = 0; i < indices.Length; i++)
            {
                destination[index(indices[i], destination.Length)] = value;
            }
        }

        /// <summary>
        ///     Sets a subvector to the given value.
        /// </summary>
        /// <param name="destination">The vector to return the subvector from.</param>
        /// <param name="value">The matrix of values to which matrix elements will be set.</param>
        /// <param name="startRow">Starting index.</param>
        /// <param name="endRow">End index.</param>
        public static void Set<T>(this T[] destination, T value, int startRow, int endRow)
        {
            endRow = end(endRow, destination.Length);
            for (var i = startRow; i < endRow; i++)
            {
                destination[i] = value;
            }
        }

        /// <summary>
        ///     Sets a subvector to the given value.
        /// </summary>
        /// <param name="destination">The vector to return the subvector from.</param>
        /// <param name="value">The matrix of values to which matrix elements will be set.</param>
        /// <param name="index">The index of the element to be set.</param>
        public static void Set<T>(this T[] destination, T value, int index)
        {
            if (destination == null)
            {
                throw new ArgumentNullException("destination");
            }

            if (index >= destination.Length)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            index = Matrix.index(index, destination.Length);

            destination[index] = value;
        }

        /// <summary>
        ///     Sets a subvector to the given value.
        /// </summary>
        /// <param name="destination">The vector to return the subvector from.</param>
        /// <param name="value">The matrix of values to which matrix elements will be set.</param>
        /// <param name="indices">Array of indices.</param>
        public static void Set<T>(this List<T> destination, T value, int[] indices)
        {
            if (destination == null)
            {
                throw new ArgumentNullException("destination");
            }

            if (indices == null)
            {
                throw new ArgumentNullException("indices");
            }

            for (var i = 0; i < indices.Length; i++)
            {
                destination[indices[i]] = value;
            }
        }

        private static void set<T>(this
                T[,] dst, int[] dstRowIndices, int[] dstColumnIndices,
            T[,] src, int[] srcRowIndices, int[] srcColumnIndices)
        {
            if (src == null)
            {
                throw new ArgumentNullException("src");
            }

            if (dst == null)
            {
                throw new ArgumentNullException("dst");
            }

            if (srcRowIndices == null)
            {
                srcRowIndices = Math.Vector.Range(0, src.Rows());
            }

            if (srcColumnIndices == null)
            {
                srcColumnIndices = Math.Vector.Range(0, src.Columns());
            }

            if (dstRowIndices == null)
            {
                dstRowIndices = Math.Vector.Range(0, dst.Rows());
            }

            if (dstColumnIndices == null)
            {
                dstColumnIndices = Math.Vector.Range(0, dst.Columns());
            }

            for (var i = 0; i < srcRowIndices.Length; i++)
            {
                var si = index(srcRowIndices[i], src.Rows());
                var di = index(dstRowIndices[i], dst.Rows());

                for (var j = 0; j < srcColumnIndices.Length; j++)
                {
                    var sj = index(srcColumnIndices[j], src.Columns());
                    var dj = index(dstColumnIndices[j], dst.Columns());
                    dst[di, dj] = src[si, sj];
                }
            }
        }

        private static void set<T>(this
                T[][] dst, int[] dstRowIndices, int[] dstColumnIndices,
            T[][] src, int[] srcRowIndices, int[] srcColumnIndices)
        {
            if (src == null)
            {
                throw new ArgumentNullException("src");
            }

            if (dst == null)
            {
                throw new ArgumentNullException("dst");
            }

            if (srcRowIndices == null)
            {
                srcRowIndices = Math.Vector.Range(0, src.Rows());
            }

            if (srcColumnIndices == null)
            {
                srcColumnIndices = Math.Vector.Range(0, src.Columns());
            }

            if (dstRowIndices == null)
            {
                dstRowIndices = Math.Vector.Range(0, dst.Rows());
            }

            if (dstColumnIndices == null)
            {
                dstColumnIndices = Math.Vector.Range(0, dst.Columns());
            }

            for (var i = 0; i < srcRowIndices.Length; i++)
            {
                var si = index(srcRowIndices[i], src.Rows());
                var di = index(dstRowIndices[i], dst.Rows());

                for (var j = 0; j < srcColumnIndices.Length; j++)
                {
                    var sj = index(srcColumnIndices[j], src.Columns());
                    var dj = index(dstColumnIndices[j], dst.Columns());
                    dst[di][dj] = src[si][sj];
                }
            }
        }
    }
}