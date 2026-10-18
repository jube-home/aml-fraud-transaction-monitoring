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
    ///     Grubb's Test for Outliers (for approximately Normal distributions).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Grubbs' test (named after Frank E. Grubbs, who published the test in 1950),
    ///         also known as the maximum normed residual test or extreme studentized deviate
    ///         test, is a statistical test used to detect outliers in a univariate data set
    ///         assumed to come from a normally distributed population.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="https://en.wikipedia.org/wiki/Grubbs%27_test_for_outliers">
    ///                         Wikipedia, The Free Encyclopedia. Grubb's test for outliers. Available on:
    ///                         https://en.wikipedia.org/wiki/Grubbs%27_test_for_outliers
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <seealso cref="NormalDistribution" />
    [Serializable]
    public class GrubbTest : HypothesisTest<GrubbDistribution>
    {
        /// <summary>
        ///     Constructs a Grubb's test.
        /// </summary>
        public GrubbTest(double[] samples, GrubbTestHypothesis hypothesis = GrubbTestHypothesis.ThereAreOutliers)
        {
            NumberOfSamples = samples.Length;
            Mean = samples.Mean();
            StandardDeviation = samples.StandardDeviation(Mean);

            // Compute Grubb's statistic
            Min = Max = samples[0] - Mean;
            for (var i = 1; i < samples.Length; i++)
            {
                var z = samples[i] - Mean;
                if (z > Max)
                {
                    Max = z;
                }

                if (z < Min)
                {
                    Min = z;
                }
            }

            Abs = System.Math.Max(-Min, Max);

            Statistic = g(hypothesis) / StandardDeviation;
            StatisticDistribution = new GrubbDistribution(NumberOfSamples);
            Tail = DistributionTail.OneUpper;
            Hypothesis = hypothesis;

            // Compute the transformed p-value
            PValue = StatisticToPValue(Statistic);
        }

        /// <summary>
        ///     Gets the number of observations in the sample.
        /// </summary>
        /// <value>The number of observations in the sample.</value>
        public int NumberOfSamples { get; private set; }

        /// <summary>
        ///     Gets the sample's mean.
        /// </summary>
        public double Mean { get; private set; }

        /// <summary>
        ///     Gets the sample's standard deviation.
        /// </summary>
        public double StandardDeviation { get; private set; }

        /// <summary>
        ///     Gets the difference between the minimum value and the mean.
        /// </summary>
        public double Min { get; }

        /// <summary>
        ///     Gets the difference between the maximum value and the mean.
        /// </summary>
        public double Max { get; }

        /// <summary>
        ///     Gets the maximum absolute difference between
        ///     an observation in the sample and the mean.
        /// </summary>
        public double Abs { get; }

        /// <summary>
        ///     Gets the <see cref="GrubbTestHypothesis" /> being tested.
        /// </summary>
        public GrubbTestHypothesis Hypothesis { get; private set; }

        private double g(GrubbTestHypothesis hypothesis)
        {
            switch (hypothesis)
            {
                case GrubbTestHypothesis.ThereAreOutliers:
                    return Abs;
                case GrubbTestHypothesis.TheMaximumIsAnOutlier:
                    return Max;
                case GrubbTestHypothesis.TheMinimumIsAnOutlier:
                    return -Min;
            }

            throw new ArgumentOutOfRangeException("hypothesis");
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
                    p = 2.0 * StatisticDistribution.ComplementaryDistributionFunction(System.Math.Abs(x));
                    break;

                case DistributionTail.OneUpper:
                    p = StatisticDistribution.ComplementaryDistributionFunction(x);
                    break;

                case DistributionTail.OneLower:
                    p = StatisticDistribution.DistributionFunction(x);
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
            double t;
            switch (Tail)
            {
                case DistributionTail.OneLower:
                    t = StatisticDistribution.InverseDistributionFunction(p);
                    break;
                case DistributionTail.OneUpper:
                    t = StatisticDistribution.InverseDistributionFunction(1.0 - p);
                    break;
                case DistributionTail.TwoTail:
                    t = StatisticDistribution.InverseDistributionFunction(1.0 - p / 2.0);
                    break;
                default:
                    throw new InvalidOperationException();
            }

            return t;
        }
    }
}