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
using Jube.AskJooby.Observability;
using Microsoft.Extensions.AI;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class PromptCacheUsageTests
    {
        private static UsageDetails Usage(params (string Key, long Value)[] counts)
        {
            var details = new UsageDetails { InputTokenCount = 1000, OutputTokenCount = 10 };

            if (counts.Length == 0)
            {
                return details;
            }

            details.AdditionalCounts = new AdditionalPropertiesDictionary<long>();

            foreach (var (key, value) in counts)
            {
                details.AdditionalCounts[key] = value;
            }

            return details;
        }

        [Fact]
        public void TheProvidersOwnCacheReadCountIsTakenWhateverItCallsIt()
        {
            PromptCacheUsage.CachedInputTokens(Usage(("cache_read_input_tokens", 820)))
                .Should().Be(820, "DigitalOcean reports an Anthropic-shaped field name on an "
                                  + "OpenAI-shaped API, so the reader cannot assume either spelling");

            PromptCacheUsage.CachedInputTokens(Usage(("InputTokenDetails.CachedTokenCount", 640)))
                .Should().Be(640, "and the OpenAI spelling has to keep working for a provider that "
                                  + "does use it");
        }

        [Fact]
        public void AReportedMissIsZeroAndNotSilenceBecauseTheTwoMeanDifferentThings()
        {
            PromptCacheUsage.CachedInputTokens(Usage(("cache_read_input_tokens", 0)))
                .Should().Be(0, "a provider saying nothing came from cache is a measurement");

            (PromptCacheUsage.CachedInputTokens(Usage()) is null).Should().BeTrue(
                "a provider reporting no cache figure at all is the absence of a measurement, and "
                + "recording it as zero would make a silent provider look like a permanent miss");
        }

        [Fact]
        public void ACacheWriteIsNeverMistakenForARead()
        {
            (PromptCacheUsage.CachedInputTokens(Usage(("cache_creation_input_tokens", 5000))) is null)
                .Should().BeTrue(
                    "DigitalOcean say the creation counters are not a reliable signal on this model, "
                    + "and counting them as reads would report a cache that is working when it is not");
        }

        [Fact]
        public void AReadIsStillFoundAlongsideAWrite()
        {
            PromptCacheUsage.CachedInputTokens(Usage(
                    ("cache_creation_input_tokens", 5000),
                    ("cache_read_input_tokens", 311)))
                .Should().Be(311);
        }

        [Fact]
        public void AnUnrelatedCountIsNotReadAsACacheFigure()
        {
            (PromptCacheUsage.CachedInputTokens(Usage(("reasoning_tokens", 4096))) is null)
                .Should().BeTrue();
        }

        [Fact]
        public void NoUsageAtAllIsReportedAsNothingRatherThanThrowing()
        {
            (PromptCacheUsage.CachedInputTokens(null) is null).Should().BeTrue();
        }
    }
}