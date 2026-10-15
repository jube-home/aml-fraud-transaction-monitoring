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
using Jube.Service.Extensions.FinancialIdentifiers;
using Jube.Service.Extensions.FinancialIdentifiers.Models;
using Xunit;

namespace Jube.Test.Service.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class FinancialIdentifiersToolsTests
    {
        [Fact]
        [Trait("Covers", "ValidateIban")]
        [Trait("Covers", "ValidateBic")]
        public void BankIdentifiers()
        {
            FinancialIdentifiersTools.ValidateIban("gb82west12345698765432")
                .Should().Be(new IbanDetails(true, "GB", "82", "WEST12345698765432", "GB82 WEST 1234 5698 7654 32"));

            FinancialIdentifiersTools.ValidateIban("GB00WEST12345698765432").IsValid.Should().BeFalse();

            FinancialIdentifiersTools.ValidateBic("DEUTDEFF").Should()
                .Be(new BicDetails(true, "DEUT", "DE", "FF", "XXX"));
        }

        [Fact]
        [Trait("Covers", "ValidateCardNumber")]
        [Trait("Covers", "LuhnCheckDigit")]
        [Trait("Covers", "IsCardExpiryValid")]
        public void CardIdentifiers()
        {
            FinancialIdentifiersTools.ValidateCardNumber("4111 1111 1111 1111")
                .Should().Be(new CardNumberDetails(true, "Visa", "411111", "41111111", "************1111"));

            FinancialIdentifiersTools.ValidateCardNumber("4111 1111 1111 1112").IsValid.Should().BeFalse();
            FinancialIdentifiersTools.LuhnCheckDigit("7992739871").Should().Be(3);
            FinancialIdentifiersTools.IsCardExpiryValid("09/26", new DateTime(2026, 9, 30)).Should().BeTrue();
            FinancialIdentifiersTools.IsCardExpiryValid("09/26", new DateTime(2026, 10, 1)).Should().BeFalse();
            var act = () => FinancialIdentifiersTools.LuhnCheckDigit("12a");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "IsValidAbaRoutingNumber")]
        [Trait("Covers", "IsValidIsin")]
        [Trait("Covers", "IsValidLei")]
        [Trait("Covers", "IsValidCusip")]
        [Trait("Covers", "IsValidSedol")]
        public void SecuritiesIdentifiers()
        {
            FinancialIdentifiersTools.IsValidAbaRoutingNumber("021000021").Should().BeTrue();
            FinancialIdentifiersTools.IsValidIsin("US0378331005").Should().BeTrue();
            FinancialIdentifiersTools.IsValidLei("5493001KJTIIGC8Y1R12").Should().BeTrue();
            FinancialIdentifiersTools.IsValidCusip("037833100").Should().BeTrue();
            FinancialIdentifiersTools.IsValidSedol("0263494").Should().BeTrue();
            FinancialIdentifiersTools.IsValidSedol("0263495").Should().BeFalse();
        }
    }
}