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

namespace Accord.Statistics.Running
{
    /// <summary>
    ///     Running circular statistics.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class computes the running variance using Welford’s method. Running statistics
    ///         need only one pass over the data, and do not require all data to be available prior
    ///         to computing.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://www.johndcook.com/standard_deviation.html">
    ///                         John D. Cook. Accurately computing running variance. Available on:
    ///                         http://www.johndcook.com/standard_deviation.html
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Chan, Tony F.; Golub, Gene H.; LeVeque, Randall J. (1983). Algorithms for
    ///                     Computing the Sample Variance: Analysis and Recommendations. The American
    ///                     Statistician 37, 242-247.
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Ling, Robert F. (1974). Comparison of Several Algorithms for Computing Sample
    ///                     Means and Variances. Journal of the American Statistical Association, Vol. 69,
    ///                     No. 348, 859-866.
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    [Serializable]
    public class RunningCircularStatistics : IRunningStatistics
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RunningCircularStatistics" /> class.
        /// </summary>
        public RunningCircularStatistics()
        {
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
        ///     Gets the current count of values seen.
        /// </summary>
        /// <value>The number of samples seen.</value>
        public int Count { get; private set; }

        /// <summary>
        ///     Gets the current mean of the gathered values.
        /// </summary>
        /// <value>The mean of the values.</value>
        public double Mean { get; private set; }

        /// <summary>
        ///     Gets the current variance of the gathered values.
        /// </summary>
        /// <value>The variance of the values.</value>
        public double Variance { get; private set; }

        /// <summary>
        ///     Gets the current standard deviation of the gathered values.
        /// </summary>
        /// <value>The standard deviation of the values.</value>
        public double StandardDeviation { get; private set; }

        /// <summary>
        ///     Registers the occurrence of a value.
        /// </summary>
        /// <param name="value">The value to be registered.</param>
        public void Push(double value)
        {
            Count++;

            var cos = System.Math.Cos(value);
            var sin = System.Math.Sin(value);

            SumOfSines += sin;
            SumOfCosines += cos;

            double N = Count;

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
            Count = 0;

            Rho = 0;
            Mean = 0;
            Variance = 0;
            StandardDeviation = 0;

            SumOfSines = 0;
            SumOfCosines = 0;
        }
    }
}