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
using Accord.Statistics.Analysis;
using Accord.Statistics.Distributions;
using Accord.Statistics.Distributions.Univariate;

namespace Accord.Statistics.Testing
{
    /// <summary>
    ///     Two-Sample (Goodness-of-fit) Chi-Square Test (Upper Tail)
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A chi-square test (also chi-squared or χ² test) is any statistical
    ///         hypothesis test in which the sampling distribution of the test statistic
    ///         is a <see cref="ChiSquareDistribution">chi-square distribution</see> when
    ///         the null hypothesis is true, or any in which this is asymptotically true,
    ///         meaning that the sampling distribution (if the null hypothesis is true)
    ///         can be made to approximate a chi-square distribution as closely as desired
    ///         by making the sample size large enough.
    ///     </para>
    ///     <para>
    ///         The chi-square test is used whenever one would like to test whether the
    ///         actual data differs from a random distribution.
    ///     </para>
    ///     <para>
    ///         References:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <a href="http://en.wikipedia.org/wiki/Chi-square_test">
    ///                         Wikipedia, The Free Encyclopedia. Chi-Square Test. Available on:
    ///                         http://en.wikipedia.org/wiki/Chi-square_test
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <a href="http://www2.lv.psu.edu/jxm57/irp/chisquar.html">
    ///                         J. S. McLaughlin. Chi-Square Test. Available on:
    ///                         http://www2.lv.psu.edu/jxm57/irp/chisquar.html
    ///                     </a>
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         The following example has been based on the example section
    ///         of the
    ///         <a href="http://en.wikipedia.org/wiki/Pearson's_chi-squared_test#Goodness_of_fit">
    ///             Pearson's chi-squared test
    ///         </a>
    ///         article on Wikipedia.
    ///     </para>
    ///     <code>
    /// // Suppose we would like to test the hypothesis that a random sample of 
    /// // 100 people has been drawn from a population in which men and women are
    /// // equal in frequency. 
    /// 
    /// // Under this hypothesis, the observed number of men and women would be 
    /// // compared to the theoretical frequencies of 50 men and 50 women. So,
    /// // after drawing our sample, we found out that there were 44 men and 56
    /// // women in the sample:
    /// 
    /// //                     man  woman
    /// double[] observed = {  44,   56  };
    /// double[] expected = {  50,   50  };
    /// 
    /// // If the null hypothesis is true (i.e., men and women are chosen with 
    /// // equal probability), the test statistic will be drawn from a chi-squared
    /// // distribution with one degree of freedom. If the male frequency is known, 
    /// // then the female frequency is determined.
    /// //
    /// int degreesOfFreedom = 1;
    /// 
    /// // So now we have:
    /// //
    /// var chi = new ChiSquareTest(expected, observed, degreesOfFreedom);
    /// 
    /// 
    /// // The chi-squared distribution for 1 degree of freedom shows that the 
    /// // probability of observing this difference (or a more extreme difference 
    /// // than this) if men and women are equally numerous in the population is 
    /// // approximately 0.23. 
    ///             
    /// double pvalue = chi.PValue; // 0.23
    ///             
    /// // This probability is higher than conventional criteria for statistical
    /// // significance (0.001 or 0.05), so normally we would not reject the null
    /// // hypothesis that the number of men in the population is the same as the
    /// // number of women.
    /// 
    /// bool significant = chi.Significant; // false
    /// </code>
    /// </example>
    /// <seealso cref="ChiSquareDistribution" />
    [Serializable]
    public class ChiSquareTest : HypothesisTest<ChiSquareDistribution>
    {
        /// <summary>
        ///     Constructs a Chi-Square Test.
        /// </summary>
        /// <param name="statistic">The test statistic.</param>
        /// <param name="degreesOfFreedom">The chi-square distribution degrees of freedom.</param>
        public ChiSquareTest(double statistic, int degreesOfFreedom)
        {
            Compute(statistic, degreesOfFreedom);
        }

        /// <summary>
        ///     Constructs a Chi-Square Test.
        /// </summary>
        /// <param name="expected">The expected variable values.</param>
        /// <param name="observed">The observed variable values.</param>
        /// <param name="degreesOfFreedom">The chi-square distribution degrees of freedom.</param>
        public ChiSquareTest(double[] expected, double[] observed, int degreesOfFreedom)
        {
            if (expected == null)
            {
                throw new ArgumentNullException("expected");
            }

            if (observed == null)
            {
                throw new ArgumentNullException("observed");
            }

            var sum = compute(expected, observed);

            Compute(sum, degreesOfFreedom);
        }

        /// <summary>
        ///     Constructs a Chi-Square Test.
        /// </summary>
        public ChiSquareTest(ConfusionMatrix matrix, bool yatesCorrection = false)
        {
            if (matrix == null)
            {
                throw new ArgumentNullException("matrix");
            }


            var row = matrix.RowTotals;
            var col = matrix.ColumnTotals;
            var values = matrix.Matrix;
            var samples = matrix.NumberOfSamples;

            var chiSquare = compute(values, row, col, samples, yatesCorrection);

            Compute(chiSquare, 1);
        }

        /// <summary>
        ///     Constructs a Chi-Square Test.
        /// </summary>
        public ChiSquareTest(GeneralConfusionMatrix matrix, bool yatesCorrection = false)
        {
            if (matrix == null)
            {
                throw new ArgumentNullException("matrix");
            }


            var df = (matrix.NumberOfClasses - 1) * (matrix.NumberOfClasses - 1);

            var row = matrix.RowTotals;
            var col = matrix.ColumnTotals;
            var values = matrix.Matrix;
            var samples = matrix.NumberOfSamples;

            var chiSquare = compute(values, row, col, samples, yatesCorrection);

            Compute(chiSquare, df);
        }

        /// <summary>
        ///     Constructs a Chi-Square Test.
        /// </summary>
        public ChiSquareTest(double[] observations, IUnivariateDistribution<double> hypothesizedDistribution)
        {
            var n = observations.Length;
            var E = new EmpiricalDistribution(observations);
            var F = hypothesizedDistribution;


            // Create bins with the observations
            var bins = (int)System.Math.Ceiling(1 + System.Math.Log(observations.Length));

            var ebins = new double[bins + 1];

            for (var i = 0; i <= bins; i++)
            {
                var p = i / (double)bins;
                ebins[i] = F.InverseDistributionFunction(p);
            }


            var expected = new double[bins - 1];

            var size = expected.Length;
            for (var i = 0; i < expected.Length; i++)
            {
                var a = ebins[i];
                var b = ebins[i + 1];

                if (double.IsPositiveInfinity(b))
                {
                    break;
                }

                var Fa = F.DistributionFunction(a);
                var Fb = F.DistributionFunction(b);
                var samples = System.Math.Abs(Fb - Fa) * n;

                expected[i] = samples;

                if (samples < 5)
                {
                    size = i + 1;
                    for (var j = i + 1; j < ebins.Length - 1; j++)
                    {
                        ebins[j] = ebins[j + 1];
                    }

                    ebins[ebins.Length - 1] = double.PositiveInfinity;

                    i--;
                }
            }


            ebins = ebins.First(size + 2);
            expected = expected.First(ebins.Length - 2);

            var observed = new double[expected.Length];
            for (var i = 0; i < observed.Length; i++)
            {
                var a = ebins[i];
                var b = ebins[i + 1];
                observed[i] = E.DistributionFunction(a, b) * n;
            }


            var sum = compute(expected, observed);

            Compute(sum, bins - 1);
        }

        /// <summary>
        ///     Constructs a Chi-Square Test.
        /// </summary>
        protected ChiSquareTest()
        {
        }

        /// <summary>
        ///     Gets the degrees of freedom for the Chi-Square distribution.
        /// </summary>
        public int DegreesOfFreedom => StatisticDistribution.DegreesOfFreedom;

        /// <summary>
        ///     Computes the Chi-Square Test.
        /// </summary>
        protected void Compute(double statistic, int degreesOfFreedom)
        {
            Statistic = statistic;
            StatisticDistribution = new ChiSquareDistribution(degreesOfFreedom);

            Tail = DistributionTail.OneUpper;
            PValue = StatisticDistribution.ComplementaryDistributionFunction(Statistic);
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

        /// <summary>
        ///     Converts a given p-value to a test statistic.
        /// </summary>
        /// <param name="p">The p-value.</param>
        /// <returns>The test statistic which would generate the given p-value.</returns>
        public override double PValueToStatistic(double p)
        {
            throw new NotSupportedException();
        }

        private static double compute(double[] expected, double[] observed)
        {
            // X² = sum(o - e)²
            //          -----
            //            e

            var sum = 0.0;
            for (var i = 0; i < observed.Length; i++)
            {
                var d = observed[i] - expected[i];

                if (d != 0)
                {
                    sum += d * d / expected[i];
                }
            }

            return sum;
        }

        private static double compute(int[,] values, int[] row, int[] col, int samples, bool yatesCorrection)
        {
            // X² = sum(o - e)²
            //          -----
            //            e

            double x = 0;

            for (var i = 0; i < row.Length; i++)
            {
                for (var j = 0; j < col.Length; j++)
                {
                    var e = row[i] * col[j] / (double)samples;
                    double o = values[i, j];

                    var u = o - e;

                    if (yatesCorrection)
                    {
                        u = System.Math.Abs(u) - 0.5;
                    }

                    if (u != 0)
                    {
                        x += u * u / e;
                    }
                }
            }

            return x;
        }
    }
}