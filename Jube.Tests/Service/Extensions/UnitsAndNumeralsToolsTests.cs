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
using Jube.Service.Extensions.UnitsAndNumerals;
using Xunit;

namespace Jube.Test.Service.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class UnitsAndNumeralsToolsTests
    {
        [Fact]
        [Trait("Covers", "ConvertUnits")]
        public void ConvertUnitsWithinAndAcrossDimensions()
        {
            UnitsAndNumeralsTools.ConvertUnits(26.2, "mi", "km").Should().BeApproximately(42.1648, 1e-4);
            UnitsAndNumeralsTools.ConvertUnits(-40, "c", "f").Should().BeApproximately(-40, 1e-9);
            var act = () => UnitsAndNumeralsTools.ConvertUnits(1, "kg", "km");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "IntegerToRomanNumerals")]
        [Trait("Covers", "RomanNumeralsToInteger")]
        public void RomanNumerals()
        {
            UnitsAndNumeralsTools.IntegerToRomanNumerals(2026).Should().Be("MMXXVI");
            UnitsAndNumeralsTools.RomanNumeralsToInteger("mmxxvi").Should().Be(2026);
            var act = () => UnitsAndNumeralsTools.RomanNumeralsToInteger("IIII");
            act.Should().Throw<ArgumentException>();
            var zero = () => UnitsAndNumeralsTools.IntegerToRomanNumerals(0);
            zero.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "IntegerToBase")]
        [Trait("Covers", "BaseToInteger")]
        public void BaseConversion()
        {
            UnitsAndNumeralsTools.IntegerToBase(255, 16).Should().Be("ff");
            UnitsAndNumeralsTools.BaseToInteger("FF", 16).Should().Be(255);
            var bad = () => UnitsAndNumeralsTools.BaseToInteger("12", 2);
            bad.Should().Throw<ArgumentException>();
            var overflow = () => UnitsAndNumeralsTools.BaseToInteger(new string('f', 17), 16);
            overflow.Should().Throw<ArgumentException>().WithMessage("*64-bit*");
        }

        [Fact]
        [Trait("Covers", "IsPrimeNumber")]
        [Trait("Covers", "DigitSum")]
        public void WholeNumberFacts()
        {
            UnitsAndNumeralsTools.IsPrimeNumber(97).Should().BeTrue();
            UnitsAndNumeralsTools.IsPrimeNumber(1).Should().BeFalse();
            UnitsAndNumeralsTools.DigitSum(9999).Should().Be(36);
        }
    }
}