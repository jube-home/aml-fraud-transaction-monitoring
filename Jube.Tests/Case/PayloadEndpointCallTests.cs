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
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Case.Outbound;
using Jube.Test.Case.Models;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.Case;

[Trait("Category", "Unit")]
public sealed class PayloadEndpointCallTests
{
    private static Jube.DynamicEnvironment.DynamicEnvironment Strict =>
        TestDynamicEnvironment.Create(new Dictionary<string, string>());

    private static Jube.DynamicEnvironment.DynamicEnvironment Loopback =>
        TestDynamicEnvironment.Create(new Dictionary<string, string>
        {
            [OutboundHostPolicy.AllowedCidrsKey] = LoopbackAllowed.Cidrs
        });

    [Theory]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    [InlineData("http://10.0.0.5/release")]
    [InlineData("http://localhost/release")]
    [InlineData("http://[::1]/release")]
    [InlineData("http://2852039166/latest/meta-data")]
    public async Task AnInternalLiteralTargetIsRefusedAndNeverCalled(string url)
    {
        var result = await PayloadEndpointCall.SendAsync(Strict, url, text => text, true, () => "{}", TestLog.NoOp);

        result.Ok.Should().BeFalse();
        result.Refusal.Should().NotBeNull();
    }

    [Theory]
    [InlineData("[@Payload.Host@]/release")]
    [InlineData("https://[@Payload.Host@]/release")]
    [InlineData("https://payments.example.test[@Payload.Path@]")]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://payments.example.test/x")]
    [InlineData("https://user:pass@payments.example.test/x")]
    public async Task AHostThatTheTransactionCouldChooseOrAnotherSchemeIsRefused(string template)
    {
        var result = await PayloadEndpointCall.SendAsync(Loopback, template,
            text => text.Replace("[@Payload.Host@]", "169.254.169.254").Replace("[@Payload.Path@]", "@evil.test/x"),
            false, () => string.Empty, TestLog.NoOp);

        result.Ok.Should().BeFalse();
        result.Refusal.Should().NotBeNull();
    }

    [Fact]
    public async Task ATokenInThePathIsRenderedAndAnAllowedHostIsCalled()
    {
        using var listener = new LoopbackListener();

        var result = await PayloadEndpointCall.SendAsync(Loopback, $"http://127.0.0.1:{listener.Port}/case/[@Payload.Id@]",
            text => text.Replace("[@Payload.Id@]", "42"), true, () => "{}", TestLog.NoOp);

        result.Ok.Should().BeTrue();
        listener.Paths.Should().Equal("/case/42");
    }

    [Fact]
    public async Task ARepeatedlyFailingHostIsSuspendedAndNoLongerCalled()
    {
        using var listener = new LoopbackListener(_ => (500, null));
        var url = $"http://127.0.0.1:{listener.Port}/fail";

        for (var attempt = 0; attempt < 12; attempt++)
        {
            await PayloadEndpointCall.SendAsync(Loopback, url, text => text, false, () => string.Empty, TestLog.NoOp);
        }

        var calls = listener.Paths.Length;
        var next = await PayloadEndpointCall.SendAsync(Loopback, url, text => text, false, () => string.Empty,
            TestLog.NoOp);

        next.Ok.Should().BeFalse();
        next.Error.Should().Contain("suspended");
        listener.Paths.Length.Should().Be(calls);
        calls.Should().BeLessThan(12);
    }
}
