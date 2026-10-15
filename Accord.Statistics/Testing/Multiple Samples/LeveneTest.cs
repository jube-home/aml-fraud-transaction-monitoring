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
    ///     Levene test computation methods.
    /// </summary>
    public enum LeveneTestMethod
    {
        /// <summary>
        ///     The test has been computed using the Mean.
        /// </summary>
        Mean,

        /// <summary>
        ///     The test has been computed using the Median
        ///     (which is known as the Brown-Forsythe test).
        /// </summary>
        Median,

        /// <summary>
        ///     The test has been computed using the trimmed mean.
        /// </summary>
        TruncatedMean
    }

    /// <summary>
    ///     Levene's test for equality of variances.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         In statistics, Levene's test is an inferential statistic used to assess the
    ///         equality of variances for a variable calculated for two or more groups. Some
    ///         common statistical procedures assume that variances of the populations from
    ///         which different samples are drawn are equal. Levene's test assesses this
    ///         assumption. It tests the null hypothesis that the population variances are
    ///         equal (called homogeneity of variance or homoscedasticity). If the resulting
    ///         P-value of Levene's test is less than some significance level (typically 0.05),
    ///         the obtained differences in sample variances are unlikely to have occurred based
    ///         on random sampling from a population with equal variances. Thus, the null hypothesis
    ///         of equal variances is rejected and it is concluded that there is a difference
    ///         between the variances in the population.
    ///     </para>
    ///     <para>
    ///         Some of the procedures typically assuming homoscedasticity, for which one can use
    ///         Levene's tests, include <see cref="IAnova">analysis of variance</see> and
    ///         <see cref="TTest">
    ///             t-tests
    ///         </see>
    ///         . Levene's test is often used before a comparison of means. When Levene's
    ///         test shows significance, one should switch to generalized tests, free from homoscedasticity
    ///         assumptions. Levene's test may also be used as a main test for answering a stand-alone
    ///         question of whether two sub-samples in a given population have equal or different variances.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     Wikipedia, The Free Encyclopedia. Levene's test. Available on:
    ///                     http://en.wikipedia.org/wiki/Levene's_test
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <seealso cref="BartlettTest" />
    /// <seealso cref="FTest" />
    /// <seealso cref="Distributions.Univariate.FDistribution" />
    [Serializable]
    public class LeveneTest : FTest
    {
        /// <summary>
        ///     Tests the null hypothesis that all group variances are equal.
        /// </summary>
        /// <param name="samples">The grouped samples.</param>
        /// <param name="median">
        ///     <c>True</c> to use the median in the Levene calculation.
        ///     <c>False</c> to use the mean. Default is false (use the mean).
        /// </param>
        public LeveneTest(double[][] samples, bool median = false)
        {
            compute(samples, median ? LeveneTestMethod.Median : LeveneTestMethod.Mean, 0);
        }

        /// <summary>
        ///     Tests the null hypothesis that all group variances are equal.
        /// </summary>
        /// <param name="samples">The grouped samples.</param>
        /// <param name="percent">
        ///     The percentage of observations to discard
        ///     from the sample when computing the test with the truncated mean.
        /// </param>
        public LeveneTest(double[][] samples, double percent)
        {
            compute(samples, LeveneTestMethod.TruncatedMean, percent);
        }

        /// <summary>
        ///     Gets the method used to compute the Levene's test.
        /// </summary>
        public LeveneTestMethod Method { get; private set; }

        private void compute(double[][] samples, LeveneTestMethod method, double percent)
        {
            Method = method;
            int N = 0, k = samples.Length;

            // Compute group means
            var means = new double[samples.Length];

            switch (method)
            {
                case LeveneTestMethod.Mean:
                    for (var i = 0; i < means.Length; i++)
                    {
                        means[i] = samples[i].Mean();
                    }

                    break;

                case LeveneTestMethod.Median:
                    for (var i = 0; i < means.Length; i++)
                    {
                        means[i] = samples[i].Median();
                    }

                    break;

                case LeveneTestMethod.TruncatedMean:
                    for (var i = 0; i < means.Length; i++)
                    {
                        means[i] = samples[i].TruncatedMean(percent);
                    }

                    break;
            }

            // Compute absolute centered samples
            var z = new double[samples.Length][];
            for (var i = 0; i < z.Length; i++)
            {
                z[i] = new double[samples[i].Length];
                for (var j = 0; j < z[i].Length; j++)
                {
                    z[i][j] = System.Math.Abs(samples[i][j] - means[i]);
                }
            }

            // Compute means for the centered samples
            var newMeans = new double[samples.Length];
            for (var i = 0; i < newMeans.Length; i++)
            {
                newMeans[i] = z[i].Mean();
            }

            // Compute total mean
            double totalMean = 0;
            for (var i = 0; i < samples.Length; i++)
            {
                for (var j = 0; j < samples[i].Length; j++)
                {
                    totalMean += z[i][j];
                }

                N += samples[i].Length;
            }

            totalMean /= N;

            double sum1 = 0; // Numerator sum
            for (var i = 0; i < samples.Length; i++)
            {
                var n = samples[i].Length;
                var u = newMeans[i] - totalMean;
                sum1 += n * u * u;
            }

            double sum2 = 0; // Denominator sum
            for (var i = 0; i < samples.Length; i++)
            {
                for (var j = 0; j < samples[i].Length; j++)
                {
                    var u = z[i][j] - newMeans[i];
                    sum2 += u * u;
                }
            }

            var num = (N - k) * sum1;
            var den = (k - 1) * sum2;

            var W = num / den;
            var degree1 = k - 1;
            var degree2 = N - k;

            Compute(W, degree1, degree2, TwoSampleHypothesis.FirstValueIsGreaterThanSecond);
        }
    }
}