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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.AskJooby.Rag.Models;
using Jube.AskJooby.Tools.Rag.Interfaces;
using Jube.Data.Poco;
using Jube.Data.Query.GetAskJoobyRagChunkByQualifiedNameQuery;
using Jube.Data.Repository.Models;
using Jube.Service.AskJoobyConversation;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class AskJoobyChatAuditCodeGroundingTests
    {
        [Fact]
        public async Task BuildAgentCodeAsyncKeepsOnlyCodeCategoryHitsAsync()
        {
            var ragClient = new FakeRagClient([
                new RagChunk("hash-1", null, "Code", "Method", "Jube.AskJooby/Agents/Basic/AgentDefinitions.cs",
                    "Jube.AskJooby.Agents.Basic.AgentDefinitions", "the code", 1),
                new RagChunk("hash-2", null, "Document", "File", "docs/AskJooby/CaseAnalyst.md",
                    "CaseAnalyst.md", "the doc", 1)
            ]);

            var result = await AskJoobyChatAuditCodeGrounding.BuildAgentCodeAsync(
                ragClient, ["case-analyst"], 16000, CancellationToken.None);

            result.Should().Contain("Agent Implementation Code");
            result.Should().Contain("the code");
            result.Should().NotContain("the doc");
        }

        [Fact]
        public async Task BuildAgentCodeAsyncDedupesTheSameChunkAcrossAgentTypesAsync()
        {
            var ragClient = new FakeRagClient([
                new RagChunk("shared-hash", null, "Code", "Method", "Jube.AskJooby/Routing/AgentRouter.cs",
                    "Jube.AskJooby.Routing.AgentRouter.RouteAsync", "shared routing code", 1)
            ]);

            var result = await AskJoobyChatAuditCodeGrounding.BuildAgentCodeAsync(
                ragClient, ["case-analyst", "validator"], 16000, CancellationToken.None);

            result!.Split("shared routing code").Length.Should().Be(2,
                "the same Bite surfacing for two different agents should only be written once");
        }

        [Fact]
        public async Task BuildAgentCodeAsyncReturnsNullWhenNothingIsCodeAsync()
        {
            var ragClient = new FakeRagClient([
                new RagChunk("hash-1", null, "Document", "File", "docs/AskJooby/CaseAnalyst.md",
                    "CaseAnalyst.md", "the doc", 1)
            ]);

            var result = await AskJoobyChatAuditCodeGrounding.BuildAgentCodeAsync(
                ragClient, ["case-analyst"], 16000, CancellationToken.None);

            result.Should().BeNull();
        }

        [Fact]
        public async Task BuildAgentCodeAsyncStopsOnceTheCharacterBudgetIsReachedAsync()
        {
            var ragClient = new FakeRagClient([
                new RagChunk("hash-1", null, "Code", "Method", "a.cs", "a", new string('a', 50), 1),
                new RagChunk("hash-2", null, "Code", "Method", "b.cs", "b", new string('b', 50), 1)
            ]);

            var result = await AskJoobyChatAuditCodeGrounding.BuildAgentCodeAsync(
                ragClient, ["case-analyst"], 60, CancellationToken.None);

            result.Should().Contain(new string('a', 50));
            result.Should().NotContain(new string('b', 50));
        }

        [Trait("Category", "AskJooby")]
        [Collection("Database")]
        public sealed class WithRealChunks(DatabaseFixture fx) : IAsyncLifetime
        {
            private readonly List<string> insertedHashes = [];

            public Task InitializeAsync()
            {
                return Task.CompletedTask;
            }

            public async Task DisposeAsync()
            {
                if (insertedHashes.Count == 0)
                {
                    return;
                }

                await using var dbContext = fx.GetDbContext();

                await dbContext.AskJoobyRagChunk.Where(w => insertedHashes.Contains(w.Hash)).DeleteAsync();
            }

            [Fact]
            public async Task BuildOrchestrationCodeAsyncRetrievesTheFixedOrchestrationChunksAsync()
            {
                await using var dbContext = fx.GetDbContext();

                var hash = $"test-{Guid.NewGuid():N}";
                insertedHashes.Add(hash);

                await dbContext.InsertAsync(new AskJoobyRagChunk
                {
                    Hash = hash,
                    CategoryId = (byte)AskJoobyRagChunkCategory.Code,
                    KindId = (byte)AskJoobyRagChunkKind.Type,
                    SourceFile = "Jube.AskJooby/Routing/AgentRouter.cs",
                    QualifiedName = "Jube.AskJooby.Routing.AgentRouter",
                    Content = "# Jube.AskJooby.Routing.AgentRouter\n\nthe real orchestration source",
                    StartLine = 1,
                    EndLine = 10
                });

                var result = await AskJoobyChatAuditCodeGrounding.BuildOrchestrationCodeAsync(
                    new GetAskJoobyRagChunkByQualifiedNameQuery(dbContext), 16000, CancellationToken.None);

                result.Should().Contain("Core Orchestration Code");
                result.Should().Contain("the real orchestration source");
            }
        }

        private sealed class FakeRagClient(IReadOnlyList<RagChunk> hits) : IRagClient
        {
            public Task<IReadOnlyList<RagChunk>> SearchAsync(
                string query, int topK = 5, double minScore = 0, int candidates = 0,
                CancellationToken token = default)
            {
                return Task.FromResult(hits);
            }
        }
    }
}