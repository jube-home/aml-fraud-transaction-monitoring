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
using System.Diagnostics;
using Accord.Math;
using Accord.Statistics.Distributions;
using Accord.Statistics.Distributions.Univariate;

namespace Accord.Statistics.Testing
{
    /// <summary>
    ///     One-sample Anderson-Darling (AD) test.
    /// </summary>
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Testing\AndersonDarlingTestTest.cs" region="doc_test_uniform" />
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Testing\AndersonDarlingTestTest.cs" region="doc_test_normal" />
    /// </example>
    [Serializable]
    public class AndersonDarlingTest : HypothesisTest<AndersonDarlingDistribution>,
        IHypothesisTest<AndersonDarlingDistribution>
    {
        /// <summary>
        ///     Creates a new Anderson-Darling test.
        /// </summary>
        /// <param name="sample">The sample we would like to test as belonging to the <paramref name="hypothesizedDistribution" />.</param>
        /// <param name="hypothesizedDistribution">A fully specified distribution.</param>
        public AndersonDarlingTest(double[] sample, IUnivariateDistribution<double> hypothesizedDistribution)
        {
            // Create the test statistic distribution 
            TheoreticalDistribution = hypothesizedDistribution;
            if (hypothesizedDistribution is UniformContinuousDistribution)
            {
                StatisticDistribution =
                    new AndersonDarlingDistribution(AndersonDarlingDistributionType.Uniform, sample.Length);
            }
            else if (hypothesizedDistribution is NormalDistribution)
            {
                StatisticDistribution =
                    new AndersonDarlingDistribution(AndersonDarlingDistributionType.Normal, sample.Length);
            }
            else
            {
                Trace.WriteLine(string.Format(
                    "Unsupported distribution in AndersonDarling: {0}. P-values will not be computed, but test statistic may be useful.",
                    hypothesizedDistribution, hypothesizedDistribution.GetType()));
            }

            // Create a copy of the samples to prevent altering the
            // constructor's original arguments in the sorting step 
            var sortedSamples = sample.Sorted();

            // Create the theoretical and empirical distributions
            TheoreticalDistribution = hypothesizedDistribution;

            Statistic = GetStatistic(sortedSamples, TheoreticalDistribution);
            PValue = StatisticToPValue(Statistic);
        }

        /// <summary>
        ///     Gets the theoretical, hypothesized distribution for the samples,
        ///     which should have been stated <i>before</i> any measurements.
        /// </summary>
        public IUnivariateDistribution<double> TheoreticalDistribution { get; private set; }

        /// <summary>
        ///     Not supported.
        /// </summary>
        public override double PValueToStatistic(double p)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        ///     Converts a given test statistic to a p-value.
        /// </summary>
        /// <param name="x">The value of the test statistic.</param>
        /// <returns>The p-value for the given statistic.</returns>
        public override double StatisticToPValue(double x)
        {
            if (StatisticDistribution == null)
            {
                return double.NaN;
            }

            return StatisticDistribution.ComplementaryDistributionFunction(x);
        }

        /// <summary>
        ///     Gets the Anderson-Darling statistic for the samples and target distribution.
        /// </summary>
        /// <param name="sortedSamples">The sorted samples.</param>
        /// <param name="distribution">The target distribution.</param>
        public static double GetStatistic(double[] sortedSamples, IUnivariateDistribution<double> distribution)
        {
            double S = 0;
            var n = sortedSamples.Length;

            // Finally, compute the test statistic.
            for (var i = 0; i < sortedSamples.Length; i++)
            {
                var a = 2.0 * (i + 1) - 1;
                var b = distribution.DistributionFunction(sortedSamples[i]);
                var c = distribution.ComplementaryDistributionFunction(sortedSamples[n - i - 1]);

                S += a * (System.Math.Log(b) + System.Math.Log(c));
            }

            return -n - S / n;
        }
    }
}