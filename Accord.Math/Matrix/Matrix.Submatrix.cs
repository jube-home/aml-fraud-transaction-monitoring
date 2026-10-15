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
using Accord.Statistics;

namespace Accord.Math
{
    public static partial class Matrix
    {
        /// <summary>
        ///     Returns a sub matrix extracted from the current matrix.
        /// </summary>
        /// <param name="source">The matrix to return the submatrix from.</param>
        /// <param name="startRow">Start row index</param>
        /// <param name="endRow">End row index</param>
        /// <param name="startColumn">Start column index</param>
        /// <param name="endColumn">End column index</param>
        [Obsolete("Please use Get instead.")]
        public static T[,] Submatrix<T>(this T[,] source,
            int startRow, int endRow, int startColumn, int endColumn)
        {
            return Get(source, startRow, endRow + 1, startColumn, endColumn + 1);
        }

        /// <summary>
        ///     Returns a sub matrix extracted from the current matrix.
        /// </summary>
        /// <param name="source">The matrix to return the submatrix from.</param>
        /// <param name="destination">The matrix where results should be stored.</param>
        /// <param name="startRow">Start row index</param>
        /// <param name="endRow">End row index</param>
        /// <param name="startColumn">Start column index</param>
        /// <param name="endColumn">End column index</param>
        [Obsolete("Please use Get instead.")]
        public static T[,] Submatrix<T>(this T[,] source, T[,] destination,
            int startRow, int endRow, int startColumn, int endColumn)
        {
            return Get(source, destination, startRow, endRow + 1, startColumn, endColumn + 1);
        }

        /// <summary>
        ///     Returns a sub matrix extracted from the current matrix.
        /// </summary>
        /// <param name="source">The matrix to return the submatrix from.</param>
        /// <param name="rowIndexes">Array of row indices. Pass null to select all indices.</param>
        /// <param name="columnIndexes">Array of column indices. Pass null to select all indices.</param>
        [Obsolete("Please use Get instead.")]
        public static T[,] Submatrix<T>(this T[,] source, int[] rowIndexes, int[] columnIndexes)
        {
            return get(source, null, rowIndexes, columnIndexes);
        }

        /// <summary>
        ///     Returns a sub matrix extracted from the current matrix.
        /// </summary>
        /// <param name="source">The matrix to return the submatrix from.</param>
        /// <param name="destination">The matrix where results should be stored.</param>
        /// <param name="rowIndexes">Array of row indices. Pass null to select all indices.</param>
        /// <param name="columnIndexes">Array of column indices. Pass null to select all indices.</param>
        [Obsolete("Please use Get instead.")]
        public static void Submatrix<T>(this T[,] source, T[,] destination, int[] rowIndexes, int[] columnIndexes)
        {
            get(source, destination, rowIndexes, columnIndexes);
        }

        /// <summary>
        ///     Returns a sub matrix extracted from the current matrix.
        /// </summary>
        /// <param name="source">The matrix to return the submatrix from.</param>
        /// <param name="rowIndexes">Array of row indices</param>
        [Obsolete("Please use Get instead.")]
        public static T[,] Submatrix<T>(this T[,] source, int[] rowIndexes)
        {
            return get(source, null, rowIndexes, null);
        }

        /// <summary>
        ///     Returns a sub matrix extracted from the current matrix.
        /// </summary>
        /// <param name="source">The matrix to return the submatrix from.</param>
        /// <param name="startRow">Starting row index</param>
        /// <param name="endRow">End row index</param>
        /// <param name="columnIndexes">Array of column indices</param>
        /// <remarks>
        ///     Routine adapted from Lutz Roeder's Mapack for .NET, September 2000.
        /// </remarks>
        [Obsolete("Please use Get instead.")]
        public static T[,] Submatrix<T>(this T[,] source, int startRow, int endRow, int[] columnIndexes)
        {
            return Get(source, startRow, endRow + 1, columnIndexes);
        }

        /// <summary>
        ///     Returns a sub matrix extracted from the current matrix.
        /// </summary>
        /// <param name="source">The matrix to return the submatrix from.</param>
        /// <param name="rowIndexes">Array of row indices</param>
        /// <param name="startColumn">Start column index</param>
        /// <param name="endColumn">End column index</param>
        /// <remarks>
        ///     Routine adapted from Lutz Roeder's Mapack for .NET, September 2000.
        /// </remarks>
        [Obsolete("Please use Get instead.")]
        public static T[,] Submatrix<T>(this T[,] source, int[] rowIndexes, int startColumn, int endColumn)
        {
            return Get(source, rowIndexes, startColumn, endColumn + 1);
        }

        /// <summary>
        ///     Returns a sub matrix extracted from the current matrix.
        /// </summary>
        /// <param name="source">The matrix to return the submatrix from.</param>
        /// <param name="startRow">Start row index</param>
        /// <param name="endRow">End row index</param>
        /// <param name="startColumn">Start column index</param>
        /// <param name="endColumn">End column index</param>
        [Obsolete("Please use Get instead.")]
        public static T[][] Submatrix<T>(this T[][] source,
            int startRow, int endRow, int startColumn, int endColumn)
        {
            return get(source, null, startRow, endRow + 1, startColumn, endColumn + 1, false);
        }

        /// <summary>
        ///     Returns a sub matrix extracted from the current matrix.
        /// </summary>
        /// <param name="source">The matrix to return the submatrix from.</param>
        /// <param name="rowIndexes">Array of row indices. Pass null to select all indices.</param>
        /// <param name="columnIndexes">Array of column indices. Pass null to select all indices.</param>
        /// <param name="reuseMemory">
        ///     Set to true to avoid memory allocations
        ///     when possible. This might result on the shallow copies of some
        ///     elements. Default is false (default is to always provide a true,
        ///     deep copy of every element in the matrices, using more memory).
        /// </param>
        public static T[][] Submatrix<T>(this T[][] source,
            int[] rowIndexes, int[] columnIndexes, bool reuseMemory = false)
        {
            return get(source, null, rowIndexes, columnIndexes, reuseMemory);
        }

        /// <summary>
        ///     Returns a sub matrix extracted from the current matrix.
        /// </summary>
        /// <param name="source">The matrix to return the submatrix from.</param>
        /// <param name="indexes">Array of indices.</param>
        /// <param name="transpose">True to return a transposed matrix; false otherwise.</param>
        [Obsolete("Please use Get instead.")]
        public static T[][] Submatrix<T>(this T[][] source, int[] indexes, bool transpose = false)
        {
            return Get(source, indexes, transpose);
        }

        /// <summary>
        ///     Returns a sub matrix extracted from the current matrix.
        /// </summary>
        /// <param name="source">The matrix to return the submatrix from.</param>
        /// <param name="rowIndexes">Array of row indices</param>
        /// <param name="startColumn">Start column index</param>
        /// <param name="endColumn">End column index</param>
        /// <param name="reuseMemory">
        ///     Set to true to avoid memory allocations
        ///     when possible. This might result on the shallow copies of some
        ///     elements. Default is false (default is to always provide a true,
        ///     deep copy of every element in the matrices, using more memory).
        /// </param>
        /// <remarks>
        ///     Routine adapted from Lutz Roeder's Mapack for .NET, September 2000.
        /// </remarks>
        [Obsolete("Please use Get instead.")]
        public static T[][] Submatrix<T>(this T[][] source, int[] rowIndexes,
            int startColumn, int endColumn, bool reuseMemory = false)
        {
            return Get(source, rowIndexes, startColumn, endColumn + 1, reuseMemory);
        }

        /// <summary>
        ///     Returns a sub matrix extracted from the current matrix.
        /// </summary>
        /// <param name="source">The matrix to return the submatrix from.</param>
        /// <param name="startRow">Starting row index</param>
        /// <param name="endRow">End row index</param>
        /// <param name="columnIndexes">Array of column indices</param>
        /// <remarks>
        ///     Routine adapted from Lutz Roeder's Mapack for .NET, September 2000.
        /// </remarks>
        [Obsolete("Please use Get instead.")]
        public static T[][] Submatrix<T>(this T[][] source, int startRow, int endRow, int[] columnIndexes)
        {
            return Get(source, startRow, endRow + 1, columnIndexes);
        }

        /// <summary>
        ///     Returns a subvector extracted from the current vector.
        /// </summary>
        /// <param name="source">The vector to return the subvector from.</param>
        /// <param name="indexes">Array of indices.</param>
        /// <param name="inPlace">
        ///     True to return the results in place, changing the
        ///     original <paramref name="source" /> vector; false otherwise.
        /// </param>
        [Obsolete("Please use Get instead.")]
        public static T[] Submatrix<T>(this T[] source, int[] indexes, bool inPlace = false)
        {
            return Get(source, indexes, inPlace);
        }

        /// <summary>
        ///     Returns a subvector extracted from the current vector.
        /// </summary>
        /// <param name="source">The vector to return the subvector from.</param>
        /// <param name="indexes">Array of indices.</param>
        [Obsolete("Please use Get instead.")]
        public static T[] Submatrix<T>(this T[] source, IList<int> indexes)
        {
            return Get(source, indexes);
        }

        /// <summary>
        ///     Returns a subvector extracted from the current vector.
        /// </summary>
        /// <param name="source">The vector to return the subvector from.</param>
        /// <param name="startRow">Starting index.</param>
        /// <param name="endRow">End index.</param>
        /// <remarks>
        ///     Routine adapted from Lutz Roeder's Mapack for .NET, September 2000.
        /// </remarks>
        [Obsolete("Please use Get instead.")]
        public static T[] Submatrix<T>(this T[] source, int startRow, int endRow)
        {
            return Get(source, startRow, endRow + 1);
        }

        /// <summary>
        ///     Returns a value extracted from the current vector.
        /// </summary>
        [Obsolete("Please use Get instead.")]
        public static T[] Submatrix<T>(this T[] source, int first)
        {
            return Get(source, 0, first);
        }

        /// <summary>
        ///     Returns a subvector extracted from the current vector.
        /// </summary>
        /// <param name="source">The vector to return the subvector from.</param>
        /// <param name="indexes">Array of indices.</param>
        [Obsolete("Please use Get instead.")]
        public static List<T> Submatrix<T>(this List<T> source, int[] indexes)
        {
            return Get(source, indexes);
        }

        /// <summary>
        ///     Obsolete. Please use <see cref="Classes.Separate{T}(T[], int[])" /> instead.
        /// </summary>
        [Obsolete("Please use Classes.Separate(T[], int[]) instead.")]
        public static T[][] Subgroups<T>(this T[] values, int[] groups)
        {
            var distinct = groups.Distinct().Sorted();
            var regroups = groups.Apply(x => Array.IndexOf(distinct, x));

            return values.Separate(regroups);
        }

        /// <summary>
        ///     Obsolete. Please use <see cref="Classes.Separate{T}(T[], int[], int)" /> instead.
        /// </summary>
        [Obsolete("Please use Classes.Separate(T[], int[], int) instead.")]
        public static T[][] Subgroups<T>(this T[] values, int[] groups, int classes)
        {
            return values.Separate(groups, classes);
        }
    }
}