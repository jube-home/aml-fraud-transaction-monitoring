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

using FluentAssertions;
using Jube.Dictionary.Extensions;
using Xunit;

namespace Jube.Test.Dictionary.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class NetworkAndGeoExtensionsTests
    {
        [Theory]
        [InlineData("192.168.1.10", true, false)]
        [InlineData("2001:db8::1", false, true)]
        [InlineData("::ffff:10.0.0.1", false, true)]
        [InlineData("1.2.3", false, false)]
        [InlineData("256.1.1.1", false, false)]
        [InlineData("fe80::1%eth0", false, false)]
        public void AddressFamilyIsDetectedFromText(string text, bool isV4, bool isV6)
        {
            text.IsIPv4Address().Should().Be(isV4);
            text.IsIPv6Address().Should().Be(isV6);
        }

        [Theory]
        [InlineData("10.1.2.3", true, false, false)]
        [InlineData("172.31.255.255", true, false, false)]
        [InlineData("100.64.0.1", true, false, false)]
        [InlineData("fd00::1", true, false, false)]
        [InlineData("127.0.0.1", false, true, false)]
        [InlineData("203.0.113.9", false, true, false)]
        [InlineData("8.8.8.8", false, false, true)]
        [InlineData("2606:4700::1111", false, false, true)]
        public void AddressesAreClassified(string text, bool isPrivate, bool isReserved, bool isPublic)
        {
            text.IsPrivateIpAddress().Should().Be(isPrivate);
            text.IsReservedIpAddress().Should().Be(isReserved);
            text.IsPublicIpAddress().Should().Be(isPublic);
        }

        [Fact]
        public void IsLoopbackIpAddressRecognisesBothFamilies()
        {
            "127.0.0.2".IsLoopbackIpAddress().Should().BeTrue();
            "::1".IsLoopbackIpAddress().Should().BeTrue();
            "10.0.0.1".IsLoopbackIpAddress().Should().BeFalse();
        }

        [Theory]
        [InlineData("10.20.30.40", "10.0.0.0/8", true)]
        [InlineData("11.0.0.1", "10.0.0.0/8", false)]
        [InlineData("2001:db8::42", "2001:db8::/32", true)]
        [InlineData("10.0.0.1", "2001:db8::/32", false)]
        [InlineData("10.0.0.1", "10.0.0.0/33", false)]
        [InlineData("10.0.0.1", "0.0.0.0/0", true)]
        public void IsInCidrMatchesTheNetworkPrefix(string address, string cidr, bool expected)
        {
            address.IsInCidr(cidr).Should().Be(expected);
        }

        [Fact]
        public void CidrRangeIsDescribed()
        {
            "192.168.1.77/24".CidrFirstAddress().Should().Be("192.168.1.0");
            "192.168.1.77/24".CidrLastAddress().Should().Be("192.168.1.255");
            "192.168.1.77/24".CidrAddressCount().Should().Be(256);
            "2001:db8::/126".CidrLastAddress().Should().Be("2001:db8::3");
            "nonsense".CidrAddressCount().Should().Be(double.NaN);
        }

        [Fact]
        public void IPv4AddressesConvertToAndFromNumbers()
        {
            "192.168.1.1".IPv4AddressToNumber().Should().Be(3232235777L);
            3232235777L.NumberToIPv4Address().Should().Be("192.168.1.1");
            "::1".IPv4AddressToNumber().Should().Be(-1);
            (-1L).NumberToIPv4Address().Should().BeEmpty();
        }

        [Fact]
        public void NormaliseIpAddressProducesTheCanonicalText()
        {
            "2001:0DB8:0000:0000:0000:0000:0000:0001".NormaliseIpAddress().Should().Be("2001:db8::1");
            "::ffff:10.0.0.1".NormaliseIpAddress().Should().Be("10.0.0.1");
            "bad".NormaliseIpAddress().Should().BeEmpty();
        }

        [Fact]
        public void InitialBearingDegreesPointsFromTheFirstToTheSecondPoint()
        {
            50.06639.InitialBearingDegrees(-5.71472, 58.64389, -3.07).Should().BeApproximately(9.1198, 1e-3);
            0d.InitialBearingDegrees(0, 0, 10).Should().BeApproximately(90, 1e-9);
            0d.InitialBearingDegrees(0, 91, 0).Should().Be(double.NaN);
        }

        [Fact]
        public void GeohashRoundTripsToTheCellCentre()
        {
            57.64911.ToGeohash(10.40744, 11).Should().Be("u4pruydqqvj");
            "u4pruydqqvj".GeohashLatitude().Should().BeApproximately(57.64911, 1e-5);
            "u4pruydqqvj".GeohashLongitude().Should().BeApproximately(10.40744, 1e-5);
            "u4pa!".GeohashLatitude().Should().Be(double.NaN);
            0d.ToGeohash(0, 13).Should().BeEmpty();
        }

        [Fact]
        public void IsValidCoordinateChecksBothRanges()
        {
            (-90d).IsValidCoordinate(180).Should().BeTrue();
            90.1d.IsValidCoordinate(0).Should().BeFalse();
            double.NaN.IsValidCoordinate(0).Should().BeFalse();
        }
    }
}