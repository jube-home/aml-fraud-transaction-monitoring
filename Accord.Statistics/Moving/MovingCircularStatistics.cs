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
using System.Collections.Generic;

namespace Accord.Statistics.Moving
{
    /// <summary>
    ///     Moving-window circular statistics.
    /// </summary>
    [Serializable]
    public class MovingCircularStatistics : IMovingStatistics
    {
        private Queue<double> cosines;
        private Queue<double> sines;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MovingCircularStatistics" /> class.
        /// </summary>
        /// <param name="windowSize">The size of the moving window.</param>
        public MovingCircularStatistics(int windowSize)
        {
            if (windowSize < 0 || windowSize == int.MaxValue)
            {
                throw new ArgumentOutOfRangeException("windowSize");
            }

            Window = windowSize;
            sines = new Queue<double>(windowSize + 1);
            cosines = new Queue<double>(windowSize + 1);
        }

        /// <summary>
        ///     Gets the sum of the sines of the angles within the window.
        /// </summary>
        public double SumOfSines { get; private set; }

        /// <summary>
        ///     Gets the sum of the cosines of the angles within the window.
        /// </summary>
        public double SumOfCosines { get; private set; }

        /// <summary>
        ///     Gets the current length of the sample mean resultant vector of the gathered values.
        /// </summary>
        public double Rho { get; private set; }

        /// <summary>
        ///     Gets the size of the window.
        /// </summary>
        /// <value>The window's size.</value>
        public int Window { get; private set; }

        /// <summary>
        ///     Gets the number of samples within the window.
        /// </summary>
        /// <value>The number of samples within the window.</value>
        public int Count => sines.Count;

        /// <summary>
        ///     Gets the mean of the angles within the window.
        /// </summary>
        /// <value>The mean.</value>
        public double Mean { get; private set; }

        /// <summary>
        ///     Gets the variance of the angles within the window.
        /// </summary>
        public double Variance { get; private set; }

        /// <summary>
        ///     Gets the standard deviation of the angles within the window.
        /// </summary>
        public double StandardDeviation { get; private set; }

        /// <summary>
        ///     Registers the occurrence of a value.
        /// </summary>
        /// <param name="value">The value to be registered.</param>
        public void Push(double value)
        {
            if (sines.Count == Window)
            {
                SumOfSines -= sines.Dequeue();
                SumOfCosines -= cosines.Dequeue();
            }

            var cos = System.Math.Cos(value);
            var sin = System.Math.Sin(value);

            sines.Enqueue(sin);
            cosines.Enqueue(cos);

            SumOfSines += sin;
            SumOfCosines += cos;

            double N = sines.Count;

            Rho = System.Math.Sqrt(SumOfSines * SumOfSines + SumOfCosines * SumOfCosines);
            Mean = System.Math.Atan2(SumOfSines / N, SumOfCosines / N);
            Variance = System.Math.Max(0, 1.0 - Rho / N);
            StandardDeviation = System.Math.Sqrt(-2.0 * System.Math.Log(Rho / N));
        }

        /// <summary>
        ///     Clears all measures previously computed.
        /// </summary>
        public void Clear()
        {
            sines.Clear();
            cosines.Clear();

            Mean = 0;
            Variance = 0;
            StandardDeviation = 0;
            Rho = 0;

            SumOfSines = 0;
            SumOfCosines = 0;
        }
    }
}