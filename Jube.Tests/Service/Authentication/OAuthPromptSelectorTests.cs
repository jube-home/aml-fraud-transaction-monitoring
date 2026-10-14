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

namespace Jube.Test.Service.Authentication;

[Trait("Category", "Unit")]
public sealed class OAuthPromptSelectorTests
{
    [Theory]
    [InlineData("login")]
    [InlineData("select_account")]
    [InlineData("consent")]
    public void Resolve_AppliesTheConfiguredPromptWhenNoneIsAlreadySet(string configuredPrompt)
    {
        OAuthPromptSelector.Resolve(configuredPrompt, null).Should().Be(configuredPrompt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Resolve_LeavesThePromptUnsetWhenNothingIsConfigured(string configuredPrompt)
    {
        OAuthPromptSelector.Resolve(configuredPrompt, null).Should().BeNull();
    }

    [Fact]
    public void Resolve_NeverOverwritesAPromptAlreadyOnTheProtocolMessage()
    {
        OAuthPromptSelector.Resolve("login", "consent").Should().Be("consent");
    }
}
