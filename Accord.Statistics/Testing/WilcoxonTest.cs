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
    ///     Base class for Wilcoxon's W tests.
    /// </summary>
    /// <remarks>
    ///     This is a base class which doesn't need to be used directly.
    ///     Instead, you may wish to call <see cref="WilcoxonSignedRankTest" />
    ///     and <see cref="TwoSampleWilcoxonSignedRankTest" />.
    /// </remarks>
    /// <seealso cref="WilcoxonSignedRankTest" />
    /// <seealso cref="TwoSampleWilcoxonSignedRankTest" />
    /// <seealso cref="WilcoxonDistribution" />
    public class WilcoxonTest : HypothesisTest<WilcoxonDistribution>
    {
        private bool hasTies;

        /// <summary>
        ///     Creates a new Wilcoxon's W+ test.
        /// </summary>
        /// <param name="signs">The signs for the sample differences.</param>
        /// <param name="diffs">The differences between samples.</param>
        /// <param name="tail">The distribution tail to test.</param>
        public WilcoxonTest(int[] signs, double[] diffs, DistributionTail tail)
        {
            Compute(signs, diffs, tail, null, true);
        }

        /// <summary>
        ///     Creates a new Wilcoxon's W+ test.
        /// </summary>
        protected WilcoxonTest()
        {
        }

        /// <summary>
        ///     Gets the number of samples being tested.
        /// </summary>
        public int NumberOfSamples => StatisticDistribution.NumberOfSamples;

        /// <summary>
        ///     Gets the signs for each of the <see cref="Delta" /> differences.
        /// </summary>
        public int[] Signs { get; protected set; }

        /// <summary>
        ///     Gets the differences between the samples.
        /// </summary>
        public double[] Delta { get; protected set; }

        /// <summary>
        ///     Gets the rank statistics for the differences.
        /// </summary>
        public double[] Ranks { get; protected set; }

        /// <summary>
        ///     Gets wether the samples to be ranked contain zeros.
        /// </summary>
        public bool HasZeros { get; private set; }

        /// <summary>
        ///     Gets wether the samples to be ranked contain ties.
        /// </summary>
        public bool HasTies => hasTies;

        /// <summary>
        ///     Gets whether we are using a exact test.
        /// </summary>
        public bool IsExact { get; private set; }

        /// <summary>
        ///     Computes the Wilcoxon Signed-Rank test.
        /// </summary>
        protected void Compute(int[] signs, double[] diffs, DistributionTail tail,
            bool? exact, bool adjustForTies)
        {
            var nonZeros = diffs.Find(x => x != 0);
            HasZeros = nonZeros.Length != diffs.Length;

            if (HasZeros)
            {
                // It is actually necessary to discard zeros before the test procedure
                // https://en.wikipedia.org/wiki/Wilcoxon_signed-rank_test#Test_procedure
                // http://vassarstats.net/textbook/ch12a.html
                signs = signs.Get(nonZeros);
                diffs = diffs.Get(nonZeros);
            }

            Signs = signs;
            Delta = diffs;
            Ranks = Delta.Rank(out hasTies, adjustForTies: adjustForTies);

            var W = WilcoxonDistribution.WPositive(Signs, Ranks);

            Compute(W, Ranks, tail, exact);
        }

        /// <summary>
        ///     Computes the Wilcoxon Signed-Rank test.
        /// </summary>
        protected void Compute(double statistic, double[] ranks, DistributionTail tail, bool? exact)
        {
            if (HasZeros)
            {
                if (!exact.HasValue)
                {
                    exact = false;
                }

                if (exact == true)
                {
                    throw new ArgumentException(
                        "An exact test cannot be computed when there are zeros in the samples (or when paired samples are the same in a paired test).");
                }
            }

            Statistic = statistic;
            Tail = tail;

            if (ranks.Length != 0)
            {
                StatisticDistribution = new WilcoxonDistribution(ranks, exact)
                {
                    Correction = Tail == DistributionTail.TwoTail
                        ? ContinuityCorrection.Midpoint
                        : ContinuityCorrection.KeepInside
                };

                IsExact = StatisticDistribution.Exact;
            }
            else
            {
                StatisticDistribution = null;
                IsExact = exact.GetValueOrDefault(false);
            }

            PValue = StatisticToPValue(Statistic);

            OnSizeChanged();
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
                return double.NaN; // return NaN to match R's behavior
            }

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
                    p = StatisticDistribution.DistributionFunction(x);
                    break;

                case DistributionTail.OneUpper:
                    p = StatisticDistribution.ComplementaryDistributionFunction(x);
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
            double b;
            switch (Tail)
            {
                case DistributionTail.OneLower:
                    b = StatisticDistribution.InverseDistributionFunction(p);
                    break;
                case DistributionTail.OneUpper:
                    b = StatisticDistribution.InverseDistributionFunction(1.0 - p);
                    break;
                case DistributionTail.TwoTail:
                    b = StatisticDistribution.InverseDistributionFunction(1.0 - p / 2.0);
                    break;
                default:
                    throw new InvalidOperationException();
            }

            return b;
        }
    }
}