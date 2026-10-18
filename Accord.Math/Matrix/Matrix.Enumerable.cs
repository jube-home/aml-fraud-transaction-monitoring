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

using System.Collections.Generic;

namespace Accord.Math
{
    public static partial class Matrix
    {
        /// <summary>
        ///     Gets the number of rows in a jagged matrix.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the matrix.</typeparam>
        /// <param name="values">The matrix whose number of rows must be computed.</param>
        /// <returns>The number of rows in the matrix.</returns>
        public static int Rows<T>(this IList<IList<T>> values)
        {
            return values.Count;
        }

        /// <summary>
        ///     Gets the number of columns in a jagged matrix.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the matrix.</typeparam>
        /// <param name="values">The matrix whose number of rows must be computed.</param>
        /// <returns>The number of columns in the matrix.</returns>
        public static int Columns<T>(this IList<IList<T>> values)
        {
            return values[0].Count;
        }

        /// <summary>
        ///     Converts a matrix represented as a nested list of lists into a multi-dimensional matrix.
        /// </summary>
        public static T[,] ToMatrix<T, U>(this IList<IList<T>> values)
        {
            var rows = values.Rows();
            var cols = values.Columns();

            var result = Zeros<T>(rows, cols);
            for (var i = 0; i < values.Count; i++)
            {
                for (var j = 0; j < values[i].Count; j++)
                {
                    result[i, j] = values[i][j];
                }
            }

            return result;
        }

        /// <summary>
        ///     Converts a matrix represented as a nested list of lists into a jagged matrix.
        /// </summary>
        public static T[][] ToJagged<T>(this IList<IList<T>> values)
        {
            var rows = values.Rows();
            var cols = values.Columns();

            var result = Jagged.Zeros<T>(rows, cols);
            for (var i = 0; i < values.Count; i++)
            {
                for (var j = 0; j < values[i].Count; j++)
                {
                    result[i][j] = values[i][j];
                }
            }

            return result;
        }
    }
}