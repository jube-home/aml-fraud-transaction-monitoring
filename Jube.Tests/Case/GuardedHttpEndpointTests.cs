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

using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Case.Outbound;
using Jube.Test.Case.Models;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.Case;

[Trait("Category", "Unit")]
public sealed class GuardedHttpEndpointTests
{
    private static readonly IPAddress[] Loopback = [IPAddress.Loopback];

    private static OutboundHostPolicy AllowLoopback => new(null, ["127.0.0.0/8"]);

    [Fact]
    public async Task ANamedHostIsConnectedToTheAddressTheResolverReturnedAndNoOther()
    {
        using var listener = new LoopbackListener();
        var resolver = new StubHostResolver(_ => Loopback);
        var endpoint = new GuardedHttpEndpoint(AllowLoopback, resolver);

        var result = await endpoint.PostAsync($"http://payments.example.test:{listener.Port}/release", "{}",
            TestLog.NoOp);

        result.Ok.Should().BeTrue();
        listener.Paths.Should().Equal("/release");
        resolver.Asked.Should().Contain("payments.example.test");
    }

    [Fact]
    public async Task ANameThatResolvesToAnInternalAddressIsRefusedAndNeverConnected()
    {
        using var listener = new LoopbackListener();
        var resolver = new StubHostResolver(_ => Loopback);
        var endpoint = new GuardedHttpEndpoint(OutboundHostPolicy.Default, resolver);

        var result = await endpoint.PostAsync($"http://payments.example.test:{listener.Port}/release", "{}",
            TestLog.NoOp);

        result.Ok.Should().BeFalse();
        result.Refusal.Should().Be(PolicyCode.ResolvesToBlockedAddress);
        result.Error.Should().NotContain("127.0.0.1");
        listener.Paths.Should().BeEmpty();
    }

    [Theory]
    [InlineData("10.0.0.5")]
    [InlineData("169.254.169.254")]
    [InlineData("::1")]
    [InlineData("::ffff:127.0.0.1")]
    [InlineData("fd00::1")]
    [InlineData("100.64.0.1")]
    public async Task EveryInternalAnswerFromTheResolverIsRefused(string answer)
    {
        var resolver = new StubHostResolver(_ => [IPAddress.Parse(answer)]);
        var endpoint = new GuardedHttpEndpoint(OutboundHostPolicy.Default, resolver);

        var result = await endpoint.GetAsync("http://payments.example.test/release", TestLog.NoOp);

        result.Refusal.Should().Be(PolicyCode.ResolvesToBlockedAddress);
    }

    [Fact]
    public async Task ANameThatResolvesToNothingIsRefused()
    {
        var resolver = new StubHostResolver(_ => []);
        var endpoint = new GuardedHttpEndpoint(OutboundHostPolicy.Default, resolver);

        var result = await endpoint.GetAsync("http://payments.example.test/release", TestLog.NoOp);

        result.Refusal.Should().Be(PolicyCode.HostNotResolved);
    }

    [Fact]
    public async Task ARebindingAnswerIsCheckedAtConnectTimeNotJustAtSaveTime()
    {
        using var listener = new LoopbackListener();
        var calls = 0;
        var resolver = new StubHostResolver(_ => ++calls == 1 ? [IPAddress.Parse("93.184.216.34")] : Loopback);
        var endpoint = new GuardedHttpEndpoint(OutboundHostPolicy.Default, resolver);

        var save = OutboundHostPolicy.Default.CheckTemplate($"http://rebind.example.test:{listener.Port}/release");
        save.Should().BeNull();

        var first = await endpoint.GetAsync($"http://rebind.example.test:{listener.Port}/release", TestLog.NoOp);
        var second = await endpoint.GetAsync($"http://rebind.example.test:{listener.Port}/release", TestLog.NoOp);

        first.Ok.Should().BeFalse();
        second.Refusal.Should().Be(PolicyCode.ResolvesToBlockedAddress);
        listener.Paths.Should().BeEmpty();
    }

    [Fact]
    public async Task AMixedAnswerIsPinnedToThePermittedAddressOnly()
    {
        using var listener = new LoopbackListener();
        var resolver = new StubHostResolver(_ => [IPAddress.Parse("10.0.0.5"), IPAddress.Loopback]);
        var endpoint = new GuardedHttpEndpoint(AllowLoopback, resolver);

        var result = await endpoint.GetAsync($"http://payments.example.test:{listener.Port}/release", TestLog.NoOp);

        result.Ok.Should().BeTrue();
        listener.Paths.Should().Equal("/release");
    }

    [Theory]
    [InlineData("http://127.0.0.1:1/x")]
    [InlineData("http://169.254.169.254/latest/meta-data/")]
    [InlineData("http://2130706433/x")]
    [InlineData("http://[::1]/x")]
    [InlineData("http://localhost/x")]
    [InlineData("ftp://payments.example.test/x")]
    [InlineData("https://user:pass@payments.example.test/x")]
    public async Task ALiteralInternalOrDisallowedTargetIsRefusedBeforeAnySocketIsOpened(string url)
    {
        var resolver = new StubHostResolver(_ => Loopback);
        var endpoint = new GuardedHttpEndpoint(OutboundHostPolicy.Default, resolver);

        var result = await endpoint.PostAsync(url, "{}", TestLog.NoOp);

        result.Ok.Should().BeFalse();
        result.Refusal.Should().NotBeNull();
        resolver.Asked.Should().BeEmpty();
    }

    [Fact]
    public async Task ARelativeAddressIsRefused()
    {
        var endpoint = new GuardedHttpEndpoint(OutboundHostPolicy.Default, new StubHostResolver(_ => Loopback));

        var result = await endpoint.GetAsync("payments.example.test/release", TestLog.NoOp);

        result.Refusal.Should().Be(PolicyCode.NotAbsolute);
    }

    [Theory]
    [InlineData(301)]
    [InlineData(302)]
    [InlineData(303)]
    [InlineData(307)]
    [InlineData(308)]
    public async Task ARedirectIsRefusedAndTheLocationIsNeverFollowed(int status)
    {
        using var listener = new LoopbackListener(path => path == "/release"
            ? (status, $"/followed")
            : (200, null));
        var endpoint = new GuardedHttpEndpoint(AllowLoopback, new StubHostResolver(_ => Loopback));

        var result = await endpoint.PostAsync($"http://127.0.0.1:{listener.Port}/release", "{}", TestLog.NoOp);

        result.Ok.Should().BeFalse();
        result.Refusal.Should().Be(PolicyCode.RedirectRefused);
        result.Status.Should().Be(status);
        listener.Paths.Should().Equal("/release");
    }

    [Fact]
    public async Task ARedirectToTheMetadataAddressIsNotFollowedEither()
    {
        using var listener = new LoopbackListener(_ => (302, "http://169.254.169.254/latest/meta-data/"));
        var resolver = new StubHostResolver(_ => Loopback);
        var endpoint = new GuardedHttpEndpoint(AllowLoopback, resolver);

        var result = await endpoint.GetAsync($"http://127.0.0.1:{listener.Port}/release", TestLog.NoOp);

        result.Refusal.Should().Be(PolicyCode.RedirectRefused);
        listener.Paths.Should().HaveCount(1);
        resolver.Asked.Should().BeEmpty();
    }

    [Fact]
    public async Task AnAllowListedCidrPermitsALoopbackReceiverAndASuccessIsReported()
    {
        using var listener = new LoopbackListener();
        var endpoint = GuardedHttpEndpoint.For(LoopbackAllowed.Environment);

        var result = await endpoint.PostAsync($"http://127.0.0.1:{listener.Port}/release", "{}", TestLog.NoOp);

        result.Ok.Should().BeTrue();
        result.Refusal.Should().BeNull();
        result.Status.Should().Be(200);
    }

    [Fact]
    public async Task ANon2xxFromAPermittedHostIsAFailureNotARefusal()
    {
        using var listener = new LoopbackListener(_ => (500, null));
        var endpoint = new GuardedHttpEndpoint(AllowLoopback, new StubHostResolver(_ => Loopback));

        var result = await endpoint.PostAsync($"http://127.0.0.1:{listener.Port}/release", "{}", TestLog.NoOp);

        result.Ok.Should().BeFalse();
        result.Refusal.Should().BeNull();
        result.Status.Should().Be(500);
    }

    [Fact]
    public async Task TheSharedEndpointForAnEnvironmentWithNoAllowListRefusesLoopback()
    {
        using var listener = new LoopbackListener();
        var endpoint = GuardedHttpEndpoint.For(TestDynamicEnvironment.Create(new Dictionary<string, string>()));

        var result = await endpoint.PostAsync($"http://127.0.0.1:{listener.Port}/release", "{}", TestLog.NoOp);

        result.Refusal.Should().Be(PolicyCode.HostBlocked);
        listener.Paths.Should().BeEmpty();
    }
}
