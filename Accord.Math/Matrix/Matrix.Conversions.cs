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
using System.Runtime.InteropServices;

namespace Accord.Math
{
#if !NETSTANDARD1_4
    using System.Data;
#endif

    public static partial class Matrix
    {
        /// <summary>
        ///     Converts a jagged-array into a multidimensional array.
        /// </summary>
        public static Array DeepToMatrix(this Array array)
        {
            var shape = array.GetLength();
            var totalLength = array.GetTotalLength();
            var elementType = array.GetInnerMostType();

            var flat = array.DeepFlatten();

            var result = Array.CreateInstance(elementType, shape);
            Buffer.BlockCopy(flat, 0, result, 0, totalLength);
            return result;
        }

        /// <summary>
        ///     Converts a jagged-array into a multidimensional array.
        /// </summary>
        public static T[,] ToMatrix<T>(this T[][] array, bool transpose = false)
        {
            var rows = array.Length;
            if (rows == 0)
            {
                return new T[0, rows];
            }

            var cols = array[0].Length;

            T[,] m;

            if (transpose)
            {
                m = new T[cols, rows];
                for (var i = 0; i < rows; i++)
                {
                    for (var j = 0; j < cols; j++)
                    {
                        m[j, i] = array[i][j];
                    }
                }
            }
            else
            {
                m = new T[rows, cols];
                for (var i = 0; i < rows; i++)
                {
                    for (var j = 0; j < cols; j++)
                    {
                        m[i, j] = array[i][j];
                    }
                }
            }

            return m;
        }

        /// <summary>
        ///     Converts an array into a multidimensional array.
        /// </summary>
        public static T[][] ToJagged<T>(this T[] array, bool asColumnVector = true)
        {
            if (asColumnVector)
            {
                var m = new T[array.Length][];
                for (var i = 0; i < array.Length; i++)
                {
                    m[i] = new[] { array[i] };
                }

                return m;
            }

            return new[] { array };
        }

        /// <summary>
        ///     Converts an array into a multidimensional array.
        /// </summary>
        public static T[,] ToMatrix<T>(this T[] array, bool asColumnVector = false)
        {
            if (asColumnVector)
            {
                var m = new T[array.Length, 1];
                for (var i = 0; i < array.Length; i++)
                {
                    m[i, 0] = array[i];
                }

                return m;
            }
            else
            {
                var m = new T[1, array.Length];
                for (var i = 0; i < array.Length; i++)
                {
                    m[0, i] = array[i];
                }

                return m;
            }
        }

        /// <summary>
        ///     Converts a multidimensional array into a jagged array.
        /// </summary>
        public static T[][] ToJagged<T>(this T[,] matrix, bool transpose = false)
        {
            T[][] array;

            if (transpose)
            {
                var cols = matrix.GetLength(1);

                array = new T[cols][];
                for (var i = 0; i < cols; i++)
                {
                    array[i] = matrix.GetColumn(i);
                }
            }
            else
            {
                var rows = matrix.GetLength(0);

                array = new T[rows][];
                for (var i = 0; i < rows; i++)
                {
                    array[i] = matrix.GetRow(i);
                }
            }

            return array;
        }

        /// <summary>
        ///     Converts a multidimensional array into a jagged array.
        /// </summary>
        public static T[][][] ToJagged<T>(this T[,,] matrix)
        {
            var rows = matrix.GetLength(0);
            var cols = matrix.GetLength(1);
            var depth = matrix.GetLength(2);

            var array = new T[rows][][];
            for (var i = 0; i < rows; i++)
            {
                var row = array[i] = new T[cols][];
                for (var j = 0; j < row.Length; j++)
                {
                    var plane = row[j] = new T[depth];
                    for (var k = 0; k < plane.Length; k++)
                    {
                        plane[k] = matrix[i, j, k];
                    }
                }
            }

            return array;
        }

        /// <summary>
        ///     Creates a vector containing every index that can be used to
        ///     address a given <paramref name="array" />, in order.
        /// </summary>
        /// <param name="array">The array whose indices will be returned.</param>
        /// <param name="deep">
        ///     Pass true to retrieve all dimensions of the array,
        ///     even if it contains nested arrays (as in jagged matrices).
        /// </param>
        /// <param name="max">
        ///     Bases computations on the maximum length possible for
        ///     each dimension (in case the jagged matrices has different lengths).
        /// </param>
        /// <param name="order">
        ///     The direction to access the matrix. Pass 1 to read the
        ///     matrix in row-major order. Pass 0 to read in column-major order. Default is
        ///     1 (row-major, c-style order).
        /// </param>
        /// <returns>
        ///     An enumerable object that can be used to iterate over all
        ///     positions of the given <paramref name="array">System.Array</paramref>.
        /// </returns>
        /// <example>
        ///     <code>
        ///   double[,] a = 
        ///   { 
        ///      { 5.3, 2.3 },
        ///      { 4.2, 9.2 }
        ///   };
        ///   
        ///   foreach (int[] idx in a.GetIndices())
        ///   {
        ///      // Get the current element
        ///      double e = (double)a.GetValue(idx);
        ///   }
        /// </code>
        /// </example>
        /// <seealso cref="Vector.GetIndices{T}(T[])" />
        public static IEnumerable<int[]> GetIndices(this Array array, bool deep = false, bool max = false,
            MatrixOrder order = MatrixOrder.Default)
        {
            return array.GetLength(deep, max)
                .Sequences(firstColumnChangesFaster: order == MatrixOrder.FortranColumnMajor);
        }

        #region Type conversions

        /// <summary>
        ///     Converts the values of a vector using the given converter expression.
        /// </summary>
        /// <typeparam name="TInput">The type of the input.</typeparam>
        /// <typeparam name="TOutput">The type of the output.</typeparam>
        /// <param name="vector">The vector to be converted.</param>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Math\Matrix\Matrix.Conversion.cs" region="doc_convert_matrix" />
        ///     <code source="Unit Tests\Accord.Tests.Math\Matrix\Matrix.Conversion.cs" region="doc_convert_jagged" />
        /// </example>
        public static TOutput[] Convert<TInput, TOutput>(this TInput[] vector)
        {
            return Convert(vector, x => (TOutput)System.Convert.ChangeType(x, typeof(TOutput)));
        }

        /// <summary>
        ///     Converts the values of a matrix using the default converter.
        /// </summary>
        /// <typeparam name="TInput">The type of the input.</typeparam>
        /// <typeparam name="TOutput">The type of the output.</typeparam>
        /// <param name="matrix">The matrix to be converted.</param>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Math\Matrix\Matrix.Conversion.cs" region="doc_convert_matrix" />
        ///     <code source="Unit Tests\Accord.Tests.Math\Matrix\Matrix.Conversion.cs" region="doc_convert_jagged" />
        /// </example>
        public static TOutput[,] Convert<TInput, TOutput>(this TInput[,] matrix)
        {
            return Convert(matrix, x => (TOutput)System.Convert.ChangeType(x, typeof(TOutput)));
        }

        /// <summary>
        ///     Converts the values of a matrix using the default converter.
        /// </summary>
        /// <typeparam name="TInput">The type of the input.</typeparam>
        /// <typeparam name="TOutput">The type of the output.</typeparam>
        /// <param name="matrix">The matrix to be converted.</param>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Math\Matrix\Matrix.Conversion.cs" region="doc_convert_matrix" />
        ///     <code source="Unit Tests\Accord.Tests.Math\Matrix\Matrix.Conversion.cs" region="doc_convert_jagged" />
        /// </example>
        public static TOutput[,] Convert<TInput, TOutput>(TInput[][] matrix)
        {
            return Convert<TInput, TOutput>(matrix, x => (TOutput)System.Convert.ChangeType(x, typeof(TOutput)));
        }

        /// <summary>
        ///     Converts the values of a vector using the given converter expression.
        /// </summary>
        /// <typeparam name="TInput">The type of the input.</typeparam>
        /// <typeparam name="TOutput">The type of the output.</typeparam>
        /// <param name="vector">The vector to be converted.</param>
        /// <param name="converter">The converter function.</param>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Math\Matrix\Matrix.Conversion.cs" region="doc_convert_matrix" />
        ///     <code source="Unit Tests\Accord.Tests.Math\Matrix\Matrix.Conversion.cs" region="doc_convert_jagged" />
        /// </example>
        public static TOutput[] Convert<TInput, TOutput>(this TInput[] vector,
#if !NETSTANDARD1_4
            Converter<TInput, TOutput>
#else
            Func<TInput, TOutput>
#endif
                converter)
        {
#if !NETSTANDARD1_4
            return Array.ConvertAll(vector, converter);
#else
            return vector.Apply(converter);
#endif
        }

        /// <summary>
        ///     Converts the values of a matrix using the given converter expression.
        /// </summary>
        /// <typeparam name="TInput">The type of the input.</typeparam>
        /// <typeparam name="TOutput">The type of the output.</typeparam>
        /// <param name="matrix">The matrix to be converted.</param>
        /// <param name="converter">The converter function.</param>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Math\Matrix\Matrix.Conversion.cs" region="doc_convert_matrix" />
        ///     <code source="Unit Tests\Accord.Tests.Math\Matrix\Matrix.Conversion.cs" region="doc_convert_jagged" />
        /// </example>
        public static TOutput[,] Convert<TInput, TOutput>(this TInput[][] matrix,
#if !NETSTANDARD1_4
            Converter<TInput, TOutput>
#else
            Func<TInput, TOutput>
#endif
                converter)
        {
            var result = CreateAs<TInput, TOutput>(matrix);

            for (var i = 0; i < matrix.Length; i++)
            {
                for (var j = 0; j < matrix[i].Length; j++)
                {
                    result[i, j] = converter(matrix[i][j]);
                }
            }

            return result;
        }

        /// <summary>
        ///     Converts the values of a matrix using the given converter expression.
        /// </summary>
        /// <typeparam name="TInput">The type of the input.</typeparam>
        /// <typeparam name="TOutput">The type of the output.</typeparam>
        /// <param name="matrix">The vector to be converted.</param>
        /// <param name="converter">The converter function.</param>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Math\Matrix\Matrix.Conversion.cs" region="doc_convert_matrix" />
        ///     <code source="Unit Tests\Accord.Tests.Math\Matrix\Matrix.Conversion.cs" region="doc_convert_jagged" />
        /// </example>
        public static TOutput[,] Convert<TInput, TOutput>(this TInput[,] matrix,
#if !NETSTANDARD1_4
            Converter<TInput, TOutput>
#else
            Func<TInput, TOutput>
#endif
                converter)
        {
            var rows = matrix.GetLength(0);
            var cols = matrix.GetLength(1);

            var result = new TOutput[rows, cols];
            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < cols; j++)
                {
                    result[i, j] = converter(matrix[i, j]);
                }
            }

            return result;
        }

        /// <summary>
        ///     Converts an object into another type, irrespective of whether
        ///     the conversion can be done at compile time or not. This can be
        ///     used to convert generic types to numeric types during runtime.
        /// </summary>
        /// <typeparam name="TOutput">The type of the output.</typeparam>
        /// <param name="array">The vector or array to be converted.</param>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Math\Matrix\Matrix.Conversion.cs" region="doc_convert_matrix" />
        ///     <code source="Unit Tests\Accord.Tests.Math\Matrix\Matrix.Conversion.cs" region="doc_convert_jagged" />
        /// </example>
        public static TOutput To<TOutput>(this Array array)
        {
            return To(array, typeof(TOutput)).To<TOutput>();
        }

        /// <summary>
        ///     Converts an object into another type, irrespective of whether
        ///     the conversion can be done at compile time or not. This can be
        ///     used to convert generic types to numeric types during runtime.
        /// </summary>
        /// <param name="array">The vector or array to be converted.</param>
        /// <param name="outputType">The type of the output.</param>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Math\Matrix\Matrix.Conversion.cs" region="doc_convert_matrix" />
        ///     <code source="Unit Tests\Accord.Tests.Math\Matrix\Matrix.Conversion.cs" region="doc_convert_jagged" />
        /// </example>
        public static object To(this Array array, Type outputType)
        {
            if (array.GetType() == outputType)
            {
                return array;
            }

            if (!outputType.IsArray && array.Length == 1)
            {
                foreach (var obj in array)
                {
                    return obj.To(outputType);
                }
            }

            Array result;

            if (outputType.IsJagged())
            {
                // multidimensional or jagged -> jagged
                result = Jagged.CreateAs(array, outputType);
            }
            else
            {
                // multidimensional or jagged -> multidimensional
                result = CreateAs(array, outputType);
            }

            Copy(array, result);

            return result;
        }

        /// <summary>
        ///     Copies elements from an array to another array even if one
        ///     is a jagged array and the other a multidimensional array.
        /// </summary>
        /// <param name="source">The array whose elements should be copied from.</param>
        /// <param name="destination">The array where elements will be written to.</param>
        public static void Copy(this Array source, Array destination)
        {
            var outputElementType = destination.GetInnerMostType();

#if !NETSTANDARD1_4
            if (source.GetType() == destination.GetType() && source.IsMatrix() && destination.IsMatrix())
            {
                if (outputElementType.IsPrimitive)
                {
                    Buffer.BlockCopy(source, 0, destination, 0, source.Length * Marshal.SizeOf(outputElementType));
                }
                else
                {
                    Array.Copy(source, destination, source.Length);
                }
            }
            else
#endif
            {
                var deep = true;

                if (destination.GetLength().Contains(-1))
                {
                    // Result is a jagged array where not all dimensions have been specified
                    var a = source.GetIndices(deep).GetEnumerator();

                    while (a.MoveNext())
                    {
                        var inputValue = source.GetValue(deep, a.Current);
                        var outputValue = convertValue(outputElementType, inputValue);
                        destination.SetValue(outputValue, deep, a.Current);
                    }
                }
                else
                {
                    // Both matrices have been completely specified
                    var a = source.GetIndices(deep).GetEnumerator();
                    var b = destination.GetIndices(deep).GetEnumerator();

                    while (a.MoveNext() && b.MoveNext())
                    {
                        var inputValue = source.GetValue(deep, a.Current);
                        var outputValue = convertValue(outputElementType, inputValue);
                        destination.SetValue(outputValue, deep, b.Current);
                    }
                }
            }
        }

        /// <summary>
        ///     Gets the value at the specified position in the multidimensional System.Array.
        ///     The indexes are specified as an array of 32-bit integers.
        /// </summary>
        /// <param name="array">A jagged or multidimensional array.</param>
        /// <param name="deep">If set to true, internal arrays in jagged arrays will be followed.</param>
        /// <param name="indices">
        ///     A one-dimensional array of 32-bit integers that represent the
        ///     indexes specifying the position of the System.Array element to get.
        /// </param>
        public static object GetValue(this Array array, bool deep, int[] indices)
        {
            if (array.IsVector())
            {
                return array.GetValue(indices);
            }

            if (deep && array.IsJagged())
            {
                var current = array.GetValue(indices[0]) as Array;
                if (indices.Length == 1)
                {
                    return current;
                }

                var last = indices.Get(1, 0);
                return GetValue(current, true, last);
            }

            return array.GetValue(indices);
        }

        /// <summary>
        ///     Gets the value at the specified position in the multidimensional System.Array.
        ///     The indexes are specified as an array of 32-bit integers.
        /// </summary>
        /// <param name="array">A jagged or multidimensional array.</param>
        /// <param name="deep">If set to true, internal arrays in jagged arrays will be followed.</param>
        /// <param name="indices">
        ///     A one-dimensional array of 32-bit integers that represent the
        ///     indexes specifying the position of the System.Array element to get.
        /// </param>
        /// <param name="value">The value retrieved from the array.</param>
        public static bool TryGetValue(this Array array, bool deep, int[] indices, out object value)
        {
            value = null;

            if (array.IsVector())
            {
                if (indices.Length > array.Rank)
                {
                    return false;
                }

                for (var i = 0; i < indices.Length; i++)
                {
                    if (indices[i] > array.GetUpperBound(i))
                    {
                        return false;
                    }
                }

                value = array.GetValue(indices);
                return true;
            }

            if (deep && array.IsJagged())
            {
                var current = array.GetValue(indices[0]) as Array;
                if (indices.Length == 1)
                {
                    value = current;
                    return true;
                }

                var last = indices.Get(1, 0);
                return TryGetValue(current, true, last, out value);
            }

            value = array.GetValue(indices);
            return true;
        }

        /// <summary>
        ///     Sets a value to the element at the specified position in the multidimensional
        ///     or jagged System.Array. The indexes are specified as an array of 32-bit integers.
        /// </summary>
        /// <param name="array">A jagged or multidimensional array.</param>
        /// <param name="value">The new value for the specified element.</param>
        /// <param name="deep">If set to true, internal arrays in jagged arrays will be followed.</param>
        /// <param name="indices">
        ///     A one-dimensional array of 32-bit integers that represent
        ///     the indexes specifying the position of the element to set.
        /// </param>
        public static void SetValue(this Array array, object value, bool deep, int[] indices)
        {
            if (deep && array.IsJagged())
            {
                var current = array.GetValue(indices[0]) as Array;
                var last = indices.Get(1, 0);
                var length = last.Length == 0 ? 1 : last.Max() + 1;
                if (current == null || current.Length < length)
                {
                    if (current == null)
                    {
                        current = Array.CreateInstance(array.GetType().GetElementType().GetElementType(), length);
                    }
                    else
                    {
                        var r = Array.CreateInstance(array.GetType().GetElementType().GetElementType(), length);
                        current.CopyTo(r, 0);
                        current = r;
                    }

                    array.SetValue(current, indices[0]);
                }

                SetValue(current, value, true, last);
            }
            else
            {
                array.SetValue(value, indices);
            }
        }

        private static object convertValue(Type outputElementType, object inputValue)
        {
            var inputArray = inputValue as Array;
            if (inputArray != null)
            {
                return To(inputArray, outputElementType);
            }

            return inputValue.To(outputElementType);
        }

        #endregion

        #region DataTable Conversions

#if !NETSTANDARD1_4
        /// <summary>
        ///     Converts a DataTable to a double[,] array.
        /// </summary>
        public static double[,] ToMatrix(this DataTable table)
        {
            return ToMatrix<double>(table);
        }

        /// <summary>
        ///     Converts a DataTable to a double[,] array.
        /// </summary>
        public static double[,] ToMatrix(this DataTable table, out string[] columnNames)
        {
            return ToMatrix<double>(table, out columnNames);
        }

        /// <summary>
        ///     Converts a DataTable to a double[,] array.
        /// </summary>
        public static double[,] ToMatrix(this DataTable table, IFormatProvider provider)
        {
            return ToMatrix<double>(table, provider);
        }

        /// <summary>
        ///     Converts a DataTable to a double[,] array.
        /// </summary>
        public static double[,] ToMatrix(this DataTable table, params string[] columnNames)
        {
            return ToMatrix<double>(table, columnNames);
        }

        /// <summary>
        ///     Converts a DataTable to a double[,] array.
        /// </summary>
        public static T[,] ToMatrix<T>(this DataTable table, IFormatProvider provider)
        {
            return ToMatrix<T>(table, out _, provider);
        }

        /// <summary>
        ///     Converts a DataTable to a double[,] array.
        /// </summary>
        public static T[,] ToMatrix<T>(this DataTable table)
        {
            return ToMatrix<T>(table, out _);
        }

        /// <summary>
        ///     Converts a DataTable to a double[,] array.
        /// </summary>
        public static T[,] ToMatrix<T>(this DataTable table, out string[] columnNames)
        {
            var m = new T[table.Rows.Count, table.Columns.Count];
            columnNames = new string[table.Columns.Count];

            for (var j = 0; j < table.Columns.Count; j++)
            {
                for (var i = 0; i < table.Rows.Count; i++)
                {
                    var obj = table.Rows[i][j];
                    m[i, j] = (T)System.Convert.ChangeType(obj, typeof(T));
                }

                columnNames[j] = table.Columns[j].Caption;
            }

            return m;
        }

        /// <summary>
        ///     Converts a DataTable to a double[,] array.
        /// </summary>
        public static T[,] ToMatrix<T>(this DataTable table, out string[] columnNames, IFormatProvider provider)
        {
            var m = new T[table.Rows.Count, table.Columns.Count];
            columnNames = new string[table.Columns.Count];

            for (var j = 0; j < table.Columns.Count; j++)
            {
                for (var i = 0; i < table.Rows.Count; i++)
                {
                    var obj = table.Rows[i][j];
                    m[i, j] = (T)System.Convert.ChangeType(obj, typeof(T), provider);
                }

                columnNames[j] = table.Columns[j].Caption;
            }

            return m;
        }

        /// <summary>
        ///     Converts a DataTable to a double[,] array.
        /// </summary>
        public static T[,] ToMatrix<T>(this DataTable table, params string[] columnNames)
        {
            var m = new T[table.Rows.Count, columnNames.Length];

            for (var j = 0; j < columnNames.Length; j++)
            {
                for (var i = 0; i < table.Rows.Count; i++)
                {
                    m[i, j] = (T)System.Convert.ChangeType(table.Rows[i][columnNames[j]], typeof(T));
                }
            }

            return m;
        }

        /// <summary>
        ///     Converts a DataTable to a double[,] array.
        /// </summary>
        public static T[,] ToMatrix<T>(this DataTable table, IFormatProvider provider, params string[] columnNames)
        {
            var m = new T[table.Rows.Count, columnNames.Length];

            for (var j = 0; j < columnNames.Length; j++)
            {
                for (var i = 0; i < table.Rows.Count; i++)
                {
                    m[i, j] = (T)System.Convert.ChangeType(table.Rows[i][columnNames[j]], typeof(T), provider);
                }
            }

            return m;
        }

        /// <summary>
        ///     Converts a DataTable to a double[,] array.
        /// </summary>
        public static DataTable ToTable(this double[,] matrix)
        {
            var cols = matrix.GetLength(1);

            var columnNames = new string[cols];
            for (var i = 0; i < columnNames.Length; i++)
            {
                columnNames[i] = "Column " + i;
            }

            return ToTable(matrix, columnNames);
        }

        /// <summary>
        ///     Converts a DataTable to a double[,] array.
        /// </summary>
        public static DataTable ToTable(this double[,] matrix, params string[] columnNames)
        {
            var table = new DataTable();
            table.Locale = CultureInfo.CurrentCulture;

            var rows = matrix.GetLength(0);
            var cols = matrix.GetLength(1);

            for (var i = 0; i < cols; i++)
            {
                table.Columns.Add(columnNames[i], typeof(double));
            }

            for (var i = 0; i < rows; i++)
            {
                table.Rows.Add(matrix.GetRow(i).Convert(x => (object)x));
            }

            return table;
        }

        /// <summary>
        ///     Converts a DataTable to a double[,] array.
        /// </summary>
        public static DataTable ToTable(this double[][] matrix)
        {
            var cols = matrix[0].Length;

            var columnNames = new string[cols];
            for (var i = 0; i < columnNames.Length; i++)
            {
                columnNames[i] = "Column " + i;
            }

            return ToTable(matrix, columnNames);
        }

        /// <summary>
        ///     Converts a DataTable to a double[,] array.
        /// </summary>
        public static DataTable ToTable(this double[][] matrix, params string[] columnNames)
        {
            var table = new DataTable();
            table.Locale = CultureInfo.CurrentCulture;

            for (var i = 0; i < columnNames.Length; i++)
            {
                table.Columns.Add(columnNames[i], typeof(double));
            }

            for (var i = 0; i < matrix.Length; i++)
            {
                table.Rows.Add(matrix[i].Convert(x => (object)x));
            }

            return table;
        }

        /// <summary>
        ///     Converts an array of values into a <see cref="DataTable" />,
        ///     attempting to guess column types by inspecting the data.
        /// </summary>
        /// <param name="values">The values to be converted.</param>
        /// <returns>A <see cref="DataTable" /> containing the given values.</returns>
        /// <example>
        ///     <code>
        /// // Specify some data in a table format
        /// //
        /// object[,] data = 
        /// {
        ///     { "Id", "IsSmoker", "Age" },
        ///     {   0,       1,        10  },
        ///     {   1,       1,        15  },
        ///     {   2,       0,        40  },
        ///     {   3,       1,        20  },
        ///     {   4,       0,        70  },
        ///     {   5,       0,        55  },
        /// };
        /// 
        /// // Create a new table with the data
        /// DataTable dataTable = data.ToTable();
        /// </code>
        /// </example>
        public static DataTable ToTable(this object[,] values)
        {
            var columnNames = new string[values.Columns()];
            for (var i = 0; i < columnNames.Length; i++)
            {
                columnNames[i] = "Column " + i;
            }

            return ToTable(values, columnNames);
        }

        /// <summary>
        ///     Converts an array of values into a <see cref="DataTable" />,
        ///     attempting to guess column types by inspecting the data.
        /// </summary>
        /// <param name="matrix">The values to be converted.</param>
        /// <param name="columnNames">The column names to use in the data table.</param>
        /// <returns>A <see cref="DataTable" /> containing the given values.</returns>
        /// <example>
        ///     <code>
        /// // Specify some data in a table format
        /// //
        /// object[,] data = 
        /// {
        ///     { "Id", "IsSmoker", "Age" },
        ///     {   0,       1,        10  },
        ///     {   1,       1,        15  },
        ///     {   2,       0,        40  },
        ///     {   3,       1,        20  },
        ///     {   4,       0,        70  },
        ///     {   5,       0,        55  },
        /// };
        /// 
        /// // Create a new table with the data
        /// DataTable dataTable = data.ToTable();
        /// </code>
        /// </example>
        public static DataTable ToTable(this object[,] matrix, string[] columnNames)
        {
            var table = new DataTable();
            table.Locale = CultureInfo.InvariantCulture;

            var headers = matrix.GetRow(0);

            if (headers.All(x => x is string))
            {
                // Get first data row to set types
                var first = matrix.GetRow(1);

                // Assume first row is header row
                for (var i = 0; i < first.Length; i++)
                {
                    table.Columns.Add(headers[i] as string, first[i].GetType());
                }

                // Parse all the other rows
                var rows = matrix.GetLength(0);
                for (var i = 1; i < rows; i++)
                {
                    var row = matrix.GetRow(i);
                    table.Rows.Add(row);
                }
            }
            else
            {
                for (var i = 0; i < matrix.Columns(); i++)
                {
                    var columnType = GetHighestEnclosingType(matrix.GetColumn(i));
                    table.Columns.Add(columnNames[i], columnType);
                }

                var rows = matrix.GetLength(0);
                for (var i = 0; i < rows; i++)
                {
                    var row = matrix.GetRow(i);
                    table.Rows.Add(row);
                }
            }

            return table;
        }

        private static Type GetHighestEnclosingType(object[] values)
        {
            var types = values.Select(x => x != null ? x.GetType() : null).ToArray();
            if (types.Any(x => x == typeof(object)))
            {
                return typeof(object);
            }

            if (types.Any(x => x == typeof(string)))
            {
                return typeof(string);
            }

            if (types.Any(x => x == typeof(decimal)))
            {
                return typeof(decimal);
            }

            if (types.Any(x => x == typeof(double)))
            {
                return typeof(double);
            }

            if (types.Any(x => x == typeof(float)))
            {
                return typeof(float);
            }

            if (types.Any(x => x == typeof(int)))
            {
                return typeof(int);
            }

            if (types.Any(x => x == typeof(uint)))
            {
                return typeof(uint);
            }

            if (types.Any(x => x == typeof(short)))
            {
                return typeof(int);
            }

            if (types.Any(x => x == typeof(byte)))
            {
                return typeof(int);
            }

            if (types.Any(x => x == typeof(sbyte)))
            {
                return typeof(int);
            }

            return typeof(object);
        }

        /// <summary>
        ///     Converts a DataTable to a double[][] array.
        /// </summary>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Math\Matrix\Matrix.Conversion.cs" region="doc_table_tojagged" />
        /// </example>
        public static double[][] ToJagged(this DataTable table)
        {
            return ToJagged<double>(table);
        }

        /// <summary>
        ///     Converts a DataTable to a double[][] array.
        /// </summary>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Math\Matrix\Matrix.Conversion.cs" region="doc_table_tojagged" />
        /// </example>
        public static double[][] ToJagged(this DataTable table, IFormatProvider provider)
        {
            return ToJagged<double>(table, provider);
        }

        /// <summary>
        ///     Converts a DataTable to a double[][] array.
        /// </summary>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Math\Matrix\Matrix.Conversion.cs" region="doc_table_tojagged" />
        /// </example>
        public static double[][] ToJagged(this DataTable table, out string[] columnNames)
        {
            return ToJagged<double>(table, out columnNames);
        }

        /// <summary>
        ///     Converts a DataTable to a double[][] array.
        /// </summary>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Math\Matrix\Matrix.Conversion.cs" region="doc_table_tojagged" />
        /// </example>
        public static double[][] ToJagged(this DataTable table, IFormatProvider provider, out string[] columnNames)
        {
            return ToJagged<double>(table, provider, out columnNames);
        }

        /// <summary>
        ///     Converts a DataTable to a double[][] array.
        /// </summary>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Math\Matrix\Matrix.Conversion.cs" region="doc_table_tojagged" />
        /// </example>
        public static double[][] ToJagged(this DataTable table, params string[] columnNames)
        {
            return ToJagged<double>(table, columnNames);
        }

        /// <summary>
        ///     Converts a DataTable to a T[][] array.
        /// </summary>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Math\Matrix\Matrix.Conversion.cs" region="doc_table_tojagged" />
        /// </example>
        public static T[][] ToJagged<T>(this DataTable table)
        {
            return ToJagged<T>(table, out _);
        }

        /// <summary>
        ///     Converts a DataTable to a T[][] array.
        /// </summary>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Math\Matrix\Matrix.Conversion.cs" region="doc_table_tojagged" />
        /// </example>
        public static T[][] ToJagged<T>(this DataTable table, IFormatProvider provider)
        {
            return ToJagged<T>(table, provider, out _);
        }

        /// <summary>
        ///     Converts a DataTable to a T[][] array.
        /// </summary>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Math\Matrix\Matrix.Conversion.cs" region="doc_table_tojagged" />
        /// </example>
        public static T[][] ToJagged<T>(this DataTable table, out string[] columnNames)
        {
            var m = new T[table.Rows.Count][];
            columnNames = new string[table.Columns.Count];

            for (var i = 0; i < table.Rows.Count; i++)
            {
                m[i] = new T[table.Columns.Count];
            }

            for (var j = 0; j < table.Columns.Count; j++)
            {
                for (var i = 0; i < table.Rows.Count; i++)
                {
                    var value = table.Rows[i][j];
                    m[i][j] = (T)System.Convert.ChangeType(value, typeof(T));
                }

                columnNames[j] = table.Columns[j].Caption;
            }

            return m;
        }

        /// <summary>
        ///     Converts a DataTable to a T[][] array.
        /// </summary>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Math\Matrix\Matrix.Conversion.cs" region="doc_table_tojagged" />
        /// </example>
        public static T[][] ToJagged<T>(this DataTable table, IFormatProvider provider, out string[] columnNames)
        {
            var m = new T[table.Rows.Count][];
            columnNames = new string[table.Columns.Count];

            for (var i = 0; i < table.Rows.Count; i++)
            {
                m[i] = new T[table.Columns.Count];
            }

            for (var j = 0; j < table.Columns.Count; j++)
            {
                for (var i = 0; i < table.Rows.Count; i++)
                {
                    m[i][j] = (T)System.Convert.ChangeType(table.Rows[i][j], typeof(T), provider);
                }

                columnNames[j] = table.Columns[j].Caption;
            }

            return m;
        }

        /// <summary>
        ///     Converts a DataTable to a T[][] array.
        /// </summary>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Math\Matrix\Matrix.Conversion.cs" region="doc_table_tojagged" />
        /// </example>
        public static T[][] ToJagged<T>(this DataTable table, params string[] columnNames)
        {
            var m = new T[table.Rows.Count][];

            for (var i = 0; i < table.Rows.Count; i++)
            {
                m[i] = new T[columnNames.Length];
            }

            for (var j = 0; j < columnNames.Length; j++)
            {
                var col = table.Columns[columnNames[j]];

                for (var i = 0; i < table.Rows.Count; i++)
                {
                    m[i][j] = (T)System.Convert.ChangeType(table.Rows[i][col], typeof(T));
                }
            }

            return m;
        }

        /// <summary>
        ///     Converts a DataTable to a double[][] array.
        /// </summary>
        public static double[] ToVector(this DataTable table)
        {
            return ToVector<double>(table);
        }

        /// <summary>
        ///     Converts a DataTable to a double[] array.
        /// </summary>
        public static double[] ToVector(this DataTable table, IFormatProvider provider)
        {
            return ToVector<double>(table, provider);
        }

        /// <summary>
        ///     Converts a DataTable to a double[][] array.
        /// </summary>
        public static double[] ToVector(this DataTable table, out string columnName)
        {
            return ToVector<double>(table, out columnName);
        }

        /// <summary>
        ///     Converts a DataTable to a double[] array.
        /// </summary>
        public static double[] ToVector(this DataTable table, IFormatProvider provider, out string columnName)
        {
            return ToVector<double>(table, provider, out columnName);
        }

        /// <summary>
        ///     Converts a DataTable to a double[] array.
        /// </summary>
        public static double[] ToVector(this DataTable table, string columnName)
        {
            return ToVector<double>(table, columnName);
        }

        /// <summary>
        ///     Converts a DataTable to a double[] array.
        /// </summary>
        public static T[] ToVector<T>(this DataTable table)
        {
            return ToVector<T>(table, out _);
        }

        /// <summary>
        ///     Converts a DataTable to a double[] array.
        /// </summary>
        public static T[] ToVector<T>(this DataTable table, IFormatProvider provider)
        {
            return ToVector<T>(table, provider, out _);
        }

        /// <summary>
        ///     Converts a DataTable to a double[] array.
        /// </summary>
        public static T[] ToVector<T>(this DataTable table, out string columnName)
        {
            if (table.Columns.Count > 1)
            {
                throw new ArgumentException(
                    "The given table has more than one column. Please specify which column should be converted.");
            }

            columnName = table.Columns[0].ColumnName;
            return table.Columns[0].ToArray<T>();
        }

        /// <summary>
        ///     Converts a DataTable to a double[] array.
        /// </summary>
        public static T[] ToVector<T>(this DataTable table, IFormatProvider provider, out string columnName)
        {
            if (table.Columns.Count > 1)
            {
                throw new ArgumentException(
                    "The given table has more than one column. Please specify which column should be converted.");
            }

            columnName = table.Columns[0].ColumnName;
            return table.Columns[0].ToArray<T>(provider);
        }

        /// <summary>
        ///     Converts a DataTable to a double[] array.
        /// </summary>
        public static T[] ToVector<T>(this DataTable table, string columnName)
        {
            return table.Columns[columnName].ToArray<T>();
        }

        /// <summary>
        ///     Converts a DataColumn to a double[] array.
        /// </summary>
        public static double[] ToArray(this DataColumn column)
        {
            return ToArray<double>(column);
        }

        /// <summary>
        ///     Converts a DataColumn to a double[] array.
        /// </summary>
        public static double[] ToArray(this DataColumn column, IFormatProvider provider)
        {
            return ToArray<double>(column, provider);
        }

        /// <summary>
        ///     Converts a DataColumn to a double[] array.
        /// </summary>
        public static double[] ToArray(this DataRow row, IFormatProvider provider, params string[] colNames)
        {
            return ToArray<double>(row, provider, colNames);
        }

        /// <summary>
        ///     Converts a DataColumn to a double[] array.
        /// </summary>
        public static double[] ToArray(this DataRow row, params string[] colNames)
        {
            return ToArray<double>(row, colNames);
        }

        /// <summary>
        ///     Converts a DataColumn to a double[] array.
        /// </summary>
        public static T[] ToArray<T>(this DataColumn column)
        {
            var m = new T[column.Table.Rows.Count];

            for (var i = 0; i < m.Length; i++)
            {
                m[i] = (T)System.Convert.ChangeType(column.Table.Rows[i][column], typeof(T));
            }

            return m;
        }

        /// <summary>
        ///     Converts a DataColumn to a double[] array.
        /// </summary>
        public static T[] ToArray<T>(this DataColumn column, IFormatProvider provider)
        {
            var m = new T[column.Table.Rows.Count];

            for (var i = 0; i < m.Length; i++)
            {
                m[i] = (T)System.Convert.ChangeType(column.Table.Rows[i][column], typeof(T), provider);
            }

            return m;
        }

        /// <summary>
        ///     Converts a DataColumn to a generic array.
        /// </summary>
        public static T[] ToArray<T>(this DataRow row, params string[] colNames)
        {
            var m = new T[colNames.Length];

            for (var i = 0; i < m.Length; i++)
            {
                m[i] = (T)System.Convert.ChangeType(row[colNames[i]], typeof(T));
            }

            return m;
        }

        /// <summary>
        ///     Converts a DataColumn to a generic array.
        /// </summary>
        public static T[] ToArray<T>(this DataRow row, IFormatProvider provider, params string[] colNames)
        {
            var m = new T[colNames.Length];

            for (var i = 0; i < m.Length; i++)
            {
                m[i] = (T)System.Convert.ChangeType(row[colNames[i]], typeof(T), provider);
            }

            return m;
        }

        /// <summary>
        ///     Converts a DataTable to a generic array.
        /// </summary>
        public static T[] ToArray<T>(this DataTable table, string columnName)
        {
            var m = new T[table.Rows.Count];

            var col = table.Columns[columnName];
            for (var i = 0; i < table.Rows.Count; i++)
            {
                m[i] = (T)System.Convert.ChangeType(table.Rows[i][col], typeof(T));
            }

            return m;
        }

        /// <summary>
        ///     Converts a DataTable to a generic array.
        /// </summary>
        public static T[] ToArray<T>(this DataTable table, IFormatProvider provider, string columnName)
        {
            var m = new T[table.Rows.Count];

            var col = table.Columns[columnName];
            for (var i = 0; i < table.Rows.Count; i++)
            {
                m[i] = (T)System.Convert.ChangeType(table.Rows[i][col], typeof(T), provider);
            }

            return m;
        }
#endif

        #endregion
    }
}