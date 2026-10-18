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
using Jube.Service.Extensions.OnlineIdentifiers;
using Jube.Service.Extensions.OnlineIdentifiers.Models;
using Xunit;

namespace Jube.Test.Service.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class OnlineIdentifiersToolsTests
    {
        [Fact]
        [Trait("Covers", "ValidateEmailAddress")]
        [Trait("Covers", "EmailAddressesMatchIgnoringAliases")]
        public void EmailAddresses()
        {
            OnlineIdentifiersTools.ValidateEmailAddress("Ann+promo@Mail.Example.com")
                .Should().Be(new EmailAddressDetails(true, "Ann+promo", "mail.example.com", "com",
                    "ann@mail.example.com"));

            OnlineIdentifiersTools.ValidateEmailAddress("not-an-email").IsValid.Should().BeFalse();
            OnlineIdentifiersTools.EmailAddressesMatchIgnoringAliases("ann+1@x.com", "ANN@x.com").Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "ValidatePhoneNumber")]
        [Trait("Covers", "DescribeUrl")]
        [Trait("Covers", "IsValidDomainName")]
        public void PhonesUrlsAndDomains()
        {
            OnlineIdentifiersTools.ValidatePhoneNumber("0044 7911 123456").Should()
                .Be(new PhoneNumberDetails("+447911123456", true));

            OnlineIdentifiersTools.DescribeUrl("https://Bücher.example/x").Should()
                .Be(new UrlDetails(true, "https", "xn--bcher-kva.example", "example"));

            OnlineIdentifiersTools.DescribeUrl("file:///etc/passwd").IsValid.Should().BeFalse();
            OnlineIdentifiersTools.IsValidDomainName("example.co.uk").Should().BeTrue();
        }

        [Fact]
        [Trait("Covers", "DescribeIpAddress")]
        [Trait("Covers", "IsIpAddressInRange")]
        [Trait("Covers", "DescribeIpRange")]
        [Trait("Covers", "NumberToIPv4Address")]
        public void IpAddresses()
        {
            OnlineIdentifiersTools.DescribeIpAddress("192.168.1.1")
                .Should().Be(new IpAddressDetails(true, "192.168.1.1", "IPv4", true, false, false, false, 3232235777));

            OnlineIdentifiersTools.DescribeIpAddress("2606:4700::1111").IsPublic.Should().BeTrue();
            OnlineIdentifiersTools.DescribeIpAddress("banana").IsValid.Should().BeFalse();
            OnlineIdentifiersTools.IsIpAddressInRange("10.1.2.3", "10.0.0.0/8").Should().BeTrue();

            OnlineIdentifiersTools.DescribeIpRange("192.168.1.77/24").Should()
                .Be(new IpRangeDetails("192.168.1.0", "192.168.1.255", 256));

            OnlineIdentifiersTools.NumberToIPv4Address(3232235777).Should().Be("192.168.1.1");
            var act = () => OnlineIdentifiersTools.DescribeIpRange("10.0.0.0/40");
            act.Should().Throw<ArgumentException>();
        }
    }
}