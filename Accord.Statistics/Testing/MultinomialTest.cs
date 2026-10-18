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
using Accord.Math;
using Accord.Statistics.Distributions.Univariate;

namespace Accord.Statistics.Testing
{
    /// <summary>
    ///     Multinomial test (approximated).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         In statistics, the multinomial test is the test of the null hypothesis that the
    ///         parameters of a multinomial distribution equal specified values. The test can be
    ///         approximated using a <see cref="ChiSquareDistribution">chi-square distribution</see>.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Multinomial_test">
    ///                         Wikipedia, The Free Encyclopedia. Multinomial Test. Available on:
    ///                         http://en.wikipedia.org/wiki/Multinomial_test
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The following example is based on the example available on About.com Statistics,
    ///         <a
    ///             href="http://statistics.about.com/od/Inferential-Statistics/a/An-Example-Of-Chi-Square-Test-For-A-Multinomial-Experiment.htm">
    ///             An
    ///             Example of Chi-Square Test for a Multinomial Experiment
    ///         </a>
    ///         By Courtney Taylor.
    ///     </para>
    ///     <para>
    ///         In this example, we would like to test if a die is fair. For this, we
    ///         will be rolling the die 600 times, annotating the result every time
    ///         the die falls. In the end, we got a one 106 times, a two 90 times, a
    ///         three 98 times, a four 102 times, a five 100 times and a six 104 times:
    ///     </para>
    ///     <code>
    /// int[] sample = { 106, 90, 98, 102, 100, 104 };
    /// 
    /// // If the die was fair, we should note that we would be expecting the
    /// // probabilities to be all equal to 1 / 6:
    /// 
    /// double[] hypothesizedProportion = 
    /// { 
    ///    //   1        2           3          4          5         6
    ///    1 / 6.0,   1 / 6.0,   1 / 6.0,   1 / 6.0,   1 / 6.0,   1 / 6.0, 
    /// };
    /// 
    /// // Now, we create our test using the samples and the expected proportion
    /// MultinomialTest test = new MultinomialTest(sample, hypothesizedProportion);
    /// 
    /// double chiSquare = test.Statistic; // 1.6
    /// bool significant = test.Significant; // false
    /// </code>
    ///     <para>
    ///         Since the test didn't come up significant, it means that we
    ///         don't have enough evidence to to reject the null hypothesis
    ///         that the die is fair.
    ///     </para>
    /// </example>
    /// <seealso cref="ChiSquareTest" />
    /// <seealso cref="ChiSquareDistribution" />
    [Serializable]
    public class MultinomialTest : ChiSquareTest
    {
        /// <summary>
        ///     Creates a new Multinomial test.
        /// </summary>
        /// <param name="sampleProportions">The proportions for each category in the sample.</param>
        /// <param name="sampleSize">The number of observations in the sample.</param>
        public MultinomialTest(double[] sampleProportions, int sampleSize)
        {
            var hypothesizedProportions = new double[sampleProportions.Length];
            for (var i = 0; i < hypothesizedProportions.Length; i++)
            {
                hypothesizedProportions[i] = 1.0 / hypothesizedProportions.Length;
            }

            Compute(sampleSize, sampleProportions, hypothesizedProportions);
        }

        /// <summary>
        ///     Creates a new Multinomial test.
        /// </summary>
        /// <param name="sampleCounts">The number of occurrences for each category in the sample.</param>
        public MultinomialTest(int[] sampleCounts)
        {
            var hypothesizedProportions = new double[sampleCounts.Length];
            for (var i = 0; i < hypothesizedProportions.Length; i++)
            {
                hypothesizedProportions[i] = 1.0 / hypothesizedProportions.Length;
            }

            var sampleSize = sampleCounts.Sum();
            var sampleProportions = sampleCounts.Divide(sampleSize);

            Compute(sampleSize, sampleProportions, hypothesizedProportions);
        }

        /// <summary>
        ///     Creates a new Multinomial test.
        /// </summary>
        /// <param name="sampleCounts">The number of occurrences for each category in the sample.</param>
        /// <param name="hypothesizedProportions">
        ///     The hypothesized category proportions. Default is
        ///     to assume uniformly equal proportions.
        /// </param>
        public MultinomialTest(int[] sampleCounts, double[] hypothesizedProportions)
        {
            if (sampleCounts.Length != hypothesizedProportions.Length)
            {
                throw new DimensionMismatchException("hypothesizedProportions");
            }

            var sampleSize = sampleCounts.Sum();
            var sampleProportions = sampleCounts.Divide(sampleSize);

            Compute(sampleSize, sampleProportions, hypothesizedProportions);
        }

        /// <summary>
        ///     Creates a new Multinomial test.
        /// </summary>
        /// <param name="sampleProportions">The proportions for each category in the sample.</param>
        /// <param name="sampleSize">The number of observations in the sample.</param>
        /// <param name="hypothesizedProportions">
        ///     The hypothesized category proportions. Default is
        ///     to assume uniformly equal proportions.
        /// </param>
        public MultinomialTest(double[] sampleProportions, int sampleSize, double[] hypothesizedProportions)
        {
            if (sampleProportions.Length != hypothesizedProportions.Length)
            {
                throw new DimensionMismatchException("hypothesizedProportions");
            }

            Compute(sampleSize, sampleProportions, hypothesizedProportions);
        }

        /// <summary>
        ///     Creates a new Multinomial test.
        /// </summary>
        /// <param name="sample">The categories for each observation in the sample.</param>
        /// <param name="categories">The number of possible categories.</param>
        public MultinomialTest(int[] sample, int categories)
        {
            var hypothesizedProportions = new double[categories];
            for (var i = 0; i < hypothesizedProportions.Length; i++)
            {
                hypothesizedProportions[i] = 1.0 / hypothesizedProportions.Length;
            }

            var observed = new double[categories];
            for (var i = 0; i < sample.Length; i++)
            {
                observed[sample[i]]++;
            }

            Compute(sample.Length, observed.Divide(sample.Length), hypothesizedProportions);
        }

        /// <summary>
        ///     Creates a new Multinomial test.
        /// </summary>
        /// <param name="sample">The categories for each observation in the sample.</param>
        /// <param name="categories">The number of possible categories.</param>
        /// <param name="hypothesizedProportions">
        ///     The hypothesized category proportions. Default is
        ///     to assume uniformly equal proportions.
        /// </param>
        public MultinomialTest(int[] sample, int categories, double[] hypothesizedProportions)
        {
            var observed = new double[categories];
            for (var i = 0; i < sample.Length; i++)
            {
                observed[sample[i]]++;
            }

            Compute(sample.Length, observed.Divide(sample.Length), hypothesizedProportions);
        }

        /// <summary>
        ///     Gets the observed sample proportions.
        /// </summary>
        public double[] ObservedProportions { get; private set; }

        /// <summary>
        ///     Gets the hypothesized population proportions.
        /// </summary>
        public double[] HypothesizedProportions { get; private set; }

        /// <summary>
        ///     Computes the Multinomial test.
        /// </summary>
        protected void Compute(int sampleSize, double[] observed, double[] expected)
        {
            // Approximate using a Chi-Square distribution

            ObservedProportions = observed;
            HypothesizedProportions = expected;

            double sum = 0;
            for (var i = 0; i < ObservedProportions.Length; i++)
            {
                var e = sampleSize * expected[i];
                var u = sampleSize * observed[i] - e;
                sum += u * u / e;
            }

            Compute(sum, observed.Length - 1);
        }
    }
}