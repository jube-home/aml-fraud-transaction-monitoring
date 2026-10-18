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

namespace Accord.Math
{
    public static partial class Jagged
    {
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
        public static TOutput[][] Convert<TInput, TOutput>(TInput[,] matrix)
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
        public static TOutput[][] Convert<TInput, TOutput>(this TInput[][] matrix)
        {
            return Convert(matrix, x => (TOutput)System.Convert.ChangeType(x, typeof(TOutput)));
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
        public static TOutput[][] Convert<TInput, TOutput>(this TInput[,] matrix,
#if !NETSTANDARD1_4
            Converter<TInput, TOutput> converter
#else
            Func<TInput, TOutput> converter
#endif
        )
        {
            var rows = matrix.Rows();
            var cols = matrix.Columns();

            var result = Zeros<TOutput>(rows, cols);
            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < cols; j++)
                {
                    result[i][j] = converter(matrix[i, j]);
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
        public static TOutput[][] Convert<TInput, TOutput>(this TInput[][] matrix,
#if !NETSTANDARD1_4
            Converter<TInput, TOutput> converter
#else
            Func<TInput, TOutput> converter
#endif
        )
        {
            var rows = matrix.Rows();
            var cols = matrix.Columns();

            var result = Zeros<TOutput>(rows, cols);
            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < cols; j++)
                {
                    result[i][j] = converter(matrix[i][j]);
                }
            }

            return result;
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
    }
}