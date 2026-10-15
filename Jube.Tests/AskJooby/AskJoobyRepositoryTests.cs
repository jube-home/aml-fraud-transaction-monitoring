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
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Data.Repository.Models;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Data;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AskJoobyRepositoryTests(DatabaseFixture fx) : IAsyncLifetime
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
                var runIds = await dbContext.AskJoobyRun
                    .Where(w => createdConversationIds.Contains(w.AskJoobyConversationId!.Value))
                    .Select(s => s.Id).ToListAsync();

                await dbContext.AskJoobyModelCall.Where(w => runIds.Contains(w.AskJoobyRunId!.Value))
                    .DeleteAsync();
                await dbContext.AskJoobyToolCall.Where(w => runIds.Contains(w.AskJoobyRunId!.Value))
                    .DeleteAsync();
                await dbContext.AskJoobyRun.Where(w => createdConversationIds.Contains(w.AskJoobyConversationId!.Value))
                    .DeleteAsync();
                await dbContext.AskJoobyFile
                    .Where(w => createdConversationIds.Contains(w.AskJoobyConversationId!.Value)).DeleteAsync();
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
                Name = $"{DatabaseFixture.Prefix}AskJoobyTenant{Guid.NewGuid():N}",
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

        private async Task<int> SeedConversationAsync(DbContext dbContext, int tenantRegistryId,
            DateTime? createdDate = null)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new AskJoobyConversation
            {
                TenantRegistryId = tenantRegistryId,
                Guid = Guid.NewGuid(),
                Title = $"{DatabaseFixture.Prefix}Conversation",
                CreatedDate = createdDate ?? DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix,
                Version = 1
            });
            createdConversationIds.Add(id);

            return id;
        }

        [Fact]
        public async Task ListMessagesByConversationAsyncExcludesNarrationEntriesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var conversationId = await SeedConversationAsync(dbContext, tenantId);
            var messageRepository = new AskJoobyMessageRepository(dbContext);

            await messageRepository.InsertAsync(new AskJoobyMessage
            {
                AskJoobyConversationId = conversationId, RoleId = (byte)AskJoobyMessageRole.User,
                Content = "What are TTL counters?"
            });

            await messageRepository.InsertAsync(new AskJoobyMessage
            {
                AskJoobyConversationId = conversationId, Content = "Grazing the Pasture for \"TTL counters\"...",
                EntryTypeId = AskJoobyMessageRepository.EntryTypeStep
            });
            await messageRepository.InsertAsync(new AskJoobyMessage
            {
                AskJoobyConversationId = conversationId, Content = "Calling in a Joobrik (researcher)...",
                EntryTypeId = AskJoobyMessageRepository.EntryTypeDelegation
            });

            await messageRepository.InsertAsync(new AskJoobyMessage
            {
                AskJoobyConversationId = conversationId, RoleId = (byte)AskJoobyMessageRole.Assistant,
                Content = "TTL counters expire after N."
            });

            var modelContextMessages = await messageRepository.ListMessagesByConversationAsync(conversationId);
            var fullReplayEntries = await messageRepository.ListEntriesByConversationAsync(conversationId);

            modelContextMessages.Should().HaveCount(2,
                "only entryType='message' rows may ever be fed back into the model's own context");

            modelContextMessages.Select(s => s.Content).Should()
                .Contain(["What are TTL counters?", "TTL counters expire after N."]);

            modelContextMessages.Should().NotContain(m => m.EntryTypeId == AskJoobyMessageRepository.EntryTypeStep);
            modelContextMessages.Should()
                .NotContain(m => m.EntryTypeId == AskJoobyMessageRepository.EntryTypeDelegation);

            fullReplayEntries.Should().HaveCount(4, "replay must show exactly what streamed live, narration included");
            fullReplayEntries.Select(s => s.EntryTypeId).Should().Contain(AskJoobyMessageRepository.EntryTypeStep);
            fullReplayEntries.Select(s => s.EntryTypeId).Should()
                .Contain(AskJoobyMessageRepository.EntryTypeDelegation);
        }

        [Fact]
        public async Task InsertAsyncDefaultsEntryTypeToMessageWhenNotSpecifiedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var conversationId = await SeedConversationAsync(dbContext, tenantId);
            var messageRepository = new AskJoobyMessageRepository(dbContext);

            var inserted = await messageRepository.InsertAsync(new AskJoobyMessage
            {
                AskJoobyConversationId = conversationId, RoleId = (byte)AskJoobyMessageRole.User, Content = "Hello"
            });

            inserted.EntryTypeId.Should().Be(AskJoobyMessageRepository.EntryTypeMessage);
        }

        [Fact]
        public async Task ListEntriesByConversationAsyncExcludesCompactsButListMessagesIncludesThemAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var conversationId = await SeedConversationAsync(dbContext, tenantId);
            var messageRepository = new AskJoobyMessageRepository(dbContext);

            await messageRepository.InsertAsync(new AskJoobyMessage
            {
                AskJoobyConversationId = conversationId, RoleId = (byte)AskJoobyMessageRole.User,
                Content = "How do TTL counters work?"
            });

            await messageRepository.InsertWithEmbeddingAsync(new AskJoobyMessage
            {
                AskJoobyConversationId = conversationId, RoleId = (byte)AskJoobyMessageRole.System,
                Content = "Summary of the discussion so far.", IsCompact = true
            }, string.Empty);

            var modelContextMessages = await messageRepository.ListMessagesByConversationAsync(conversationId);
            var transcriptEntries = await messageRepository.ListEntriesByConversationAsync(conversationId);

            modelContextMessages.Should().Contain(m => m.IsCompact == true,
                "a compact must still flow back into the model -- that's the entire point of it");
            transcriptEntries.Should().NotContain(m => m.IsCompact == true,
                "a compact is internal housekeeping and must never appear in the live transcript replay");
        }

        [Fact]
        public async Task InsertWithEmbeddingAsyncPersistsTheEmbeddingAndCompactFlagAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var conversationId = await SeedConversationAsync(dbContext, tenantId);
            var messageRepository = new AskJoobyMessageRepository(dbContext);
            var vector = "[" + string.Join(",", Enumerable.Repeat("0.001", 1024)) + "]";

            var saved = await messageRepository.InsertWithEmbeddingAsync(new AskJoobyMessage
            {
                AskJoobyConversationId = conversationId, RoleId = (byte)AskJoobyMessageRole.System,
                Content = "A compact summary.",
                IsCompact = true
            }, vector);

            saved.IsCompact.Should().BeTrue();

            var hasEmbedding = await dbContext.QueryToListAsync<bool>(
                "SELECT \"Embedding\" IS NOT NULL FROM \"AskJoobyMessage\" WHERE \"Id\" = @id",
                CancellationToken.None, new DataParameter("id", saved.Id));

            hasEmbedding.Should().ContainSingle(v => v);
        }

        [Fact]
        public async Task InsertWithEmbeddingAsyncAcceptsAnEmptyEmbeddingWithoutThrowingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var conversationId = await SeedConversationAsync(dbContext, tenantId);
            var messageRepository = new AskJoobyMessageRepository(dbContext);

            var act = () => messageRepository.InsertWithEmbeddingAsync(new AskJoobyMessage
            {
                AskJoobyConversationId = conversationId, RoleId = (byte)AskJoobyMessageRole.System,
                Content = "No embedding available.",
                IsCompact = true
            }, string.Empty);

            await act.Should().NotThrowAsync(
                "an empty embedding must fall back to SQL NULL, not attempt an invalid vector cast");
        }

        [Fact]
        public async Task GetTotalsByRunIdsAsyncSumsTokensOnlyAcrossTheGivenRunTreeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var conversationId = await SeedConversationAsync(dbContext, tenantId);
            var runRepository = new AskJoobyRunRepository(dbContext);
            var modelCallRepository = new AskJoobyModelCallRepository(dbContext);

            var rootRun = await runRepository.InsertAsync(new AskJoobyRun
            {
                AskJoobyConversationId = conversationId, AgentType = "general", Depth = 0, Task = "root",
                CreatedUser = DatabaseFixture.Prefix
            });

            var childRun = await runRepository.InsertAsync(new AskJoobyRun
            {
                AskJoobyConversationId = conversationId, ParentAskJoobyRunId = rootRun.Id, AgentType = "researcher",
                Depth = 1, Task = "delegated", CreatedUser = DatabaseFixture.Prefix
            });

            var otherTurnRun = await runRepository.InsertAsync(new AskJoobyRun
            {
                AskJoobyConversationId = conversationId, AgentType = "general", Depth = 0, Task = "unrelated turn",
                CreatedUser = DatabaseFixture.Prefix
            });

            await modelCallRepository.InsertAsync(new AskJoobyModelCall
            {
                AskJoobyRunId = rootRun.Id, InputTokens = 100, OutputTokens = 40
            });

            await modelCallRepository.InsertAsync(new AskJoobyModelCall
            {
                AskJoobyRunId = childRun.Id, InputTokens = 30, OutputTokens = 10,
                CachedInputTokens = 25
            });

            await modelCallRepository.InsertAsync(new AskJoobyModelCall
            {
                AskJoobyRunId = otherTurnRun.Id, InputTokens = 999, OutputTokens = 999
            });

            var (inputTokens, outputTokens, cachedInputTokens) =
                await modelCallRepository.GetTotalsByRunIdsAsync([rootRun.Id, childRun.Id]);

            inputTokens.Should().Be(130, "it must include the root run's own calls plus its delegated child's");
            outputTokens.Should().Be(50);

            cachedInputTokens.Should().Be(25,
                "what the provider served from cache sums across the same tree, so a reply can be "
                + "shown what fraction of its input it did not actually pay full price for");
        }

        [Fact]
        public async Task GetTotalsByRunIdsAsyncReturnsZeroForAnEmptyRunIdCollectionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelCallRepository = new AskJoobyModelCallRepository(dbContext);

            var (inputTokens, outputTokens, cachedInputTokens) =
                await modelCallRepository.GetTotalsByRunIdsAsync([]);

            inputTokens.Should().Be(0);
            outputTokens.Should().Be(0);

            (cachedInputTokens is null).Should().BeTrue(
                "no calls means nothing reported a cache figure, which is not the same as every "
                + "call reporting a miss -- zero here would read as a measured nought per cent");
        }

        [Fact]
        public async Task ListByConversationAsyncReturnsAllRunsInTheRunTreeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var conversationId = await SeedConversationAsync(dbContext, tenantId);
            var runRepository = new AskJoobyRunRepository(dbContext);

            var rootRun = await runRepository.InsertAsync(new AskJoobyRun
            {
                AskJoobyConversationId = conversationId, AgentType = "general", Depth = 0, Task = "root",
                CreatedUser = DatabaseFixture.Prefix
            });

            var childRun = await runRepository.InsertAsync(new AskJoobyRun
            {
                AskJoobyConversationId = conversationId, ParentAskJoobyRunId = rootRun.Id, AgentType = "researcher",
                Depth = 1, Task = "delegated", CreatedUser = DatabaseFixture.Prefix
            });

            var runs = await runRepository.ListByConversationAsync(conversationId);

            runs.Select(s => s.Id).Should().Contain([rootRun.Id, childRun.Id]);
            runs.Single(s => s.Id == childRun.Id).ParentAskJoobyRunId.Should().Be(rootRun.Id);
        }

        [Fact]
        public async Task AttachToMessageAsyncLinksAnUploadedFileToTheMessageItWasSentWithAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var conversationId = await SeedConversationAsync(dbContext, tenantId);
            var messageRepository = new AskJoobyMessageRepository(dbContext);
            var fileRepository = new AskJoobyFileRepository(dbContext, tenantId, DatabaseFixture.Prefix);

            var message = await messageRepository.InsertAsync(new AskJoobyMessage
            {
                AskJoobyConversationId = conversationId, RoleId = (byte)AskJoobyMessageRole.User,
                Content = "Here is a file"
            });

            var file = await fileRepository.InsertAsync(new AskJoobyFile
            {
                AskJoobyConversationId = conversationId, Name = "notes.txt", ContentType = "text/plain",
                Extension = ".txt", Size = 12, Object = [.. "hello world!"u8]
            });

            await fileRepository.AttachToMessageAsync([file.Id], message.Id);

            var attached = await fileRepository.ListByMessageIdsAsync([message.Id]);
            attached.Should().ContainSingle(f => f.Id == file.Id && f.Name == "notes.txt");
        }

        [Fact]
        public async Task ListByMessageIdsAsyncReturnsAnEmptyListForAnEmptyMessageIdCollectionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var fileRepository = new AskJoobyFileRepository(dbContext, tenantId, DatabaseFixture.Prefix);

            var attached = await fileRepository.ListByMessageIdsAsync([]);

            attached.Should().BeEmpty();
        }

        [Fact]
        public async Task DeleteAsyncSoftDeletesAConversationSoItNoLongerLoadsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var conversationId = await SeedConversationAsync(dbContext, tenantId);

            var conversationRepository =
                new AskJoobyConversationRepository(dbContext, tenantId, DatabaseFixture.Prefix);

            await conversationRepository.DeleteAsync(conversationId);

            var reloaded = await conversationRepository.GetByIdAsync(conversationId);
            reloaded.Should().BeNull("a soft-deleted conversation must no longer be retrievable");
        }

        [Fact]
        public async Task DeleteAsyncThrowsKeyNotFoundForAConversationThatDoesNotExistAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);

            var conversationRepository =
                new AskJoobyConversationRepository(dbContext, tenantId, DatabaseFixture.Prefix);

            var act = () => conversationRepository.DeleteAsync(-1);

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task ListAsyncOrdersByMostRecentlyActiveRatherThanCreationOrderAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var now = DateTime.UtcNow;

            var olderConversationId =
                await SeedConversationAsync(dbContext, tenantId, createdDate: now.AddMinutes(-10));
            var newerConversationId =
                await SeedConversationAsync(dbContext, tenantId, createdDate: now.AddMinutes(-5));

            var conversationRepository =
                new AskJoobyConversationRepository(dbContext, tenantId, DatabaseFixture.Prefix);

            await conversationRepository.TouchAsync(olderConversationId);

            var conversations = await conversationRepository.ListAsync(string.Empty, 50, 0);

            conversations.Select(c => c.Id).Should().Equal(
                [olderConversationId, newerConversationId],
                "the touched conversation was created first but is now the most recently active, so it must sort first");
        }

        [Fact]
        public async Task ListAsyncFallsBackToCreatedDateWhenUpdatedDateIsNullAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var now = DateTime.UtcNow;

            var olderConversationId =
                await SeedConversationAsync(dbContext, tenantId, createdDate: now.AddMinutes(-10));
            var newerConversationId =
                await SeedConversationAsync(dbContext, tenantId, createdDate: now.AddMinutes(-5));

            var conversationRepository =
                new AskJoobyConversationRepository(dbContext, tenantId, DatabaseFixture.Prefix);

            var conversations = await conversationRepository.ListAsync(string.Empty, 50, 0);

            conversations.Select(c => c.Id).Should().Equal(
                [newerConversationId, olderConversationId],
                "with neither conversation ever touched, ordering must fall back to CreatedDate rather than " +
                "treating a null UpdatedDate as the dawn of time");
        }
    }
}