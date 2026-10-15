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
using System.Globalization;
using Accord.Statistics.Distributions;

namespace Accord.Statistics.Testing
{
    /// <summary>
    ///     Base class for Hypothesis Tests.
    /// </summary>
    /// <remarks>
    ///     A statistical hypothesis test is a method of making decisions using data, whether from
    ///     a controlled experiment or an observational study (not controlled). In statistics, a
    ///     result is called statistically significant if it is unlikely to have occurred by chance
    ///     alone, according to a pre-determined threshold probability, the significance level.
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Statistical_hypothesis_testing">
    ///                         Wikipedia, The Free Encyclopedia. Statistical Hypothesis Testing.
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    [Serializable]
    public abstract class HypothesisTest<TDistribution> : IFormattable, IHypothesisTest<TDistribution>
        where TDistribution : IUnivariateDistribution
    {
        private double alpha = 0.05;

        /// <summary>
        ///     Initializes a new instance of the class.
        /// </summary>
        protected HypothesisTest()
        {
        }

        /// <summary>
        ///     Gets the P-value associated with this test.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         In statistical hypothesis testing, the p-value is the probability of
        ///         obtaining a test statistic at least as extreme as the one that was
        ///         actually observed, assuming that the null hypothesis is true.
        ///     </para>
        ///     <para>
        ///         The lower the p-value, the less likely the result can be explained
        ///         by chance alone, assuming the null hypothesis is true.
        ///     </para>
        /// </remarks>
        public double PValue { get; protected set; }

        /// <summary>
        ///     Gets the test statistic.
        /// </summary>
        public double Statistic { get; protected set; }

        /// <summary>
        ///     Gets the significance level for the
        ///     test. Default value is 0.05 (5%).
        /// </summary>
        public double Size
        {
            get => alpha;
            set
            {
                alpha = value;
                OnSizeChanged();
            }
        }

        /// <summary>
        ///     Gets the critical value for the current <see cref="Size">significance level</see>.
        /// </summary>
        public virtual double CriticalValue => PValueToStatistic(alpha);

        /// <summary>
        ///     Converts the numeric P-Value of this test to its equivalent string representation.
        /// </summary>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            return PValue.ToString(format, formatProvider);
        }

        /// <summary>
        ///     Gets the distribution associated
        ///     with the test statistic.
        /// </summary>
        public TDistribution StatisticDistribution { get; protected set; }

        /// <summary>
        ///     Gets the test type.
        /// </summary>
        public DistributionTail Tail { get; protected set; }

        /// <summary>
        ///     Gets whether the null hypothesis should be rejected.
        /// </summary>
        /// <remarks>
        ///     A test result is said to be statistically significant when the
        ///     result would be very unlikely to have occurred by chance alone.
        /// </remarks>
        public bool Significant => PValue < alpha;

        /// <summary>
        ///     Converts a given test statistic to a p-value.
        /// </summary>
        /// <param name="x">The value of the test statistic.</param>
        /// <returns>The p-value for the given statistic.</returns>
        public abstract double StatisticToPValue(double x);

        /// <summary>
        ///     Converts a given p-value to a test statistic.
        /// </summary>
        /// <param name="p">The p-value.</param>
        /// <returns>The test statistic which would generate the given p-value.</returns>
        public abstract double PValueToStatistic(double p);

        /// <summary>
        ///     Called whenever the test <see cref="Size">significance level</see> changes.
        /// </summary>
        protected virtual void OnSizeChanged()
        {
        }

        /// <summary>
        ///     Converts the numeric P-Value of this test to its equivalent string representation.
        /// </summary>
        public override string ToString()
        {
            return PValue.ToString(CultureInfo.CurrentCulture);
        }
    }
}