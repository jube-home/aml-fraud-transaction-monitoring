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
using Accord.Statistics.Distributions.Univariate;

namespace Accord.Statistics.Testing.Power
{
    /// <summary>
    ///     Power analysis for two-sample Z-Tests.
    /// </summary>
    /// <example>
    ///     <para>
    ///         Please take a look at the <see cref="TwoSampleTTest" /> example section.
    ///     </para>
    /// </example>
    /// <seealso cref="IPowerAnalysis" />
    /// <seealso cref="ZTestPowerAnalysis" />
    /// <seealso cref="TwoSampleTTestPowerAnalysis" />
    [Serializable]
    public class TwoSampleZTestPowerAnalysis : BaseTwoSamplePowerAnalysis
    {
        /// <summary>
        ///     Creates a new <see cref="ZTestPowerAnalysis" />.
        /// </summary>
        /// <param name="hypothesis">The hypothesis tested.</param>
        public TwoSampleZTestPowerAnalysis(TwoSampleHypothesis hypothesis)
            : base((DistributionTail)hypothesis)
        {
        }

        /// <summary>
        ///     Creates a new <see cref="ZTestPowerAnalysis" />.
        /// </summary>
        /// <param name="test">The test to create the analysis for.</param>
        public TwoSampleZTestPowerAnalysis(TwoSampleZTest test)
            : base(test.Tail)
        {
            Power = test.Analysis.Power;
            Size = test.Analysis.Size;
            Effect = test.Analysis.Effect;
            Samples1 = test.Analysis.Samples1;
            Samples2 = test.Analysis.Samples2;
        }

        /// <summary>
        ///     Computes the power for a test with givens values of
        ///     <see cref="IPowerAnalysis.Effect">effect size</see> and
        ///     <see cref="IPowerAnalysis.Samples">
        ///         number of samples
        ///     </see>
        ///     under <see cref="IPowerAnalysis.Size" />.
        /// </summary>
        /// <returns>
        ///     The power for the test under the given conditions.
        /// </returns>
        public override void ComputePower()
        {
            var samples = Samples1 * Samples2 / (Samples1 + Samples2);

            var Za = ZTest.PValueToStatistic(Size, Tail);
            var Zb = Za - System.Math.Sqrt(samples) * Effect;

            Power = NormalDistribution.Standard.ComplementaryDistributionFunction(Zb);
        }

        /// <summary>
        ///     Gets the recommended sample size for the test to attain
        ///     the power indicating in <see cref="IPowerAnalysis.Power" /> considering
        ///     values of <see cref="IPowerAnalysis.Effect" /> and <see cref="IPowerAnalysis.Size" />.
        /// </summary>
        /// <returns>
        ///     Recommended sample size for attaining the given
        ///     <see cref="IPowerAnalysis.Power" /> for size effect <see cref="IPowerAnalysis.Effect" />
        ///     under the given <see cref="IPowerAnalysis.Size" />.
        /// </returns>
        public override void ComputeSamples(double proportion = 1)
        {
            var Za = ZTest.PValueToStatistic(Size, Tail);
            var Zb = NormalDistribution.Standard
                .InverseDistributionFunction(Power);

            var n = (Za + Zb) / Effect;

            Samples1 = n * n * (1.0 + proportion) / proportion;
            Samples2 = Samples1 * proportion;
        }

        /// <summary>
        ///     Computes the minimum detectable effect size for the test
        ///     considering the power given in <see cref="IPowerAnalysis.Power" />, the
        ///     number of samples in <see cref="IPowerAnalysis.Samples" /> and the significance
        ///     level <see cref="IPowerAnalysis.Size" />.
        /// </summary>
        /// <returns>
        ///     The minimum detectable
        ///     <see cref="IPowerAnalysis.Effect">
        ///         effect
        ///         size
        ///     </see>
        ///     for the test under the given conditions.
        /// </returns>
        public override void ComputeEffect()
        {
            var Za = ZTest.PValueToStatistic(Size, Tail);
            var Zb = NormalDistribution.Standard
                .InverseDistributionFunction(Power);

            var n = System.Math.Sqrt(Samples1 * Samples2 / (Samples1 + Samples2));
            var d = (Za + Zb) / n;

            Effect = d;
        }

        /// <summary>
        ///     Estimates the number of samples necessary to attain the
        ///     required power level for the given effect size.
        /// </summary>
        /// <param name="delta">The minimum detectable difference.</param>
        /// <param name="standardDeviation">The difference standard deviation.</param>
        /// <param name="power">The desired power level. Default is 0.8.</param>
        /// <param name="alpha">The desired significance level. Default is 0.05.</param>
        /// <param name="hypothesis">The alternative hypothesis (research hypothesis) to be tested.</param>
        /// <param name="proportion">
        ///     The proportion of observations in the second group
        ///     when compared to the first group. A proportion of 2:1 results in twice more
        ///     samples in the second group than in the first. Default is 1.
        /// </param>
        /// <returns>The required number of samples.</returns>
        public static TwoSampleZTestPowerAnalysis GetSampleSize(double delta,
            double standardDeviation = 1, double proportion = 1.0, double power = 0.8, double alpha = 0.05,
            TwoSampleHypothesis hypothesis = TwoSampleHypothesis.ValuesAreDifferent)
        {
            var analysis = new TwoSampleZTestPowerAnalysis(hypothesis)
            {
                Effect = delta / standardDeviation,
                Size = alpha,
                Power = power
            };

            analysis.ComputeSamples(proportion);

            return analysis;
        }

        /// <summary>
        ///     Estimates the number of samples necessary to attain the
        ///     required power level for the given effect size.
        /// </summary>
        /// <param name="sampleSize1">The number of observations in the first sample.</param>
        /// <param name="sampleSize2">The number of observations in the second sample.</param>
        /// <param name="power">The desired power level. Default is 0.8.</param>
        /// <param name="alpha">The desired significance level. Default is 0.05.</param>
        /// <param name="hypothesis">The alternative hypothesis (research hypothesis) to be tested.</param>
        /// <returns>The required number of samples.</returns>
        public static TwoSampleZTestPowerAnalysis GetEffectSize(int sampleSize1, int sampleSize2,
            double power = 0.8, double alpha = 0.05,
            TwoSampleHypothesis hypothesis = TwoSampleHypothesis.ValuesAreDifferent)
        {
            var analysis = new TwoSampleZTestPowerAnalysis(hypothesis)
            {
                Samples1 = sampleSize1,
                Samples2 = sampleSize2,
                Size = alpha,
                Power = power
            };

            analysis.ComputeEffect();

            return analysis;
        }
    }
}