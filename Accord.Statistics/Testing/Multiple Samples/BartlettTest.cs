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
    ///     Bartlett's test for equality of variances.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         In statistics, Bartlett's test is used to test if k samples are from populations
    ///         with equal variances. Equal variances across samples is called homoscedasticity
    ///         or homogeneity of variances. Some statistical tests, for example the
    ///         <see cref="IAnova">
    ///             analysis of variance
    ///         </see>
    ///         , assume that variances are equal across groups or samples.
    ///         The Bartlett test can be used to verify that assumption.
    ///     </para>
    ///     <para>
    ///         Bartlett's test is sensitive to departures from normality. That is, if the samples
    ///         come from non-normal distributions, then Bartlett's test may simply be testing for
    ///         non-normality. <see cref="LeveneTest">Levene's test</see> and the Brown–Forsythe test
    ///         are alternatives to the Bartlett test that are less sensitive to departures from
    ///         normality.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     Wikipedia, The Free Encyclopedia. Bartlett's test. Available on:
    ///                     http://en.wikipedia.org/wiki/Bartlett's_test
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <seealso cref="LeveneTest" />
    /// <seealso cref="ChiSquareTest" />
    /// <seealso cref="Distributions.Univariate.ChiSquareDistribution" />
    [Serializable]
    public class BartlettTest : ChiSquareTest
    {
        /// <summary>
        ///     Tests the null hypothesis that all group variances are equal.
        /// </summary>
        /// <param name="samples">The grouped samples.</param>
        public BartlettTest(params double[][] samples)
        {
            int N = 0, k = samples.Length;
            var Sp = Measures.PooledVariance(samples);

            double sum1 = 0; // Numerator sum
            for (var i = 0; i < samples.Length; i++)
            {
                var n = samples[i].Length;
                var s = samples[i].Variance();
                var logs = System.Math.Log(s);

                sum1 += (n - 1.0) * logs;
                N += n;
            }

            double sum2 = 0; // Denominator sum
            for (var i = 0; i < samples.Length; i++)
            {
                var n = samples[i].Length;
                sum2 += 1.0 / (n - 1);
            }


            var num = (N - k) * System.Math.Log(Sp) - sum1;
            var den = 1 + 1 / (3.0 * (k - 1)) * (sum2 - 1.0 / (N - k));

            var W = num / den;
            var degrees = k - 1;

            Compute(W, degrees);
        }
    }
}