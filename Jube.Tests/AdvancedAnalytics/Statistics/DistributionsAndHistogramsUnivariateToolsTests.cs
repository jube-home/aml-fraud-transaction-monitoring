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
using Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms;
using Xunit;
using Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms.Models;

namespace Jube.Test.AdvancedAnalytics.Statistics
{
    [Trait("Category", "Unit")]
    public sealed class DistributionsAndHistogramsUnivariateToolsTests
    {
        private const double StandardNormalCdfAt196 = 0.9750021048517796;
        private const double StandardNormalQuantileAt975 = 1.959963984540054;
        private const double StandardNormalPdfAtZero = 0.3989422804014327;
        private const double StandardNormalLogPdfAtZero = -0.9189385332046727;
        private const double PoissonMeanFourPmfAtTwo = 0.14652511110987343;
        private const double PoissonMeanFourCdfAtThree = 0.43347012036670884;
        private const double BinomialTenHalfPmfAtFive = 252.0 / 1024.0;
        private const double BinomialTenHalfCdfAtFive = 638.0 / 1024.0;
        private const double HypergeometricFiftyTenFifteenPmfAtThree = 0.2978556995210343;
        private const double ChiSquareThreeDfCriticalValue95 = 7.814727903251179;
        private const double StudentTFiveDfCriticalValue975 = 2.5705818366147395;
        private const double FiveTenDfCriticalValue95 = 3.325834;
        private const double EulerMascheroni = 0.5772156649015329;
        private const double Tolerance = 1e-9;
        private static readonly UnivariateDistributionTools tools = new();

        [Fact]
        [Trait("Covers", "ListDistributions")]
        public void ListDistributionsDescribesEveryKindWithParameterNamesAndFitSupport()
        {
            var list = tools.ListDistributions();

            list.Length.Should().Be(Enum.GetValues<DistributionKind>().Length);
            list.Select(d => d.Name).Should().OnlyHaveUniqueItems();

            var normal = list.Single(d => d.Name == "Normal");
            normal.IsDiscrete.Should().BeFalse();
            normal.ParameterNames.Should().Equal("mean", "standardDeviation");
            normal.ParameterDescriptions.Should().OnlyContain(d => d.Length > 10);
            normal.ExampleParameters.Should().Equal(0.0, 1.0);
            normal.CanFit.Should().BeTrue();
            normal.Description.Length.Should().BeGreaterThan(20);
            normal.Support.Should().NotBeNullOrWhiteSpace();
            normal.TypicalUses.Should().NotBeNullOrWhiteSpace();

            list.Single(d => d.Name == "Poisson").IsDiscrete.Should().BeTrue();
            list.Single(d => d.Name == "Levy").CanFit.Should().BeFalse();
            list.Single(d => d.Name == "Levy").FitMethod.Should().Contain("Not fittable");
            list.Single(d => d.Name == "Laplace").FitMethod.Should().Contain("numerical");
            list.Single(d => d.Name == "HyperbolicSecant").ParameterNames.Should().BeEmpty();
            list.Single(d => d.Name == "GeneralDiscrete").ParameterNames.Should().Equal("probabilities");

            list.All(d => !d.Description.Contains(string.Concat("Acc", "ord"), StringComparison.OrdinalIgnoreCase))
                .Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "ListDistributions")]
        public void EveryListedDistributionAcceptsItsOwnExampleParametersInDescribe()
        {
            foreach (var info in tools.ListDistributions())
            {
                var kind = Enum.Parse<DistributionKind>(info.Name);
                var parameters = info.Name == "GeneralDiscrete" ? [0.2, 0.3, 0.5] : info.ExampleParameters;

                var summary = tools.DescribeDistribution(kind, parameters);

                summary.Distribution.Should().Be(info.Name);
                summary.IsDiscrete.Should().Be(info.IsDiscrete);
            }
        }

        [Fact]
        [Trait("Covers", "DescribeDistribution")]
        public void DescribeNormalMatchesClosedFormMoments()
        {
            var summary = tools.DescribeDistribution(DistributionKind.Normal, [10.0, 2.0]);

            summary.Mean.Should().BeApproximately(10.0, Tolerance);
            summary.Variance.Should().BeApproximately(4.0, Tolerance);
            summary.StandardDeviation.Should().BeApproximately(2.0, Tolerance);
            summary.Median.Should().BeApproximately(10.0, Tolerance);
            summary.Mode.Should().BeApproximately(10.0, Tolerance);
            summary.Entropy.Should().BeApproximately(0.5 * Math.Log(2 * Math.PI * Math.E * 4.0), 1e-9);
            summary.SupportLower.Should().BeNull();
            summary.SupportUpper.Should().BeNull();
            summary.ParameterNames.Should().Equal("mean", "standardDeviation");
            summary.QuantileProbabilities.Should().Contain(0.5);
            var index = Array.IndexOf(summary.QuantileProbabilities, 0.95);
            summary.QuantileValues[index].Should().BeApproximately(10.0 + 2.0 * 1.6448536269514722, 1e-6);
        }

        [Theory]
        [InlineData(DistributionKind.Exponential, new[] { 2.0 }, 0.5, 0.25)]
        [InlineData(DistributionKind.Gamma, new[] { 2.0, 3.0 }, 6.0, 12.0)]
        [InlineData(DistributionKind.Beta, new[] { 2.0, 5.0 }, 2.0 / 7.0, 10.0 / (49.0 * 8.0))]
        [InlineData(DistributionKind.Rayleigh, new[] { 1.0 }, 1.2533141373155001, 0.42920367320510344)]
        [InlineData(DistributionKind.Pareto, new[] { 1.0, 3.0 }, 1.5, 0.75)]
        [InlineData(DistributionKind.Gumbel, new[] { 0.0, 1.0 }, EulerMascheroni, 1.6449340668482264)]
        [InlineData(DistributionKind.Laplace, new[] { 0.0, 1.0 }, 0.0, 2.0)]
        [InlineData(DistributionKind.Logistic, new[] { 0.0, 1.0 }, 0.0, 3.289868133696453)]
        [InlineData(DistributionKind.Lognormal, new[] { 0.0, 1.0 }, 1.6487212707001282, 4.670774270471604)]
        [InlineData(DistributionKind.ChiSquare, new[] { 3.0 }, 3.0, 6.0)]
        [InlineData(DistributionKind.Poisson, new[] { 4.0 }, 4.0, 4.0)]
        [InlineData(DistributionKind.Binomial, new[] { 10.0, 0.5 }, 5.0, 2.5)]
        [InlineData(DistributionKind.Geometric, new[] { 0.3 }, 7.0 / 3.0, 70.0 / 9.0)]
        [InlineData(DistributionKind.Bernoulli, new[] { 0.3 }, 0.3, 0.21)]
        [InlineData(DistributionKind.UniformDiscrete, new[] { 1.0, 6.0 }, 3.5, 35.0 / 12.0)]
        [InlineData(DistributionKind.NegativeBinomial, new[] { 5.0, 0.5 }, 5.0, 10.0)]
        [InlineData(DistributionKind.FoldedNormal, new[] { 0.0, 1.0 }, 0.7978845608028654, 0.3633802276324186)]
        [InlineData(DistributionKind.Triangular, new[] { 0.0, 1.0, 0.5 }, 0.5, 1.0 / 24.0)]
        [InlineData(DistributionKind.UniformContinuous, new[] { 0.0, 1.0 }, 0.5, 1.0 / 12.0)]
        [Trait("Covers", "DescribeDistribution")]
        public void DescribeMatchesTextbookMeanAndVariance(DistributionKind kind, double[] parameters, double mean,
            double variance)
        {
            var summary = tools.DescribeDistribution(kind, parameters);

            summary.Mean.Should().BeApproximately(mean, 1e-8);
            summary.Variance.Should().BeApproximately(variance, 1e-8);
        }

        [Fact]
        [Trait("Covers", "DescribeDistribution")]
        public void DescribeUniformDiscreteEntropyIsLogOfTheNumberOfOutcomes()
        {
            tools.DescribeDistribution(DistributionKind.UniformDiscrete, [1.0, 6.0]).Entropy
                .Should().BeApproximately(Math.Log(6.0), Tolerance);
        }

        [Fact]
        [Trait("Covers", "DescribeDistribution")]
        public void DescribeReportsNullForMomentsThatDoNotExist()
        {
            var summary = tools.DescribeDistribution(DistributionKind.Cauchy, [0.0, 1.0]);

            summary.Mean.Should().BeNull();
            summary.Variance.Should().BeNull();
            summary.StandardDeviation.Should().BeNull();
            summary.Median.Should().BeApproximately(0.0, Tolerance);
        }

        [Fact]
        [Trait("Covers", "DescribeDistribution")]
        public void DescribeRejectsWrongParameterCountAndDomains()
        {
            Action wrongCount = () => tools.DescribeDistribution(DistributionKind.Normal, [1.0]);
            Action negativeScale = () => tools.DescribeDistribution(DistributionKind.Normal, [0.0, -1.0]);
            Action nan = () => tools.DescribeDistribution(DistributionKind.Normal, [double.NaN, 1.0]);
            Action fractionalTrials = () => tools.DescribeDistribution(DistributionKind.Binomial, [2.5, 0.5]);
            Action badProbability = () => tools.DescribeDistribution(DistributionKind.Bernoulli, [1.5]);
            Action missing = () => tools.DescribeDistribution(DistributionKind.Normal, null!);
            Action badSum = () => tools.DescribeDistribution(DistributionKind.GeneralDiscrete, [0.5, 0.6]);
            Action ordering = () => tools.DescribeDistribution(DistributionKind.Triangular, [1.0, 0.0, 0.5]);

            wrongCount.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("parameters");
            wrongCount.Should().Throw<ArgumentException>().WithMessage("*mean, standardDeviation*");
            negativeScale.Should().Throw<ArgumentException>().WithMessage("*greater than 0*");
            nan.Should().Throw<ArgumentException>().WithMessage("*finite*");
            fractionalTrials.Should().Throw<ArgumentException>().WithMessage("*whole number*");
            badProbability.Should().Throw<ArgumentException>().WithMessage("*between 0 and 1*");
            missing.Should().Throw<ArgumentException>();
            badSum.Should().Throw<ArgumentException>().WithMessage("*sum to 1*");
            ordering.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("parameters");
        }

        [Fact]
        [Trait("Covers", "DistributionDensity")]
        public void DensityOfStandardNormalAtZeroIsOneOverRootTwoPi()
        {
            var result = tools.DistributionDensity(DistributionKind.Normal, [0.0, 1.0],
                [0.0, 1.0, -1.0]);

            result.Quantity.Should().Be("ProbabilityDensity");
            result.Values[0].Should().BeApproximately(StandardNormalPdfAtZero, Tolerance);
            result.Values[1].Should().BeApproximately(0.24197072451914337, Tolerance);
            result.Values[2].Should().Be(result.Values[1]);
        }

        [Theory]
        [InlineData(DistributionKind.Exponential, new[] { 2.0 }, 1.0, 0.2706705664732254)]
        [InlineData(DistributionKind.Beta, new[] { 2.0, 5.0 }, 0.2, 2.4576)]
        [InlineData(DistributionKind.Cauchy, new[] { 0.0, 1.0 }, 0.0, 0.3183098861837907)]
        [InlineData(DistributionKind.Laplace, new[] { 0.0, 1.0 }, 0.0, 0.5)]
        [InlineData(DistributionKind.Gamma, new[] { 2.0, 3.0 }, 3.0, 0.12551071508349176)]
        [InlineData(DistributionKind.ChiSquare, new[] { 3.0 }, 3.0, 0.15418032980376928)]
        [InlineData(DistributionKind.Logistic, new[] { 0.0, 1.0 }, 0.0, 0.25)]
        [InlineData(DistributionKind.UniformContinuous, new[] { 2.0, 6.0 }, 3.0, 0.25)]
        [InlineData(DistributionKind.BirnbaumSaunders, new[] { 0.0, 1.0, 1.0 }, 1.0, 0.3989422804014327)]
        [InlineData(DistributionKind.BirnbaumSaunders, new[] { 0.0, 2.0, 1.0 }, 2.0, 0.19947114020071635)]
        [Trait("Covers", "DistributionDensity")]
        public void DensityMatchesClosedForms(DistributionKind kind, double[] parameters, double x, double expected)
        {
            tools.DistributionDensity(kind, parameters, [x]).Values[0].Should().BeApproximately(expected, 1e-9);
        }

        [Fact]
        [Trait("Covers", "DistributionDensity")]
        public void DiscreteDensityIsExactProbabilityMassAndZeroBetweenIntegers()
        {
            var poisson = tools.DistributionDensity(DistributionKind.Poisson, [4.0], [2.0, 2.5, -1.0]);
            poisson.Quantity.Should().Be("ProbabilityMass");
            poisson.Values[0].Should().BeApproximately(PoissonMeanFourPmfAtTwo, Tolerance);
            poisson.Values[1].Should().Be(0.0);
            poisson.Values[2].Should().Be(0.0);

            tools.DistributionDensity(DistributionKind.Binomial, [10.0, 0.5], [5.0]).Values[0]
                .Should().BeApproximately(BinomialTenHalfPmfAtFive, Tolerance);

            tools.DistributionDensity(DistributionKind.Hypergeometric, [50.0, 10.0, 15.0], [3.0])
                .Values[0].Should().BeApproximately(HypergeometricFiftyTenFifteenPmfAtThree, Tolerance);

            tools.DistributionDensity(DistributionKind.Geometric, [0.3], [2.0]).Values[0]
                .Should().BeApproximately(0.147, Tolerance);

            tools.DistributionDensity(DistributionKind.NegativeBinomial, [5.0, 0.5], [3.0]).Values[0]
                .Should().BeApproximately(0.13671875, Tolerance);

            tools.DistributionDensity(DistributionKind.UniformDiscrete, [1.0, 6.0], [4.0]).Values[0]
                .Should().BeApproximately(1.0 / 6.0, Tolerance);

            tools.DistributionDensity(DistributionKind.Rademacher, [], [1.0, 0.0]).Values
                .Should().Equal(0.5, 0.0);

            tools.DistributionDensity(DistributionKind.GeneralDiscrete, [0.2, 0.3, 0.5], [2.0])
                .Values[0].Should().BeApproximately(0.5, Tolerance);
        }

        [Fact]
        [Trait("Covers", "DistributionDensity")]
        public void DensityValidatesInputs()
        {
            Action nanPoint = () => tools.DistributionDensity(DistributionKind.Normal, [0.0, 1.0],
                [double.NaN]);

            Action nullPoints = () => tools.DistributionDensity(DistributionKind.Normal, [0.0, 1.0], null!);

            nanPoint.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("x");
            nullPoints.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("x");

            tools.DistributionDensity(DistributionKind.Normal, [0.0, 1.0], []).Values
                .Should().BeEmpty();
        }

        [Fact]
        [Trait("Covers", "DistributionLogDensity")]
        public void LogDensityIsNaturalLogAndSurvivesTheFarTail()
        {
            var result = tools.DistributionLogDensity(DistributionKind.Normal, [0.0, 1.0],
                [0.0, 40.0]);

            result.Quantity.Should().Be("LogDensity");
            result.Values[0].Should().BeApproximately(StandardNormalLogPdfAtZero, Tolerance);
            result.Values[1].Should().BeApproximately(-800.0 + StandardNormalLogPdfAtZero, 1e-6);

            tools.DistributionLogDensity(DistributionKind.Poisson, [4.0], [2.0, 2.5]).Values[0]
                .Should().BeApproximately(Math.Log(PoissonMeanFourPmfAtTwo), Tolerance);

            tools.DistributionLogDensity(DistributionKind.Poisson, [4.0], [2.5]).Values[0]
                .Should().BeNull();
        }

        [Fact]
        [Trait("Covers", "DistributionCumulativeProbability")]
        public void CumulativeProbabilityMatchesTablesAndLimits()
        {
            var normal = tools.DistributionCumulativeProbability(DistributionKind.Normal, [0.0, 1.0],
                [1.96, double.NegativeInfinity, double.PositiveInfinity, 0.0]);

            normal.Values[0].Should().BeApproximately(StandardNormalCdfAt196, Tolerance);
            normal.Values[1].Should().BeApproximately(0.0, Tolerance);
            normal.Values[2].Should().BeApproximately(1.0, Tolerance);
            normal.Values[3].Should().BeApproximately(0.5, Tolerance);

            tools.DistributionCumulativeProbability(DistributionKind.Exponential, [2.0], [1.0])
                .Values[0].Should().BeApproximately(1 - Math.Exp(-2), Tolerance);

            tools.DistributionCumulativeProbability(DistributionKind.Weibull, [1.5, 1.0], [1.0])
                .Values[0].Should().BeApproximately(1 - Math.Exp(-1), Tolerance);

            tools.DistributionCumulativeProbability(DistributionKind.Cauchy, [0.0, 1.0], [1.0])
                .Values[0].Should().BeApproximately(0.75, Tolerance);

            tools.DistributionCumulativeProbability(DistributionKind.Gumbel, [0.0, 1.0], [0.0])
                .Values[0].Should().BeApproximately(Math.Exp(-1), Tolerance);

            tools.DistributionCumulativeProbability(DistributionKind.Rayleigh, [1.0], [1.0])
                .Values[0].Should().BeApproximately(1 - Math.Exp(-0.5), Tolerance);

            tools.DistributionCumulativeProbability(DistributionKind.Pareto, [1.0, 3.0], [2.0])
                .Values[0].Should().BeApproximately(0.875, Tolerance);

            tools.DistributionCumulativeProbability(DistributionKind.Triangular, [0.0, 1.0, 0.5],
                [0.25]).Values[0].Should().BeApproximately(0.125, Tolerance);

            tools.DistributionCumulativeProbability(DistributionKind.Logistic, [0.0, 1.0], [1.0])
                .Values[0].Should().BeApproximately(0.7310585786300049, Tolerance);
            tools.DistributionCumulativeProbability(DistributionKind.Lognormal, [0.0, 1.0], [1.0])
                .Values[0].Should().BeApproximately(0.5, Tolerance);

            tools.DistributionCumulativeProbability(DistributionKind.BirnbaumSaunders, [0.0, 2.0, 1.0],
                [2.0]).Values[0].Should().BeApproximately(0.5, Tolerance);
        }

        [Fact]
        [Trait("Covers", "DistributionCumulativeProbability")]
        public void DiscreteCumulativeProbabilityRoundsDownToWholeNumbers()
        {
            var poisson = tools.DistributionCumulativeProbability(DistributionKind.Poisson, [4.0],
                [3.0, 3.9, -0.5, 1000.0]);

            poisson.Values[0].Should().BeApproximately(PoissonMeanFourCdfAtThree, Tolerance);
            poisson.Values[1].Should().BeApproximately(PoissonMeanFourCdfAtThree, Tolerance);
            poisson.Values[2].Should().BeApproximately(0.0, Tolerance);
            poisson.Values[3].Should().BeApproximately(1.0, 1e-9);

            tools.DistributionCumulativeProbability(DistributionKind.Binomial, [10.0, 0.5], [5.0])
                .Values[0].Should().BeApproximately(BinomialTenHalfCdfAtFive, Tolerance);
        }

        [Fact]
        [Trait("Covers", "DistributionUpperTailProbability")]
        public void UpperTailProbabilityIsAccurateInTheFarTail()
        {
            var result = tools.DistributionUpperTailProbability(DistributionKind.Normal, [0.0, 1.0],
                [1.96, 10.0]);

            result.Values[0].Should().BeApproximately(1 - StandardNormalCdfAt196, Tolerance);
            result.Values[1].Should().BeApproximately(7.619853024160527e-24, 1e-30);

            tools.DistributionUpperTailProbability(DistributionKind.Exponential, [2.0], [1.5])
                .Values[0].Should().BeApproximately(Math.Exp(-3), Tolerance);

            tools.DistributionUpperTailProbability(DistributionKind.Poisson, [4.0], [3.0]).Values[0]
                .Should().BeApproximately(1 - PoissonMeanFourCdfAtThree, Tolerance);
        }

        [Fact]
        [Trait("Covers", "DistributionQuantile")]
        public void QuantileMatchesPublishedCriticalValues()
        {
            tools.DistributionQuantile(DistributionKind.Normal, [0.0, 1.0], [0.975]).Values[0]
                .Should().BeApproximately(StandardNormalQuantileAt975, 1e-6);

            tools.DistributionQuantile(DistributionKind.ChiSquare, [3.0], [0.95]).Values[0]
                .Should().BeApproximately(ChiSquareThreeDfCriticalValue95, 1e-4);
            tools.DistributionQuantile(DistributionKind.StudentT, [5.0], [0.975]).Values[0]
                .Should().BeApproximately(StudentTFiveDfCriticalValue975, 1e-4);

            tools.DistributionQuantile(DistributionKind.F, [5.0, 10.0], [0.95]).Values[0]
                .Should().BeApproximately(FiveTenDfCriticalValue95, 1e-4);

            tools.DistributionQuantile(DistributionKind.Exponential, [2.0], [0.5]).Values[0]
                .Should().BeApproximately(Math.Log(2) / 2, 1e-9);

            tools.DistributionQuantile(DistributionKind.Weibull, [1.5, 1.0], [1 - Math.Exp(-1)])
                .Values[0].Should().BeApproximately(1.0, 1e-6);
        }

        [Fact]
        [Trait("Covers", "DistributionQuantile")]
        public void QuantileOfDiscreteIsSmallestWholeNumberReachingTheProbability()
        {
            var result = tools.DistributionQuantile(DistributionKind.Poisson, [4.0],
                [0.43, 0.44, 0.5]);

            result.Values[0].Should().Be(3.0);
            result.Values[1].Should().Be(4.0);

            tools.DistributionQuantile(DistributionKind.Degenerate, [7.0], [0.1, 0.5, 0.9]).Values
                .Should().Equal(7.0, 7.0, 7.0);
        }

        [Fact]
        [Trait("Covers", "DistributionQuantile")]
        public void QuantileRejectsProbabilitiesOutsideZeroOne()
        {
            Action tooHigh = () => tools.DistributionQuantile(DistributionKind.Normal, [0.0, 1.0],
                [1.5]);

            Action empty = () => tools.DistributionQuantile(DistributionKind.Normal, [0.0, 1.0],
                []);

            tooHigh.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("probabilities");
            empty.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("probabilities");

            tools.DistributionQuantile(DistributionKind.Normal, [0.0, 1.0], [0.0, 1.0]).Values
                .Should().OnlyContain(v => v == null);
        }

        [Fact]
        [Trait("Covers", "DistributionHazardRate")]
        public void HazardRateIsConstantForExponentialAndRisingForWeibullAboveOne()
        {
            tools.DistributionHazardRate(DistributionKind.Exponential, [2.0], [0.1, 1.0, 5.0])
                .Values.Should().OnlyContain(v => Math.Abs(v!.Value - 2.0) < 1e-9);

            var weibull = tools.DistributionHazardRate(DistributionKind.Weibull, [2.0, 1.0],
                [0.5, 1.0, 2.0]);
            weibull.Values[0].Should().BeApproximately(2.0 * 0.5, 1e-9);
            weibull.Values[1].Should().BeApproximately(2.0, 1e-9);
            weibull.Values[2].Should().BeApproximately(4.0, 1e-9);

            tools.DistributionHazardRate(DistributionKind.Normal, [0.0, 1.0], [0.0]).Values[0]
                .Should().BeApproximately(StandardNormalPdfAtZero / 0.5, 1e-9);
        }

        [Fact]
        [Trait("Covers", "DistributionCumulativeHazard")]
        public void CumulativeHazardIsMinusLogOfTheUpperTail()
        {
            tools.DistributionCumulativeHazard(DistributionKind.Exponential, [2.0], [1.5])
                .Values[0].Should().BeApproximately(3.0, 1e-9);

            tools.DistributionCumulativeHazard(DistributionKind.Weibull, [2.0, 1.0], [2.0])
                .Values[0].Should().BeApproximately(4.0, 1e-9);

            tools.DistributionCumulativeHazard(DistributionKind.Normal, [0.0, 1.0], [1.0])
                .Values[0].Should().BeApproximately(-Math.Log(1 - 0.8413447460685429), 1e-9);
        }

        [Fact]
        [Trait("Covers", "DistributionIntervalProbability")]
        public void IntervalProbabilityIsDifferenceOfCumulativeProbabilities()
        {
            var normal = tools.DistributionIntervalProbability(DistributionKind.Normal, [0.0, 1.0],
                [-1.96, double.NegativeInfinity], [1.96, 0.0]);

            normal.Values[0].Should().BeApproximately(2 * StandardNormalCdfAt196 - 1, Tolerance);
            normal.Values[1].Should().BeApproximately(0.5, Tolerance);

            var poisson = tools.DistributionIntervalProbability(DistributionKind.Poisson, [4.0],
                [2.0, 0.0], [3.0, 3.0]);

            poisson.Values[0].Should().BeApproximately(PoissonMeanFourCdfAtThree - Math.Exp(-4) * (1 + 4), Tolerance);
            poisson.Values[1].Should().BeApproximately(PoissonMeanFourCdfAtThree, Tolerance);
        }

        [Fact]
        [Trait("Covers", "DistributionIntervalProbability")]
        public void IntervalProbabilityRejectsMismatchedOrReversedEnds()
        {
            Action mismatched = () => tools.DistributionIntervalProbability(DistributionKind.Normal,
                [0.0, 1.0], [0.0], [1.0, 2.0]);

            Action reversed = () => tools.DistributionIntervalProbability(DistributionKind.Normal,
                [0.0, 1.0], [2.0], [1.0]);

            mismatched.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("upper");
            reversed.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("lower");
        }

        [Fact]
        [Trait("Covers", "DistributionCurve")]
        public void CurveSpansTheRequestedQuantilesAndCumulativeIsMonotone()
        {
            var curve = tools.DistributionCurve(DistributionKind.Normal, [0.0, 1.0], 11);

            curve.X.Length.Should().Be(11);
            curve.X[0].Should().BeApproximately(-3.090232306167813, 1e-6);
            curve.X[10].Should().BeApproximately(3.090232306167813, 1e-6);
            curve.X[5].Should().BeApproximately(0.0, 1e-9);
            curve.Density[5].Should().BeApproximately(StandardNormalPdfAtZero, Tolerance);
            curve.Cumulative[5].Should().BeApproximately(0.5, Tolerance);
            curve.Survival[5].Should().BeApproximately(0.5, Tolerance);
            curve.Cumulative.Select(v => v!.Value).Should().BeInAscendingOrder();
            curve.Cumulative[0].Should().BeApproximately(0.001, 1e-6);
        }

        [Fact]
        [Trait("Covers", "DistributionCurve")]
        public void CurveValidatesItsArguments()
        {
            Action tooFew = () => tools.DistributionCurve(DistributionKind.Normal, [0.0, 1.0], 1);
            Action badOrder = () => tools.DistributionCurve(DistributionKind.Normal, [0.0, 1.0], 10, 0.9, 0.1);
            Action badProbability = () => tools.DistributionCurve(DistributionKind.Normal, [0.0, 1.0], 10, 0.0);

            tooFew.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("pointCount");
            badOrder.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("lowerProbability");
            badProbability.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("lowerProbability");
        }

        [Fact]
        [Trait("Covers", "DistributionProbabilityTable")]
        public void ProbabilityTableListsExactMassesAndTotals()
        {
            var table = tools.DistributionProbabilityTable(DistributionKind.Binomial, [10.0, 0.5], 0, 10);

            table.Values.Should().Equal(Enumerable.Range(0, 11));
            table.Probability[5].Should().BeApproximately(BinomialTenHalfPmfAtFive, Tolerance);
            table.CumulativeProbability[5].Should().BeApproximately(BinomialTenHalfCdfAtFive, Tolerance);
            table.TotalProbability.Should().BeApproximately(1.0, 1e-12);

            var partial = tools.DistributionProbabilityTable(DistributionKind.Poisson, [4.0], 0, 3);
            partial.TotalProbability.Should().BeApproximately(PoissonMeanFourCdfAtThree, Tolerance);
        }

        [Fact]
        [Trait("Covers", "DistributionProbabilityTable")]
        public void ProbabilityTableRejectsContinuousDistributionsAndBadRanges()
        {
            Action continuous = () => tools.DistributionProbabilityTable(DistributionKind.Normal, [0.0, 1.0], 0,
                5);

            Action reversed = () => tools.DistributionProbabilityTable(DistributionKind.Poisson, [4.0], 5, 1);
            Action huge = () => tools.DistributionProbabilityTable(DistributionKind.Poisson, [4.0], 0, 20000);

            continuous.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("distribution");
            reversed.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("last");
            huge.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("last");
        }

        [Fact]
        [Trait("Covers", "DistributionRandomSample")]
        public void RandomSampleIsReproducibleAndHasTheRightMoments()
        {
            var first = tools.DistributionRandomSample(DistributionKind.Normal, [5.0, 2.0], 20000, 42);
            var second = tools.DistributionRandomSample(DistributionKind.Normal, [5.0, 2.0], 20000, 42);
            var other = tools.DistributionRandomSample(DistributionKind.Normal, [5.0, 2.0], 20000, 43);

            first.Values.Should().Equal(second.Values);
            first.Values.Should().NotEqual(other.Values);
            first.Seed.Should().Be(42);
            first.SampleMean.Should().BeApproximately(5.0, 0.06);
            first.SampleStandardDeviation.Should().BeApproximately(2.0, 0.06);
            first.Values.Length.Should().Be(20000);
        }

        [Fact]
        [Trait("Covers", "DistributionRandomSample")]
        public void RandomSampleOfDiscreteDistributionsGivesWholeNumbersInSupport()
        {
            var poisson = tools.DistributionRandomSample(DistributionKind.Poisson, [4.0], 20000, 7);
            poisson.Values.Should().OnlyContain(v => v >= 0 && Math.Abs(v - Math.Round(v)) < 1e-12);
            poisson.SampleMean.Should().BeApproximately(4.0, 0.08);

            var uniform = tools.DistributionRandomSample(DistributionKind.UniformDiscrete, [1.0, 6.0], 5000, 3);
            uniform.Values.Should().OnlyContain(v => v >= 1 && v <= 6);
            uniform.Values.Distinct().Count().Should().Be(6);

            var exponential = tools.DistributionRandomSample(DistributionKind.Exponential, [2.0], 20000, 5);
            exponential.SampleMean.Should().BeApproximately(0.5, 0.02);
        }

        [Fact]
        [Trait("Covers", "DistributionRandomSample")]
        public void RandomSampleValidatesCountAndSupport()
        {
            Action zero = () => tools.DistributionRandomSample(DistributionKind.Normal, [0.0, 1.0], 0);
            Action huge = () => tools.DistributionRandomSample(DistributionKind.Normal, [0.0, 1.0], 1000000);
            Action unsupported = () => tools.DistributionRandomSample(DistributionKind.WrappedCauchy,
                [0.0, 1.0], 10);

            zero.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("count");
            huge.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("count");
            unsupported.Should().Throw<ArgumentException>().WithMessage("*not available*");

            tools.DistributionRandomSample(DistributionKind.Normal, [0.0, 1.0], 1).SampleStandardDeviation
                .Should().BeNull();
        }

        [Fact]
        [Trait("Covers", "DistributionLogLikelihood")]
        public void LogLikelihoodSumsLogDensitiesAndComputesAicAndBic()
        {
            var values = new[] { 0.0, 1.0, -1.0 };

            var result = tools.DistributionLogLikelihood(DistributionKind.Normal, [0.0, 1.0], values);

            var expected = 3 * StandardNormalLogPdfAtZero - 0.5 * 2.0;
            result.LogLikelihood.Should().BeApproximately(expected, 1e-9);
            result.MeanLogLikelihood.Should().BeApproximately(expected / 3, 1e-9);
            result.ParameterCount.Should().Be(2);
            result.Aic.Should().BeApproximately(4 - 2 * expected, 1e-9);
            result.Bic.Should().BeApproximately(2 * Math.Log(3) - 2 * expected, 1e-9);
            result.SampleSize.Should().Be(3);
            result.ZeroLikelihoodCount.Should().Be(0);
        }

        [Fact]
        [Trait("Covers", "DistributionLogLikelihood")]
        public void LogLikelihoodIsNullWhenAnObservationIsImpossible()
        {
            var result = tools.DistributionLogLikelihood(DistributionKind.Exponential, [1.0],
                [1.0, -1.0]);

            result.LogLikelihood.Should().BeNull();
            result.Aic.Should().BeNull();
            result.ZeroLikelihoodCount.Should().Be(1);

            Action nan = () => tools.DistributionLogLikelihood(DistributionKind.Exponential, [1.0],
                [double.NaN]);

            nan.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("values");
        }

        [Fact]
        [Trait("Covers", "DistributionTailScores")]
        public void TailScoresGiveLowerUpperTwoSidedAndSurprise()
        {
            var result = tools.DistributionTailScores(DistributionKind.Normal, [0.0, 1.0],
                [1.96, 0.0, -1.96]);

            result.LowerTail[0].Should().BeApproximately(StandardNormalCdfAt196, Tolerance);
            result.UpperTail[0].Should().BeApproximately(1 - StandardNormalCdfAt196, Tolerance);
            result.TwoSided[0].Should().BeApproximately(2 * (1 - StandardNormalCdfAt196), Tolerance);
            result.Surprise[0].Should().BeApproximately(-Math.Log10(2 * (1 - StandardNormalCdfAt196)), 1e-9);
            result.TwoSided[1].Should().BeApproximately(1.0, Tolerance);
            result.Surprise[1].Should().BeApproximately(0.0, 1e-9);
            result.TwoSided[2].Should().BeApproximately(result.TwoSided[0]!.Value, Tolerance);
        }

        [Fact]
        [Trait("Covers", "DistributionTailScores")]
        public void TailScoresOfDiscreteValuesIncludeTheValueInBothTails()
        {
            var result = tools.DistributionTailScores(DistributionKind.Poisson, [4.0], [3.0]);

            result.LowerTail[0].Should().BeApproximately(PoissonMeanFourCdfAtThree, Tolerance);

            result.UpperTail[0].Should().BeApproximately(1 - (PoissonMeanFourCdfAtThree - Math.Exp(-4) * 64.0 / 6.0),
                Tolerance);
        }
    }
}