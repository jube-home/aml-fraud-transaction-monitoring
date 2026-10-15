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

namespace Accord.Statistics.Testing
{
    /// <summary>
    ///     Wilcoxon signed-rank test for paired samples.
    /// </summary>
    /// <seealso cref="WilcoxonSignedRankTest" />
    /// <seealso cref="Distributions.Univariate.WilcoxonDistribution" />
    public class TwoSampleWilcoxonSignedRankTest : WilcoxonTest
    {
        /// <summary>
        ///     Tests whether the medians of two paired samples are different.
        /// </summary>
        /// <param name="sample1">The first sample.</param>
        /// <param name="sample2">The second sample.</param>
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
        public TwoSampleWilcoxonSignedRankTest(double[] sample1, double[] sample2,
            TwoSampleHypothesis alternate = TwoSampleHypothesis.ValuesAreDifferent,
            bool? exact = null, bool adjustForTies = true)
        {
            if (sample1.Length != sample2.Length)
            {
                throw new DimensionMismatchException("sample2", "Both samples should be of the same size.");
            }

            var signs = new int[sample1.Length];
            var diffs = new double[sample1.Length];

            // 1. Compute absolute difference and sign function
            for (var i = 0; i < sample1.Length; i++)
            {
                var d = sample1[i] - sample2[i];
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
        public TwoSampleHypothesis Hypothesis { get; protected set; }
    }
}