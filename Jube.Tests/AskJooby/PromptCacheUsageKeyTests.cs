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
    public sealed class PromptCacheUsageKeyTests
    {
        private static UsageDetails Usage(params (string Key, long Value)[] counts)
        {
            var usage = new UsageDetails { AdditionalCounts = new AdditionalPropertiesDictionary<long>() };

            foreach (var (key, value) in counts)
            {
                usage.AdditionalCounts[key] = value;
            }

            return usage;
        }

        [Fact]
        public void TheHitCountIsReadAndTheMissCountBesideItIsNot()
        {
            var usage = Usage(("prompt_cache_hit_tokens", 9000), ("prompt_cache_miss_tokens", 1000));

            PromptCacheUsage.CachedInputTokens(usage).Should().Be(9000);
        }

        [Fact]
        public void AMissCountOnItsOwnIsNeverMistakenForACachedFigure()
        {
            PromptCacheUsage.CachedInputTokens(Usage(("prompt_cache_miss_tokens", 1000)))
                .Should().BeNull("nothing here says any part of the prompt was served from cache");
        }

        [Fact]
        public void ACacheWriteIsNotACacheRead()
        {
            PromptCacheUsage.CachedInputTokens(Usage(("cache_creation_input_tokens", 4000)))
                .Should().BeNull("filling a cache is charged, not discounted");
        }

        [Fact]
        public void AProviderReportingNothingYieldsNothingRatherThanAZeroReading()
        {
            PromptCacheUsage.CachedInputTokens(new UsageDetails()).Should().BeNull(
                "silence from the provider has to stay distinguishable from a measured nought, or a "
                + "turn gets charged against a cache figure nobody ever sent");
        }

        [Fact]
        public void TheOpenAiCompatibleDetailSpellingIsRead()
        {
            PromptCacheUsage.CachedInputTokens(Usage(("prompt_tokens_details.cached_tokens", 7000)))
                .Should().Be(7000);
        }
    }
}