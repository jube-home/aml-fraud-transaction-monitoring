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

using Accord.Math.Decompositions;

namespace Accord.Math
{
    /// <summary>
    ///     Static class Norm. Defines a set of extension methods defining norms measures.
    /// </summary>
    public static partial class Norm
    {
        /// <summary>
        ///     Returns the maximum column sum of the given matrix.
        /// </summary>
        public static double Norm1(this double[,] a)
        {
            double max = 0;
            for (var j = 0; j < a.GetLength(1); j++)
            {
                double sum = 0;
                for (var i = 0; i < a.GetLength(0); i++)
                {
                    sum += System.Math.Abs(a[i, j]);
                }

                if (sum > max)
                {
                    max = sum;
                }
            }

            return max;
        }

        /// <summary>
        ///     Returns the maximum column sum of the given matrix.
        /// </summary>
        public static double Norm1(this double[][] a)
        {
            double max = 0;
            for (var j = 0; j < a[0].Length; j++)
            {
                double sum = 0;
                for (var i = 0; i < a.Length; i++)
                {
                    sum += System.Math.Abs(a[i][j]);
                }

                if (sum > max)
                {
                    max = sum;
                }
            }

            return max;
        }

        /// <summary>
        ///     Returns the maximum singular value of the given matrix.
        /// </summary>
        public static double Norm2(this double[,] a)
        {
            return new SingularValueDecomposition(a, false, false).TwoNorm;
        }

        /// <summary>
        ///     Returns the maximum singular value of the given matrix.
        /// </summary>
        public static double Norm2(this double[][] a)
        {
            return new JaggedSingularValueDecomposition(a, false, false).TwoNorm;
        }
    }
}