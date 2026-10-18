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
    /// <summary>
    ///     Reduced row Echelon form
    /// </summary>
    public class ReducedRowEchelonForm
    {
        private readonly int cols;

        private readonly int rows;
        private int? freeCount;

        /// <summary>
        ///     Reduces a matrix to reduced row Echelon form.
        /// </summary>
        /// <param name="value">The matrix to be reduced.</param>
        /// <param name="inPlace">
        ///     Pass <see langword="true" /> to perform the reduction in place. The matrix
        ///     <paramref name="value" /> will be destroyed in the process, resulting in less
        ///     memory consumption.
        /// </param>
        public ReducedRowEchelonForm(double[,] value, bool inPlace = false)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            Result = inPlace ? value : (double[,])value.Clone();

            var lead = 0;
            rows = Result.GetLength(0);
            cols = Result.GetLength(1);

            Pivot = new int[rows];
            for (var i = 0; i < Pivot.Length; i++)
            {
                Pivot[i] = i;
            }


            for (var r = 0; r < rows; r++)
            {
                if (cols <= lead)
                {
                    break;
                }

                var i = r;

                while (Result[i, lead] == 0)
                {
                    i++;

                    if (i >= rows)
                    {
                        i = r;

                        if (lead < cols - 1)
                        {
                            lead++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                if (i != r)
                {
                    // Swap rows i and r
                    for (var j = 0; j < cols; j++)
                    {
                        var temp = Result[r, j];
                        Result[r, j] = Result[i, j];
                        Result[i, j] = temp;
                    }

                    // Update indices
                    {
                        var temp = Pivot[r];
                        Pivot[r] = Pivot[i];
                        Pivot[i] = temp;
                    }
                }

                // Set to reduced row echelon form
                var div = Result[r, lead];
                if (div != 0)
                {
                    for (var j = 0; j < cols; j++)
                    {
                        Result[r, j] /= div;
                    }
                }

                for (var j = 0; j < rows; j++)
                {
                    if (j != r)
                    {
                        var sub = Result[j, lead];
                        for (var k = 0; k < cols; k++)
                        {
                            Result[j, k] -= sub * Result[r, k];
                        }
                    }
                }

                lead++;
            }
        }

        /// <summary>
        ///     Gets the pivot indicating the position
        ///     of the original rows before the swap.
        /// </summary>
        public int[] Pivot { get; }

        /// <summary>
        ///     Gets the matrix in row reduced Echelon form.
        /// </summary>
        public double[,] Result { get; }

        /// <summary>
        ///     Gets the number of free variables (linear
        ///     dependent rows) in the given matrix.
        /// </summary>
        public int FreeVariables
        {
            get
            {
                if (freeCount == null)
                {
                    freeCount = count();
                }

                return freeCount.Value;
            }
        }

        private int count()
        {
            for (var i = rows - 1; i >= 0; i--)
            {
                for (var j = 0; j < cols; j++)
                {
                    if (Result[i, j] != 0)
                    {
                        return rows - i - 1;
                    }
                }
            }

            return 0;
        }
    }
}