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
    ///     Wilcoxon signed-rank test for the median.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The Wilcoxon signed-rank test is a non-parametric statistical hypothesis test
    ///         used when comparing two related samples, matched samples, or repeated measurements
    ///         on a single sample to assess whether their population mean ranks differ (i.e. it is
    ///         a paired difference test). It can be used as an alternative to the paired
    ///         <see cref="TTest">
    ///             Student's t-test
    ///         </see>
    ///         , <see cref="PairedTTest">t-test for matched pairs</see>, or the t-test
    ///         for dependent samples when the population cannot be assumed to be normally distributed.
    ///     </para>
    ///     <para>
    ///         The Wilcoxon signed-rank test is not the same as the
    ///         <see cref="WilcoxonTest">
    ///             Wilcoxon rank-sum
    ///             test
    ///         </see>
    ///         , although both are nonparametric and involve summation of ranks.
    ///     </para>
    ///     <para>
    ///         This test uses the positive W statistic, as explained in
    ///         https://onlinecourses.science.psu.edu/stat414/node/319
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     Wikipedia, The Free Encyclopedia. Wilcoxon signed-rank test. Available on:
    ///                     http://en.wikipedia.org/wiki/Wilcoxon_signed-rank_test
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
    /// WilcoxonSignedRankTest test = new WilcoxonSignedRankTest(sample,
    ///   hypothesizedMedian, OneSampleHypothesis.ValueIsSmallerThanHypothesis);
    /// 
    /// // Now, we can check whether this result would be
    /// // unlikely under a standard significance level:
    /// 
    /// bool significant = test.Significant; // false (so the event was likely)
    /// 
    /// // We can also check the test statistic and its P-Value
    /// double statistic = test.Statistic; // 40.0
    /// double pvalue = test.PValue; // 0.98585347446367344
    /// </code>
    /// </example>
    /// <seealso cref="TTest" />
    /// <seealso cref="SignTest" />
    /// <seealso cref="WilcoxonTest" />
    /// <seealso cref="TwoSampleWilcoxonSignedRankTest" />
    /// <seealso cref="Distributions.Univariate.WilcoxonDistribution" />
    [Serializable]
    public class WilcoxonSignedRankTest : WilcoxonTest
    {
        /// <summary>
        ///     Tests the null hypothesis that the sample median is equal to a hypothesized value.
        /// </summary>
        /// <param name="sample">The data samples from which the test will be performed.</param>
        /// <param name="hypothesizedMedian">The constant to be compared with the samples.</param>
        /// <param name="alternate">The alternative hypothesis (research hypothesis) to test.</param>
        /// <param name="exact">
        ///     True to compute the exact distribution. May require a significant
        ///     amount of processing power for large samples (n > 12). If left at null, whether to
        ///     compute the exact or approximate distribution will depend on the number of samples.
        ///     Default is null.
        /// </param>
        /// <param name="adjustForTies">
        ///     Whether to account for ties when computing the
        ///     rank statistics or not. Default is true.
        /// </param>
        public WilcoxonSignedRankTest(double[] sample, double hypothesizedMedian = 0,
            OneSampleHypothesis alternate = OneSampleHypothesis.ValueIsDifferentFromHypothesis,
            bool? exact = null, bool adjustForTies = true)
        {
            var signs = new int[sample.Length];
            var diffs = new double[sample.Length];

            // 1. Compute absolute difference and sign function
            for (var i = 0; i < sample.Length; i++)
            {
                var d = sample[i] - hypothesizedMedian;
                signs[i] = System.Math.Sign(d);
                diffs[i] = System.Math.Abs(d);
            }

            Hypothesis = alternate;

            Compute(signs, diffs, (DistributionTail)alternate, exact, adjustForTies);
        }

        /// <summary>
        ///     Gets the alternative hypothesis under test. If the test is
        ///     <see cref="IHypothesisTest.Significant" />, the null hypothesis can be rejected
        ///     in favor of this alternative hypothesis.
        /// </summary>
        public OneSampleHypothesis Hypothesis { get; protected set; }
    }
}