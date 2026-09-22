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

using FluentAssertions;
using Jube.Dictionary.Extensions;
using Xunit;

namespace Jube.Test.Dictionary.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class DoubleCalculationExtensionsTests
    {
        private static void Check(double actual, double expected, bool exact)
        {
            if (double.IsNaN(expected))
            {
                double.IsNaN(actual).Should().BeTrue("the calculation is undefined and must be NaN, not " + actual);
            }
            else if (exact)
            {
                actual.Should().Be(expected);
            }
            else
            {
                actual.Should().BeApproximately(expected, 1e-9);
            }
        }

        [Theory]
        [InlineData(6, 3, 2)]
        [InlineData(1, 4, 0.25)]
        [InlineData(0, 5, 0)]
        [InlineData(5, 0, double.NaN)]
        [InlineData(double.NaN, 1, double.NaN)]
        [InlineData(-6, 3, -2)]
        [InlineData(double.PositiveInfinity, 2, double.NaN)]
        public void RatioOfComputesTheDocumentedValue(double value, double denominator, double expected)
        {
            Check(value.RatioOf(denominator), expected, true);
        }

        [Fact]
        public void RatioOfIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.RatioOf(3)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.RatioOf(3)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.RatioOf(3)).Should().BeFalse();
        }

        [Theory]
        [InlineData(25, 200, 12.5)]
        [InlineData(50, 0, double.NaN)]
        [InlineData(0, 10, 0)]
        [InlineData(300, 200, 150)]
        [InlineData(double.NaN, 10, double.NaN)]
        public void PercentOfComputesTheDocumentedValue(double value, double total, double expected)
        {
            Check(value.PercentOf(total), expected, true);
        }

        [Fact]
        public void PercentOfIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.PercentOf(200)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.PercentOf(200)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.PercentOf(200)).Should().BeFalse();
        }

        [Theory]
        [InlineData(30, 70, 0.3)]
        [InlineData(1, 1, 0.5)]
        [InlineData(0, 0, double.NaN)]
        [InlineData(5, 0, 1)]
        [InlineData(0, 5, 0)]
        [InlineData(-1, 1, double.NaN)]
        public void ShareOfSumWithComputesTheDocumentedValue(double value, double other, double expected)
        {
            Check(value.ShareOfSumWith(other), expected, true);
        }

        [Fact]
        public void ShareOfSumWithIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.ShareOfSumWith(70)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.ShareOfSumWith(70)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.ShareOfSumWith(70)).Should().BeFalse();
        }

        [Theory]
        [InlineData(0.25, 0.75)]
        [InlineData(1, 0)]
        [InlineData(double.NaN, double.NaN)]
        [InlineData(2, -1)]
        public void ComplementComputesTheDocumentedValue(double value, double expected)
        {
            Check(value.Complement(), expected, true);
        }

        [Fact]
        public void ComplementIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.Complement()).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.Complement()).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.Complement()).Should().BeFalse();
        }

        [Theory]
        [InlineData(100, 100, 0)]
        [InlineData(100, 10, 2.302585092994046)]
        [InlineData(10, 100, -2.302585092994046)]
        [InlineData(0, 1, double.NaN)]
        [InlineData(1, 0, double.NaN)]
        [InlineData(-1, 1, double.NaN)]
        public void LogRatioOfComputesTheDocumentedValue(double value, double denominator, double expected)
        {
            Check(value.LogRatioOf(denominator), expected, false);
        }

        [Fact]
        public void LogRatioOfIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.LogRatioOf(100)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.LogRatioOf(100)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.LogRatioOf(100)).Should().BeFalse();
        }

        [Theory]
        [InlineData(10, 5, 3, 6, 4)]
        [InlineData(10, 0, 3, 6, double.NaN)]
        [InlineData(10, 5, 0, 1, double.NaN)]
        [InlineData(10, 5, 1, 0, double.NaN)]
        public void RatioOfRatiosComputesTheDocumentedValue(double value, double denominator, double otherNumerator, double otherDenominator, double expected)
        {
            Check(value.RatioOfRatios(denominator, otherNumerator, otherDenominator), expected, true);
        }

        [Fact]
        public void RatioOfRatiosIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.RatioOfRatios(5, 3, 6)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.RatioOfRatios(5, 3, 6)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.RatioOfRatios(5, 3, 6)).Should().BeFalse();
        }

        [Theory]
        [InlineData(4, 8, 2)]
        [InlineData(0, 5, double.NaN)]
        [InlineData(8, 4, 0.5)]
        public void InverseRatioOfComputesTheDocumentedValue(double value, double other, double expected)
        {
            Check(value.InverseRatioOf(other), expected, true);
        }

        [Fact]
        public void InverseRatioOfIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.InverseRatioOf(8)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.InverseRatioOf(8)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.InverseRatioOf(8)).Should().BeFalse();
        }

        [Theory]
        [InlineData(10, 4, 6)]
        [InlineData(4, 10, -6)]
        [InlineData(double.NaN, 1, double.NaN)]
        public void DifferenceFromComputesTheDocumentedValue(double value, double other, double expected)
        {
            Check(value.DifferenceFrom(other), expected, true);
        }

        [Fact]
        public void DifferenceFromIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.DifferenceFrom(4)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.DifferenceFrom(4)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.DifferenceFrom(4)).Should().BeFalse();
        }

        [Theory]
        [InlineData(4, 10, 6)]
        [InlineData(10, 4, 6)]
        [InlineData(5, 5, 0)]
        public void AbsoluteDifferenceFromComputesTheDocumentedValue(double value, double other, double expected)
        {
            Check(value.AbsoluteDifferenceFrom(other), expected, true);
        }

        [Fact]
        public void AbsoluteDifferenceFromIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.AbsoluteDifferenceFrom(10)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.AbsoluteDifferenceFrom(10)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.AbsoluteDifferenceFrom(10)).Should().BeFalse();
        }

        [Theory]
        [InlineData(150, 50, 100)]
        [InlineData(100, 100, 0)]
        [InlineData(0, 0, 0)]
        [InlineData(0, 10, 200)]
        [InlineData(-50, 50, 200)]
        [InlineData(double.NaN, 1, double.NaN)]
        public void PercentDifferenceFromComputesTheDocumentedValue(double value, double other, double expected)
        {
            Check(value.PercentDifferenceFrom(other), expected, true);
        }

        [Fact]
        public void PercentDifferenceFromIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.PercentDifferenceFrom(50)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.PercentDifferenceFrom(50)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.PercentDifferenceFrom(50)).Should().BeFalse();
        }

        [Theory]
        [InlineData(150, 100, 0.5)]
        [InlineData(50, 100, -0.5)]
        [InlineData(5, 0, double.NaN)]
        [InlineData(0, 0, double.NaN)]
        public void RelativeChangeFromComputesTheDocumentedValue(double value, double previous, double expected)
        {
            Check(value.RelativeChangeFrom(previous), expected, true);
        }

        [Fact]
        public void RelativeChangeFromIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.RelativeChangeFrom(100)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.RelativeChangeFrom(100)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.RelativeChangeFrom(100)).Should().BeFalse();
        }

        [Theory]
        [InlineData(150, 100, 1.5)]
        [InlineData(50, 100, 0.5)]
        [InlineData(5, 0, double.NaN)]
        public void GrowthFactorFromComputesTheDocumentedValue(double value, double previous, double expected)
        {
            Check(value.GrowthFactorFrom(previous), expected, true);
        }

        [Fact]
        public void GrowthFactorFromIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.GrowthFactorFrom(100)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.GrowthFactorFrom(100)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.GrowthFactorFrom(100)).Should().BeFalse();
        }

        [Theory]
        [InlineData(10, 4, 3, 2)]
        [InlineData(4, 10, 3, -2)]
        [InlineData(1, 1, 0, double.NaN)]
        public void SlopeFromComputesTheDocumentedValue(double value, double previous, double elapsed, double expected)
        {
            Check(value.SlopeFrom(previous, elapsed), expected, true);
        }

        [Fact]
        public void SlopeFromIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.SlopeFrom(4, 3)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.SlopeFrom(4, 3)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.SlopeFrom(4, 3)).Should().BeFalse();
        }

        [Theory]
        [InlineData(121, 100, 2, 0.1)]
        [InlineData(100, 100, 5, 0)]
        [InlineData(100, 0, 2, double.NaN)]
        [InlineData(100, 100, 0, double.NaN)]
        [InlineData(-1, 100, 2, double.NaN)]
        public void CompoundGrowthRateComputesTheDocumentedValue(double value, double previous, double periods, double expected)
        {
            Check(value.CompoundGrowthRate(previous, periods), expected, false);
        }

        [Fact]
        public void CompoundGrowthRateIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.CompoundGrowthRate(100, 2)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.CompoundGrowthRate(100, 2)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.CompoundGrowthRate(100, 2)).Should().BeFalse();
        }

        [Theory]
        [InlineData(110, 100, 10)]
        [InlineData(90, 100, -10)]
        [InlineData(5, 0, double.NaN)]
        [InlineData(-90, -100, 10)]
        public void PercentDeviationFromMeanComputesTheDocumentedValue(double value, double mean, double expected)
        {
            Check(value.PercentDeviationFromMean(mean), expected, true);
        }

        [Fact]
        public void PercentDeviationFromMeanIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.PercentDeviationFromMean(100)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.PercentDeviationFromMean(100)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.PercentDeviationFromMean(100)).Should().BeFalse();
        }

        [Theory]
        [InlineData(110, 100, 10, 0.6745)]
        [InlineData(100, 100, 10, 0)]
        [InlineData(1, 1, 0, double.NaN)]
        [InlineData(90, 100, 10, -0.6745)]
        public void ModifiedZScoreComputesTheDocumentedValue(double value, double median, double medianAbsoluteDeviation, double expected)
        {
            Check(value.ModifiedZScore(median, medianAbsoluteDeviation), expected, false);
        }

        [Fact]
        public void ModifiedZScoreIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.ModifiedZScore(100, 10)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.ModifiedZScore(100, 10)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.ModifiedZScore(100, 10)).Should().BeFalse();
        }

        [Theory]
        [InlineData(5, 10, 0.5)]
        [InlineData(5, -10, 0.5)]
        [InlineData(5, 0, double.NaN)]
        public void CoefficientOfVariationComputesTheDocumentedValue(double value, double mean, double expected)
        {
            Check(value.CoefficientOfVariation(mean), expected, true);
        }

        [Fact]
        public void CoefficientOfVariationIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.CoefficientOfVariation(10)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.CoefficientOfVariation(10)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.CoefficientOfVariation(10)).Should().BeFalse();
        }

        [Theory]
        [InlineData(5, 0, 10, 0.5)]
        [InlineData(0, 0, 10, 0)]
        [InlineData(10, 0, 10, 1)]
        [InlineData(15, 0, 10, 1.5)]
        [InlineData(5, 3, 3, double.NaN)]
        public void MinMaxNormaliseComputesTheDocumentedValue(double value, double minimum, double maximum, double expected)
        {
            Check(value.MinMaxNormalise(minimum, maximum), expected, true);
        }

        [Fact]
        public void MinMaxNormaliseIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.MinMaxNormalise(0, 10)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.MinMaxNormalise(0, 10)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.MinMaxNormalise(0, 10)).Should().BeFalse();
        }

        [Theory]
        [InlineData(5, 0, 10, 50)]
        [InlineData(25, 0, 100, 25)]
        [InlineData(5, 3, 3, double.NaN)]
        public void PercentOfRangeComputesTheDocumentedValue(double value, double minimum, double maximum, double expected)
        {
            Check(value.PercentOfRange(minimum, maximum), expected, true);
        }

        [Fact]
        public void PercentOfRangeIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.PercentOfRange(0, 10)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.PercentOfRange(0, 10)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.PercentOfRange(0, 10)).Should().BeFalse();
        }

        [Theory]
        [InlineData(1, new[] { 2d, 3d }, 6)]
        [InlineData(1, new double[0], 1)]
        [InlineData(double.NaN, new[] { 1d }, double.NaN)]
        [InlineData(1, new[] { double.NaN }, double.NaN)]
        public void SumWithComputesTheDocumentedValue(double value, double[] others, double expected)
        {
            Check(value.SumWith(others), expected, true);
        }

        [Fact]
        public void SumWithIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.SumWith(2d, 3d)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.SumWith(2d, 3d)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.SumWith(2d, 3d)).Should().BeFalse();
        }

        [Fact]
        public void SumWithTreatsANullArrayAsNoOthers()
        {
            var withNull = ((double)1).SumWith(null);
            var withNone = ((double)1).SumWith();

            Check(withNull, withNone, true);
        }

        [Theory]
        [InlineData(2, new[] { 4d, 6d }, 4)]
        [InlineData(5, new double[0], 5)]
        [InlineData(double.NaN, new[] { 1d }, double.NaN)]
        public void MeanWithComputesTheDocumentedValue(double value, double[] others, double expected)
        {
            Check(value.MeanWith(others), expected, true);
        }

        [Fact]
        public void MeanWithIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.MeanWith(4d, 6d)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.MeanWith(4d, 6d)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.MeanWith(4d, 6d)).Should().BeFalse();
        }

        [Fact]
        public void MeanWithTreatsANullArrayAsNoOthers()
        {
            var withNull = ((double)2).MeanWith(null);
            var withNone = ((double)2).MeanWith();

            Check(withNull, withNone, true);
        }

        [Theory]
        [InlineData(1, new[] { 5d, 3d }, 5)]
        [InlineData(9, new[] { 5d, 3d }, 9)]
        [InlineData(1, new double[0], 1)]
        [InlineData(double.NaN, new[] { 1d }, double.NaN)]
        [InlineData(1, new[] { double.NaN }, double.NaN)]
        public void MaxWithComputesTheDocumentedValue(double value, double[] others, double expected)
        {
            Check(value.MaxWith(others), expected, true);
        }

        [Fact]
        public void MaxWithIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.MaxWith(5d, 3d)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.MaxWith(5d, 3d)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.MaxWith(5d, 3d)).Should().BeFalse();
        }

        [Fact]
        public void MaxWithTreatsANullArrayAsNoOthers()
        {
            var withNull = ((double)1).MaxWith(null);
            var withNone = ((double)1).MaxWith(null);

            Check(withNull, withNone, true);
        }

        [Theory]
        [InlineData(5, new[] { 1d, 3d }, 1)]
        [InlineData(0, new[] { 1d, 3d }, 0)]
        [InlineData(5, new double[0], 5)]
        [InlineData(double.NaN, new[] { 1d }, double.NaN)]
        public void MinWithComputesTheDocumentedValue(double value, double[] others, double expected)
        {
            Check(value.MinWith(others), expected, true);
        }

        [Fact]
        public void MinWithIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.MinWith(1d, 3d)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.MinWith(1d, 3d)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.MinWith(1d, 3d)).Should().BeFalse();
        }

        [Fact]
        public void MinWithTreatsANullArrayAsNoOthers()
        {
            var withNull = ((double)5).MinWith();
            var withNone = ((double)5).MinWith();

            Check(withNull, withNone, true);
        }

        [Theory]
        [InlineData(1, new[] { 5d, 3d }, 4)]
        [InlineData(2, new double[0], 0)]
        [InlineData(double.NaN, new[] { 1d }, double.NaN)]
        public void SpreadWithComputesTheDocumentedValue(double value, double[] others, double expected)
        {
            Check(value.SpreadWith(others), expected, true);
        }

        [Fact]
        public void SpreadWithIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.SpreadWith(5d, 3d)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.SpreadWith(5d, 3d)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.SpreadWith(5d, 3d)).Should().BeFalse();
        }

        [Fact]
        public void SpreadWithTreatsANullArrayAsNoOthers()
        {
            var withNull = ((double)1).SpreadWith();
            var withNone = ((double)1).SpreadWith();

            Check(withNull, withNone, true);
        }

        [Theory]
        [InlineData(10, 1, 20, 3, 17.5)]
        [InlineData(10, 0, 20, 0, double.NaN)]
        [InlineData(10, 2, 20, 2, 15)]
        public void WeightedMeanWithComputesTheDocumentedValue(double value, double weight, double other, double otherWeight, double expected)
        {
            Check(value.WeightedMeanWith(weight, other, otherWeight), expected, true);
        }

        [Fact]
        public void WeightedMeanWithIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.WeightedMeanWith(1, 20, 3)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.WeightedMeanWith(1, 20, 3)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.WeightedMeanWith(1, 20, 3)).Should().BeFalse();
        }

        [Theory]
        [InlineData(4, 9, 6)]
        [InlineData(-1, 4, double.NaN)]
        [InlineData(0, 5, 0)]
        public void GeometricMeanWithComputesTheDocumentedValue(double value, double other, double expected)
        {
            Check(value.GeometricMeanWith(other), expected, true);
        }

        [Fact]
        public void GeometricMeanWithIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.GeometricMeanWith(9)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.GeometricMeanWith(9)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.GeometricMeanWith(9)).Should().BeFalse();
        }

        [Theory]
        [InlineData(2, 6, 3)]
        [InlineData(0, 0, double.NaN)]
        [InlineData(1, -1, double.NaN)]
        public void HarmonicMeanWithComputesTheDocumentedValue(double value, double other, double expected)
        {
            Check(value.HarmonicMeanWith(other), expected, true);
        }

        [Fact]
        public void HarmonicMeanWithIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.HarmonicMeanWith(6)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.HarmonicMeanWith(6)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.HarmonicMeanWith(6)).Should().BeFalse();
        }

        [Theory]
        [InlineData(5, new[] { 10d, 20d }, 0.25)]
        [InlineData(20, new[] { 10d }, 1)]
        [InlineData(0, new[] { 0d }, double.NaN)]
        public void ShareOfMaxComputesTheDocumentedValue(double value, double[] others, double expected)
        {
            Check(value.ShareOfMax(others), expected, true);
        }

        [Fact]
        public void ShareOfMaxIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.ShareOfMax(10d, 20d)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.ShareOfMax(10d, 20d)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.ShareOfMax(10d, 20d)).Should().BeFalse();
        }

        [Fact]
        public void ShareOfMaxTreatsANullArrayAsNoOthers()
        {
            var withNull = ((double)5).ShareOfMax();
            var withNone = ((double)5).ShareOfMax();

            Check(withNull, withNone, true);
        }

        [Theory]
        [InlineData(1, new[] { 1d }, 0.5)]
        [InlineData(1, new[] { 0d }, 1)]
        [InlineData(1, new[] { 1d, 1d, 1d }, 0.25)]
        [InlineData(0, new[] { 0d }, double.NaN)]
        [InlineData(-1, new[] { 2d }, double.NaN)]
        public void HerfindahlWithComputesTheDocumentedValue(double value, double[] others, double expected)
        {
            Check(value.HerfindahlWith(others), expected, true);
        }

        [Fact]
        public void HerfindahlWithIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.HerfindahlWith(1d)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.HerfindahlWith(1d)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.HerfindahlWith(1d)).Should().BeFalse();
        }

        [Fact]
        public void HerfindahlWithTreatsANullArrayAsNoOthers()
        {
            var withNull = ((double)1).HerfindahlWith();
            var withNone = ((double)1).HerfindahlWith();

            Check(withNull, withNone, true);
        }

        [Theory]
        [InlineData(1, new[] { 1d }, 1)]
        [InlineData(1, new[] { 1d, 1d, 1d }, 2)]
        [InlineData(1, new[] { 0d }, 0)]
        [InlineData(0, new[] { 0d }, double.NaN)]
        public void EntropyOfSharesWithComputesTheDocumentedValue(double value, double[] others, double expected)
        {
            Check(value.EntropyOfSharesWith(others), expected, true);
        }

        [Fact]
        public void EntropyOfSharesWithIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.EntropyOfSharesWith(1d)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.EntropyOfSharesWith(1d)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.EntropyOfSharesWith(1d)).Should().BeFalse();
        }

        [Fact]
        public void EntropyOfSharesWithTreatsANullArrayAsNoOthers()
        {
            var withNull = ((double)1).EntropyOfSharesWith();
            var withNone = ((double)1).EntropyOfSharesWith();

            Check(withNull, withNone, true);
        }

        [Theory]
        [InlineData(30, 100, 70)]
        [InlineData(120, 100, -20)]
        [InlineData(double.NaN, 1, double.NaN)]
        public void HeadroomToComputesTheDocumentedValue(double value, double limit, double expected)
        {
            Check(value.HeadroomTo(limit), expected, true);
        }

        [Fact]
        public void HeadroomToIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.HeadroomTo(100)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.HeadroomTo(100)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.HeadroomTo(100)).Should().BeFalse();
        }

        [Theory]
        [InlineData(120, 100, 20)]
        [InlineData(80, 100, 0)]
        [InlineData(double.NaN, 1, double.NaN)]
        public void ExcessOverComputesTheDocumentedValue(double value, double limit, double expected)
        {
            Check(value.ExcessOver(limit), expected, true);
        }

        [Fact]
        public void ExcessOverIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.ExcessOver(100)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.ExcessOver(100)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.ExcessOver(100)).Should().BeFalse();
        }

        [Theory]
        [InlineData(30, 100, 70)]
        [InlineData(120, 100, 0)]
        [InlineData(double.NaN, 1, double.NaN)]
        public void ShortfallBelowComputesTheDocumentedValue(double value, double minimum, double expected)
        {
            Check(value.ShortfallBelow(minimum), expected, true);
        }

        [Fact]
        public void ShortfallBelowIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.ShortfallBelow(100)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.ShortfallBelow(100)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.ShortfallBelow(100)).Should().BeFalse();
        }

        [Theory]
        [InlineData(9500, 10000, 500)]
        [InlineData(10500, 10000, 500)]
        [InlineData(10000, 10000, 0)]
        public void DistanceToThresholdComputesTheDocumentedValue(double value, double threshold, double expected)
        {
            Check(value.DistanceToThreshold(threshold), expected, true);
        }

        [Fact]
        public void DistanceToThresholdIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.DistanceToThreshold(10000)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.DistanceToThreshold(10000)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.DistanceToThreshold(10000)).Should().BeFalse();
        }

        [Theory]
        [InlineData(9500, 10000, 5)]
        [InlineData(10000, 10000, 0)]
        [InlineData(10500, 10000, -5)]
        [InlineData(5, 0, double.NaN)]
        public void PercentBelowThresholdComputesTheDocumentedValue(double value, double threshold, double expected)
        {
            Check(value.PercentBelowThreshold(threshold), expected, true);
        }

        [Fact]
        public void PercentBelowThresholdIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.PercentBelowThreshold(10000)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.PercentBelowThreshold(10000)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.PercentBelowThreshold(10000)).Should().BeFalse();
        }

        [Theory]
        [InlineData(30, 10, 0.5)]
        [InlineData(10, 30, -0.5)]
        [InlineData(5, 5, 0)]
        [InlineData(0, 0, double.NaN)]
        public void ImbalanceWithComputesTheDocumentedValue(double value, double other, double expected)
        {
            Check(value.ImbalanceWith(other), expected, true);
        }

        [Fact]
        public void ImbalanceWithIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.ImbalanceWith(10)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.ImbalanceWith(10)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.ImbalanceWith(10)).Should().BeFalse();
        }

        [Theory]
        [InlineData(100, 90, 0.1)]
        [InlineData(100, 100, 0)]
        [InlineData(0, 5, double.NaN)]
        [InlineData(100, 120, -0.2)]
        public void RetentionAfterComputesTheDocumentedValue(double value, double outflow, double expected)
        {
            Check(value.RetentionAfter(outflow), expected, true);
        }

        [Fact]
        public void RetentionAfterIsNaNForANaNValueAndNeverInfinite()
        {
            double.IsNaN(double.NaN.RetentionAfter(90)).Should().BeTrue();
            double.IsInfinity(double.PositiveInfinity.RetentionAfter(90)).Should().BeFalse();
            double.IsInfinity(double.NegativeInfinity.RetentionAfter(90)).Should().BeFalse();
        }
    }
}
