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
using System.Collections;

namespace Accord.Math.Distances
{
    /// <summary>
    ///     Hamming distance.
    /// </summary>
    [Serializable]
    public struct Hamming : IMetric<byte[]>, IMetric<string>, IDistance<double[]>, IMetric<BitArray>, ICloneable
    {
        /// <summary>
        ///     Computes the distance <c>d(x,y)</c> between points
        ///     <paramref name="x" /> and <paramref name="y" />.
        /// </summary>
        /// <param name="x">The first point <c>x</c>.</param>
        /// <param name="y">The second point <c>y</c>.</param>
        /// <returns>
        ///     A double-precision value representing the distance <c>d(x,y)</c>
        ///     between <paramref name="x" /> and <paramref name="y" /> according
        ///     to the distance function implemented by this class.
        /// </returns>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public double Distance(byte[] x, byte[] y)
        {
            var sum = 0;
            for (var i = 0; i < x.Length; i++)
            {
                sum += lookup[(byte)(x[i] ^ y[i])];
            }

            return sum;
        }

        /// <summary>
        ///     Computes the distance <c>d(x,y)</c> between points
        ///     <paramref name="x" /> and <paramref name="y" />.
        /// </summary>
        /// <param name="x">The first point <c>x</c>.</param>
        /// <param name="y">The second point <c>y</c>.</param>
        /// <returns>
        ///     A double-precision value representing the distance <c>d(x,y)</c>
        ///     between <paramref name="x" /> and <paramref name="y" /> according
        ///     to the distance function implemented by this class.
        /// </returns>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public double Distance(string x, string y)
        {
            var sum = 0;
            for (var i = 0; i < x.Length; i++)
            {
                sum += lookup[(byte)(x[i] ^ y[i])];
            }

            return sum;
        }

        /// <summary>
        ///     Computes the distance <c>d(x,y)</c> between points
        ///     <paramref name="x" /> and <paramref name="y" />.
        /// </summary>
        /// <param name="x">The first point <c>x</c>.</param>
        /// <param name="y">The second point <c>y</c>.</param>
        /// <returns>
        ///     A double-precision value representing the distance <c>d(x,y)</c>
        ///     between <paramref name="x" /> and <paramref name="y" /> according
        ///     to the distance function implemented by this class.
        /// </returns>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public double Distance(double[] x, double[] y)
        {
            var sum = 0;

            for (var i = 0; i < x.Length; i++)
            {
                var bits = BitConverter.DoubleToInt64Bits(x[i]) ^ BitConverter.DoubleToInt64Bits(y[i]);

                for (var b = 0; b < sizeof(double); b++)
                {
                    sum += lookup[(byte)(bits >> (b * 8))];
                }
            }

            return sum;
        }

        /// <summary>
        ///     Computes the distance <c>d(x,y)</c> between points
        ///     <paramref name="x" /> and <paramref name="y" />.
        /// </summary>
        /// <param name="x">The first point <c>x</c>.</param>
        /// <param name="y">The second point <c>y</c>.</param>
        /// <returns>
        ///     A double-precision value representing the distance <c>d(x,y)</c>
        ///     between <paramref name="x" /> and <paramref name="y" /> according
        ///     to the distance function implemented by this class.
        /// </returns>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public double Distance(BitArray x, BitArray y)
        {
            var bytes = x.Xor(y);


            byte b = 0;
            double sum = 0;
            for (int i = 0, j = 0; i < bytes.Length; i++, j++)
            {
                if (bytes[i])
                {
                    b |= (byte)(1 << (8 - j));
                }

                if (j == 8)
                {
                    sum += lookup[b];
                    j = 0;
                    b = 0;
                }
            }

            return sum;
        }

        private static readonly byte[] lookup =
        {
            0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4,
            1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5,
            1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5,
            2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6,
            1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5,
            2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6,
            2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6,
            3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7,
            1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5,
            2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6,
            2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6,
            3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7,
            2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6,
            3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7,
            3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7,
            4, 5, 5, 6, 5, 6, 6, 7, 5, 6, 6, 7, 6, 7, 7, 8
        };

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public object Clone()
        {
            return new Hamming();
        }
    }
}