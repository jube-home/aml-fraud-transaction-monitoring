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
    public sealed class IdentifierExtensionsTests
    {
        private const string Iban = "GB82 WEST 1234 5698 7654 32";

        [Fact]
        public void IbanPartsAreExtractedFromAValidIban()
        {
            Iban.IbanCountryCode().Should().Be("GB");
            Iban.IbanCheckDigits().Should().Be("82");
            Iban.IbanBasicBankAccountNumber().Should().Be("WEST12345698765432");
            "gb82west12345698765432".IbanPrintFormat().Should().Be("GB82 WEST 1234 5698 7654 32");
        }

        [Fact]
        public void IbanPartsAreEmptyForAnInvalidIban()
        {
            "GB83WEST12345698765432".IbanCountryCode().Should().BeEmpty();
            "GB83WEST12345698765432".IbanPrintFormat().Should().BeEmpty();
        }

        [Fact]
        public void BicPartsAreExtractedFromAValidBic()
        {
            "DEUTDEFF500".BicInstitutionCode().Should().Be("DEUT");
            "DEUTDEFF500".BicCountryCode().Should().Be("DE");
            "DEUTDEFF500".BicLocationCode().Should().Be("FF");
            "DEUTDEFF500".BicBranchCode().Should().Be("500");
            "DEUTDEFF".BicBranchCode().Should().Be("XXX");
            "DEUT".BicCountryCode().Should().BeEmpty();
        }

        [Theory]
        [InlineData("7992739871", 3)]
        [InlineData("411111111111111", 1)]
        [InlineData("12a", -1)]
        public void LuhnCheckDigitCompletesThePayload(string payload, int expected)
        {
            payload.LuhnCheckDigit().Should().Be(expected);
            if (expected >= 0)
            {
                (payload + expected).IsValidLuhn().Should().BeTrue();
            }
        }

        [Fact]
        public void CardIssuerPrefixTakesTheLeadingDigits()
        {
            "4111 1111 1111 1111".CardIssuerPrefix(6).Should().Be("411111");
            "5555-5555-5555-4444".CardIssuerPrefix(8).Should().Be("55555555");
            "4111".CardIssuerPrefix(6).Should().BeEmpty();
        }

        [Theory]
        [InlineData("US0378331005", true)]
        [InlineData("US0378331006", false)]
        [InlineData("0378331005US", false)]
        public void IsValidIsinChecksTheLuhnCheckDigit(string isin, bool expected)
        {
            isin.IsValidIsin().Should().Be(expected);
        }

        [Theory]
        [InlineData("5493001KJTIIGC8Y1R12", true)]
        [InlineData("529900T8BM49AURSDO55", true)]
        [InlineData("5493001KJTIIGC8Y1R13", false)]
        [InlineData("5493001KJTIIGC8Y1R1", false)]
        public void IsValidLeiChecksIso7064Mod97(string lei, bool expected)
        {
            lei.IsValidLei().Should().Be(expected);
        }

        [Theory]
        [InlineData("037833100", true)]
        [InlineData("38259P508", true)]
        [InlineData("037833101", false)]
        public void IsValidCusipChecksTheCheckDigit(string cusip, bool expected)
        {
            cusip.IsValidCusip().Should().Be(expected);
        }

        [Theory]
        [InlineData("0263494", true)]
        [InlineData("B0YBKJ7", true)]
        [InlineData("0263495", false)]
        [InlineData("A263494", false)]
        public void IsValidSedolChecksTheWeightedCheckDigit(string sedol, bool expected)
        {
            sedol.IsValidSedol().Should().Be(expected);
        }

        [Theory]
        [InlineData("011000015", true)]
        [InlineData("021000021", true)]
        [InlineData("011000016", false)]
        [InlineData("000000000", false)]
        public void IsValidAbaRoutingNumberChecksTheWeightedSum(string routing, bool expected)
        {
            routing.IsValidAbaRoutingNumber().Should().Be(expected);
        }

        [Fact]
        public void UuidHelpersParseAndReportTheVersion()
        {
            "f47ac10b-58cc-4372-a567-0e02b2c3d479".IsValidUuid().Should().BeTrue();
            "f47ac10b-58cc-4372-a567-0e02b2c3d479".UuidVersion().Should().Be(4);
            "not-a-uuid".IsValidUuid().Should().BeFalse();
            "not-a-uuid".UuidVersion().Should().Be(-1);
        }

        [Theory]
        [InlineData("+447911123456", true)]
        [InlineData("+0447911123456", false)]
        [InlineData("447911123456", false)]
        [InlineData("+44 7911 123456", false)]
        public void IsValidE164PhoneNumberRequiresTheCanonicalForm(string phone, bool expected)
        {
            phone.IsValidE164PhoneNumber().Should().Be(expected);
        }

        [Theory]
        [InlineData("+44 (0) 7911-123456", "+4407911123456")]
        [InlineData("0044 7911 123456", "+447911123456")]
        [InlineData("07911 123456", "07911123456")]
        public void PhoneNumberDigitsKeepsTheInternationalPrefix(string phone, string expected)
        {
            phone.PhoneNumberDigits().Should().Be(expected);
        }

        [Theory]
        [InlineData("example.com", true)]
        [InlineData("sub.example.co.uk", true)]
        [InlineData("-bad.example.com", false)]
        [InlineData("localhost", false)]
        [InlineData("1.2.3.4", false)]
        public void IsValidDomainNameChecksLabels(string domain, bool expected)
        {
            domain.IsValidDomainName().Should().Be(expected);
        }

        [Fact]
        public void UrlAndDomainPartsAreExtracted()
        {
            "someone@Mail.Example.COM".TopLevelDomain().Should().Be("com");
            "HTTPS://Www.Example.com:8443/a?b=1".UrlHost().Should().Be("www.example.com");
            "HTTPS://Www.Example.com:8443/a?b=1".UrlScheme().Should().Be("https");
            "file:///etc/passwd".UrlHost().Should().BeEmpty();
            "not a url".UrlHost().Should().BeEmpty();
        }

        [Fact]
        public void QueryStringValueDecodesTheNamedParameter()
        {
            "https://x.test/p?name=J%C3%BCbe+Ltd&empty=&flag#frag".QueryStringValue("name").Should().Be("Jübe Ltd");
            "a=1&b=2".QueryStringValue("b").Should().Be("2");
            "a=1".QueryStringValue("missing").Should().BeEmpty();
        }

        [Fact]
        public void IsValidCardExpiryAsOfComparesWithTheSuppliedDate()
        {
            "12/26".IsValidCardExpiryAsOf(new DateTime(2026, 12, 31)).Should().BeTrue();
            "12/26".IsValidCardExpiryAsOf(new DateTime(2027, 1, 1)).Should().BeFalse();
            "13/26".IsValidCardExpiryAsOf(new DateTime(2020, 1, 1)).Should().BeFalse();
        }
    }
}