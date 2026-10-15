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
using System.Collections.Generic;

namespace Accord.Statistics.Moving
{
    /// <summary>
    ///     Moving-window statistics.
    /// </summary>
    [Serializable]
    public class MovingRangeStatistics : IMoving<double>, IEnumerable<double>
    {
        private Queue<double> values;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MovingNormalStatistics" /> class.
        /// </summary>
        /// <param name="windowSize">The size of the moving window.</param>
        public MovingRangeStatistics(int windowSize)
        {
            if (windowSize < 0 || windowSize == int.MaxValue)
            {
                throw new ArgumentOutOfRangeException("windowSize");
            }

            Window = windowSize;
            values = new Queue<double>(windowSize + 1);
        }

        /// <summary>
        ///     Gets the minimum value in the window.
        /// </summary>
        public double Min { get; private set; }

        /// <summary>
        ///     Gets the maximum value in the window.
        /// </summary>
        public double Max { get; private set; }

        /// <summary>
        ///     Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<double> GetEnumerator()
        {
            return values.GetEnumerator();
        }

        /// <summary>
        ///     Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        ///     An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return values.GetEnumerator();
        }

        /// <summary>
        ///     Gets the size of the window.
        /// </summary>
        /// <value>The window's size.</value>
        public int Window { get; private set; }

        /// <summary>
        ///     Gets the number of samples within the window.
        /// </summary>
        /// <value>The number of samples within the window.</value>
        public int Count => values.Count;

        /// <summary>
        ///     Pushes a value into the window.
        /// </summary>
        public void Push(double value)
        {
            if (values.Count == Window)
            {
                values.Dequeue();
            }

            values.Enqueue(value);

            // TODO: Rewrite to avoid iterating on every addition
            Min = value;
            Max = value;
            foreach (var v in values)
            {
                if (v < Min)
                {
                    Min = v;
                }

                if (v > Max)
                {
                    Max = v;
                }
            }
        }

        /// <summary>
        ///     Removes all elements from the window and resets statistics.
        /// </summary>
        public void Clear()
        {
            values.Clear();
            Min = Max = 0;
        }
    }
}