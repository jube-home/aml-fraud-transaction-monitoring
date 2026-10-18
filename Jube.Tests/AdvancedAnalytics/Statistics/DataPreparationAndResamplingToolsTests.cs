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
using Jube.Service.Agent.AdvancedAnalyticsTools.DataPreparationAndResampling;
using Xunit;
using Jube.Analytics.Models;

namespace Jube.Test.AdvancedAnalytics.Statistics
{
    public class DataPreparationAndResamplingToolsTests
    {
        private static readonly EncodingTools encoding = new();
        private static readonly BinningTools binning = new();
        private static readonly FeatureEngineeringTools engineering = new();
        private static readonly FeatureSelectionTools selection = new();
        private static readonly int[] targetLabels = [1, 1, 1, 0, 1, 0, 0, 0];
        private static readonly double[] zeroToTen = [.. Enumerable.Range(0, 11).Select(i => (double)i)];

        private static string[][] Column(params string?[] values)
        {
            return [.. values.Select(v => new[] { v! })];
        }

        private static string[][] TargetTable()
        {
            return Column("A", "A", "A", "A", "B", "B", "B", "B");
        }

        [Fact]
        [Trait("Covers", "FitLabelEncoder")]
        public void FitLabelEncoderSortedCodesAndMissingIsMinusOne()
        {
            var result = encoding.FitLabelEncoder(Column("b", "a", "b", "", "c"));

            result.Categories[0].Should().Equal("a", "b", "c");
            result.Table.Select(r => r[0]).Should().Equal(1.0, 0.0, 1.0, -1.0, 2.0);
        }

        [Fact]
        [Trait("Covers", "FitLabelEncoder")]
        public void FitLabelEncoderFirstAppearanceAndFrequencyOrders()
        {
            var data = Column("b", "a", "b", "c", "c", "c");

            encoding.FitLabelEncoder(data, null, CategoryOrder.FirstAppearance).Categories[0]
                .Should().Equal("b", "a", "c");
            encoding.FitLabelEncoder(data, null, CategoryOrder.MostFrequentFirst).Categories[0]
                .Should().Equal("c", "b", "a");

            Action act = () => encoding.FitLabelEncoder([]);
            act.Should().Throw<ArgumentException>().WithParameterName("data");
        }

        [Fact]
        [Trait("Covers", "ApplyLabelEncoder")]
        public void ApplyLabelEncoderUsesGivenRankOrderAndUnknownCode()
        {
            var result = encoding.ApplyLabelEncoder(Column("High", "Low", "Medium", "Unseen", null),
                [["Low", "Medium", "High"]], -9);

            result.Select(r => r[0]).Should().Equal(2.0, 0.0, 1.0, -9.0, -9.0);
        }

        [Fact]
        [Trait("Covers", "FitOneHotEncoder")]
        public void FitOneHotEncoderProducesOneColumnPerCategory()
        {
            var result = encoding.FitOneHotEncoder(Column("a", "b", "a", ""), ["col"]);

            result.FeatureNames.Should().Equal("col=a", "col=b");
            result.Table[0].Should().Equal(1.0, 0.0);
            result.Table[1].Should().Equal(0.0, 1.0);
            result.Table[3].Should().Equal(0.0, 0.0);

            var dropped = encoding.FitOneHotEncoder(Column("a", "b", "c"), ["col"], true);
            dropped.FeatureNames.Should().Equal("col=b", "col=c");
            dropped.Table[0].Should().Equal(0.0, 0.0);
            dropped.Table[2].Should().Equal(0.0, 1.0);
        }

        [Fact]
        [Trait("Covers", "FitOneHotEncoder")]
        public void FitOneHotEncoderRefusesExplosiveCardinality()
        {
            var many = Column([.. Enumerable.Range(0, 30).Select(i => "v" + i)]);

            Action act = () => encoding.FitOneHotEncoder(many, null, false, 10);
            act.Should().Throw<ArgumentException>().WithParameterName("maximumCategoriesPerColumn");
        }

        [Fact]
        [Trait("Covers", "ApplyOneHotEncoder")]
        public void ApplyOneHotEncoderUnseenCategoryIsAllZeros()
        {
            var categories = new[] { new[] { "a", "b" } };

            var result = encoding.ApplyOneHotEncoder(Column("b", "zzz", null), categories);
            result[0].Should().Equal(0.0, 1.0);
            result[1].Should().Equal(0.0, 0.0);
            result[2].Should().Equal(0.0, 0.0);

            encoding.ApplyOneHotEncoder(Column("b", "a"), categories, true).Select(r => r.Single())
                .Should().Equal(1.0, 0.0);
        }

        [Fact]
        [Trait("Covers", "FitTargetMeanEncoder")]
        public void FitTargetMeanEncoderShrinksTowardsTheOverallRate()
        {
            var raw = encoding.FitTargetMeanEncoder(TargetTable(), targetLabels, null, 0);
            raw.Tables[0].Values.Should().Equal(0.75, 0.25);

            var smoothed = encoding.FitTargetMeanEncoder(TargetTable(), targetLabels, null, 4);
            smoothed.Tables[0].Categories.Should().Equal("A", "B");
            smoothed.Tables[0].Values[0].Should().BeApproximately((4 * 0.75 + 4 * 0.5) / 8, Tight);
            smoothed.Tables[0].Values[1].Should().BeApproximately((4 * 0.25 + 4 * 0.5) / 8, Tight);
            smoothed.Tables[0].DefaultValue.Should().Be(0.5);
            smoothed.Tables[0].Counts.Should().Equal(4, 4);
            smoothed.Tables[0].Events.Should().Equal(3, 1);
            smoothed.Table[0][0].Should().BeApproximately(0.625, Tight);
        }

        [Fact]
        [Trait("Covers", "FitTargetMeanEncoder")]
        public void FitTargetMeanEncoderRejectsBadLabelsAndNegativeSmoothing()
        {
            Action labels = () => encoding.FitTargetMeanEncoder(TargetTable(), [1, 2, 0, 0, 1, 1, 0, 0]);
            labels.Should().Throw<ArgumentException>().WithParameterName("labels");

            Action length = () => encoding.FitTargetMeanEncoder(TargetTable(), [1, 0]);
            length.Should().Throw<ArgumentException>().WithParameterName("labels");

            Action smoothing = () => encoding.FitTargetMeanEncoder(TargetTable(), targetLabels, null, -1);
            smoothing.Should().Throw<ArgumentException>().WithParameterName("smoothing");
        }

        [Fact]
        [Trait("Covers", "FitWeightOfEvidenceEncoder")]
        public void FitWeightOfEvidenceIsLogRatioOfEventToNonEventShares()
        {
            var result = encoding.FitWeightOfEvidenceEncoder(TargetTable(), targetLabels, null, 0);

            result.Tables[0].Values[0].Should().BeApproximately(Math.Log(3), Tight);
            result.Tables[0].Values[1].Should().BeApproximately(-Math.Log(3), Tight);
            result.Tables[0].InformationValue.Should().BeApproximately(Math.Log(3), Tight);
            result.Tables[0].DefaultValue.Should().Be(0);
        }

        [Fact]
        [Trait("Covers", "FitWeightOfEvidenceEncoder")]
        public void FitWeightOfEvidenceSmoothingKeepsEmptyCellsFiniteAndSingleClassIsRejected()
        {
            var data = Column("A", "A", "A", "B", "B", "B");
            var labels = new[] { 1, 1, 1, 0, 0, 0 };

            var smoothed = encoding.FitWeightOfEvidenceEncoder(data, labels);
            smoothed.Tables[0].Values.Should().OnlyContain(v => !double.IsInfinity(v) && !double.IsNaN(v));

            smoothed.Tables[0].Values[0].Should()
                .BeApproximately(Math.Log((3 + 0.5) / (3 + 1.0) / ((0 + 0.5) / (3 + 1.0))), Tight);

            Action act = () => encoding.FitWeightOfEvidenceEncoder(data, [1, 1, 1, 1, 1, 1]);
            act.Should().Throw<ArgumentException>().WithMessage("*only one class*");
        }

        [Fact]
        [Trait("Covers", "FitFrequencyEncoder")]
        public void FitFrequencyEncoderReturnsFractionsOrCounts()
        {
            var data = Column("a", "a", "a", "b");

            var fraction = encoding.FitFrequencyEncoder(data);
            fraction.Table.Select(r => r[0]).Should().Equal(0.75, 0.75, 0.75, 0.25);

            var counts = encoding.FitFrequencyEncoder(data, null, false);
            counts.Table.Select(r => r[0]).Should().Equal(3.0, 3.0, 3.0, 1.0);
            counts.Tables[0].DefaultValue.Should().Be(0);
        }

        [Fact]
        [Trait("Covers", "ApplyCategoryEncoder")]
        public void ApplyCategoryEncoderMapsKnownAndFallsBackToDefault()
        {
            var result = encoding.ApplyCategoryEncoder(Column("A", "B", "Z", null),
                [["A", "B"]], [[10.0, 20.0]], [-1.0]);

            result.Select(r => r[0]).Should().Equal(10.0, 20.0, -1.0, -1.0);

            Action act = () => encoding.ApplyCategoryEncoder(Column("A"), [["A", "B"]],
                [[10.0]], [0.0]);

            act.Should().Throw<ArgumentException>().WithParameterName("values");
        }

        [Fact]
        [Trait("Covers", "FitRareCategoryGrouping")]
        public void FitRareCategoryGroupingReplacesRareLevelsWithOther()
        {
            var data = Column("a", "a", "a", "b", "c");
            var result = encoding.FitRareCategoryGrouping(data, null, 2);

            result.KeptCategories[0].Should().Equal("a");
            result.CategoriesGroupedPerColumn.Should().Equal(2);
            result.Table.Select(r => r[0]).Should().Equal("a", "a", "a", "other", "other");

            var byFraction = encoding.FitRareCategoryGrouping(data, null, 0, 0.3);
            byFraction.KeptCategories[0].Should().Equal("a");

            Action act = () => encoding.FitRareCategoryGrouping(data, null, -1);
            act.Should().Throw<ArgumentException>().WithParameterName("minimumCount");
        }

        [Fact]
        [Trait("Covers", "ApplyRareCategoryGrouping")]
        public void ApplyRareCategoryGroupingMapsUnseenToOther()
        {
            var result = encoding.ApplyRareCategoryGrouping(Column("a", "x", null), [["a"]], "rest");

            result.Select(r => r[0]).Should().Equal("a", "rest", "rest");
        }

        [Fact]
        [Trait("Covers", "DiscretiseEqualWidth")]
        public void DiscretiseEqualWidthCutsRangeIntoEqualIntervals()
        {
            var result = binning.DiscretiseEqualWidth(zeroToTen, 5);

            result.Edges.Should().Equal(2.0, 4.0, 6.0, 8.0);
            result.Assignments.Should().Equal(0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4);
            result.Table.Select(r => r.Count).Should().Equal(3, 2, 2, 2, 2);
            result.Table[0].Lower.Should().BeNull();
            result.Table[0].Upper.Should().Be(2.0);
            result.Table[4].Upper.Should().BeNull();
            result.Table[0].Fraction.Should().BeApproximately(3.0 / 11.0, Tight);
        }

        [Fact]
        [Trait("Covers", "DiscretiseEqualWidth")]
        public void DiscretiseEqualWidthWithLabelsAddsEventStatisticsAndHandlesMissingAndConstant()
        {
            var labels = zeroToTen.Select(v => v >= 6 ? 1 : 0).ToArray();
            var result = binning.DiscretiseEqualWidth(zeroToTen, 5, labels);

            result.Table.Select(r => r.Events).Should().Equal(0, 0, 1, 2, 2);
            result.Table[3].EventRate.Should().Be(1.0);
            result.Table[4].WeightOfEvidence.Should().BeGreaterThan(0);
            result.Table[0].WeightOfEvidence.Should().BeLessThan(0);
            result.InformationValue.Should().BeGreaterThan(0);

            var withMissing = binning.DiscretiseEqualWidth([1.0, Nan, 3.0], 2);
            withMissing.Assignments[1].Should().Be(-1);
            withMissing.MissingCount.Should().Be(1);

            var constant = binning.DiscretiseEqualWidth([3.0, 3.0, 3.0], 4);
            constant.Edges.Should().BeEmpty();
            constant.Table.Should().ContainSingle();

            Action allMissing = () => binning.DiscretiseEqualWidth([Nan, Nan], 3);
            allMissing.Should().Throw<ArgumentException>().WithParameterName("values");

            Action bins = () => binning.DiscretiseEqualWidth(zeroToTen, 0);
            bins.Should().Throw<ArgumentException>().WithParameterName("bins");

            Action length = () => binning.DiscretiseEqualWidth(zeroToTen, 3, [0, 1]);
            length.Should().Throw<ArgumentException>().WithParameterName("labels");
        }

        [Fact]
        [Trait("Covers", "DiscretiseEqualFrequency")]
        public void DiscretiseEqualFrequencyCutsAtQuantilesGivingEqualCounts()
        {
            var values = Enumerable.Range(1, 8).Select(i => (double)i).ToArray();
            var result = binning.DiscretiseEqualFrequency(values, 4);

            result.Edges.Should().HaveCount(3);
            result.Edges[0].Should().BeApproximately(2.75, Tight);
            result.Edges[1].Should().BeApproximately(4.5, Tight);
            result.Edges[2].Should().BeApproximately(6.25, Tight);
            result.Table.Select(r => r.Count).Should().Equal(2, 2, 2, 2);
        }

        [Fact]
        [Trait("Covers", "DiscretiseEqualFrequency")]
        public void DiscretiseEqualFrequencyMergesTiedCutPoints()
        {
            var result = binning.DiscretiseEqualFrequency([1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 9.0], 4);

            result.Table.Length.Should().BeLessThan(4);
            result.Table.Sum(r => r.Count).Should().Be(8);

            binning.DiscretiseEqualFrequency([2.0, 2.0, 2.0], 3).Table.Should().ContainSingle();
        }

        [Fact]
        [Trait("Covers", "DiscretiseKMeans")]
        public void DiscretiseKMeansSeparatesThreeClumps()
        {
            var values = new[] { 1.0, 1.1, 0.9, 10.0, 10.1, 9.9, 20.0, 20.1, 19.9 };
            var result = binning.DiscretiseKMeans(values, 3);

            result.Edges.Should().HaveCount(2);
            result.Edges[0].Should().BeApproximately(5.5, 1e-6);
            result.Edges[1].Should().BeApproximately(15.0, 1e-6);
            result.Table.Select(r => r.Count).Should().Equal(3, 3, 3);
        }

        [Fact]
        [Trait("Covers", "DiscretiseKMeans")]
        public void DiscretiseKMeansWithFewDistinctValuesReducesBins()
        {
            var result = binning.DiscretiseKMeans([1.0, 1.0, 2.0, 2.0]);

            result.Table.Length.Should().BeLessOrEqualTo(2);
            binning.DiscretiseKMeans([4.0, 4.0], 3).Table.Should().ContainSingle();
        }

        [Fact]
        [Trait("Covers", "DiscretiseSupervised")]
        public void DiscretiseSupervisedFindsTheCutWhereTheEventRateJumps()
        {
            var values = Enumerable.Range(1, 20).Select(i => (double)i).ToArray();
            var labels = values.Select(v => v > 10 ? 1 : 0).ToArray();
            var result = binning.DiscretiseSupervised(values, labels, 2, 0.1);

            result.Edges.Should().ContainSingle().Which.Should().BeInRange(10.0, 11.0);
            result.Table.Select(r => r.Events).Should().Equal(0, 10);
            result.Table.Select(r => r.Count).Should().Equal(10, 10);
        }

        [Fact]
        [Trait("Covers", "DiscretiseSupervised")]
        public void DiscretiseSupervisedRejectsSingleClassAndTinyBinCount()
        {
            var values = Enumerable.Range(1, 10).Select(i => (double)i).ToArray();

            Action single = () => binning.DiscretiseSupervised(values, new int[10]);
            single.Should().Throw<ArgumentException>().WithParameterName("labels");

            Action bins = () => binning.DiscretiseSupervised(values, [.. values.Select(v => v > 5 ? 1 : 0)], 1);
            bins.Should().Throw<ArgumentException>().WithParameterName("maximumBins");
        }

        [Fact]
        [Trait("Covers", "DiscretiseCustomEdges")]
        public void DiscretiseCustomEdgesPutsAnEdgeValueInTheLowerBin()
        {
            var result = binning.DiscretiseCustomEdges([500.0, 1000.0, 1001.0, 5000.0, 20000.0],
                [1000.0, 5000.0]);

            result.Assignments.Should().Equal(0, 0, 1, 1, 2);
            result.Table.Select(r => r.Count).Should().Equal(2, 2, 1);

            Action unsorted = () => binning.DiscretiseCustomEdges([1.0], [5.0, 5.0]);
            unsorted.Should().Throw<ArgumentException>().WithParameterName("edges");

            binning.DiscretiseCustomEdges([1.0, 2.0], []).Table.Should().ContainSingle();
        }

        [Fact]
        [Trait("Covers", "ApplyBinEdges")]
        public void ApplyBinEdgesAssignsBeyondRangeAndMissing()
        {
            binning.ApplyBinEdges([-100.0, 2.0, 2.5, 100.0, Nan], [2.0, 4.0])
                .Should().Equal(0, 0, 1, 2, -1);

            binning.ApplyBinEdges([], [1.0]).Should().BeEmpty();
        }

        [Fact]
        [Trait("Covers", "GeneratePolynomialFeatures")]
        public void GeneratePolynomialFeaturesDegreeTwoHasPowersAndProduct()
        {
            var result = engineering.GeneratePolynomialFeatures([[2.0, 3.0]], ["a", "b"]);

            result.ColumnNames.Should().Equal("a", "b", "a^2", "a*b", "b^2");
            result.Table[0].Should().Equal(2.0, 3.0, 4.0, 6.0, 9.0);

            var interaction = engineering.GeneratePolynomialFeatures([[2.0, 3.0]], ["a", "b"],
                2, true, true);

            interaction.ColumnNames.Should().Equal("bias", "a", "b", "a*b");
            interaction.Table[0].Should().Equal(1.0, 2.0, 3.0, 6.0);
        }

        [Fact]
        [Trait("Covers", "GeneratePolynomialFeatures")]
        public void GeneratePolynomialFeaturesDegreeThreeCountsAndLimit()
        {
            var three = engineering.GeneratePolynomialFeatures([[1.0, 2.0, 3.0]], null, 3);
            three.ColumnNames.Length.Should().Be(19);

            Action limit = () => engineering.GeneratePolynomialFeatures([[1.0, 2.0, 3.0]], null, 3,
                false,
                false, 10);

            limit.Should().Throw<ArgumentException>().WithParameterName("maximumColumns");

            var withMissingValue =
                engineering.GeneratePolynomialFeatures([[Nan, 3.0]], ["a", "b"]);

            double.IsNaN(withMissingValue.Table[0][3]).Should().BeTrue();
            withMissingValue.Table[0][4].Should().Be(9.0);

            Action degree = () => engineering.GeneratePolynomialFeatures([[1.0]], null, 6);
            degree.Should().Throw<ArgumentException>().WithParameterName("degree");
        }

        [Fact]
        [Trait("Covers", "GenerateColumnPairFeatures")]
        public void GenerateColumnPairFeaturesRatioDifferenceAndUndefinedCounts()
        {
            var data = new[] { new[] { 6.0, 3.0 }, new[] { 5.0, 0.0 }, new[] { Nan, 1.0 } };
            var names = new[] { "amount", "balance" };

            var ratio = engineering.GenerateColumnPairFeatures(data, names, ["amount"], ["balance"]);
            ratio.ColumnNames.Should().Equal("amount/balance");
            ratio.Table[0][0].Should().Be(2.0);
            double.IsNaN(ratio.Table[1][0]).Should().BeTrue();
            ratio.UndefinedCells.Should().Equal(2);

            var diff = engineering.GenerateColumnPairFeatures(data, names, ["amount", "balance"],
                ["balance", "amount"], PairOperation.Difference);
            diff.Table[0].Should().Equal(3.0, -3.0);

            engineering.GenerateColumnPairFeatures(data, names, ["amount"], ["balance"],
                PairOperation.Product).Table[0][0].Should().Be(18.0);
            engineering.GenerateColumnPairFeatures(data, names, ["amount"], ["balance"],
                PairOperation.Maximum).Table[0][0].Should().Be(6.0);
            engineering.GenerateColumnPairFeatures(data, names, ["amount"], ["balance"],
                PairOperation.AbsoluteDifference).Table[0][0].Should().Be(3.0);

            Action unknown = () =>
                engineering.GenerateColumnPairFeatures(data, names, ["nope"], ["balance"]);

            unknown.Should().Throw<ArgumentException>().WithMessage("*amount, balance*");

            Action unequal = () =>
                engineering.GenerateColumnPairFeatures(data, names, ["amount"], ["balance", "amount"]);

            unequal.Should().Throw<ArgumentException>().WithParameterName("firstColumns");
        }

        [Fact]
        [Trait("Covers", "GenerateLagFeatures")]
        public void GenerateLagFeaturesShiftsAndDifferences()
        {
            var result = engineering.GenerateLagFeatures([10.0, 20.0, 30.0, 40.0], [1, 2]);

            result.ColumnNames.Should().Equal("lag_1", "lag_2", "diff_1", "diff_2");
            double.IsNaN(result.Table[0][0]).Should().BeTrue();
            result.Table[1][0].Should().Be(10.0);
            result.Table[3][0].Should().Be(30.0);
            double.IsNaN(result.Table[1][1]).Should().BeTrue();
            result.Table[3][1].Should().Be(20.0);
            result.Table[2][2].Should().Be(10.0);
            result.Table[3][3].Should().Be(20.0);
        }

        [Fact]
        [Trait("Covers", "GenerateLagFeatures")]
        public void GenerateLagFeaturesWithinGroupsAndRollingWindowExcludesCurrentRow()
        {
            var grouped = engineering.GenerateLagFeatures([10.0, 20.0, 30.0, 40.0], [1],
                ["x", "x", "y", "y"], false);

            double.IsNaN(grouped.Table[2][0]).Should().BeTrue();
            grouped.Table[1][0].Should().Be(10.0);
            grouped.Table[3][0].Should().Be(30.0);

            var rolling =
                engineering.GenerateLagFeatures([10.0, 20.0, 30.0, 40.0], [1], null, false, 2);

            rolling.ColumnNames.Should().Equal("lag_1", "rollMean_2", "rollStd_2", "rollMin_2", "rollMax_2");
            double.IsNaN(rolling.Table[0][1]).Should().BeTrue();
            rolling.Table[3][1].Should().Be(25.0);
            rolling.Table[3][2].Should().BeApproximately(Math.Sqrt(50.0), Tight);
            rolling.Table[3][3].Should().Be(20.0);
            rolling.Table[3][4].Should().Be(30.0);

            Action act = () => engineering.GenerateLagFeatures([1.0], [0]);
            act.Should().Throw<ArgumentException>().WithParameterName("lags");
        }

        [Fact]
        [Trait("Covers", "ExtractDateTimeParts")]
        public void ExtractDateTimePartsForAKnownDate()
        {
            var result = engineering.ExtractDateTimeParts(["2024-03-15T14:30:00Z"]);

            double Part(string name)
            {
                return result.Table[0][Array.IndexOf(result.ColumnNames, name)];
            }

            Part("year").Should().Be(2024);
            Part("quarter").Should().Be(1);
            Part("month").Should().Be(3);
            Part("day").Should().Be(15);
            Part("dayOfWeek").Should().Be(5);
            Part("dayOfYear").Should().Be(75);
            Part("isoWeek").Should().Be(11);
            Part("hour").Should().Be(14);
            Part("minute").Should().Be(30);
            Part("second").Should().Be(0);
            Part("secondsOfDay").Should().Be(52200);
            Part("isWeekend").Should().Be(0);
            Part("isMonthStart").Should().Be(0);
            Part("isMonthEnd").Should().Be(0);
        }

        [Fact]
        [Trait("Covers", "ExtractDateTimeParts")]
        public void ExtractDateTimePartsOffsetWeekendMonthEndAndUnparsed()
        {
            var shifted = engineering.ExtractDateTimeParts(["2024-03-15T23:30:00Z"], 60,
                ["day", "hour"]);
            shifted.ColumnNames.Should().Equal("day", "hour");
            shifted.Table[0].Should().Equal(16.0, 0.0);

            var end = engineering.ExtractDateTimeParts(["2024-03-31", "garbage", ""], 0,
                ["dayOfWeek", "isWeekend", "isMonthEnd", "isMonthStart"]);

            end.Table[0].Should().Equal(7.0, 1.0, 1.0, 0.0);
            double.IsNaN(end.Table[1][0]).Should().BeTrue();
            end.UnparsedRows.Should().Be(2);

            Action unknown = () => engineering.ExtractDateTimeParts(["2024-03-31"], 0, ["fortnight"]);
            unknown.Should().Throw<ArgumentException>().WithParameterName("parts");

            Action offset = () => engineering.ExtractDateTimeParts(["2024-03-31"], 5000);
            offset.Should().Throw<ArgumentException>().WithParameterName("utcOffsetMinutes");
        }

        [Fact]
        [Trait("Covers", "ExtractEpochParts")]
        public void ExtractEpochPartsSecondsMillisecondsAndInvalid()
        {
            var seconds = engineering.ExtractEpochParts([1710513000.0], EpochUnit.Seconds, 0,
                ["year", "month", "day", "hour", "minute", "dayOfWeek"]);

            seconds.Table[0].Should().Equal(2024.0, 3.0, 15.0, 14.0, 30.0, 5.0);

            var millis = engineering.ExtractEpochParts([1710513000000.0, Nan], EpochUnit.Milliseconds, 0,
                ["hour"]);
            millis.Table[0][0].Should().Be(14.0);
            double.IsNaN(millis.Table[1][0]).Should().BeTrue();
            millis.UnparsedRows.Should().Be(1);

            engineering.ExtractEpochParts([0.0], EpochUnit.Seconds, 0, ["year"]).Table[0][0]
                .Should().Be(1970.0);

            engineering.ExtractEpochParts([1e30], EpochUnit.Seconds, 0, ["year"]).UnparsedRows
                .Should().Be(1);
        }

        [Fact]
        [Trait("Covers", "ComputeElapsedTime")]
        public void ComputeElapsedTimeInDifferentUnits()
        {
            var from = new[] { "2024-01-01T00:00:00Z", "bad" };
            var to = new[] { "2024-01-03T12:00:00Z", "2024-01-03T12:00:00Z" };

            var days = engineering.ComputeElapsedTime(from, to);
            days[0].Should().BeApproximately(2.5, Tight);
            double.IsNaN(days[1]).Should().BeTrue();

            engineering.ComputeElapsedTime(from, to, ElapsedUnit.Hours)[0].Should().BeApproximately(60.0, Tight);
            engineering.ComputeElapsedTime(to, from, ElapsedUnit.Minutes)[0].Should().BeApproximately(-3600.0, Tight);
            engineering.ComputeElapsedTime(from, to, ElapsedUnit.Seconds)[0].Should().BeApproximately(216000.0, Tight);

            Action act = () => engineering.ComputeElapsedTime(from, ["2024-01-03"]);
            act.Should().Throw<ArgumentException>().WithParameterName("toIsoDateTimes");
        }

        [Fact]
        [Trait("Covers", "EncodeCyclicalFeature")]
        public void EncodeCyclicalFeatureMakesTheEndsOfTheCycleNeighbours()
        {
            var result = engineering.EncodeCyclicalFeature([0.0, 6.0, 12.0, 24.0, Nan]);

            result.ColumnNames.Should().Equal("sin", "cos");
            result.Table[0][0].Should().BeApproximately(0.0, Tight);
            result.Table[0][1].Should().BeApproximately(1.0, Tight);
            result.Table[1][0].Should().BeApproximately(1.0, Tight);
            result.Table[1][1].Should().BeApproximately(0.0, 1e-12);
            result.Table[2][1].Should().BeApproximately(-1.0, Tight);
            result.Table[3][0].Should().BeApproximately(0.0, 1e-12);
            double.IsNaN(result.Table[4][0]).Should().BeTrue();

            Action act = () => engineering.EncodeCyclicalFeature([1.0], 0);
            act.Should().Throw<ArgumentException>().WithParameterName("period");
        }

        [Fact]
        [Trait("Covers", "SelectTableColumns")]
        public void SelectTableColumnsKeepsAndReordersByName()
        {
            var result = engineering.SelectTableColumns([[1.0, 2.0, 3.0]], ["a", "b", "c"],
                ["c", "a"]);

            result.ColumnNames.Should().Equal("c", "a");
            result.Table[0].Should().Equal(3.0, 1.0);

            Action act = () => engineering.SelectTableColumns([[1.0]], ["a"], ["z"]);
            act.Should().Throw<ArgumentException>().WithParameterName("keep");
        }

        [Fact]
        [Trait("Covers", "SelectByVarianceThreshold")]
        public void SelectByVarianceThresholdDropsConstantAndLowVarianceColumns()
        {
            var table = new[]
            {
                new[] { 5.0, 1.0, 10.0, Nan },
                new[] { 5.0, 2.0, 20.0, 1.0 },
                new[] { 5.0, 3.0, 30.0, Nan }
            };

            var names = new[] { "const", "small", "large", "sparse" };

            var zero = selection.SelectByVarianceThreshold(table, names);
            zero.Variances[0].Should().Be(0);
            zero.Variances[1].Should().BeApproximately(1.0, Tight);
            zero.Variances[2].Should().BeApproximately(100.0, Tight);
            double.IsNaN(zero.Variances[3]).Should().BeTrue();
            zero.Kept.Should().Equal("small", "large");
            zero.Dropped.Should().Equal("const", "sparse");
            zero.FiniteCounts.Should().Equal(3, 3, 3, 1);

            selection.SelectByVarianceThreshold(table, names, 1.0).Kept.Should().Equal("large");

            Action act = () => selection.SelectByVarianceThreshold(table, names, -1);
            act.Should().Throw<ArgumentException>().WithParameterName("threshold");
        }

        [Fact]
        [Trait("Covers", "FilterCorrelatedFeatures")]
        public void FilterCorrelatedFeaturesDropsOneOfAPerfectlyCorrelatedPair()
        {
            var a = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
            var c = new[] { 2.0, 1.0, 4.0, 3.0, 5.0 };
            var table = a.Select((v, i) => new[] { v, 2 * v, c[i] }).ToArray();

            var result = selection.FilterCorrelatedFeatures(table, ["a", "b", "c"]);

            result.Pairs.Should().ContainSingle();
            result.Pairs[0].Correlation.Should().BeApproximately(1.0, 1e-12);
            result.Dropped.Should().ContainSingle();
            new[] { "a", "b" }.Should().Contain(result.Dropped[0].Name);
            result.Kept.Should().Contain("c").And.HaveCount(2);

            selection.FilterCorrelatedFeatures(table, ["a", "b", "c"], 0.7).Pairs.Should().HaveCount(3);
        }

        [Fact]
        [Trait("Covers", "FilterCorrelatedFeatures")]
        public void FilterCorrelatedFeaturesKeepsTheMemberMoreCorrelatedWithTheTarget()
        {
            var a = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
            var d = new[] { 1.0, 2.0, 3.0, 4.0, 5.5 };
            var table = a.Select((v, i) => new[] { v, d[i] }).ToArray();
            var names = new[] { "a", "d" };

            selection.FilterCorrelatedFeatures(table, names).Kept.Should().Equal("d");

            selection.FilterCorrelatedFeatures(table, names, 0.9, CorrelationKind.Pearson, a).Kept
                .Should().Equal("a");

            var spearman = selection.FilterCorrelatedFeatures(table, names, 0.9, CorrelationKind.Spearman);
            spearman.Pairs[0].Correlation.Should().BeApproximately(1.0, 1e-12);

            Action act = () =>
                selection.FilterCorrelatedFeatures(table, names, 0.9, CorrelationKind.Pearson, [1.0]);

            act.Should().Throw<ArgumentException>().WithParameterName("target");
        }

        [Fact]
        [Trait("Covers", "RankFeaturesByAnovaF")]
        public void RankFeaturesByAnovaFMatchesHandComputedTwoClassAnova()
        {
            var data = new[]
            {
                new[] { 1.0, 1.0 }, new[] { 2.0, 3.0 }, new[] { 3.0, 2.0 },
                new[] { 4.0, 2.0 }, new[] { 5.0, 1.0 }, new[] { 6.0, 3.0 }
            };

            var labels = new[] { 0, 0, 0, 1, 1, 1 };

            var result = selection.RankFeaturesByAnovaF(data, labels, ["signal", "noise"]);

            result.Features[0].Name.Should().Be("signal");
            result.Features[0].Rank.Should().Be(1);
            result.Features[0].Score.Should().BeApproximately(13.5, 1e-9);
            result.Features[0].PValue.Should().BeApproximately(0.02131164112875661, 1e-6);
            result.Features[1].Name.Should().Be("noise");
            result.Features[1].Score.Should().BeApproximately(0.0, 1e-9);
            result.Features[0].AdjustedPValue.Should().BeApproximately(2 * result.Features[0].PValue, 1e-9);
        }

        [Fact]
        [Trait("Covers", "RankFeaturesByAnovaF")]
        public void RankFeaturesByAnovaFRejectsSingleClassAndRanksUnusableFeatureLast()
        {
            Action act = () => selection.RankFeaturesByAnovaF([[1.0], [2.0]], [1, 1]);
            act.Should().Throw<ArgumentException>().WithParameterName("labels");

            var data = new[] { new[] { 1.0, Nan }, new[] { 2.0, Nan }, new[] { 9.0, Nan }, new[] { 10.0, Nan } };
            var result = selection.RankFeaturesByAnovaF(data, [0, 0, 1, 1]);
            result.Features.Last().Name.Should().Be("column_1");
            double.IsNaN(result.Features.Last().Score).Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "RankFeaturesByFRegression")]
        public void RankFeaturesByFRegressionMatchesTextbookSimpleRegression()
        {
            var x = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
            var y = new[] { 2.0, 4.0, 5.0, 4.0, 5.0 };
            var data = x.Select(v => new[] { v, 0.0 }).ToArray();
            data[0][1] = 1;
            data[1][1] = 3;
            data[2][1] = 2;
            data[3][1] = 3;
            data[4][1] = 1;

            var result = selection.RankFeaturesByFRegression(data, y, ["x", "wobble"]);

            result.Features[0].Name.Should().Be("x");
            result.Features[0].Score.Should().BeApproximately(4.5, 1e-9);
            result.Features[0].PValue.Should().BeApproximately(0.12402706265755459, 1e-6);
        }

        [Fact]
        [Trait("Covers", "RankFeaturesByFRegression")]
        public void RankFeaturesByFRegressionConstantFeatureIsNaN()
        {
            var data = new[] { new[] { 1.0, 5.0 }, new[] { 2.0, 5.0 }, new[] { 3.0, 5.0 }, new[] { 4.0, 5.0 } };
            var result = selection.RankFeaturesByFRegression(data, [1.0, 2.0, 3.0, 5.0]);

            double.IsNaN(result.Features.Last().Score).Should().BeTrue();
            result.Features.Last().Name.Should().Be("column_1");

            Action act = () => selection.RankFeaturesByFRegression(data, [1.0]);
            act.Should().Throw<ArgumentException>().WithParameterName("target");
        }

        [Fact]
        [Trait("Covers", "RankCountFeaturesByChiSquare")]
        public void RankCountFeaturesByChiSquareMatchesHandComputedStatistic()
        {
            var data = new[] { new[] { 1.0, 2.0 }, new[] { 1.0, 2.0 }, new[] { 3.0, 2.0 }, new[] { 3.0, 2.0 } };
            var result = selection.RankCountFeaturesByChiSquare(data, [0, 0, 1, 1], ["skewed", "even"]);

            result.Features[0].Name.Should().Be("skewed");
            result.Features[0].Score.Should().BeApproximately(2.0, 1e-12);
            result.Features[0].PValue.Should().BeApproximately(0.15729920705028513, 1e-6);
            result.Features[1].Score.Should().BeApproximately(0.0, 1e-12);
            result.Features[1].PValue.Should().BeApproximately(1.0, 1e-9);
        }

        [Fact]
        [Trait("Covers", "RankCountFeaturesByChiSquare")]
        public void RankCountFeaturesByChiSquareRejectsNegativeValuesAndZeroColumns()
        {
            Action negative = () =>
                selection.RankCountFeaturesByChiSquare([[-1.0], [2.0]], [0, 1]);

            negative.Should().Throw<ArgumentException>().WithParameterName("data");

            var zeros = selection.RankCountFeaturesByChiSquare([[0.0], [0.0]], [0, 1]);
            double.IsNaN(zeros.Features[0].Score).Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "RankCategoricalFeaturesByChiSquare")]
        public void RankCategoricalFeaturesByChiSquareMatchesTwoByTwoHandComputation()
        {
            var rows = Enumerable.Repeat(("X", 0), 20).Concat(Enumerable.Repeat(("X", 1), 10))
                .Concat(Enumerable.Repeat(("Y", 0), 10)).Concat(Enumerable.Repeat(("Y", 1), 20)).ToArray();

            var data = rows.Select(r => new[] { r.Item1, "same" }).ToArray();
            var labels = rows.Select(r => r.Item2).ToArray();

            var result = selection.RankCategoricalFeaturesByChiSquare(data, labels, ["feature", "constant"]);

            result.Features[0].Name.Should().Be("feature");
            result.Features[0].Score.Should().BeApproximately(20.0 / 3.0, 1e-9);
            result.Features[0].DegreesOfFreedom.Should().Be(1);
            result.Features[0].PValue.Should().BeApproximately(0.009823274507519244, 1e-6);
            result.Features[0].EffectSize.Should().BeApproximately(1.0 / 3.0, 1e-9);
            double.IsNaN(result.Features[1].Score).Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "RankCategoricalFeaturesByChiSquare")]
        public void RankCategoricalFeaturesByChiSquareRejectsSingleClass()
        {
            Action act = () => selection.RankCategoricalFeaturesByChiSquare(Column("a", "b"), [1, 1]);
            act.Should().Throw<ArgumentException>().WithParameterName("labels");
        }

        [Fact]
        [Trait("Covers", "RankFeaturesByMutualInformation")]
        public void RankFeaturesByMutualInformationPerfectSeparatorIsLnTwo()
        {
            var labels = new[] { 0, 0, 0, 0, 1, 1, 1, 1 };
            var data = Enumerable.Range(1, 8).Select(i => new[] { i, i % 2 + 1.0 }).ToArray();

            var result =
                selection.RankFeaturesByMutualInformation(data, labels, ["separator", "alternating"], 2);

            result.Features[0].Name.Should().Be("separator");
            result.Features[0].Score.Should().BeApproximately(Math.Log(2), 1e-9);
            result.Features[0].EffectSize.Should().BeApproximately(1.0, 1e-9);
            result.Features[1].Score.Should().BeApproximately(0.0, 1e-9);
        }

        [Fact]
        [Trait("Covers", "RankFeaturesByMutualInformation")]
        public void RankFeaturesByMutualInformationCapturesInformativeMissingness()
        {
            var labels = new[] { 0, 0, 0, 0, 1, 1, 1, 1 };

            var data = new[]
            {
                new[] { 1.0 }, new[] { 2.0 }, new[] { 3.0 }, new[] { 4.0 },
                new[] { Nan }, new[] { Nan }, new[] { Nan }, new[] { Nan }
            };

            selection.RankFeaturesByMutualInformation(data, labels, null, 2).Features[0].Score
                .Should().BeApproximately(Math.Log(2), 1e-9);

            Action act = () => selection.RankFeaturesByMutualInformation(data, labels, null, 1);
            act.Should().Throw<ArgumentException>().WithParameterName("bins");
        }

        [Fact]
        [Trait("Covers", "EliminateFeaturesRecursively")]
        public void EliminateFeaturesRecursivelyKeepsTheOnlyRealPredictor()
        {
            var x1 = new[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0 };
            var x2 = new[] { 2.0, 1.0, 4.0, 3.0, 6.0, 5.0, 8.0, 7.0 };
            var x3 = new[] { 1.0, -1.0, 1.0, -1.0, 1.0, -1.0, 1.0, -1.0 };
            var data = x1.Select((v, i) => new[] { v, x2[i], x3[i] }).ToArray();
            var y = x1.Select(v => 5 * v).ToArray();

            var result = selection.EliminateFeaturesRecursively(data, y, 1, ["real", "shadow", "flip"]);

            result.Selected.Should().Equal("real");
            result.Features.Single(f => f.Name == "real").Rank.Should().Be(1);
            result.Features.Where(f => f.Name != "real").Select(f => f.Rank).OrderBy(r => r).Should().Equal(2, 3);

            result.Features.Single(f => f.Name == "real").StandardisedCoefficient
                .Should().BeApproximately(5 * Math.Sqrt(x1.Sum(v => (v - 4.5) * (v - 4.5)) / 7), 1e-6);
        }

        [Fact]
        [Trait("Covers", "EliminateFeaturesRecursively")]
        public void EliminateFeaturesRecursivelyValidatesArguments()
        {
            var data = Enumerable.Range(0, 10).Select(i => new[] { (double)i, i * i % 7 }).ToArray();
            var y = Enumerable.Range(0, 10).Select(i => (double)i).ToArray();

            Action keep = () => selection.EliminateFeaturesRecursively(data, y, 2);
            keep.Should().Throw<ArgumentException>().WithParameterName("featuresToKeep");

            Action few = () => selection.EliminateFeaturesRecursively([.. data.Take(3)], [.. y.Take(3)], 1);
            few.Should().Throw<ArgumentException>().WithMessage("*too few*");
        }

        private const double Nan = double.NaN;
        private const double Tight = 1e-9;
        private static readonly MissingValueTools missing = new();
        private static readonly ScalingTools scaling = new();

        private static readonly double[][] gappyTable =
        [
            [1.0, Nan],
            [2.0, 3.0],
            [Nan, Nan],
            [4.0, 5.0]
        ];

        [Fact]
        [Trait("Covers", "ProfileMissingValues")]
        public void ProfileMissingValuesCountsCellsRowsAndPatterns()
        {
            var profile = missing.ProfileMissingValues(gappyTable, ["a", "b"]);

            profile.Rows.Should().Be(4);
            profile.Columns.Should().Be(2);
            profile.TotalMissing.Should().Be(3);
            profile.MissingFraction.Should().BeApproximately(3.0 / 8.0, Tight);
            profile.CompleteRows.Should().Be(2);
            profile.ColumnProfiles[0].MissingCount.Should().Be(1);
            profile.ColumnProfiles[1].MissingFraction.Should().BeApproximately(0.5, Tight);
            profile.Patterns[0].MissingColumns.Should().BeEmpty();
            profile.Patterns[0].Rows.Should().Be(2);
            profile.Patterns.Select(p => string.Join("|", p.MissingColumns)).Should().Contain(["a|b", "b"]);
            profile.Warnings.Should().BeEmpty();
        }

        [Fact]
        [Trait("Covers", "ProfileMissingValues")]
        public void ProfileMissingValuesWarnsAboutEntirelyMissingColumnAndRejectsBadInput()
        {
            var profile = missing.ProfileMissingValues([[1.0, Nan], [2.0, double.PositiveInfinity]]);

            profile.Warnings.Should().ContainSingle(w => w.Contains("column_1") && w.Contains("entirely missing"));

            Action empty = () => missing.ProfileMissingValues([]);
            empty.Should().Throw<ArgumentException>().WithParameterName("data");

            Action ragged = () => missing.ProfileMissingValues([[1.0], [1.0, 2.0]]);
            ragged.Should().Throw<ArgumentException>().WithMessage("*Row 1*");
        }

        [Fact]
        [Trait("Covers", "ImputeMissingValues")]
        public void ImputeMissingValuesMedianReplacesGapsAndReportsThem()
        {
            var result = missing.ImputeMissingValues(gappyTable, ["a", "b"]);

            result.FillValues.Should().Equal(2.0, 4.0);
            result.Table[2].Should().Equal(2.0, 4.0);
            result.Table[0].Should().Equal(1.0, 4.0);
            result.ReplacedPerColumn.Should().Equal(1, 2);
            result.TotalReplaced.Should().Be(3);
            result.ReplacedCells.Should().Contain(c => c.Row == 2 && c.Column == "b" && c.Value == 4.0);
        }

        [Fact]
        [Trait("Covers", "ImputeMissingValues")]
        public void ImputeMissingValuesMeanModeConstantAndIndicators()
        {
            var mean = missing.ImputeMissingValues(gappyTable, null, ImputationStrategy.Mean);
            mean.FillValues[0].Should().BeApproximately(7.0 / 3.0, Tight);

            var mode = missing.ImputeMissingValues(
                [[1.0], [2.0], [2.0], [4.0], [Nan]], null,
                ImputationStrategy.Mode);
            mode.FillValues[0].Should().Be(2.0);

            var constant = missing.ImputeMissingValues(gappyTable, null, ImputationStrategy.Constant, -1);
            constant.Table[2].Should().Equal(-1.0, -1.0);

            var flagged = missing.ImputeMissingValues(gappyTable, ["a", "b"], addMissingIndicators: true);
            flagged.ColumnNames.Should().Equal("a", "b", "a_missing", "b_missing");
            flagged.Table[2].Should().Equal(2.0, 4.0, 1.0, 1.0);
            flagged.Table[1].Should().Equal(2.0, 3.0, 0.0, 0.0);
            flagged.IndicatorSourceColumns.Should().Equal("a", "b");
        }

        [Fact]
        [Trait("Covers", "ImputeMissingValues")]
        public void ImputeMissingValuesRejectsAllMissingColumnUnlessConstant()
        {
            var table = new[] { new[] { Nan }, new[] { Nan } };

            Action act = () => missing.ImputeMissingValues(table);
            act.Should().Throw<ArgumentException>().WithMessage("*Constant*");

            missing.ImputeMissingValues(table, null, ImputationStrategy.Constant, 7).Table[0][0].Should().Be(7);

            Action infinite = () => missing.ImputeMissingValues(gappyTable, null, ImputationStrategy.Constant, Nan);
            infinite.Should().Throw<ArgumentException>().WithParameterName("constant");
        }

        [Fact]
        [Trait("Covers", "ApplyImputer")]
        public void ApplyImputerUsesFixedFillValuesAndIndicators()
        {
            var result = missing.ApplyImputer([[Nan, 1.0], [5.0, Nan]],
                [2.0, Nan], ["a", "b"], ["a"]);

            result.Table[0].Should().Equal(2.0, 1.0, 1.0);
            result.Table[1][0].Should().Be(5.0);
            double.IsNaN(result.Table[1][1]).Should().BeTrue();
            result.Table[1][2].Should().Be(0.0);
            result.TotalReplaced.Should().Be(1);
        }

        [Fact]
        [Trait("Covers", "ApplyImputer")]
        public void ApplyImputerRejectsWrongNumberOfFillValues()
        {
            Action act = () => missing.ApplyImputer(gappyTable, [1.0]);
            act.Should().Throw<ArgumentException>().WithParameterName("fillValues");
        }

        [Fact]
        [Trait("Covers", "ImputeForwardFill")]
        public void ImputeForwardFillCarriesLastValueAndOptionallyBackfillsLeadingGaps()
        {
            var column = new[] { Nan, 1.0, Nan, Nan, 4.0, Nan }.Select(v => new[] { v }).ToArray();

            var forward = missing.ImputeForwardFill(column);
            double.IsNaN(forward.Table[0][0]).Should().BeTrue();
            forward.Table.Skip(1).Select(r => r[0]).Should().Equal(1.0, 1.0, 1.0, 4.0, 4.0);
            forward.TotalReplaced.Should().Be(3);

            var back = missing.ImputeForwardFill(column, null, true);
            back.Table.Select(r => r[0]).Should().Equal(1.0, 1.0, 1.0, 1.0, 4.0, 4.0);
            back.TotalReplaced.Should().Be(4);
        }

        [Fact]
        [Trait("Covers", "ImputeLinearInterpolation")]
        public void ImputeLinearInterpolationFillsInteriorGapsOnALine()
        {
            var column = new[] { 1.0, Nan, Nan, 4.0 }.Select(v => new[] { v }).ToArray();

            missing.ImputeLinearInterpolation(column).Table.Select(r => r[0]).Should().Equal(1.0, 2.0, 3.0, 4.0);

            var edges = new[] { Nan, 2.0, Nan }.Select(v => new[] { v }).ToArray();
            double.IsNaN(missing.ImputeLinearInterpolation(edges).Table[0][0]).Should().BeTrue();
            missing.ImputeLinearInterpolation(edges, null, true).Table.Select(r => r[0]).Should().Equal(2.0, 2.0, 2.0);
        }

        [Fact]
        [Trait("Covers", "ImputeNearestNeighbours")]
        public void ImputeNearestNeighboursCopiesTheNearestRowsValue()
        {
            var data = new[]
            {
                new[] { 1.0, 10.0 },
                new[] { 2.0, 20.0 },
                new[] { 3.0, Nan },
                new[] { 100.0, 1000.0 }
            };

            var one = missing.ImputeNearestNeighbours(data, null, 1, false);
            one.Table[2][1].Should().Be(20.0);

            var two = missing.ImputeNearestNeighbours(data, null, 2, false);
            two.Table[2][1].Should().BeApproximately(15.0, Tight);
            two.ReplacedCells.Should().ContainSingle();
        }

        [Fact]
        [Trait("Covers", "ImputeNearestNeighbours")]
        public void ImputeNearestNeighboursFallsBackToColumnMeanWithoutOverlapAndRejectsEmptyColumn()
        {
            var data = new[] { new[] { Nan, 4.0 }, new[] { 5.0, Nan }, new[] { Nan, 8.0 } };
            var result = missing.ImputeNearestNeighbours(data, null, 1);
            result.Table[0][0].Should().Be(5.0);

            Action act = () => missing.ImputeNearestNeighbours([[Nan], [Nan]]);
            act.Should().Throw<ArgumentException>().WithMessage("*no observed value*");
        }

        [Fact]
        [Trait("Covers", "DropSparseRowsAndColumns")]
        public void DropSparseRowsAndColumnsRemovesEmptyColumnThenIncompleteRows()
        {
            var data = new[]
            {
                new[] { 1.0, Nan, 3.0 },
                new[] { 4.0, Nan, 6.0 },
                new[] { Nan, Nan, 9.0 },
                new[] { 7.0, Nan, 1.0 }
            };

            var result = missing.DropSparseRowsAndColumns(data, ["a", "b", "c"]);

            result.KeptColumns.Should().Equal("a", "c");
            result.DroppedColumns.Should().Equal("b");
            result.KeptRows.Should().Equal(0, 1, 3);
            result.DroppedRows.Should().Equal(2);
            result.Table.Should().HaveCount(3);
            result.Table[2].Should().Equal(7.0, 1.0);

            var lenient = missing.DropSparseRowsAndColumns(data, ["a", "b", "c"], 0.5, 0.5);
            lenient.KeptRows.Should().Equal(0, 1, 2, 3);
        }

        [Fact]
        [Trait("Covers", "DropSparseRowsAndColumns")]
        public void DropSparseRowsAndColumnsRejectsBadFractions()
        {
            Action act = () => missing.DropSparseRowsAndColumns(gappyTable, null, 1.5);
            act.Should().Throw<ArgumentException>().WithParameterName("maximumColumnMissingFraction");
        }

        [Fact]
        [Trait("Covers", "FitTableScaler")]
        public void FitTableScalerStandardIsZScoreWithSampleStandardDeviation()
        {
            var column = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 }.Select(v => new[] { v }).ToArray();
            var result = scaling.FitTableScaler(column);

            result.Centers[0].Should().BeApproximately(3.0, Tight);
            result.Scales[0].Should().BeApproximately(Math.Sqrt(2.5), Tight);
            result.Table[0][0].Should().BeApproximately(-2.0 / Math.Sqrt(2.5), Tight);
            result.Table.Sum(r => r[0]).Should().BeApproximately(0, Tight);
        }

        [Fact]
        [Trait("Covers", "FitTableScaler")]
        public void FitTableScalerMinMaxRobustAndMaxAbsMatchDefinitions()
        {
            var column = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 }.Select(v => new[] { v }).ToArray();

            scaling.FitTableScaler(column, null, TableScalingMethod.MinMax).Table.Select(r => r[0])
                .Should().BeEquivalentTo([0.0, 0.25, 0.5, 0.75, 1.0], o => o.WithStrictOrdering());

            var mad = scaling.FitTableScaler(column, null, TableScalingMethod.RobustMad);
            mad.Centers[0].Should().Be(3.0);
            mad.Scales[0].Should().BeApproximately(1.482602218505602, Tight);

            var iqr = scaling.FitTableScaler(column, null, TableScalingMethod.RobustIqr);
            iqr.Scales[0].Should().BeApproximately(2.0, Tight);
            iqr.Table[0][0].Should().BeApproximately(-1.0, Tight);

            var maxAbs =
                scaling.FitTableScaler([[-4.0], [2.0]], null, TableScalingMethod.MaxAbs);
            maxAbs.Centers[0].Should().Be(0);
            maxAbs.Scales[0].Should().Be(4);
            maxAbs.Table.Select(r => r[0]).Should().Equal(-1.0, 0.5);
        }

        [Fact]
        [Trait("Covers", "FitTableScaler")]
        public void FitTableScalerHandlesConstantMissingAndEmptyColumns()
        {
            var table = new[] { new[] { 7.0, 1.0 }, new[] { 7.0, Nan }, new[] { 7.0, 3.0 } };
            var result = scaling.FitTableScaler(table, ["k", "v"]);

            result.ConstantColumns.Should().Equal("k");
            result.Table.Select(r => r[0]).Should().OnlyContain(v => v == 0);
            double.IsNaN(result.Table[1][1]).Should().BeTrue();

            Action act = () => scaling.FitTableScaler([[Nan], [Nan]]);
            act.Should().Throw<ArgumentException>().WithMessage("*no finite value*");
        }

        [Fact]
        [Trait("Covers", "ApplyTableScaler")]
        public void ApplyTableScalerUsesFixedCentersAndScales()
        {
            var result = scaling.ApplyTableScaler(
                [[5.0, 1.0], [Nan, 2.0]], [3.0, 0.0], [2.0, 0.0]);

            result[0].Should().Equal(1.0, 0.0);
            double.IsNaN(result[1][0]).Should().BeTrue();

            Action act = () => scaling.ApplyTableScaler(gappyTable, [1.0], [1.0, 1.0]);
            act.Should().Throw<ArgumentException>().WithParameterName("centers");
        }

        [Fact]
        [Trait("Covers", "InvertTableScaler")]
        public void InvertTableScalerRoundTripsAFittedScaler()
        {
            var table = new[] { new[] { 10.0, 5.0 }, new[] { 20.0, 15.0 }, new[] { 40.0, 25.0 } };
            var fit = scaling.FitTableScaler(table);
            var back = scaling.InvertTableScaler(fit.Table, fit.Centers, fit.Scales);

            for (var i = 0; i < table.Length; i++)
            {
                for (var j = 0; j < 2; j++)
                {
                    back[i][j].Should().BeApproximately(table[i][j], 1e-9);
                }
            }
        }

        [Fact]
        [Trait("Covers", "NormaliseRowsToUnitNorm")]
        public void NormaliseRowsToUnitNormMatchesNormDefinitions()
        {
            var table = new[] { new[] { 3.0, 4.0 }, new[] { 0.0, 0.0 } };

            var l2 = scaling.NormaliseRowsToUnitNorm(table);
            l2.Table[0].Should().Equal(0.6, 0.8);
            l2.Norms.Should().Equal(5.0, 0.0);
            l2.Table[1].Should().Equal(0.0, 0.0);

            var l1 = scaling.NormaliseRowsToUnitNorm(table, RowNorm.L1);
            l1.Table[0][0].Should().BeApproximately(3.0 / 7.0, Tight);
            l1.Norms[0].Should().Be(7.0);

            var max = scaling.NormaliseRowsToUnitNorm(table, RowNorm.Max);
            max.Table[0].Should().Equal(0.75, 1.0);
        }

        [Fact]
        [Trait("Covers", "FitQuantileTransformer")]
        public void FitQuantileTransformerUniformMapsRanksToUnitInterval()
        {
            var column = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 }.Select(v => new[] { v }).ToArray();
            var result = scaling.FitQuantileTransformer(column, null, QuantileOutputDistribution.Uniform, 5);

            result.ReferenceQuantiles[0].Should().Equal(1.0, 2.0, 3.0, 4.0, 5.0);
            result.Table.Select(r => r[0]).Should().Equal(0.0, 0.25, 0.5, 0.75, 1.0);

            var normal = scaling.FitQuantileTransformer(column, null, QuantileOutputDistribution.Normal, 5);
            normal.Table[2][0].Should().BeApproximately(0.0, 1e-7);
            normal.Table[3][0].Should().BeApproximately(0.6744897501960817, 1e-6);
        }

        [Fact]
        [Trait("Covers", "FitQuantileTransformer")]
        public void FitQuantileTransformerTiesShareTheMidRank()
        {
            var column = new[] { 1.0, 2.0, 2.0, 2.0, 3.0 }.Select(v => new[] { v }).ToArray();
            var result = scaling.FitQuantileTransformer(column, null, QuantileOutputDistribution.Uniform, 5);

            result.Table[2][0].Should().BeApproximately(0.5, Tight);

            Action act = () => scaling.FitQuantileTransformer(column, null, QuantileOutputDistribution.Uniform, 1);
            act.Should().Throw<ArgumentException>().WithParameterName("quantileCount");
        }

        [Fact]
        [Trait("Covers", "ApplyQuantileTransformer")]
        public void ApplyQuantileTransformerInterpolatesAndClipsToTheEnds()
        {
            var references = new[] { new[] { 1.0, 2.0, 3.0, 4.0, 5.0 } };

            var result = scaling.ApplyQuantileTransformer(
                [[2.5], [10.0], [-3.0], [Nan]], references);

            result[0][0].Should().BeApproximately(0.375, Tight);
            result[1][0].Should().Be(1.0);
            result[2][0].Should().Be(0.0);
            double.IsNaN(result[3][0]).Should().BeTrue();

            var normal = scaling.ApplyQuantileTransformer([[10.0]], references,
                QuantileOutputDistribution.Normal);
            normal[0][0].Should().BeApproximately(5.199337582, 1e-5);
        }

        [Fact]
        [Trait("Covers", "ApplyPowerTransformer")]
        public void ApplyPowerTransformerMatchesBoxCoxAndYeoJohnsonFormulas()
        {
            var box = scaling.ApplyPowerTransformer([[Math.E], [3.0], [-1.0]],
                [0.0], PowerTransformMethod.BoxCox);

            box.Table[0][0].Should().BeApproximately(1.0, Tight);
            double.IsNaN(box.Table[2][0]).Should().BeTrue();

            var box2 = scaling.ApplyPowerTransformer([[3.0]], [2.0],
                PowerTransformMethod.BoxCox);

            box2.Table[0][0].Should().BeApproximately(4.0, Tight);

            var identity = scaling.ApplyPowerTransformer([[-2.0], [0.0], [3.0]],
                [1.0]);

            identity.Table.Select(r => r[0]).Should().Equal(-2.0, 0.0, 3.0);

            var log = scaling.ApplyPowerTransformer([[3.0], [-2.0]], [0.0]);
            log.Table[0][0].Should().BeApproximately(Math.Log(4), Tight);
            log.Table[1][0].Should().BeApproximately(-4.0, Tight);

            var two = scaling.ApplyPowerTransformer([[-2.0]], [2.0]);
            two.Table[0][0].Should().BeApproximately(-Math.Log(3), Tight);
        }

        [Fact]
        [Trait("Covers", "FitPowerTransformer")]
        public void FitPowerTransformerYeoJohnsonOfSymmetricDataHasLambdaOne()
        {
            var column = new[] { -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0 }.Select(v => new[] { v }).ToArray();
            var result = scaling.FitPowerTransformer(column);

            result.Lambdas[0].Should().BeApproximately(1.0, 1e-3);
        }

        [Fact]
        [Trait("Covers", "FitPowerTransformer")]
        public void FitPowerTransformerBoxCoxOfLogSymmetricDataIsNearLogarithm()
        {
            var column = Enumerable.Range(-8, 17).Select(i => new[] { Math.Exp(i * 0.25) }).ToArray();
            var result = scaling.FitPowerTransformer(column, null, PowerTransformMethod.BoxCox);

            result.Lambdas[0].Should().BeInRange(-0.15, 0.15);
            var transformed = result.Table.Select(r => r[0]).ToArray();
            var mean = transformed.Average();
            var skew = transformed.Sum(v => Math.Pow(v - mean, 3)) / transformed.Length;
            Math.Abs(skew).Should().BeLessThan(0.05);
        }

        [Fact]
        [Trait("Covers", "FitPowerTransformer")]
        public void FitPowerTransformerRejectsNonPositiveBoxCoxTooFewValuesAndBadRange()
        {
            Action negative = () => scaling.FitPowerTransformer(
                [[1.0], [2.0], [-3.0]], null, PowerTransformMethod.BoxCox);

            negative.Should().Throw<ArgumentException>().WithMessage("*Yeo*");

            Action few = () => scaling.FitPowerTransformer([[1.0], [2.0]]);
            few.Should().Throw<ArgumentException>().WithMessage("*at least 3*");

            Action range = () => scaling.FitPowerTransformer(
                [[1.0], [2.0], [3.0]], null, PowerTransformMethod.YeoJohnson,
                [5.0, -5.0]);

            range.Should().Throw<ArgumentException>().WithParameterName("lambdaRange");

            var constant = scaling.FitPowerTransformer([[4.0], [4.0], [4.0]]);
            constant.Lambdas[0].Should().Be(1.0);
        }

        [Fact]
        [Trait("Covers", "ApplyColumnFunction")]
        public void ApplyColumnFunctionMatchesClosedFormsAndCountsUndefinedCells()
        {
            var logs = scaling.ApplyColumnFunction(
                [[1.0], [Math.E], [0.0], [Nan]],
                ColumnFunction.NaturalLog);
            logs.Table[0][0].Should().Be(0.0);
            logs.Table[1][0].Should().BeApproximately(1.0, Tight);
            logs.UndefinedCells.Should().Be(1);

            scaling.ApplyColumnFunction([[0.0]], ColumnFunction.NaturalLog, 1).Table[0][0].Should()
                .Be(0);

            scaling.ApplyColumnFunction([[-3.0]], ColumnFunction.SignedLog1P).Table[0][0]
                .Should().BeApproximately(-Math.Log(4), Tight);

            scaling.ApplyColumnFunction([[27.0]], ColumnFunction.CubeRoot).Table[0][0]
                .Should().BeApproximately(3.0, Tight);

            scaling.ApplyColumnFunction([[4.0]], ColumnFunction.Reciprocal).Table[0][0].Should()
                .Be(0.25);

            scaling.ApplyColumnFunction([[1000.0]], ColumnFunction.Log10).Table[0][0]
                .Should().BeApproximately(3.0, Tight);
            scaling.ApplyColumnFunction([[8.0]], ColumnFunction.Log2).Table[0][0]
                .Should().BeApproximately(3.0, Tight);

            scaling.ApplyColumnFunction([[-9.0]], ColumnFunction.SignedSqrt).Table[0][0].Should()
                .Be(-3.0);

            scaling.ApplyColumnFunction([[-9.0]], ColumnFunction.Sqrt).UndefinedCells.Should().Be(1);
            scaling.ApplyColumnFunction([[-3.0]], ColumnFunction.Square).Table[0][0].Should().Be(9);
            scaling.ApplyColumnFunction([[-3.0]], ColumnFunction.AbsoluteValue).Table[0][0].Should()
                .Be(3);

            scaling.ApplyColumnFunction([[1.0]], ColumnFunction.Log1P).Table[0][0]
                .Should().BeApproximately(Math.Log(2), Tight);
        }

        [Fact]
        [Trait("Covers", "FitOutlierClipper")]
        public void FitOutlierClipperIqrFencesClipTheExtremeValue()
        {
            var column = Enumerable.Range(1, 10).Select(i => (double)i).Append(100.0).Select(v => new[] { v })
                .ToArray();

            var result = scaling.FitOutlierClipper(column, null, OutlierBoundsMethod.IqrFences);

            result.LowerBounds[0].Should().BeApproximately(-4.0, Tight);
            result.UpperBounds[0].Should().BeApproximately(16.0, Tight);
            result.ClippedHighPerColumn[0].Should().Be(1);
            result.ClippedLowPerColumn[0].Should().Be(0);
            result.Table.Last()[0].Should().Be(16.0);
            result.TotalClipped.Should().Be(1);
        }

        [Fact]
        [Trait("Covers", "FitOutlierClipper")]
        public void FitOutlierClipperQuantileMadAndStandardDeviationBounds()
        {
            var column = Enumerable.Range(1, 10).Select(i => (double)i).Append(100.0).Select(v => new[] { v })
                .ToArray();

            var quantile = scaling.FitOutlierClipper(column, null, OutlierBoundsMethod.Quantiles, 0.1, 0.9);
            quantile.LowerBounds[0].Should().BeApproximately(2.0, Tight);
            quantile.UpperBounds[0].Should().BeApproximately(10.0, Tight);
            quantile.ClippedLowPerColumn[0].Should().Be(1);
            quantile.ClippedHighPerColumn[0].Should().Be(1);

            var mad = scaling.FitOutlierClipper(column, null, OutlierBoundsMethod.MedianAbsoluteDeviations, 0.01, 0.99,
                3);
            mad.LowerBounds[0].Should().BeApproximately(6.0 - 3 * 1.482602218505602 * 3, 1e-9);
            mad.UpperBounds[0].Should().BeApproximately(6.0 + 3 * 1.482602218505602 * 3, 1e-9);

            var sd = scaling.FitOutlierClipper([[1.0], [2.0], [3.0]], null,
                OutlierBoundsMethod.StandardDeviations, 0.01, 0.99, 1);

            sd.LowerBounds[0].Should().BeApproximately(1.0, Tight);
            sd.UpperBounds[0].Should().BeApproximately(3.0, Tight);
        }

        [Fact]
        [Trait("Covers", "FitOutlierClipper")]
        public void FitOutlierClipperRejectsBadQuantilesAndEmptyColumns()
        {
            var column = new[] { new[] { 1.0 }, new[] { 2.0 } };

            Action bad = () => scaling.FitOutlierClipper(column, null, OutlierBoundsMethod.Quantiles, 0.9, 0.1);
            bad.Should().Throw<ArgumentException>().WithParameterName("lowerQuantile");

            Action empty = () => scaling.FitOutlierClipper([[Nan]]);
            empty.Should().Throw<ArgumentException>().WithMessage("*no finite value*");
        }

        [Fact]
        [Trait("Covers", "ApplyOutlierClipper")]
        public void ApplyOutlierClipperCapsAtFixedBoundsAndKeepsMissing()
        {
            var result = scaling.ApplyOutlierClipper(
                [[-10.0], [5.0], [50.0], [Nan]], [0.0], [10.0]);

            result.Table.Select(r => r[0]).Take(3).Should().Equal(0.0, 5.0, 10.0);
            double.IsNaN(result.Table[3][0]).Should().BeTrue();
            result.ClippedLowPerColumn[0].Should().Be(1);
            result.ClippedHighPerColumn[0].Should().Be(1);

            Action act = () => scaling.ApplyOutlierClipper(gappyTable, [5.0, 5.0], [1.0, 9.0]);
            act.Should().Throw<ArgumentException>().WithParameterName("upperBounds");
        }

        [Fact]
        [Trait("Covers", "DescribeDatasetShape")]
        public void DescribeDatasetShapeInfersColumnTypes()
        {
            var table = new[]
            {
                new[] { 1.0, 0.0, 1.0, 0.5, Nan },
                new[] { 1.0, 1.0, 2.0, 1.5, Nan },
                new[] { 1.0, 0.0, 3.0, 2.5, Nan },
                new[] { 1.0, 1.0, 4.0, 3.5, Nan }
            };

            var names = new[] { "const", "flag", "count", "amount", "gone" };

            var shape = quality.DescribeDatasetShape(table, names);

            shape.Rows.Should().Be(4);
            shape.Columns.Should().Be(5);

            shape.ColumnSummaries.Select(c => c.Type).Should()
                .Equal("Constant", "Binary", "Integer", "Continuous", "AllMissing");

            shape.ColumnSummaries[2].Minimum.Should().Be(1.0);
            shape.ColumnSummaries[2].Maximum.Should().Be(4.0);
            shape.ColumnSummaries[2].Mean.Should().BeApproximately(2.5, Tight);
            shape.ColumnSummaries[2].Distinct.Should().Be(4);
            shape.ColumnSummaries[4].Missing.Should().Be(4);
            double.IsNaN(shape.ColumnSummaries[4].Mean).Should().BeTrue();
            shape.ApproximateBytes.Should().Be(4 * 5 * 8);
        }

        [Fact]
        [Trait("Covers", "DescribeDatasetShape")]
        public void DescribeDatasetShapeRejectsRaggedTables()
        {
            Action act = () => quality.DescribeDatasetShape([[1.0], [1.0, 2.0]]);
            act.Should().Throw<ArgumentException>().WithParameterName("data");
        }

        [Fact]
        [Trait("Covers", "DescribeCategoricalTable")]
        public void DescribeCategoricalTableProfilesCardinalityAndEntropy()
        {
            var profile = quality.DescribeCategoricalTable(Column("a", "a", "b", "", "c"), ["merchant"], 2)
                .Single();

            profile.Name.Should().Be("merchant");
            profile.Distinct.Should().Be(3);
            profile.Missing.Should().Be(1);
            profile.MostFrequent.Should().Be("a");
            profile.MostFrequentCount.Should().Be(2);
            profile.CardinalityRatio.Should().BeApproximately(0.6, Tight);
            profile.Entropy.Should().BeApproximately(0.5 * Math.Log(2) + 2 * 0.25 * Math.Log(4), 1e-12);
            profile.TopValues.Select(v => v.Value).Should().Equal("a", "b");

            var empty = quality.DescribeCategoricalTable(Column("", null)).Single();
            empty.Distinct.Should().Be(0);
            double.IsNaN(empty.Entropy).Should().BeTrue();

            Action act = () => quality.DescribeCategoricalTable(Column("a"), null, 0);
            act.Should().Throw<ArgumentException>().WithParameterName("topValues");
        }

        [Fact]
        [Trait("Covers", "FindDuplicateRows")]
        public void FindDuplicateRowsGroupsRepeatsAndTreatsNaNAsEqual()
        {
            var table = new[]
            {
                new[] { 1.0, 2.0 }, new[] { 3.0, 4.0 }, new[] { 1.0, 2.0 },
                new[] { 1.0, 2.0 }, new[] { Nan, 1.0 }, new[] { Nan, 1.0 }
            };

            var result = quality.FindDuplicateRows(table);

            result.DuplicateRows.Should().Be(3);
            result.Groups.Should().HaveCount(2);
            result.Groups[0].FirstRow.Should().Be(0);
            result.Groups[0].Repeats.Should().Equal(2, 3);
            result.Groups[1].FirstRow.Should().Be(4);
            result.Groups[1].Repeats.Should().Equal(5);
            result.RowsToKeep.Should().Equal(0, 1, 4);

            quality.FindDuplicateRows([[1.0], [2.0]]).DuplicateRows.Should().Be(0);
        }

        [Fact]
        [Trait("Covers", "FindDuplicateCategoricalRows")]
        public void FindDuplicateCategoricalRowsTreatsNullAndEmptyAlike()
        {
            var table = new[]
            {
                new[] { "a", "b" }, new[] { "a", null }, new[] { "a", "" }, new[] { "a", "b" }, new[] { "ab", "" }
            };

            var result = quality.FindDuplicateCategoricalRows(table);

            result.DuplicateRows.Should().Be(2);
            result.Groups.Select(g => g.FirstRow).Should().Equal(0, 1);
            result.RowsToKeep.Should().Equal(0, 1, 4);
        }

        [Fact]
        [Trait("Covers", "CheckTargetBalance")]
        public void CheckTargetBalanceReportsRatioAndEntropy()
        {
            var even = quality.CheckTargetBalance([0, 1, 0, 1]);
            even.RelativeEntropy.Should().BeApproximately(1.0, 1e-12);
            even.ImbalanceRatio.Should().Be(1.0);
            even.IsBinary.Should().BeTrue();
            even.Warnings.Should().OnlyContain(w => w.Contains("only 2 row"));

            var skewed = quality.CheckTargetBalance([.. Enumerable.Repeat(0, 95), .. Enumerable.Repeat(1, 5)]);
            skewed.ImbalanceRatio.Should().BeApproximately(19.0, Tight);
            skewed.SmallestClassFraction.Should().BeApproximately(0.05, Tight);
            skewed.Classes.Select(c => c.Count).Should().Equal(95, 5);
            skewed.Warnings.Should().Contain(w => w.Contains("stratified"));

            skewed.RelativeEntropy.Should()
                .BeApproximately(-(0.95 * Math.Log(0.95) + 0.05 * Math.Log(0.05)) / Math.Log(2), 1e-12);
        }

        [Fact]
        [Trait("Covers", "CheckTargetBalance")]
        public void CheckTargetBalanceWarnsOnSingleAndVeryRareClasses()
        {
            quality.CheckTargetBalance([1, 1, 1]).Warnings.Should().Contain(w => w.Contains("single class"));

            var rare = quality.CheckTargetBalance([.. Enumerable.Repeat(0, 995), .. Enumerable.Repeat(1, 5)]);
            rare.Warnings.Should().Contain(w => w.Contains("misleading"));
            rare.Warnings.Should().Contain(w => w.Contains("only 5"));

            Action act = () => quality.CheckTargetBalance([]);
            act.Should().Throw<ArgumentException>().WithParameterName("labels");
        }

        private static double[][] QualityTable(out int[] labels)
        {
            labels = [.. Enumerable.Range(0, 60).Select(i => i % 2)];
            var l = labels;

            var rows = Enumerable.Range(0, 60).Select(i => new[]
            {
                i * 7 % 11,
                5.0,
                l[i],
                i % 5 == 0 ? i : Nan,
                Math.Exp(i % 9),
                i * 1e6
            }).ToList();
            rows.Add((double[])rows[0].Clone());

            return [.. rows];
        }

        [Fact]
        [Trait("Covers", "DataQualityReport")]
        public void DataQualityReportFlagsConstantLeakageMissingnessAndDuplicates()
        {
            var table = QualityTable(out var labels);
            var all = labels.Concat([labels[0]]).ToArray();
            var names = new[] { "noise", "constant", "leak", "sparse", "skewed", "huge" };

            var report = quality.DataQualityReport(table, names, all);

            report.Rows.Should().Be(61);
            report.Columns.Should().Be(6);
            report.DuplicateRows.Should().Be(1);
            report.Issues.Should().Contain(i => i.Category == "Constant" && i.Column == "constant");
            report.Issues.Should().Contain(i => i.Category == "Leakage" && i.Column == "leak" && i.Severity == "Error");
            report.LeakageSuspects.Select(s => s.Column).Should().Contain("leak");
            report.Issues.Should().Contain(i => i.Category == "Missing" && i.Column == "sparse");
            report.Issues.Should().Contain(i => i.Category == "Duplicates");
            report.Issues.Should().Contain(i => i.Category == "Scale");
            report.TargetBalance.Should().NotBeNull();
            report.TargetBalance!.IsBinary.Should().BeTrue();
            report.Issues.Should().NotContain(i => i.Category == "Leakage" && i.Column == "noise");
            report.Issues.Should().OnlyContain(i => i.SuggestedTool.Length > 0 && i.Message.Length > 0);
            report.ColumnSummaries.Should().HaveCount(6);
        }

        [Fact]
        [Trait("Covers", "DataQualityReport")]
        public void DataQualityReportFindsPerfectSeparatorAndIdentifierAndValidatesTargets()
        {
            var labels = Enumerable.Range(0, 40).Select(i => i < 20 ? 0 : 1).ToArray();

            var table = Enumerable.Range(0, 40)
                .Select(i => new[] { i, i < 20 ? -5.0 - i * 0.5 : 5.0 + i * 0.5 })
                .ToArray();

            var report = quality.DataQualityReport(table, ["id", "separator"], labels);

            report.Issues.Should().Contain(i => i.Category == "IdentifierLike" && i.Column == "id");
            report.LeakageSuspects.Should().Contain(s => s.Column == "separator");

            report.LeakageSuspects.Should().Contain(s =>
                (s.Column == "id" && s.Measure == "Auc") || (s.Column == "id" && s.Measure == "Correlation"));

            Action both = () => quality.DataQualityReport(table, null, labels, new double[40]);
            both.Should().Throw<ArgumentException>().WithParameterName("numericTarget");

            Action length = () => quality.DataQualityReport(table, null, [0, 1]);
            length.Should().Throw<ArgumentException>().WithParameterName("labels");

            Action threshold = () => quality.DataQualityReport(table, null, null, null, 0.1);
            threshold.Should().Throw<ArgumentException>().WithParameterName("leakageThreshold");
        }

        [Fact]
        [Trait("Covers", "DataQualityReport")]
        public void DataQualityReportOnCleanDataHasNoErrorsAndUsesNumericTarget()
        {
            var rng = new Random(11);

            var table = Enumerable.Range(0, 200).Select(_ => new[] { rng.NextDouble(), rng.NextDouble() * 3 })
                .ToArray();

            var target = table.Select(r => r[0] * 2 + rng.NextDouble()).ToArray();

            var report = quality.DataQualityReport(table, ["a", "b"], null, target);

            report.Issues.Where(i => i.Severity == "Error").Should().BeEmpty();
            report.TargetBalance.Should().BeNull();
            report.DuplicateRows.Should().Be(0);

            var leaky = quality.DataQualityReport(table, ["a", "b"], null,
                [.. table.Select(r => r[0] * 3)]);

            leaky.LeakageSuspects.Should().ContainSingle(s => s.Column == "a" && s.Measure == "Correlation");
        }

        [Fact]
        [Trait("Covers", "DataQualityReportCategorical")]
        public void DataQualityReportCategoricalFlagsFormattingConstantLeakageAndDuplicates()
        {
            var labels = Enumerable.Range(0, 40).Select(i => i % 2).ToArray();

            var data = Enumerable.Range(0, 40).Select(i => new[]
            {
                i % 3 == 0 ? "Yes" : i % 3 == 1 ? "yes " : "No",
                "same",
                "L" + labels[i],
                i % 2 == 0 ? "" : "x",
                "id" + i
            }).ToList();
            data.Add((string[])data[0].Clone());
            var all = labels.Concat([labels[0]]).ToArray();
            var names = new[] { "answer", "constant", "leak", "half", "key" };

            var report = quality.DataQualityReportCategorical([.. data], names, all);

            report.Rows.Should().Be(41);
            report.DuplicateRows.Should().Be(1);
            report.ColumnProfiles.Should().HaveCount(5);
            report.Issues.Should().Contain(i => i.Category == "InconsistentFormatting" && i.Column == "answer");
            report.Issues.Should().Contain(i => i.Category == "Constant" && i.Column == "constant");
            report.Issues.Should().Contain(i => i.Category == "Leakage" && i.Column == "leak" && i.Severity == "Error");
            report.Issues.Should().Contain(i => i.Category == "Duplicates");
            report.Issues.Should().NotContain(i => i.Category == "Leakage" && i.Column == "answer");
        }

        [Fact]
        [Trait("Covers", "DataQualityReportCategorical")]
        public void DataQualityReportCategoricalFlagsHighCardinalityMissingAndRareCategories()
        {
            var data = Enumerable.Range(0, 120)
                .Select(i => new[] { "user" + i, i < 100 ? "" : "v", i % 30 < 6 ? "r" + i : "common" })
                .ToArray();

            var report = quality.DataQualityReportCategorical(data, ["user", "sparse", "mixed"]);

            report.Issues.Should().Contain(i => i.Category == "HighCardinality" && i.Column == "user");
            report.Issues.Should().Contain(i => i.Category == "Missing" && i.Column == "sparse");
            report.Issues.Should().Contain(i => i.Category == "RareCategories" && i.Column == "mixed");

            var empty = quality.DataQualityReportCategorical([[""], [null]]);
            empty.Issues.Should().Contain(i => i.Category == "Missing" && i.Severity == "Error");

            Action act = () => quality.DataQualityReportCategorical(data, null, null, 0.2);
            act.Should().Throw<ArgumentException>().WithParameterName("leakageThreshold");
        }

        private static readonly SamplingTools sampling = new();
        private static readonly ClassImbalanceTools imbalance = new();
        private static readonly ResamplingInferenceTools inference = new();
        private static readonly DataQualityTools quality = new();

        private static void ShouldPartition(int total, params int[][] sets)
        {
            var all = sets.SelectMany(s => s).ToArray();
            all.Should().OnlyHaveUniqueItems();
            all.OrderBy(i => i).Should().Equal(Enumerable.Range(0, total));
        }

        [Fact]
        [Trait("Covers", "SampleSimpleRandom")]
        public void SampleSimpleRandomWithoutReplacementIsUniqueSortedAndReproducible()
        {
            var first = sampling.SampleSimpleRandom(100, 10, false, 7);
            var again = sampling.SampleSimpleRandom(100, 10, false, 7);

            first.Indices.Should().HaveCount(10).And.OnlyHaveUniqueItems().And.BeInAscendingOrder();
            first.Indices.Should().OnlyContain(i => i >= 0 && i < 100);
            first.NotSelected.Should().HaveCount(90);
            ShouldPartition(100, first.Indices, first.NotSelected);
            again.Indices.Should().Equal(first.Indices);
            sampling.SampleSimpleRandom(100, 10, false, 8).Indices.Should().NotEqual(first.Indices);
            first.Seed.Should().Be(7);
        }

        [Fact]
        [Trait("Covers", "SampleSimpleRandom")]
        public void SampleSimpleRandomWithReplacementCanExceedPopulationAndRejectsImpossibleDraws()
        {
            sampling.SampleSimpleRandom(5, 50, true).Indices.Should().HaveCount(50)
                .And.OnlyContain(i => i >= 0 && i < 5);

            Action act = () => sampling.SampleSimpleRandom(5, 6);
            act.Should().Throw<ArgumentException>().WithParameterName("sampleSize");

            sampling.SampleSimpleRandom(3, 3).Indices.Should().Equal(0, 1, 2);
        }

        [Fact]
        [Trait("Covers", "SampleStratified")]
        public void SampleStratifiedProportionalAndEqualAllocation()
        {
            var strata = Enumerable.Repeat("a", 80).Concat(Enumerable.Repeat("b", 20)).ToArray();

            var proportional = sampling.SampleStratified(strata, 10);
            proportional.Allocation.Select(a => a.Sampled).Should().Equal(8, 2);
            proportional.Indices.Count(i => i < 80).Should().Be(8);
            proportional.Indices.Should().HaveCount(10).And.BeInAscendingOrder();
            ShouldPartition(100, proportional.Indices, proportional.NotSelected);

            var equal = sampling.SampleStratified(strata, 10, true);
            equal.Allocation.Select(a => a.Sampled).Should().Equal(5, 5);
            equal.Allocation.Select(a => a.Population).Should().Equal(80, 20);

            var capped = sampling.SampleStratified(["a", "a", "a", "a", "b"], 4, true);
            capped.Allocation.Select(a => a.Sampled).Should().Equal(3, 1);

            Action act = () => sampling.SampleStratified(strata, 101);
            act.Should().Throw<ArgumentException>().WithParameterName("sampleSize");
        }

        [Fact]
        [Trait("Covers", "SampleSystematic")]
        public void SampleSystematicHasConstantSpacing()
        {
            var result = sampling.SampleSystematic(100, 10, 3);

            result.Interval.Should().Be(10.0);
            result.Indices.Should().HaveCount(10);
            result.Indices.Zip(result.Indices.Skip(1), (a, b) => b - a).Should().OnlyContain(d => d == 10);
            result.Indices[0].Should().BeInRange(0, 9);
            result.Start.Should().BeInRange(0.0, 10.0);

            sampling.SampleSystematic(7, 7).Indices.Should().Equal(Enumerable.Range(0, 7));

            Action act = () => sampling.SampleSystematic(5, 6);
            act.Should().Throw<ArgumentException>().WithParameterName("sampleSize");
        }

        [Fact]
        [Trait("Covers", "SampleReservoir")]
        public void SampleReservoirKeepsKUniqueRowsAndIsRoughlyUniform()
        {
            sampling.SampleReservoir(1000, 10, 5).Should().HaveCount(10).And.OnlyHaveUniqueItems();
            sampling.SampleReservoir(3, 10).Should().Equal(0, 1, 2);

            var hitsOfFirstRow = Enumerable.Range(1, 3000)
                .Count(seed => sampling.SampleReservoir(10, 3, seed).Contains(0));
            (hitsOfFirstRow / 3000.0).Should().BeApproximately(0.3, 0.03);

            var hitsOfLastRow = Enumerable.Range(1, 3000)
                .Count(seed => sampling.SampleReservoir(10, 3, seed).Contains(9));
            (hitsOfLastRow / 3000.0).Should().BeApproximately(0.3, 0.03);
        }

        [Fact]
        [Trait("Covers", "SampleWeighted")]
        public void SampleWeightedNeverPicksZeroWeightAndFavoursHeavyRows()
        {
            var weights = new[] { 0.0, 1000.0, 1.0, 1.0, 0.0 };

            for (var seed = 1; seed <= 200; seed++)
            {
                sampling.SampleWeighted(weights, 2, false, seed).Should().NotContain([0, 4]);
            }

            var heavy = Enumerable.Range(1, 500)
                .Count(seed => sampling.SampleWeighted(weights, 1, false, seed)[0] == 1);
            heavy.Should().BeGreaterThan(475);

            var withReplacement = sampling.SampleWeighted(weights, 20, true, 3);
            withReplacement.Should().HaveCount(20).And.NotContain([0, 4]);
        }

        [Fact]
        [Trait("Covers", "SampleWeighted")]
        public void SampleWeightedRejectsBadWeightsAndImpossibleDraws()
        {
            Action negative = () => sampling.SampleWeighted([1.0, -1.0], 1);
            negative.Should().Throw<ArgumentException>().WithParameterName("weights");

            Action zeros = () => sampling.SampleWeighted([0.0, 0.0], 1);
            zeros.Should().Throw<ArgumentException>().WithParameterName("weights");

            Action tooMany = () => sampling.SampleWeighted([1.0, 0.0, 1.0], 3);
            tooMany.Should().Throw<ArgumentException>().WithParameterName("sampleSize");
        }

        [Fact]
        [Trait("Covers", "SampleClusters")]
        public void SampleClustersReturnsWholeGroups()
        {
            var keys = Enumerable.Range(0, 30).Select(i => "g" + i / 3).ToArray();
            var result = sampling.SampleClusters(keys, 4, 2);

            result.ChosenGroups.Should().HaveCount(4);
            result.Indices.Should().HaveCount(12);
            result.Indices.Select(i => keys[i]).Distinct().Should().BeEquivalentTo(result.ChosenGroups);
            ShouldPartition(30, result.Indices, result.NotSelected);

            Action act = () => sampling.SampleClusters(keys, 11);
            act.Should().Throw<ArgumentException>().WithParameterName("groupCount");
        }

        [Fact]
        [Trait("Covers", "SplitTrainValidationTest")]
        public void SplitTrainValidationTestPartitionsRowsInTheRequestedShares()
        {
            var result = sampling.SplitTrainValidationTest(100, 0.6, 0.2, 0.2, null, true, 4);

            result.Train.Should().HaveCount(60);
            result.Validation.Should().HaveCount(20);
            result.Test.Should().HaveCount(20);
            ShouldPartition(100, result.Train, result.Validation, result.Test);
            sampling.SplitTrainValidationTest(100, 0.6, 0.2, 0.2, null, true, 4).Train.Should().Equal(result.Train);
        }

        [Fact]
        [Trait("Covers", "SplitTrainValidationTest")]
        public void SplitTrainValidationTestStratifiesAndValidatesFractions()
        {
            var labels = Enumerable.Repeat(0, 90).Concat(Enumerable.Repeat(1, 10)).ToArray();
            var result = sampling.SplitTrainValidationTest(100, 0.6, 0.2, 0.2, labels);

            result.Train.Count(i => labels[i] == 1).Should().Be(6);
            result.Validation.Count(i => labels[i] == 1).Should().Be(2);
            result.Test.Count(i => labels[i] == 1).Should().Be(2);

            Action sum = () => sampling.SplitTrainValidationTest(100, 0.7);
            sum.Should().Throw<ArgumentException>().WithParameterName("trainFraction");

            Action length = () => sampling.SplitTrainValidationTest(100, 0.6, 0.2, 0.2, [1, 0]);
            length.Should().Throw<ArgumentException>().WithParameterName("labels");

            sampling.SplitTrainValidationTest(10, 0.5, 0.0, 0.5).Validation.Should().BeEmpty();
        }

        [Fact]
        [Trait("Covers", "SplitByGroup")]
        public void SplitByGroupKeepsEveryGroupInOneSet()
        {
            var keys = Enumerable.Range(0, 100).Select(i => "g" + i / 5).ToArray();
            var result = sampling.SplitByGroup(keys, 0.6, 0.2, 0.2, 9);

            ShouldPartition(100, result.Train, result.Validation, result.Test);
            var trainGroups = result.Train.Select(i => keys[i]).Distinct().ToArray();
            var validationGroups = result.Validation.Select(i => keys[i]).Distinct().ToArray();
            var testGroups = result.Test.Select(i => keys[i]).Distinct().ToArray();
            trainGroups.Intersect(validationGroups).Should().BeEmpty();
            trainGroups.Intersect(testGroups).Should().BeEmpty();
            validationGroups.Intersect(testGroups).Should().BeEmpty();
            result.Train.Should().HaveCount(60);
            result.Test.Should().HaveCount(20);

            Action act = () => sampling.SplitByGroup(keys, 0, 0, 0);
            act.Should().Throw<ArgumentException>().WithParameterName("trainFraction");
        }

        [Fact]
        [Trait("Covers", "SplitTimeOrdered")]
        public void SplitTimeOrderedPutsTheFutureInTest()
        {
            var times = new[] { 5.0, 1.0, 9.0, 3.0, 7.0, 2.0, 10.0, 4.0, 8.0, 6.0 };
            var result = sampling.SplitTimeOrdered(times);

            result.Train.Select(i => times[i]).Should().Equal(1.0, 2.0, 3.0, 4.0, 5.0, 6.0);
            result.Validation.Select(i => times[i]).Should().Equal(7.0, 8.0);
            result.Test.Select(i => times[i]).Should().Equal(9.0, 10.0);
            result.ValidationStartTime.Should().Be(7.0);
            result.TestStartTime.Should().Be(9.0);
        }

        [Fact]
        [Trait("Covers", "SplitTimeOrdered")]
        public void SplitTimeOrderedPurgeGapAndValidation()
        {
            var times = Enumerable.Range(1, 10).Select(i => (double)i).ToArray();
            var result = sampling.SplitTimeOrdered(times, 0.6, 0.2, 0.2, 1);

            result.Train.Should().HaveCount(6);
            result.Validation.Select(i => times[i]).Should().Equal(8.0);
            result.Test.Select(i => times[i]).Should().Equal(10.0);

            Action nan = () => sampling.SplitTimeOrdered([1.0, Nan]);
            nan.Should().Throw<ArgumentException>().WithParameterName("times");
        }

        [Fact]
        [Trait("Covers", "GenerateKFolds")]
        public void GenerateKFoldsTestSetsPartitionTheRows()
        {
            var folds = sampling.GenerateKFolds(10, 5, 3);

            folds.Should().HaveCount(5);
            folds.Should().OnlyContain(f => f.Test.Length == 2 && f.Train.Length == 8);
            ShouldPartition(10, [.. folds.Select(f => f.Test)]);
            folds.Should().OnlyContain(f => f.Train.Intersect(f.Test).Count() == 0);
            folds.Select(f => f.Fold).Should().Equal(0, 1, 2, 3, 4);

            Action act = () => sampling.GenerateKFolds(3);
            act.Should().Throw<ArgumentException>().WithParameterName("folds");
        }

        [Fact]
        [Trait("Covers", "GenerateStratifiedKFolds")]
        public void GenerateStratifiedKFoldsKeepsClassProportions()
        {
            var labels = new[] { 0, 0, 0, 0, 0, 0, 0, 0, 1, 1 };
            var folds = sampling.GenerateStratifiedKFolds(labels, 2);

            folds.Should().OnlyContain(f =>
                f.Test.Count(i => labels[i] == 1) == 1 && f.Test.Count(i => labels[i] == 0) == 4);

            ShouldPartition(10, [.. folds.Select(f => f.Test)]);

            Action act = () => sampling.GenerateStratifiedKFolds(labels, 1);
            act.Should().Throw<ArgumentException>().WithParameterName("folds");
        }

        [Fact]
        [Trait("Covers", "GenerateGroupKFolds")]
        public void GenerateGroupKFoldsNeverSplitsAGroup()
        {
            var keys = Enumerable.Range(0, 40).Select(i => "g" + i / 4).ToArray();
            var folds = sampling.GenerateGroupKFolds(keys, 5, 2);

            ShouldPartition(40, [.. folds.Select(f => f.Test)]);

            foreach (var fold in folds)
            {
                fold.Train.Select(i => keys[i]).Intersect(fold.Test.Select(i => keys[i])).Should().BeEmpty();
                fold.Test.Should().HaveCount(8);
            }

            Action act = () => sampling.GenerateGroupKFolds(["a", "a", "b"], 3);
            act.Should().Throw<ArgumentException>().WithParameterName("folds");
        }

        [Fact]
        [Trait("Covers", "GenerateTimeSeriesFolds")]
        public void GenerateTimeSeriesFoldsExpandTheTrainingWindow()
        {
            var times = Enumerable.Range(1, 12).Select(i => (double)i).Reverse().ToArray();
            var folds = sampling.GenerateTimeSeriesFolds(times, 3);

            folds.Should().HaveCount(3);
            folds[0].Train.Select(i => times[i]).Should().Equal(1.0, 2.0, 3.0);
            folds[0].Test.Select(i => times[i]).Should().Equal(4.0, 5.0, 6.0);
            folds[1].Train.Should().HaveCount(6);
            folds[2].Train.Should().HaveCount(9);
            folds[2].Test.Select(i => times[i]).Should().Equal(10.0, 11.0, 12.0);

            var gapped = sampling.GenerateTimeSeriesFolds(times, 3, 1);
            gapped[0].Train.Select(i => times[i]).Should().Equal(1.0, 2.0);

            Action act = () => sampling.GenerateTimeSeriesFolds([1.0, 2.0], 3);
            act.Should().Throw<ArgumentException>().WithParameterName("folds");
        }

        [Fact]
        [Trait("Covers", "GenerateLeaveOneOutFolds")]
        public void GenerateLeaveOneOutFoldsTestsOneRowEach()
        {
            var folds = sampling.GenerateLeaveOneOutFolds(5);

            folds.Should().HaveCount(5);
            folds.Should().OnlyContain(f => f.Test.Length == 1 && f.Train.Length == 4);
            folds.Select(f => f.Test[0]).Should().Equal(0, 1, 2, 3, 4);
            folds[2].Train.Should().Equal(0, 1, 3, 4);

            Action act = () => sampling.GenerateLeaveOneOutFolds(1001);
            act.Should().Throw<ArgumentException>().WithParameterName("rowCount");
        }

        [Fact]
        [Trait("Covers", "GenerateRepeatedKFolds")]
        public void GenerateRepeatedKFoldsGivesRepeatsTimesFoldsWithDifferentShuffles()
        {
            var folds = sampling.GenerateRepeatedKFolds(10);

            folds.Should().HaveCount(15);
            folds.Select(f => f.Repeat).Distinct().Should().Equal(0, 1, 2);

            foreach (var repeat in folds.GroupBy(f => f.Repeat))
            {
                ShouldPartition(10, [.. repeat.Select(f => f.Test)]);
            }

            var first = folds.Where(f => f.Repeat == 0).Select(f => string.Join(",", f.Test)).ToArray();
            var second = folds.Where(f => f.Repeat == 1).Select(f => string.Join(",", f.Test)).ToArray();
            first.Should().NotEqual(second);

            var labels = new[] { 0, 0, 0, 0, 1, 1, 0, 0, 0, 0 };
            var stratified = sampling.GenerateRepeatedKFolds(10, 2, 2, 1, labels);
            stratified.Should().OnlyContain(f => f.Test.Count(i => labels[i] == 1) == 1);

            Action act = () => sampling.GenerateRepeatedKFolds(10, 5, 0);
            act.Should().Throw<ArgumentException>().WithParameterName("repeats");
        }

        [Fact]
        [Trait("Covers", "GenerateBootstrapSamples")]
        public void GenerateBootstrapSamplesLeaveAboutAThirdOutOfBag()
        {
            var samples = sampling.GenerateBootstrapSamples(1000, 5);

            samples.Should().HaveCount(5);

            foreach (var sample in samples)
            {
                sample.InBag.Should().HaveCount(1000);
                sample.InBag.Intersect(sample.OutOfBag).Should().BeEmpty();
                (sample.OutOfBag.Length / 1000.0).Should().BeApproximately(Math.Exp(-1), 0.05);
                sample.InBag.Distinct().Count().Should().Be(1000 - sample.OutOfBag.Length);
            }

            sampling.GenerateBootstrapSamples(10, 1, 3)[0].InBag.Should().HaveCount(3);
            samples[0].InBag.Should().NotEqual(samples[1].InBag);
            sampling.GenerateBootstrapSamples(1000, 5)[0].InBag.Should().Equal(samples[0].InBag);

            Action act = () => sampling.GenerateBootstrapSamples(10, 1, -1);
            act.Should().Throw<ArgumentException>().WithParameterName("sampleSize");
        }

        [Fact]
        [Trait("Covers", "GeneratePermutation")]
        public void GeneratePermutationIsAShuffleOfAllIndices()
        {
            var permutation = sampling.GeneratePermutation(50, 4);

            permutation.OrderBy(i => i).Should().Equal(Enumerable.Range(0, 50));
            permutation.Should().NotEqual(Enumerable.Range(0, 50));
            sampling.GeneratePermutation(50, 4).Should().Equal(permutation);
            sampling.GeneratePermutation(1).Should().Equal(0);

            Action act = () => sampling.GeneratePermutation(0);
            act.Should().Throw<ArgumentException>().WithParameterName("rowCount");
        }

        [Fact]
        [Trait("Covers", "ComputeClassWeights")]
        public void ComputeClassWeightsBalancedIsNOverClassesTimesCount()
        {
            var labels = Enumerable.Repeat(0, 8).Concat(Enumerable.Repeat(1, 2)).ToArray();
            var result = imbalance.ComputeClassWeights(labels);

            result.Classes.Should().Equal(0, 1);
            result.Counts.Should().Equal(8, 2);
            result.ClassWeights[0].Should().BeApproximately(0.625, Tight);
            result.ClassWeights[1].Should().BeApproximately(2.5, Tight);
            result.RowWeights.Average().Should().BeApproximately(1.0, Tight);
            result.RowWeights[9].Should().BeApproximately(2.5, Tight);
        }

        [Fact]
        [Trait("Covers", "ComputeClassWeights")]
        public void ComputeClassWeightsOtherMethodsAreRescaledToMeanOne()
        {
            var labels = Enumerable.Repeat(0, 8).Concat(Enumerable.Repeat(1, 2)).ToArray();

            var inverse = imbalance.ComputeClassWeights(labels, ClassWeightMethod.InverseFrequency);
            inverse.ClassWeights[0].Should().BeApproximately(0.625, Tight);

            var root = imbalance.ComputeClassWeights(labels, ClassWeightMethod.SquareRootInverse);
            root.ClassWeights[0].Should().BeApproximately(5.0 / 6.0, Tight);
            root.ClassWeights[1].Should().BeApproximately(5.0 / 3.0, Tight);

            var effective = imbalance.ComputeClassWeights(labels, ClassWeightMethod.EffectiveNumber);
            effective.ClassWeights[0].Should().BeApproximately(0.6259371863287853, 1e-9);
            effective.ClassWeights[1].Should().BeApproximately(2.496251254684859, 1e-9);
            effective.RowWeights.Average().Should().BeApproximately(1.0, Tight);

            Action act = () => imbalance.ComputeClassWeights(labels, ClassWeightMethod.EffectiveNumber, 1.0);
            act.Should().Throw<ArgumentException>().WithParameterName("beta");
        }

        [Fact]
        [Trait("Covers", "RandomUnderSample")]
        public void RandomUnderSampleKeepsTheMinorityAndCutsTheMajority()
        {
            var labels = Enumerable.Repeat(0, 90).Concat(Enumerable.Repeat(1, 10)).ToArray();
            var result = imbalance.RandomUnderSample(labels, 1, 5);

            result.Indices.Should().HaveCount(20).And.OnlyHaveUniqueItems().And.BeInAscendingOrder();
            result.Indices.Count(i => labels[i] == 1).Should().Be(10);
            result.CountsBefore.Select(c => c.Count).Should().Equal(90, 10);
            result.CountsAfter.Select(c => c.Count).Should().Equal(10, 10);

            imbalance.RandomUnderSample(labels, 0.5, 5).CountsAfter.Select(c => c.Count).Should().Equal(20, 10);

            Action act = () => imbalance.RandomUnderSample(labels, 0);
            act.Should().Throw<ArgumentException>().WithParameterName("targetRatio");
        }

        [Fact]
        [Trait("Covers", "RandomOverSample")]
        public void RandomOverSampleKeepsEveryOriginalRowAndRepeatsMinorityRows()
        {
            var labels = Enumerable.Repeat(0, 90).Concat(Enumerable.Repeat(1, 10)).ToArray();
            var result = imbalance.RandomOverSample(labels, 1, 5);

            result.Indices.Should().HaveCount(180);
            result.Indices.Distinct().Should().BeEquivalentTo(Enumerable.Range(0, 100));
            result.CountsAfter.Select(c => c.Count).Should().Equal(90, 90);
            result.Indices.Where(i => labels[i] == 1).Should().OnlyContain(i => i >= 90);

            imbalance.RandomOverSample(labels, 0.5, 5).CountsAfter.Select(c => c.Count).Should().Equal(90, 45);
            imbalance.RandomOverSample(labels, 1, 5).Indices.Should().Equal(result.Indices);
        }

        [Fact]
        [Trait("Covers", "GenerateSmoteSamples")]
        public void GenerateSmoteSamplesInterpolatesBetweenMinorityNeighbours()
        {
            var data = new[]
            {
                new[] { 0.0, 0.0 }, new[] { 1.0, 1.0 }, new[] { 2.0, 2.0 },
                new[] { 50.0, 0.0 }, new[] { 51.0, 0.0 }, new[] { 52.0, 0.0 }, new[] { 53.0, 0.0 },
                new[] { 54.0, 0.0 }, new[] { 55.0, 0.0 }
            };

            var labels = new[] { 1, 1, 1, 0, 0, 0, 0, 0, 0 };

            var result = imbalance.GenerateSmoteSamples(data, labels, 1, 1, 0, false, true, 3);

            result.SyntheticRows.Should().HaveCount(3);
            result.SyntheticRows.Should().OnlyContain(r => Math.Abs(r[0] - r[1]) < 1e-12 && r[0] >= 0 && r[0] <= 2);
            result.SourceRows.Should().OnlyContain(i => i <= 2);
            result.NeighbourRows.Should().OnlyContain(i => i <= 2);
            result.SourceRows.Zip(result.NeighbourRows, (s, n) => s != n).Should().OnlyContain(b => b);
            result.Table.Should().HaveCount(12);
            result.Labels.Should().HaveCount(12);
            result.Labels.Skip(9).Should().OnlyContain(l => l == 1);

            var first = imbalance.GenerateSmoteSamples(data, labels, 1, 1, 5, false, false, 3);
            first.SyntheticRows.Should().HaveCount(5);
            first.Table.Should().BeEmpty();

            imbalance.GenerateSmoteSamples(data, labels, 1, 1, 5, false, false, 3).SyntheticRows[0]
                .Should().Equal(first.SyntheticRows[0]);
        }

        [Fact]
        [Trait("Covers", "GenerateSmoteSamples")]
        public void GenerateSmoteSamplesRejectsMissingValuesAndTinyMinority()
        {
            var data = new[] { new[] { 1.0 }, new[] { Nan }, new[] { 3.0 } };

            Action withMissingRow = () => imbalance.GenerateSmoteSamples(data, [1, 1, 0], 1);
            withMissingRow.Should().Throw<ArgumentException>().WithMessage("*Row 1*");

            Action tiny = () =>
                imbalance.GenerateSmoteSamples([[1.0], [2.0]], [1, 0], 1);

            tiny.Should().Throw<ArgumentException>().WithParameterName("minorityLabel");
        }

        [Fact]
        [Trait("Covers", "BootstrapStatisticInterval")]
        public void BootstrapStatisticIntervalOfTheMeanMatchesTheClassicalStandardError()
        {
            var values = Enumerable.Range(1, 20).Select(i => (double)i).ToArray();
            var result = inference.BootstrapStatisticInterval(values);
            var classicalSe = Math.Sqrt(35.0 / 20.0);

            result.Estimate.Should().BeApproximately(10.5, Tight);
            result.StandardError.Should().BeApproximately(classicalSe, 0.1);
            result.Lower.Should().BeApproximately(10.5 - 1.96 * classicalSe, 0.45);
            result.Upper.Should().BeApproximately(10.5 + 1.96 * classicalSe, 0.45);
            result.Bias.Should().BeApproximately(0.0, 0.1);
            result.Resamples.Should().Be(2000);
            result.SampleSize.Should().Be(20);
            inference.BootstrapStatisticInterval(values).Lower.Should().Be(result.Lower);
        }

        [Fact]
        [Trait("Covers", "BootstrapStatisticInterval")]
        public void BootstrapStatisticIntervalAllMethodsBracketTheEstimate()
        {
            var values = Enumerable.Range(1, 20).Select(i => (double)i).ToArray();

            foreach (var method in Enum.GetValues<BootstrapIntervalMethod>())
            {
                var result = inference.BootstrapStatisticInterval(values, ResamplingStatistic.Mean, 0.9, 1000, method);
                result.Lower.Should().BeLessThan(10.5);
                result.Upper.Should().BeGreaterThan(10.5);
                (result.Upper - result.Lower).Should().BeInRange(2.0, 5.0);
            }

            var bca = inference.BootstrapStatisticInterval(values, ResamplingStatistic.Mean, 0.95, 1000,
                BootstrapIntervalMethod.BiasCorrectedAccelerated);

            Math.Abs(bca.Acceleration).Should().BeLessThan(0.05);
            Math.Abs(bca.BiasConstantZ0).Should().BeLessThan(0.3);
        }

        [Fact]
        [Trait("Covers", "BootstrapStatisticInterval")]
        public void BootstrapStatisticIntervalStatisticsConstantsAndValidation()
        {
            var values = new[] { 1.0, 2.0, 3.0, 4.0, 10.0 };

            inference.BootstrapStatisticInterval(values, ResamplingStatistic.Median).Estimate.Should().Be(3.0);
            inference.BootstrapStatisticInterval(values, ResamplingStatistic.Maximum).Estimate.Should().Be(10.0);
            inference.BootstrapStatisticInterval(values, ResamplingStatistic.Minimum).Estimate.Should().Be(1.0);

            inference.BootstrapStatisticInterval(values, ResamplingStatistic.Variance).Estimate.Should()
                .BeApproximately(12.5, Tight);

            inference.BootstrapStatisticInterval(values, ResamplingStatistic.StandardDeviation).Estimate
                .Should().BeApproximately(Math.Sqrt(12.5), Tight);

            inference.BootstrapStatisticInterval(values, ResamplingStatistic.Quantile, 0.95, 500)
                .Estimate.Should().Be(3.0);

            inference.BootstrapStatisticInterval([1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0, 100.0],
                    ResamplingStatistic.TrimmedMean, 0.95, 500).Estimate
                .Should().BeApproximately(5.5, Tight);

            inference.BootstrapStatisticInterval(values, ResamplingStatistic.Skewness).Estimate.Should()
                .BeGreaterThan(0);

            var constant = inference.BootstrapStatisticInterval([5.0, 5.0, 5.0, 5.0]);
            constant.Lower.Should().Be(5.0);
            constant.Upper.Should().Be(5.0);

            var withMissing = inference.BootstrapStatisticInterval([1.0, Nan, 3.0, double.PositiveInfinity]);
            withMissing.MissingDropped.Should().Be(2);
            withMissing.Estimate.Should().Be(2.0);

            Action few = () => inference.BootstrapStatisticInterval([1.0]);
            few.Should().Throw<ArgumentException>().WithParameterName("values");

            Action level = () => inference.BootstrapStatisticInterval(values, ResamplingStatistic.Mean, 1.0);
            level.Should().Throw<ArgumentException>().WithParameterName("confidenceLevel");

            Action resamples = () => inference.BootstrapStatisticInterval(values, ResamplingStatistic.Mean, 0.95, 10);
            resamples.Should().Throw<ArgumentException>().WithParameterName("resamples");
        }

        [Fact]
        [Trait("Covers", "BootstrapTwoSampleDifference")]
        public void BootstrapTwoSampleDifferenceExcludesZeroForSeparatedSamples()
        {
            var first = Enumerable.Range(1, 20).Select(i => (double)i).ToArray();
            var second = Enumerable.Range(11, 20).Select(i => (double)i).ToArray();

            var result = inference.BootstrapTwoSampleDifference(first, second);

            result.Estimate.Should().BeApproximately(-10.0, Tight);
            result.Upper.Should().BeLessThan(0);
            result.StandardError.Should().BeApproximately(Math.Sqrt(2 * 35.0 / 20.0), 0.25);
            result.Lower.Should().BeApproximately(-10 - 1.96 * 1.87, 1.0);

            var same = inference.BootstrapTwoSampleDifference(first, first);
            same.Lower.Should().BeLessThan(0);
            same.Upper.Should().BeGreaterThan(0);

            Action bca = () => inference.BootstrapTwoSampleDifference(first, second, ResamplingStatistic.Mean, 0.95,
                500,
                BootstrapIntervalMethod.BiasCorrectedAccelerated);

            bca.Should().Throw<ArgumentException>().WithParameterName("method");

            Action few = () => inference.BootstrapTwoSampleDifference(first, [1.0]);
            few.Should().Throw<ArgumentException>().WithParameterName("second");
        }

        [Fact]
        [Trait("Covers", "BootstrapCorrelationInterval")]
        public void BootstrapCorrelationIntervalOfAPerfectLineIsDegenerate()
        {
            var x = Enumerable.Range(1, 15).Select(i => (double)i).ToArray();
            var result = inference.BootstrapCorrelationInterval(x, [.. x.Select(v => 3 * v + 2)]);

            result.Estimate.Should().BeApproximately(1.0, 1e-9);
            result.Lower.Should().BeApproximately(1.0, 1e-9);
            result.Upper.Should().BeApproximately(1.0, 1e-9);
        }

        [Fact]
        [Trait("Covers", "BootstrapCorrelationInterval")]
        public void BootstrapCorrelationIntervalMatchesTextbookCorrelationAndBracketsIt()
        {
            var x = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
            var y = new[] { 2.0, 4.0, 5.0, 4.0, 5.0 };

            foreach (var method in new[]
                         { BootstrapIntervalMethod.Percentile, BootstrapIntervalMethod.BiasCorrectedAccelerated })
            {
                var result = inference.BootstrapCorrelationInterval(x, y, CorrelationKind.Pearson, 0.9, 1000, method);
                result.Estimate.Should().BeApproximately(6.0 / Math.Sqrt(60.0), 1e-12);
                result.Lower.Should().BeInRange(-1.0, result.Estimate);
                result.Upper.Should().BeInRange(result.Estimate, 1.0);
            }

            var spearman = inference.BootstrapCorrelationInterval(x, [.. x.Select(v => Math.Exp(v))],
                CorrelationKind.Spearman);
            spearman.Estimate.Should().BeApproximately(1.0, 1e-12);

            Action constant = () => inference.BootstrapCorrelationInterval(x, [1.0, 1.0, 1.0, 1.0, 1.0]);
            constant.Should().Throw<ArgumentException>().WithMessage("*constant*");

            Action pairs = () =>
                inference.BootstrapCorrelationInterval([1.0, Nan, 3.0], [1.0, 2.0, 3.0]);

            pairs.Should().Throw<ArgumentException>().WithMessage("*at least 3*");
        }

        [Fact]
        [Trait("Covers", "JackknifeStatistic")]
        public void JackknifeOfTheMeanHasZeroBiasAndTextbookStandardError()
        {
            var values = new[] { 1.0, 2.0, 3.0, 4.0, 10.0 };
            var result = inference.JackknifeStatistic(values);

            result.Estimate.Should().BeApproximately(4.0, Tight);
            result.LeaveOneOutEstimates.Should().Equal(4.75, 4.5, 4.25, 4.0, 2.5);
            result.Bias.Should().BeApproximately(0.0, 1e-12);
            result.BiasCorrectedEstimate.Should().BeApproximately(4.0, 1e-12);
            result.StandardError.Should().BeApproximately(Math.Sqrt(12.5 / 5.0), 1e-12);
            result.Lower.Should().BeLessThan(4.0);
            result.Upper.Should().BeGreaterThan(4.0);
            result.Upper.Should().BeApproximately(4.0 + 2.7764451051977987 * Math.Sqrt(2.5), 1e-6);
        }

        [Fact]
        [Trait("Covers", "JackknifeStatistic")]
        public void JackknifeOfTheUnbiasedVarianceHasZeroBiasAndValidatesInput()
        {
            var values = new[] { 2.0, 4.0, 4.0, 4.0, 5.0, 5.0, 7.0, 9.0 };
            var result = inference.JackknifeStatistic(values, ResamplingStatistic.Variance);

            result.Bias.Should().BeApproximately(0.0, 1e-9);
            result.LeaveOneOutEstimates.Should().HaveCount(8);

            var withMissing = inference.JackknifeStatistic([1.0, 2.0, Nan, 3.0, 4.0]);
            withMissing.MissingDropped.Should().Be(1);

            Action few = () => inference.JackknifeStatistic([1.0, 2.0]);
            few.Should().Throw<ArgumentException>().WithParameterName("values");
        }

        [Fact]
        [Trait("Covers", "PermutationTestTwoSample")]
        public void PermutationTestTwoSampleExactMatchesHandEnumeration()
        {
            var a = new[] { 1.0, 2.0, 3.0 };
            var b = new[] { 4.0, 5.0, 6.0 };

            var two = inference.PermutationTestTwoSample(a, b);
            two.Exact.Should().BeTrue();
            two.Permutations.Should().Be(20);
            two.Observed.Should().BeApproximately(-3.0, Tight);
            two.PValue.Should().BeApproximately(0.1, Tight);
            two.NullMean.Should().BeApproximately(0.0, 1e-12);

            inference.PermutationTestTwoSample(a, b, PermutationStatistic.DifferenceInMeans, TestAlternative.Less)
                .PValue.Should().BeApproximately(0.05, Tight);
            inference.PermutationTestTwoSample(a, b, PermutationStatistic.DifferenceInMeans, TestAlternative.Greater)
                .PValue.Should().BeApproximately(1.0, Tight);
        }

        [Fact]
        [Trait("Covers", "PermutationTestTwoSample")]
        public void PermutationTestTwoSampleMonteCarloPValueIsBoundedBelowAndOtherStatisticsRun()
        {
            var a = Enumerable.Range(1, 15).Select(i => (double)i).ToArray();
            var b = Enumerable.Range(16, 15).Select(i => (double)i).ToArray();

            var result = inference.PermutationTestTwoSample(a, b, PermutationStatistic.DifferenceInMeans,
                TestAlternative.TwoSided, 10000, 3);

            result.Exact.Should().BeFalse();
            result.Permutations.Should().Be(10000);
            result.PValue.Should().BeApproximately(1.0 / 10001.0, 1e-12);
            result.NullLower95.Should().BeLessThan(0);
            result.NullUpper95.Should().BeGreaterThan(0);

            inference.PermutationTestTwoSample(a, b, PermutationStatistic.DifferenceInMedians).Observed
                .Should().BeApproximately(-15.0, Tight);

            inference.PermutationTestTwoSample([1.0, 2.0, 3.0], [10.0, 20.0, 30.0],
                PermutationStatistic.DifferenceInStandardDeviations).Observed.Should().BeApproximately(-9.0, Tight);

            Action perms = () => inference.PermutationTestTwoSample(a, b, PermutationStatistic.DifferenceInMeans,
                TestAlternative.TwoSided, 5);

            perms.Should().Throw<ArgumentException>().WithParameterName("permutations");

            Action few = () => inference.PermutationTestTwoSample([1.0], b);
            few.Should().Throw<ArgumentException>().WithParameterName("first");
        }

        [Fact]
        [Trait("Covers", "PermutationTestCorrelation")]
        public void PermutationTestCorrelationExactForPerfectAssociation()
        {
            var x = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };

            var two = inference.PermutationTestCorrelation(x, x);
            two.Exact.Should().BeTrue();
            two.Permutations.Should().Be(120);
            two.Observed.Should().BeApproximately(1.0, 1e-12);
            two.PValue.Should().BeApproximately(2.0 / 120.0, 1e-12);

            inference.PermutationTestCorrelation(x, x, CorrelationKind.Pearson, TestAlternative.Greater).PValue
                .Should().BeApproximately(1.0 / 120.0, 1e-12);

            inference.PermutationTestCorrelation(x, x, CorrelationKind.Pearson, TestAlternative.Less).PValue
                .Should().BeApproximately(1.0, 1e-12);
        }

        [Fact]
        [Trait("Covers", "PermutationTestCorrelation")]
        public void PermutationTestCorrelationMonteCarloAndValidation()
        {
            var x = Enumerable.Range(1, 20).Select(i => (double)i).ToArray();

            var strong = inference.PermutationTestCorrelation(x, [.. x.Select(v => v * v)],
                CorrelationKind.Spearman,
                TestAlternative.Greater, 2000, 5);
            strong.Exact.Should().BeFalse();
            strong.PValue.Should().BeApproximately(1.0 / 2001.0, 1e-12);

            var none = inference.PermutationTestCorrelation(x, [.. x.Select(v => v % 2 == 0 ? 1.0 : 0.0)],
                CorrelationKind.Pearson, TestAlternative.TwoSided, 2000, 5);
            none.PValue.Should().BeGreaterThan(0.2);

            Action constant = () => inference.PermutationTestCorrelation(x, new double[20]);
            constant.Should().Throw<ArgumentException>().WithMessage("*constant*");

            Action length = () => inference.PermutationTestCorrelation(x, [1.0]);
            length.Should().Throw<ArgumentException>().WithParameterName("y");
        }

        [Fact]
        [Trait("Covers", "PermutationTestPaired")]
        public void PermutationTestPairedExactSignFlipsMatchHandCount()
        {
            var after = new[] { 0.0, 0.0, 0.0, 0.0 };
            var before = new[] { 1.0, 2.0, 3.0, 4.0 };

            var two = inference.PermutationTestPaired(before, after);
            two.Exact.Should().BeTrue();
            two.Permutations.Should().Be(16);
            two.Observed.Should().BeApproximately(2.5, Tight);
            two.PValue.Should().BeApproximately(2.0 / 16.0, Tight);

            inference.PermutationTestPaired(before, after, TestAlternative.Greater).PValue
                .Should().BeApproximately(1.0 / 16.0, Tight);

            inference.PermutationTestPaired(before, after, TestAlternative.Less).PValue
                .Should().BeApproximately(1.0, Tight);
        }

        [Fact]
        [Trait("Covers", "PermutationTestPaired")]
        public void PermutationTestPairedMonteCarloAndValidation()
        {
            var before = Enumerable.Range(1, 30).Select(i => (double)i).ToArray();
            var after = new double[30];

            var result = inference.PermutationTestPaired(before, after, TestAlternative.Greater, 5000, 2);
            result.Exact.Should().BeFalse();
            result.PValue.Should().BeApproximately(1.0 / 5001.0, 1e-12);

            Action same = () => inference.PermutationTestPaired(before, before);
            same.Should().Throw<ArgumentException>().WithMessage("*Fewer than 2 pairs*");

            Action length = () => inference.PermutationTestPaired(before, [1.0]);
            length.Should().Throw<ArgumentException>().WithParameterName("second");
        }
    }
}