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

using Accord.Statistics.Running;

namespace Accord.Statistics.Moving
{
    /// <summary>
    ///     Common interface for moving-window statistics.
    /// </summary>
    /// <remarks>
    ///     Moving-window statistics such as moving average and moving variance,
    ///     are a type of finite impulse response filters used to analyze a set
    ///     of data points by creating a series of averages of different subsets
    ///     of the full data set.
    /// </remarks>
    public interface IMoving<in TValue> : IRunning<TValue>
    {
        /// <summary>
        ///     Gets the size of the window.
        /// </summary>
        /// <value>The window's size.</value>
        int Window { get; }

        /// <summary>
        ///     Gets the number of samples within the window.
        /// </summary>
        /// <value>The number of samples within the window.</value>
        int Count { get; }
    }
}