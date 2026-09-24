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
    public sealed class NumericAndReferenceExtensionsTests
    {
        [Theory]
        [InlineData(1234.0, 50.0, 1250.0)]
        [InlineData(1224.0, 50.0, 1200.0)]
        [InlineData(0.125, 0.05, 0.15)]
        public void RoundToNearestSnapsToTheIncrement(double value, double increment, double expected)
        {
            value.RoundToNearest(increment).Should().BeApproximately(expected, 1e-9);
        }

        [Fact]
        public void RoundToNearestIsUndefinedForANonPositiveIncrement()
        {
            10d.RoundToNearest(0).Should().Be(double.NaN);
        }

        [Fact]
        public void ProductWithMultipliesEveryValue()
        {
            2d.ProductWith(3, 4).Should().Be(24);
        }

        [Theory]
        [InlineData(27.0, 3.0, 3.0)]
        [InlineData(-8.0, 3.0, -2.0)]
        [InlineData(16.0, 4.0, 2.0)]
        public void NthRootReturnsTheRealRoot(double value, double degree, double expected)
        {
            value.NthRoot(degree).Should().BeApproximately(expected, 1e-9);
        }

        [Fact]
        public void NthRootOfANegativeNumberWithAnEvenDegreeIsUndefined()
        {
            (-16d).NthRoot(2).Should().Be(double.NaN);
        }

        [Theory]
        [InlineData(7.0, 3.0, 1.0)]
        [InlineData(-7.0, 3.0, 2.0)]
        [InlineData(7.0, -3.0, -2.0)]
        public void ModuloTakesTheSignOfTheDivisor(double value, double divisor, double expected)
        {
            value.Modulo(divisor).Should().Be(expected);
        }

        [Fact]
        public void PercentHelpersApplyAndReverseAPercentage()
        {
            100d.AddPercent(20).Should().BeApproximately(120, 1e-9);
            100d.SubtractPercent(20).Should().BeApproximately(80, 1e-9);
            120d.BaseBeforePercentAdded(20).Should().BeApproximately(100, 1e-9);
        }

        [Fact]
        public void ConvertAtRateRejectsANonPositiveRate()
        {
            100d.ConvertAtRate(1.25).Should().Be(125);
            100d.ConvertAtRate(0).Should().Be(double.NaN);
        }

        [Theory]
        [InlineData("USD", 2)]
        [InlineData("jpy", 0)]
        [InlineData("KWD", 3)]
        [InlineData("CLF", 4)]
        [InlineData("XXX", -1)]
        public void CurrencyMinorUnitExponentFollowsIso4217(string code, int expected)
        {
            code.CurrencyMinorUnitExponent().Should().Be(expected);
        }

        [Fact]
        public void MinorUnitsConvertExactlyWithoutBinaryDrift()
        {
            19.99d.ToMinorUnits(2).Should().Be(1999);
            1999d.FromMinorUnits(2).Should().Be(19.99);
            1.2345d.ToMinorUnits(3).Should().Be(1235);
            1d.ToMinorUnits(7).Should().Be(double.NaN);
        }

        [Fact]
        public void RoundToCurrencyUsesTheCurrencyExponent()
        {
            1234.5678d.RoundToCurrency("JPY").Should().Be(1235);
            1234.5678d.RoundToCurrency("BHD").Should().Be(1234.568);
            1d.RoundToCurrency("ZZZ").Should().Be(double.NaN);
        }

        [Fact]
        public void CurrencyAndCountryCodesValidateAgainstTheEmbeddedTables()
        {
            "EUR".IsValidCurrencyCode().Should().BeTrue();
            "EURO".IsValidCurrencyCode().Should().BeFalse();
            "gb".IsValidCountryCode().Should().BeTrue();
            "UK".IsValidCountryCode().Should().BeFalse();
        }

        [Theory]
        [InlineData("GB", "GBR", "GB", "United Kingdom")]
        [InlineData("deu", "DEU", "DE", "Germany")]
        [InlineData("ZZ", "", "", "")]
        public void CountryLookupsAcceptEitherAlpha2OrAlpha3(string input, string alpha3, string alpha2, string name)
        {
            input.CountryAlpha3().Should().Be(alpha3);
            input.CountryAlpha2().Should().Be(alpha2);
            input.CountryName().Should().Be(name);
        }

        [Theory]
        [InlineData(1994, "MCMXCIV")]
        [InlineData(3999, "MMMCMXCIX")]
        [InlineData(4, "IV")]
        [InlineData(0, "")]
        [InlineData(4000, "")]
        public void RomanNumeralsRoundTrip(int value, string roman)
        {
            value.ToRomanNumerals().Should().Be(roman);
            if (roman.Length > 0)
            {
                roman.ToLowerInvariant().FromRomanNumerals().Should().Be(value);
            }
        }

        [Theory]
        [InlineData("IIII")]
        [InlineData("VX")]
        [InlineData("ABC")]
        public void FromRomanNumeralsRejectsNonCanonicalNumerals(string roman)
        {
            roman.FromRomanNumerals().Should().Be(0);
        }

        [Theory]
        [InlineData(255L, 16, "ff")]
        [InlineData(-10L, 2, "-1010")]
        [InlineData(0L, 36, "0")]
        [InlineData(long.MinValue, 16, "-8000000000000000")]
        public void RadixStringsRoundTrip(long value, int radix, string text)
        {
            value.ToRadixString(radix).Should().Be(text);
            text.FromRadixString(radix).Should().Be(value);
        }

        [Fact]
        public void FromRadixStringRejectsADigitOutsideTheBase()
        {
            var act = () => "129".FromRadixString(2);

            act.Should().Throw<FormatException>();
        }

        [Theory]
        [InlineData(1.0, "km", "m", 1000.0)]
        [InlineData(1.0, "mi", "km", 1.609344)]
        [InlineData(100.0, "c", "f", 212.0)]
        [InlineData(0.0, "c", "k", 273.15)]
        [InlineData(1.0, "gib", "mib", 1024.0)]
        [InlineData(1.0, "kn", "km/h", 1.852)]
        public void ConvertUnitsConvertsWithinADimension(double value, string from, string to, double expected)
        {
            value.ConvertUnits(from, to).Should().BeApproximately(expected, 1e-9);
        }

        [Fact]
        public void ConvertUnitsIsUndefinedAcrossDimensions()
        {
            1d.ConvertUnits("kg", "m").Should().Be(double.NaN);
            1d.ConvertUnits("parsec", "m").Should().Be(double.NaN);
        }
    }
}