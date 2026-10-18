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

namespace Accord.Math
{
    public static partial class Vector
    {
        /// <summary>
        ///     Gets the inner product (scalar product) between two vectors (a'*b).
        /// </summary>
        /// <param name="a">A vector.</param>
        /// <param name="b">A vector.</param>
        /// <returns>The inner product of the multiplication of the vectors.</returns>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static double Dot(this Sparse<double> a, Sparse<double> b)
        {
            double sum = 0;

            int i = 0, j = 0;

            while (i < a.Indices.Length && j < b.Indices.Length)
            {
                var posx = a.Indices[i];
                var posy = b.Indices[j];

                if (posx == posy)
                {
                    sum += a.Values[i] * b.Values[j];
                    i++;
                    j++;
                }
                else if (posx < posy)
                {
                    i++;
                }
                else //if (posx > posy)
                {
                    j++;
                }
            }

            return sum;
        }

        /// <summary>
        ///     Gets the inner product (scalar product) between two vectors (a'*b).
        /// </summary>
        /// <param name="a">A vector.</param>
        /// <param name="b">A vector.</param>
        /// <returns>The inner product of the multiplication of the vectors.</returns>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static double Dot(this Sparse<double> a, double[] b)
        {
            double sum = 0;

            int i = 0, j = 0;

            while (i < a.Indices.Length && j < b.Length)
            {
                var posx = a.Indices[i];
                var posy = j;

                if (posx == posy)
                {
                    sum += a.Values[i] * b[j];
                    i++;
                    j++;
                }
                else if (posx < posy)
                {
                    i++;
                }
                else // if (posx > posy)
                {
                    j++;
                }
            }

            return sum;
        }

        /// <summary>
        ///     Adds a sparse vector to a dense vector.
        /// </summary>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static double[] Add(this Sparse<double> a, double[] b, double[] result)
        {
            for (var j = 0; j < b.Length; j++)
            {
                result[j] = b[j];
            }

            for (var j = 0; j < a.Indices.Length; j++)
            {
                result[a.Indices[j]] += a.Values[j];
            }

            return result;
        }

        /// <summary>
        ///     Divides an array of sparse vectors by the associated scalars in a dense vector.
        /// </summary>
        public static Sparse<double>[] Divide(this Sparse<double>[] a, double[] b, Sparse<double>[] result)
        {
            for (var i = 0; i < a.Length; i++)
            {
                a[i].Values.Divide(b[i], result[i].Values);
            }

            return result;
        }

        /// <summary>
        ///     Divides a sparse vector by a scalar.
        /// </summary>
        public static Sparse<double> Divide(this Sparse<double> a, double b, Sparse<double> result)
        {
            a.Values.Divide(b, result.Values);
            return result;
        }
    }
}