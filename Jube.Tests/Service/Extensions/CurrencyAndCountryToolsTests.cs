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
using Jube.Service.Extensions.CurrencyAndCountry;
using Jube.Service.Extensions.CurrencyAndCountry.Models;
using Xunit;

namespace Jube.Test.Service.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class CurrencyAndCountryToolsTests
    {
        [Fact]
        [Trait("Covers", "CurrencyMinorUnitDigits")]
        [Trait("Covers", "IsValidCurrencyCode")]
        public void CurrencyReferenceData()
        {
            CurrencyAndCountryTools.CurrencyMinorUnitDigits("kwd").Should().Be(3);
            CurrencyAndCountryTools.IsValidCurrencyCode("GBP").Should().BeTrue();
            CurrencyAndCountryTools.IsValidCurrencyCode("GBX").Should().BeFalse();
            var act = () => CurrencyAndCountryTools.CurrencyMinorUnitDigits("ZZZ");
            act.Should().Throw<ArgumentException>().WithMessage("*ISO 4217*");
        }

        [Fact]
        [Trait("Covers", "ToMinorUnits")]
        [Trait("Covers", "FromMinorUnits")]
        [Trait("Covers", "RoundToCurrency")]
        [Trait("Covers", "ConvertAtExchangeRate")]
        public void MoneyArithmeticUsesTheCurrencyPrecision()
        {
            CurrencyAndCountryTools.ToMinorUnits(19.99, "USD").Should().Be(1999);
            CurrencyAndCountryTools.ToMinorUnits(1500, "JPY").Should().Be(1500);
            CurrencyAndCountryTools.FromMinorUnits(1234, "KWD").Should().Be(1.234);
            CurrencyAndCountryTools.RoundToCurrency(1234.5678, "BHD").Should().Be(1234.568);
            CurrencyAndCountryTools.ConvertAtExchangeRate(100, 1.25).Should().Be(125);
            var act = () => CurrencyAndCountryTools.ConvertAtExchangeRate(100, -1);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "IsValidCountryCode")]
        [Trait("Covers", "DescribeCountry")]
        public void CountryReferenceData()
        {
            CurrencyAndCountryTools.IsValidCountryCode("UK").Should().BeFalse();
            CurrencyAndCountryTools.DescribeCountry("deu").Should().Be(new CountryDetails("DE", "DEU", "Germany"));
            var act = () => CurrencyAndCountryTools.DescribeCountry("XX");
            act.Should().Throw<ArgumentException>();
        }
    }
}