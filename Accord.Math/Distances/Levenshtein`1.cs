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

namespace Accord.Math.Distances
{
    /// <summary>
    ///     Levenshtein distance.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         In information theory and computer science, the Levenshtein distance is a
    ///         string metric for measuring the difference between two sequences. Informally,
    ///         the Levenshtein distance between two words is the minimum number of single-character
    ///         edits (i.e. insertions, deletions or substitutions) required to change one
    ///         word into the other. It is named after Vladimir Levenshtein, who considered
    ///         this distance in 1965.
    ///     </para>
    ///     <para>
    ///         Levenshtein distance may also be referred to as edit distance, although that
    ///         may also denote a larger family of distance metrics. It is closely related to
    ///         pairwise string alignments.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="https://en.wikipedia.org/wiki/Levenshtein_distance">
    ///                         https://en.wikipedia.org/wiki/Levenshtein_distance
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <typeparam name="T">The type of elements in the string. Default is char.</typeparam>
    [Serializable]
    public struct Levenshtein<T> : IMetric<T[]>, ICloneable
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
        public double Distance(T[] x, T[] y)
        {
            if (x == null || x.Length == 0)
            {
                if (y == null || y.Length == 0)
                {
                    return 0;
                }

                return y.Length;
            }

            if (y == null || y.Length == 0)
            {
                return x.Length;
            }

            var d = new int[x.Length + 1, y.Length + 1];

            for (var i = 0; i <= x.Length; i++)
            {
                d[i, 0] = i;
            }

            for (var i = 0; i <= y.Length; i++)
            {
                d[0, i] = i;
            }

            for (var i = 0; i < x.Length; i++)
            {
                for (var j = 0; j < y.Length; j++)
                {
                    var cost = x[i].Equals(y[j]) ? 0 : 1;

                    var a = d[i, j + 1] + 1;
                    var b = d[i + 1, j] + 1;
                    var c = d[i, j] + cost;

                    d[i + 1, j + 1] = System.Math.Min(System.Math.Min(a, b), c);
                }
            }

            return d[x.Length, y.Length];
        }

        /// <summary>
        ///     Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public object Clone()
        {
            return new Levenshtein<T>();
        }
    }
}