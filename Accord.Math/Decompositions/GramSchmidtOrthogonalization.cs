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

namespace Accord.Math.Decompositions
{
    /// <summary>
    ///     Gram-Schmidt Orthogonalization.
    /// </summary>
    public class GramSchmidtOrthogonalization
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="GramSchmidtOrthogonalization" /> class.
        /// </summary>
        /// <param name="value">The matrix <c>A</c> to be decomposed.</param>
        public GramSchmidtOrthogonalization(double[,] value)
            : this(value, true)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="GramSchmidtOrthogonalization" /> class.
        /// </summary>
        /// <param name="value">The matrix <c>A</c> to be decomposed.</param>
        /// <param name="modified">
        ///     True to use modified Gram-Schmidt; false
        ///     otherwise. Default is true (and is the recommended setup).
        /// </param>
        public GramSchmidtOrthogonalization(double[,] value, bool modified)
        {
            if (value.GetLength(0) != value.GetLength(1))
            {
                throw new DimensionMismatchException("value", "Matrix must be square.");
            }

            var size = value.GetLength(0);

            OrthogonalFactor = new double[size, size];
            UpperTriangularFactor = new double[size, size];

            if (modified)
            {
                for (var j = 0; j < size; j++)
                {
                    var v = value.GetColumn(j);

                    for (var i = 0; i < j; i++)
                    {
                        UpperTriangularFactor[i, j] = OrthogonalFactor.GetColumn(i).Dot(v);
                        var t = UpperTriangularFactor[i, j].Multiply(OrthogonalFactor.GetColumn(i));
                        v.Subtract(t, v);
                    }

                    UpperTriangularFactor[j, j] = v.Euclidean();
                    OrthogonalFactor.SetColumn(j, v.Divide(UpperTriangularFactor[j, j]));
                }
            }

            else
            {
                for (var j = 0; j < size; j++)
                {
                    var v = value.GetColumn(j);
                    var a = value.GetColumn(j);

                    for (var i = 0; i < j; i++)
                    {
                        UpperTriangularFactor[i, j] = OrthogonalFactor.GetColumn(i).Dot(a);
                        v = v.Subtract(UpperTriangularFactor[i, j].Multiply(OrthogonalFactor.GetColumn(i)));
                    }

                    UpperTriangularFactor[j, j] = v.Euclidean();
                    OrthogonalFactor.SetColumn(j, v.Divide(UpperTriangularFactor[j, j]));
                }
            }
        }

        /// <summary>
        ///     Returns the orthogonal factor matrix <c>Q</c>.
        /// </summary>
        public double[,] OrthogonalFactor { get; }

        /// <summary>
        ///     Returns the upper triangular factor matrix <c>R</c>.
        /// </summary>
        public double[,] UpperTriangularFactor { get; }
    }
}