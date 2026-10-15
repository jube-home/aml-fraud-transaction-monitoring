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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.AskJooby.Rag;
using Jube.AskJooby.Rag.Models;
using Jube.AskJooby.Tools.Rag;
using Jube.AskJooby.Tools.Rag.Interfaces;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "Unit")]
    public sealed class RetrievedBiteLedgerTests
    {
        private const string Bite = "Document/Section/Abstraction/docs/abstraction.md";

        [Fact]
        public async Task ASecondAgentIsGivenTheBiteInFullRatherThanTheSuppressionNoticeAsync()
        {
            var turn = new RetrievedBiteLedger();
            var rag = new SingleBiteRagClient();

            await new RagSearchTool(rag, ledger: turn.ForAgent("run-greshgosh")).SearchRag("first question");

            var second = await new RagSearchTool(rag, ledger: turn.ForAgent("run-tamara"))
                .SearchRag("the same question, asked by someone else");

            second.Should().Contain(SingleBiteRagClient.Body,
                "an agent that has never been handed the Bite has nowhere to scroll up to");

            second.Should().NotContain("Already given to you earlier this turn");
        }

        [Fact]
        public async Task TheSameAgentAskingTwiceIsToldToScrollUpRatherThanSentItAgainAsync()
        {
            var turn = new RetrievedBiteLedger();
            var rag = new SingleBiteRagClient();
            var mine = turn.ForAgent("run-greshgosh");

            await new RagSearchTool(rag, ledger: mine).SearchRag("first question");
            var second = await new RagSearchTool(rag, ledger: mine).SearchRag("reworded question");

            second.Should().NotContain(SingleBiteRagClient.Body,
                "a tool result stays in context and is resent on every remaining model call of the turn");

            second.Should().Contain("Already given to you earlier this turn");
        }

        [Fact]
        public void TheSearchAllowanceIsSpentByTheTurnAndNotByEachAgentSeparately()
        {
            var turn = new RetrievedBiteLedger(2);

            turn.ForAgent("run-one").TryStartSearch().Should().BeTrue();
            turn.ForAgent("run-two").TryStartSearch().Should().BeTrue();
            turn.ForAgent("run-three").TryStartSearch().Should().BeFalse();

            turn.TryStartSearch().Should().BeFalse(
                "the turn-wide ledger and every view onto it count against one allowance");
        }

        [Fact]
        public void WhetherTheTurnRetrievedAnythingIsAnsweredAcrossEveryAgentInIt()
        {
            var turn = new RetrievedBiteLedger();

            turn.AnyRetrieved.Should().BeFalse();
            turn.ForAgent("run-greshgosh").Register(Bite).Should().BeTrue();

            turn.AnyRetrieved.Should().BeTrue();
            turn.ForAgent("run-tamara").AnyRetrieved.Should().BeTrue();
        }

        [Fact]
        public void AnAgentWithNoIdentityFallsBackToTheOneUnattributedBucket()
        {
            var turn = new RetrievedBiteLedger();

            turn.ForAgent(null).Register(Bite).Should().BeTrue();
            turn.ForAgent(" ").Register(Bite).Should().BeFalse();
            turn.Register(Bite).Should().BeFalse();
        }

        private sealed class SingleBiteRagClient : IRagClient
        {
            public const string Body = "Abstraction rules are the derived state the engine reasons over.";

            public Task<IReadOnlyList<RagChunk>> SearchAsync(
                string query, int topK = 5, double minScore = 0, int candidates = 0, CancellationToken token = default)
            {
                IReadOnlyList<RagChunk> hits =
                [
                    new("h1", null, "Document", "Section", "docs/abstraction.md", "Abstraction",
                        Body, 0.9)
                ];

                return Task.FromResult(hits);
            }
        }
    }
}