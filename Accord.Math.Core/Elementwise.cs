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

using System.CodeDom.Compiler;
using System.Diagnostics;

namespace Accord.Math
{
    /// <summary>
    ///     Vector types.
    /// </summary>
    public enum VectorType
    {
        /// <summary>
        ///     The vector is a row vector, meaning it should have a size equivalent
        ///     to [1 x N] where N is the number of elements in the vector.
        /// </summary>
        RowVector = 0,

        /// <summary>
        ///     The vector is a column vector, meaning it should have a size equivalent
        ///     to [N x 1] where N is the number of elements in the vector.
        /// </summary>
        ColumnVector = 1
    }

    /// <summary>
    ///     Elementwise matrix and vector operations.
    /// </summary>
    /// <seealso cref="VectorType" />
    [GeneratedCode("Accord.NET T4 Templates", "3.7")]
    public static partial class Elementwise
    {
        private static int rows<U>(U[] b)
        {
            return b.Length;
        }

        private static int cols<U>(U[][] b)
        {
            return b[0].Length;
        }

        private static int rows<U>(U[,] b)
        {
            return b.GetLength(0);
        }

        private static int cols<U>(U[,] b)
        {
            return b.GetLength(1);
        }

        [Conditional("DEBUG")]
        private static void check<T, U>(T[] a, U[] b)
        {
            if (a.Length != b.Length)
            {
                throw new DimensionMismatchException("b");
            }
        }

        [Conditional("DEBUG")]
        private static void check<T, U, V>(T[] a, U[] b, V[] result)
        {
            if (a.Length != b.Length)
            {
                throw new DimensionMismatchException("b");
            }

            if (a.Length != result.Length)
            {
                throw new DimensionMismatchException("result");
            }
        }

        [Conditional("DEBUG")]
        private static void check<T, U, V>(T[] a, U b, V[] result)
        {
            if (a.Length != result.Length)
            {
                throw new DimensionMismatchException("result");
            }
        }

        [Conditional("DEBUG")]
        private static void check<T, U, V>(T a, U[] b, V[] result)
        {
            if (b.Length != result.Length)
            {
                throw new DimensionMismatchException("result");
            }
        }

        [Conditional("DEBUG")]
        private static void check<T, U>(T[,] a, U[,] b)
        {
            if (rows(a) != rows(b) || cols(a) != cols(b))
            {
                throw new DimensionMismatchException("b");
            }
        }

        [Conditional("DEBUG")]
        private static void check<T, U>(T[][] a, U[][] b)
        {
            if (rows(a) != rows(b) || cols(a) != cols(b))
            {
                throw new DimensionMismatchException("b");
            }
        }

        [Conditional("DEBUG")]
        private static void check<T, U, V>(T[,] a, U[,] b, V[,] result)
        {
            if (rows(a) != rows(b) || cols(a) != cols(b))
            {
                throw new DimensionMismatchException("b");
            }

            if (rows(a) != rows(result) || cols(a) != cols(result))
            {
                throw new DimensionMismatchException("result");
            }
        }

        [Conditional("DEBUG")]
        private static void check<T, U, V>(T[][] a, U[][] b, V[][] result)
        {
            if (rows(a) != rows(b) || cols(a) != cols(b))
            {
                throw new DimensionMismatchException("b");
            }

            if (rows(a) != rows(result) || cols(a) != cols(result))
            {
                throw new DimensionMismatchException("result");
            }
        }

        [Conditional("DEBUG")]
        private static void check<T, U, V>(int d, T[,] a, U[] b, V[,] result)
        {
            if (d == 0)
            {
                if (cols(a) != rows(b))
                {
                    throw new DimensionMismatchException("b");
                }
            }
            else
            {
                if (rows(a) != rows(b))
                {
                    throw new DimensionMismatchException("b");
                }
            }

            if (rows(a) != rows(result) || cols(a) != cols(result))
            {
                throw new DimensionMismatchException("result");
            }
        }

        [Conditional("DEBUG")]
        private static void check<T, U, V>(int d, T[][] a, U[] b, V[][] result)
        {
            if (d == 0)
            {
                if (cols(a) != rows(b))
                {
                    throw new DimensionMismatchException("b");
                }
            }
            else
            {
                if (rows(a) != rows(b))
                {
                    throw new DimensionMismatchException("b");
                }
            }

            if (rows(a) != rows(result) || cols(a) != cols(result))
            {
                throw new DimensionMismatchException("result");
            }
        }

        [Conditional("DEBUG")]
        private static void check<T, U, V>(int d, T[] a, U[,] b, V[,] result)
        {
            if (d == 0)
            {
                if (cols(b) != rows(a))
                {
                    throw new DimensionMismatchException("b");
                }
            }
            else
            {
                if (rows(b) != rows(a))
                {
                    throw new DimensionMismatchException("b");
                }
            }

            if (rows(b) != rows(result) || cols(b) != cols(result))
            {
                throw new DimensionMismatchException("result");
            }
        }

        [Conditional("DEBUG")]
        private static void check<T, U, V>(int d, T[] a, U[][] b, V[][] result)
        {
            if (d == 0)
            {
                if (cols(b) != rows(a))
                {
                    throw new DimensionMismatchException("b");
                }
            }
            else
            {
                if (rows(b) != rows(a))
                {
                    throw new DimensionMismatchException("b");
                }
            }

            if (rows(b) != rows(result) || cols(b) != cols(result))
            {
                throw new DimensionMismatchException("result");
            }
        }

        [Conditional("DEBUG")]
        private static void check<T, U, V>(VectorType d, T[,] a, U[] b, V[,] result)
        {
            if (d == 0)
            {
                if (cols(a) != rows(b))
                {
                    throw new DimensionMismatchException("b");
                }
            }
            else
            {
                if (rows(a) != rows(b))
                {
                    throw new DimensionMismatchException("b");
                }
            }

            if (rows(a) != rows(result) || cols(a) != cols(result))
            {
                throw new DimensionMismatchException("result");
            }
        }

        [Conditional("DEBUG")]
        private static void check<T, U, V>(VectorType d, T[][] a, U[] b, V[][] result)
        {
            if (d == 0)
            {
                if (cols(a) != rows(b))
                {
                    throw new DimensionMismatchException("b");
                }
            }
            else
            {
                if (rows(a) != rows(b))
                {
                    throw new DimensionMismatchException("b");
                }
            }

            if (rows(a) != rows(result) || cols(a) != cols(result))
            {
                throw new DimensionMismatchException("result");
            }
        }

        [Conditional("DEBUG")]
        private static void check<T, U, V>(VectorType d, T[] a, U[,] b, V[,] result)
        {
            if (d == 0)
            {
                if (cols(b) != rows(a))
                {
                    throw new DimensionMismatchException("b");
                }
            }
            else
            {
                if (rows(b) != rows(a))
                {
                    throw new DimensionMismatchException("b");
                }
            }

            if (rows(b) != rows(result) || cols(b) != cols(result))
            {
                throw new DimensionMismatchException("result");
            }
        }

        [Conditional("DEBUG")]
        private static void check<T, U, V>(VectorType d, T[] a, U[][] b, V[][] result)
        {
            if (d == 0)
            {
                if (cols(b) != rows(a))
                {
                    throw new DimensionMismatchException("b");
                }
            }
            else
            {
                if (rows(b) != rows(a))
                {
                    throw new DimensionMismatchException("b");
                }
            }

            if (rows(b) != rows(result) || cols(b) != cols(result))
            {
                throw new DimensionMismatchException("result");
            }
        }

        [Conditional("DEBUG")]
        private static void check<T, U, V>(T[,] a, U[] b, V[,] result)
        {
            if (rows(a) != rows(b))
            {
                throw new DimensionMismatchException("b");
            }

            if (rows(a) != rows(result) || cols(a) != cols(result))
            {
                throw new DimensionMismatchException("result");
            }
        }

        [Conditional("DEBUG")]
        private static void check<T, U, V>(T[][] a, U[] b, V[][] result)
        {
            if (rows(a) != rows(b))
            {
                throw new DimensionMismatchException("b");
            }

            if (rows(a) != rows(result) || cols(a) != cols(result))
            {
                throw new DimensionMismatchException("result");
            }
        }

        [Conditional("DEBUG")]
        private static void check<T, U, V>(T[] a, U[,] b, V[,] result)
        {
            if (rows(a) != rows(b))
            {
                throw new DimensionMismatchException("b");
            }

            if (rows(a) != rows(result) || cols(b) != cols(result))
            {
                throw new DimensionMismatchException("result");
            }
        }

        [Conditional("DEBUG")]
        private static void check<T, U, V>(T[] a, U[][] b, V[][] result)
        {
            if (rows(a) != rows(b))
            {
                throw new DimensionMismatchException("b");
            }

            if (rows(a) != rows(result) || cols(b) != cols(result))
            {
                throw new DimensionMismatchException("result");
            }
        }

        [Conditional("DEBUG")]
        private static void check<T, U, V>(int d, T a, U[,] b, V[,] result)
        {
            if (rows(b) != rows(result) || cols(b) != cols(result))
            {
                throw new DimensionMismatchException("result");
            }
        }

        [Conditional("DEBUG")]
        private static void check<T, U, V>(int d, T a, U[][] b, V[][] result)
        {
            if (rows(b) != rows(result) || cols(b) != cols(result))
            {
                throw new DimensionMismatchException("result");
            }
        }

        [Conditional("DEBUG")]
        private static void check<T, U, V>(T a, U[,] b, V[,] result)
        {
            if (rows(b) != rows(result) || cols(b) != cols(result))
            {
                throw new DimensionMismatchException("result");
            }
        }

        [Conditional("DEBUG")]
        private static void check<T, U, V>(T a, U[][] b, V[][] result)
        {
            if (rows(b) != rows(result) || cols(b) != cols(result))
            {
                throw new DimensionMismatchException("result");
            }
        }

        [Conditional("DEBUG")]
        private static void check<T, U, V>(T[][] a, U b, V[][] result)
        {
            if (rows(a) != rows(result) || cols(a) != cols(result))
            {
                throw new DimensionMismatchException("result");
            }
        }

        [Conditional("DEBUG")]
        private static void check<T, U, V>(T[,] a, U b, V[,] result)
        {
            if (rows(a) != rows(result) || cols(a) != cols(result))
            {
                throw new DimensionMismatchException("result");
            }
        }

        [Conditional("DEBUG")]
        private static void check<T, U, V, W>(T[][] a, U b, V[,] c, W[,] result)
        {
            if (rows(a) != rows(c) || cols(a) != cols(c))
            {
                throw new DimensionMismatchException("c");
            }

            if (rows(a) != rows(result) || cols(a) != cols(result))
            {
                throw new DimensionMismatchException("result");
            }
        }

        [Conditional("DEBUG")]
        private static void check<T, U, V, W>(T[][] a, U b, V[][] c, W[][] result)
        {
            if (rows(a) != rows(c) || cols(a) != cols(c))
            {
                throw new DimensionMismatchException("c");
            }

            if (rows(a) != rows(result) || cols(a) != cols(result))
            {
                throw new DimensionMismatchException("result");
            }
        }

        [Conditional("DEBUG")]
        private static void check<T, U, V, W>(T[,] a, U b, V[,] c, W[,] result)
        {
            if (rows(a) != rows(c) || cols(a) != cols(c))
            {
                throw new DimensionMismatchException("c");
            }

            if (rows(a) != rows(result) || cols(a) != cols(result))
            {
                throw new DimensionMismatchException("result");
            }
        }
    }
}