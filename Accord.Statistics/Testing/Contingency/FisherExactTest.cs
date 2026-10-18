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
using Accord.Statistics.Analysis;
using Accord.Statistics.Distributions.Univariate;

namespace Accord.Statistics.Testing
{
    /// <summary>
    ///     Fisher's exact test for contingency tables.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This test statistic distribution is the
    ///         <see cref="HypergeometricDistribution">
    ///             Hypergeometric
    ///         </see>
    ///         .
    ///     </para>
    /// </remarks>
    /// <seealso cref="HypergeometricDistribution" />
    /// <example>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Testing\Contingency\FisherExactTestTest.cs" region="doc_test" />
    /// </example>
    [Serializable]
    public class FisherExactTest : HypothesisTest<HypergeometricDistribution>
    {
        /// <summary>
        ///     Constructs a new Fisher's exact test.
        /// </summary>
        /// <param name="matrix">The matrix to be tested.</param>
        /// <param name="alternate">The alternative hypothesis (research hypothesis) to test.</param>
        public FisherExactTest(ConfusionMatrix matrix,
            OneSampleHypothesis alternate = OneSampleHypothesis.ValueIsDifferentFromHypothesis)
        {
            if (matrix == null)
            {
                throw new ArgumentNullException("matrix");
            }

            var mat = matrix.Matrix;

            var a = mat[0, 0];
            var b = mat[0, 1];
            var c = mat[1, 0];
            var d = mat[1, 1];

            var N = matrix.NumberOfSamples; // total number in population
            var k = a; // number of success in sample
            var m = a + b; // number of success in population
            var n = a + c; // sample size

            Debug.Assert(a + b + c + d == N);

            Compute(k, N, n, m, alternate);
        }

        /// <summary>
        ///     Gets the alternative hypothesis under test. If the test is
        ///     <see cref="IHypothesisTest.Significant" />, the null hypothesis
        ///     can be rejected in favor of this alternative hypothesis.
        /// </summary>
        public OneSampleHypothesis Hypothesis { get; protected set; }

        /// <summary>
        ///     Computes the Fisher's exact test.
        /// </summary>
        protected void Compute(double statistic, int populationSize, int n, int m, OneSampleHypothesis alternate)
        {
            Statistic = statistic;
            StatisticDistribution = new HypergeometricDistribution(populationSize, m, n);

            Hypothesis = alternate;
            Tail = (DistributionTail)alternate;
            PValue = StatisticToPValue(Statistic);
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
                    var observed = StatisticDistribution.ProbabilityMassFunction((int)x);
                    var threshold = observed * (1.0 + 1e-7);
                    double sum = 0;
                    var support = StatisticDistribution.Support;
                    for (var k = support.Min; k <= support.Max; k++)
                    {
                        var pk = StatisticDistribution.ProbabilityMassFunction(k);
                        if (pk <= threshold)
                        {
                            sum += pk;
                        }
                    }

                    p = System.Math.Min(1.0, sum);
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
            throw new NotImplementedException();
        }
    }
}