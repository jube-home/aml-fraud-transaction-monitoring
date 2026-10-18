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
using Jube.Data.Query.GetAskJoobyStartupStatsQuery;
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
    public sealed class AskJoobyConversationStartTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static readonly string connectionString =
            Environment.GetEnvironmentVariable("JubeTestConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionString")
            ?? throw new InvalidOperationException(
                "Set JubeTestConnectionString (or ConnectionString) to run database-backed tests.");

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
                var runIds = await dbContext.AskJoobyRun
                    .Where(w => createdConversationIds.Contains(w.AskJoobyConversationId!.Value))
                    .Select(w => w.Id).ToListAsync();

                await dbContext.AskJoobyModelCall.Where(w => runIds.Contains(w.AskJoobyRunId!.Value)).DeleteAsync();
                await dbContext.AskJoobyToolCall.Where(w => runIds.Contains(w.AskJoobyRunId!.Value)).DeleteAsync();
                await dbContext.AskJoobyRun
                    .Where(w => createdConversationIds.Contains(w.AskJoobyConversationId!.Value)).DeleteAsync();
                await dbContext.AskJoobyMessage
                    .Where(w => createdConversationIds.Contains(w.AskJoobyConversationId!.Value)).DeleteAsync();
                await dbContext.AskJoobyDataCache
                    .Where(w => createdConversationIds.Contains(w.AskJoobyConversationId!.Value)).DeleteAsync();
                await dbContext.DynamicSqlQueryLog
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
                await dbContext.AskJoobyMemory.Where(w => createdTenantIds.Contains(w.TenantRegistryId!.Value))
                    .DeleteAsync();

                await dbContext.TenantRegistry.Where(w => createdTenantIds.Contains(w.Id)).DeleteAsync();
            }
        }

        private async Task<(int TenantId, string UserName)> SeedPermittedUserAsync(
            DbContext dbContext)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];

            var tenantId = await dbContext.InsertWithInt32IdentityAsync(new TenantRegistry
            {
                Name = $"{DatabaseFixture.Prefix}AskJoobyStartTenant{suffix}", Active = 1, Locked = 0, Deleted = 0,
                Landlord = 0, Version = 1, CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
            });
            createdTenantIds.Add(tenantId);

            var roleGuid = Guid.NewGuid();

            var roleId = await dbContext.InsertWithInt32IdentityAsync(new RoleRegistry
            {
                Guid = roleGuid, Name = $"{DatabaseFixture.Prefix}AskJoobyStartRole{suffix}", Active = 1, Locked = 0,
                Deleted = 0, TenantRegistryId = tenantId, Version = 1, CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            createdRoleIds.Add(roleId);

            await dbContext.InsertAsync(new RoleRegistryPermission
            {
                Guid = Guid.NewGuid(), PermissionSpecificationId = AskJoobyPermissions.Access, RoleRegistryId = roleId,
                Active = 1, Locked = 0, Deleted = 0, Version = 1, CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });

            var userName = $"{DatabaseFixture.Prefix}AskJoobyStartUser{suffix}";
            createdUsers.Add(userName);

            await dbContext.InsertAsync(new UserRegistry
            {
                Guid = Guid.NewGuid(), RoleRegistryGuid = roleGuid, Name = userName,
                Email = $"{userName}@example.invalid",
                Password = "not-used-by-permission-checks", Active = 1, PasswordLocked = 0, Deleted = 0, Version = 1,
                CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
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

        [Fact]
        public async Task StartConversationIsANoOpWhenTheConversationAlreadyHasMessagesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) = await SeedPermittedUserAsync(dbContext);
            var conversationId = await SeedConversationAsync(dbContext, tenantId, userName);
            var messageRepository = new AskJoobyMessageRepository(dbContext);

            await messageRepository.InsertAsync(new AskJoobyMessage
            {
                AskJoobyConversationId = conversationId, RoleId = (byte)AskJoobyMessageRole.User,
                Content = "Already talking."
            });

            var service = await AskJoobyConversationService.CreateAsync(dbContext, userName, TestLog.NoOp,
                localizers, new NullServiceChangeBus(), new NoOpChatClient(), null);

            var items = new List<string>();

            await foreach (var item in service.StartConversationAsync(conversationId, null, true))
            {
                items.Add(item.Kind);
            }

            items.Should().BeEmpty("a conversation that's already started must never be greeted a second time");

            var entries = await messageRepository.ListEntriesByConversationAsync(conversationId);
            entries.Should().HaveCount(1, "no extra message should have been inserted");
        }

        [Fact]
        public async Task StartConversationUsesTheStaticFallbackWhenNoInferenceBackendIsConfiguredAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) = await SeedPermittedUserAsync(dbContext);
            var conversationId = await SeedConversationAsync(dbContext, tenantId, userName);
            var messageRepository = new AskJoobyMessageRepository(dbContext);

            var service = await AskJoobyConversationService.CreateAsync(dbContext, userName, TestLog.NoOp,
                localizers, new NullServiceChangeBus(), new NoOpChatClient(), null);

            var texts = new List<string>();

            await foreach (var item in service.StartConversationAsync(conversationId, null, false))
            {
                if (item.Kind == "Text")
                {
                    texts.Add(item.Text);
                }
            }

            texts.Should().ContainSingle();

            texts[0].Should().Contain("inference server",
                "the fallback must tell whoever's running this deployment what's actually wrong");

            var entries = await messageRepository.ListEntriesByConversationAsync(conversationId);
            entries.Should()
                .ContainSingle(e => e.RoleId == (byte)AskJoobyMessageRole.Assistant && e.Content == texts[0]);
        }

        [Fact]
        public async Task StartConversationNeverCallsTheChatClientWhenNoInferenceBackendIsConfiguredAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) = await SeedPermittedUserAsync(dbContext);
            var conversationId = await SeedConversationAsync(dbContext, tenantId, userName);
            var chatClient = new NoOpChatClient();

            var service = await AskJoobyConversationService.CreateAsync(dbContext, userName, TestLog.NoOp,
                localizers, new NullServiceChangeBus(), chatClient, null);

            await foreach (var _ in service.StartConversationAsync(conversationId, null, false))
            {
            }

            chatClient.WasCalled.Should().BeFalse(
                "the static fallback must never attempt a live model call");
        }

        [Fact]
        public async Task StartConversationSendsTheEmptyReplyFallbackWhenTheModelProducesNoTextAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) = await SeedPermittedUserAsync(dbContext);
            var conversationId = await SeedConversationAsync(dbContext, tenantId, userName);

            var fakeChatClient = new EmptyReplyFakeChatClient();
            var messageRepository = new AskJoobyMessageRepository(dbContext);

            var service = await AskJoobyConversationService.CreateAsync(dbContext, userName, TestLog.NoOp,
                localizers, new NullServiceChangeBus(), fakeChatClient, null,
                connectionString: connectionString);

            var items = new List<string>();

            await foreach (var item in service.StartConversationAsync(conversationId, "AdvancedAnalytics", true))
            {
                items.Add(item.Kind);
            }

            items.Should().Contain("Text", "a turn that ends with nothing to say must still "
                                           + "surface the same friendly EmptyReply fallback that SendMessageAsync falls back to -- "
                                           + "silently ending the stream here is indistinguishable from Jooby dying on the response, "
                                           + "and leaves the caller with no choice but to send a brand new message to get anything "
                                           + "moving again");

            var entries = await messageRepository.ListEntriesByConversationAsync(conversationId);

            entries.Should().Contain(e => e.RoleId == (byte)AskJoobyMessageRole.Assistant,
                "the fallback reply must be persisted, exactly as SendMessageAsync persists its own "
                + "EmptyReply fallback, so the conversation history is not left silently one turn short");
        }

        [Fact]
        public async Task StatsQueryCountsBitesGloballyButTrailsAndSaltlicksPerTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) = await SeedPermittedUserAsync(dbContext);
            var (otherTenantId, otherUserName) = await SeedPermittedUserAsync(dbContext);
            await SeedConversationAsync(dbContext, tenantId, userName);
            await SeedConversationAsync(dbContext, otherTenantId, otherUserName);

            var memoryRepository = new AskJoobyMemoryRepository(dbContext, tenantId, userName);
            await memoryRepository.InsertAsync("Mine.", (byte)AskJoobyMemoryScope.User, null);
            var otherMemoryRepository = new AskJoobyMemoryRepository(dbContext, otherTenantId, otherUserName);
            await otherMemoryRepository.InsertAsync("Not mine.", (byte)AskJoobyMemoryScope.User, null);

            var query = new GetAskJoobyStartupStatsQuery(dbContext);
            var stats = await query.ExecuteAsync(tenantId, userName);
            var otherStats = await query.ExecuteAsync(otherTenantId, otherUserName);

            stats.Bites.Should().Be(otherStats.Bites, "Bites has no tenant column -- it's a global count");
            stats.Trails.Should().BeGreaterThanOrEqualTo(1);
            stats.Saltlicks.Should().Be(1, "only this tenant's own memory should be counted");
            otherStats.Saltlicks.Should().Be(1, "the other tenant's memory must not leak into this count");
        }

        private sealed class NoOpChatClient : IChatClient
        {
            public bool WasCalled { get; private set; }

            public Task<ChatResponse> GetResponseAsync(
                IEnumerable<ChatMessage> messages, ChatOptions? options = null,
                CancellationToken cancellationToken = default)
            {
                WasCalled = true;

                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
            }

            public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
                IEnumerable<ChatMessage> messages, ChatOptions? options = null,
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                WasCalled = true;
                await Task.CompletedTask;
                yield return new ChatResponseUpdate(ChatRole.Assistant, "ok");
            }

            public object? GetService(Type serviceType, object? serviceKey = null)
            {
                return null;
            }

            public void Dispose()
            {
            }
        }

        private sealed class EmptyReplyFakeChatClient : IChatClient
        {
            public Task<ChatResponse> GetResponseAsync(
                IEnumerable<ChatMessage> messages, ChatOptions? options = null,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, string.Empty)));
            }

            public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
                IEnumerable<ChatMessage> messages, ChatOptions? options = null,
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                await Task.Delay(1, cancellationToken);
                yield return new ChatResponseUpdate(ChatRole.Assistant, string.Empty);
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