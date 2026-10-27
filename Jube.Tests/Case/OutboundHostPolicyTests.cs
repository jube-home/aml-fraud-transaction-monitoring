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
using System.Net;
using FluentAssertions;
using Jube.Case;
using Jube.Case.Outbound;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.Case;

[Trait("Category", "Unit")]
public sealed class OutboundHostPolicyTests
{
    private static readonly OutboundHostPolicy Strict = OutboundHostPolicy.Default;

    [Theory]
    [InlineData("http://localhost/hook")]
    [InlineData("http://LOCALHOST:8080/hook")]
    [InlineData("http://localhost./hook")]
    [InlineData("http://api.localhost/hook")]
    [InlineData("http://127.0.0.1/hook")]
    [InlineData("http://127.1/hook")]
    [InlineData("http://2130706433/hook")]
    [InlineData("http://0x7f000001/hook")]
    [InlineData("http://0177.0.0.1/hook")]
    [InlineData("http://[::1]/hook")]
    [InlineData("http://[::ffff:127.0.0.1]/hook")]
    [InlineData("http://[::ffff:7f00:1]/hook")]
    [InlineData("http://169.254.169.254/latest/meta-data/")]
    [InlineData("http://2852039166/latest/meta-data/")]
    [InlineData("http://[fd00:ec2::254]/latest/meta-data/")]
    [InlineData("http://10.0.0.5/hook")]
    [InlineData("https://172.16.4.4/hook")]
    [InlineData("https://192.168.0.10:8443/hook")]
    [InlineData("http://100.64.1.1/hook")]
    [InlineData("http://[fc00::1]/hook")]
    [InlineData("http://[fe80::1]/hook")]
    [InlineData("http://0.0.0.0/hook")]
    [InlineData("http://224.0.0.1/hook")]
    [InlineData("http://255.255.255.255/hook")]
    [InlineData("http://[::]/hook")]
    public void AnInternalLiteralHostIsBlockedByDefault(string url)
    {
        Strict.CheckUri(new Uri(url)).Should().Be(PolicyCode.HostBlocked);
    }

    [Theory]
    [InlineData("ftp://payments.example.com/hook", PolicyCode.SchemeNotAllowed)]
    [InlineData("file:///etc/hosts", PolicyCode.SchemeNotAllowed)]
    [InlineData("gopher://payments.example.com/", PolicyCode.SchemeNotAllowed)]
    [InlineData("ws://payments.example.com/", PolicyCode.SchemeNotAllowed)]
    [InlineData("https://user@payments.example.com/hook", PolicyCode.UserInfoNotAllowed)]
    [InlineData("https://user:pass@payments.example.com/hook", PolicyCode.UserInfoNotAllowed)]
    [InlineData("https://payments.example.com@169.254.169.254/hook", PolicyCode.UserInfoNotAllowed)]
    public void ANonHttpSchemeOrUserInfoIsRefused(string url, string code)
    {
        Strict.CheckUri(new Uri(url)).Should().Be(code);
    }

    [Theory]
    [InlineData("https://payments.example.com/v1/release")]
    [InlineData("http://payments.example.com:8080/release")]
    [InlineData("https://93.184.216.34/release")]
    [InlineData("https://[2606:4700:4700::1111]/release")]
    public void APublicHostIsAcceptedAtSaveTime(string url)
    {
        Strict.CheckUri(new Uri(url)).Should().BeNull();
    }

    [Fact]
    public void ATokenInThePathIsNeutralisedBeforeTheHostIsChecked()
    {
        Strict.CheckTemplate("https://payments.example.com/release/[@Payload.PaymentId@]").Should().BeNull();
        Strict.CheckTemplate("http://10.0.0.5/release/[@Payload.PaymentId@]").Should().Be(PolicyCode.HostBlocked);
        Strict.CheckTemplate("http://169.254.169.254/[@Payload.PaymentId@]").Should().Be(PolicyCode.HostBlocked);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AnEmptyTemplateIsNotAnAbsoluteAddress(string? template)
    {
        Strict.CheckTemplate(template).Should().Be(PolicyCode.NotAbsolute);
    }

    [Fact]
    public void ATokenCannotSupplyTheHostSoTheTemplateIsRefusedBeforeTheHostPolicyRuns()
    {
        Strict.CheckTemplate("https://[@Payload.Host@]/release").Should().NotBeNull();
    }

    [Fact]
    public void AnAllowedHostIsAcceptedWhateverItResolvesTo()
    {
        var policy = new OutboundHostPolicy(["internal.payments.local", "*.corp.example"]);

        policy.CheckUri(new Uri("https://internal.payments.local/hook")).Should().BeNull();
        policy.CheckUri(new Uri("https://INTERNAL.payments.local./hook")).Should().BeNull();
        policy.CheckUri(new Uri("https://a.corp.example/hook")).Should().BeNull();
        policy.CheckUri(new Uri("https://a.b.corp.example/hook")).Should().BeNull();
        policy.IsAddressPermitted(IPAddress.Parse("10.1.1.1"), "internal.payments.local").Should().BeTrue();
        policy.IsAddressPermitted(IPAddress.Parse("10.1.1.1"), "a.corp.example").Should().BeTrue();
    }

    [Fact]
    public void AnAllowedHostDoesNotAllowItsNeighboursOrALookalikeSuffix()
    {
        var policy = new OutboundHostPolicy(["internal.payments.local", "*.corp.example"]);

        policy.IsAddressPermitted(IPAddress.Parse("10.1.1.1"), "other.payments.local").Should().BeFalse();
        policy.IsAddressPermitted(IPAddress.Parse("10.1.1.1"), "evilcorp.example").Should().BeFalse();
        policy.IsAddressPermitted(IPAddress.Parse("10.1.1.1"), "corp.example.evil.com").Should().BeFalse();
        policy.IsAddressPermitted(IPAddress.Parse("10.1.1.1"), "xinternal.payments.local").Should().BeFalse();
    }

    [Fact]
    public void AnAllowedCidrPermitsOnlyThatRangeAndNothingElseInternal()
    {
        var policy = new OutboundHostPolicy(null, ["10.20.0.0/16"]);

        policy.CheckUri(new Uri("http://10.20.4.5/hook")).Should().BeNull();
        policy.CheckUri(new Uri("http://10.21.0.1/hook")).Should().Be(PolicyCode.HostBlocked);
        policy.CheckUri(new Uri("http://169.254.169.254/hook")).Should().Be(PolicyCode.HostBlocked);
        policy.CheckUri(new Uri("http://127.0.0.1/hook")).Should().Be(PolicyCode.HostBlocked);
    }

    [Fact]
    public void AnAllowedLiteralHostOpensOnlyThatLiteral()
    {
        var policy = new OutboundHostPolicy(["10.0.0.5"]);

        policy.CheckUri(new Uri("http://10.0.0.5/hook")).Should().BeNull();
        policy.CheckUri(new Uri("http://10.0.0.6/hook")).Should().Be(PolicyCode.HostBlocked);
    }

    [Fact]
    public void AnInvalidCidrIsReportedAndPermitsNothing()
    {
        var policy = new OutboundHostPolicy(null, ["10.0.0.0/99", "garbage", "127.0.0.0/8"]);

        policy.InvalidAllowedCidrs.Should().BeEquivalentTo("10.0.0.0/99", "garbage");
        policy.CheckUri(new Uri("http://127.0.0.1/")).Should().BeNull();
        policy.CheckUri(new Uri("http://10.0.0.1/")).Should().Be(PolicyCode.HostBlocked);
    }

    [Fact]
    public void ResolutionToOnlyBlockedAddressesIsRefused()
    {
        var code = Strict.CheckResolved("payments.example.com",
            [IPAddress.Parse("10.0.0.5"), IPAddress.Parse("169.254.169.254")], out var permitted);

        code.Should().Be(PolicyCode.ResolvesToBlockedAddress);
        permitted.Should().BeEmpty();
    }

    [Fact]
    public void ResolutionToNothingIsRefused()
    {
        Strict.CheckResolved("payments.example.com", [], out _).Should().Be(PolicyCode.HostNotResolved);
    }

    [Fact]
    public void AMixedAnswerKeepsOnlyThePublicAddresses()
    {
        var code = Strict.CheckResolved("payments.example.com",
            [IPAddress.Parse("10.0.0.5"), IPAddress.Parse("93.184.216.34"), IPAddress.Parse("::1")],
            out var permitted);

        code.Should().BeNull();
        permitted.Should().Equal(IPAddress.Parse("93.184.216.34"));
    }

    [Fact]
    public void ThePolicyIsReadFromTheDynamicEnvironment()
    {
        var environment = TestDynamicEnvironment.Create(new Dictionary<string, string>
        {
            [OutboundHostPolicy.AllowedHostsKey] = "hooks.internal, *.corp.example",
            [OutboundHostPolicy.AllowedCidrsKey] = "10.9.0.0/16;192.168.5.0/24"
        });

        var policy = OutboundHostPolicy.From(environment);

        policy.CheckUri(new Uri("https://hooks.internal/x")).Should().BeNull();
        policy.CheckUri(new Uri("https://a.corp.example/x")).Should().BeNull();
        policy.CheckUri(new Uri("http://10.9.1.1/x")).Should().BeNull();
        policy.CheckUri(new Uri("http://192.168.5.9/x")).Should().BeNull();
        policy.CheckUri(new Uri("http://192.168.6.9/x")).Should().Be(PolicyCode.HostBlocked);
        OutboundHostPolicy.From(environment).Should().BeSameAs(policy);
    }

    [Fact]
    public void WithNoConfigurationNothingInternalIsAllowed()
    {
        var policy = OutboundHostPolicy.From(TestDynamicEnvironment.Create());

        policy.CheckUri(new Uri("http://127.0.0.1/x")).Should().Be(PolicyCode.HostBlocked);
        policy.CheckUri(new Uri("http://10.0.0.1/x")).Should().Be(PolicyCode.HostBlocked);
        policy.InvalidAllowedCidrs.Should().BeEmpty();
    }

    [Theory]
    [InlineData("JUBE_HOOK_ALLOWED_HOSTS")]
    [InlineData("JUBE_HOOK_ALLOWED_CIDRS")]
    [InlineData("JUBE_HOOK_ALLOWED_ANYTHING")]
    public void TheAllowListVariablesCanNeverBeNamedAsACredential(string name)
    {
        HookCredentials.IsAllowedSecretName(name).Should().BeFalse();
    }

    [Fact]
    public void AnOrdinaryHookCredentialNameIsStillAccepted()
    {
        HookCredentials.IsAllowedSecretName("JUBE_HOOK_PAYMENTS_TOKEN").Should().BeTrue();
    }

    [Fact]
    public void ARefusedDispatchCarriesTheCodeAndNoAddress()
    {
        var result = DispatchResult.Refused(PolicyCode.HostBlocked);

        result.Ok.Should().BeFalse();
        result.Refusal.Should().Be(PolicyCode.HostBlocked);
        result.Error.Should().Contain(PolicyCode.HostBlocked);
        result.Error.Should().NotContainAny("10.", "169.254", "127.0");
    }
}
