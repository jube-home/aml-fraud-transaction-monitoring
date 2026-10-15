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
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Query.GetAskJoobyConversationRetrievedDocsQuery;
using Jube.Data.Repository.Models;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    [Collection("Database")]
    public sealed class GetAskJoobyConversationRetrievedDocsQueryTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly List<int> createdConversationIds = [];
        private readonly List<int> createdTenantIds = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            if (createdConversationIds.Count > 0)
            {
                await dbContext.AskJoobyToolCall
                    .Where(w => dbContext.AskJoobyRun
                        .Any(r => r.Id == w.AskJoobyRunId &&
                                  createdConversationIds.Contains(r.AskJoobyConversationId!.Value)))
                    .DeleteAsync();

                await dbContext.AskJoobyRun
                    .Where(w => createdConversationIds.Contains(w.AskJoobyConversationId!.Value)).DeleteAsync();

                await dbContext.AskJoobyConversation.Where(w => createdConversationIds.Contains(w.Id))
                    .DeleteAsync();
            }

            if (createdTenantIds.Count > 0)
            {
                await dbContext.TenantRegistry.Where(w => createdTenantIds.Contains(w.Id)).DeleteAsync();
            }
        }

        private async Task<int> SeedConversationAsync(DbContext dbContext)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];

            var tenantId = await dbContext.InsertWithInt32IdentityAsync(new TenantRegistry
            {
                Name = $"{DatabaseFixture.Prefix}RetrievedDocsTenant{suffix}", Active = 1, Locked = 0, Deleted = 0,
                Landlord = 0, Version = 1, CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
            });
            createdTenantIds.Add(tenantId);

            var conversationId = await dbContext.InsertWithInt32IdentityAsync(new AskJoobyConversation
            {
                TenantRegistryId = tenantId, Guid = Guid.NewGuid(), Title = null,
                CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix, Version = 1
            });
            createdConversationIds.Add(conversationId);

            return conversationId;
        }

        private static async Task SeedSearchRagCallAsync(DbContext dbContext, int conversationId,
            string result, DateTime createdDate)
        {
            var runId = await dbContext.InsertWithInt32IdentityAsync(new AskJoobyRun
            {
                AskJoobyConversationId = conversationId, AgentType = "general", Depth = 0,
                StatusId = (byte)AskJoobyRunStatus.Completed,
                CreatedDate = createdDate, CreatedUser = DatabaseFixture.Prefix
            });

            await dbContext.InsertWithInt32IdentityAsync(new AskJoobyToolCall
            {
                AskJoobyRunId = runId, ToolName = "SearchRag", Result = result,
                StatusId = (byte)AskJoobyToolCallStatus.Completed,
                CreatedDate = createdDate
            });
        }

        [Fact]
        public async Task DeduplicatesTheSameBiteTextRetrievedByMultipleSearchesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var conversationId = await SeedConversationAsync(dbContext);
            var now = DateTime.UtcNow;

            await SeedSearchRagCallAsync(dbContext, conversationId, "Found 1 Bite.\n\nsame content",
                now.AddMinutes(-2));
            await SeedSearchRagCallAsync(dbContext, conversationId, "Found 1 Bite.\n\nsame content",
                now.AddMinutes(-1));

            await SeedSearchRagCallAsync(dbContext, conversationId, "Found 1 Bite.\n\ndifferent content", now);

            var query = new GetAskJoobyConversationRetrievedDocsQuery(dbContext);
            var docs = await query.ExecuteAsync(conversationId);

            docs.Should().HaveCount(2, "the repeated identical search result must only be counted once");
        }

        [Fact]
        public async Task ExcludesNoMatchingBitesFoundResultsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var conversationId = await SeedConversationAsync(dbContext);

            await SeedSearchRagCallAsync(dbContext, conversationId, "No matching Bites found.", DateTime.UtcNow);

            var query = new GetAskJoobyConversationRetrievedDocsQuery(dbContext);
            var docs = await query.ExecuteAsync(conversationId);

            docs.Should().BeEmpty();
        }

        [Fact]
        public async Task NeverReturnsAnotherConversationsSearchResultsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var thisConversationId = await SeedConversationAsync(dbContext);
            var otherConversationId = await SeedConversationAsync(dbContext);

            await SeedSearchRagCallAsync(dbContext, thisConversationId, "Found 1 Bite.\n\nmine", DateTime.UtcNow);
            await SeedSearchRagCallAsync(dbContext, otherConversationId, "Found 1 Bite.\n\nnot mine",
                DateTime.UtcNow);

            var query = new GetAskJoobyConversationRetrievedDocsQuery(dbContext);
            var docs = await query.ExecuteAsync(thisConversationId);

            docs.Should().ContainSingle().Which.Should().Contain("mine");
            docs.Should().NotContain(d => d.Contains("not mine"));
        }

        [Fact]
        public async Task CapsTheResultCountEvenWhenFarMoreDistinctSearchesExistAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var conversationId = await SeedConversationAsync(dbContext);
            var now = DateTime.UtcNow;

            for (var i = 0; i < 20; i++)
            {
                await SeedSearchRagCallAsync(dbContext, conversationId, $"Found 1 Bite.\n\ndistinct content {i}",
                    now.AddSeconds(i));
            }

            var query = new GetAskJoobyConversationRetrievedDocsQuery(dbContext);
            var docs = await query.ExecuteAsync(conversationId);

            docs.Should().HaveCountLessThanOrEqualTo(12,
                "an unbounded accumulation across a long conversation is what caused a 1.3-million-token request");
        }

        [Fact]
        public async Task PrefersTheMostRecentDistinctSearchesWhenCappingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var conversationId = await SeedConversationAsync(dbContext);
            var now = DateTime.UtcNow;

            for (var i = 0; i < 20; i++)
            {
                await SeedSearchRagCallAsync(dbContext, conversationId, $"Found 1 Bite.\n\ndistinct content {i}",
                    now.AddSeconds(i));
            }

            var query = new GetAskJoobyConversationRetrievedDocsQuery(dbContext);
            var docs = await query.ExecuteAsync(conversationId);

            docs.Should().Contain(d => d.Contains("distinct content 19"), "the most recent search must survive");
            docs.Should().NotContain(d => d.Contains("distinct content 0"), "the oldest search should be dropped");
        }
    }
}