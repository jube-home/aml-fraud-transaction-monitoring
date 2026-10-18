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
using FluentAssertions;
using Jube.Service.Agent.AdvancedAnalyticsTools.DescriptiveStatistics;
using Xunit;
using System.Linq;

namespace Jube.Test.AdvancedAnalytics.Statistics
{
    public class DescriptiveStatisticsToolsTests
    {
        private const double Tight = 1e-9;
        private const double Loose = 1e-6;
        private static readonly double[] standardDeviationTextbookExample = [2, 4, 4, 4, 5, 5, 7, 9];
        private readonly CentralTendencyTools central = new();
        private readonly QuantileRankAndFrequencyTools quantile = new();
        private readonly SpreadAndShapeTools spread = new();
        private readonly TableAndTransformTools table = new();

        private static void Throws(Action action, string parameter)
        {
            action.Should().Throw<ArgumentException>().Which.ParamName.Should().Be(parameter);
        }

        [Fact]
        [Trait("Covers", "Mean")]
        public void MeanOfTextbookExampleIsFive()
        {
            var result = central.Mean(standardDeviationTextbookExample);
            result.Value.Should().BeApproximately(5.0, Tight);
            result.Count.Should().Be(8);
            result.MissingIgnored.Should().Be(0);
        }

        [Fact]
        [Trait("Covers", "Mean")]
        public void MeanRejectsNaNUnlessIgnoredAndHandlesEmptyAndHugeValues()
        {
            Throws(() => central.Mean([1.0, double.NaN]), "values");
            Throws(() => central.Mean([]), "values");
            Throws(() => central.Mean(null!), "values");
            Throws(() => central.Mean([double.NaN], true), "values");
            var dropped = central.Mean([1.0, double.NaN, 3.0], true);
            dropped.Value.Should().BeApproximately(2.0, Tight);
            dropped.MissingIgnored.Should().Be(1);
            central.Mean([1e300, 1e300]).Value.Should().Be(1e300);
            central.Mean([7.0]).Value.Should().Be(7.0);
        }

        [Fact]
        [Trait("Covers", "WeightedMean")]
        public void WeightedMeanMatchesHandComputation()
        {
            central.WeightedMean([1.0, 2.0, 3.0], [3.0, 2.0, 1.0]).Value
                .Should().BeApproximately(10.0 / 6.0, Tight);

            Throws(() => central.WeightedMean([1.0, 2.0], [1.0]), "weights");
            Throws(() => central.WeightedMean([1.0, 2.0], [1.0, -1.0]), "weights");
            Throws(() => central.WeightedMean([1.0, 2.0], [0.0, 0.0]), "weights");

            central.WeightedMean([1.0, double.NaN, 3.0], [1.0, 5.0, 1.0], true).Value
                .Should().BeApproximately(2.0, Tight);
        }

        [Fact]
        [Trait("Covers", "TrimmedMean")]
        public void TrimmedMeanDropsOneValuePerTail()
        {
            central.TrimmedMean(standardDeviationTextbookExample, 0.125).Value.Should()
                .BeApproximately(29.0 / 6.0, Tight);

            central.TrimmedMean(standardDeviationTextbookExample, 0).Value.Should().BeApproximately(5.0, Tight);
            Throws(() => central.TrimmedMean(standardDeviationTextbookExample, 0.5), "trimFraction");
        }

        [Fact]
        [Trait("Covers", "WinsorisedMean")]
        public void WinsorisedMeanReplacesTailsWithNeighbours()
        {
            central.WinsorisedMean(standardDeviationTextbookExample, 0.125).Value.Should().BeApproximately(5.0, Tight);

            central.WinsorisedMean([1.0, 2.0, 3.0, 4.0, 100.0], 0.2).Value.Should()
                .BeApproximately((2 + 2 + 3 + 4 + 4) / 5.0, Tight);

            Throws(() => central.WinsorisedMean(standardDeviationTextbookExample, -0.1), "winsorFraction");
        }

        [Fact]
        [Trait("Covers", "GeometricMean")]
        public void GeometricMeanIsRootOfProduct()
        {
            central.GeometricMean(standardDeviationTextbookExample).Value.Should()
                .BeApproximately(Math.Pow(201600.0, 1.0 / 8.0), Loose);

            central.GeometricMean([1.0, 4.0]).Value.Should().BeApproximately(2.0, Loose);
            Throws(() => central.GeometricMean([1.0, 0.0]), "values");
            Throws(() => central.GeometricMean([1.0, -2.0]), "values");
        }

        [Fact]
        [Trait("Covers", "HarmonicMean")]
        public void HarmonicMeanIsCountOverSumOfReciprocals()
        {
            central.HarmonicMean(standardDeviationTextbookExample).Value
                .Should().BeApproximately(8.0 / (0.5 + 0.75 + 0.4 + 1.0 / 7 + 1.0 / 9), Tight);

            central.HarmonicMean([40.0, 60.0]).Value.Should().BeApproximately(48.0, Tight);
            Throws(() => central.HarmonicMean([0.0, 1.0]), "values");
        }

        [Fact]
        [Trait("Covers", "QuadraticMean")]
        public void QuadraticMeanIsRootMeanSquare()
        {
            central.QuadraticMean(standardDeviationTextbookExample).Value.Should()
                .BeApproximately(Math.Sqrt(29.0), Tight);

            central.QuadraticMean([-3.0, 3.0]).Value.Should().BeApproximately(3.0, Tight);
        }

        [Fact]
        [Trait("Covers", "PowerMean")]
        public void PowerMeanRecoversSpecialCases()
        {
            var x = standardDeviationTextbookExample;
            central.PowerMean(x, 1).Value.Should().BeApproximately(5.0, Tight);
            central.PowerMean(x, 2).Value.Should().BeApproximately(Math.Sqrt(29.0), Tight);
            central.PowerMean(x, 3).Value.Should().BeApproximately(Math.Pow(190.25, 1.0 / 3.0), Tight);
            central.PowerMean(x, 0).Value.Should().BeApproximately(Math.Pow(201600.0, 1.0 / 8.0), Loose);
            central.PowerMean([1.0, 4.0], -1).Value.Should().BeApproximately(1.6, Tight);
            central.PowerMean([-3.0, 3.0], 2).Value.Should().BeApproximately(3.0, Tight);
            Throws(() => central.PowerMean([-3.0, 3.0], 3), "values");
            Throws(() => central.PowerMean(x, double.NaN), "order");
        }

        [Fact]
        [Trait("Covers", "ContraharmonicMean")]
        public void ContraharmonicMeanIsSumOfSquaresOverSum()
        {
            central.ContraharmonicMean(standardDeviationTextbookExample).Value.Should()
                .BeApproximately(232.0 / 40.0, Tight);

            central.ContraharmonicMean(standardDeviationTextbookExample, 3).Value.Should()
                .BeApproximately(1522.0 / 232.0, Tight);

            central.ContraharmonicMean([1.0, 2.0, 3.0]).Value.Should().BeApproximately(14.0 / 6.0, Tight);
            Throws(() => central.ContraharmonicMean([-1.0, 1.0]), "values");
            Throws(() => central.ContraharmonicMean([1.0], 1), "order");
        }

        [Fact]
        [Trait("Covers", "ExponentialWeightedMean")]
        public void ExponentialWeightedMeanWithFullAlphaIsLastValue()
        {
            central.ExponentialWeightedMean([1.0, 2.0, 3.0], 1.0).Value.Should().BeApproximately(3.0, Tight);
            central.ExponentialWeightedMean([5.0, 5.0, 5.0], 0.4).Value.Should().BeApproximately(5.0, Tight);
            var recent = central.ExponentialWeightedMean([1.0, 1.0, 1.0, 10.0], 0.5).Value;
            recent.Should().BeGreaterThan(central.Mean([1.0, 1.0, 1.0, 10.0]).Value);
            Throws(() => central.ExponentialWeightedMean([1.0], 0), "alpha");
        }

        [Fact]
        [Trait("Covers", "GrandMean")]
        public void GrandMeanWeightsByGroupSize()
        {
            central.GrandMean([10.0, 20.0], [1, 3]).Value.Should().BeApproximately(17.5, Tight);
            central.GrandMean([10.0, 20.0], [1, 3]).Count.Should().Be(4);
            Throws(() => central.GrandMean([10.0], [1, 2]), "sampleSizes");
            Throws(() => central.GrandMean([10.0], [0]), "sampleSizes");
            Throws(() => central.GrandMean([], []), "groupMeans");
        }

        [Fact]
        [Trait("Covers", "Median")]
        public void MedianAveragesTheMiddlePairForEvenCounts()
        {
            central.Median(standardDeviationTextbookExample).Value.Should().BeApproximately(4.5, Tight);
            central.Median([9.0, 1.0, 5.0]).Value.Should().BeApproximately(5.0, Tight);
            central.Median([42.0]).Value.Should().Be(42.0);
            Throws(() => central.Median([]), "values");
        }

        [Fact]
        [Trait("Covers", "Trimean")]
        public void TrimeanCombinesQuartilesAndMedian()
        {
            central.Trimean(standardDeviationTextbookExample).Value.Should().BeApproximately(4.625, Tight);
        }

        [Fact]
        [Trait("Covers", "Midhinge")]
        public void MidhingeAveragesQuartiles()
        {
            central.Midhinge(standardDeviationTextbookExample).Value.Should().BeApproximately(4.75, Tight);
        }

        [Fact]
        [Trait("Covers", "MinimumMaximumRange")]
        public void MinimumMaximumRangeReportsExtent()
        {
            var r = central.MinimumMaximumRange(standardDeviationTextbookExample);
            r.Minimum.Should().Be(2);
            r.Maximum.Should().Be(9);
            r.Range.Should().Be(7);
            r.Midrange.Should().Be(5.5);
            r.Count.Should().Be(8);
            central.MinimumMaximumRange([3.0]).Range.Should().Be(0);
        }

        [Fact]
        [Trait("Covers", "Minimum")]
        public void MinimumFindsSmallestAndSkipsMissingWhenAsked()
        {
            central.Minimum([3.0, -1.0, 2.0]).Value.Should().Be(-1.0);
            central.Minimum([3.0, double.NaN, 2.0], true).Value.Should().Be(2.0);
            Throws(() => central.Minimum([3.0, double.NaN]), "values");
        }

        [Fact]
        [Trait("Covers", "Maximum")]
        public void MaximumFindsLargest()
        {
            central.Maximum([3.0, -1.0, 2.0]).Value.Should().Be(3.0);
            central.Maximum([double.NegativeInfinity, 1.0]).Value.Should().Be(1.0);
        }

        [Fact]
        [Trait("Covers", "ArgumentOfMinimum")]
        public void ArgumentOfMinimumReturnsFirstIndexInOriginalArray()
        {
            var r = central.ArgumentOfMinimum([5.0, double.NaN, 1.0, 1.0], true);
            r.Index.Should().Be(2);
            r.Value.Should().Be(1.0);
            r.MissingIgnored.Should().Be(1);
            Throws(() => central.ArgumentOfMinimum([double.NaN], true), "values");
        }

        [Fact]
        [Trait("Covers", "ArgumentOfMaximum")]
        public void ArgumentOfMaximumReturnsFirstIndexInOriginalArray()
        {
            var r = central.ArgumentOfMaximum([5.0, 9.0, 1.0, 9.0]);
            r.Index.Should().Be(1);
            r.Value.Should().Be(9.0);
            Throws(() => central.ArgumentOfMaximum([1.0, double.NaN]), "values");
        }

        [Fact]
        [Trait("Covers", "Sum")]
        public void SumAddsValues()
        {
            central.Sum(standardDeviationTextbookExample).Value.Should().Be(40.0);
            central.Sum([1e308, 1e308]).Value.Should().Be(double.PositiveInfinity);
        }

        [Fact]
        [Trait("Covers", "Product")]
        public void ProductMultipliesValues()
        {
            central.Product(standardDeviationTextbookExample).Value.Should().Be(201600.0);
            central.Product([2.0, 0.0, 5.0]).Value.Should().Be(0.0);
        }

        [Fact]
        [Trait("Covers", "CountValues")]
        public void CountValuesProfilesDataQuality()
        {
            var p = central.CountValues([1.0, double.NaN, 0.0, -2.0, double.PositiveInfinity, 1.0]);
            p.Total.Should().Be(6);
            p.Valid.Should().Be(5);
            p.Missing.Should().Be(1);
            p.Infinite.Should().Be(1);
            p.Distinct.Should().Be(4);
            p.Zeros.Should().Be(1);
            p.Positives.Should().Be(3);
            p.Negatives.Should().Be(1);
            p.MissingFraction.Should().BeApproximately(1.0 / 6.0, Tight);
            central.CountValues([]).MissingFraction.Should().Be(double.NaN);
            Throws(() => central.CountValues(null!), "values");
        }

        [Fact]
        [Trait("Covers", "ModeOfNumbers")]
        public void ModeOfNumbersReturnsEveryTiedMode()
        {
            var r = central.ModeOfNumbers([1.0, 2.0, 2.0, 3.0, 3.0]);
            r.Modes.Should().Equal(2.0, 3.0);
            r.Frequency.Should().Be(2);
            r.IsUnique.Should().BeFalse();
            var single = central.ModeOfNumbers(standardDeviationTextbookExample);
            single.Modes.Should().Equal(4.0);
            single.Frequency.Should().Be(3);
            single.IsUnique.Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "ModeOfCategories")]
        public void ModeOfCategoriesReturnsTiedLabels()
        {
            var r = central.ModeOfCategories(["a", "b", "b", "c", "c"]);
            r.Modes.Should().Equal("b", "c");
            r.Frequency.Should().Be(2);
            var missing = central.ModeOfCategories(["x", null!, "x"], true);
            missing.MissingIgnored.Should().Be(1);
            missing.Modes.Should().Equal("x");
            Throws(() => central.ModeOfCategories(["x", null!]), "values");
        }

        [Fact]
        [Trait("Covers", "WeightedMode")]
        public void WeightedModeUsesTotalWeight()
        {
            var r = central.WeightedMode([1.0, 2.0, 2.0], [10.0, 3.0, 4.0]);
            r.Modes.Should().Equal(1.0);
            r.Frequency.Should().Be(10);
            central.WeightedMode([1.0, 2.0, 2.0], [5.0, 3.0, 4.0]).Modes.Should().Equal(2.0);
            central.WeightedMode([1.0, 2.0], [1.0, 1.0]).Modes.Should().Equal(1.0, 2.0);
        }

        [Fact]
        [Trait("Covers", "CircularMean")]
        public void CircularMeanOfHoursAroundMidnightIsMidnight()
        {
            var mean = central.CircularMean([23.0, 1.0]).Value;
            Math.Min(mean, 24.0 - mean).Should().BeLessThan(1e-6);
            var degrees = central.CircularMean([350.0, 10.0], DescriptiveAngleUnit.Degrees).Value;
            Math.Min(degrees, 360.0 - degrees).Should().BeLessThan(1e-6);
            central.CircularMean([6.0, 6.0]).Value.Should().BeApproximately(6.0, Loose);
        }

        [Fact]
        [Trait("Covers", "CircularSummaryStatistics")]
        public void CircularSummaryResultantIsCosineOfHalfSpread()
        {
            var s = central.CircularSummaryStatistics([23.0, 1.0]);
            s.ResultantLength.Should().BeApproximately(Math.Cos(Math.PI / 12.0), Loose);
            s.CircularVariance.Should().BeApproximately(1 - Math.Cos(Math.PI / 12.0), Loose);
            var tight = central.CircularSummaryStatistics([6.0, 6.0, 6.0]);
            tight.ResultantLength.Should().BeApproximately(1.0, Loose);
            tight.CircularVariance.Should().BeApproximately(0.0, Loose);
            s.Count.Should().Be(2);
        }

        private static readonly double[] oneToTen = [.. Enumerable.Range(1, 10).Select(i => (double)i)];

        [Fact]
        [Trait("Covers", "Quantile")]
        public void QuantileFollowsHyndmanFanDefinitions()
        {
            quantile.Quantile(oneToTen, 0.25).Value.Should().BeApproximately(3.25, Tight);

            quantile.Quantile(oneToTen, 0.25, DescriptiveQuantileInterpolation.Weibull).Value.Should()
                .BeApproximately(2.75, Tight);

            quantile.Quantile(oneToTen, 0).Value.Should().Be(1.0);
            quantile.Quantile(oneToTen, 1).Value.Should().Be(10.0);
            quantile.Quantile([9.0], 0.5).Value.Should().Be(9.0);
            Throws(() => quantile.Quantile(oneToTen, 1.5), "probability");
            Throws(() => quantile.Quantile(oneToTen, double.NaN), "probability");
            Throws(() => quantile.Quantile([], 0.5), "values");
        }

        [Fact]
        [Trait("Covers", "Quantiles")]
        public void QuantilesReturnOnePerProbabilityInOrder()
        {
            var r = quantile.Quantiles(oneToTen, [0.9, 0.5, 0.1]);
            r.Values.Should().HaveCount(3);
            r.Values[0].Should().BeApproximately(9.1, Tight);
            r.Values[1].Should().BeApproximately(5.5, Tight);
            r.Values[2].Should().BeApproximately(1.9, Tight);
            r.Probabilities.Should().Equal(0.9, 0.5, 0.1);
            Throws(() => quantile.Quantiles(oneToTen, []), "probabilities");
            Throws(() => quantile.Quantiles(oneToTen, [-0.1]), "probabilities");
        }

        [Fact]
        [Trait("Covers", "Percentiles")]
        public void PercentilesUsePercentLevels()
        {
            var r = quantile.Percentiles(oneToTen, [50.0, 100.0]);
            r.Values.Should().Equal(5.5, 10.0);
            r.Probabilities.Should().Equal(0.5, 1.0);
            Throws(() => quantile.Percentiles(oneToTen, [101.0]), "percentLevels");
        }

        [Fact]
        [Trait("Covers", "EqualProbabilityCutPoints")]
        public void CutPointsDivideIntoEqualGroups()
        {
            var r = quantile.EqualProbabilityCutPoints([1.0, 2.0, 3.0, 4.0, 5.0], 4);
            r.Values.Should().Equal(2.0, 3.0, 4.0);
            r.Probabilities.Should().Equal(0.25, 0.5, 0.75);
            quantile.EqualProbabilityCutPoints(oneToTen, 10).Values.Should().HaveCount(9);
            Throws(() => quantile.EqualProbabilityCutPoints(oneToTen, 1), "parts");
        }

        [Fact]
        [Trait("Covers", "InterpercentileRange")]
        public void InterpercentileRangeIsDifferenceOfPercentiles()
        {
            quantile.InterpercentileRange(oneToTen, 10, 90).Value.Should().BeApproximately(9.1 - 1.9, Tight);
            quantile.InterpercentileRange(oneToTen, 25, 75).Value.Should().BeApproximately(4.5, Tight);
            Throws(() => quantile.InterpercentileRange(oneToTen, 90, 10), "upperPercent");
        }

        [Fact]
        [Trait("Covers", "FiveNumberSummary")]
        public void FiveNumberSummaryProvidesBoxPlotFences()
        {
            var r = quantile.FiveNumberSummary([1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0]);
            r.Minimum.Should().Be(1);
            r.LowerQuartile.Should().Be(3);
            r.Median.Should().Be(5);
            r.UpperQuartile.Should().Be(7);
            r.Maximum.Should().Be(9);
            r.InterquartileRange.Should().Be(4);
            r.LowerInnerFence.Should().Be(-3);
            r.UpperInnerFence.Should().Be(13);
            r.LowerOuterFence.Should().Be(-9);
            r.UpperOuterFence.Should().Be(19);
            r.Count.Should().Be(9);
            quantile.FiveNumberSummary([4.0]).InterquartileRange.Should().Be(0);
        }

        [Fact]
        [Trait("Covers", "PercentileRankOfValue")]
        public void PercentileRankOfValueHandlesTies()
        {
            var x = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };

            quantile.PercentileRankOfValue(x, 3, DescriptivePercentileRankKind.Weak).Value.Should()
                .BeApproximately(60.0, Tight);
            quantile.PercentileRankOfValue(x, 3, DescriptivePercentileRankKind.Strict).Value.Should()
                .BeApproximately(40.0, Tight);

            quantile.PercentileRankOfValue(x, 3).Value.Should().BeApproximately(50.0, Tight);
            quantile.PercentileRankOfValue(x, 100).Value.Should().BeApproximately(100.0, Tight);
            quantile.PercentileRankOfValue(x, -100).Value.Should().BeApproximately(0.0, Tight);
            Throws(() => quantile.PercentileRankOfValue(x, double.NaN), "score");
        }

        [Fact]
        [Trait("Covers", "PercentileRanksOfValues")]
        public void PercentileRanksOfValuesKeepPositionsAndMissing()
        {
            var r = quantile.PercentileRanksOfValues([10.0, double.NaN, 20.0, 30.0, 20.0]);
            r.Values[0].Should().BeApproximately(12.5, Tight);
            r.Values[1].Should().Be(double.NaN);
            r.Values[2].Should().BeApproximately(50.0, Tight);
            r.Values[3].Should().BeApproximately(87.5, Tight);
            r.Values[4].Should().BeApproximately(50.0, Tight);
            r.Count.Should().Be(4);
            r.MissingCount.Should().Be(1);
            Throws(() => quantile.PercentileRanksOfValues([double.NaN]), "values");
        }

        [Fact]
        [Trait("Covers", "RanksOfValues")]
        public void RanksHandleEveryTieMethod()
        {
            var x = new[] { 10.0, 20.0, 20.0, 30.0 };
            var average = quantile.RanksOfValues(x);
            average.Ranks.Should().Equal(1.0, 2.5, 2.5, 4.0);
            average.HasTies.Should().BeTrue();
            average.TiedGroups.Should().Be(1);
            quantile.RanksOfValues(x, DescriptiveRankTieMethod.Minimum).Ranks.Should().Equal(1.0, 2.0, 2.0, 4.0);
            quantile.RanksOfValues(x, DescriptiveRankTieMethod.Maximum).Ranks.Should().Equal(1.0, 3.0, 3.0, 4.0);
            quantile.RanksOfValues(x, DescriptiveRankTieMethod.Dense).Ranks.Should().Equal(1.0, 2.0, 2.0, 3.0);
            quantile.RanksOfValues(x, DescriptiveRankTieMethod.Ordinal).Ranks.Should().Equal(1.0, 2.0, 3.0, 4.0);
            quantile.RanksOfValues(x, DescriptiveRankTieMethod.Average, true).Ranks.Should().Equal(4.0, 2.5, 2.5, 1.0);
            var withMissing = quantile.RanksOfValues([3.0, double.NaN, 1.0]);
            withMissing.Ranks[0].Should().Be(2.0);
            withMissing.Ranks[1].Should().Be(double.NaN);
            withMissing.Ranks[2].Should().Be(1.0);
            withMissing.HasTies.Should().BeFalse();
            withMissing.MissingCount.Should().Be(1);
            Throws(() => quantile.RanksOfValues([]), "values");
        }

        [Fact]
        [Trait("Covers", "ThresholdShare")]
        public void ThresholdShareCountsBothSides()
        {
            var r = quantile.ThresholdShare([1.0, 2.0, 3.0, 4.0, 5.0], 3.0);
            r.Above.Should().Be(2);
            r.Below.Should().Be(2);
            r.Equal.Should().Be(1);
            r.ProportionAbove.Should().BeApproximately(0.4, Tight);
            r.ProportionEqual.Should().BeApproximately(0.2, Tight);
            Throws(() => quantile.ThresholdShare([1.0], double.NaN), "threshold");
        }

        [Fact]
        [Trait("Covers", "CumulativeSum")]
        public void CumulativeSumSkipsMissingButKeepsPositions()
        {
            quantile.CumulativeSum([1.0, 2.0, 3.0, 4.0]).Values.Should().Equal(1.0, 3.0, 6.0, 10.0);
            var r = quantile.CumulativeSum([1.0, double.NaN, 2.0]);
            r.Values[0].Should().Be(1.0);
            r.Values[1].Should().Be(double.NaN);
            r.Values[2].Should().Be(3.0);
            r.MissingCount.Should().Be(1);
            Throws(() => quantile.CumulativeSum([]), "values");
        }

        [Fact]
        [Trait("Covers", "CumulativeProduct")]
        public void CumulativeProductCompoundsFactors()
        {
            quantile.CumulativeProduct([1.0, 2.0, 3.0, 4.0]).Values.Should().Equal(1.0, 2.0, 6.0, 24.0);
            quantile.CumulativeProduct([1.1, 1.1]).Values[1].Should().BeApproximately(1.21, Tight);
        }

        [Fact]
        [Trait("Covers", "CumulativeMaximum")]
        public void CumulativeMaximumIsHighWaterMark()
        {
            quantile.CumulativeMaximum([3.0, 1.0, 4.0, 1.0, 5.0]).Values.Should()
                .Equal(3.0, 3.0, 4.0, 4.0, 5.0);
        }

        [Fact]
        [Trait("Covers", "CumulativeMinimum")]
        public void CumulativeMinimumIsLowWaterMark()
        {
            quantile.CumulativeMinimum([3.0, 1.0, 4.0, 1.0, 0.0]).Values.Should()
                .Equal(3.0, 1.0, 1.0, 1.0, 0.0);
        }

        [Fact]
        [Trait("Covers", "RollingStatistic")]
        public void RollingStatisticComputesTrailingWindows()
        {
            var x = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
            var mean = quantile.RollingStatistic(x, 3).Values;
            mean[0].Should().Be(double.NaN);
            mean[1].Should().Be(double.NaN);
            mean.Skip(2).Should().Equal(2.0, 3.0, 4.0);

            quantile.RollingStatistic(x, 3, DescriptiveWindowStatistic.Mean, 1).Values.Should()
                .Equal(1.0, 1.5, 2.0, 3.0, 4.0);

            quantile.RollingStatistic([1.0, 2.0, 3.0], 2, DescriptiveWindowStatistic.Sum).Values.Skip(1)
                .Should().Equal(3.0, 5.0);

            var sd = quantile
                .RollingStatistic([1.0, 3.0, 5.0], 2, DescriptiveWindowStatistic.StandardDeviation).Values;

            sd[1].Should().BeApproximately(Math.Sqrt(2.0), Tight);
            sd[2].Should().BeApproximately(Math.Sqrt(2.0), Tight);

            quantile.RollingStatistic(x, 2, DescriptiveWindowStatistic.Maximum).Values.Skip(1).Should()
                .Equal(2.0, 3.0, 4.0, 5.0);
            quantile.RollingStatistic(x, 2, DescriptiveWindowStatistic.Median).Values.Skip(1).Should()
                .Equal(1.5, 2.5, 3.5, 4.5);

            quantile.RollingStatistic(x, 1, DescriptiveWindowStatistic.Range).Values.Should()
                .Equal(0.0, 0.0, 0.0, 0.0, 0.0);

            var withGap = quantile
                .RollingStatistic([1.0, double.NaN, 3.0], 2, DescriptiveWindowStatistic.Count, 1).Values;

            withGap.Should().Equal(1.0, 1.0, 1.0);
            Throws(() => quantile.RollingStatistic(x, 0), "window");
            Throws(() => quantile.RollingStatistic(x, 6), "window");
            Throws(() => quantile.RollingStatistic([], 1), "values");
        }

        [Fact]
        [Trait("Covers", "ExpandingStatistic")]
        public void ExpandingStatisticUsesEverythingSoFar()
        {
            quantile.ExpandingStatistic([2.0, 4.0, 6.0]).Values.Should().Equal(2.0, 3.0, 4.0);

            var sd = quantile.ExpandingStatistic([2.0, 4.0, 6.0], DescriptiveWindowStatistic.StandardDeviation)
                .Values;
            sd[0].Should().Be(double.NaN);
            sd[1].Should().BeApproximately(Math.Sqrt(2.0), Tight);
            sd[2].Should().BeApproximately(2.0, Tight);

            quantile.ExpandingStatistic([2.0, 4.0, 6.0], DescriptiveWindowStatistic.Maximum, 2).Values[0]
                .Should().Be(double.NaN);

            Throws(() => quantile.ExpandingStatistic(new double[20001]), "values");
        }

        [Fact]
        [Trait("Covers", "ExponentialMovingAverageSeries")]
        public void ExponentialMovingAverageFollowsRecursion()
        {
            quantile.ExponentialMovingAverageSeries([2.0, 4.0, 8.0], 0.5).Values.Should().Equal(2.0, 3.0, 5.5);
            var gap = quantile.ExponentialMovingAverageSeries([2.0, double.NaN, 4.0], 0.5).Values;
            gap[1].Should().Be(double.NaN);
            gap[2].Should().Be(3.0);
            quantile.ExponentialMovingAverageSeries([1.0, 5.0], 1.0).Values.Should().Equal(1.0, 5.0);
            Throws(() => quantile.ExponentialMovingAverageSeries([1.0], 0.0), "alpha");
        }

        [Fact]
        [Trait("Covers", "FrequencyTableOfCategories")]
        public void FrequencyTableOfCategoriesCountsAndAccumulates()
        {
            var r = quantile.FrequencyTableOfCategories(["a", "b", "a", "c", "a", "b"]);
            r.Rows.Select(x => x.Value).Should().Equal("a", "b", "c");
            r.Rows.Select(x => x.Count).Should().Equal(3, 2, 1);
            r.Rows[0].Proportion.Should().BeApproximately(0.5, Tight);
            r.Rows[0].Percent.Should().BeApproximately(50.0, Tight);
            r.Rows[1].CumulativeCount.Should().Be(5);
            r.Rows[2].CumulativeProportion.Should().BeApproximately(1.0, Tight);
            r.Total.Should().Be(6);
            r.Distinct.Should().Be(3);

            var limited = quantile.FrequencyTableOfCategories(["a", "b", "a", "c"],
                DescriptiveFrequencyOrder.ByValueAscending, 2);

            limited.Rows.Select(x => x.Value).Should().Equal("a", "b");
            limited.RowsOmitted.Should().Be(1);

            quantile.FrequencyTableOfCategories(["z", "a"], DescriptiveFrequencyOrder.ByFirstAppearance)
                .Rows[0].Value.Should().Be("z");

            Throws(
                () => quantile.FrequencyTableOfCategories(["a"], DescriptiveFrequencyOrder.ByCountDescending,
                    0), "maxRows");

            Throws(() => quantile.FrequencyTableOfCategories([]), "values");
        }

        [Fact]
        [Trait("Covers", "FrequencyTableOfIntegers")]
        public void FrequencyTableOfIntegersOrdersNumerically()
        {
            var r = quantile.FrequencyTableOfIntegers([10, 2, 2, 10, 10]);
            r.Rows.Select(x => x.Value).Should().Equal("10", "2");

            quantile.FrequencyTableOfIntegers([10, 2, 2], DescriptiveFrequencyOrder.ByValueAscending).Rows
                .Select(x => x.Value)
                .Should().Equal("2", "10");

            Throws(() => quantile.FrequencyTableOfIntegers([]), "values");
        }

        [Fact]
        [Trait("Covers", "FrequencyTableOfNumbers")]
        public void FrequencyTableOfNumbersFindsRepeatedAmounts()
        {
            var r = quantile.FrequencyTableOfNumbers([9.99, 100.0, 9.99, 9.99, 100.0, 1.5]);
            r.Rows[0].Value.Should().Be("9.99");
            r.Rows[0].Count.Should().Be(3);
            r.Rows[1].Value.Should().Be("100");

            var ascending =
                quantile.FrequencyTableOfNumbers([100.0, 9.99], DescriptiveFrequencyOrder.ByValueAscending);

            ascending.Rows.Select(x => x.Value).Should().Equal("9.99", "100");

            var missing = quantile.FrequencyTableOfNumbers([1.0, double.NaN],
                DescriptiveFrequencyOrder.ByCountDescending, 5, true);
            missing.MissingCount.Should().Be(1);
            Throws(() => quantile.FrequencyTableOfNumbers([1.0, double.NaN]), "values");
        }

        [Fact]
        [Trait("Covers", "RareCategories")]
        public void RareCategoriesListsSingletons()
        {
            var r = quantile.RareCategories(["a", "a", "b", "c"]);
            r.Rows.Select(x => x.Value).Should().Equal("b", "c");
            r.Distinct.Should().Be(3);
            r.Total.Should().Be(4);
            quantile.RareCategories(["a", "a", "b", "c"], 2).Rows.Should().HaveCount(3);
            quantile.RareCategories(["a", "b", "c"], 1, 1).RowsOmitted.Should().Be(2);
            Throws(() => quantile.RareCategories(["a"], 0), "maxCount");
        }

        private static readonly double sampleVarianceOfTextbookExample = 32.0 / 7.0;

        [Fact]
        [Trait("Covers", "Variance")]
        public void VarianceMatchesPopulationAndSampleForms()
        {
            spread.Variance(standardDeviationTextbookExample, false).Value.Should().BeApproximately(4.0, Tight);

            spread.Variance(standardDeviationTextbookExample).Value.Should()
                .BeApproximately(sampleVarianceOfTextbookExample, Tight);

            spread.Variance([3.0], false).Value.Should().Be(0.0);
            spread.Variance([4.0, 4.0, 4.0]).Value.Should().Be(0.0);
            Throws(() => spread.Variance([3.0]), "values");
            Throws(() => spread.Variance([1.0, double.NaN, 2.0]), "values");
        }

        [Fact]
        [Trait("Covers", "StandardDeviation")]
        public void StandardDeviationMatchesWikipediaExample()
        {
            spread.StandardDeviation(standardDeviationTextbookExample, false).Value.Should()
                .BeApproximately(2.0, Tight);

            spread.StandardDeviation(standardDeviationTextbookExample).Value.Should()
                .BeApproximately(Math.Sqrt(32.0 / 7.0), Tight);

            spread.StandardDeviation([1e150, -1e150], false).Value.Should().BeApproximately(1e150, 1e140);
            Throws(() => spread.StandardDeviation([]), "values");
        }

        [Fact]
        [Trait("Covers", "StandardErrorOfMean")]
        public void StandardErrorIsSdOverRootN()
        {
            spread.StandardErrorOfMean(standardDeviationTextbookExample).Value
                .Should().BeApproximately(Math.Sqrt(32.0 / 7.0) / Math.Sqrt(8.0), Tight);

            Throws(() => spread.StandardErrorOfMean([1.0]), "values");
        }

        [Fact]
        [Trait("Covers", "CoefficientOfVariation")]
        public void CoefficientOfVariationIsSdOverMean()
        {
            spread.CoefficientOfVariation(standardDeviationTextbookExample, false).Value.Should()
                .BeApproximately(0.4, Tight);

            spread.CoefficientOfVariation(standardDeviationTextbookExample).Value.Should()
                .BeApproximately(Math.Sqrt(32.0 / 7.0) / 5.0, Tight);

            spread.CoefficientOfVariation([-1.0, 1.0]).Value.Should().Be(double.PositiveInfinity);
        }

        [Fact]
        [Trait("Covers", "VarianceToMeanRatio")]
        public void VarianceToMeanRatioFlagsOverdispersion()
        {
            spread.VarianceToMeanRatio(standardDeviationTextbookExample).Value.Should()
                .BeApproximately(sampleVarianceOfTextbookExample / 5.0, Tight);

            spread.VarianceToMeanRatio([2.0, 2.0, 2.0]).Value.Should().Be(0.0);
        }

        [Fact]
        [Trait("Covers", "MeanAbsoluteDeviation")]
        public void MeanAbsoluteDeviationAroundMeanAndMedian()
        {
            spread.MeanAbsoluteDeviation(standardDeviationTextbookExample).Value.Should().BeApproximately(1.5, Tight);

            spread.MeanAbsoluteDeviation(standardDeviationTextbookExample, true).Value.Should()
                .BeApproximately(12.0 / 8.0, Tight);

            spread.MeanAbsoluteDeviation([1.0, 2.0, 9.0], true).Value.Should()
                .BeApproximately((1 + 0 + 7) / 3.0, Tight);
        }

        [Fact]
        [Trait("Covers", "MedianAbsoluteDeviation")]
        public void MedianAbsoluteDeviationIsRobustToOutlier()
        {
            spread.MedianAbsoluteDeviation(standardDeviationTextbookExample, false).Value.Should()
                .BeApproximately(0.5, Tight);

            spread.MedianAbsoluteDeviation(standardDeviationTextbookExample).Value.Should()
                .BeApproximately(0.5 * 1.482602218505602, Tight);

            spread.MedianAbsoluteDeviation([1.0, 2.0, 3.0, 4.0, 1000.0], false).Value.Should()
                .BeApproximately(1.0, Tight);

            spread.MedianAbsoluteDeviation([5.0, 5.0, 5.0]).Value.Should().Be(0.0);
        }

        [Fact]
        [Trait("Covers", "InterquartileRange")]
        public void InterquartileRangeUsesChosenInterpolation()
        {
            spread.InterquartileRange(standardDeviationTextbookExample).Value.Should().BeApproximately(1.5, Tight);

            spread.InterquartileRange([.. Enumerable.Range(1, 10).Select(i => (double)i)]).Value.Should()
                .BeApproximately(4.5, Tight);

            spread.InterquartileRange([.. Enumerable.Range(1, 10).Select(i => (double)i)],
                    DescriptiveQuantileInterpolation.Weibull).Value
                .Should().BeApproximately(8.25 - 2.75, Tight);
        }

        [Fact]
        [Trait("Covers", "QuartileCoefficientOfDispersion")]
        public void QuartileCoefficientOfDispersionIsQuartileRatio()
        {
            spread.QuartileCoefficientOfDispersion(standardDeviationTextbookExample).Value.Should()
                .BeApproximately(1.5 / 9.5, Tight);
        }

        [Fact]
        [Trait("Covers", "SumOfSquaredDeviations")]
        public void SumOfSquaredDeviationsIsThirtyTwo()
        {
            spread.SumOfSquaredDeviations(standardDeviationTextbookExample).Value.Should()
                .BeApproximately(32.0, Tight);

            spread.SumOfSquaredDeviations([6.0]).Value.Should().Be(0.0);
        }

        [Fact]
        [Trait("Covers", "WeightedVariance")]
        public void WeightedVarianceFrequencyAndReliabilityCorrections()
        {
            var x = new[] { 1.0, 2.0, 3.0 };
            var w = new[] { 1.0, 2.0, 1.0 };

            spread.WeightedVariance(x, w, DescriptiveWeightKind.Frequency).Value.Should()
                .BeApproximately(2.0 / 3.0, Tight);

            spread.WeightedVariance(x, w).Value.Should()
                .BeApproximately(2.0 / 2.5, Tight);

            spread.WeightedVariance(x, w, DescriptiveWeightKind.Frequency, false).Value.Should()
                .BeApproximately(0.5, Tight);

            Throws(() => spread.WeightedVariance(x, [1.0, 2.0]), "weights");
            Throws(() => spread.WeightedVariance(x, [0.0, 0.0, 0.0]), "weights");
        }

        [Fact]
        [Trait("Covers", "WeightedStandardDeviation")]
        public void WeightedStandardDeviationIsRootOfWeightedVariance()
        {
            var x = new[] { 1.0, 2.0, 3.0 };
            var w = new[] { 1.0, 2.0, 1.0 };

            spread.WeightedStandardDeviation(x, w, DescriptiveWeightKind.Frequency).Value.Should()
                .BeApproximately(Math.Sqrt(2.0 / 3.0), Tight);

            spread.WeightedStandardDeviation(x, w, DescriptiveWeightKind.Frequency, false).Value.Should()
                .BeApproximately(Math.Sqrt(0.5), Tight);
        }

        [Fact]
        [Trait("Covers", "ExponentialWeightedVariance")]
        public void ExponentialWeightedVarianceOfConstantSeriesIsZero()
        {
            spread.ExponentialWeightedVariance([3.0, 3.0, 3.0, 3.0], 0.4).Value.Should()
                .BeApproximately(0.0, Tight);

            spread.ExponentialWeightedVariance([1.0, 5.0, 2.0, 8.0], 0.4).Value.Should().BeGreaterThan(0.0);
            Throws(() => spread.ExponentialWeightedVariance([1.0, 2.0], 1.5), "alpha");
            Throws(() => spread.ExponentialWeightedVariance([1.0]), "values");
        }

        [Fact]
        [Trait("Covers", "PooledVariance")]
        public void PooledVarianceWeightsByDegreesOfFreedom()
        {
            var groups = new[] { new[] { 1.0, 2.0, 3.0 }, new[] { 2.0, 4.0, 6.0 } };
            var r = spread.PooledVariance(groups);
            r.Value.Should().BeApproximately(2.5, Tight);
            r.Count.Should().Be(6);
            Throws(() => spread.PooledVariance([[1.0, 2.0]]), "groups");
            Throws(() => spread.PooledVariance([[1.0, 2.0], [3.0]]), "groups[1]");
        }

        [Fact]
        [Trait("Covers", "PooledStandardDeviation")]
        public void PooledStandardDeviationIsRootOfPooledVariance()
        {
            var groups = new[] { new[] { 1.0, 2.0, 3.0 }, new[] { 2.0, 4.0, 6.0 } };
            spread.PooledStandardDeviation(groups).Value.Should().BeApproximately(Math.Sqrt(2.5), Tight);

            spread.PooledStandardDeviation([[1.0, double.NaN, 3.0], [2.0, 4.0]], true).Value
                .Should().BeApproximately(Math.Sqrt((1 * 2.0 + 1 * 2.0) / 2.0), Tight);
        }

        [Fact]
        [Trait("Covers", "PooledVarianceFromSummaries")]
        public void PooledVarianceFromSummariesMatchesRawData()
        {
            spread.PooledVarianceFromSummaries([3, 3], [1.0, 4.0]).Value.Should()
                .BeApproximately(2.5, Tight);

            Throws(() => spread.PooledVarianceFromSummaries([1, 3], [1.0, 4.0]), "variances");
            Throws(() => spread.PooledVarianceFromSummaries([3], [1.0, 4.0]), "sizes");
        }

        [Fact]
        [Trait("Covers", "Skewness")]
        public void SkewnessMatchesMomentCoefficient()
        {
            spread.Skewness(standardDeviationTextbookExample, false).Value.Should().BeApproximately(0.65625, Tight);

            spread.Skewness(standardDeviationTextbookExample).Value.Should()
                .BeApproximately(Math.Sqrt(56.0) / 6.0 * 0.65625, Tight);

            spread.Skewness([1.0, 2.0, 3.0], false).Value.Should().BeApproximately(0.0, Tight);
            spread.Skewness([4.0, 4.0, 4.0], false).Value.Should().Be(double.NaN);
            Throws(() => spread.Skewness([1.0, 2.0]), "values");
        }

        [Fact]
        [Trait("Covers", "ExcessKurtosis")]
        public void ExcessKurtosisMatchesMomentCoefficient()
        {
            spread.ExcessKurtosis(standardDeviationTextbookExample, false).Value.Should()
                .BeApproximately(-0.21875, Tight);

            var n = 8.0;

            var expected = n * (n + 1) / ((n - 1) * (n - 2) * (n - 3)) * 356.0 / Math.Pow(32.0 / 7.0, 2)
                           - 3 * (n - 1) * (n - 1) / ((n - 2) * (n - 3));

            spread.ExcessKurtosis(standardDeviationTextbookExample).Value.Should().BeApproximately(expected, Tight);
            Throws(() => spread.ExcessKurtosis([1.0, 2.0, 3.0]), "values");
        }

        [Fact]
        [Trait("Covers", "Kurtosis")]
        public void KurtosisIsExcessPlusThree()
        {
            spread.Kurtosis(standardDeviationTextbookExample).Value.Should().BeApproximately(2.78125, Tight);
            spread.Kurtosis([-1.0, 1.0]).Value.Should().BeApproximately(1.0, Tight);
        }

        [Fact]
        [Trait("Covers", "ShapeStatistics")]
        public void ShapeStatisticsCombinesAllThree()
        {
            var s = spread.ShapeStatistics(standardDeviationTextbookExample, false);
            s.Skewness.Should().BeApproximately(0.65625, Tight);
            s.ExcessKurtosis.Should().BeApproximately(-0.21875, Tight);
            s.Kurtosis.Should().BeApproximately(2.78125, Tight);
            s.UnbiasedEstimators.Should().BeFalse();
            spread.ShapeStatistics(standardDeviationTextbookExample).UnbiasedEstimators.Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "RawMoment")]
        public void RawMomentsMatchHandComputation()
        {
            spread.RawMoment(standardDeviationTextbookExample, 1).Value.Should().BeApproximately(5.0, Tight);
            spread.RawMoment(standardDeviationTextbookExample, 2).Value.Should().BeApproximately(29.0, Tight);
            spread.RawMoment(standardDeviationTextbookExample, 3).Value.Should().BeApproximately(190.25, Tight);
            Throws(() => spread.RawMoment(standardDeviationTextbookExample, 0), "order");
            Throws(() => spread.RawMoment(standardDeviationTextbookExample, 13), "order");
        }

        [Fact]
        [Trait("Covers", "CentralMoment")]
        public void CentralMomentsMatchHandComputation()
        {
            spread.CentralMoment(standardDeviationTextbookExample, 1).Value.Should().BeApproximately(0.0, Tight);
            spread.CentralMoment(standardDeviationTextbookExample, 2).Value.Should().BeApproximately(4.0, Tight);
            spread.CentralMoment(standardDeviationTextbookExample, 3).Value.Should().BeApproximately(5.25, Tight);
            spread.CentralMoment(standardDeviationTextbookExample, 4).Value.Should().BeApproximately(44.5, Tight);
        }

        [Fact]
        [Trait("Covers", "StandardisedMoment")]
        public void StandardisedMomentsRecoverSkewnessAndKurtosis()
        {
            spread.StandardisedMoment(standardDeviationTextbookExample, 3).Value.Should()
                .BeApproximately(0.65625, Tight);
            spread.StandardisedMoment(standardDeviationTextbookExample, 4).Value.Should()
                .BeApproximately(2.78125, Tight);

            spread.StandardisedMoment([2.0, 2.0], 3).Value.Should().Be(double.NaN);
            Throws(() => spread.StandardisedMoment(standardDeviationTextbookExample, 2), "order");
        }

        [Fact]
        [Trait("Covers", "EntropyOfCounts")]
        public void EntropyOfCountsMatchesInformationTheoryExamples()
        {
            var even = spread.EntropyOfCounts([1.0, 1.0, 1.0, 1.0]);
            even.Entropy.Should().BeApproximately(2.0, Tight);
            even.NormalisedEntropy.Should().BeApproximately(1.0, Tight);
            even.Perplexity.Should().BeApproximately(4.0, Tight);
            var skew = spread.EntropyOfCounts([2.0, 1.0, 1.0]);
            skew.Entropy.Should().BeApproximately(1.5, Tight);
            skew.MaximumEntropy.Should().BeApproximately(Math.Log(3, 2), Tight);

            spread.EntropyOfCounts([2.0, 1.0, 1.0], DescriptiveEntropyUnit.Nats).Entropy.Should()
                .BeApproximately(1.5 * Math.Log(2), Tight);

            spread.EntropyOfCounts([5.0, 0.0]).Entropy.Should().BeApproximately(0.0, Tight);
            Throws(() => spread.EntropyOfCounts([0.0, 0.0]), "counts");
            Throws(() => spread.EntropyOfCounts([-1.0, 2.0]), "counts");
        }

        [Fact]
        [Trait("Covers", "EntropyOfCategories")]
        public void EntropyOfCategoriesUsesObservedFrequencies()
        {
            var r = spread.EntropyOfCategories(["a", "a", "b", "c"]);
            r.Entropy.Should().BeApproximately(1.5, Tight);
            r.Categories.Should().Be(3);
            spread.EntropyOfCategories(["x", "x"]).Entropy.Should().BeApproximately(0.0, Tight);
            Throws(() => spread.EntropyOfCategories(["x", null!]), "values");
        }

        [Fact]
        [Trait("Covers", "InequalityMeasures")]
        public void InequalityMeasuresForOneToFour()
        {
            var r = spread.InequalityMeasures([1.0, 2.0, 3.0, 4.0]);
            r.Gini.Should().BeApproximately(0.25, Tight);
            r.HerfindahlIndex.Should().BeApproximately(0.30, Tight);
            var theil = new[] { 0.4, 0.8, 1.2, 1.6 }.Sum(s => s * Math.Log(s)) / 4.0;
            r.Theil.Should().BeApproximately(theil, Tight);
            r.Lorenz.Select(p => p.ValueShare).Should().Equal(0.0, 0.1, 0.3, 0.6, 1.0);
            r.Lorenz.Select(p => p.PopulationShare).Should().Equal(0.0, 0.25, 0.5, 0.75, 1.0);
            spread.InequalityMeasures([5.0, 5.0, 5.0]).Gini.Should().BeApproximately(0.0, Tight);
            spread.InequalityMeasures([0.0, 0.0, 0.0, 10.0]).Gini.Should().BeApproximately(0.75, Tight);
            Throws(() => spread.InequalityMeasures([-1.0, 2.0]), "values");
            Throws(() => spread.InequalityMeasures([0.0, 0.0]), "values");

            spread.InequalityMeasures([.. Enumerable.Range(1, 1000).Select(i => (double)i)]).Lorenz.Length
                .Should().BeLessOrEqualTo(101);
        }

        [Fact]
        [Trait("Covers", "GiniCoefficient")]
        public void GiniCoefficientOfConsecutiveIntegers()
        {
            spread.GiniCoefficient([1.0, 2.0, 3.0, 4.0]).Value.Should().BeApproximately(0.25, Tight);

            spread.GiniCoefficient([.. Enumerable.Range(1, 10).Select(i => (double)i)]).Value.Should()
                .BeApproximately(9.0 / 30.0, Tight);

            spread.GiniCoefficient([7.0]).Value.Should().BeApproximately(0.0, Tight);
        }

        [Fact]
        [Trait("Covers", "GeometricStandardDeviation")]
        public void GeometricStandardDeviationIsExpOfLogSd()
        {
            spread.GeometricStandardDeviation([1.0, 10.0]).Value.Should()
                .BeApproximately(Math.Exp(Math.Log(10.0) / Math.Sqrt(2.0)), Tight);

            spread.GeometricStandardDeviation([3.0, 3.0, 3.0]).Value.Should().BeApproximately(1.0, Tight);
            Throws(() => spread.GeometricStandardDeviation([1.0, 0.0]), "values");
            Throws(() => spread.GeometricStandardDeviation([1.0]), "values");
        }

        private static readonly double[] outlierExample = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 100];

        private static double[][] Rows(params double[][] rows)
        {
            return rows;
        }

        [Fact]
        [Trait("Covers", "SummariseColumns")]
        public void SummariseColumnsGivesOneRowPerColumn()
        {
            var rows = standardDeviationTextbookExample.Select((x, i) => new[] { x, i * 10.0 }).ToArray();
            var r = table.SummariseColumns(rows, ["score", "index"]);
            r.RowCount.Should().Be(8);
            r.ColumnCount.Should().Be(2);
            var c = r.Columns[0];
            c.Name.Should().Be("score");
            c.Count.Should().Be(8);
            c.Sum.Should().Be(40.0);
            c.Mean.Should().BeApproximately(5.0, Tight);
            c.Median.Should().BeApproximately(4.5, Tight);
            c.Variance.Should().BeApproximately(sampleVarianceOfTextbookExample, Tight);
            c.StandardDeviation.Should().BeApproximately(Math.Sqrt(sampleVarianceOfTextbookExample), Tight);
            c.Minimum.Should().Be(2);
            c.Maximum.Should().Be(9);
            c.Range.Should().Be(7);
            c.LowerQuartile.Should().BeApproximately(4.0, Tight);
            c.UpperQuartile.Should().BeApproximately(5.5, Tight);
            c.InterquartileRange.Should().BeApproximately(1.5, Tight);
            c.MedianAbsoluteDeviation.Should().BeApproximately(0.5 * 1.482602218505602, Tight);
            c.Skewness.Should().BeApproximately(Math.Sqrt(56.0) / 6.0 * 0.65625, Tight);
            c.Mode.Should().Be(4.0);
            c.Distinct.Should().Be(5);
            c.GeometricMean.Should().BeApproximately(Math.Pow(201600.0, 1.0 / 8.0), Loose);
            r.Columns[1].Mean.Should().BeApproximately(35.0, Tight);
        }

        [Fact]
        [Trait("Covers", "SummariseColumns")]
        public void SummariseColumnsHandlesMissingSingleAndAllMissingColumns()
        {
            var r = table.SummariseColumns(Rows([1.0, double.NaN], [double.NaN, double.NaN],
                [3.0, double.NaN]));

            r.Columns[0].Name.Should().Be("column0");
            r.Columns[0].Count.Should().Be(2);
            r.Columns[0].Missing.Should().Be(1);
            r.Columns[0].Skewness.Should().Be(double.NaN);
            r.Columns[1].Count.Should().Be(0);
            r.Columns[1].Mean.Should().Be(double.NaN);
            table.SummariseColumns(Rows([5.0])).Columns[0].StandardDeviation.Should().Be(double.NaN);
            Throws(() => table.SummariseColumns([]), "rows");
            Throws(() => table.SummariseColumns(Rows([1.0, 2.0], [1.0])), "rows");
            Throws(() => table.SummariseColumns(Rows([1.0, 2.0]), ["a"]), "columnNames");
            Throws(() => table.SummariseColumns(Rows([1.0, 2.0]), ["a", "a"]), "columnNames");
        }

        [Fact]
        [Trait("Covers", "DescribeColumn")]
        public void DescribeColumnProfilesTextbookExample()
        {
            var r = table.DescribeColumn(standardDeviationTextbookExample, "score");
            r.Summary.Name.Should().Be("score");
            r.Summary.Mean.Should().BeApproximately(5.0, Tight);
            r.HarmonicMean.Should().BeApproximately(8.0 / (0.5 + 0.75 + 0.4 + 1.0 / 7 + 1.0 / 9), Tight);
            r.TrimmedMean.Should().BeApproximately(5.0, Tight);
            r.MeanAbsoluteDeviation.Should().BeApproximately(1.5, Tight);
            r.Kurtosis.Should().BeApproximately(2.78125, Tight);
            r.Trimean.Should().BeApproximately(4.625, Tight);
            r.PercentileLevels.Should().Equal(1.0, 5.0, 10.0, 25.0, 50.0, 75.0, 90.0, 95.0, 99.0);
            r.Percentiles[4].Should().BeApproximately(4.5, Tight);
            r.LowerInnerFence.Should().BeApproximately(4.0 - 2.25, Tight);
            r.UpperInnerFence.Should().BeApproximately(5.5 + 2.25, Tight);
            r.IqrOutlierCount.Should().Be(1);
            r.Gini.Should().BeGreaterThan(0.0);
            r.TopValues[0].Value.Should().Be("4");
            r.TopValues[0].Count.Should().Be(3);
            r.ShapeReading.Should().Contain("right-skewed");
        }

        [Fact]
        [Trait("Covers", "DescribeColumn")]
        public void DescribeColumnHandlesConstantMissingAndNegativeData()
        {
            var constant = table.DescribeColumn([3.0, 3.0, 3.0, double.NaN]);
            constant.ShapeReading.Should().Contain("constant");
            constant.ShapeReading.Should().Contain("missing");
            constant.Summary.Missing.Should().Be(1);
            var negative = table.DescribeColumn([-1.0, 1.0, 2.0]);
            negative.Gini.Should().Be(double.NaN);
            negative.HarmonicMean.Should().Be(double.NaN);
            table.DescribeColumn(outlierExample).MadOutlierCount.Should().Be(1);
            table.DescribeColumn(outlierExample).ZScoreOutlierCount.Should().Be(0);
            Throws(() => table.DescribeColumn([]), "values");
            Throws(() => table.DescribeColumn([double.NaN]), "values");
        }

        [Fact]
        [Trait("Covers", "DescribeTable")]
        public void DescribeTableFindsCorrelationsConstantsSkewAndGaps()
        {
            var rows = Rows(
                [1.0, 10.0, 5.0, 1.0],
                [2.0, 20.0, 5.0, double.NaN],
                [3.0, 30.0, 5.0, 1.0],
                [4.0, 40.0, 5.0, 2.0],
                [5.0, 50.0, 5.0, 100.0]);

            var r = table.DescribeTable(rows, ["x", "y", "flat", "spiky"]);
            r.Summary.ColumnCount.Should().Be(4);
            r.StrongestCorrelations[0].First.Should().Be("x");
            r.StrongestCorrelations[0].Second.Should().Be("y");
            r.StrongestCorrelations[0].Correlation.Should().BeApproximately(1.0, Tight);
            r.ConstantColumns.Should().Equal("flat");
            r.ColumnsWithMissingValues.Should().Equal("spiky");
            r.ColumnsWithOutliers.Should().Equal("spiky");
            r.SkewedColumns.Should().Contain("spiky");
            r.Missing.RowsWithAnyMissing.Should().Be(1);
        }

        [Fact]
        [Trait("Covers", "TableMissingnessProfile")]
        public void MissingnessProfileCountsCellsAndRows()
        {
            var r = table.TableMissingnessProfile(
                Rows([1.0, double.NaN], [double.NaN, double.NaN], [3.0, 4.0],
                    [double.PositiveInfinity, 1.0]),
                ["a", "b"]);
            r.RowCount.Should().Be(4);
            r.Columns[0].Missing.Should().Be(1);
            r.Columns[1].Missing.Should().Be(2);
            r.Columns[0].Present.Should().Be(3);
            r.Columns[0].Infinite.Should().Be(1);
            r.Columns[1].MissingFraction.Should().BeApproximately(0.5, Tight);
            r.CompleteRows.Should().Be(2);
            r.RowsWithAnyMissing.Should().Be(2);
            r.MissingCountPerRowDistribution.Should().Equal(2, 1, 1);
            r.FirstIncompleteRowIndices.Should().Equal(0, 1);
        }

        [Fact]
        [Trait("Covers", "ColumnCovarianceMatrix")]
        public void CovarianceMatrixMatchesHandComputation()
        {
            var rows = Rows([1.0, 10.0], [2.0, 20.0], [3.0, 30.0], [4.0, 40.0]);
            var r = table.ColumnCovarianceMatrix(rows, ["x", "y"]);
            r.Values[0][0].Should().BeApproximately(5.0 / 3.0, Tight);
            r.Values[1][1].Should().BeApproximately(500.0 / 3.0, Tight);
            r.Values[0][1].Should().BeApproximately(50.0 / 3.0, Tight);
            r.Values[1][0].Should().BeApproximately(50.0 / 3.0, Tight);
            r.RowsUsed.Should().Be(4);
            Throws(() => table.ColumnCovarianceMatrix(Rows([1.0, double.NaN], [2.0, 3.0])), "rows");

            var dropped =
                table.ColumnCovarianceMatrix(Rows([1.0, double.NaN], [2.0, 3.0], [4.0, 9.0]),
                    null, true);
            dropped.RowsDropped.Should().Be(1);
            dropped.RowsUsed.Should().Be(2);
            Throws(() => table.ColumnCovarianceMatrix(Rows([1.0, 2.0])), "rows");
        }

        [Fact]
        [Trait("Covers", "ColumnCorrelationMatrix")]
        public void CorrelationMatrixIsUnitDiagonalAndSymmetric()
        {
            var rows = Rows([1.0, 10.0, 4.0], [2.0, 20.0, 3.0], [3.0, 30.0, 2.0],
                [4.0, 40.0, 1.0]);

            var r = table.ColumnCorrelationMatrix(rows);
            r.Names.Should().Equal("column0", "column1", "column2");
            r.Values[0][0].Should().Be(1.0);
            r.Values[0][1].Should().BeApproximately(1.0, Tight);
            r.Values[0][2].Should().BeApproximately(-1.0, Tight);
            r.Values[2][0].Should().BeApproximately(-1.0, Tight);

            var known = table.ColumnCorrelationMatrix(Rows([1.0, 2.0], [2.0, 1.0], [3.0, 4.0],
                [4.0, 3.0]));

            known.Values[0][1].Should().BeApproximately(0.6, Tight);
            var flat = table.ColumnCorrelationMatrix(Rows([1.0, 5.0], [2.0, 5.0], [3.0, 5.0]));
            double.IsNaN(flat.Values[0][1]).Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "StrongestColumnCorrelations")]
        public void StrongestCorrelationsAreRankedByAbsoluteValue()
        {
            var rows = Rows(
                [1.0, 2.0, 4.0], [2.0, 1.0, 3.0], [3.0, 4.0, 2.0], [4.0, 3.0, 1.0]);

            var r = table.StrongestColumnCorrelations(rows, ["a", "b", "c"], 0.5);
            r.Pairs[0].First.Should().Be("a");
            r.Pairs[0].Second.Should().Be("c");
            r.Pairs[0].Correlation.Should().BeApproximately(-1.0, Tight);
            Math.Abs(r.Pairs[1].Correlation).Should().BeApproximately(0.6, Tight);
            table.StrongestColumnCorrelations(rows, null, 0.95).Pairs.Should().HaveCount(1);
            table.StrongestColumnCorrelations(rows, null, 0.0, 1).Pairs.Should().HaveCount(1);
            Throws(() => table.StrongestColumnCorrelations(rows, null, 2.0), "minimumAbsoluteCorrelation");
            Throws(() => table.StrongestColumnCorrelations(rows, null, 0.3, 0), "maxPairs");
        }

        [Fact]
        [Trait("Covers", "SummariseRows")]
        public void SummariseRowsAggregatesAcrossColumns()
        {
            var r = table.SummariseRows(Rows([1.0, 2.0, 3.0], [double.NaN, 4.0, 6.0],
                [double.NaN, double.NaN, double.NaN]));

            r.Names.Should().Equal("Count", "Sum", "Mean", "Minimum", "Maximum", "StandardDeviation");
            r.Rows[0].Should().Equal(3.0, 6.0, 2.0, 1.0, 3.0, 1.0);
            r.Rows[1][0].Should().Be(2.0);
            r.Rows[1][1].Should().Be(10.0);
            r.Rows[1][5].Should().BeApproximately(Math.Sqrt(2.0), Tight);
            r.Rows[2][0].Should().Be(0.0);
            r.Rows[2][2].Should().Be(double.NaN);
        }

        [Fact]
        [Trait("Covers", "SummariseByGroup")]
        public void SummariseByGroupSplitsStatistics()
        {
            var r = table.SummariseByGroup([1.0, 2.0, 3.0, 4.0], ["a", "a", "b", "b"]);
            r.Groups.Select(g => g.Group).Should().Equal("a", "b");
            r.Groups[0].Mean.Should().BeApproximately(1.5, Tight);
            r.Groups[1].Mean.Should().BeApproximately(3.5, Tight);
            r.Groups[0].StandardDeviation.Should().BeApproximately(Math.Sqrt(0.5), Tight);
            r.PooledStandardDeviation.Should().BeApproximately(Math.Sqrt(0.5), Tight);
            r.GrandMean.Should().BeApproximately(2.5, Tight);
            var single = table.SummariseByGroup([1.0, 2.0, double.NaN], ["a", "b", "b"]);
            single.Groups[0].StandardDeviation.Should().Be(double.NaN);
            single.PooledStandardDeviation.Should().Be(double.NaN);
            single.MissingIgnored.Should().Be(1);
            Throws(() => table.SummariseByGroup([1.0, 2.0], ["a"]), "groups");
            Throws(() => table.SummariseByGroup([1.0, double.NaN], ["a", "b"], false), "values");
        }

        [Fact]
        [Trait("Covers", "SummariseCategories")]
        public void SummariseCategoriesProfilesLabels()
        {
            var r = table.SummariseCategories(["a", "a", "b", "c", null!]);
            r.Total.Should().Be(4);
            r.Missing.Should().Be(1);
            r.Distinct.Should().Be(3);
            r.Modes.Should().Equal("a");
            r.ModeFrequency.Should().Be(2);
            r.ShannonEntropyBits.Should().BeApproximately(1.5, Tight);
            r.NormalisedEntropy.Should().BeApproximately(1.5 / Math.Log(3, 2), Tight);
            r.GiniImpurity.Should().BeApproximately(0.625, Tight);
            r.HerfindahlIndex.Should().BeApproximately(0.375, Tight);
            r.SingletonCount.Should().Be(2);
            r.TopValues[0].Value.Should().Be("a");
            r.TopValues[2].CumulativeProportion.Should().BeApproximately(1.0, Tight);
            Throws(() => table.SummariseCategories([]), "values");
        }

        [Fact]
        [Trait("Covers", "TopShareConcentration")]
        public void TopShareConcentrationMatchesHandComputation()
        {
            var r = table.TopShareConcentration(oneToTen, [0.1, 0.2], [0.5, 1.0]);
            r.Shares[0].Should().BeApproximately(10.0 / 55.0, Tight);
            r.Shares[1].Should().BeApproximately(19.0 / 55.0, Tight);
            r.HoldersIncluded.Should().Equal(1, 2);
            r.HoldersNeeded.Should().Equal(4, 10);
            r.HolderFractionNeeded[0].Should().BeApproximately(0.4, Tight);
            r.Total.Should().Be(55.0);
            table.TopShareConcentration(oneToTen).Fractions.Should().Equal(0.01, 0.1, 0.2);
            Throws(() => table.TopShareConcentration([-1.0, 2.0]), "values");
            Throws(() => table.TopShareConcentration(oneToTen, [0.0]), "topFractions");
        }

        [Fact]
        [Trait("Covers", "ZScores")]
        public void ZScoresStandardiseTextbookExample()
        {
            var r = table.ZScores(standardDeviationTextbookExample, false);
            r.Values.Should().Equal(-1.5, -0.5, -0.5, -0.5, 0.0, 0.0, 1.0, 2.0);
            r.Centre.Should().BeApproximately(5.0, Tight);
            r.Scale.Should().BeApproximately(2.0, Tight);
            var gap = table.ZScores([1.0, double.NaN, 3.0]);
            gap.Values[1].Should().Be(double.NaN);
            gap.Values[0].Should().BeApproximately(-Math.Sqrt(0.5), Tight);
            gap.MissingCount.Should().Be(1);
            Throws(() => table.ZScores([4.0, 4.0, 4.0]), "values");
            Throws(() => table.ZScores([4.0]), "values");
        }

        [Fact]
        [Trait("Covers", "RobustZScores")]
        public void RobustZScoresUseMedianAndMad()
        {
            var r = table.RobustZScores(standardDeviationTextbookExample);
            r.Centre.Should().BeApproximately(4.5, Tight);
            r.Scale.Should().BeApproximately(0.5 * 1.482602218505602, Tight);
            r.Values[0].Should().BeApproximately(-2.5 / (0.5 * 1.482602218505602), Tight);

            var iqr = table.RobustZScores(standardDeviationTextbookExample,
                DescriptiveStandardisationScale.InterquartileRange);

            iqr.Scale.Should().BeApproximately(1.5 / 1.3489795003921634, Tight);

            var sd = table.RobustZScores(standardDeviationTextbookExample,
                DescriptiveStandardisationScale.StandardDeviation);

            sd.Centre.Should().BeApproximately(5.0, Tight);
            Throws(() => table.RobustZScores([3.0, 3.0, 3.0]), "values");
        }

        [Fact]
        [Trait("Covers", "CentreValues")]
        public void CentreValuesSubtractsMeanOrMedian()
        {
            table.CentreValues([1.0, 2.0, 6.0]).Values.Should().Equal(-2.0, -1.0, 3.0);

            table.CentreValues([1.0, 2.0, 6.0], DescriptiveCentre.Median).Values.Should()
                .Equal(-1.0, 0.0, 4.0);

            table.CentreValues([5.0]).Values.Should().Equal(0.0);
        }

        [Fact]
        [Trait("Covers", "TransformValues")]
        public void TransformValuesAppliesAndValidatesDomain()
        {
            table.TransformValues([1.0, Math.E], DescriptiveValueTransform.NaturalLog).Values[1].Should()
                .BeApproximately(1.0, Tight);

            table.TransformValues([100.0], DescriptiveValueTransform.Log10).Values[0].Should()
                .BeApproximately(2.0, Tight);
            table.TransformValues([8.0], DescriptiveValueTransform.Log2).Values[0].Should()
                .BeApproximately(3.0, Tight);

            table.TransformValues([0.0], DescriptiveValueTransform.Log1P).Values[0].Should().Be(0.0);
            table.TransformValues([9.0], DescriptiveValueTransform.SquareRoot).Values[0].Should().Be(3.0);

            table.TransformValues([-27.0], DescriptiveValueTransform.CubeRoot).Values[0].Should()
                .BeApproximately(-3.0, Tight);

            table.TransformValues([-3.0], DescriptiveValueTransform.Square).Values[0].Should().Be(9.0);
            table.TransformValues([4.0], DescriptiveValueTransform.Reciprocal).Values[0].Should().Be(0.25);
            table.TransformValues([-4.0], DescriptiveValueTransform.Absolute).Values[0].Should().Be(4.0);

            table.TransformValues([-4.0, 0.0, 2.0], DescriptiveValueTransform.Sign).Values.Should()
                .Equal(-1.0, 0.0, 1.0);

            table.TransformValues([4.0], DescriptiveValueTransform.Negate).Values[0].Should().Be(-4.0);

            table.TransformValues([1.0 - Math.E], DescriptiveValueTransform.SignedLog1P).Values[0].Should()
                .BeApproximately(-1.0, Tight);

            var gap = table.TransformValues([1.0, double.NaN], DescriptiveValueTransform.NaturalLog);
            gap.Values[1].Should().Be(double.NaN);
            gap.MissingCount.Should().Be(1);
            Throws(() => table.TransformValues([1.0, 0.0], DescriptiveValueTransform.NaturalLog), "values");
            Throws(() => table.TransformValues([0.0], DescriptiveValueTransform.Reciprocal), "values");
            Throws(() => table.TransformValues([-1.0], DescriptiveValueTransform.Log1P), "values");
            Throws(() => table.TransformValues([-1.0], DescriptiveValueTransform.SquareRoot), "values");
        }

        [Fact]
        [Trait("Covers", "ProportionsOfTotal")]
        public void ProportionsOfTotalSumToOne()
        {
            var r = table.ProportionsOfTotal([1.0, 3.0, double.NaN]);
            r.Values[0].Should().BeApproximately(0.25, Tight);
            r.Values[1].Should().BeApproximately(0.75, Tight);
            r.Scale.Should().Be(4.0);
            Throws(() => table.ProportionsOfTotal([-1.0, 1.0]), "values");
        }

        [Fact]
        [Trait("Covers", "WinsoriseValues")]
        public void WinsoriseValuesCapsAtPercentiles()
        {
            var x = Enumerable.Range(1, 11).Select(i => (double)i).ToArray();
            var r = table.WinsoriseValues(x, 10, 90);
            r.Values.Should().Equal(2.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0, 10.0, 10.0);
            r.Centre.Should().BeApproximately(2.0, Tight);
            r.Scale.Should().BeApproximately(10.0, Tight);
            Throws(() => table.WinsoriseValues(x, 90, 10), "upperPercent");
        }

        [Fact]
        [Trait("Covers", "FlagOutliersByZScore")]
        public void ZScoreOutliersFlagTheExtremeValue()
        {
            var r = table.FlagOutliersByZScore(outlierExample, 2.0);
            r.OutlierIndices.Should().Equal(10);
            r.OutlierValues.Should().Equal(100.0);
            r.IsOutlier[10].Should().BeTrue();
            r.IsOutlier[0].Should().BeFalse();
            var mean = 155.0 / 11.0;
            var sd = Math.Sqrt((10385.0 - 155.0 * 155.0 / 11.0) / 10.0);
            r.Scores[10].Should().BeApproximately((100.0 - mean) / sd, Tight);
            r.UpperBound.Should().BeApproximately(mean + 2 * sd, Tight);
            var constant = table.FlagOutliersByZScore([2.0, 2.0, 2.0]);
            constant.OutlierCount.Should().Be(0);
            var gap = table.FlagOutliersByZScore([1.0, double.NaN, 1.0, 1.0, 50.0], 1.0);
            gap.IsOutlier[1].Should().BeFalse();
            gap.MissingCount.Should().Be(1);
            Throws(() => table.FlagOutliersByZScore(outlierExample, 0), "threshold");
            Throws(() => table.FlagOutliersByZScore([1.0]), "values");
        }

        [Fact]
        [Trait("Covers", "FlagOutliersByIqrFence")]
        public void IqrFenceFlagsValuesBeyondTukeyFences()
        {
            var r = table.FlagOutliersByIqrFence(outlierExample);
            r.OutlierIndices.Should().Equal(10);
            r.UpperBound.Should().BeApproximately(16.0, Tight);
            r.LowerBound.Should().BeApproximately(3.5 - 7.5, Tight);
            r.Scores[10].Should().BeApproximately(18.3, Tight);
            r.Scores[3].Should().Be(0.0);
            table.FlagOutliersByIqrFence(outlierExample, 20).OutlierCount.Should().Be(0);
            table.FlagOutliersByIqrFence([5.0, 5.0, 5.0]).OutlierCount.Should().Be(0);
            Throws(() => table.FlagOutliersByIqrFence(outlierExample, -1), "fenceMultiplier");
        }

        [Fact]
        [Trait("Covers", "FlagOutliersByMedianAbsoluteDeviation")]
        public void MadOutliersUseModifiedZScore()
        {
            var r = table.FlagOutliersByMedianAbsoluteDeviation(outlierExample);
            r.OutlierIndices.Should().Equal(10);
            r.Centre.Should().BeApproximately(6.0, Tight);
            r.Scale.Should().BeApproximately(3.0 / 0.6745, Tight);
            r.Scores[10].Should().BeApproximately(0.6745 * 94.0 / 3.0, Tight);
            r.UpperBound.Should().BeApproximately(6.0 + 3.5 * 3.0 / 0.6745, Tight);
            var fallback = table.FlagOutliersByMedianAbsoluteDeviation([5.0, 5.0, 5.0, 5.0, 9.0]);
            fallback.OutlierIndices.Should().Equal(4);
            fallback.Scale.Should().BeApproximately(0.8 * 1.2533141373155001, Tight);
            table.FlagOutliersByMedianAbsoluteDeviation([3.0, 3.0, 3.0]).OutlierCount.Should().Be(0);
            Throws(() => table.FlagOutliersByMedianAbsoluteDeviation(outlierExample, 0), "threshold");
            Throws(() => table.FlagOutliersByMedianAbsoluteDeviation([]), "values");
        }

        [Fact]
        [Trait("Covers", "QuantilesByColumn")]
        public void QuantilesByColumnReturnsMatrixOfQuantiles()
        {
            var rows = oneToTen.Select((x, i) => new[] { x, i < 5 ? double.NaN : x * 10 }).ToArray();
            var r = table.QuantilesByColumn(rows, [0.5, 1.0], ["a", "b"]);
            r.ColumnNames.Should().Equal("a", "b");
            r.Values[0].Should().Equal(5.5, 10.0);
            r.Values[1][0].Should().BeApproximately(80.0, Tight);
            r.Values[1][1].Should().Be(100.0);
            r.Counts.Should().Equal(10, 5);
            var empty = table.QuantilesByColumn(Rows([double.NaN]), [0.5]);
            empty.Values[0][0].Should().Be(double.NaN);
            Throws(() => table.QuantilesByColumn(rows, []), "probabilities");
            Throws(() => table.QuantilesByColumn(rows, [1.5]), "probabilities");
        }
    }
}