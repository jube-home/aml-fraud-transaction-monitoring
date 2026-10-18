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
    /// <remarks>
    ///     Provides statistics derived from successive segments of constant, overlapping size ('windowSize')
    ///     of a series of values. Values are added one at a time to a MovingNormalStatistics instance through
    ///     <see cref="Push(double)" /> method and are actually kept inside the instance.
    /// </remarks>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\MovingNormalStatisticsTest.cs" region="doc_example" />
    /// </example>
    [Serializable]
    public class MovingNormalStatistics : IMovingStatistics, IEnumerable<double>
    {
        private Queue<double> squares;
        private Queue<double> values;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MovingNormalStatistics" /> class.
        /// </summary>
        /// <param name="windowSize">The size of the moving window.</param>
        public MovingNormalStatistics(int windowSize)
        {
            if (windowSize < 0 || windowSize == int.MaxValue)
            {
                throw new ArgumentOutOfRangeException("windowSize");
            }

            Window = windowSize;
            values = new Queue<double>(windowSize + 1);
            squares = new Queue<double>(windowSize + 1);
        }

        /// <summary>
        ///     Gets the sum the values within the window.
        /// </summary>
        /// <value>The sum of values within the window.</value>
        public double Sum { get; private set; }

        /// <summary>
        ///     Gets the sum of squared values within the window.
        /// </summary>
        /// <value>The sum of squared values.</value>
        public double SumOfSquares { get; private set; }

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
        ///     Gets the mean of the values within the window.
        /// </summary>
        /// <value>The mean of the values.</value>
        public double Mean { get; private set; }

        /// <summary>
        ///     Gets the variance of the values within the window.
        /// </summary>
        /// <value>The variance of the values.</value>
        public double Variance { get; private set; }

        /// <summary>
        ///     Gets the standard deviation of the values within the window.
        /// </summary>
        /// <value>The standard deviation of the values.</value>
        public double StandardDeviation => System.Math.Sqrt(Variance);

        /// <summary>
        ///     Pushes a value into the window.
        /// </summary>
        public void Push(double value)
        {
            if (values.Count == Window)
            {
                Sum -= values.Dequeue();
                SumOfSquares -= squares.Dequeue();
            }

            var square = value * value;

            values.Enqueue(value);
            squares.Enqueue(square);

            Sum += value;
            SumOfSquares += square;

            double N = values.Count;

            Mean = Sum / N;
            Variance = (N * SumOfSquares - Sum * Sum) / (N * (N - 1));
        }

        /// <summary>
        ///     Removes all elements from the window and resets statistics.
        /// </summary>
        public void Clear()
        {
            values.Clear();
            squares.Clear();

            Mean = 0;
            Variance = 0;

            SumOfSquares = 0;
            Sum = 0;
        }
    }
}