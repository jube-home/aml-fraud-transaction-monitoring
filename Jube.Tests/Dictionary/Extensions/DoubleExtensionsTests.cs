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
using Jube.Dictionary.Extensions;
using Xunit;

namespace Jube.Test.Dictionary.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class DoubleExtensionsTests
    {
        [Theory]
        [InlineData(5.5, 5.5)]
        [InlineData(-5.5, 5.5)]
        [InlineData(0d, 0d)]
        public void AbsReturnsTheAbsoluteValue(double input, double expected)
        {
            input.Abs().Should().Be(expected);
        }

        [Fact]
        public void AbsOfNegativeInfinityIsPositiveInfinity()
        {
            double.NegativeInfinity.Abs().Should().Be(double.PositiveInfinity);
        }

        [Fact]
        public void AcosOfOneIsZero()
        {
            1d.Acos().Should().Be(0d);
        }

        [Fact]
        public void AcosOfAValueOutsideMinusOneToOneIsNaN()
        {
            2d.Acos().Should().Be(double.NaN);
            double.IsNaN(2d.Acos()).Should().BeTrue();
        }

        [Fact]
        public void AsinOfZeroIsZero()
        {
            0d.Asin().Should().Be(0d);
        }

        [Fact]
        public void AsinOfAValueOutsideMinusOneToOneIsNaN()
        {
            double.IsNaN(2d.Asin()).Should().BeTrue();
        }

        [Fact]
        public void Atan2MatchesMathAtan2ForAPointInEachQuadrant()
        {
            1d.Atan2(1d).Should().Be(Math.Atan2(1d, 1d));
            1d.Atan2(-1d).Should().Be(Math.Atan2(1d, -1d));
            (-1d).Atan2(-1d).Should().Be(Math.Atan2(-1d, -1d));
        }

        [Fact]
        public void AtanOfZeroIsZero()
        {
            0d.Atan().Should().Be(0d);
        }

        [Fact]
        public void AtanOfPositiveInfinityIsHalfPi()
        {
            double.PositiveInfinity.Atan().Should().BeApproximately(Math.PI / 2, 1e-12);
        }

        [Theory]
        [InlineData(5d, 1d, 10d, true)]
        [InlineData(1d, 1d, 10d, false)]
        [InlineData(10d, 1d, 10d, false)]
        [InlineData(0d, 1d, 10d, false)]
        public void BetweenIsExclusiveOfBothBounds(double value, double min, double max, bool expected)
        {
            value.Between(min, max).Should().Be(expected);
        }

        [Theory]
        [InlineData(1.1, 2d)]
        [InlineData(-1.1, -1d)]
        [InlineData(2d, 2d)]
        public void CeilingRoundsUpToTheSmallestIntegralValueNotLessThanTheInput(double input, double expected)
        {
            input.Ceiling().Should().Be(expected);
        }

        [Fact]
        public void CosOfZeroIsOne()
        {
            0d.Cos().Should().Be(1d);
        }

        [Fact]
        public void CosOfPiIsMinusOne()
        {
            Math.PI.Cos().Should().BeApproximately(-1d, 1e-12);
        }

        [Fact]
        public void CoshOfZeroIsOne()
        {
            0d.Cosh().Should().Be(1d);
        }

        [Fact]
        public void ExpOfZeroIsOne()
        {
            0d.Exp().Should().Be(1d);
        }

        [Fact]
        public void ExpOfOneIsE()
        {
            1d.Exp().Should().BeApproximately(Math.E, 1e-12);
        }

        [Theory]
        [InlineData(1.9, 1d)]
        [InlineData(-1.1, -2d)]
        [InlineData(2d, 2d)]
        public void FloorRoundsDownToTheLargestIntegralValueNotGreaterThanTheInput(double input, double expected)
        {
            input.Floor().Should().Be(expected);
        }

        [Fact]
        public void FromDaysConvertsToATimeSpanOfThatManyDays()
        {
            2d.FromDays().Should().Be(TimeSpan.FromDays(2));
        }

        [Fact]
        public void FromHoursConvertsToATimeSpanOfThatManyHours()
        {
            3d.FromHours().Should().Be(TimeSpan.FromHours(3));
        }

        [Fact]
        public void FromMillisecondsConvertsToATimeSpanOfThatManyMilliseconds()
        {
            1500d.FromMilliseconds().Should().Be(TimeSpan.FromMilliseconds(1500));
        }

        [Fact]
        public void FromMinutesConvertsToATimeSpanOfThatManyMinutes()
        {
            90d.FromMinutes().Should().Be(TimeSpan.FromMinutes(90));
        }

        [Fact]
        public void FromOaDateConvertsAnOleAutomationDateToADateTime()
        {
            var result = 0d.FromOADate();

            result.Should().Be(new DateTime(1899, 12, 30));
        }

        [Fact]
        public void FromSecondsConvertsToATimeSpanOfThatManySeconds()
        {
            45d.FromSeconds().Should().Be(TimeSpan.FromSeconds(45));
        }

        [Fact]
        public void IeeeRemainderFollowsBankersRoundingOfTheQuotientNotTruncatedDivisionRemainder()
        {
            3d.IEEERemainder(2d).Should().Be(-1d);
        }

        [Fact]
        public void IeeeRemainderOfDivisionByZeroIsNaN()
        {
            double.IsNaN(5d.IEEERemainder(0d)).Should().BeTrue();
        }

        [Theory]
        [InlineData(2d, new[] { 1d, 2d, 3d }, true)]
        [InlineData(9d, new[] { 1d, 2d, 3d }, false)]
        public void InChecksMembershipAgainstTheProvidedValues(double input, double[] values, bool expected)
        {
            input.In(values).Should().Be(expected);
        }

        [Theory]
        [InlineData(2d, new[] { 1d, 2d, 3d }, false)]
        [InlineData(9d, new[] { 1d, 2d, 3d }, true)]
        public void NotInIsTheInverseOfIn(double input, double[] values, bool expected)
        {
            input.NotIn(values).Should().Be(expected);
        }

        [Theory]
        [InlineData(1d, 1d, 10d, true)]
        [InlineData(10d, 1d, 10d, true)]
        [InlineData(5d, 1d, 10d, true)]
        [InlineData(0d, 1d, 10d, false)]
        [InlineData(11d, 1d, 10d, false)]
        public void InRangeIsInclusiveOfBothBoundsUnlikeBetween(double value, double min, double max, bool expected)
        {
            value.InRange(min, max).Should().Be(expected);
        }

        [Theory]
        [InlineData(double.PositiveInfinity, true)]
        [InlineData(double.NegativeInfinity, true)]
        [InlineData(1d, false)]
        [InlineData(double.NaN, false)]
        public void IsInfinityDetectsEitherSignOfInfinity(double input, bool expected)
        {
            input.IsInfinity().Should().Be(expected);
        }

        [Theory]
        [InlineData(double.NaN, true)]
        [InlineData(1d, false)]
        [InlineData(double.PositiveInfinity, false)]
        public void IsNaNDetectsNotANumber(double input, bool expected)
        {
            input.IsNaN().Should().Be(expected);
        }

        [Theory]
        [InlineData(double.NegativeInfinity, true)]
        [InlineData(double.PositiveInfinity, false)]
        [InlineData(1d, false)]
        public void IsNegativeInfinityOnlyDetectsTheNegativeCase(double input, bool expected)
        {
            input.IsNegativeInfinity().Should().Be(expected);
        }

        [Theory]
        [InlineData(double.PositiveInfinity, true)]
        [InlineData(double.NegativeInfinity, false)]
        [InlineData(1d, false)]
        public void IsPositiveInfinityOnlyDetectsThePositiveCase(double input, bool expected)
        {
            input.IsPositiveInfinity().Should().Be(expected);
        }

        [Fact]
        public void Log10OfOneIsZero()
        {
            1d.Log10().Should().Be(0d);
        }

        [Fact]
        public void Log10OfOneHundredIsTwo()
        {
            100d.Log10().Should().BeApproximately(2d, 1e-12);
        }

        [Fact]
        public void Log10OfZeroIsNegativeInfinity()
        {
            0d.Log10().Should().Be(double.NegativeInfinity);
        }

        [Fact]
        public void Log10OfANegativeNumberIsNaN()
        {
            double.IsNaN((-1d).Log10()).Should().BeTrue();
        }

        [Fact]
        public void LogOfEIsOne()
        {
            Math.E.Log().Should().BeApproximately(1d, 1e-12);
        }

        [Fact]
        public void LogWithAnExplicitBaseMatchesThatBasesLogarithm()
        {
            8d.Log(2d).Should().BeApproximately(3d, 1e-12);
        }

        [Theory]
        [InlineData(1d, 5d, 5d)]
        [InlineData(5d, 1d, 5d)]
        [InlineData(-5d, -1d, -1d)]
        public void MaxReturnsTheLargerOfTwoValues(double a, double b, double expected)
        {
            a.Max(b).Should().Be(expected);
        }

        [Theory]
        [InlineData(1d, 5d, 1d)]
        [InlineData(5d, 1d, 1d)]
        [InlineData(-5d, -1d, -5d)]
        public void MinReturnsTheSmallerOfTwoValues(double a, double b, double expected)
        {
            a.Min(b).Should().Be(expected);
        }

        [Fact]
        public void MaxWithNaNAlwaysReturnsNaN()
        {
            double.IsNaN(1d.Max(double.NaN)).Should().BeTrue();
        }

        [Theory]
        [InlineData(2d, 3d, 8d)]
        [InlineData(2d, 0d, 1d)]
        [InlineData(4d, 0.5, 2d)]
        public void PowRaisesTheFirstOperandToTheSecond(double x, double y, double expected)
        {
            x.Pow(y).Should().BeApproximately(expected, 1e-12);
        }

        [Theory]
        [InlineData(2.5, 2d)]
        [InlineData(3.5, 4d)]
        [InlineData(2.4, 2d)]
        public void RoundWithNoDigitsUsesBankersRoundingOnMidpoints(double input, double expected)
        {
            input.Round().Should().Be(expected);
        }

        [Fact]
        public void RoundWithDigitsRoundsToThatManyFractionalDigits()
        {
            3.14159.Round(2).Should().Be(3.14);
        }

        [Theory]
        [InlineData(2.5, MidpointRounding.AwayFromZero, 3d)]
        [InlineData(2.5, MidpointRounding.ToEven, 2d)]
        public void RoundWithAMidpointRoundingModeHonoursThatMode(double input, MidpointRounding mode, double expected)
        {
            input.Round(mode).Should().Be(expected);
        }

        [Fact]
        public void RoundWithDigitsAndModeCombinesBothBehaviours()
        {
            2.345.Round(2, MidpointRounding.AwayFromZero).Should().Be(2.35);
        }

        [Theory]
        [InlineData(5d, 1)]
        [InlineData(-5d, -1)]
        [InlineData(0d, 0)]
        public void SignReturnsMinusOneZeroOrOne(double input, int expected)
        {
            input.Sign().Should().Be(expected);
        }

        [Fact]
        public void SinOfZeroIsZero()
        {
            0d.Sin().Should().Be(0d);
        }

        [Fact]
        public void SinOfHalfPiIsOne()
        {
            (Math.PI / 2).Sin().Should().BeApproximately(1d, 1e-12);
        }

        [Fact]
        public void SinhOfZeroIsZero()
        {
            0d.Sinh().Should().Be(0d);
        }

        [Theory]
        [InlineData(4d, 2d)]
        [InlineData(9d, 3d)]
        [InlineData(0d, 0d)]
        public void SqrtReturnsThePositiveSquareRoot(double input, double expected)
        {
            input.Sqrt().Should().Be(expected);
        }

        [Fact]
        public void SqrtOfANegativeNumberIsNaN()
        {
            double.IsNaN((-4d).Sqrt()).Should().BeTrue();
        }

        [Fact]
        public void TanOfZeroIsZero()
        {
            0d.Tan().Should().Be(0d);
        }

        [Fact]
        public void TanhOfZeroIsZero()
        {
            0d.Tanh().Should().Be(0d);
        }

        [Fact]
        public void TanhSaturatesToPlusOrMinusOneAtInfinity()
        {
            double.PositiveInfinity.Tanh().Should().Be(1d);
            double.NegativeInfinity.Tanh().Should().Be(-1d);
        }

        [Theory]
        [InlineData(1.005, 1.0)]
        [InlineData(1.115, 1.12)]
        [InlineData(2d, 2d)]
        public void ToMoneyRoundsToTwoDecimalPlacesUsingBankersRoundingJustLikeMathRound(double input, double expected)
        {
            input.ToMoney().Should().Be(expected);
        }

        [Theory]
        [InlineData(1.9, 1d)]
        [InlineData(-1.9, -1d)]
        [InlineData(0d, 0d)]
        public void TruncateDiscardsTheFractionalPartTowardZero(double input, double expected)
        {
            input.Truncate().Should().Be(expected);
        }

        [Fact]
        public void HaversineDistanceKilometersOfAPointToItselfIsZero()
        {
            51.5.HaversineDistanceKilometers(-0.1, 51.5, -0.1).Should().BeApproximately(0, 0.0001);
        }

        [Fact]
        public void HaversineDistanceKilometersIsSymmetric()
        {
            var ab = 51.5074.HaversineDistanceKilometers(-0.1278, 40.7128, -74.0060);
            var ba = 40.7128.HaversineDistanceKilometers(-74.0060, 51.5074, -0.1278);

            ab.Should().BeApproximately(ba, 0.0001);
        }

        [Fact]
        public void HaversineDistanceKilometersOfOneDegreeOfLatitudeIsAboutOneHundredElevenKilometers()
        {
            0d.HaversineDistanceKilometers(0, 1, 0).Should().BeApproximately(111.2, 0.5);
        }

        [Fact]
        public void HaversineDistanceMilesAgreesWithKilometersUsingTheStandardConversionFactor()
        {
            var km = 40.7128.HaversineDistanceKilometers(-74.0060, 34.0522, -118.2437);
            var miles = 40.7128.HaversineDistanceMiles(-74.0060, 34.0522, -118.2437);

            miles.Should().BeApproximately(km * 0.621371, 0.5);
        }

        [Fact]
        public void ImpliedTravelSpeedKmhDividesHaversineDistanceByHoursElapsed()
        {
            var speed = 0d.ImpliedTravelSpeedKmh(0, 1, 0, 2.0);
            var distance = 0d.HaversineDistanceKilometers(0, 1, 0);

            speed.Should().BeApproximately(distance / 2.0, 0.0001);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ImpliedTravelSpeedKmhWithZeroOrNegativeHoursReturnsPositiveInfinity(double hoursElapsed)
        {
            0d.ImpliedTravelSpeedKmh(0, 1, 0, hoursElapsed).Should().Be(double.PositiveInfinity);
        }

        [Theory]
        [InlineData(5, 0, 10, 5)]
        [InlineData(-5, 0, 10, 0)]
        [InlineData(15, 0, 10, 10)]
        public void ClampRestrictsTheValueToTheGivenRange(double input, double min, double max, double expected)
        {
            input.Clamp(min, max).Should().Be(expected);
        }

        [Theory]
        [InlineData(0, 10, 0.5, 5)]
        [InlineData(0, 10, 0, 0)]
        [InlineData(0, 10, 1, 10)]
        [InlineData(10, 0, 0.25, 7.5)]
        public void LerpInterpolatesBetweenThisAndEnd(double start, double end, double amount, double expected)
        {
            start.Lerp(end, amount).Should().Be(expected);
        }

        [Theory]
        [InlineData(110, 100, 10)]
        [InlineData(90, 100, -10)]
        [InlineData(100, 100, 0)]
        public void PercentageChangeFromComputesRelativeChange(double current, double previous, double expected)
        {
            current.PercentageChangeFrom(previous).Should().BeApproximately(expected, 0.0001);
        }

        [Fact]
        public void PercentageChangeFromZeroPreviousValueIsPositiveInfinityWhenCurrentIsPositive()
        {
            100d.PercentageChangeFrom(0).Should().Be(double.PositiveInfinity);
        }

        [Fact]
        public void PercentageChangeFromZeroToZeroIsNaN()
        {
            double.IsNaN(0d.PercentageChangeFrom(0)).Should().BeTrue();
        }

        [Theory]
        [InlineData(100, 100, 15, 0)]
        [InlineData(115, 100, 15, 1)]
        [InlineData(70, 100, 15, -2)]
        public void ZScoreMeasuresStandardDeviationsFromTheMean(double value, double mean, double stdDev,
            double expected)
        {
            value.ZScore(mean, stdDev).Should().BeApproximately(expected, 0.0001);
        }

        [Theory]
        [InlineData(5000, 1000, true)]
        [InlineData(5050, 1000, false)]
        [InlineData(100, 100, true)]
        [InlineData(150, 100, false)]
        [InlineData(0, 100, true)]
        [InlineData(-5000, 1000, true)]
        [InlineData(-5050, 1000, false)]
        public void IsRoundAmountChecksExactMultiples(double amount, double nearest, bool expected)
        {
            amount.IsRoundAmount(nearest).Should().Be(expected);
        }

        [Fact]
        public void IsRoundAmountWithNonPositiveNearestReturnsFalse()
        {
            5000d.IsRoundAmount(0).Should().BeFalse();
            5000d.IsRoundAmount(-100).Should().BeFalse();
        }

        [Theory]
        [InlineData(9500, 10000, 10, true)]
        [InlineData(9000, 10000, 10, true)]
        [InlineData(8999, 10000, 10, false)]
        [InlineData(10000, 10000, 10, false)]
        [InlineData(10500, 10000, 10, false)]
        public void IsJustBelowThresholdFlagsAmountsWithinTheMarginBelowTheThreshold(double amount, double threshold,
            double marginPercent, bool expected)
        {
            amount.IsJustBelowThreshold(threshold, marginPercent).Should().Be(expected);
        }

        [Theory]
        [InlineData(51.5, -0.1, 51.5, -0.1, 1, true)]
        [InlineData(0, 0, 0, 1, 50, false)]
        public void IsWithinRadiusKmDelegatesToHaversineDistance(double lat1, double lon1, double lat2, double lon2,
            double radiusKm, bool expected)
        {
            lat1.IsWithinRadiusKm(lon1, lat2, lon2, radiusKm).Should().Be(expected);
        }

        [Theory]
        [InlineData(5.0, 5)]
        [InlineData(4567d, 4)]
        [InlineData(0.0456, 4)]
        [InlineData(-0.0089, 8)]
        [InlineData(0d, 0)]
        [InlineData(9.999, 9)]
        [InlineData(double.PositiveInfinity, 0)]
        [InlineData(double.NegativeInfinity, 0)]
        [InlineData(double.NaN, 0)]
        public void LeadingSignificantDigitExtractsTheFirstNonZeroDigit(double input, int expected)
        {
            input.LeadingSignificantDigit().Should().Be(expected);
        }

        [Theory]
        [InlineData(102, 100, 5, true)]
        [InlineData(95, 100, 5, true)]
        [InlineData(80, 100, 5, false)]
        [InlineData(0, 0, 5, true)]
        [InlineData(5, 0, 5, false)]
        public void IsWithinPercentOfChecksTheAbsolutePercentageChange(double value, double target, double percent,
            bool expected)
        {
            value.IsWithinPercentOf(target, percent).Should().Be(expected);
        }
    }
}