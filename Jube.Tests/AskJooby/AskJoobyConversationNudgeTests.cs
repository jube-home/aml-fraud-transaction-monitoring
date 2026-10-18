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
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.AskJooby.Guidance;
using Jube.AskJooby.Security;
using Jube.Data.Context;
using Jube.Data.Poco;
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
    public sealed class AskJoobyConversationNudgeTests(DatabaseFixture fx) : IAsyncLifetime
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
        public async Task SendingWhileAnotherTurnIsRunningPersistsANudgeAndStartsNoNewRunAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) = await SeedPermittedUserAsync(dbContext);
            var conversationId = await SeedConversationAsync(dbContext, tenantId, userName);

            var turnTracker = new AskJoobyTurnTracker();
            var conversationKey = conversationId.ToString(CultureInfo.InvariantCulture);
            using var runningTurn = turnTracker.Begin(conversationKey);

            var service = await AskJoobyConversationService.CreateAsync(dbContext, userName, TestLog.NoOp,
                localizers, new NullServiceChangeBus(), new NeverCalledFakeChatClient(), null,
                turnTracker: turnTracker);

            var items = new List<string>();

            await foreach (var item in service.SendMessageAsync(
                               conversationId, "also check the other account", null, null, CancellationToken.None))
            {
                items.Add(item.Kind);
            }

            items.Should().BeEmpty(
                "a message sent while a turn is already running must not start a second reply of its own");

            var runCount = await dbContext.AskJoobyRun
                .Where(w => w.AskJoobyConversationId == conversationId).CountAsync();
            runCount.Should().Be(0, "no new agent run may be started while the conversation is already busy");

            var messages = await dbContext.AskJoobyMessage
                .Where(w => w.AskJoobyConversationId == conversationId).ToListAsync();

            messages.Should().ContainSingle()
                .Which.IsNudge.Should().BeTrue("the message was sent while a turn was already running");

            messages[0].Content.Should().Be("also check the other account");
        }

        [Fact]
        public async Task SendingWhileIdleNeverSetsIsNudgeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) = await SeedPermittedUserAsync(dbContext);
            var conversationId = await SeedConversationAsync(dbContext, tenantId, userName);

            var turnTracker = new AskJoobyTurnTracker();

            var service = await AskJoobyConversationService.CreateAsync(dbContext, userName, TestLog.NoOp,
                localizers, new NullServiceChangeBus(), new PlainReplyFakeChatClient(), null,
                turnTracker: turnTracker);

            await foreach (var _ in service.SendMessageAsync(
                               conversationId, "what is a Bite?", null, null, CancellationToken.None))
            {
            }

            var userMessage = await dbContext.AskJoobyMessage
                .Where(w => w.AskJoobyConversationId == conversationId
                            && w.RoleId == (byte)AskJoobyMessageRole.User)
                .SingleAsync();

            userMessage.IsNudge.Should().NotBe(true, "an idle send is an ordinary message, not a nudge");
        }

        [Fact]
        public async Task SendNudgeAlwaysPersistsEvenWhenNothingIsActuallyRunningAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) = await SeedPermittedUserAsync(dbContext);
            var conversationId = await SeedConversationAsync(dbContext, tenantId, userName);

            var service = await AskJoobyConversationService.CreateAsync(dbContext, userName, TestLog.NoOp,
                localizers, new NullServiceChangeBus(), new NeverCalledFakeChatClient(), null);

            await service.SendNudgeAsync(conversationId, "one more thing", CancellationToken.None);

            var messages = await dbContext.AskJoobyMessage
                .Where(w => w.AskJoobyConversationId == conversationId).ToListAsync();

            messages.Should().ContainSingle().Which.IsNudge.Should().BeTrue();
            messages[0].Content.Should().Be("one more thing");
        }

        [Fact]
        public async Task PendingGuidanceIsInjectedIntoTheRunningTurnsNextModelCallAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) = await SeedPermittedUserAsync(dbContext);
            var conversationId = await SeedConversationAsync(dbContext, tenantId, userName);

            var exchange = new AgentGuidanceExchange();

            exchange.Send(conversationId.ToString(CultureInfo.InvariantCulture),
                "actually, focus on the dormant accounts only");

            var fakeChatClient = new CapturingFakeChatClient();

            var service = await AskJoobyConversationService.CreateAsync(dbContext, userName, TestLog.NoOp,
                localizers, new NullServiceChangeBus(), fakeChatClient, null,
                guidanceExchange: exchange);

            await foreach (var _ in service.SendMessageAsync(
                               conversationId, "look at suspicious accounts", null, null, CancellationToken.None))
            {
            }

            fakeChatClient.CapturedMessages.Should().Contain(
                messages => messages.Any(m => m.Text.Contains("actually, focus on the dormant accounts only")),
                "RunContext.GuidanceExchange must actually reach the chat client the running turn calls, " +
                "not just sit unused on the context");
        }

        private async Task<(int TenantId, string UserName)> SeedPermittedUserAsync(
            DbContext dbContext)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];

            var tenantId = await dbContext.InsertWithInt32IdentityAsync(new TenantRegistry
            {
                Name = $"{DatabaseFixture.Prefix}AskJoobyNudgeTenant{suffix}", Active = 1, Locked = 0,
                Deleted = 0, Landlord = 0, Version = 1, CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            createdTenantIds.Add(tenantId);

            var roleGuid = Guid.NewGuid();

            var roleId = await dbContext.InsertWithInt32IdentityAsync(new RoleRegistry
            {
                Guid = roleGuid, Name = $"{DatabaseFixture.Prefix}AskJoobyNudgeRole{suffix}", Active = 1,
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

            var userName = $"{DatabaseFixture.Prefix}AskJoobyNudgeUser{suffix}";
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

        private sealed class NeverCalledFakeChatClient : IChatClient
        {
            public Task<ChatResponse> GetResponseAsync(
                IEnumerable<ChatMessage> messages, ChatOptions? options = null,
                CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException("The model must not be called while the turn is busy.");
            }

            public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
                IEnumerable<ChatMessage> messages, ChatOptions? options = null,
                CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException("The model must not be called while the turn is busy.");
            }

            public object? GetService(Type serviceType, object? serviceKey = null)
            {
                return null;
            }

            public void Dispose()
            {
            }
        }

        private sealed class PlainReplyFakeChatClient : IChatClient
        {
            public Task<ChatResponse> GetResponseAsync(
                IEnumerable<ChatMessage> messages, ChatOptions? options = null,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "general")));
            }

            public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
                IEnumerable<ChatMessage> messages, ChatOptions? options = null,
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                await Task.Delay(10, cancellationToken);

                yield return new ChatResponseUpdate(ChatRole.Assistant,
                    "A Bite is a chunk of the Pasture's indexed documentation.");
            }

            public object? GetService(Type serviceType, object? serviceKey = null)
            {
                return null;
            }

            public void Dispose()
            {
            }
        }

        private sealed class CapturingFakeChatClient : IChatClient
        {
            public List<List<ChatMessage>> CapturedMessages { get; } = [];

            public Task<ChatResponse> GetResponseAsync(
                IEnumerable<ChatMessage> messages, ChatOptions? options = null,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "general")));
            }

            public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
                IEnumerable<ChatMessage> messages, ChatOptions? options = null,
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                CapturedMessages.Add([.. messages]);
                await Task.Delay(10, cancellationToken);

                yield return new ChatResponseUpdate(ChatRole.Assistant,
                    "Flagged the accounts that match.");
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