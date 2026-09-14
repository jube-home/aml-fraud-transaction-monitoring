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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FluentAssertions;
using Jube.Dictionary.Models;
using Xunit;

namespace Jube.Test.Dictionary.Models
{
    [Trait("Category", "Unit")]
    public sealed class InternalValueTests
    {
        [Fact]
        public void ADefaultInternalValueHasTheNoneType()
        {
            var value = default(InternalValue);

            value.Type.Should().Be(InternalValue.ValueType.None);
            value.AsString().Should().Be(string.Empty);
            value.AsInt().Should().Be(0);
            value.AsDouble().Should().Be(0d);
            value.AsBool().Should().BeFalse();
            value.AsDateTime().Should().Be(default);
            value.AsGuid().Should().Be(Guid.Empty);
            value.ToString().Should().Be("None");
            value.GetHashCode().Should().Be(0);
        }

        [Fact]
        public void AStringValueRoundTripsThroughAsStringAndToString()
        {
            var value = new InternalValue("hello");

            value.Type.Should().Be(InternalValue.ValueType.String);
            value.AsString().Should().Be("hello");
            value.ToString().Should().Be("hello");
        }

        [Fact]
        public void ANullStringValueIsStillTypedAsStringButRendersAsTheLiteralNull()
        {
            var value = new InternalValue(null);

            value.Type.Should().Be(InternalValue.ValueType.String);
            value.AsString().Should().Be(string.Empty);
            value.ToString().Should().Be("null");
        }

        [Fact]
        public void AnEmptyStringValueRoundTrips()
        {
            var value = new InternalValue(string.Empty);

            value.AsString().Should().Be(string.Empty);
            value.ToString().Should().Be(string.Empty);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        public void AnIntValueRoundTripsThroughAsInt(int input)
        {
            var value = new InternalValue(input);

            value.Type.Should().Be(InternalValue.ValueType.Int);
            value.AsInt().Should().Be(input);
            value.ToString().Should().Be(input.ToString());
        }

        [Theory]
        [InlineData(0d)]
        [InlineData(-1.5)]
        [InlineData(double.MaxValue)]
        [InlineData(double.MinValue)]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        public void ADoubleValueRoundTripsThroughAsDoubleBitForBit(double input)
        {
            var value = new InternalValue(input);

            value.Type.Should().Be(InternalValue.ValueType.Double);
            value.AsDouble().Should().Be(input);
            value.ToString().Should().Be(input.ToString(CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ABoolValueRoundTripsThroughAsBool(bool input)
        {
            var value = new InternalValue(input);

            value.Type.Should().Be(InternalValue.ValueType.Bool);
            value.AsBool().Should().Be(input);
            value.ToString().Should().Be(input.ToString());
        }

        [Fact]
        public void ADateTimeValueIsNormalisedToUtcTicksOnConstruction()
        {
            var utc = new DateTime(2024, 6, 15, 12, 30, 45, DateTimeKind.Utc);

            var value = new InternalValue(utc);

            value.Type.Should().Be(InternalValue.ValueType.DateTime);
            value.AsDateTime().Should().Be(utc);
            value.AsDateTime().Kind.Should().Be(DateTimeKind.Utc);
        }

        [Fact]
        public void AnUnspecifiedKindDateTimeIsConvertedAsIfItWereLocalOnConstruction()
        {
            var unspecified = new DateTime(2024, 6, 15, 12, 30, 45, DateTimeKind.Unspecified);
            var expectedUtc = DateTime.SpecifyKind(unspecified, DateTimeKind.Local).ToUniversalTime();

            var value = new InternalValue(unspecified);

            value.AsDateTime().Should().Be(expectedUtc);
        }

        [Fact]
        public void AGuidValueRoundTripsThroughAsGuid()
        {
            var guid = Guid.NewGuid();

            var value = new InternalValue(guid);

            value.Type.Should().Be(InternalValue.ValueType.Guid);
            value.AsGuid().Should().Be(guid);
            value.ToString().Should().Be(guid.ToString());
        }

        [Fact]
        public void TheEmptyGuidRoundTrips()
        {
            var value = new InternalValue(Guid.Empty);

            value.AsGuid().Should().Be(Guid.Empty);
        }

        public static IEnumerable<object[]> WrongAccessorCases()
        {
            yield return [new InternalValue("text")];
            yield return [new InternalValue(42)];
            yield return [new InternalValue(3.14)];
            yield return [new InternalValue(true)];
            yield return [new InternalValue(DateTime.UtcNow)];
            yield return [new InternalValue(Guid.NewGuid())];
        }

        [Theory]
        [MemberData(nameof(WrongAccessorCases))]
        public void AccessorsForTypesOtherThanTheStoredTypeReturnTheirDefaultRatherThanThrowing(InternalValue value)
        {
            if (value.Type != InternalValue.ValueType.String)
            {
                value.AsString().Should().Be(string.Empty);
            }

            if (value.Type != InternalValue.ValueType.Int)
            {
                value.AsInt().Should().Be(0);
            }

            if (value.Type != InternalValue.ValueType.Double)
            {
                value.AsDouble().Should().Be(0d);
            }

            if (value.Type != InternalValue.ValueType.Bool)
            {
                value.AsBool().Should().BeFalse();
            }

            if (value.Type != InternalValue.ValueType.DateTime)
            {
                value.AsDateTime().Should().Be(default);
            }

            if (value.Type != InternalValue.ValueType.Guid)
            {
                value.AsGuid().Should().Be(Guid.Empty);
            }
        }

        [Fact]
        public void TwoValuesOfTheSameTypeAndContentAreEqualAndShareAHashCode()
        {
            var left = new InternalValue("abc");
            var right = new InternalValue("abc");

            left.Equals(right).Should().BeTrue();
            (left == right).Should().BeTrue();
            (left != right).Should().BeFalse();
            left.GetHashCode().Should().Be(right.GetHashCode());
            left.Equals((object)right).Should().BeTrue();
            // ReSharper disable once SuspiciousTypeConversion.Global
            left.Equals("not an internal value").Should().BeFalse();
        }

        [Fact]
        public void DoubleEqualityUsesASmallToleranceRatherThanExactBitwiseComparison()
        {
            var left = new InternalValue(1.0d);
            var right = new InternalValue(1.0d + 0.00005d);
            var farRight = new InternalValue(1.0d + 0.001d);

            (left == right).Should().BeTrue("the difference is within the 0.0001 tolerance");
            (left == farRight).Should().BeFalse("the difference exceeds the 0.0001 tolerance");
        }

        [Fact]
        public void EqualityRequiresTheSameStoredTypeEvenWhenTheNumericValueMatches()
        {
            var intFive = new InternalValue(5);
            var doubleFive = new InternalValue(5.0d);

            (intFive == doubleFive).Should()
                .BeFalse("Equals short-circuits on a Type mismatch before comparing values");
            intFive.Equals(doubleFive).Should().BeFalse();
        }

        [Fact]
        public void RelationalOperatorsFallBackToNumericComparisonAcrossDifferentTypesEvenThoughEqualityDoesNot()
        {
            var intFive = new InternalValue(5);
            var doubleFive = new InternalValue(5.0d);

            (intFive <= doubleFive).Should().BeTrue("CompareTo falls back to AsNumeric() when Type differs");
            (intFive >= doubleFive).Should().BeTrue();
            (intFive < doubleFive).Should().BeFalse();
            (intFive > doubleFive).Should().BeFalse();
        }

        [Fact]
        public void ComparingANonNumericTypeAgainstANumericTypeTreatsTheNonNumericSideAsZero()
        {
            var text = new InternalValue("anything");
            var positive = new InternalValue(1);
            var negative = new InternalValue(-1);

            (text < positive).Should().BeTrue("a non-numeric operand's AsNumeric() is 0");
            (text > negative).Should().BeTrue();
        }

        [Fact]
        public void CompareToOrdersStringsOrdinally()
        {
            var a = new InternalValue("a");
            var b = new InternalValue("b");

            a.CompareTo(b).Should().BeLessThan(0);
            b.CompareTo(a).Should().BeGreaterThan(0);
            a.CompareTo(a).Should().Be(0);
        }

        [Fact]
        public void CompareToOrdersIntsNumerically()
        {
            new InternalValue(1).CompareTo(new InternalValue(2)).Should().BeLessThan(0);
            new InternalValue(2).CompareTo(new InternalValue(1)).Should().BeGreaterThan(0);
        }

        [Fact]
        public void CompareToOrdersDatesChronologically()
        {
            var earlier = new InternalValue(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var later = new InternalValue(new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            earlier.CompareTo(later).Should().BeLessThan(0);
        }

        [Fact]
        public void CompareToOrdersBooleansWithFalseBeforeTrue()
        {
            new InternalValue(false).CompareTo(new InternalValue(true)).Should().BeLessThan(0);
        }

        [Fact]
        public void AListOfInternalValuesCanBeSortedBecauseTheStructImplementsIComparable()
        {
            var values = new List<InternalValue>
            {
                new(3),
                new(1),
                new(2)
            };

            values.Sort();

            values.Select(v => v.AsInt()).Should().Equal(1, 2, 3);
        }

        [Theory]
        [InlineData(5, 5, true)]
        [InlineData(5, 6, false)]
        public void EqualityOperatorAgainstARawIntWorks(int stored, int comparand, bool expectedEqual)
        {
            var value = new InternalValue(stored);

            (value == comparand).Should().Be(expectedEqual);
            (comparand == value).Should().Be(expectedEqual);
            (value != comparand).Should().Be(!expectedEqual);
        }

        [Fact]
        public void RelationalOperatorsAgainstARawIntAreSymmetricWhenOperandsAreSwapped()
        {
            var value = new InternalValue(10);

            (value > 5).Should().BeTrue();
            (5 < value).Should().BeTrue();
            (value < 5).Should().BeFalse();
            (5 > value).Should().BeFalse();
            (value >= 10).Should().BeTrue();
            (10 <= value).Should().BeTrue();
        }

        [Theory]
        [InlineData(5.5, 5.5, true)]
        [InlineData(5.5, 5.6, false)]
        public void EqualityOperatorAgainstARawDoubleWorks(double stored, double comparand, bool expectedEqual)
        {
            var value = new InternalValue(stored);

            (value == comparand).Should().Be(expectedEqual);
            (comparand == value).Should().Be(expectedEqual);
        }

        [Fact]
        public void RelationalOperatorsAgainstARawDoubleWork()
        {
            var value = new InternalValue(10.0d);

            (value > 5.0d).Should().BeTrue();
            (5.0d < value).Should().BeTrue();
            (value <= 10.0d).Should().BeTrue();
            (10.0d >= value).Should().BeTrue();
        }

        [Fact]
        public void EqualityAndRelationalOperatorsAgainstARawStringWork()
        {
            var value = new InternalValue("b");

            (value == "b").Should().BeTrue();
            ("b" == value).Should().BeTrue();
            (value != "c").Should().BeTrue();
            (value < "c").Should().BeTrue();
            ("a" < value).Should().BeTrue();
            (value > "a").Should().BeTrue();
            (value <= "b").Should().BeTrue();
            (value >= "b").Should().BeTrue();
        }

        [Fact]
        public void EqualityAndRelationalOperatorsAgainstARawDateTimeWork()
        {
            var earlier = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var later = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var value = new InternalValue(later);

            (value == later).Should().BeTrue();
            (later == value).Should().BeTrue();
            (value > earlier).Should().BeTrue();
            (earlier < value).Should().BeTrue();
            (value >= later).Should().BeTrue();
            (value <= later).Should().BeTrue();
        }

        [Fact]
        public void EqualityOperatorAgainstARawBoolWorks()
        {
            var value = new InternalValue(true);

            (value == true).Should().BeTrue();
            (true == value).Should().BeTrue();
            (value != false).Should().BeTrue();
        }

        [Fact]
        public void EqualityOperatorAgainstARawGuidWorks()
        {
            var guid = Guid.NewGuid();
            var value = new InternalValue(guid);

            (value == guid).Should().BeTrue();
            (guid == value).Should().BeTrue();
            (value != Guid.NewGuid()).Should().BeTrue();
        }

        [Fact]
        public void ArithmeticOperatorsCombineTheNumericValueOfTwoInternalValues()
        {
            var five = new InternalValue(5);
            var three = new InternalValue(3.0d);

            (five + three).Should().Be(8d);
            (five - three).Should().Be(2d);
            (five * three).Should().Be(15d);
            (five / three).Should().BeApproximately(1.6666666666, 1e-9);
        }

        [Fact]
        public void ArithmeticOperatorsTreatNonNumericOperandsAsZero()
        {
            var text = new InternalValue("ignored");
            var five = new InternalValue(5);

            (text + five).Should().Be(5d);
            (five + text).Should().Be(5d);
            (five - text).Should().Be(5d);
            (text - five).Should().Be(-5d);
            (five * text).Should().Be(0d);
        }

        [Fact]
        public void DivisionByZeroFollowsIeee754RatherThanThrowing()
        {
            var five = new InternalValue(5.0d);
            var zero = new InternalValue(0.0d);

            (five / zero).Should().Be(double.PositiveInfinity);
            double.IsNaN(zero / zero).Should().BeTrue();
        }

        [Fact]
        public void ArithmeticOperatorsAgainstARawDoubleWorkInBothOperandPositions()
        {
            var value = new InternalValue(10.0d);

            (value + 5.0d).Should().Be(15d);
            (5.0d + value).Should().Be(15d);
            (value - 5.0d).Should().Be(5d);
            (5.0d - value).Should().Be(-5d);
            (value * 2.0d).Should().Be(20d);
            (2.0d * value).Should().Be(20d);
            (value / 2.0d).Should().Be(5d);
            (20.0d / value).Should().Be(2d);
        }

        [Fact]
        public void ArithmeticOperatorsAgainstARawIntWorkInBothOperandPositions()
        {
            var value = new InternalValue(10.0d);

            (value + 5).Should().Be(15d);
            (5 + value).Should().Be(15d);
            (value - 5).Should().Be(5d);
            (5 - value).Should().Be(-5d);
            (value * 2).Should().Be(20d);
            (2 * value).Should().Be(20d);
            (value / 2).Should().Be(5d);
            (20 / value).Should().Be(2d);
        }

        [Fact]
        public void ImplicitConversionToStringDelegatesToAsString()
        {
            var value = new InternalValue("payload");

            string s = value;

            s.Should().Be("payload");
        }

        [Fact]
        public void ImplicitConversionToIntDelegatesToAsInt()
        {
            var value = new InternalValue(99);

            int i = value;

            i.Should().Be(99);
        }

        [Fact]
        public void ImplicitConversionToDoubleDelegatesToAsDouble()
        {
            var value = new InternalValue(9.9d);

            double d = value;

            d.Should().Be(9.9d);
        }

        [Fact]
        public void ImplicitConversionToBoolDelegatesToAsBool()
        {
            var value = new InternalValue(true);

            bool b = value;

            b.Should().BeTrue();
        }

        [Fact]
        public void ImplicitConversionToDateTimeDelegatesToAsDateTime()
        {
            var dt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var value = new InternalValue(dt);

            DateTime converted = value;

            converted.Should().Be(dt);
        }

        [Fact]
        public void ImplicitConversionToGuidDelegatesToAsGuid()
        {
            var guid = Guid.NewGuid();
            var value = new InternalValue(guid);

            Guid converted = value;

            converted.Should().Be(guid);
        }
    }
}