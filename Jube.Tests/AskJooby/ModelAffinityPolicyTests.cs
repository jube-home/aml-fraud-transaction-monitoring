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

using System;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.AskJooby.Observability;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class ModelAffinityPolicyTests
    {
        private static string? StampedHeader()
        {
            var pipeline = ClientPipeline.Create(new ClientPipelineOptions(),
                [new ModelAffinityPolicy()], [], []);

            using var message = pipeline.CreateMessage();
            message.Request.Method = "POST";
            message.Request.Uri = new Uri("https://example.invalid/v1/chat/completions");

            var policy = new ModelAffinityPolicy();
            policy.Process(message, [policy, new TerminalPolicy()], 0);

            return message.Request.Headers.TryGetValue(ModelAffinityPolicy.HeaderName, out var value)
                ? value
                : null;
        }

        [Fact]
        public void TheHeaderCarriesTheConversationWheneverOneIsInScope()
        {
            using var scope = ModelAffinityScope.Begin("conversation-abc");

            StampedHeader().Should().Be("conversation-abc",
                "the whole prompt cache depends on this reaching the wire, and it is set from "
                + "ambient state rather than passed in, so it is worth proving rather than assuming");
        }

        [Fact]
        public void NoConversationInScopeLeavesTheRequestToRouteNormally()
        {
            (StampedHeader() is null).Should().BeTrue(
                "a call made outside a turn should not be pinned to whichever worker last served "
                + "some unrelated conversation");
        }

        [Fact]
        public async Task TheConversationReachesWorkDoneOnAnotherThreadAsync()
        {
            using var scope = ModelAffinityScope.Begin("conversation-fanout");

            var stamped = await Task.Run(StampedHeader);

            stamped.Should().Be("conversation-fanout",
                "a fan out runs each Joobrik on its own thread and every one of them has to carry "
                + "the same affinity, or the cache that the turn is warming is split across workers");
        }

        [Fact]
        public async Task EveryBranchOfAFanOutCarriesTheSameValueAsync()
        {
            using var scope = ModelAffinityScope.Begin("conversation-parallel");

            var stamped = await Task.WhenAll(
                Task.Run(StampedHeader), Task.Run(StampedHeader), Task.Run(StampedHeader));

            stamped.Should().AllBe("conversation-parallel");
        }

        [Fact]
        public void LeavingAConversationRestoresWhateverWasThereBefore()
        {
            using (ModelAffinityScope.Begin("outer"))
            {
                using (ModelAffinityScope.Begin("inner"))
                {
                    StampedHeader().Should().Be("inner");
                }

                StampedHeader().Should().Be("outer",
                    "a nested turn must not leave the outer one pinned to its value");
            }
        }

        private sealed class TerminalPolicy : PipelinePolicy
        {
            public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline,
                int currentIndex)
            {
            }

            public override ValueTask ProcessAsync(PipelineMessage message,
                IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
            {
                return default;
            }
        }
    }
}