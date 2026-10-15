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

namespace Accord.Statistics.Testing
{
    /// <summary>
    ///     Binomial test.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         In statistics, the binomial test is an exact test of the statistical significance
    ///         of deviations from a theoretically expected distribution of observations into two
    ///         categories. The most common use of the binomial test is in the case where the null
    ///         hypothesis is that two categories are equally likely to occur (such as a coin toss).
    ///     </para>
    ///     <para>
    ///         When there are more than two categories, and an exact test is required, the
    ///         <see cref="MultinomialTest"> multinomial test</see>, based on the multinomial
    ///         distribution, must be used instead of the binomial test.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Binomial_test">
    ///                         Wikipedia, The Free Encyclopedia. Binomial-Test. Available from:
    ///                         http://en.wikipedia.org/wiki/Binomial_test
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         This is the second example from Wikipedia's page on hypothesis testing. In this example,
    ///         a person is tested for clairvoyance (ability of gaining information about something through
    ///         extra sensory perception; detecting something without using the known human senses.
    ///     </para>
    ///     <code>
    /// // A person is shown the reverse of a playing card 25 times and is
    /// // asked which of the four suits the card belongs to. Every time
    /// // the person correctly guesses the suit of the card, we count this
    /// // result as a correct answer. Let's suppose the person obtained 13
    /// // correctly answers out of the 25 cards.
    /// 
    /// // Since each suit appears 1/4 of the time in the card deck, we 
    /// // would assume the probability of producing a correct answer by
    /// // chance alone would be of 1/4.
    /// 
    /// // And finally, we must consider we are interested in which the
    /// // subject performs better than what would expected by chance. 
    /// // In other words, that the person's probability of predicting
    /// // a card is higher than the chance hypothesized value of 1/4.
    /// 
    /// BinomialTest test = new BinomialTest(
    ///     successes: 13, trials: 25,
    ///     hypothesizedProbability: 1.0 / 4.0,
    ///     alternate: OneSampleHypothesis.ValueIsGreaterThanHypothesis);
    /// 
    /// Console.WriteLine("Test p-Value: " + test.PValue);     // ~ 0.003
    /// Console.WriteLine("Significant? " + test.Significant); // True.
    /// </code>
    /// </example>
    /// <seealso cref="BinomialDistribution" />
    [Serializable]
    public class BinomialTest : HypothesisTest<BinomialDistribution>
    {
        /// <summary>
        ///     Tests the probability of two outcomes in a series of experiments.
        /// </summary>
        /// <param name="trials">The experimental trials.</param>
        /// <param name="hypothesizedProbability">The hypothesized occurrence probability.</param>
        /// <param name="alternate">The alternative hypothesis (research hypothesis) to test.</param>
        public BinomialTest(bool[] trials, double hypothesizedProbability = 0.5,
            OneSampleHypothesis alternate = OneSampleHypothesis.ValueIsDifferentFromHypothesis)
        {
            if (trials == null)
            {
                throw new ArgumentNullException("trials");
            }

            if (hypothesizedProbability < 0 || hypothesizedProbability > 1.0)
            {
                throw new ArgumentOutOfRangeException("hypothesizedProbability");
            }

            var successes = 0;

            for (var i = 0; i < trials.Length; i++)
            {
                if (trials[i])
                {
                    successes++;
                }
            }

            Compute(successes, trials.Length, hypothesizedProbability, alternate);
        }

        /// <summary>
        ///     Tests the probability of two outcomes in a series of experiments.
        /// </summary>
        /// <param name="successes">The number of successes in the trials.</param>
        /// <param name="trials">The total number of experimental trials.</param>
        /// <param name="hypothesizedProbability">The hypothesized occurrence probability.</param>
        /// <param name="alternate">The alternative hypothesis (research hypothesis) to test.</param>
        public BinomialTest(int successes, int trials, double hypothesizedProbability = 0.5,
            OneSampleHypothesis alternate = OneSampleHypothesis.ValueIsDifferentFromHypothesis)
        {
            if (successes > trials)
            {
                throw new ArgumentOutOfRangeException("successes");
            }

            if (hypothesizedProbability < 0 || hypothesizedProbability > 1.0)
            {
                throw new ArgumentOutOfRangeException("hypothesizedProbability");
            }

            Compute(successes, trials, hypothesizedProbability, alternate);
        }

        /// <summary>
        ///     Creates a Binomial test.
        /// </summary>
        protected BinomialTest()
        {
        }

        /// <summary>
        ///     Gets the alternative hypothesis under test. If the test is
        ///     <see cref="IHypothesisTest.Significant" />, the null hypothesis can be rejected
        ///     in favor of this alternative hypothesis.
        /// </summary>
        public OneSampleHypothesis Hypothesis { get; protected set; }

        /// <summary>
        ///     Computes the Binomial test.
        /// </summary>
        protected void Compute(double statistic, int m, double p, OneSampleHypothesis alternate)
        {
            Statistic = statistic;
            StatisticDistribution = new BinomialDistribution(m, p);
            Hypothesis = alternate;
            Tail = (DistributionTail)alternate;
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
            double p;
            switch (Tail)
            {
                case DistributionTail.TwoTail:
                    p = wilsonSterne(x);
                    break;

                case DistributionTail.OneUpper:
                    p = StatisticDistribution.ComplementaryDistributionFunction((int)x, true);
                    break;

                case DistributionTail.OneLower:
                    p = StatisticDistribution.DistributionFunction((int)x);
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
                    throw new NotSupportedException();

                default: throw new InvalidOperationException();
            }

            return b;
        }

        /// <summary>
        ///     Computes the two-tail probability using the Wilson-Sterne rule,
        ///     which defines the tail of the distribution based on a ordering
        ///     of the null probabilities of X. (Smirnoff, 2003)
        /// </summary>
        /// <remarks>
        ///     References: Jeffrey S. Simonoff, Analyzing
        ///     Categorical Data, Springer, 2003 (pg 64).
        /// </remarks>
        private double wilsonSterne(double x)
        {
            var mean = StatisticDistribution.Mean;

            if (x == mean)
            {
                return 1;
            }


            var trials = StatisticDistribution.NumberOfTrials;

            var observed = StatisticDistribution.ProbabilityMassFunction((int)x);
            var threshold = observed * (1.0 + 1e-7);

            double sum = 0;
            for (var i = 0; i <= trials; i++)
            {
                var p = StatisticDistribution.ProbabilityMassFunction(i);
                if (p <= threshold)
                {
                    sum += p;
                }
            }

            return System.Math.Min(1.0, sum);
        }
    }
}