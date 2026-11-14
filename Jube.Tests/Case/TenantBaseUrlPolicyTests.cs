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
public sealed class TenantBaseUrlPolicyTests
{
    [Theory]
    [InlineData("https://scoring.example.com")]
    [InlineData("https://scoring.example.com/")]
    [InlineData("https://scoring.example.com:8443/api")]
    [InlineData("https://8.8.8.8/")]
    public void PublicHttpsUrlsArePermitted(string url)
    {
        TenantBaseUrlPolicy.Check(url, false).Should().BeNull();
    }

    [Theory]
    [InlineData(null, PolicyCode.NotAbsolute)]
    [InlineData("", PolicyCode.NotAbsolute)]
    [InlineData("scoring.example.com", PolicyCode.NotAbsolute)]
    [InlineData("http://scoring.example.com", PolicyCode.SchemeNotAllowed)]
    [InlineData("ftp://scoring.example.com", PolicyCode.SchemeNotAllowed)]
    [InlineData("file:///etc/passwd", PolicyCode.SchemeNotAllowed)]
    [InlineData("https://user:pass@scoring.example.com", PolicyCode.UserInfoNotAllowed)]
    [InlineData("https://user@scoring.example.com", PolicyCode.UserInfoNotAllowed)]
    [InlineData("https://scoring.example.com/?a=1", PolicyCode.NotAbsolute)]
    [InlineData("https://scoring.example.com/#frag", PolicyCode.NotAbsolute)]
    [InlineData("https://localhost", PolicyCode.HostBlocked)]
    [InlineData("https://app.localhost", PolicyCode.HostBlocked)]
    [InlineData("https://127.0.0.1", PolicyCode.HostBlocked)]
    [InlineData("https://127.1", PolicyCode.HostBlocked)]
    [InlineData("https://2130706433", PolicyCode.HostBlocked)]
    [InlineData("https://0x7f000001", PolicyCode.HostBlocked)]
    [InlineData("https://[::1]", PolicyCode.HostBlocked)]
    [InlineData("https://[::ffff:127.0.0.1]", PolicyCode.HostBlocked)]
    [InlineData("https://169.254.169.254/latest/meta-data/", PolicyCode.HostBlocked)]
    [InlineData("https://[fe80::1]", PolicyCode.HostBlocked)]
    [InlineData("https://10.0.0.5", PolicyCode.HostBlocked)]
    [InlineData("https://172.16.0.1", PolicyCode.HostBlocked)]
    [InlineData("https://192.168.1.1", PolicyCode.HostBlocked)]
    [InlineData("https://[fd00::1]", PolicyCode.HostBlocked)]
    [InlineData("https://224.0.0.1", PolicyCode.HostBlocked)]
    [InlineData("https://0.0.0.0", PolicyCode.HostBlocked)]
    public void UnsafeUrlsAreRefusedWithTheirCode(string? url, string expected)
    {
        TenantBaseUrlPolicy.Check(url, false).Should().Be(expected);
    }

    [Theory]
    [InlineData("http://localhost:5001")]
    [InlineData("http://127.0.0.1:5001/")]
    [InlineData("https://10.0.0.5")]
    public void TheDevelopmentFlagPermitsHttpAndInternalHostsButNotCredentialsOrOtherSchemes(string url)
    {
        TenantBaseUrlPolicy.Check(url, true).Should().BeNull();
        TenantBaseUrlPolicy.Check("https://user:pass@localhost", true).Should().Be(PolicyCode.UserInfoNotAllowed);
        TenantBaseUrlPolicy.Check("gopher://localhost", true).Should().Be(PolicyCode.SchemeNotAllowed);
    }

    [Fact]
    public void ResolvedAddressesAreCheckedSoARebindingNameIsRefused()
    {
        var refusal = TenantBaseUrlPolicy.CheckResolved([IPAddress.Parse("169.254.169.254")], false,
            out var permitted);

        refusal.Should().Be(PolicyCode.ResolvesToBlockedAddress);
        permitted.Should().BeEmpty();
    }

    [Fact]
    public void AMixedAnswerKeepsOnlyThePublicAddress()
    {
        var refusal = TenantBaseUrlPolicy.CheckResolved([IPAddress.Parse("10.0.0.1"), IPAddress.Parse("8.8.8.8")],
            false, out var permitted);

        refusal.Should().BeNull();
        permitted.Should().Equal(IPAddress.Parse("8.8.8.8"));
    }

    [Fact]
    public void NoAddressIsNotResolved()
    {
        TenantBaseUrlPolicy.CheckResolved([], false, out _).Should().Be(PolicyCode.HostNotResolved);
    }

    [Fact]
    public void TheDevelopmentFlagPermitsInternalAddresses()
    {
        TenantBaseUrlPolicy.CheckResolved([IPAddress.Loopback], true, out var permitted).Should().BeNull();
        permitted.Should().Equal(IPAddress.Loopback);
    }

    [Fact]
    public void TheFlagKeyIsTheDocumentedSetting()
    {
        TenantBaseUrlPolicy.AllowInsecureTargetsKey.Should().Be("HttpAdaptationAllowInsecureTargets");
    }
}
