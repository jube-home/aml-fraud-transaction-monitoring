/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using FluentAssertions;
using Jube.Analytics.Bootstrap;
using Xunit;

namespace Jube.Test.Analytics
{
    public class DVineCopulaTests
    {
        private static double Correlation(double[] a, double[] b)
        {
            var meanA = a.Average();
            var meanB = b.Average();
            var covariance = 0d;
            var varA = 0d;
            var varB = 0d;

            for (var i = 0; i < a.Length; i++)
            {
                covariance += (a[i] - meanA) * (b[i] - meanB);
                varA += (a[i] - meanA) * (a[i] - meanA);
                varB += (b[i] - meanB) * (b[i] - meanB);
            }

            return covariance / Math.Sqrt(varA * varB);
        }

        private static double[][] SimulateGaussianChain(int n, double rho12, double rho23, int seed)
        {
            var random = new Random(seed);

            double Normal()
            {
                var u1 = 1d - random.NextDouble();
                var u2 = random.NextDouble();

                return Math.Sqrt(-2d * Math.Log(u1)) * Math.Cos(2d * Math.PI * u2);
            }

            var x1 = new double[n];
            var x2 = new double[n];
            var x3 = new double[n];

            for (var i = 0; i < n; i++)
            {
                x1[i] = Normal();
                x2[i] = rho12 * x1[i] + Math.Sqrt(1d - rho12 * rho12) * Normal();
                x3[i] = rho23 * x2[i] + Math.Sqrt(1d - rho23 * rho23) * Normal();
            }

            return [x1, x2, x3];
        }

        private static double StandardDeviation(double[] values)
        {
            var mean = values.Average();
            var variance = values.Sum(v => (v - mean) * (v - mean)) / (values.Length - 1);

            return Math.Sqrt(variance);
        }

        [Fact]
        public void Sample_RecoversPairwiseCorrelationsOfAKnownGaussianChain()
        {
            const double rho12 = 0.7;
            const double rho23 = 0.6;
            var columns = SimulateGaussianChain(3000, rho12, rho23, 7);

            var vine = DVineCopula.Fit(columns);
            var synthetic = vine.Sample(6000, new Random(9));

            var s1 = synthetic.Select(r => r[0]).ToArray();
            var s2 = synthetic.Select(r => r[1]).ToArray();
            var s3 = synthetic.Select(r => r[2]).ToArray();

            Correlation(s1, s2).Should().BeApproximately(rho12, 0.08);
            Correlation(s2, s3).Should().BeApproximately(rho23, 0.08);

            Correlation(s1, s3).Should().BeApproximately(rho12 * rho23, 0.1,
                "the vine's tree-2 pair copula should recover the Markov chain's implied indirect correlation");
        }

        [Fact]
        public void Sample_ReproducesEachColumnsMarginalMeanAndStandardDeviation()
        {
            var columns = SimulateGaussianChain(2000, 0.5, 0.3, 13);
            var vine = DVineCopula.Fit(columns);
            var synthetic = vine.Sample(4000, new Random(17));

            for (var col = 0; col < columns.Length; col++)
            {
                var original = columns[col];
                var syntheticColumn = synthetic.Select(r => r[col]).ToArray();

                syntheticColumn.Average().Should().BeApproximately(original.Average(), 0.1);

                StandardDeviation(syntheticColumn).Should()
                    .BeApproximately(StandardDeviation(original), StandardDeviation(original) * 0.2);
            }
        }

        [Fact]
        public void Fit_TruncatedAtTreeOne_StillProducesAValidSample()
        {
            var columns = SimulateGaussianChain(1500, 0.6, 0.5, 19);
            var vine = DVineCopula.Fit(columns, 1);

            var synthetic = vine.Sample(500, new Random(29));

            synthetic.Should().HaveCount(500);
            synthetic.SelectMany(r => r).Should().OnlyContain(v => double.IsFinite(v));
        }

        [Fact]
        public void Sample_TwoVariables_RecoversTheSingleCorrelation()
        {
            var random = new Random(31);

            double Normal()
            {
                var u1 = 1d - random.NextDouble();
                var u2 = random.NextDouble();

                return Math.Sqrt(-2d * Math.Log(u1)) * Math.Cos(2d * Math.PI * u2);
            }

            const double rho = 0.65;
            var n = 2000;
            var x1 = new double[n];
            var x2 = new double[n];

            for (var i = 0; i < n; i++)
            {
                x1[i] = Normal();
                x2[i] = rho * x1[i] + Math.Sqrt(1d - rho * rho) * Normal();
            }

            var vine = DVineCopula.Fit([x1, x2]);
            var synthetic = vine.Sample(4000, new Random(41));

            var s1 = synthetic.Select(r => r[0]).ToArray();
            var s2 = synthetic.Select(r => r[1]).ToArray();

            Correlation(s1, s2).Should().BeApproximately(rho, 0.08);
        }
    }
}