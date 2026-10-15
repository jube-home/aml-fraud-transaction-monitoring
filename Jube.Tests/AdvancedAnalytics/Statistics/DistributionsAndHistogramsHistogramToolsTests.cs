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
    public sealed class DistributionsAndHistogramsHistogramToolsTests
    {
        private static readonly HistogramAndDensityTools tools = new();
        private static readonly UnivariateDistributionTools univariate = new();

        private static double[] OneToN(int n)
        {
            return [.. Enumerable.Range(1, n).Select(i => (double)i)];
        }

        private static double[] QuantileGrid(DistributionKind kind, double[] parameters, int n)
        {
            var probabilities = Enumerable.Range(0, n).Select(i => (i + 0.5) / n).ToArray();

            return [.. univariate.DistributionQuantile(kind, parameters, probabilities).Values.Select(v => v!.Value)];
        }

        [Fact]
        [Trait("Covers", "HistogramFixedBinCount")]
        public void FixedBinCountOfOneToTenInFiveBinsHasTwoPerBin()
        {
            var h = tools.HistogramFixedBinCount(OneToN(10), 5);

            h.BinCount.Should().Be(5);
            h.Edges.Should().HaveCount(6);

            new[] { 1.0, 2.8, 4.6, 6.4, 8.2, 10.0 }.Zip(h.Edges, (e, a) => Math.Abs(e - a)).Should()
                .OnlyContain(d => d < 1e-12);

            h.Counts.Should().Equal(2, 2, 2, 2, 2);
            h.Centers[0].Should().BeApproximately(1.9, 1e-12);
            h.Widths.Should().OnlyContain(w => Math.Abs(w - 1.8) < 1e-12);
            h.RelativeFrequencies.Should().OnlyContain(f => Math.Abs(f - 0.2) < 1e-12);
            h.Densities.Should().OnlyContain(d => Math.Abs(d - 2.0 / (10 * 1.8)) < 1e-12);
            h.Values.Should().Equal(2.0, 2.0, 2.0, 2.0, 2.0);
            h.Scale.Should().Be("Count");
            h.Minimum.Should().Be(1.0);
            h.Maximum.Should().Be(10.0);
            h.OutsideCount.Should().Be(0);
        }

        [Fact]
        [Trait("Covers", "HistogramFixedBinCount")]
        public void FixedBinCountLastBinIncludesTheMaximumAndScalesAndAccumulates()
        {
            var counts = tools.HistogramFixedBinCount(OneToN(10), 9);
            counts.Counts.Should().Equal(1, 1, 1, 1, 1, 1, 1, 1, 2);

            var percent = tools.HistogramFixedBinCount(OneToN(10), 5, HistogramScale.Percent);
            percent.Values.Should().OnlyContain(v => Math.Abs(v - 20.0) < 1e-12);

            var density = tools.HistogramFixedBinCount(OneToN(10), 5, HistogramScale.Density);
            density.Values[0].Should().BeApproximately(2.0 / 18.0, 1e-12);

            var cumulative = tools.HistogramFixedBinCount(OneToN(10), 5, HistogramScale.Count, true);
            cumulative.Values.Should().Equal(2.0, 4.0, 6.0, 8.0, 10.0);
            cumulative.Cumulative.Should().BeTrue();

            var cumulativeDensity = tools.HistogramFixedBinCount(OneToN(10), 5, HistogramScale.Density, true);
            cumulativeDensity.Values[4].Should().BeApproximately(1.0, 1e-12);

            var relative = tools.HistogramFixedBinCount(OneToN(10), 5, HistogramScale.RelativeFrequency, true);
            relative.Values[4].Should().BeApproximately(1.0, 1e-12);
        }

        [Fact]
        [Trait("Covers", "HistogramFixedBinCount")]
        public void FixedBinCountHandlesConstantSingleAndInvalidInput()
        {
            var constant = tools.HistogramFixedBinCount([5.0, 5.0, 5.0], 4);
            constant.BinCount.Should().Be(1);
            constant.Edges.Should().Equal(4.5, 5.5);
            constant.Counts.Should().Equal(3);
            constant.Notes.Should().ContainSingle(n => n.Contains("identical"));

            tools.HistogramFixedBinCount([7.0], 3).Counts.Should().Equal(1);

            Action nan = () => tools.HistogramFixedBinCount([1.0, double.NaN], 3);
            Action empty = () => tools.HistogramFixedBinCount([], 3);
            Action zeroBins = () => tools.HistogramFixedBinCount(OneToN(5), 0);
            Action huge = () => tools.HistogramFixedBinCount(OneToN(5), 1000000);

            nan.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("values");
            empty.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("values");
            zeroBins.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("binCount");
            huge.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("binCount");
        }

        [Fact]
        [Trait("Covers", "HistogramFixedBinCount")]
        public void FixedBinCountCopesWithHugeMagnitudes()
        {
            var values = new[] { 1e15, 2e15, 3e15, 4e15 };

            var h = tools.HistogramFixedBinCount(values, 3);

            h.Counts.Sum().Should().Be(4);
            h.Edges[0].Should().Be(1e15);
            h.Edges[3].Should().BeApproximately(4e15, 1.0);
        }

        [Fact]
        [Trait("Covers", "HistogramFixedBinWidth")]
        public void FixedBinWidthStartsAtTheMinimumAndCoversTheMaximum()
        {
            var h = tools.HistogramFixedBinWidth([0.0, 1.0, 2.0, 3.0, 4.0, 5.0], 2.0);

            h.Edges.Should().Equal(0.0, 2.0, 4.0, 6.0);
            h.Counts.Should().Equal(2, 2, 2);
            h.BinningMethod.Should().Contain("FixedBinWidth");

            tools.HistogramFixedBinWidth([0.0, 1.0, 2.0, 3.0, 4.0, 5.0], 5.0).Counts.Should().Equal(6);

            tools.HistogramFixedBinWidth([0.0, 1.0, 2.0, 3.0, 4.0, 5.0], 2.5, HistogramScale.Density)
                .Values.Should().OnlyContain(v => Math.Abs(v - 3.0 / (6 * 2.5)) < 1e-12);
        }

        [Fact]
        [Trait("Covers", "HistogramFixedBinWidth")]
        public void FixedBinWidthRejectsNonPositiveWidthsAndAbsurdBinCounts()
        {
            Action zero = () => tools.HistogramFixedBinWidth(OneToN(5), 0.0);
            Action nan = () => tools.HistogramFixedBinWidth(OneToN(5), double.NaN);
            Action tiny = () => tools.HistogramFixedBinWidth(OneToN(5), 1e-12);

            zero.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("binWidth");
            nan.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("binWidth");
            tiny.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("binWidth");
            tools.HistogramFixedBinWidth([3.0, 3.0], 2.0).Edges.Should().Equal(2.0, 4.0);
        }

        [Theory]
        [InlineData(BinningRule.Sturges, 8)]
        [InlineData(BinningRule.Rice, 10)]
        [InlineData(BinningRule.SquareRoot, 10)]
        [InlineData(BinningRule.Scott, 5)]
        [InlineData(BinningRule.FreedmanDiaconis, 5)]
        [InlineData(BinningRule.Doane, 8)]
        [Trait("Covers", "HistogramByBinningRule")]
        public void BinningRulesGiveTheTextbookBinCountForOneToOneHundred(BinningRule rule, int expectedBins)
        {
            var h = tools.HistogramByBinningRule(OneToN(100), rule);

            h.BinCount.Should().Be(expectedBins);
            h.Counts.Sum().Should().Be(100);
            h.BinningMethod.Should().StartWith(rule.ToString());
        }

        [Fact]
        [Trait("Covers", "HistogramByBinningRule")]
        public void BinningRuleFallsBackToSturgesWhenTheSpreadMeasureIsZero()
        {
            var values = new[] { 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 10.0 };

            var h = tools.HistogramByBinningRule(values, BinningRule.FreedmanDiaconis);

            h.Notes.Should().Contain(n => n.Contains("fell back to Sturges"));
            h.BinCount.Should().Be((int)Math.Ceiling(Math.Log2(9) + 1));
            h.Counts.Sum().Should().Be(9);

            tools.HistogramByBinningRule([4.0, 4.0], BinningRule.Scott).BinCount.Should().Be(1);
            tools.HistogramByBinningRule([4.0], BinningRule.Sturges).BinCount.Should().Be(1);
        }

        [Fact]
        [Trait("Covers", "HistogramCustomEdges")]
        public void CustomEdgesCountIntoUnequalBinsAndReportOutsideValues()
        {
            var values = new[] { 1.0, 2.0, 3.0, 4.0, 5.0, 10.0, 20.0, 100.0, 500.0, -3.0 };

            var h = tools.HistogramCustomEdges(values, [0.0, 5.0, 10.0, 100.0]);

            h.Counts.Should().Equal(4, 1, 3);
            h.OutsideCount.Should().Be(2);
            h.Widths.Should().Equal(5.0, 5.0, 90.0);
            h.Densities[2].Should().BeApproximately(3.0 / (8 * 90.0), 1e-12);
            h.RelativeFrequencies[0].Should().BeApproximately(0.4, 1e-12);
            h.Notes.Should().ContainSingle(n => n.Contains("outside"));
            h.SampleSize.Should().Be(10);

            tools.HistogramCustomEdges(values, [0.0, 5.0, 10.0, 100.0], HistogramScale.Count, true)
                .Values.Should().Equal(4.0, 5.0, 8.0);
        }

        [Fact]
        [Trait("Covers", "HistogramCustomEdges")]
        public void CustomEdgesMustBeStrictlyIncreasingAndAtLeastTwo()
        {
            Action single = () => tools.HistogramCustomEdges(OneToN(3), [1.0]);
            Action equal = () => tools.HistogramCustomEdges(OneToN(3), [1.0, 1.0, 2.0]);
            Action decreasing = () => tools.HistogramCustomEdges(OneToN(3), [3.0, 2.0]);
            Action infinite = () => tools.HistogramCustomEdges(OneToN(3), [1.0, double.PositiveInfinity]);

            single.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("edges");
            equal.Should().Throw<ArgumentException>().WithMessage("*strictly increasing*");
            decreasing.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("edges");
            infinite.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("edges");
        }

        [Fact]
        [Trait("Covers", "SuggestHistogramBinCounts")]
        public void SuggestionsListEveryRuleWithItsBinWidth()
        {
            var result = tools.SuggestHistogramBinCounts(OneToN(100));

            var byRule = result.Suggestions.ToDictionary(s => s.Rule, s => s);
            byRule["Sturges"].BinCount.Should().Be(8);
            byRule["Rice"].BinCount.Should().Be(10);
            byRule["SquareRoot"].BinCount.Should().Be(10);
            byRule["Scott"].BinCount.Should().Be(5);
            byRule["FreedmanDiaconis"].BinCount.Should().Be(5);
            byRule["Doane"].BinCount.Should().Be(8);
            byRule["Sturges"].BinWidth.Should().BeApproximately(99.0 / 8, 1e-12);
            result.Minimum.Should().Be(1.0);
            result.Maximum.Should().Be(100.0);
            result.SampleSize.Should().Be(100);

            tools.SuggestHistogramBinCounts([3.0]).Suggestions.Should().OnlyContain(s => s.BinCount == 1);
        }

        [Fact]
        [Trait("Covers", "Histogram2D")]
        public void Histogram2DCountsPairsIntoAGridWithMarginals()
        {
            var x = new[] { 1.0, 2.0, 3.0, 4.0 };
            var y = new[] { 1.0, 2.0, 3.0, 4.0 };

            var h = tools.Histogram2D(x, y, 2, 2);

            h.XEdges.Should().Equal(1.0, 2.5, 4.0);
            h.Counts[0].Should().Equal(2, 0);
            h.Counts[1].Should().Equal(0, 2);
            h.RelativeFrequencies[0][0].Should().BeApproximately(0.5, 1e-12);
            h.XMarginalCounts.Should().Equal(2, 2);
            h.YMarginalCounts.Should().Equal(2, 2);
            h.OutsideCount.Should().Be(0);
            h.SampleSize.Should().Be(4);
        }

        [Fact]
        [Trait("Covers", "Histogram2D")]
        public void Histogram2DAcceptsExplicitEdgesAndRejectsMismatchedLengths()
        {
            var h = tools.Histogram2D([1.0, 2.0, 9.0], [5.0, 6.0, 7.0], 3, 3,
                [0.0, 5.0, 10.0], [0.0, 10.0]);

            h.Counts[0].Should().Equal(2);
            h.Counts[1].Should().Equal(1);

            var clipped = tools.Histogram2D([1.0, 2.0, 30.0], [5.0, 6.0, 7.0], 3, 3,
                [0.0, 5.0, 10.0], [0.0, 10.0]);
            clipped.OutsideCount.Should().Be(1);

            Action mismatched = () => tools.Histogram2D([1.0, 2.0], [1.0]);
            Action badBins = () => tools.Histogram2D([1.0, 2.0], [1.0, 2.0], 0);

            mismatched.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("y");
            badBins.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("xBins");
        }

        [Fact]
        [Trait("Covers", "EmpiricalCumulativeDistribution")]
        public void EmpiricalCdfCountsTheShareAtOrBelowEachPoint()
        {
            var values = new[] { 1.0, 2.0, 2.0, 3.0, 4.0 };

            var result = tools.EmpiricalCumulativeDistribution(values,
                [0.0, 1.0, 2.0, 2.5, 4.0, 5.0, double.NegativeInfinity, double.PositiveInfinity]);

            result.Probabilities.Should().Equal(0.0, 0.2, 0.6, 0.6, 1.0, 1.0, 0.0, 1.0);
            result.UpperTail.Should().BeFalse();

            var upper = tools.EmpiricalCumulativeDistribution(values, [2.0], true);
            upper.Probabilities[0].Should().BeApproximately(0.4, 1e-12);
            upper.UpperTail.Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "EmpiricalCumulativeDistribution")]
        public void EmpiricalCdfValidatesInput()
        {
            Action nanPoint = () => tools.EmpiricalCumulativeDistribution([1.0], [double.NaN]);
            Action empty = () => tools.EmpiricalCumulativeDistribution([], [1.0]);

            nanPoint.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("points");
            empty.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("values");

            tools.EmpiricalCumulativeDistribution([5.0], [4.0, 5.0]).Probabilities
                .Should().Equal(0.0, 1.0);
        }

        [Fact]
        [Trait("Covers", "EmpiricalCumulativeDistributionSteps")]
        public void EmpiricalCdfStepsGroupRepeatedValues()
        {
            var steps = tools.EmpiricalCumulativeDistributionSteps([3.0, 1.0, 2.0, 4.0, 2.0]);

            steps.DistinctValues.Should().Equal(1.0, 2.0, 3.0, 4.0);
            steps.Counts.Should().Equal(1, 2, 1, 1);
            steps.CumulativeCounts.Should().Equal(1, 3, 4, 5);
            steps.CumulativeProbabilities.Should().Equal(0.2, 0.6, 0.8, 1.0);
            steps.SurvivalProbabilities[0].Should().BeApproximately(0.8, 1e-12);
            steps.SurvivalProbabilities[3].Should().BeApproximately(0.0, 1e-12);
            steps.SampleSize.Should().Be(5);
        }

        [Fact]
        [Trait("Covers", "EmpiricalQuantileFunction")]
        public void EmpiricalQuantilesFollowTheStandardDefinitions()
        {
            var values = new[] { 5.0, 1.0, 3.0, 2.0, 4.0 };
            var p = new[] { 0.0, 0.25, 0.5, 0.9, 1.0 };

            var type7 = tools.EmpiricalQuantileFunction(values, p);
            type7.Quantiles[0].Should().BeApproximately(1.0, 1e-12);
            type7.Quantiles[1].Should().BeApproximately(2.0, 1e-12);
            type7.Quantiles[2].Should().BeApproximately(3.0, 1e-12);
            type7.Quantiles[3].Should().BeApproximately(4.6, 1e-12);
            type7.Quantiles[4].Should().BeApproximately(5.0, 1e-12);
            type7.Definition.Should().Be("Type7");

            tools.EmpiricalQuantileFunction(values, [0.25], QuantileDefinition.Type6).Quantiles[0]
                .Should().BeApproximately(1.5, 1e-12);

            tools.EmpiricalQuantileFunction(values, [0.5, 0.61], QuantileDefinition.Type1).Quantiles
                .Should().Equal(3.0, 4.0);

            tools.EmpiricalQuantileFunction(values, [0.5], QuantileDefinition.Type8).Quantiles[0]
                .Should().BeApproximately(3.0, 1e-12);
        }

        [Fact]
        [Trait("Covers", "EmpiricalQuantileFunction")]
        public void EmpiricalQuantilesValidateProbabilities()
        {
            Action bad = () => tools.EmpiricalQuantileFunction([1.0, 2.0], [1.2]);
            Action empty = () => tools.EmpiricalQuantileFunction([], [0.5]);

            bad.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("probabilities");
            empty.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("values");
            tools.EmpiricalQuantileFunction([7.0], [0.1, 0.9]).Quantiles.Should().Equal(7.0, 7.0);
        }

        [Fact]
        [Trait("Covers", "KernelDensityEstimate")]
        public void GaussianKernelDensityMatchesTheHandComputedSumOfBumps()
        {
            var single = tools.KernelDensityEstimate([0.0], [0.0], KernelKind.Gaussian,
                BandwidthRule.Manual, 1.0);
            single.Density[0].Should().BeApproximately(0.3989422804014327, 1e-9);
            single.Bandwidth.Should().Be(1.0);
            single.Kernel.Should().Be("Gaussian");

            var two = tools.KernelDensityEstimate([-1.0, 1.0], [0.0], KernelKind.Gaussian,
                BandwidthRule.Manual, 1.0);
            two.Density[0].Should().BeApproximately(0.24197072451914337, 1e-9);

            var scaled = tools.KernelDensityEstimate([0.0], [1.0], KernelKind.Gaussian,
                BandwidthRule.Manual, 2.0);
            scaled.Density[0].Should().BeApproximately(0.3520653267642995 / 2.0, 1e-9);
        }

        [Fact]
        [Trait("Covers", "KernelDensityEstimate")]
        public void EpanechnikovAndUniformKernelsHaveFiniteSupport()
        {
            var epan = tools.KernelDensityEstimate([0.0], [0.5, 1.0, 2.0], KernelKind.Epanechnikov,
                BandwidthRule.Manual, 1.0);
            epan.Density[0].Should().BeApproximately(0.5625, 1e-12);
            epan.Density[1].Should().BeApproximately(0.0, 1e-12);
            epan.Density[2].Should().Be(0.0);

            var uniform = tools.KernelDensityEstimate([0.0], [1.0, 3.0], KernelKind.Uniform,
                BandwidthRule.Manual, 2.0);
            uniform.Density[0].Should().BeApproximately(0.25, 1e-12);
            uniform.Density[1].Should().Be(0.0);
        }

        [Fact]
        [Trait("Covers", "KernelDensityEstimate")]
        public void KernelDensityUsesTheNamedBandwidthRuleAndValidatesInput()
        {
            var normalReference = tools.KernelDensityEstimate(OneToN(100), [50.0]);
            normalReference.Bandwidth.Should().BeApproximately(12.233699573265657, 1e-9);
            normalReference.BandwidthRule.Should().Be("NormalReference");

            tools.KernelDensityEstimate(OneToN(100), [50.0], KernelKind.Gaussian,
                BandwidthRule.SilvermanRobust).Bandwidth.Should().BeApproximately(10.39471468564849, 1e-9);
            tools.KernelDensityEstimate(OneToN(100), [50.0], KernelKind.Gaussian, BandwidthRule.Oversmoothed)
                .Bandwidth.Should().BeApproximately(13.212837333757633, 1e-9);

            Action manualMissing = () => tools.KernelDensityEstimate(OneToN(5), [1.0], KernelKind.Gaussian,
                BandwidthRule.Manual);

            Action constant = () => tools.KernelDensityEstimate([2.0, 2.0, 2.0], [1.0]);
            Action nanPoint = () => tools.KernelDensityEstimate(OneToN(5), [double.NaN]);

            manualMissing.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("bandwidth");
            constant.Should().Throw<ArgumentException>().WithMessage("*Manual*");
            nanPoint.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("points");
        }

        [Fact]
        [Trait("Covers", "KernelDensityCurve")]
        public void KernelDensityCurveIntegratesToOneAndFindsASinglePeakForBellShapedData()
        {
            var data = QuantileGrid(DistributionKind.Normal, [0.0, 1.0], 300);

            var curve = tools.KernelDensityCurve(data, 400);

            curve.X.Length.Should().Be(400);
            var integral = 0.0;

            for (var i = 1; i < curve.X.Length; i++)
            {
                integral += 0.5 * (curve.Density[i] + curve.Density[i - 1]) * (curve.X[i] - curve.X[i - 1]);
            }

            integral.Should().BeApproximately(1.0, 0.01);
            curve.ModeCount.Should().Be(1);
            curve.PeakLocations[0].Should().BeApproximately(0.0, 0.2);
            curve.PeakDensities[0].Should().BeApproximately(0.39, 0.03);
            curve.X[0].Should().BeApproximately(-3.0 * curve.Bandwidth + data.Min(), 1e-9);
        }

        [Fact]
        [Trait("Covers", "KernelDensityCurve")]
        public void KernelDensityCurveFindsTwoPeaksForBimodalData()
        {
            var left = QuantileGrid(DistributionKind.Normal, [0.0, 1.0], 150);
            var right = QuantileGrid(DistributionKind.Normal, [10.0, 1.0], 150);

            var curve = tools.KernelDensityCurve([.. left, .. right], 500, KernelKind.Gaussian,
                BandwidthRule.SilvermanRobust);

            curve.ModeCount.Should().Be(2);
            curve.PeakLocations[0].Should().BeApproximately(0.0, 0.5);
            curve.PeakLocations[1].Should().BeApproximately(10.0, 0.5);

            curve.Notes.Should()
                .ContainSingle(n => n.Contains("more than one peak", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        [Trait("Covers", "KernelDensityCurve")]
        public void KernelDensityCurveValidatesItsArguments()
        {
            Action grid = () => tools.KernelDensityCurve(OneToN(10), 2);

            Action padding = () => tools.KernelDensityCurve(OneToN(10), 10, KernelKind.Gaussian,
                BandwidthRule.NormalReference, null, 20);

            grid.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("gridCount");
            padding.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("padding");
        }

        [Fact]
        [Trait("Covers", "KernelBandwidthRules")]
        public void BandwidthRulesReportEveryRuleWithItsInputs()
        {
            var result = tools.KernelBandwidthRules(OneToN(100));

            result.Estimates.Select(e => e.Rule).Should().Equal("NormalReference", "SilvermanRobust", "Oversmoothed");
            result.Estimates[0].Bandwidth.Should().BeApproximately(12.233699573265657, 1e-9);
            result.Estimates[1].Bandwidth.Should().BeApproximately(10.39471468564849, 1e-9);
            result.Estimates[2].Bandwidth.Should().BeApproximately(13.212837333757633, 1e-9);
            result.StandardDeviation.Should().BeApproximately(29.011491975882016, 1e-9);
            result.InterquartileRange.Should().BeApproximately(49.5, 1e-9);
        }

        [Fact]
        [Trait("Covers", "KernelBandwidthRules")]
        public void BandwidthRulesOmitRulesThatCannotBeComputed()
        {
            var result = tools.KernelBandwidthRules([4.0, 4.0, 4.0]);

            result.Estimates.Should().BeEmpty();
            result.Notes.Length.Should().Be(3);
        }
    }
}