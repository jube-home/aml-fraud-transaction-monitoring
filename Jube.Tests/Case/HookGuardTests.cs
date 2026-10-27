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
using FluentAssertions;
using Jube.Case;
using Xunit;

namespace Jube.Test.Case;

[Trait("Category", "Unit")]
public sealed class HookGuardTests
{
    private static string Render(string template) => HookTokens.Replace(template,
        new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Payload.PaymentId"] = "a b/c?d=e",
            ["Hold.Name"] = "Review hold",
            ["Hold.Elevation"] = "150"
        }, HookTokens.Url);

    [Theory]
    [InlineData("https://payments.example.com/release/[@Payload.PaymentId@]")]
    [InlineData("http://payments.example.com:8080/release?name=[@Hold.Name@]&level=[@Hold.Elevation@]")]
    [InlineData("https://payments.example.com/release")]
    public void ALiteralHostWithTokensOnlyInThePathAndQueryIsAccepted(string template)
    {
        var (url, refusal) = WebhookTarget.Resolve(template, Render);

        refusal.Should().BeNull();
        url.Should().StartWith(template[..template.IndexOf("example.com", StringComparison.Ordinal)]);
    }

    [Fact]
    public void ATokenValueIsEscapedSoItCanNeverAddAPathSegmentOrAQueryParameter()
    {
        var (url, refusal) = WebhookTarget.Resolve("https://payments.example.com/release/[@Payload.PaymentId@]", Render);

        refusal.Should().BeNull();
        url.Should().Be("https://payments.example.com/release/a%20b%2Fc%3Fd%3De");
    }

    [Theory]
    [InlineData("https://[@Payload.PaymentId@]/release", WebhookTarget.AuthorityNotLiteral)]
    [InlineData("[@Payload.PaymentId@]://payments.example.com", WebhookTarget.AuthorityNotLiteral)]
    [InlineData("https://payments.example.com[@Payload.PaymentId@]", WebhookTarget.AuthorityNotLiteral)]
    [InlineData("payments.example.com/release", WebhookTarget.NotAbsolute)]
    [InlineData("ftp://payments.example.com/release", WebhookTarget.SchemeNotAllowed)]
    [InlineData("", WebhookTarget.Empty)]
    public void AHostThatIsNotWrittenOutLiterallyIsRefused(string template, string refusal)
    {
        WebhookTarget.Resolve(template, Render).Refusal.Should().Be(refusal);
    }

    [Fact]
    public void ATokenThatRendersToAnotherHostIsRefused()
    {
        var (url, refusal) = WebhookTarget.Resolve("https://payments.example.com/release",
            _ => "https://evil.example.org/release");

        url.Should().BeNull();
        refusal.Should().Be(WebhookTarget.HostChanged);
    }

    [Fact]
    public void ARenderedAddressWithCredentialsInTheAuthorityIsRefused()
    {
        WebhookTarget.Resolve("https://payments.example.com/release",
            _ => "https://user:pass@payments.example.com/release").Refusal.Should().Be(WebhookTarget.HostChanged);
    }

    [Fact]
    public void AnOverlongAddressIsRefused()
    {
        WebhookTarget.Resolve("https://payments.example.com/release",
            _ => "https://payments.example.com/" + new string('a', 3000)).Refusal.Should().Be(WebhookTarget.TooLong);
    }

    [Theory]
    [InlineData("JUBE_HOOK_PAYMENTS", true)]
    [InlineData("JUBE_HOOK_A", true)]
    [InlineData("ConnectionString", false)]
    [InlineData("JWTKey", false)]
    [InlineData("PasswordHashingKey", false)]
    [InlineData("jube_hook_payments", false)]
    [InlineData("JUBE_HOOK_", false)]
    [InlineData("JUBE_HOOK_BAD NAME", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void OnlyAVariableWithTheHookPrefixCanBeNamedAsACredential(string? name, bool allowed)
    {
        HookCredentials.IsAllowedSecretName(name).Should().Be(allowed);
    }

    [Fact]
    public void ACredentialIsReadOnlyFromAVariableWithTheHookPrefix()
    {
        Environment.SetEnvironmentVariable("JUBE_HOOK_UNITTEST", "s3cret");

        try
        {
            HookCredentials.ReadSecret("JUBE_HOOK_UNITTEST").Should().Be("s3cret");
            HookCredentials.ReadSecret("PATH").Should().BeNull("a variable without the prefix is never read");
            HookCredentials.ReadSecret(null).Should().BeNull();
        }
        finally
        {
            Environment.SetEnvironmentVariable("JUBE_HOOK_UNITTEST", null);
        }
    }

    [Theory]
    [InlineData("Authorization", true)]
    [InlineData("X-Api-Key", true)]
    [InlineData("Host", false)]
    [InlineData("content-length", false)]
    [InlineData("Content-Type", false)]
    [InlineData("X-Correlation-Id", false)]
    [InlineData("Bad Header", false)]
    [InlineData("Bad:Header", false)]
    [InlineData("", false)]
    public void AReservedOrMalformedHeaderNameIsRefused(string name, bool allowed)
    {
        HookCredentials.IsAllowedHeaderName(name).Should().Be(allowed);
    }

    [Fact]
    public void TokensAreMatchedWithoutRegardToCaseAndUnknownOnesAreLeftAlone()
    {
        HookTokens.Replace("[@payload.paymentid@] [@Unknown.Token@]",
            new Dictionary<string, string?> { ["Payload.PaymentId"] = "42" }, HookTokens.Url)
            .Should().Be("42 [@Unknown.Token@]");
    }

    [Fact]
    public void ABodyValueIsEscapedForJsonSoItCanNeverBreakOutOfTheString()
    {
        HookTokens.Replace("{\"reason\":\"[@Decision.Reason@]\"}",
            new Dictionary<string, string?> { ["Decision.Reason"] = "say \"stop\"\n\\" }, HookTokens.Json)
            .Should().Be("{\"reason\":\"say \\\"stop\\\"\\n\\\\\"}");
    }
}
