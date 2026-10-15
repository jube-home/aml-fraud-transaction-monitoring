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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.AskJooby.Rag;
using Jube.AskJooby.Rag.Models;
using Jube.AskJooby.Tools.Rag;
using Jube.AskJooby.Tools.Rag.Interfaces;
using Jube.AskJooby.Validation;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "Unit")]
    public sealed class TokenReductionTests
    {
        [Fact]
        public async Task ABiteAlreadyGivenThisTurnIsNamedRatherThanSentAgainAsync()
        {
            var ledger = new RetrievedBiteLedger();
            var rag = new RepeatingRagClient();
            var tool = new RagSearchTool(rag, 3, 0, ledger);

            var first = await tool.SearchRag("what is an abstraction rule");
            var second = await tool.SearchRag("how do abstraction rules work");

            first.Should().Contain("Abstraction rules are evaluated");

            second.Should().NotContain("Abstraction rules are evaluated",
                "the same document resent is paid for again on every remaining model call of the turn");

            second.Should().Contain("Already given to you earlier this turn");

            second.Length.Should().BeLessThan(first.Length,
                "naming a repeat must cost less than reprinting it, or the deduplication buys nothing");
        }

        [Fact]
        public async Task AGenuinelyNewBiteStillComesBackInFullAsync()
        {
            var ledger = new RetrievedBiteLedger();
            var rag = new RepeatingRagClient();
            var tool = new RagSearchTool(rag, 3, 0, ledger);

            await tool.SearchRag("first");
            rag.Swap("Gateway rules run before abstraction.");
            var second = await tool.SearchRag("second");

            second.Should().Contain("Gateway rules run before abstraction.",
                "deduplication must never withhold something the model has not been given");
        }

        [Fact]
        public async Task SearchingStopsOnceTheTurnsAllowanceIsSpentAsync()
        {
            var ledger = new RetrievedBiteLedger(2);
            var tool = new RagSearchTool(new RepeatingRagClient(), 3, 0, ledger);

            await tool.SearchRag("one");
            await tool.SearchRag("two");
            var third = await tool.SearchRag("three");

            third.Should().Contain("allowance",
                "a loop re-querying indefinitely needs a mechanical stop, not a wording in a prompt");
        }

        [Fact]
        public void ATurnThatRetrievedNothingIsDistinguishableFromOneThatDid()
        {
            var ledger = new RetrievedBiteLedger();

            ledger.AnyRetrieved.Should().BeFalse();
            ledger.Register("Document/Section/Thing (docs/index.md)");

            ledger.AnyRetrieved.Should().BeTrue(
                "a reply written with nothing retrieved all turn rests on the model's own recollection, "
                + "which is the case most worth checking");
        }

        [Fact]
        public void ValidatorGroundingKeepsOneCopyOfEachBite()
        {
            var one = "Found 2 Bites.\n\n[CRITICAL CONTEXT START]\n"
                      + "[Document/Section] Welcome (docs/index.md)\nJube is monitoring software.\n\n"
                      + "[Document/Section] Architecture (docs/arch.md)\nJube is stateless.\n\n"
                      + "[CRITICAL CONTEXT END]";

            var two = "Found 1 Bite.\n\n[CRITICAL CONTEXT START]\n"
                      + "[Document/Section] Welcome (docs/index.md)\nJube is monitoring software.\n\n"
                      + "[CRITICAL CONTEXT END]";

            var compacted = ValidationDocs.Compact([one, two]);

            CountOf(compacted, "Jube is monitoring software.").Should().Be(1,
                "three differently-worded searches routinely return the same top documents, and joining "
                + "the stored results verbatim sent each of them again");

            compacted.Should().Contain("Jube is stateless.");
        }

        [Fact]
        public void ValidatorGroundingNamesWhatItCouldNotAffordToQuote()
        {
            var big = "Found 2 Bites.\n\n[CRITICAL CONTEXT START]\n"
                      + "[Document/Section] First (docs/a.md)\n" + new string('a', 400) + "\n\n"
                      + "[Document/Section] Second (docs/b.md)\n" + new string('b', 400) + "\n\n"
                      + "[CRITICAL CONTEXT END]";

            var compacted = ValidationDocs.Compact([big], 300);

            compacted.Should().Contain("[Document/Section] First (docs/a.md)");

            compacted.Should().NotContain(new string('b', 400),
                "past the budget a Bite is named rather than quoted, the validator having its own "
                + "SearchRag to go and read it if it matters");

            compacted.Should().Contain("Also retrieved in this conversation")
                .And.Contain("[Document/Section] Second (docs/b.md)");
        }

        [Fact]
        public void ValidatorGroundingKeepsAnUnparseablePayloadRatherThanDroppingIt()
        {
            var compacted = ValidationDocs.Compact(["something with no Bite header at all"]);

            compacted.Should().Contain("something with no Bite header at all",
                "an unparseable result is still grounding, and losing it silently would weaken the check");
        }

        private static int CountOf(string haystack, string needle)
        {
            var count = 0;
            var at = haystack.IndexOf(needle, StringComparison.Ordinal);

            while (at >= 0)
            {
                count++;
                at = haystack.IndexOf(needle, at + needle.Length, StringComparison.Ordinal);
            }

            return count;
        }

        private sealed class RepeatingRagClient : IRagClient
        {
            private string content = "Abstraction rules are evaluated for every transaction and are the "
                                     + "derived state the engine reasons over. "
                                     + new string('x', 1200);

            private string name = "Abstraction (docs/abstraction.md)";

            public Task<IReadOnlyList<RagChunk>> SearchAsync(string query, int topK = 5,
                double minScore = 0, int candidates = 0, CancellationToken token = default)
            {
                IReadOnlyList<RagChunk> hits =
                [
                    new(name, null, "Document", "Section", "docs/x.md", name, content, 0.9)
                ];

                return Task.FromResult(hits);
            }

            public void Swap(string replacement)
            {
                content = replacement;
                name = "Gateway (docs/gateway.md)";
            }
        }
    }
}