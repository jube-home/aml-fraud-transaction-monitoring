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
using System.Threading.Tasks;
using Accord.Math;
using Accord.Statistics.Distributions;
using Accord.Statistics.Distributions.Univariate;

namespace Accord.Statistics.Testing
{
    /// <summary>
    ///     One sample Lilliefors' corrected Kolmogorov-Smirnov (KS) test.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         In statistics, the Lilliefors test, named after Hubert Lilliefors, professor of statistics at George
    ///         Washington University, is a test based on the Kolmogorov–Smirnov test. It is used to test the null
    ///         hypothesis that data come from a normally distributed population, when the null hypothesis does not
    ///         specify which normal distribution; i.e., it does not specify the expected value and variance of the
    ///         distribution.
    ///     </para>
    ///     <para>
    ///         Contrary to the <see cref="KolmogorovSmirnovTest">Kolmogorov-Smirnov</see> test, this test can be used
    ///         to assess the likelihood that a given sample could have been generated from a distribution that has been
    ///         fitted from the data.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="https://en.wikipedia.org/wiki/Lilliefors_test">
    ///                         Wikipedia, The Free Encyclopedia. Lilliefors Test.
    ///                         Available on: https://en.wikipedia.org/wiki/Lilliefors_test
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         In this first example, suppose we got a new sample, and we would
    ///         like to test whether this sample has been originated from a uniform
    ///         continuous distribution. Unlike <see cref="KolmogorovSmirnovTest" />,
    ///         we can actually use this test whether the data fits a distribution
    ///         that has been estimated from the data.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Testing\LillieforsTestTest.cs" region="doc_uniform" />
    ///     <para>
    ///         We can also check whether a Normal distribution fitted on the data
    ///         is a good candidate model for the samples:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Testing\LillieforsTestTest.cs" region="doc_normal" />
    /// </example>
    /// <seealso cref="KolmogorovSmirnovDistribution" />
    [Serializable]
    public class LillieforsTest : HypothesisTest<EmpiricalDistribution>,
        IHypothesisTest<EmpiricalDistribution>
    {
        private LillieforsTest()
        {
            // this.ParallelOptions = new ParallelOptions();
        }

        /// <summary>
        ///     Creates a new One-Sample Lilliefors' Kolmogorov-Smirnov test.
        /// </summary>
        /// <param name="sample">The sample we would like to test as belonging to the <paramref name="hypothesizedDistribution" />.</param>
        /// <param name="hypothesizedDistribution">A fully specified distribution (which could have been estimated from the data).</param>
        /// <param name="alternate">The alternative hypothesis (research hypothesis) to test.</param>
        /// <param name="iterations">The number of Monte-Carlo iterations to perform. Default is 10,000.</param>
        /// <param name="reestimate">
        ///     Whether the target distribution should be re-estimated from the sampled data
        ///     at each Monte-Carlo iteration. Pass true in case <paramref name="hypothesizedDistribution" /> has been
        ///     estimated from the data.
        /// </param>
        public LillieforsTest(double[] sample, ISampleableDistribution<double> hypothesizedDistribution,
            KolmogorovSmirnovTestHypothesis alternate = KolmogorovSmirnovTestHypothesis.SampleIsDifferent,
            int iterations = 10000, bool reestimate = true)
            : this()
        {
            if (reestimate)
            {
                if (!(hypothesizedDistribution is IFittableDistribution<double>))
                {
                    throw new InvalidOperationException(
                        "The estimate option can only be used with distributions that implement IFittableDistribution<double>.");
                }
            }

            Hypothesis = alternate;

            // Create a copy of the samples to prevent altering the
            // constructor's original arguments in the sorting step 
            var orderedSamples = sample.Sorted();

            // Create the theoretical and empirical distributions
            TheoreticalDistribution = hypothesizedDistribution;
            EmpiricalDistribution = new EmpiricalDistribution(orderedSamples, 0);
            NumberOfSamples = sample.Length;

            StatisticDistribution = GetSimulatedDistribution(hypothesizedDistribution,
                reestimate, iterations, alternate);

            // Finally, compute the test statistic and perform actual testing.
            Statistic = KolmogorovSmirnovTest.GetStatistic(orderedSamples, TheoreticalDistribution, alternate);
            Tail = (DistributionTail)alternate;
            PValue = StatisticToPValue(Statistic);
        }

        /// <summary>
        ///     Gets the alternative hypothesis under test. If the test is
        ///     <see cref="IHypothesisTest.Significant" />, the null hypothesis
        ///     can be rejected
        ///     in favor of this alternative hypothesis.
        /// </summary>
        public KolmogorovSmirnovTestHypothesis Hypothesis { get; private set; }

        /// <summary>
        ///     Gets the hypothesized distribution for the samples.
        /// </summary>
        public ISampleableDistribution<double> TheoreticalDistribution { get; private set; }

        /// <summary>
        ///     Gets the empirical distribution measured from the sample.
        /// </summary>
        public EmpiricalDistribution EmpiricalDistribution { get; private set; }

        ///// <summary>
        /////   Gets or sets the parallelization options used when simulating 
        /////   the KS statistic distribution using the Monte-Carlo method.
        ///// </summary>
        ///// 
        //public ParallelOptions ParallelOptions { get; set; }

        /// <summary>
        ///     Gets the number of observations in the sample being tested.
        /// </summary>
        public int NumberOfSamples { get; private set; }

        /// <summary>
        ///     Converts a given p-value to a test statistic.
        /// </summary>
        /// <param name="p">The p-value.</param>
        /// <returns>The test statistic which would generate the given p-value.</returns>
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
            return StatisticDistribution.ComplementaryDistributionFunction(x);
        }

        private EmpiricalDistribution GetSimulatedDistribution(ISampleableDistribution<double> hypothesizedDistribution,
            bool reestimate, int iterations, KolmogorovSmirnovTestHypothesis alternate)
        {
            var samples = new double[iterations];

            if (reestimate)
            {
                Parallel.For(0, iterations, i => // TODO: Use ParallelOptions
                {
                    var s = hypothesizedDistribution.Generate(samples: NumberOfSamples);
                    s.Sort();

                    var fittable = (IFittableDistribution<double>)hypothesizedDistribution.Clone();
                    fittable.Fit(s);

                    samples[i] = KolmogorovSmirnovTest.GetStatistic(s, fittable, alternate);
                });
            }
            else
            {
                Parallel.For(0, iterations, i => // TODO: Use ParallelOptions
                {
                    var s = hypothesizedDistribution.Generate(samples: NumberOfSamples);
                    s.Sort();
                    samples[i] = KolmogorovSmirnovTest.GetStatistic(s, hypothesizedDistribution, alternate);
                });
            }


            return new EmpiricalDistribution(samples, 0);
        }

        /// <summary>
        ///     Performs a Goodness-of-Fit method by automatically creating and fitting
        ///     the chosen distribution to the samples and computing a <see cref="LillieforsTest" />
        ///     against this fitted distribution.
        /// </summary>
        /// <typeparam name="TDistribution">The type of the distribution.</typeparam>
        /// <param name="samples">The samples used to fit the distribution.</param>
        /// <returns>
        ///     A Lilliefor Test assessing whether it is likely that the samples
        ///     could have been generated by the chosen distribution.
        /// </returns>
        public static LillieforsTest GoodnessOfFit<TDistribution>(double[] samples)
            where TDistribution : IFittableDistribution<double[]>,
            ISampleableDistribution<double>,
            new()
        {
            var dist = new TDistribution();
            dist.Fit(samples);
            return new LillieforsTest(samples, dist);
        }
    }
}