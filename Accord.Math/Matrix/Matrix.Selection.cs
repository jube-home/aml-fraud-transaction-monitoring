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
using System.Linq;
using Accord.Math.Comparers;

namespace Accord.Math
{
    public static partial class Matrix
    {
        #region Remove

        /// <summary>Returns a sub matrix extracted from the current matrix.</summary>
        /// <param name="data">The matrix to return the submatrix from.</param>
        /// <param name="rowIndexes">Array of row indices. Pass null to select all indices.</param>
        /// <param name="columnIndexes">Array of column indices. Pass null to select all indices.</param>
        public static T[,] Remove<T>(this T[,] data, int[] rowIndexes, int[] columnIndexes)
        {
            if (rowIndexes == null)
            {
                rowIndexes = new int[0];
            }

            if (columnIndexes == null)
            {
                columnIndexes = new int[0];
            }


            var srcRows = data.GetLength(0);
            var srcCols = data.GetLength(1);

            var dstRows = srcRows - rowIndexes.Length;
            var dstCols = srcCols - columnIndexes.Length;


            var X = new T[dstRows, dstCols];

            for (int i = 0, y = 0; i < srcRows; i++)
            {
                if (!rowIndexes.Contains(i))
                {
                    for (int j = 0, x = 0; j < srcCols; j++)
                    {
                        if (!columnIndexes.Contains(j))
                        {
                            X[y, x] = data[i, j];
                            x++;
                        }
                    }

                    y++;
                }
            }

            return X;
        }

        #endregion

        /// <summary>
        ///     Removes all elements in the array that are equal to the given <paramref name="value" />.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values">The values.</param>
        /// <param name="value">The value to be removed.</param>
        public static T[] RemoveAll<T>(this T[] values, T value)
        {
            var result = new List<T>(values.Length);

            foreach (var v in values)
            {
                if (!Equals(v, value))
                {
                    result.Add(v);
                }
            }

            return result.ToArray();
        }

        /// <summary>
        ///     Performs an in-place re-ordering of elements in
        ///     a given array using the given vector of indices.
        /// </summary>
        /// <param name="values">The values to be ordered.</param>
        /// <param name="indices">The new index positions.</param>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static void Swap<T>(this T[] values, int[] indices)
        {
            var newValues = values.Get(indices);
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = newValues[i];
            }
        }

        /// <summary>
        ///     Swaps the contents of two object references.
        /// </summary>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static void Swap<T>(ref T a, ref T b)
        {
            var t = a;
            a = b;
            b = t;
        }

        /// <summary>
        ///     Swaps two elements in an array, given their indices.
        /// </summary>
        /// <param name="array">The array whose elements will be swapped.</param>
        /// <param name="a">The index of the first element to be swapped.</param>
        /// <param name="b">The index of the second element to be swapped.</param>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static void Swap<T>(this T[] array, int a, int b)
        {
            var aux = array[a];
            array[a] = array[b];
            array[b] = aux;
        }

        /// <summary>
        ///     Retrieves a list of the distinct values for each matrix column.
        /// </summary>
        /// <param name="values">The matrix.</param>
        /// <returns>
        ///     An array containing arrays of distinct values for
        ///     each column in the <paramref name="values" />.
        /// </returns>
        public static T[][] Distinct<T>(this T[,] values)
        {
            var rows = values.GetLength(0);
            var cols = values.GetLength(1);

            var sets = new HashSet<T>[cols];
            for (var i = 0; i < sets.Length; i++)
            {
                sets[i] = new HashSet<T>();
            }

            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < cols; j++)
                {
                    sets[j].Add(values[i, j]);
                }
            }

            var result = new T[cols][];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = sets[i].ToArray();
            }

            return result;
        }

        /// <summary>
        ///     Retrieves a list of the distinct values for each matrix column.
        /// </summary>
        /// <param name="values">The matrix.</param>
        /// <returns>
        ///     An array containing arrays of distinct values for
        ///     each column in the <paramref name="values" />.
        /// </returns>
        public static T[][] Distinct<T>(this T[][] values)
        {
            var rows = values.Length;
            var cols = values[0].Length;

            var sets = new HashSet<T>[cols];

            for (var i = 0; i < sets.Length; i++)
            {
                sets[i] = new HashSet<T>();
            }

            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < cols; j++)
                {
                    sets[j].Add(values[i][j]);
                }
            }

            var result = new T[cols][];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = sets[i].ToArray();
            }

            return result;
        }

        /// <summary>
        ///     Retrieves only distinct values contained in an array.
        /// </summary>
        /// <param name="values">The array.</param>
        /// <returns>An array containing only the distinct values in <paramref name="values" />.</returns>
        public static T[] Distinct<T>(this T[] values)
        {
            var set = new HashSet<T>(values);

            return set.ToArray();
        }

        /// <summary>
        ///     Retrieves only distinct values contained in an array.
        /// </summary>
        /// <param name="values">The array.</param>
        /// <param name="allowNulls">
        ///     Whether to allow null values in
        ///     the method's output. Default is true.
        /// </param>
        /// <returns>An array containing only the distinct values in <paramref name="values" />.</returns>
        public static T[] Distinct<T>(this T[] values, bool allowNulls)
            where T : class
        {
            var set = new HashSet<T>(values);

            if (!allowNulls)
            {
                set.Remove(null);
            }

            return set.ToArray();
        }

        /// <summary>
        ///     Retrieves only distinct values contained in an array.
        /// </summary>
        /// <param name="values">The array.</param>
        /// <param name="property">The property of the object used to determine distinct instances.</param>
        /// <returns>An array containing only the distinct values in <paramref name="values" />.</returns>
        public static T[] Distinct<T, TProperty>(this T[] values, Func<T, TProperty> property)
            where TProperty : IComparable<TProperty>
        {
            var comparer = new CustomComparer<T>((a, b) => property(a).CompareTo(property(b)));

            var set = new HashSet<T>(values, comparer);

            return set.ToArray();
        }

        /// <summary>
        ///     Gets the number of distinct values
        ///     present in each column of a matrix.
        /// </summary>
        public static int[] DistinctCount<T>(this T[,] matrix)
        {
            var distinct = matrix.Distinct();
            var counts = new int[distinct.Length];
            for (var i = 0; i < counts.Length; i++)
            {
                counts[i] = distinct[i].Length;
            }

            return counts;
        }

        /// <summary>
        ///     Gets the number of distinct values
        ///     present in each column of a matrix.
        /// </summary>
        public static int[] DistinctCount<T>(this T[][] matrix)
        {
            var distinct = matrix.Distinct();
            var counts = new int[distinct.Length];
            for (var i = 0; i < counts.Length; i++)
            {
                counts[i] = distinct[i].Length;
            }

            return counts;
        }

        /// <summary>
        ///     Gets the number of distinct values
        ///     present in each column of a matrix.
        /// </summary>
        public static int DistinctCount<T>(this T[] values)
        {
            return values.Distinct().Length;
        }

        /// <summary>
        ///     Sorts the columns of a matrix by sorting keys.
        /// </summary>
        /// <param name="keys">The key value for each column.</param>
        /// <param name="values">The matrix to be sorted.</param>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Math\Matrix\Matrix.Selection.cs" region="doc_matrix_sort" />
        /// </example>
        public static TValue[,] Sort<TKey, TValue>(TKey[] keys, TValue[,] values)
        {
            var indices = Math.Vector.Range(keys.Length);
            Array.Sort(keys.Copy(), indices);
            return values.Get(0, values.Rows(), indices);
        }

        /// <summary>
        ///     Sorts the columns of a matrix by sorting keys.
        /// </summary>
        /// <param name="keys">The key value for each column.</param>
        /// <param name="values">The matrix to be sorted.</param>
        /// <param name="comparer">The comparer to use.</param>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Math\Matrix\Matrix.Selection.cs" region="doc_matrix_sort" />
        /// </example>
        public static TValue[,] Sort<TKey, TValue>(TKey[] keys, TValue[,] values, IComparer<TKey> comparer)
        {
            var indices = Math.Vector.Range(keys.Length);
            Array.Sort(keys.Copy(), indices, comparer);
            return values.Get(0, values.Rows(), indices);
        }

        /// <summary>
        ///     Sorts the columns of a matrix by sorting keys.
        /// </summary>
        /// <param name="keys">The key value for each column.</param>
        /// <param name="values">The matrix to be sorted.</param>
        /// <param name="comparer">The comparer to use.</param>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Math\Matrix\Matrix.Selection.cs" region="doc_matrix_sort" />
        /// </example>
        public static TValue[][] Sort<TKey, TValue>(TKey[] keys, TValue[][] values, IComparer<TKey> comparer)
        {
            var indices = Math.Vector.Range(keys.Length);
            Array.Sort(keys.Copy(), indices, comparer);
            return values.Submatrix(null, indices);
        }

        /// <summary>
        ///     Sorts the columns of a matrix by sorting keys.
        /// </summary>
        /// <param name="keys">The key value for each column.</param>
        /// <param name="values">The matrix to be sorted.</param>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Math\Matrix\Matrix.Selection.cs" region="doc_matrix_sort" />
        /// </example>
        public static TValue[][] Sort<TKey, TValue>(TKey[] keys, TValue[][] values)
        {
            var indices = Math.Vector.Range(keys.Length);
            Array.Sort(keys.Copy(), indices);
            return values.Submatrix(null, indices);
        }

        /// <summary>
        ///     Returns a copy of an array in reversed order.
        /// </summary>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static T[] Reversed<T>(this T[] values)
        {
            var r = new T[values.Length];
            for (var i = 0; i < r.Length; i++)
            {
                r[i] = values[values.Length - i - 1];
            }

            return r;
        }

        /// <summary>
        ///     Returns a copy of an array in reversed order.
        /// </summary>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static T[] First<T>(this T[] values, int count)
        {
            var r = new T[count];
            for (var i = 0; i < r.Length; i++)
            {
                r[i] = values[i];
            }

            return r;
        }

        /// <summary>
        ///     Returns the last <paramref name="count" /> elements of an array.
        /// </summary>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static T[] Last<T>(this T[] values, int count)
        {
            var r = new T[count];
            for (var i = 0; i < r.Length; i++)
            {
                r[i] = values[values.Length - count + i];
            }

            return r;
        }

        /// <summary>
        ///     Retrieves the top <c>count</c> values of an array.
        /// </summary>
        public static int[] Top<T>(this T[] values, int count, bool inPlace = false)
            where T : IComparable<T>
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count",
                    "The number of elements to be selected must be positive.");
            }

            if (count == 0)
            {
                return new int[0];
            }

            var work = inPlace ? values : (T[])values.Clone();
            var idx = Math.Vector.Range(values.Length);
            if (count < values.Length)
            {
                work.NthElement(idx, 0, work.Length, count, false);
            }

            Accord.Sort.Insertion(work, idx, 0, count, false);
            return idx.First(count);
        }

        /// <summary>
        ///     Retrieves the bottom <c>count</c> values of an array.
        /// </summary>
        public static int[] Bottom<T>(this T[] values, int count, bool inPlace = false)
            where T : IComparable<T>
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count",
                    "The number of elements to be selected must be positive.");
            }

            if (count == 0)
            {
                return new int[0];
            }

            var work = inPlace ? values : (T[])values.Clone();
            var idx = Math.Vector.Range(values.Length);
            if (count < values.Length)
            {
                work.NthElement(idx, 0, work.Length, count);
            }

            Accord.Sort.Insertion(work, idx, 0, count);
            return idx.First(count);
        }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete("Please use Accord.Sort.Partition instead.")]
        public static int Partition<T, TValue>(this T[] list, TValue[] keys, int left, int right, bool asc = true)
            where T : IComparable<T>
        {
            return Accord.Sort.Partition(list, left, right, asc);
        }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete("Please use Accord.Sort.Partition instead.")]
        public static int Partition<T>(this T[] list, int left, int right, bool asc = true)
            where T : IComparable<T>
        {
            return Accord.Sort.Partition(list, left, right, asc);
        }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete("Please use Accord.Sort.Partition instead.")]
        public static int Partition<TKey, TValue>(this TKey[] list, TValue[] keys, int left, int right,
            Func<TKey, TKey, int> compare, bool asc = true)
        {
            return Accord.Sort.Partition(list, keys, left, right, compare, asc);
        }

        /// <summary>
        ///     Obsolete.
        /// </summary>
        [Obsolete("Please use Accord.Sort.Partition instead.")]
        public static int Partition<T>(this T[] list, int left, int right, Func<T, T, int> compare, bool asc = true)
        {
            return Accord.Sort.Partition(list, left, right, compare, asc);
        }

        private static T cast<T>(this object value)
        {
            return (T)System.Convert.ChangeType(value, typeof(T));
        }

        #region Row and column getters and setters

        /// <summary>
        ///     Gets a column vector from a matrix.
        /// </summary>
        public static T[,] GetPlane<T>(this T[,,] m, int index, T[,] result = null)
        {
            var rows = m.Rows();
            var cols = m.Columns();
            var depth = m.Depth();

            if (result == null)
            {
                result = new T[rows, cols];
            }

            index = Matrix.index(index, depth);
            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < cols; j++)
                {
                    result[i, j] = m[i, j, index];
                }
            }

            return result;
        }

        /// <summary>
        ///     Gets a column vector from a matrix.
        /// </summary>
        public static T[] GetColumn<T>(this T[,] m, int index, T[] result = null)
        {
            if (result == null)
            {
                result = new T[m.Rows()];
            }

            index = Matrix.index(index, m.Columns());
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = m[i, index];
            }

            return result;
        }

        /// <summary>
        ///     Gets a column vector from a matrix.
        /// </summary>
        public static T[] GetColumn<T>(this T[][] m, int index, T[] result = null)
        {
            if (result == null)
            {
                result = new T[m.Length];
            }

            index = Matrix.index(index, m.Columns());
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = m[i][index];
            }

            return result;
        }

        /// <summary>
        ///     Gets a column vector from a matrix.
        /// </summary>
        public static T[][] GetColumns<T>(this T[][] m, params int[] index)
        {
            return GetColumns(m, index, null);
        }

        /// <summary>
        ///     Gets a column vector from a matrix.
        /// </summary>
        public static T[][] GetColumns<T>(this T[][] m, int[] index, T[][] result = null)
        {
            if (result == null)
            {
                result = new T[m.Length][];
            }

            for (var i = 0; i < result.Length; i++)
            {
                if (result[i] == null)
                {
                    result[i] = new T[index.Length];
                }

                for (var j = 0; j < index.Length; j++)
                {
                    result[i][j] = m[i][index[j]];
                }
            }

            return result;
        }

        /// <summary>
        ///     Gets a row vector from a matrix.
        /// </summary>
        public static T[] GetRow<T>(this T[][] m, int index, T[] result = null)
        {
            index = Matrix.index(index, m.Rows());

            if (result == null)
            {
                return (T[])m[index].Clone();
            }

            m[index].CopyTo(result, 0);
            return result;
        }

        /// <summary>
        ///     Gets a row vector from a matrix.
        /// </summary>
        public static T[] GetRow<T>(this T[,] m, int index, T[] result = null)
        {
            if (result == null)
            {
                result = new T[m.GetLength(1)];
            }

            index = Matrix.index(index, m.Rows());
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = m[index, i];
            }

            return result;
        }

        /// <summary>
        ///     Gets a row vector from a matrix.
        /// </summary>
        public static T[][] GetRows<T>(this T[][] m, params int[] index)
        {
            return GetRows(m, index, null);
        }

        /// <summary>
        ///     Gets a row vector from a matrix.
        /// </summary>
        public static T[][] GetRows<T>(this T[][] m, int[] index, T[][] result)
        {
            if (result == null)
            {
                result = new T[index.Length][];
                for (var i = 0; i < index.Length; i++)
                {
                    result[i] = (T[])m[index[i]].Clone();
                }
            }
            else
            {
                for (var i = 0; i < index.Length; i++)
                {
                    m[index[i]].CopyTo(result[i], 0);
                }
            }

            return result;
        }

        /// <summary>
        ///     Gets a column vector from a matrix.
        /// </summary>
        public static T[,] GetColumns<T>(this T[,] m, params int[] index)
        {
            return GetColumns(m, index, null);
        }

        /// <summary>
        ///     Gets a column vector from a matrix.
        /// </summary>
        public static T[,] GetColumns<T>(this T[,] m, int[] index, T[,] result = null)
        {
            var rows = m.GetLength(0);

            if (result == null)
            {
                result = new T[rows, index.Length];
            }

            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < index.Length; j++)
                {
                    result[i, j] = m[i, index[j]];
                }
            }

            return result;
        }

        /// <summary>
        ///     Stores a column vector into the given column position of the matrix.
        /// </summary>
        public static T[,] SetColumn<T>(this T[,] m, int index, T[] column)
        {
            index = Matrix.index(index, m.Columns());
            for (var i = 0; i < column.Length; i++)
            {
                m[i, index] = column[i];
            }

            return m;
        }

        /// <summary>
        ///     Stores a column vector into the given column position of the matrix.
        /// </summary>
        public static T[][] SetColumn<T>(this T[][] m, int index, T[] column)
        {
            index = Matrix.index(index, m.Columns());
            for (var i = 0; i < column.Length; i++)
            {
                m[i][index] = column[i];
            }

            return m;
        }

        /// <summary>
        ///     Stores a column vector into the given column position of the matrix.
        /// </summary>
        public static T[][] SetColumn<T>(this T[][] m, int index, T value)
        {
            index = Matrix.index(index, m.Columns());
            for (var i = 0; i < m.Length; i++)
            {
                m[i][index] = value;
            }

            return m;
        }

        /// <summary>
        ///     Stores a row vector into the given row position of the matrix.
        /// </summary>
        public static T[,] SetRow<T>(this T[,] m, int index, T[] row)
        {
            index = Matrix.index(index, m.Rows());
            for (var i = 0; i < row.Length; i++)
            {
                m[index, i] = row[i];
            }

            return m;
        }

        /// <summary>
        ///     Stores a row vector into the given row position of the matrix.
        /// </summary>
        public static T[][] SetRow<T>(this T[][] m, int index, T[] row)
        {
            index = Matrix.index(index, m.Rows());
            for (var i = 0; i < row.Length; i++)
            {
                m[index][i] = row[i];
            }

            return m;
        }

        /// <summary>
        ///     Stores a row vector into the given row position of the matrix.
        /// </summary>
        public static T[][] SetRow<T>(this T[][] m, int index, T value)
        {
            index = Matrix.index(index, m.Rows());
            for (var i = 0; i < m[index].Length; i++)
            {
                m[index][i] = value;
            }

            return m;
        }

        #endregion

        #region Row and column insertion and removal

        /// <summary>
        ///     Returns a new matrix without one of its columns.
        /// </summary>
        public static T[][] RemoveColumn<T>(this T[][] matrix, int index)
        {
            var X = new T[matrix.Length][];

            for (var i = 0; i < matrix.Length; i++)
            {
                X[i] = new T[matrix[i].Length - 1];
                for (var j = 0; j < index; j++)
                {
                    X[i][j] = matrix[i][j];
                }

                for (var j = index + 1; j < matrix[i].Length; j++)
                {
                    X[i][j - 1] = matrix[i][j];
                }
            }

            return X;
        }

        /// <summary>
        ///     Returns a new matrix without one of its columns.
        /// </summary>
        public static T[,] RemoveColumn<T>(this T[,] matrix, int index)
        {
            var rows = matrix.GetLength(0);
            var cols = matrix.GetLength(1);
            var newCols = cols - 1;

            var X = new T[rows, newCols];

            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < index; j++)
                {
                    X[i, j] = matrix[i, j];
                }

                for (var j = index + 1; j < cols; j++)
                {
                    X[i, j - 1] = matrix[i, j];
                }
            }

            return X;
        }

        /// <summary>
        ///     Returns a new matrix with a new column vector inserted at the end of the original matrix.
        /// </summary>
        public static T[,] InsertColumn<T>(this T[,] matrix)
        {
            return InsertColumn(matrix, new T[matrix.Length], matrix.GetLength(1));
        }

        /// <summary>
        ///     Returns a new matrix with a new column vector inserted at the end of the original matrix.
        /// </summary>
        public static T[][] InsertColumn<T>(this T[][] matrix)
        {
            return InsertColumn(matrix, new T[matrix.Length], matrix[0].Length);
        }

        /// <summary>
        ///     Returns a new matrix with a given column vector inserted at the end of the original matrix.
        /// </summary>
        public static T[,] InsertColumn<T, TSource>(this T[,] matrix, TSource[] column)
        {
            return InsertColumn(matrix, column, matrix.GetLength(1));
        }

        /// <summary>
        ///     Returns a new matrix with a given column vector inserted at the end of the original matrix.
        /// </summary>
        public static T[,] InsertColumn<T, TSource>(this T[,] matrix, TSource value)
        {
            return InsertColumn(matrix, value, matrix.GetLength(1));
        }

        /// <summary>
        ///     Returns a new matrix with a given column vector inserted at the end of the original matrix.
        /// </summary>
        public static T[][] InsertColumn<T, TSource>(this T[][] matrix, TSource[] column)
        {
            return InsertColumn(matrix, column, matrix[0].Length);
        }

        /// <summary>
        ///     Returns a new matrix with a given column vector inserted at the end of the original matrix.
        /// </summary>
        public static T[][] InsertColumn<T, TSource>(this T[][] matrix, TSource value)
        {
            return InsertColumn(matrix, value, matrix[0].Length);
        }

        /// <summary>
        ///     Returns a new matrix with a given column vector inserted at the end of the original matrix.
        /// </summary>
        public static T[][] InsertRow<T, TSource>(this T[][] matrix, TSource value)
        {
            return InsertRow(matrix, value, matrix.Length);
        }

        /// <summary>
        ///     Returns a new matrix with a given row vector inserted at the end of the original matrix.
        /// </summary>
        public static T[,] InsertRow<T, TSource>(this T[,] matrix, TSource[] row)
        {
            return InsertRow(matrix, row, matrix.GetLength(0));
        }

        /// <summary>
        ///     Returns a new matrix with a given row vector inserted at the end of the original matrix.
        /// </summary>
        public static T[][] InsertRow<T, TSource>(this T[][] matrix, TSource[] row)
        {
            return InsertRow(matrix, row, matrix.Length);
        }

        /// <summary>
        ///     Returns a new matrix with a new row vector inserted at the end of the original matrix.
        /// </summary>
        public static T[,] InsertRow<T>(this T[,] matrix)
        {
            return InsertRow(matrix, new T[matrix.GetLength(1)], matrix.GetLength(0));
        }

        /// <summary>
        ///     Returns a new matrix with a new row vector inserted at the end of the original matrix.
        /// </summary>
        public static T[][] InsertRow<T>(this T[][] matrix)
        {
            return InsertRow(matrix, new T[matrix[0].Length], matrix.Length);
        }

        /// <summary>
        ///     Returns a new matrix with a given column vector inserted at a given index.
        /// </summary>
        public static T[,] InsertColumn<T, TSource>(this T[,] matrix, TSource[] column, int index)
        {
            if (matrix == null)
            {
                throw new ArgumentNullException("matrix");
            }

            if (column == null)
            {
                throw new ArgumentNullException("column");
            }

            var rows = matrix.GetLength(0);
            var cols = matrix.GetLength(1);

            var maxRows = System.Math.Max(rows, column.Length);

            var X = new T[maxRows, cols + 1];

            // Copy original matrix
            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < index; j++)
                {
                    X[i, j] = matrix[i, j];
                }

                for (var j = index; j < cols; j++)
                {
                    X[i, j + 1] = matrix[i, j];
                }
            }

            // Copy additional column
            for (var i = 0; i < column.Length; i++)
            {
                X[i, index] = cast<T>(column[i]);
            }

            return X;
        }

        /// <summary>
        ///     Returns a new matrix with a given column vector inserted at a given index.
        /// </summary>
        public static T[,] InsertColumn<T, TSource>(this T[,] matrix, TSource value, int index)
        {
            if (matrix == null)
            {
                throw new ArgumentNullException("matrix");
            }

            var rows = matrix.GetLength(0);
            var cols = matrix.GetLength(1);

            var X = new T[rows, cols + 1];

            // Copy original matrix
            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < index; j++)
                {
                    X[i, j] = matrix[i, j];
                }

                for (var j = index; j < cols; j++)
                {
                    X[i, j + 1] = matrix[i, j];
                }
            }

            // Copy additional column
            for (var i = 0; i < rows; i++)
            {
                X[i, index] = cast<T>(value);
            }

            return X;
        }

        /// <summary>
        ///     Returns a new matrix with a given column vector inserted at a given index.
        /// </summary>
        public static T[][] InsertColumn<T, TSource>(this T[][] matrix, TSource[] column, int index)
        {
            if (matrix == null)
            {
                throw new ArgumentNullException("matrix");
            }

            if (column == null)
            {
                throw new ArgumentNullException("column");
            }

            var rows = matrix.Length;
            var cols = matrix[0].Length;

            var maxRows = System.Math.Max(rows, column.Length);

            var X = new T[maxRows][];
            for (var i = 0; i < X.Length; i++)
            {
                X[i] = new T[cols + 1];
            }

            // Copy original matrix
            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < index; j++)
                {
                    X[i][j] = matrix[i][j];
                }

                for (var j = index; j < cols; j++)
                {
                    X[i][j + 1] = matrix[i][j];
                }
            }

            // Copy additional column
            for (var i = 0; i < column.Length; i++)
            {
                X[i][index] = cast<T>(column[i]);
            }

            return X;
        }

        /// <summary>
        ///     Returns a new matrix with a given column vector inserted at a given index.
        /// </summary>
        public static T[][] InsertColumn<T, TSource>(this T[][] matrix, TSource value, int index)
        {
            if (matrix == null)
            {
                throw new ArgumentNullException("matrix");
            }

            var rows = matrix.Length;
            var cols = matrix[0].Length;

            var X = new T[rows][];
            for (var i = 0; i < X.Length; i++)
            {
                X[i] = new T[cols + 1];
            }

            // Copy original matrix
            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < index; j++)
                {
                    X[i][j] = matrix[i][j];
                }

                for (var j = index; j < cols; j++)
                {
                    X[i][j + 1] = matrix[i][j];
                }
            }

            // Copy additional column
            for (var i = 0; i < rows; i++)
            {
                X[i][index] = cast<T>(value);
            }

            return X;
        }

        /// <summary>
        ///     Returns a new matrix with a given column vector inserted at a given index.
        /// </summary>
        public static T[][] InsertRow<T, TSource>(this T[][] matrix, TSource value, int index)
        {
            if (matrix == null)
            {
                throw new ArgumentNullException("matrix");
            }

            var rows = matrix.Length;
            var cols = matrix[0].Length;

            var X = new T[rows + 1][];
            for (var i = 0; i < X.Length; i++)
            {
                X[i] = new T[cols];
            }

            // Copy original matrix
            for (var i = 0; i < cols; i++)
            {
                for (var j = 0; j < rows; j++)
                {
                    X[j][i] = matrix[j][i];
                }

                for (var j = index; j < rows; j++)
                {
                    X[j + 1][i] = matrix[j][i];
                }
            }

            // Copy additional column
            for (var i = 0; i < cols; i++)
            {
                X[index][i] = cast<T>(value);
            }

            return X;
        }

        /// <summary>
        ///     Returns a new matrix with a given row vector inserted at a given index.
        /// </summary>
        public static T[,] InsertRow<T, TSource>(this T[,] matrix, TSource[] row, int index)
        {
            if (matrix == null)
            {
                throw new ArgumentNullException("matrix");
            }

            if (row == null)
            {
                throw new ArgumentNullException("row");
            }

            var rows = matrix.GetLength(0);
            var cols = matrix.GetLength(1);

            var maxCols = System.Math.Max(cols, row.Length);

            var X = new T[rows + 1, maxCols];

            // Copy original matrix
            for (var i = 0; i < cols; i++)
            {
                for (var j = 0; j < rows; j++)
                {
                    X[j, i] = matrix[j, i];
                }

                for (var j = index; j < rows; j++)
                {
                    X[j + 1, i] = matrix[j, i];
                }
            }

            // Copy additional column
            for (var i = 0; i < row.Length; i++)
            {
                X[index, i] = cast<T>(row[i]);
            }

            return X;
        }

        /// <summary>
        ///     Returns a new matrix with a given row vector inserted at a given index.
        /// </summary>
        public static T[,] InsertRow<T, TSource>(this T[,] matrix, TSource value, int index)
        {
            if (matrix == null)
            {
                throw new ArgumentNullException("matrix");
            }

            var rows = matrix.GetLength(0);
            var cols = matrix.GetLength(1);

            var X = new T[rows + 1, cols];

            // Copy original matrix
            for (var i = 0; i < cols; i++)
            {
                for (var j = 0; j < rows; j++)
                {
                    X[j, i] = matrix[j, i];
                }

                for (var j = index; j < rows; j++)
                {
                    X[j + 1, i] = matrix[j, i];
                }
            }

            // Copy additional column
            for (var i = 0; i < cols; i++)
            {
                X[index, i] = cast<T>(value);
            }

            return X;
        }

        /// <summary>
        ///     Returns a new matrix with a given row vector inserted at a given index.
        /// </summary>
        public static T[][] InsertRow<T, TSource>(this T[][] matrix, TSource[] row, int index)
        {
            if (matrix == null)
            {
                throw new ArgumentNullException("matrix");
            }

            if (row == null)
            {
                throw new ArgumentNullException("row");
            }

            var rows = matrix.Length;
            var cols = matrix[0].Length;

            var maxCols = System.Math.Max(cols, row.Length);

            var X = new T[rows + 1][];
            for (var i = 0; i < X.Length; i++)
            {
                X[i] = new T[maxCols];
            }

            // Copy original matrix
            for (var i = 0; i < cols; i++)
            {
                for (var j = 0; j < rows; j++)
                {
                    X[j][i] = matrix[j][i];
                }

                for (var j = index; j < rows; j++)
                {
                    X[j + 1][i] = matrix[j][i];
                }
            }

            // Copy additional column
            for (var i = 0; i < row.Length; i++)
            {
                X[index][i] = cast<T>(row[i]);
            }

            return X;
        }

        /// <summary>
        ///     Returns a new matrix without one of its rows.
        /// </summary>
        public static T[,] RemoveRow<T>(this T[,] matrix, int index)
        {
            if (matrix == null)
            {
                throw new ArgumentNullException("matrix");
            }

            var rows = matrix.GetLength(0);
            var cols = matrix.GetLength(1);
            var newRows = rows - 1;

            var X = new T[newRows, cols];

            for (var i = 0; i < cols; i++)
            {
                for (var j = 0; j < index; j++)
                {
                    X[j, i] = matrix[j, i];
                }

                for (var j = index + 1; j < rows; j++)
                {
                    X[j - 1, i] = matrix[j, i];
                }
            }

            return X;
        }

        /// <summary>
        ///     Removes an element from a vector.
        /// </summary>
        public static T[] RemoveAt<T>(this T[] array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            var r = new T[array.Length - 1];
            for (var i = 0; i < index; i++)
            {
                r[i] = array[i];
            }

            for (var i = index; i < r.Length; i++)
            {
                r[i] = array[i + 1];
            }

            return r;
        }

        #endregion

        #region Element search

        /// <summary>
        ///     Gets the number of elements matching a certain criteria.
        /// </summary>
        /// <typeparam name="T">The type of the array.</typeparam>
        /// <param name="data">The array to search inside.</param>
        /// <param name="func">The search criteria.</param>
        public static int Count<T>(this T[] data, Func<T, bool> func)
        {
            var count = 0;
            for (var i = 0; i < data.Length; i++)
            {
                if (func(data[i]))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        ///     Gets the indices of the first element matching a certain criteria.
        /// </summary>
        /// <typeparam name="T">The type of the array.</typeparam>
        /// <param name="data">The array to search inside.</param>
        /// <param name="func">The search criteria.</param>
        public static int First<T>(this T[] data, Func<T, bool> func)
        {
            return Find(data, func, true)[0];
        }

        /// <summary>
        ///     Gets the indices of the first element matching a certain criteria, or null if the element could not be found.
        /// </summary>
        /// <typeparam name="T">The type of the array.</typeparam>
        /// <param name="data">The array to search inside.</param>
        /// <param name="func">The search criteria.</param>
        public static int? FirstOrNull<T>(this T[] data, Func<T, bool> func)
        {
            var r = Find(data, func, true);
            if (r.Length == 0)
            {
                return null;
            }

            return r[0];
        }

        /// <summary>
        ///     Searches for the specified value and returns the index of the first occurrence within the array.
        /// </summary>
        /// <typeparam name="T">The type of the array.</typeparam>
        /// <param name="data">The array to search.</param>
        /// <param name="value">The value to be searched.</param>
        /// <returns>The index of the searched value within the array, or -1 if not found.</returns>
        public static int IndexOf<T>(this T[] data, T value)
        {
            return Array.IndexOf(data, value);
        }

        /// <summary>
        ///     Gets the indices of all elements matching a certain criteria.
        /// </summary>
        /// <typeparam name="T">The type of the array.</typeparam>
        /// <param name="data">The array to search inside.</param>
        /// <param name="func">The search criteria.</param>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static int[] Find<T>(this T[] data, Func<T, bool> func)
        {
            var idx = new List<int>();

            for (var i = 0; i < data.Length; i++)
            {
                if (func(data[i]))
                {
                    idx.Add(i);
                }
            }

            return idx.ToArray();
        }

        /// <summary>
        ///     Gets the indices of all elements matching a certain criteria.
        /// </summary>
        /// <typeparam name="T">The type of the array.</typeparam>
        /// <param name="data">The array to search inside.</param>
        /// <param name="func">The search criteria.</param>
        /// <param name="firstOnly">
        ///     Set to true to stop when the first element is
        ///     found, set to false to get all elements.
        /// </param>
        public static int[] Find<T>(this T[] data, Func<T, bool> func, bool firstOnly)
        {
            var idx = new List<int>();
            for (var i = 0; i < data.Length; i++)
            {
                if (func(data[i]))
                {
                    if (firstOnly)
                    {
                        return new[] { i };
                    }

                    idx.Add(i);
                }
            }

            return idx.ToArray();
        }

        /// <summary>
        ///     Gets the indices of all elements matching a certain criteria.
        /// </summary>
        /// <typeparam name="T">The type of the array.</typeparam>
        /// <param name="data">The array to search inside.</param>
        /// <param name="func">The search criteria.</param>
        public static int[][] Find<T>(this T[,] data, Func<T, bool> func)
        {
            return Find(data, func, false);
        }

        /// <summary>
        ///     Gets the indices of all elements matching a certain criteria.
        /// </summary>
        /// <typeparam name="T">The type of the array.</typeparam>
        /// <param name="data">The array to search inside.</param>
        /// <param name="func">The search criteria.</param>
        /// <param name="firstOnly">
        ///     Set to true to stop when the first element is
        ///     found, set to false to get all elements.
        /// </param>
        public static int[][] Find<T>(this T[,] data, Func<T, bool> func, bool firstOnly)
        {
            var idx = new List<int[]>();
            for (var i = 0; i < data.GetLength(0); i++)
            {
                for (var j = 0; j < data.GetLength(1); j++)
                {
                    if (func(data[i, j]))
                    {
                        if (firstOnly)
                        {
                            return new[] { new[] { i, j } };
                        }

                        idx.Add(new[] { i, j });
                    }
                }
            }

            return idx.ToArray();
        }

        #endregion
    }
}