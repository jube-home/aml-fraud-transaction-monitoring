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
using Accord.Statistics.Distributions.Univariate;
using Debug = Accord.Diagnostics.Debug;

namespace Accord.Statistics.Testing
{
    /// <summary>
    ///     Mann-Whitney-Wilcoxon test for unpaired samples.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The Mann–Whitney U test (also called the Mann–Whitney–Wilcoxon (MWW),
    ///         Wilcoxon rank-sum test, or Wilcoxon–Mann–Whitney test) is a non-parametric
    ///         test of the null hypothesis that two populations are the same against
    ///         an alternative hypothesis, especially that a particular population tends
    ///         to have larger values than the other.
    ///     </para>
    ///     <para>
    ///         It has greater efficiency than the <see cref="TTest">t-test</see> on
    ///         non-normal distributions, such as a <see cref="Mixture{T}">mixture</see>
    ///         of <see cref="NormalDistribution">normal distributions</see>, and it is
    ///         nearly as efficient as the <see cref="TTest">t-test</see> on normal
    ///         distributions.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The following example comes from Richard Lowry's page at
    ///         http://vassarstats.net/textbook/ch11a.html. As stated by
    ///         Richard, this example deals with persons seeking treatment
    ///         by claustrophobia. Those persons are randomly divided into
    ///         two groups, and each group receive a different treatment
    ///         for the disorder.
    ///     </para>
    ///     <para>
    ///         The hypothesis would be that treatment A would more effective
    ///         than B. To check this hypothesis, we can use Mann-Whitney's Test
    ///         to compare the medians of both groups.
    ///     </para>
    ///     <code>
    ///   // Claustrophobia test scores for people treated with treatment A
    ///   double[] sample1 = { 4.6, 4.7, 4.9, 5.1, 5.2, 5.5, 5.8, 6.1, 6.5, 6.5, 7.2 };
    ///   
    ///   // Claustrophobia test scores for people treated with treatment B
    ///   double[] sample2 = { 5.2, 5.3, 5.4, 5.6, 6.2, 6.3, 6.8, 7.7, 8.0, 8.1 };
    ///   
    ///   // Create a new Mann-Whitney-Wilcoxon's test to compare the two samples
    ///   MannWhitneyWilcoxonTest test = new MannWhitneyWilcoxonTest(sample1, sample2,
    ///     TwoSampleHypothesis.FirstValueIsSmallerThanSecond);
    ///   
    ///   double sum1 = test.RankSum1; //  96.5
    ///   double sum2 = test.RankSum2; // 134.5
    ///   
    ///   double statistic1 = test.Statistic1; // 79.5
    ///   double statistic2 = test.Statistic2; // 30.5
    ///   
    ///   double pvalue = test.PValue; // 0.043834132843420748
    ///   
    ///   // Check if the test was significant
    ///   bool significant = test.Significant; // true
    /// </code>
    /// </example>
    /// <seealso cref="TwoSampleTTest" />
    /// <seealso cref="TwoSampleWilcoxonSignedRankTest" />
    /// <seealso cref="MannWhitneyDistribution" />
    [Serializable]
    public class MannWhitneyWilcoxonTest : HypothesisTest<MannWhitneyDistribution>
    {
        private bool hasTies;

        /// <summary>
        ///     Tests whether two samples comes from the
        ///     same distribution without assuming normality.
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
        public MannWhitneyWilcoxonTest(double[] sample1, double[] sample2,
            TwoSampleHypothesis alternate = TwoSampleHypothesis.ValuesAreDifferent,
            bool? exact = null, bool adjustForTies = true)
        {
            NumberOfSamples1 = sample1.Length;
            NumberOfSamples2 = sample2.Length;

            // Concatenate both samples and rank them
            var samples = sample1.Concatenate(sample2);
            var rank = samples.Rank(out hasTies, adjustForTies: adjustForTies);

            // Split the rankings back and sum
            Rank1 = rank.Get(0, NumberOfSamples1);
            Rank2 = rank.Get(NumberOfSamples1, 0);

            // Compute rank sum statistic
            RankSum1 = Rank1.Sum();
            RankSum2 = Rank2.Sum();

            if (hasTies && exact == true)
            {
                Trace.TraceWarning("The exact method is not supported when there are ties in the data. "
                                   + "The p-value will be computed using the non-exact test.");
                exact = false;
            }

            // It is not necessary to compute the sum for both ranks. The sum of ranks in the second
            // sample can be determined from the first, since the sum of all the ranks equals n(n+1)/2 
            //   06/05/2017: This assumption is incorrect! The ranks can be different if there are ties in the data.
            // Accord.Diagnostics.Debug.Assert(RankSum2 == n * (n + 1) / 2 - RankSum1);

            // The U statistic can be obtained from the sum of the ranks in the sample,
            // minus the smallest value it can take (i.e. minus (n1 * (n1 + 1)) / 2.0),
            // meaning there is an wasy way to convert from W to U:

            // Compute Mann-Whitney's U statistic from the rank sum
            // as in Zar, Jerrold H. Biostatistical Analysis, 1998:
            Statistic1 = RankSum1 - (double)NumberOfSamples1 * (NumberOfSamples1 + 1) / 2.0; // U1
            Statistic2 = RankSum2 - (double)NumberOfSamples2 * (NumberOfSamples2 + 1) / 2.0; // U2

            // Again, it would not be necessary to compute U2 due the relation:
            //   06/05/2017: See above
            // Accord.Diagnostics.Debug.Assert(Statistic1 + Statistic2 == NumberOfSamples1 * NumberOfSamples2);

            Debug.Assert(Statistic1 == MannWhitneyDistribution.MannWhitneyU(Rank1));
            Debug.Assert(Statistic2 == MannWhitneyDistribution.MannWhitneyU(Rank2));

            // http://users.sussex.ac.uk/~grahamh/RM1web/WilcoxonHandoout2011.pdf
            // https://onlinecourses.science.psu.edu/stat464/node/38
            // http://www.real-statistics.com/non-parametric-tests/wilcoxon-rank-sum-test/
            // http://personal.vu.nl/R.Heijungs/QM/201516/stat/bs/documents/Supplement%2016B%20-%20Wilcoxon%20Mann-Whitney%20Small%20Sample%20Test.pdf
            // http://www.real-statistics.com/non-parametric-tests/wilcoxon-rank-sum-test/wilcoxon-rank-sum-exact-test/

            // The smaller value of U1 and U2 is the one used when using significance tables
            Statistic = NumberOfSamples1 < NumberOfSamples2 ? Statistic1 : Statistic2;

            Hypothesis = alternate;
            Tail = (DistributionTail)alternate;

            if (NumberOfSamples1 < NumberOfSamples2)
            {
                Statistic = Statistic1;
                StatisticDistribution = new MannWhitneyDistribution(Rank1, Rank2, exact)
                {
                    Correction = Tail == DistributionTail.TwoTail
                        ? ContinuityCorrection.Midpoint
                        : ContinuityCorrection.KeepInside
                };
            }
            else
            {
                Statistic = Statistic2;
                StatisticDistribution = new MannWhitneyDistribution(Rank2, Rank1, exact)
                {
                    Correction = Tail == DistributionTail.TwoTail
                        ? ContinuityCorrection.Midpoint
                        : ContinuityCorrection.KeepInside
                };
            }

            IsExact = StatisticDistribution.Exact;
            PValue = StatisticToPValue(Statistic);

            OnSizeChanged();
        }

        /// <summary>
        ///     Gets the alternative hypothesis under test. If the test is
        ///     <see cref="IHypothesisTest.Significant" />, the null hypothesis can be rejected
        ///     in favor of this alternative hypothesis.
        /// </summary>
        public TwoSampleHypothesis Hypothesis { get; protected set; }

        /// <summary>
        ///     Gets the number of samples in the first sample.
        /// </summary>
        public int NumberOfSamples1 { get; protected set; }

        /// <summary>
        ///     Gets the number of samples in the second sample.
        /// </summary>
        public int NumberOfSamples2 { get; protected set; }

        /// <summary>
        ///     Gets the rank statistics for the first sample.
        /// </summary>
        public double[] Rank1 { get; protected set; }

        /// <summary>
        ///     Gets the rank statistics for the second sample.
        /// </summary>
        public double[] Rank2 { get; protected set; }

        /// <summary>
        ///     Gets the sum of ranks for the first sample. Often known as Ta.
        /// </summary>
        public double RankSum1 { get; protected set; }

        /// <summary>
        ///     Gets the sum of ranks for the second sample. Often known as Tb.
        /// </summary>
        public double RankSum2 { get; protected set; }

        /// <summary>
        ///     Gets the difference between the expected value for
        ///     the observed value of <see cref="RankSum1" /> and its
        ///     expected value under the null hypothesis. Often known as U_a.
        /// </summary>
        public double Statistic1 { get; protected set; }

        /// <summary>
        ///     Gets the difference between the expected value for
        ///     the observed value of <see cref="RankSum2" /> and its
        ///     expected value under the null hypothesis. Often known as U_b.
        /// </summary>
        public double Statistic2 { get; protected set; }

        /// <summary>
        ///     Gets a value indicating whether the provided samples have tied ranks.
        /// </summary>
        public bool HasTies => hasTies;

        /// <summary>
        ///     Gets whether we are using a exact test.
        /// </summary>
        public bool IsExact { get; private set; }

        /// <summary>
        ///     Converts a given test statistic to a p-value.
        /// </summary>
        /// <param name="x">The value of the test statistic.</param>
        /// <returns>The p-value for the given statistic.</returns>
        public override double StatisticToPValue(double x)
        {
            // TODO: Maybe unify with WilcoxonTest?
            double p;
            switch (Tail)
            {
                case DistributionTail.TwoTail:
                    var a = StatisticDistribution.DistributionFunction(x);
                    var b = StatisticDistribution.ComplementaryDistributionFunction(x);
                    var c = System.Math.Min(a, b);
                    p = System.Math.Min(2 * c, 1);
                    break;

                case DistributionTail.OneLower:
                    if (NumberOfSamples1 < NumberOfSamples2)
                    {
                        p = StatisticDistribution.DistributionFunction(x);
                    }
                    else
                    {
                        p = StatisticDistribution.ComplementaryDistributionFunction(x);
                    }

                    break;

                case DistributionTail.OneUpper:
                    if (NumberOfSamples1 < NumberOfSamples2)
                    {
                        p = StatisticDistribution.ComplementaryDistributionFunction(x);
                    }
                    else
                    {
                        p = StatisticDistribution.DistributionFunction(x);
                    }

                    break;

                default:
                    throw new InvalidOperationException();
            }

            return p;
        }

        /// <summary>
        ///     Converts a given p-value to a test statistic.
        /// </summary>
        /// <param name="p">The p-value.</param>
        /// <returns>The test statistic which would generate the given p-value.</returns>
        public override double PValueToStatistic(double p)
        {
            throw new NotImplementedException(
                "This method has not been implemented yet. Please open an issue in the project issue tracker if you are interested in this feature.");
        }
    }
}