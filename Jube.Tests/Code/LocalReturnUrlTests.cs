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
using Xunit;

namespace Jube.Test.Code;

[Trait("Category", "Unit")]
public sealed class LocalReturnUrlTests
{
    [Theory]
    [InlineData("/", "/")]
    [InlineData("/ask-jooby", "/ask-jooby")]
    [InlineData("/preservation/move?tab=2#top", "/preservation/move?tab=2#top")]
    public void ALocalPathIsKept(string value, string expected)
    {
        LocalReturnUrl.Sanitise(value).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ask-jooby")]
    [InlineData("//evil.example")]
    [InlineData("/\\evil.example")]
    [InlineData("\\\\evil.example")]
    [InlineData("https://evil.example/")]
    [InlineData("javascript:alert(1)")]
    public void AnythingElseBecomesTheRoot(string? value)
    {
        LocalReturnUrl.Sanitise(value!).Should().Be("/");
    }
}
