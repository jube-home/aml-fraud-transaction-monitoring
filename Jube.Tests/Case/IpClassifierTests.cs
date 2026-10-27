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

using System.Net;
using FluentAssertions;
using Jube.Case.Outbound;
using Xunit;

namespace Jube.Test.Case;

[Trait("Category", "Unit")]
public sealed class IpClassifierTests
{
    [Theory]
    [InlineData("0.0.0.0")]
    [InlineData("0.255.255.255")]
    [InlineData("10.0.0.1")]
    [InlineData("10.255.255.255")]
    [InlineData("100.64.0.0")]
    [InlineData("100.127.255.255")]
    [InlineData("127.0.0.1")]
    [InlineData("127.255.255.254")]
    [InlineData("169.254.169.254")]
    [InlineData("169.254.0.1")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.255")]
    [InlineData("192.168.0.1")]
    [InlineData("192.168.255.255")]
    [InlineData("192.0.0.1")]
    [InlineData("192.0.2.10")]
    [InlineData("198.18.0.1")]
    [InlineData("198.19.255.255")]
    [InlineData("198.51.100.1")]
    [InlineData("203.0.113.1")]
    [InlineData("224.0.0.1")]
    [InlineData("239.255.255.255")]
    [InlineData("240.0.0.1")]
    [InlineData("255.255.255.255")]
    public void ABlockedIpv4RangeIsRefused(string text)
    {
        IpClassifier.IsBlocked(IPAddress.Parse(text)).Should().BeTrue();
        IpClassifier.BlockedCategory(IPAddress.Parse(text)).Should().NotBeNull();
    }

    [Theory]
    [InlineData("1.1.1.1")]
    [InlineData("8.8.8.8")]
    [InlineData("9.255.255.255")]
    [InlineData("11.0.0.0")]
    [InlineData("100.63.255.255")]
    [InlineData("100.128.0.0")]
    [InlineData("126.255.255.255")]
    [InlineData("128.0.0.1")]
    [InlineData("169.253.255.255")]
    [InlineData("169.255.0.0")]
    [InlineData("172.15.255.255")]
    [InlineData("172.32.0.0")]
    [InlineData("192.167.255.255")]
    [InlineData("192.169.0.0")]
    [InlineData("198.17.255.255")]
    [InlineData("198.20.0.0")]
    [InlineData("223.255.255.255")]
    [InlineData("93.184.216.34")]
    public void APublicIpv4AddressAtTheEdgeOfABlockedRangeIsPermitted(string text)
    {
        IpClassifier.IsBlocked(IPAddress.Parse(text)).Should().BeFalse();
    }

    [Theory]
    [InlineData("::")]
    [InlineData("::1")]
    [InlineData("fe80::1")]
    [InlineData("febf::1")]
    [InlineData("fc00::1")]
    [InlineData("fd12:3456:789a::1")]
    [InlineData("fec0::1")]
    [InlineData("ff02::1")]
    [InlineData("2001:db8::1")]
    [InlineData("2001::1")]
    [InlineData("100::1")]
    [InlineData("64:ff9b::7f00:1")]
    [InlineData("64:ff9b::a9fe:a9fe")]
    [InlineData("2002:7f00:1::1")]
    [InlineData("2002:a9fe:a9fe::1")]
    [InlineData("2002:c0a8:1::1")]
    [InlineData("2002:0808:0808::1")]
    public void ABlockedIpv6RangeIsRefused(string text)
    {
        IpClassifier.IsBlocked(IPAddress.Parse(text)).Should().BeTrue();
    }

    [Theory]
    [InlineData("2606:4700:4700::1111")]
    [InlineData("2a00:1450:4009:81f::200e")]
    [InlineData("2001:4860:4860::8888")]
    public void APublicIpv6AddressIsPermitted(string text)
    {
        IpClassifier.IsBlocked(IPAddress.Parse(text)).Should().BeFalse();
    }

    [Theory]
    [InlineData("::ffff:127.0.0.1")]
    [InlineData("::ffff:10.1.2.3")]
    [InlineData("::ffff:169.254.169.254")]
    [InlineData("::ffff:172.16.0.1")]
    [InlineData("::ffff:192.168.1.1")]
    [InlineData("::ffff:100.64.0.1")]
    [InlineData("::ffff:0.0.0.0")]
    [InlineData("::ffff:224.0.0.1")]
    [InlineData("::ffff:255.255.255.255")]
    [InlineData("::ffff:7f00:1")]
    [InlineData("::ffff:a9fe:a9fe")]
    public void AnIpv4MappedIpv6FormOfABlockedAddressIsRefused(string text)
    {
        IpClassifier.IsBlocked(IPAddress.Parse(text)).Should().BeTrue();
    }

    [Theory]
    [InlineData("127.0.0.1", "loopback")]
    [InlineData("10.0.0.1", "private")]
    [InlineData("169.254.169.254", "linkLocal")]
    [InlineData("100.64.0.1", "sharedAddressSpace")]
    [InlineData("0.0.0.0", "unspecified")]
    public void TheCategoryNamesTheRangeWithoutNamingTheAddress(string text, string category)
    {
        IpClassifier.BlockedCategory(IPAddress.Parse(text)).Should().Be(category);
    }

    [Theory]
    [InlineData("2130706433", "127.0.0.1")]
    [InlineData("0x7f000001", "127.0.0.1")]
    [InlineData("0x7f.0.0.1", "127.0.0.1")]
    [InlineData("017700000001", "127.0.0.1")]
    [InlineData("0177.0.0.1", "127.0.0.1")]
    [InlineData("127.1", "127.0.0.1")]
    [InlineData("127.0.1", "127.0.0.1")]
    [InlineData("2852039166", "169.254.169.254")]
    [InlineData("0xa9fea9fe", "169.254.169.254")]
    [InlineData("0251.0376.0251.0376", "169.254.169.254")]
    [InlineData("0xA9.0xFE.0xA9.0xFE", "169.254.169.254")]
    [InlineData("10.1", "10.0.0.1")]
    [InlineData("3232235521", "192.168.0.1")]
    [InlineData("127.0.0.1.", "127.0.0.1")]
    public void ALegacyHostSpellingParsesToTheAddressTheSocketLayerWouldConnectTo(string spelling, string expected)
    {
        HostSpelling.TryParseAddress(spelling, out var address).Should().BeTrue();
        address.Should().Be(IPAddress.Parse(expected));
    }

    [Theory]
    [InlineData("[::1]")]
    [InlineData("[::ffff:127.0.0.1]")]
    [InlineData("::1")]
    [InlineData("[fe80::1%25eth0]")]
    public void BracketsAndScopeIdsDoNotHideAnIpv6Literal(string spelling)
    {
        HostSpelling.TryParseAddress(spelling, out var address).Should().BeTrue();
        IpClassifier.IsBlocked(address).Should().BeTrue();
    }

    [Theory]
    [InlineData("example.com")]
    [InlineData("payments.example.com")]
    [InlineData("a.b.c")]
    [InlineData("")]
    [InlineData("1.2.3.4.5")]
    [InlineData("256.1.1.1")]
    [InlineData("0xzz")]
    [InlineData("08.0.0.1")]
    public void ANameThatIsNotAnAddressDoesNotParse(string spelling)
    {
        HostSpelling.TryParseAddress(spelling, out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("10.0.0.0/8", "10.200.1.1", true)]
    [InlineData("10.0.0.0/8", "11.0.0.1", false)]
    [InlineData("127.0.0.0/8", "127.0.0.1", true)]
    [InlineData("192.168.1.0/24", "192.168.1.255", true)]
    [InlineData("192.168.1.0/24", "192.168.2.0", false)]
    [InlineData("10.1.2.3", "10.1.2.3", true)]
    [InlineData("10.1.2.3", "10.1.2.4", false)]
    [InlineData("::1/128", "::1", true)]
    [InlineData("fd00::/8", "fd12::1", true)]
    [InlineData("fd00::/8", "fe00::1", false)]
    [InlineData("10.0.0.0/8", "::ffff:10.0.0.1", true)]
    [InlineData("10.0.0.0/8", "fd00::1", false)]
    public void ACidrContainsOnlyItsOwnAddresses(string cidr, string candidate, bool contained)
    {
        IpRange.TryParse(cidr, out var range).Should().BeTrue();
        range.Contains(IPAddress.Parse(candidate)).Should().Be(contained);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-range")]
    [InlineData("10.0.0.0/33")]
    [InlineData("10.0.0.0/-1")]
    [InlineData("::1/129")]
    [InlineData("10.0.0.0/x")]
    public void AMalformedCidrDoesNotParse(string cidr)
    {
        IpRange.TryParse(cidr, out _).Should().BeFalse();
    }
}
