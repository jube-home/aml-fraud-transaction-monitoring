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
    ///     Common interface for convergence detection algorithms.
    /// </summary>
    public interface IConvergence<T> : IConvergence
    {
        /// <summary>
        ///     Please use MaxIterations instead.
        /// </summary>
        [Obsolete("Please use MaxIterations instead.")]
        int Iterations { get; set; }

        /// <summary>
        ///     Gets or sets the watched value after the iteration.
        /// </summary>
        T NewValue { get; set; }
    }

    /// <summary>
    ///     Common interface for convergence detection algorithms.
    /// </summary>
    public interface IConvergence
    {
        /// <summary>
        ///     Gets or sets the maximum relative change in the watched value
        ///     after an iteration of the algorithm used to detect convergence.
        /// </summary>
        double Tolerance { get; set; }

        /// <summary>
        ///     Gets or sets the maximum number of iterations
        ///     performed by the iterative algorithm.
        /// </summary>
        int MaxIterations { get; set; }

        /// <summary>
        ///     Gets the current iteration number.
        /// </summary>
        int CurrentIteration { get; }

        /// <summary>
        ///     Gets or sets whether the algorithm has converged.
        /// </summary>
        bool HasConverged { get; }

        /// <summary>
        ///     Resets this instance, reverting all iteration statistics
        ///     statistics (number of iterations, last error) back to zero.
        /// </summary>
        void Clear();
    }
}