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
using System.Data;
using System.Drawing;
using System.Net;
using FluentAssertions;
using Jube.Dictionary.Extensions;
using Xunit;

namespace Jube.Test.Dictionary.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class Int32ExtensionsTests
    {
        [Theory]
        [InlineData(5, 5)]
        [InlineData(-5, 5)]
        [InlineData(0, 0)]
        public void AbsReturnsTheAbsoluteValue(int input, int expected)
        {
            input.Abs().Should().Be(expected);
        }

        [Fact]
        public void AbsOfIntMinValueOverflowsJustLikeMathAbs()
        {
            var act = () => int.MinValue.Abs();

            act.Should().Throw<OverflowException>();
        }

        [Theory]
        [InlineData(5, 1, 10, true)]
        [InlineData(1, 1, 10, false)]
        [InlineData(10, 1, 10, false)]
        [InlineData(0, 1, 10, false)]
        public void BetweenIsExclusiveOfBothBoundaries(int value, int min, int max, bool expected)
        {
            value.Between(min, max).Should().Be(expected);
        }

        [Fact]
        public void BetweenWorksAcrossAWideValueRangeIncludingNearIntBoundaries()
        {
            0.Between(int.MinValue, int.MaxValue).Should().BeTrue();
            int.MinValue.Between(int.MinValue, int.MaxValue).Should().BeFalse("the lower boundary itself is excluded");
            int.MaxValue.Between(int.MinValue, int.MaxValue).Should().BeFalse("the upper boundary itself is excluded");
        }

        [Theory]
        [InlineData(3, 4, 12L)]
        [InlineData(-3, 4, -12L)]
        [InlineData(0, 100, 0L)]
        public void BigMulProducesTheFullLongProductOfTwoInts(int a, int b, long expected)
        {
            a.BigMul(b).Should().Be(expected);
        }

        [Fact]
        public void BigMulHandlesAProductThatWouldOverflowA32BitInt()
        {
            int.MaxValue.BigMul(2).Should().Be((long)int.MaxValue * 2);
        }

        [Fact]
        public void ConvertFromUtf32ProducesASingleCharStringForABmpCodePoint()
        {
            ((int)'A').ConvertFromUtf32().Should().Be("A");
        }

        [Fact]
        public void ConvertFromUtf32ProducesASurrogatePairStringForASupplementaryCodePoint()
        {
            const string emoji = "😀";
            var codePoint = char.ConvertToUtf32(emoji[0], emoji[1]);

            codePoint.ConvertFromUtf32().Should().Be(emoji);
        }

        [Fact]
        public void ConvertFromUtf32ThrowsForAnOutOfRangeCodePoint()
        {
            var act = () => (-1).ConvertFromUtf32();

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void DaysConvertsToATimeSpanOfThatManyDays()
        {
            7.Days().Should().Be(TimeSpan.FromDays(7));
        }

        [Fact]
        public void HoursConvertsToATimeSpanOfThatManyHours()
        {
            3.Hours().Should().Be(TimeSpan.FromHours(3));
        }

        [Fact]
        public void MinutesConvertsToATimeSpanOfThatManyMinutes()
        {
            45.Minutes().Should().Be(TimeSpan.FromMinutes(45));
        }

        [Fact]
        public void SecondsConvertsToATimeSpanOfThatManySeconds()
        {
            30.Seconds().Should().Be(TimeSpan.FromSeconds(30));
        }

        [Fact]
        public void MillisecondsConvertsToATimeSpanOfThatManyMilliseconds()
        {
            500.Milliseconds().Should().Be(TimeSpan.FromMilliseconds(500));
        }

        [Fact]
        public void WeeksConvertsToATimeSpanOfThatManyWeeksExpressedAsDaysTimesSeven()
        {
            2.Weeks().Should().Be(TimeSpan.FromDays(14));
        }

        [Theory]
        [InlineData(2024, 2, 29)]
        [InlineData(2023, 2, 28)]
        [InlineData(2024, 1, 31)]
        [InlineData(2024, 4, 30)]
        public void DaysInMonthMatchesTheBclImplementationIncludingLeapFebruary(int year, int month, int expected)
        {
            year.DaysInMonth(month).Should().Be(expected);
        }

        [Theory]
        [InlineData(10, 3, 3, 1)]
        [InlineData(-10, 3, -3, -1)]
        [InlineData(9, 3, 3, 0)]
        public void DivRemReturnsTheQuotientAndOutputsTheRemainder(int a, int b, int expectedQuotient,
            int expectedRemainder)
        {
            var quotient = a.DivRem(b, out var remainder);

            quotient.Should().Be(expectedQuotient);
            remainder.Should().Be(expectedRemainder);
        }

        [Fact]
        public void DivRemThrowsForDivisionByZero()
        {
            var act = () => 10.DivRem(0, out _);

            act.Should().Throw<DivideByZeroException>();
        }

        [Theory]
        [InlineData(2, 10, true)]
        [InlineData(3, 10, false)]
        [InlineData(1, 7, true)]
        public void FactorOfChecksWhetherThisValueIsAFactorOfTheArgument(int divisor, int factorNumer, bool expected)
        {
            divisor.FactorOf(factorNumer).Should().Be(expected);
        }

        [Fact]
        public void FactorOfThrowsWhenThisValueIsZeroBecauseItDividesByZero()
        {
            var act = () => 0.FactorOf(10);

            act.Should().Throw<DivideByZeroException>();
        }

        [Fact]
        public void FromArgbBuildsAColorFromA32BitArgbValue()
        {
            var color = unchecked((int)0xFFFF0000).FromArgb();

            color.A.Should().Be(255);
            color.R.Should().Be(255);
            color.G.Should().Be(0);
            color.B.Should().Be(0);
        }

        [Fact]
        public void FromArgbWithExplicitRedGreenBlueOverridesTheColorComponents()
        {
            var color = 0.FromArgb(10, 20, 30);

            color.R.Should().Be(10);
            color.G.Should().Be(20);
            color.B.Should().Be(30);
        }

        [Fact]
        public void FromArgbWithABaseColorAppliesTheNewAlphaToThatColor()
        {
            var color = 128.FromArgb(Color.Blue);

            color.B.Should().Be(255);
            color.A.Should().Be(128);
        }

        [Fact]
        public void FromArgbWithGreenAndBlueUsesFullyOpaqueAlpha()
        {
            var color = 0.FromArgb(20, 30);

            color.G.Should().Be(20);
            color.B.Should().Be(30);
            color.A.Should().Be(255);
        }

        [Fact]
        public void FromOleTranslatesAnOleColorValue()
        {
            var color = 0x0000FF.FromOle();

            color.R.Should().Be(255);
            color.G.Should().Be(0);
            color.B.Should().Be(0);
        }

        [Fact]
        public void FromWin32TranslatesAWindowsColorValueTheSameWayAsFromOle()
        {
            var color = 0x0000FF.FromWin32();

            color.R.Should().Be(255);
        }

        [Fact]
        public void GetBytesReturnsAFourByteArrayMatchingBitConverter()
        {
            42.GetBytes().Should().Equal(BitConverter.GetBytes(42));
        }

        [Fact]
        public void HostToNetworkOrderThenNetworkToHostOrderRoundTrips()
        {
            const int original = 0x01020304;

            var network = original.HostToNetworkOrder();
            var backToHost = network.NetworkToHostOrder();

            backToHost.Should().Be(original);
        }

        [Fact]
        public void HostToNetworkOrderMatchesIpAddressImplementation()
        {
            305419896.HostToNetworkOrder().Should().Be(IPAddress.HostToNetworkOrder(305419896));
        }

        [Theory]
        [InlineData(5, new[] { 1, 3, 5 }, true)]
        [InlineData(7, new[] { 1, 3, 5 }, false)]
        public void InChecksMembershipAgainstTheProvidedValues(int input, int[] values, bool expected)
        {
            input.In(values).Should().Be(expected);
        }

        [Theory]
        [InlineData(5, new[] { 1, 3, 5 }, false)]
        [InlineData(7, new[] { 1, 3, 5 }, true)]
        public void NotInIsTheInverseOfIn(int input, int[] values, bool expected)
        {
            input.NotIn(values).Should().Be(expected);
        }

        [Theory]
        [InlineData(1, 1, 10, true)]
        [InlineData(10, 1, 10, true)]
        [InlineData(0, 1, 10, false)]
        [InlineData(11, 1, 10, false)]
        public void InRangeIsInclusiveOfBothBoundariesUnlikeBetween(int value, int min, int max, bool expected)
        {
            value.InRange(min, max).Should().Be(expected);
        }

        [Theory]
        [InlineData(4, true)]
        [InlineData(3, false)]
        [InlineData(0, true)]
        [InlineData(-4, true)]
        [InlineData(-3, false)]
        public void IsEvenIdentifiesEvenNumbersIncludingZeroAndNegatives(int input, bool expected)
        {
            input.IsEven().Should().Be(expected);
        }

        [Theory]
        [InlineData(3, true)]
        [InlineData(4, false)]
        [InlineData(0, false)]
        [InlineData(-3, true)]
        [InlineData(-4, false)]
        public void IsOddIdentifiesOddNumbersIncludingNegativesViaCsharpsRemainderOperator(int input, bool expected)
        {
            input.IsOdd().Should().Be(expected);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(-1)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        public void IsEvenAndIsOddAreAlwaysExactOppositesForAnyInt(int input)
        {
            input.IsEven().Should().Be(!input.IsOdd());
        }

        [Theory]
        [InlineData(2024, true)]
        [InlineData(2023, false)]
        [InlineData(2000, true)]
        [InlineData(1900, false)]
        public void IsLeapYearMatchesTheBclRulesIncludingTheCenturyException(int year, bool expected)
        {
            year.IsLeapYear().Should().Be(expected);
        }

        [Theory]
        [InlineData(10, 5, true)]
        [InlineData(10, 3, false)]
        [InlineData(0, 5, true)]
        public void IsMultipleOfChecksDivisibility(int value, int factor, bool expected)
        {
            value.IsMultipleOf(factor).Should().Be(expected);
        }

        [Fact]
        public void IsMultipleOfThrowsForAZeroFactor()
        {
            var act = () => 10.IsMultipleOf(0);

            act.Should().Throw<DivideByZeroException>();
        }

        [Theory]
        [InlineData(3, true)]
        [InlineData(4, false)]
        [InlineData(1, false)]
        [InlineData(2, true)]
        [InlineData(17, true)]
        [InlineData(9, false)]
        public void IsPrimeIdentifiesPrimesForPositiveInputs(int input, bool expected)
        {
            input.IsPrime().Should().Be(expected);
        }

        [Fact]
        public void IsPrimeReturnsFalseForZero()
        {
            0.IsPrime().Should().BeFalse();
        }

        [Theory]
        [InlineData(-4)]
        [InlineData(-2)]
        [InlineData(-3)]
        [InlineData(-7)]
        [InlineData(-1)]
        public void IsPrimeReturnsFalseForNegativeNumbers(int input)
        {
            input.IsPrime().Should().BeFalse();
        }

        [Theory]
        [InlineData(3, 7, 7)]
        [InlineData(7, 3, 7)]
        [InlineData(-3, 7, 7)]
        public void MaxReturnsTheLargerOfTwoValues(int a, int b, int expected)
        {
            a.Max(b).Should().Be(expected);
        }

        [Theory]
        [InlineData(3, 7, 3)]
        [InlineData(7, 3, 3)]
        [InlineData(-3, 7, -3)]
        public void MinReturnsTheSmallerOfTwoValues(int a, int b, int expected)
        {
            a.Min(b).Should().Be(expected);
        }

        [Theory]
        [InlineData(5, 1)]
        [InlineData(-5, -1)]
        [InlineData(0, 0)]
        public void SignReturnsMinusOneZeroOrOne(int input, int expected)
        {
            input.Sign().Should().Be(expected);
        }

        [Theory]
        [InlineData(56, SqlDbType.Int)]
        [InlineData(127, SqlDbType.BigInt)]
        [InlineData(231, SqlDbType.NVarChar)]
        [InlineData(104, SqlDbType.Bit)]
        [InlineData(106, SqlDbType.Decimal)]
        [InlineData(108, SqlDbType.Decimal)]
        public void SqlSystemTypeToSqlDbTypeMapsKnownSystemTypeIdsToTheirSqlDbType(int systemTypeId,
            SqlDbType expected)
        {
            systemTypeId.SqlSystemTypeToSqlDbType().Should().Be(expected);
        }

        [Fact]
        public void SqlSystemTypeToSqlDbTypeThrowsAGenericExceptionForAnUnrecognisedTypeId()
        {
            var act = () => (-1).SqlSystemTypeToSqlDbType();

            act.Should().Throw<Exception>();
        }

        [Theory]
        [InlineData(12, 8, 4)]
        [InlineData(8, 12, 4)]
        [InlineData(17, 5, 1)]
        [InlineData(0, 5, 5)]
        [InlineData(5, 0, 5)]
        [InlineData(0, 0, 0)]
        [InlineData(-12, 8, 4)]
        public void GreatestCommonDivisorComputesTheGcd(int a, int b, int expected)
        {
            a.GreatestCommonDivisor(b).Should().Be(expected);
        }

        [Theory]
        [InlineData(4, 6, 12)]
        [InlineData(6, 4, 12)]
        [InlineData(5, 7, 35)]
        [InlineData(0, 5, 0)]
        [InlineData(-4, 6, 12)]
        public void LeastCommonMultipleComputesTheLcm(int a, int b, int expected)
        {
            a.LeastCommonMultiple(b).Should().Be(expected);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(123, 6)]
        [InlineData(-123, 6)]
        [InlineData(9999, 36)]
        public void DigitSumAddsTheAbsoluteDigits(int input, int expected)
        {
            input.DigitSum().Should().Be(expected);
        }
    }
}