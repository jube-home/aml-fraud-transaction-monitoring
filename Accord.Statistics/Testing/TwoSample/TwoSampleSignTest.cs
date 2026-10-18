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
    ///     Sign test for two paired samples.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is a <see cref="BinomialTest">Binomial kind of test</see>.
    ///     </para>
    /// </remarks>
    /// <seealso cref="SignTest" />
    /// <seealso cref="BinomialTest" />
    /// <seealso cref="Distributions.Univariate.BinomialDistribution" />
    [Serializable]
    public class TwoSampleSignTest : BinomialTest
    {
        /// <summary>
        ///     Creates a new sign test for two samples.
        /// </summary>
        /// <param name="positiveSamples">The number of positive samples (successes).</param>
        /// <param name="totalSamples">The total number of samples (trials).</param>
        /// <param name="alternate">The alternative hypothesis (research hypothesis) to test.</param>
        public TwoSampleSignTest(int positiveSamples, int totalSamples, TwoSampleHypothesis alternate)
        {
            Compute(positiveSamples, totalSamples, alternate);
        }

        /// <summary>
        ///     Creates a new sign test for two samples.
        /// </summary>
        /// <param name="sample1">The first sample of observations.</param>
        /// <param name="sample2">The second sample of observations.</param>
        /// <param name="alternate">The alternative hypothesis (research hypothesis) to test.</param>
        public TwoSampleSignTest(double[] sample1, double[] sample2, TwoSampleHypothesis alternate)
        {
            if (sample1.Length != sample2.Length)
            {
                throw new DimensionMismatchException("sample2", "Both samples should be of the same size.");
            }

            var positive = 0;
            var negative = 0;

            for (var i = 0; i < sample1.Length; i++)
            {
                var d = sample2[i] - sample1[i];

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
        public new TwoSampleHypothesis Hypothesis { get; protected set; }

        /// <summary>
        ///     Computes the two sample sign test.
        /// </summary>
        protected void Compute(int positive, int total, TwoSampleHypothesis alternate)
        {
            Hypothesis = alternate;

            // The underlying test is to check whether the probability
            // value from the samples are higher than or lesser than 0.5,
            // thus the actual Binomial test hypothesis is inverted:

            OneSampleHypothesis binomialHypothesis;

            switch (alternate)
            {
                case TwoSampleHypothesis.ValuesAreDifferent:
                    binomialHypothesis = OneSampleHypothesis.ValueIsDifferentFromHypothesis; break;
                case TwoSampleHypothesis.FirstValueIsGreaterThanSecond:
                    binomialHypothesis = OneSampleHypothesis.ValueIsSmallerThanHypothesis; break;
                case TwoSampleHypothesis.FirstValueIsSmallerThanSecond:
                    binomialHypothesis = OneSampleHypothesis.ValueIsGreaterThanHypothesis; break;
                default: throw new InvalidOperationException();
            }


            Compute(positive, total, 0.5, binomialHypothesis);
        }
    }
}