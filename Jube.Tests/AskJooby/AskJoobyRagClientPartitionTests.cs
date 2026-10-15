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
using FluentAssertions;
using Jube.AskJooby.Rag.Models;
using Jube.Service.AskJoobyRagChunk;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class AskJoobyRagClientPartitionTests
    {
        private static RagChunk Chunk(string hash, int contentLength)
        {
            return new RagChunk(hash, null, "Document", "Prose", "docs/example.md", hash,
                new string('x', contentLength), 1.0);
        }

        [Fact]
        public void ABiteOverTheLimitIsDroppedAndNeverReachesTheKeptList()
        {
            var candidates = new List<RagChunk> { Chunk("small", 100), Chunk("huge", 9001) };

            var (kept, oversized) = AskJoobyRagClient.PartitionByMaxCharacters(candidates, 9000);

            kept.Should().ContainSingle(c => c.Hash == "small");
            oversized.Should().ContainSingle(c => c.Hash == "huge");
        }

        [Fact]
        public void ABiteExactlyAtTheLimitIsKept()
        {
            var candidates = new List<RagChunk> { Chunk("exact", 9000) };

            var (kept, oversized) = AskJoobyRagClient.PartitionByMaxCharacters(candidates, 9000);

            kept.Should().ContainSingle(c => c.Hash == "exact");
            oversized.Should().BeEmpty();
        }

        [Fact]
        public void AZeroOrNegativeLimitDisablesFilteringEntirely()
        {
            var candidates = new List<RagChunk> { Chunk("huge", 50_000) };

            var (kept, oversized) = AskJoobyRagClient.PartitionByMaxCharacters(candidates, 0);

            kept.Should().BeEquivalentTo(candidates);
            oversized.Should().BeEmpty();
        }

        [Fact]
        public void WhenEveryCandidateIsOversizedTheKeptListIsEmptyRatherThanFallingBackToAllOfThem()
        {
            var candidates = new List<RagChunk> { Chunk("huge1", 9001), Chunk("huge2", 10_000) };

            var (kept, oversized) = AskJoobyRagClient.PartitionByMaxCharacters(candidates, 9000);

            kept.Should().BeEmpty();
            oversized.Should().HaveCount(2);
        }
    }
}