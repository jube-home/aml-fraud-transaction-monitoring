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

using System.Threading;

namespace Accord
{
    /// <summary>
    ///     An extension of <see cref="Interlocked" /> providing
    ///     atomic operations such as Add and Increment to floating point numbers.
    /// </summary>
    public class InterlockedEx
    {
        /// <summary>
        ///     Adds two 32-bit floating point values and replaces the first
        ///     double value with their sum, as an atomic operation.
        /// </summary>
        /// <param name="location1">The first variable to be added.</param>
        /// <param name="value">The second variable to be added.</param>
        /// <returns>The updated value of the first variable.</returns>
        public static double Add(ref double location1, double value)
        {
            double newCurrentValue = 0;
            while (true)
            {
                var currentValue = newCurrentValue;
                var newValue = currentValue + value;
                newCurrentValue = Interlocked.CompareExchange(ref location1, newValue, currentValue);
                if (newCurrentValue == currentValue)
                {
                    return newValue;
                }
            }
        }

        /// <summary>
        ///     Increments a specified variable and stores the result, as an atomic operation.
        /// </summary>
        /// <param name="location1">The variable to be incremented.</param>
        /// <returns>The updated value of the variable.</returns>
        public static double Increment(ref double location1)
        {
            double newCurrentValue = 0;
            while (true)
            {
                var currentValue = newCurrentValue;
                var newValue = currentValue + 1;
                newCurrentValue = Interlocked.CompareExchange(ref location1, newValue, currentValue);
                if (newCurrentValue == currentValue)
                {
                    return newValue;
                }
            }
        }
    }
}