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

using Accord.Math.Distances;
using Accord.Statistics.Distributions;

namespace Accord.Statistics.Distances
{
    /// <summary>
    ///     Common interface for divergence measures (between
    ///     probability distributions).
    /// </summary>
    /// <typeparam name="T">The type of the first distribution to be compared.</typeparam>
    /// <typeparam name="U">The type of the second distribution to be compared.</typeparam>
    public interface IDivergence<in T, in U> : IDistance<T, U>
        where T : IDistribution
        where U : IDistribution
    {
    }

    /// <summary>
    ///     Common interface for divergence measures (between
    ///     probability distributions).
    /// </summary>
    /// <typeparam name="T">The type of the distributions to be compared.</typeparam>
    public interface IDivergence<in T> : IDivergence<T, T>, IDistance<T>
        where T : IDistribution
    {
    }
}