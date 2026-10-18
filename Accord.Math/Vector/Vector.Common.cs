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
        ///     Creates a vector containing every index that can be used to
        ///     address a given <paramref name="array" />, in order.
        /// </summary>
        /// <param name="array">The array whose indices will be returned.</param>
        /// <returns>
        ///     A vector of the same size as the given <paramref name="array" />
        ///     containing all vector indices from 0 up to the length of
        ///     <paramref name="array" />.
        /// </returns>
        /// <example>
        ///     <code>
        ///   double[] a = { 5.3, 2.3, 4.2 };
        ///   int[] idx = a.GetIndices(); // output will be { 0, 1, 2 }
        /// </code>
        /// </example>
        /// <seealso cref="Matrix.GetIndices" />
        public static int[] GetIndices<T>(this T[] array)
        {
            return Range(0, array.Length);
        }
    }
}