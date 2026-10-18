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
    [Trait("Category", "AskJooby")]
    public sealed class RagSearchToolTests
    {
        [Fact]
        public async Task ResultStartsWithAMachineParseableCountLineAsync()
        {
            var rag = new FakeRagClient(
            [
                new RagChunk("h1", null, "Document", "Concept", "a.md", "A", "content a", 0.9),
                new RagChunk("h2", null, "Document", "Concept", "b.md", "B", "content b", 0.8)
            ]);

            var tool = new RagSearchTool(rag);

            var result = await tool.SearchRag("velocity rule");

            result.Should().StartWith("Found 2 Bites.");
            result.Should().Contain("content a");
            result.Should().Contain("content b");
        }

        [Fact]
        public async Task ReportsNoMatchingBitesWhenNothingComesBackAsync()
        {
            var tool = new RagSearchTool(new FakeRagClient([]));

            var result = await tool.SearchRag("something obscure");

            result.Should().Be("No matching Bites found.");
        }

        [Fact]
        public async Task PassesTheConfiguredTopKThroughToTheRagClientAsync()
        {
            var rag = new FakeRagClient([]);
            var tool = new RagSearchTool(rag, 12);

            await tool.SearchRag("velocity rule");

            rag.LastTopK.Should().Be(12);
        }

        [Fact]
        public async Task DefaultsToATopKOfFiveWhenNoneIsSuppliedAsync()
        {
            var rag = new FakeRagClient([]);
            var tool = new RagSearchTool(rag);

            await tool.SearchRag("velocity rule");

            rag.LastTopK.Should().Be(5);
        }

        [Fact]
        public async Task UsesTheModelSuppliedMaxBitesOverTheConfiguredTopKAsync()
        {
            var rag = new FakeRagClient([]);
            var tool = new RagSearchTool(rag, 3);

            await tool.SearchRag("velocity rule", 6);

            rag.LastTopK.Should().Be(6);
        }

        [Fact]
        public async Task ClampsAModelSuppliedMaxBitesAboveTheCeilingAsync()
        {
            var rag = new FakeRagClient([]);
            var tool = new RagSearchTool(rag, 3);

            await tool.SearchRag("velocity rule", 500);

            rag.LastTopK.Should().Be(8);
        }

        [Fact]
        public async Task ClampsAModelSuppliedMaxBitesBelowOneAsync()
        {
            var rag = new FakeRagClient([]);
            var tool = new RagSearchTool(rag, 3);

            await tool.SearchRag("velocity rule", 0);

            rag.LastTopK.Should().Be(1);
        }

        [Fact]
        public async Task FreshBitesStillComeBackInFullAlongsideTheNamedRepeatsAsync()
        {
            var ledger = new RetrievedBiteLedger(8);

            var first = new FakeRagClient(
            [
                new RagChunk("h1", null, "Document", "Concept", "a.md", "A", "content a", 0.9),
                new RagChunk("h2", null, "Document", "Concept", "b.md", "B", "content b", 0.8)
            ]);

            await new RagSearchTool(first, ledger: ledger).SearchRag("first question");

            var second = new FakeRagClient(
            [
                new RagChunk("h1", null, "Document", "Concept", "a.md", "A", "content a", 0.9),
                new RagChunk("h2", null, "Document", "Concept", "b.md", "B", "content b", 0.8),
                new RagChunk("h3", null, "Document", "Concept", "c.md", "C", "content c", 0.7),
                new RagChunk("h4", null, "Document", "Concept", "d.md", "D", "content d", 0.6)
            ]);

            var result = await new RagSearchTool(second, ledger: ledger).SearchRag("second question");

            result.Should().StartWith("Found 4 Bites, 2 of them new.");
            result.Should().Contain("content c", "a Bite the model has never seen must arrive in full");
            result.Should().Contain("content d");
            result.Should().NotContain("content a", "a Bite already in context is named, not resent");
            result.Should().Contain("Already given to you earlier this turn");
            result.Should().Contain("A; B");
        }

        [Fact]
        public async Task CandidatesAreForwardedToTheSearchToBeRerankedAsync()
        {
            var rag = new FakeRagClient(
                [new RagChunk("h1", null, "Document", "Concept", "a.md", "A", "content a", 0.9)]);

            var tool = new RagSearchTool(rag);

            await tool.SearchRag("velocity rule", candidates: 40);

            rag.LastCandidates.Should().Be(40);
        }

        [Fact]
        public async Task CandidatesAreClampedToTheCeilingWhateverTheModelAsksForAsync()
        {
            var rag = new FakeRagClient(
                [new RagChunk("h1", null, "Document", "Concept", "a.md", "A", "content a", 0.9)]);

            var tool = new RagSearchTool(rag);

            await tool.SearchRag("velocity rule", candidates: 500);

            rag.LastCandidates.Should().Be(50);
        }

        [Fact]
        public async Task UnsetCandidatesDefersToTheDeploymentDefaultAsync()
        {
            var rag = new FakeRagClient(
                [new RagChunk("h1", null, "Document", "Concept", "a.md", "A", "content a", 0.9)]);

            var tool = new RagSearchTool(rag);

            await tool.SearchRag("velocity rule");

            rag.LastCandidates.Should().Be(0, "zero tells the client to use its own configured candidate count");
        }

        [Fact]
        public async Task MaxBitesStillCapsWhatComesBackAtEightAsync()
        {
            var rag = new FakeRagClient(
                [new RagChunk("h1", null, "Document", "Concept", "a.md", "A", "content a", 0.9)]);

            var tool = new RagSearchTool(rag);

            await tool.SearchRag("velocity rule", 50);

            rag.LastTopK.Should().Be(8);
        }

        private sealed class FakeRagClient(IReadOnlyList<RagChunk> hits) : IRagClient
        {
            public int? LastTopK { get; private set; }
            public int? LastCandidates { get; private set; }

            public Task<IReadOnlyList<RagChunk>> SearchAsync(
                string query, int topK = 5, double minScore = 0, int candidates = 0, CancellationToken token = default)
            {
                LastTopK = topK;
                LastCandidates = candidates;

                return Task.FromResult(hits);
            }
        }
    }
}