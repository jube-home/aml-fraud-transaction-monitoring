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
using System.Numerics;
using Accord.Math;
using Accord.Math.Transforms;
using Accord.Statistics.Distributions.Fitting;
using Accord.Statistics.Distributions.Univariate;
using Accord.Statistics.Models.Regression;
using FluentAssertions;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Library
{
    public class SurvivalAndSpectralLibraryFixesTests
    {
        [Fact]
        public void KaplanMeierSurvivalDropsToZeroAtAndAfterTheLastFailure()
        {
            var distribution = new EmpiricalHazardDistribution(SurvivalEstimator.KaplanMeier);

            distribution.Fit([1, 2, 3, 4, 5], null,
                new EmpiricalHazardOptions { Estimator = HazardEstimator.KaplanMeier });

            distribution.ComplementaryDistributionFunction(0.5).Should().BeApproximately(1.0, 1e-12);
            distribution.ComplementaryDistributionFunction(2.5).Should().BeApproximately(0.6, 1e-12);
            distribution.ComplementaryDistributionFunction(4.0).Should().BeApproximately(0.2, 1e-12);
            distribution.ComplementaryDistributionFunction(5.0).Should().BeApproximately(0.0, 1e-12);
            distribution.ComplementaryDistributionFunction(9.0).Should().BeApproximately(0.0, 1e-12);
        }

        [Fact]
        public void KaplanMeierSurvivalStaysPositiveAfterACensoredLastObservation()
        {
            var distribution = new EmpiricalHazardDistribution(SurvivalEstimator.KaplanMeier);

            distribution.Fit([1, 2, 3, 4, 5], null,
                new EmpiricalHazardOptions
                {
                    Estimator = HazardEstimator.KaplanMeier,
                    Outcome =
                    [
                        SurvivalOutcome.Failed, SurvivalOutcome.Failed, SurvivalOutcome.Failed,
                        SurvivalOutcome.Failed, SurvivalOutcome.Censored
                    ]
                });

            distribution.ComplementaryDistributionFunction(4.5).Should().BeApproximately(0.2, 1e-12);
            distribution.ComplementaryDistributionFunction(5.5).Should().BeApproximately(0.2, 1e-12);
        }

        [Fact]
        public void SurvivalSortOrdersLaterTimesFirstWithCensoredBeforeFailuresAtATie()
        {
            var time = new[] { 5.0, 5.0, 5.000001, 3.0 };

            var outcome = new[]
            {
                SurvivalOutcome.Failed, SurvivalOutcome.Censored, SurvivalOutcome.Failed, SurvivalOutcome.Censored
            };

            EmpiricalHazardDistribution.Sort(ref time, ref outcome);

            time.Should().Equal(5.000001, 5.0, 5.0, 3.0);

            outcome.Should().Equal(
                SurvivalOutcome.Failed, SurvivalOutcome.Censored, SurvivalOutcome.Failed, SurvivalOutcome.Censored);
        }

        [Fact]
        public void SurvivalSortIsStableForIdenticalTimesAndOutcomes()
        {
            var time = new[] { 2.0, 2.0, 2.0 };
            var outcome = new[] { SurvivalOutcome.Failed, SurvivalOutcome.Failed, SurvivalOutcome.Failed };

            var index = EmpiricalHazardDistribution.Sort(ref time, ref outcome);

            index.Should().Equal(0, 1, 2);
        }

        [Fact]
        public void LogisticRegressionConfidenceIntervalUsesTheRequestedNormalQuantile()
        {
            var model = new LogisticRegression
            {
                NumberOfInputs = 1,
                Intercept = 0.5,
                Weights = [1.0],
                StandardErrors = [0.2, 0.3]
            };

            var ninetyFive = model.GetConfidenceInterval(1);
            var ninetyNine = model.GetConfidenceInterval(1, 0.99);

            ninetyFive.Min.Should().BeApproximately(Math.Exp(1.0 - 1.959963984540054 * 0.3), 1e-9);
            ninetyFive.Max.Should().BeApproximately(Math.Exp(1.0 + 1.959963984540054 * 0.3), 1e-9);
            ninetyNine.Min.Should().BeApproximately(Math.Exp(1.0 - 2.5758293035489004 * 0.3), 1e-9);
            ninetyNine.Max.Should().BeApproximately(Math.Exp(1.0 + 2.5758293035489004 * 0.3), 1e-9);
        }

        [Fact]
        public void WeibullFitSatisfiesTheMaximumLikelihoodEquations()
        {
            var n = 200;

            var sample = Enumerable.Range(0, n)
                .Select(i => 3.0 * Math.Pow(-Math.Log(1 - (i + 0.5) / n), 1 / 2.0))
                .ToArray();

            var distribution = new WeibullDistribution(1, 1);
            distribution.Fit(sample);

            var k = distribution.Shape;

            var score = sample.Sum(v => Math.Pow(v, k) * Math.Log(v)) / sample.Sum(v => Math.Pow(v, k))
                        - 1 / k - sample.Average(Math.Log);
            score.Should().BeApproximately(0, 1e-8);
            distribution.Scale.Should().BeApproximately(Math.Pow(sample.Average(v => Math.Pow(v, k)), 1 / k), 1e-9);
            k.Should().BeApproximately(2.0, 0.15);
            distribution.Scale.Should().BeApproximately(3.0, 0.15);
        }

        [Fact]
        public void WeibullFitWithIntegerWeightsMatchesReplicatedObservations()
        {
            double[] values = [0.8, 1.3, 2.1, 2.9, 4.4];
            double[] weights = [1, 2, 1, 3, 1];
            var replicated = values.SelectMany((v, i) => Enumerable.Repeat(v, (int)weights[i])).ToArray();

            var weighted = new WeibullDistribution(1, 1);
            weighted.Fit(values, weights);
            var plain = new WeibullDistribution(1, 1);
            plain.Fit(replicated);

            weighted.Shape.Should().BeApproximately(plain.Shape, 1e-9);
            weighted.Scale.Should().BeApproximately(plain.Scale, 1e-9);
        }

        [Theory]
        [InlineData(8)]
        [InlineData(7)]
        public void CircularConvolutionMatchesTheDirectDefinition(int n)
        {
            var random = new Random(3);
            var a = Enumerable.Range(0, n).Select(_ => random.NextDouble()).ToArray();
            var b = Enumerable.Range(0, n).Select(_ => random.NextDouble()).ToArray();
            var expected = new double[n];

            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < n; j++)
                {
                    expected[(i + j) % n] += a[i] * b[j];
                }
            }

            var result = new double[n];
            FourierTransform2.Convolve((double[])a.Clone(), (double[])b.Clone(), result);

            for (var i = 0; i < n; i++)
            {
                result[i].Should().BeApproximately(expected[i], 1e-9);
            }

            var complexResult = new Complex[n];

            FourierTransform2.Convolve(
                [.. a.Select(v => new Complex(v, 0))], [.. b.Select(v => new Complex(v, 0))], complexResult);

            for (var i = 0; i < n; i++)
            {
                complexResult[i].Real.Should().BeApproximately(expected[i], 1e-9);
            }
        }

        [Theory]
        [InlineData(8)]
        [InlineData(9)]
        public void PowerSpectrumConservesEnergyAndDoesNotDoubleTheNyquistBin(int n)
        {
            var random = new Random(4);
            var signal = Enumerable.Range(0, n).Select(_ => random.NextDouble() - 0.5).ToArray();
            var data = signal.Select(v => new Complex(v, 0)).ToArray();
            FourierTransform2.FFT(data, FourierTransform.Direction.Forward);

            var power = FourierTransform2.GetPowerSpectrum(data);

            power.Sum().Should().BeApproximately(signal.Sum(v => v * v), 1e-9);
        }

        [Fact]
        public void HilbertTransformOfCosineIsSine()
        {
            var n = 16;
            var signal = Enumerable.Range(0, n).Select(i => Math.Cos(2 * Math.PI * 2 * i / n)).ToArray();
            var transformed = (double[])signal.Clone();

            HilbertTransform2.FHT(transformed, FourierTransform.Direction.Forward);

            for (var i = 0; i < n; i++)
            {
                transformed[i].Should().BeApproximately(Math.Sin(2 * Math.PI * 2 * i / n), 1e-9);
            }
        }
    }
}