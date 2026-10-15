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

namespace Accord.Math.Optimization
{
    /// <summary>
    ///     Binary search root finding algorithm.
    /// </summary>
    public class BinarySearch
    {
        /// <summary>
        ///     Constructs a new Binary search algorithm.
        /// </summary>
        /// <param name="function">The function to be searched.</param>
        /// <param name="a">Start of search region (inclusive).</param>
        /// <param name="b">End of search region (exclusive).</param>
        public BinarySearch(Func<int, double> function, int a, int b)
        {
            Function = function;
            LowerBound = a;
            UpperBound = b;
        }

        /// <summary>
        ///     Gets or sets the (inclusive) lower bound for the search interval <c>a</c>.
        /// </summary>
        public int LowerBound { get; set; }

        /// <summary>
        ///     Gets or sets the (exclusive) upper bound for the search interval <c>a</c>.
        /// </summary>
        public int UpperBound { get; set; }

        /// <summary>
        ///     Gets the solution found in the last call
        ///     to or <see cref="FindRoot()" />.
        /// </summary>
        public int Solution { get; private set; }

        /// <summary>
        ///     Gets the value at the solution found in the last call
        ///     to <see cref="FindRoot()" />.
        /// </summary>
        public double Value { get; private set; }

        /// <summary>
        ///     Gets the function to be searched.
        /// </summary>
        public Func<int, double> Function { get; }

        /// <summary>
        ///     Finds a value of a function in the interval [a;b). The function can
        ///     be monotonically increasing or decreasing over the interface [a;b).
        /// </summary>
        /// <returns>The location of the zero value in the given interval.</returns>
        public int Find(double value)
        {
            Solution = Find(Function, LowerBound, UpperBound, value);
            Value = Function(Solution);
            return Solution;
        }

        /// <summary>
        ///     Finds a value of a function in the interval [a;b). The function can
        ///     be monotonically increasing or decreasing over the interface [a;b).
        /// </summary>
        /// <returns>The location of the zero value in the given interval.</returns>
        public int FindRoot()
        {
            Solution = Find(Function, LowerBound, UpperBound, 0);
            Value = Function(Solution);
            return Solution;
        }

        /// <summary>
        ///     Finds a value of a function in the interval [a;b). The function can
        ///     be monotonically increasing or decreasing over the interface [a;b).
        /// </summary>
        /// <param name="function">The function to have its root computed.</param>
        /// <param name="lowerBound">Start of search region (inclusive).</param>
        /// <param name="upperBound">End of search region (exclusive).</param>
        /// <param name="value">The value to be looked for in the function.</param>
        /// <returns>The location of the zero value in the given interval.</returns>
        public static int Find(Func<int, double> function, int lowerBound, int upperBound, double value)
        {
            var start = lowerBound;
            var end = upperBound;

            // if sign is positive we assume an increasing function
            // otherwise we assume a decreasing function.
            var sign = function(start) <= function(end - 1) ? 1 : -1;

            value = sign * value;

            var m = (int)((start + (long)end) / 2);

            var v = sign * function(m);

            while (end >= start)
            {
                if (v < value)
                {
                    start = m + 1;
                }
                else if (v > value)
                {
                    end = m - 1;
                }
                else
                {
                    return m;
                }

                m = (int)((start + (long)end) / 2);
                v = sign * function(m);
            }

            if (v > value || m == upperBound)
            {
                return m;
            }

            return m + 1;
        }
    }
}