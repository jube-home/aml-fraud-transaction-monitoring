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
    ///     Shapiro-Wilk test for normality.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The Shapiro–Wilk test is a test of normality in frequentist statistics. It was
    ///         published in 1965 by Samuel Sanford Shapiro and Martin Wilk. The The Shapiro–Wilk
    ///         test tests the null hypothesis that a sample came from a normally distributed
    ///         population.
    ///     </para>
    ///     <para>
    ///         The null-hypothesis of this test is that the population is normally distributed. Thus,
    ///         if the <see cref="HypothesisTest{T}.PValue">p-value</see> is less than the chosen
    ///         <see cref="HypothesisTest{T}.Size">alpha level</see>, then the null hypothesis is rejected
    ///         and there is evidence that the data tested are not from a normally distributed population;
    ///         in other words, the data are not normal. On the contrary, if the p-value is greater than
    ///         the chosen alpha level, then the null hypothesis that the data came from a normally
    ///         distributed population cannot be rejected (e.g., for an alpha level of 0.05, a data
    ///         set with a p-value of 0.02 rejects the null hypothesis that the data are from a normally
    ///         distributed population). However, since the test is biased by sample size, the
    ///         test may be statistically significant from a normal distribution in any large samples.
    ///         Thus a Q–Q plot is required for verification in addition to the test.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Shapiro%E2%80%93Wilk_test">
    ///                         Wikipedia, The Free Encyclopedia. Shapiro-Wilk test. Available on:
    ///                         http://en.wikipedia.org/wiki/Shapiro%E2%80%93Wilk_test
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Testing\ShapiroWilkTestTest.cs" region="doc_test" />
    /// </example>
    [Serializable]
    public class ShapiroWilkTest : HypothesisTest<ShapiroWilkDistribution>,
        IHypothesisTest<ShapiroWilkDistribution>
    {
        /// <summary>
        ///     Creates a new Shapiro-Wilk test.
        /// </summary>
        /// <param name="sample">The sample we would like to test.</param>
        /// <exception cref="ArgumentException">
        ///     The sample must contain at least 4 observations.
        /// </exception>
        public ShapiroWilkTest(double[] sample)
        {
            double N = sample.Length;
            var n = sample.Length;

            if (n < 4)
            {
                throw new ArgumentException("The sample must contain at least 4 observations.", "sample");
            }

            var mean = sample.Mean();

            StatisticDistribution = new ShapiroWilkDistribution(n);

            // Create a copy of the samples to prevent altering the
            // constructor's original arguments in the sorting step 
            var Y = (double[])sample.Clone();

            // Sort sample
            Array.Sort(Y);

            var m = new double[sample.Length];

            // Compute Bloom scores
            for (var i = 0; i < Y.Length; i++)
            {
                var num = i + 1 - 3.0 / 8.0;
                var den = N + 1.0 / 4.0;
                m[i] = NormalDistribution.Standard.InverseDistributionFunction(num / den);
            }

            var mm = m.Dot(m);

            var c = m.Divide(System.Math.Sqrt(mm));

            var u = 1.0 / System.Math.Sqrt(N);

            var u2 = u * u;
            var u3 = u2 * u;
            var u4 = u2 * u2;
            var u5 = u4 * u;

            var a = new double[n];

            var an = a[n - 1] = -2.706056 * u5 + 4.434685 * u4 - 2.071190 * u3 - 0.147981 * u2 + 0.221157 * u +
                                c[n - 1];
            var mn = m[n - 1];
            a[0] = -an;

            if (N <= 5)
            {
                var phi = (mm - 2 * mn * mn) / (1.0 - 2 * an * an);
                var sqrt = System.Math.Sqrt(phi);

                for (var i = 1; i < n - 1; i++)
                {
                    a[i] = m[i] / sqrt;
                }
            }
            else
            {
                var anm1 = a[n - 2] = -3.582633 * u5 + 5.682633 * u4 - 1.752461 * u3 - 0.293762 * u2 + 0.042981 * u +
                                      c[n - 2];
                var mnm1 = m[n - 2];
                a[1] = -anm1;

                var phi = (mm - 2 * mn * mn - 2 * mnm1 * mnm1) / (1.0 - 2 * an * an - 2 * anm1 * anm1);
                var sqrt = System.Math.Sqrt(phi);

                for (var i = 2; i < n - 2; i++)
                {
                    a[i] = m[i] / sqrt;
                }
            }

            double Wnum = 0;
            double Wden = 0;

            for (var i = 0; i < sample.Length; i++)
            {
                var w = Y[i] - mean;
                Wnum += a[i] * Y[i];
                Wden += w * w;
            }

            var W = Wnum * Wnum / Wden;

            Statistic = W;
            PValue = StatisticToPValue(W);
        }

        /// <summary>
        ///     Converts a given p-value to a test statistic.
        /// </summary>
        /// <param name="p">The p-value.</param>
        /// <returns>The test statistic which would generate the given p-value.</returns>
        public override double PValueToStatistic(double p)
        {
            return StatisticDistribution.InverseDistributionFunction(p);
        }

        /// <summary>
        ///     Converts a given test statistic to a p-value.
        /// </summary>
        /// <param name="x">The value of the test statistic.</param>
        /// <returns>The p-value for the given statistic.</returns>
        public override double StatisticToPValue(double x)
        {
            return StatisticDistribution.DistributionFunction(x);
        }
    }
}