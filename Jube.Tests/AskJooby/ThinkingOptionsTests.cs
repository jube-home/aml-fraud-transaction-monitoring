/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using FluentAssertions;
using Jube.AskJooby.Reasoning;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class ThinkingOptionsTests
    {
        [Fact]
        public void RawOptionsDefaultsToHighWhenNoEffortIsSupplied()
        {
            var raw = ThinkingOptions.RawOptions();

#pragma warning disable SCME0001
            raw.Patch.GetString("$.reasoning_effort"u8).Should().Be("high");
            raw.Patch.GetString("$.thinking.type"u8).Should().Be("enabled");
#pragma warning restore SCME0001
        }

        [Theory]
        [InlineData(0, "low")]
        [InlineData(1, "high")]
        [InlineData(2, "xhigh")]
        [InlineData(3, "max")]
        public void RawOptionsMapsTheOrdinalToTheConfirmedDeepSeekEffortWord(int ordinal, string expected)
        {
            var raw = ThinkingOptions.RawOptions(ordinal);

#pragma warning disable SCME0001
            raw.Patch.GetString("$.reasoning_effort"u8).Should().Be(expected);
#pragma warning restore SCME0001
        }

        [Fact]
        public void TurningThinkingOffSendsNoEffortAtAll()
        {
            var raw = ThinkingOptions.DisabledOptions();

#pragma warning disable SCME0001
            raw.Patch.GetString("$.thinking.type"u8).Should().Be("disabled");

            raw.Patch.Contains("$.reasoning_effort"u8).Should().BeFalse(
                "the provider enumerates only [low high xhigh max] and answers a request carrying "
                + "anything else with HTTP 400, so a word meaning none does not switch reasoning "
                + "off -- it loses the call entirely, which is worse than reasoning when asked not to");
#pragma warning restore SCME0001
        }

        [Fact]
        public void EveryEffortWordOfferedIsOneTheProviderWillAccept()
        {
            var accepted = new[] { "low", "high", "xhigh", "max" };

            for (var ordinal = 0; ordinal < 4; ordinal++)
            {
#pragma warning disable SCME0001
                accepted.Should().Contain(ThinkingOptions.RawOptions(ordinal).Patch
                    .GetString("$.reasoning_effort"u8));
#pragma warning restore SCME0001
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(4)]
        public void RawOptionsFallsBackToHighForAnOutOfRangeOrdinal(int ordinal)
        {
            var raw = ThinkingOptions.RawOptions(ordinal);

#pragma warning disable SCME0001
            raw.Patch.GetString("$.reasoning_effort"u8).Should().Be("high");
#pragma warning restore SCME0001
        }
    }
}