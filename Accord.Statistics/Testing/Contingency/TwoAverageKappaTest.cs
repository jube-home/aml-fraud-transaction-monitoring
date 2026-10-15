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
using Accord.Statistics.Analysis;

namespace Accord.Statistics.Testing
{
    /// <summary>
    ///     Kappa test for the average of two groups of contingency tables.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The two-matrix Kappa test tries to assert whether the Kappa measure
    ///         of two groups of contingency tables, each group created by a different
    ///         rater or classification model and measured repeatedly, differs significantly.
    ///     </para>
    ///     <para>
    ///         This is a <see cref="TwoSampleTTest">two sample t-test kind of test</see>.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     J. L. Fleiss. Statistical methods for rates and proportions.
    ///                     Wiley-Interscience; 3rd edition (September 5, 2003)
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <seealso cref="TwoSampleTTest" />
    /// <seealso cref="TTest" />
    [Serializable]
    public class TwoAverageKappaTest : TwoSampleTTest
    {
        /// <summary>
        ///     Creates a new Two-Table Mean Kappa test.
        /// </summary>
        /// <param name="meanKappa1">The average kappa value for the first group of contingency tables.</param>
        /// <param name="meanKappa2">The average kappa value for the second group of contingency tables.</param>
        /// <param name="varKappa1">The kappa's variance in the first group of tables.</param>
        /// <param name="varKappa2">The kappa's variance in the first group of tables.</param>
        /// <param name="kappaSamples1">The number of contingency tables averaged in the first group.</param>
        /// <param name="kappaSamples2">The number of contingency tables averaged in the second group.</param>
        /// <param name="assumeEqualVariances">True to assume equal variances, false otherwise. Default is true.</param>
        /// <param name="alternate">The alternative hypothesis (research hypothesis) to test.</param>
        /// <param name="hypothesizedDifference">The hypothesized difference between the two Kappa values.</param>
        public TwoAverageKappaTest(double meanKappa1, double varKappa1, int kappaSamples1,
            double meanKappa2, double varKappa2, int kappaSamples2,
            double hypothesizedDifference = 0, bool assumeEqualVariances = true,
            TwoSampleHypothesis alternate = TwoSampleHypothesis.ValuesAreDifferent)
        {
            Variance1 = varKappa1;
            Variance2 = varKappa2;

            Compute(
                meanKappa1, varKappa1, kappaSamples1,
                meanKappa2, varKappa2, kappaSamples2,
                hypothesizedDifference, assumeEqualVariances, alternate);
        }

        /// <summary>
        ///     Creates a new Two-Table Mean Kappa test.
        /// </summary>
        /// <param name="matrices1">The first group of contingency tables.</param>
        /// <param name="matrices2">The second  group of contingency tables.</param>
        /// <param name="assumeEqualVariances">True to assume equal variances, false otherwise. Default is true.</param>
        /// <param name="hypothesizedDifference">The hypothesized difference between the two average Kappa values.</param>
        /// <param name="alternate">The alternative hypothesis (research hypothesis) to test.</param>
        public TwoAverageKappaTest(GeneralConfusionMatrix[] matrices1, GeneralConfusionMatrix[] matrices2,
            double hypothesizedDifference = 0, bool assumeEqualVariances = true,
            TwoSampleHypothesis alternate = TwoSampleHypothesis.ValuesAreDifferent)
        {
            var kappas1 = new double[matrices1.Length];
            for (var i = 0; i < matrices1.Length; i++)
            {
                kappas1[i] = matrices1[i].Kappa;
            }

            var kappas2 = new double[matrices2.Length];
            for (var i = 0; i < matrices2.Length; i++)
            {
                kappas2[i] = matrices2[i].Kappa;
            }

            var meanKappa1 = kappas1.Mean();
            var meanKappa2 = kappas2.Mean();

            Variance1 = kappas1.Variance(meanKappa1);
            Variance2 = kappas2.Variance(meanKappa2);

            var kappaSamples1 = matrices1.Length;
            var kappaSamples2 = matrices2.Length;

            Compute(
                meanKappa1, Variance1, kappaSamples1,
                meanKappa2, Variance2, kappaSamples2,
                hypothesizedDifference, assumeEqualVariances, alternate);
        }

        /// <summary>
        ///     Gets the variance for the first Kappa value.
        /// </summary>
        public double Variance1 { get; private set; }

        /// <summary>
        ///     Gets the variance for the second Kappa value.
        /// </summary>
        public double Variance2 { get; private set; }
    }
}