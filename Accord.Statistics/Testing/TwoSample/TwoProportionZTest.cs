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
    ///     Z-Test for two sample proportions.
    /// </summary>
    /// <seealso cref="TwoSampleZTest" />
    /// <seealso cref="ZTest" />
    [Serializable]
    public class TwoProportionZTest : TwoSampleZTest
    {
        /// <summary>
        ///     Creates a new Z-Test for two sample proportions.
        /// </summary>
        /// <param name="proportion1">The proportion of success observations in the first sample.</param>
        /// <param name="sampleSize1">The total number of observations in the first sample.</param>
        /// <param name="proportion2">The proportion of success observations in the second sample.</param>
        /// <param name="sampleSize2">The total number of observations in the second sample.</param>
        /// <param name="alternate">The alternative hypothesis (research hypothesis) to test.</param>
        public TwoProportionZTest(double proportion1, int sampleSize1, double proportion2, int sampleSize2,
            TwoSampleHypothesis alternate = TwoSampleHypothesis.ValuesAreDifferent)
        {
            var x1 = (int)System.Math.Round(proportion1 * sampleSize1);
            var x2 = (int)System.Math.Round(proportion2 * sampleSize2);

            Compute(x1, sampleSize1, x2, sampleSize2, alternate);
        }

        /// <summary>
        ///     Creates a new Z-Test for two sample proportions.
        /// </summary>
        /// <param name="successes1">The number of successes in the first sample.</param>
        /// <param name="trials1">The total number of trials (observations) in the first sample.</param>
        /// <param name="successes2">The number of successes in the second sample.</param>
        /// <param name="trials2">The total number of trials (observations) in the second sample.</param>
        /// <param name="alternate">The alternative hypothesis (research hypothesis) to test.</param>
        public TwoProportionZTest(int successes1, int trials1, int successes2, int trials2,
            TwoSampleHypothesis alternate = TwoSampleHypothesis.ValuesAreDifferent)
        {
            Compute(successes1, trials1, successes2, trials2, alternate);
        }

        /// <summary>
        ///     Computes the Z-test for two sample proportions.
        /// </summary>
        protected void Compute(
            int successes1, int trials1,
            int successes2, int trials2,
            TwoSampleHypothesis alternate)
        {
            var p1 = successes1 / (double)trials1;
            var p2 = successes2 / (double)trials2;
            var p = (successes1 + successes2) / (double)(trials1 + trials2);

            EstimatedValue1 = p1;
            EstimatedValue2 = p2;
            ObservedDifference = p1 - p2;
            StandardError = System.Math.Sqrt(p * (1 - p) * (1.0 / trials1 + 1.0 / trials2));

            var z = ObservedDifference / StandardError;

            Compute(z, alternate);
        }
    }
}