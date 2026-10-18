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
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.AskJooby.Security;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Data.Repository.Models;
using Jube.Service.AskJoobyConversation;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    [Collection("Database")]
    public sealed class AskJoobyConversationCompactionTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdConversationIds = [];
        private readonly List<int> createdRoleIds = [];
        private readonly List<int> createdTenantIds = [];
        private readonly List<string> createdUsers = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            if (createdConversationIds.Count > 0)
            {
                await dbContext.AskJoobyCompaction
                    .Where(w => createdConversationIds.Contains(w.AskJoobyConversationId!.Value)).DeleteAsync();

                var runIds = await dbContext.AskJoobyRun
                    .Where(w => createdConversationIds.Contains(w.AskJoobyConversationId!.Value))
                    .Select(w => w.Id).ToListAsync();

                await dbContext.AskJoobyToolCall.Where(w => runIds.Contains(w.AskJoobyRunId!.Value)).DeleteAsync();
                await dbContext.AskJoobyModelCall.Where(w => runIds.Contains(w.AskJoobyRunId!.Value)).DeleteAsync();

                await dbContext.AskJoobyRun
                    .Where(w => createdConversationIds.Contains(w.AskJoobyConversationId!.Value)).DeleteAsync();
                await dbContext.AskJoobyMessage
                    .Where(w => createdConversationIds.Contains(w.AskJoobyConversationId!.Value)).DeleteAsync();

                await dbContext.AskJoobyConversation.Where(w => createdConversationIds.Contains(w.Id))
                    .DeleteAsync();
            }

            if (createdUsers.Count > 0)
            {
                await dbContext.UserInTenant.Where(w => createdUsers.Contains(w.User)).DeleteAsync();
                await dbContext.UserRegistry.Where(w => createdUsers.Contains(w.Name)).DeleteAsync();
            }

            if (createdRoleIds.Count > 0)
            {
                await dbContext.RoleRegistryPermission.Where(w => createdRoleIds.Contains(w.RoleRegistryId!.Value))
                    .DeleteAsync();

                await dbContext.RoleRegistry.Where(w => createdRoleIds.Contains(w.Id)).DeleteAsync();
            }

            if (createdTenantIds.Count > 0)
            {
                await dbContext.TenantRegistry.Where(w => createdTenantIds.Contains(w.Id)).DeleteAsync();
            }
        }

        [Fact]
        public async Task SendingAMessageOnceTheTurnCountThresholdIsReachedActuallyCompactsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) = await SeedPermittedUserAsync(dbContext);
            var conversationId = await SeedConversationAsync(dbContext, tenantId, userName);

            var messageRepository = new AskJoobyMessageRepository(dbContext);

            for (var i = 0; i < 60; i++)
            {
                await messageRepository.InsertAsync(new AskJoobyMessage
                {
                    AskJoobyConversationId = conversationId,
                    RoleId = i % 2 == 0 ? (byte)AskJoobyMessageRole.User : (byte)AskJoobyMessageRole.Assistant,
                    Content = $"Pre-existing turn number {i} talking about an unrelated account review."
                }, CancellationToken.None);
            }

            var service = await AskJoobyConversationService.CreateAsync(dbContext, userName, TestLog.NoOp,
                localizers, new NullServiceChangeBus(), new ReplyingFakeChatClient(),
                new FixedSizeFakeEmbeddingGenerator(), connectionString: fx.ConnectionString);

            await foreach (var _ in service.SendMessageAsync(
                               conversationId, "please look up what the Pasture says about this",
                               null, null, CancellationToken.None))
            {
            }

            var compaction = await WaitForCompactionAsync(conversationId);

            compaction.Should().NotBeNull(
                "crossing the 60-message threshold on an ordinary send must actually trigger a background " +
                "compaction, not just leave the trigger code unexercised");

            compaction!.MessageCount.Should().BeGreaterThanOrEqualTo(60);
            compaction.AskJoobyMessageId.Should().NotBeNull();

            await using var verifyContext = fx.GetDbContext();

            var compactMessage = await verifyContext.AskJoobyMessage
                .Where(w => w.Id == compaction.AskJoobyMessageId!.Value).SingleAsync();

            compactMessage.IsCompact.Should().BeTrue();
            compactMessage.CompactedToAskJoobyMessageId.Should().Be(compaction.CompactedToAskJoobyMessageId);
            compactMessage.Content.Should().NotBeNullOrWhiteSpace();
        }

        private async Task<AskJoobyCompaction?> WaitForCompactionAsync(int conversationId)
        {
            var deadline = DateTime.UtcNow.AddSeconds(20);

            while (DateTime.UtcNow < deadline)
            {
                await using var pollContext = fx.GetDbContext();

                var found = await pollContext.AskJoobyCompaction
                    .Where(w => w.AskJoobyConversationId == conversationId).FirstOrDefaultAsync();

                if (found is not null)
                {
                    return found;
                }

                await Task.Delay(200);
            }

            return null;
        }

        private async Task<(int TenantId, string UserName)> SeedPermittedUserAsync(
            DbContext dbContext)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];

            var tenantId = await dbContext.InsertWithInt32IdentityAsync(new TenantRegistry
            {
                Name = $"{DatabaseFixture.Prefix}AskJoobyCompactTenant{suffix}", Active = 1, Locked = 0,
                Deleted = 0, Landlord = 0, Version = 1, CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            createdTenantIds.Add(tenantId);

            var roleGuid = Guid.NewGuid();

            var roleId = await dbContext.InsertWithInt32IdentityAsync(new RoleRegistry
            {
                Guid = roleGuid, Name = $"{DatabaseFixture.Prefix}AskJoobyCompactRole{suffix}", Active = 1,
                Locked = 0, Deleted = 0, TenantRegistryId = tenantId, Version = 1, CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            createdRoleIds.Add(roleId);

            await dbContext.InsertAsync(new RoleRegistryPermission
            {
                Guid = Guid.NewGuid(), PermissionSpecificationId = AskJoobyPermissions.Access, RoleRegistryId = roleId,
                Active = 1, Locked = 0, Deleted = 0, Version = 1, CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });

            var userName = $"{DatabaseFixture.Prefix}AskJoobyCompactUser{suffix}";
            createdUsers.Add(userName);

            await dbContext.InsertAsync(new UserRegistry
            {
                Guid = Guid.NewGuid(), RoleRegistryGuid = roleGuid, Name = userName,
                Email = $"{userName}@example.invalid", Password = "not-used-by-permission-checks", Active = 1,
                PasswordLocked = 0, Deleted = 0, Version = 1, CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });

            await dbContext.InsertAsync(new UserInTenant { User = userName, TenantRegistryId = tenantId });

            return (tenantId, userName);
        }

        private async Task<int> SeedConversationAsync(
            DbContext dbContext, int tenantRegistryId, string createdUser)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new AskJoobyConversation
            {
                TenantRegistryId = tenantRegistryId, Guid = Guid.NewGuid(), Title = null,
                CreatedDate = DateTime.UtcNow, CreatedUser = createdUser, Version = 1
            });
            createdConversationIds.Add(id);

            return id;
        }

        private sealed class ReplyingFakeChatClient : IChatClient
        {
            public Task<ChatResponse> GetResponseAsync(
                IEnumerable<ChatMessage> messages, ChatOptions? options = null,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant,
                    "Summary: the account review turns covered routine, unrelated checks.")));
            }

            public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
                IEnumerable<ChatMessage> messages, ChatOptions? options = null,
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                await Task.Delay(10, cancellationToken);

                yield return new ChatResponseUpdate(ChatRole.Assistant, "Here is what I found.");
            }

            public object? GetService(Type serviceType, object? serviceKey = null)
            {
                return null;
            }

            public void Dispose()
            {
            }
        }

        private sealed class FixedSizeFakeEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
        {
            public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
                IEnumerable<string> values, EmbeddingGenerationOptions? options = null,
                CancellationToken cancellationToken = default)
            {
                var vector = new float[1024];
                Array.Fill(vector, 0.001f);

                var generated = values.Select(_ => new Embedding<float>(vector));

                return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(generated));
            }

            public object? GetService(Type serviceType, object? serviceKey = null)
            {
                return null;
            }

            public void Dispose()
            {
            }
        }
    }
}