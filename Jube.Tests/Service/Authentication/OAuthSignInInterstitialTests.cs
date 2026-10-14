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
using Jube.App.Code;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Jube.Test.Service.Authentication;

[Trait("Category", "Unit")]
public sealed class OAuthSignInInterstitialTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("/Model/Frame/Exhaustive?modelId=7&tab=2")]
    [InlineData("https://jube.example.com/Integrity")]
    [InlineData("http://localhost:5001/")]
    public void IsSafeTarget_AcceptsLocalPathsAndHttpUrls(string target)
    {
        OAuthSignInInterstitial.IsSafeTarget(target).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("//evil.example.com")]
    [InlineData("/\\evil.example.com")]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://files.example.com/")]
    [InlineData("not a uri with spaces")]
    [InlineData("/ok\r\nSet-Cookie: injected=1")]
    [InlineData("https://jube.example.com/\t")]
    public void IsSafeTarget_RejectsOpenRedirectsSchemesAndHeaderInjection(string target)
    {
        OAuthSignInInterstitial.IsSafeTarget(target).Should().BeFalse();
    }

    [Fact]
    public void Write_SetsA200WithRefreshToTheTargetAndNoCaching()
    {
        var context = new DefaultHttpContext();

        OAuthSignInInterstitial.Write(context.Response, "/Model/Frame/Exhaustive?modelId=7");

        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        context.Response.ContentType.Should().Be("text/html");
        context.Response.ContentLength.Should().Be(0);
        context.Response.Headers.CacheControl.ToString().Should().Be("no-store");
        context.Response.Headers["Refresh"].ToString().Should().Be("0; url=/Model/Frame/Exhaustive?modelId=7");
        context.Response.Headers.Location.Should().BeEmpty();
    }
}