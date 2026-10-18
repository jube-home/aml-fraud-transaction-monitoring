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
using Jube.Data.Query.SearchAskJoobyConversationHistoryQuery;
using Jube.Data.Repository;
using Jube.Data.Repository.Models;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    [Collection("Database")]
    public sealed class AskJoobyConversationHistoryQueryTests(DatabaseFixture fx) : IAsyncLifetime
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
                await dbContext.AskJoobyMessage
                    .Where(w => createdConversationIds.Contains(w.AskJoobyConversationId!.Value)).DeleteAsync();

                await dbContext.AskJoobyConversation.Where(w => createdConversationIds.Contains(w.Id))
                    .DeleteAsync();
            }

            if (createdTenantIds.Count > 0)
            {
                await dbContext.TenantRegistry.Where(w => createdTenantIds.Contains(w.Id)).DeleteAsync();
            }
        }

        private async Task<int> SeedTenantAsync(DbContext dbContext)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new TenantRegistry
            {
                Name = $"{DatabaseFixture.Prefix}AskJoobyHistoryTenant{Guid.NewGuid():N}",
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Landlord = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            createdTenantIds.Add(id);

            return id;
        }

        private async Task<int> SeedConversationAsync(
            DbContext dbContext, int tenantRegistryId, string title, string createdUser)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new AskJoobyConversation
            {
                TenantRegistryId = tenantRegistryId,
                Guid = Guid.NewGuid(),
                Title = title,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = createdUser,
                Version = 1
            });
            createdConversationIds.Add(id);

            return id;
        }

        [Fact]
        public async Task SearchFindsAConversationByTitleEvenWhenTheBodyNeverRepeatsTheWordsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var messageRepository = new AskJoobyMessageRepository(dbContext);
            var query = new SearchAskJoobyConversationHistoryQuery(dbContext);

            var conversationId = await SeedConversationAsync(dbContext, tenantId, "TTL Counter Options", "alice");

            await messageRepository.InsertAsync(new AskJoobyMessage
            {
                AskJoobyConversationId = conversationId, RoleId = (byte)AskJoobyMessageRole.User,
                Content = "Here's a totally generic follow-up question about that thing we discussed."
            });

            var results = await query.ExecuteAsync("ttl counter", string.Empty, "System", tenantId, "alice", 10);

            results.Should().Contain(r => r.ConversationId == conversationId,
                "a title match should surface the conversation even without body overlap");
        }

        [Fact]
        public async Task UserScopeNeverSurfacesAnotherUsersConversationAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var messageRepository = new AskJoobyMessageRepository(dbContext);
            var query = new SearchAskJoobyConversationHistoryQuery(dbContext);

            var bobConversationId = await SeedConversationAsync(dbContext, tenantId, "Bob's TTL chat", "bob");

            await messageRepository.InsertAsync(new AskJoobyMessage
            {
                AskJoobyConversationId = bobConversationId, RoleId = (byte)AskJoobyMessageRole.User,
                Content = "How do TTL counters work?"
            });

            var asAliceUserScope =
                await query.ExecuteAsync("ttl counters", string.Empty, "User", tenantId, "alice", 10);
            var asAliceSystemScope =
                await query.ExecuteAsync("ttl counters", string.Empty, "System", tenantId, "alice", 10);

            asAliceUserScope.Should().NotContain(r => r.ConversationId == bobConversationId,
                "User scope must never surface a conversation someone else created");
            asAliceSystemScope.Should().Contain(r => r.ConversationId == bobConversationId,
                "System scope is deliberately shared tenant-wide");
        }

        [Fact]
        public async Task SearchReportsIsCompactForACompactionSummaryRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var messageRepository = new AskJoobyMessageRepository(dbContext);
            var query = new SearchAskJoobyConversationHistoryQuery(dbContext);

            var conversationId = await SeedConversationAsync(dbContext, tenantId, "Long chat", "carol");

            await messageRepository.InsertWithEmbeddingAsync(new AskJoobyMessage
            {
                AskJoobyConversationId = conversationId, RoleId = (byte)AskJoobyMessageRole.System,
                Content = "Summary: discussed unique keyword zzyzx piecrust workaround.", IsCompact = true
            }, string.Empty);

            var results = await query.ExecuteAsync(
                "zzyzx piecrust workaround", string.Empty, "System", tenantId, "carol", 10);

            results.Should().ContainSingle(r => r.ConversationId == conversationId && r.IsCompact);
        }

        [Fact]
        public async Task SearchExcludesNarrationEntriesLikeStepsAndThoughtsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var messageRepository = new AskJoobyMessageRepository(dbContext);
            var query = new SearchAskJoobyConversationHistoryQuery(dbContext);

            var conversationId = await SeedConversationAsync(dbContext, tenantId, "Chat with steps", "dave");

            await messageRepository.InsertAsync(new AskJoobyMessage
            {
                AskJoobyConversationId = conversationId,
                Content = "Grazing the Pasture for \"unique zzyzx marker\"...",
                EntryTypeId = AskJoobyMessageRepository.EntryTypeStep
            });

            var results = await query.ExecuteAsync("zzyzx marker", string.Empty, "System", tenantId, "dave", 10);

            results.Should().BeEmpty("Step/Thought/Delegation narration is transcript-only, not history");
        }
    }
}