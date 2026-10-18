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

namespace Accord.Statistics.Testing
{
    /// <summary>
    ///     Sign test for the median.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         In statistics, the sign test can be used to test the hypothesis that the difference
    ///         median is zero between the continuous distributions of two random variables X and Y,
    ///         in the situation when we can draw paired samples from X and Y. It is a non-parametric
    ///         test which makes very few assumptions about the nature of the distributions under test
    ///         - this means that it has very general applicability but may lack the
    ///         <see cref="Power">
    ///             statistical power
    ///         </see>
    ///         of other tests such as the
    ///         <see cref="PairedTTest">
    ///             paired-samples
    ///             t-test
    ///         </see>
    ///         or the <see cref="WilcoxonSignedRankTest">Wilcoxon signed-rank test</see>.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     Wikipedia, The Free Encyclopedia. Sign test. Available on:
    ///                     http://en.wikipedia.org/wiki/Sign_test
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // This example has been adapted from the Wikipedia's page about
    /// // the Z-Test, available from: http://en.wikipedia.org/wiki/Z-test
    /// 
    /// // We would like to check whether a sample of 20
    /// // students with a median score of 96 points ...
    /// 
    /// double[] sample = 
    /// { 
    ///     106, 115, 96, 88, 91, 88, 81, 104, 99, 68,
    ///     104, 100, 77, 98, 96, 104, 82, 94, 72, 96
    /// };
    /// 
    /// // ... could have happened just by chance inside a 
    /// // population with an hypothesized median of 100 points.
    /// 
    /// double hypothesizedMedian = 100;
    /// 
    /// // So we start by creating the test:
    /// SignTest test = new SignTest(sample, hypothesizedMedian,
    ///                 OneSampleHypothesis.ValueIsSmallerThanHypothesis);
    /// 
    /// // Now, we can check whether this result would be
    /// // unlikely under a standard significance level:
    /// 
    /// bool significant = test.Significant; // true (the median is below 100)
    /// 
    /// // We can also check the test statistic and its P-Value
    /// double statistic = test.Statistic; // 5
    /// double pvalue = test.PValue; // 0.03178
    /// </code>
    /// </example>
    /// <seealso cref="WilcoxonSignedRankTest" />
    /// <seealso cref="BinomialTest" />
    /// <seealso cref="Distributions.Univariate.BinomialDistribution" />
    [Serializable]
    public class SignTest : BinomialTest
    {
        /// <summary>
        ///     Tests the null hypothesis that the sample median is equal to a hypothesized value.
        /// </summary>
        /// <param name="positiveSamples">The number of positive samples.</param>
        /// <param name="totalSamples">The total number of samples.</param>
        /// <param name="alternate">The alternative hypothesis (research hypothesis) to test.</param>
        public SignTest(int positiveSamples, int totalSamples, OneSampleHypothesis alternate)
        {
            Compute(positiveSamples, totalSamples, alternate);
        }

        /// <summary>
        ///     Tests the null hypothesis that the sample median is equal to a hypothesized value.
        /// </summary>
        /// <param name="sample">The data samples from which the test will be performed.</param>
        /// <param name="hypothesizedMedian">The constant to be compared with the samples.</param>
        /// <param name="alternate">The alternative hypothesis (research hypothesis) to test.</param>
        public SignTest(double[] sample, double hypothesizedMedian = 0,
            OneSampleHypothesis alternate = OneSampleHypothesis.ValueIsDifferentFromHypothesis)
        {
            var positive = 0;
            var negative = 0;

            for (var i = 0; i < sample.Length; i++)
            {
                var d = sample[i] - hypothesizedMedian;

                if (d > 0)
                {
                    positive++;
                }
                else if (d < 0)
                {
                    negative++;
                }
            }


            Compute(positive, positive + negative, alternate);
        }

        /// <summary>
        ///     Gets the alternative hypothesis under test. If the test is
        ///     <see cref="IHypothesisTest.Significant" />, the null hypothesis
        ///     can be rejected in favor of this alternative hypothesis.
        /// </summary>
        public new OneSampleHypothesis Hypothesis { get; protected set; }

        /// <summary>
        ///     Computes the one sample sign test.
        /// </summary>
        protected void Compute(int positive, int total, OneSampleHypothesis alternate)
        {
            Hypothesis = alternate;

            Compute(positive, total, 0.5, alternate);
        }
    }
}