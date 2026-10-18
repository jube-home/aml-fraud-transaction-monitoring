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

namespace Accord.MachineLearning
{
    /// <summary>
    ///     Common interface for Bag of Words objects.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the element to be
    ///     converted to a fixed-length vector representation.
    /// </typeparam>
    /// <seealso cref="BagOfWords" />
    public interface IBagOfWords<T> : ITransform<T, double[]>, ITransform<T, int[]>
    {
        /// <summary>
        ///     Gets the number of words in this codebook.
        /// </summary>
        int NumberOfWords { get; }

        /// <summary>
        ///     Gets the codeword representation of a given value.
        /// </summary>
        /// <param name="value">The value to be processed.</param>
        /// <returns>
        ///     A double vector with the same length as words
        ///     in the code book.
        /// </returns>
        [Obsolete("Please use the Transform(value) method instead.")]
        double[] GetFeatureVector(T value);
    }
}