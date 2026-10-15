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

using Accord.Statistics.Distributions;

namespace Accord.Statistics.Testing
{
    /// <summary>
    ///     Common interface for Hypothesis tests depending on a statistical distribution.
    /// </summary>
    /// <typeparam name="TDistribution">The test statistic distribution.</typeparam>
    public interface IHypothesisTest<out TDistribution> : IHypothesisTest
        where TDistribution : IDistribution
    {
        /// <summary>
        ///     Gets the distribution associated
        ///     with the test statistic.
        /// </summary>
        TDistribution StatisticDistribution { get; }
    }

    /// <summary>
    ///     Common interface for Hypothesis tests depending on a statistical distribution.
    /// </summary>
    public interface IHypothesisTest
    {
        /// <summary>
        ///     Gets the test type.
        /// </summary>
        DistributionTail Tail { get; }

        /// <summary>
        ///     Gets whether the null hypothesis should be rejected.
        /// </summary>
        /// <remarks>
        ///     A test result is said to be statistically significant when the
        ///     result would be very unlikely to have occurred by chance alone.
        /// </remarks>
        bool Significant { get; }

        /// <summary>
        ///     Converts a given test statistic to a p-value.
        /// </summary>
        /// <param name="x">The value of the test statistic.</param>
        /// <returns>The p-value for the given statistic.</returns>
        double StatisticToPValue(double x);

        /// <summary>
        ///     Converts a given p-value to a test statistic.
        /// </summary>
        /// <param name="p">The p-value.</param>
        /// <returns>The test statistic which would generate the given p-value.</returns>
        double PValueToStatistic(double p);
    }
}