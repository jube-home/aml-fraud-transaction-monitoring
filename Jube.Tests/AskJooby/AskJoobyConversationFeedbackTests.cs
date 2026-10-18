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
using Jube.Service.Exceptions.AskJoobyConversation;
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
    public sealed class AskJoobyConversationFeedbackTests(DatabaseFixture fx) : IAsyncLifetime
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
                var createdRunIds = await dbContext.AskJoobyRun
                    .Where(r => createdConversationIds.Contains(r.AskJoobyConversationId!.Value))
                    .Select(r => r.Id).ToListAsync();

                if (createdRunIds.Count > 0)
                {
                    await dbContext.AskJoobyToolCall
                        .Where(w => createdRunIds.Contains(w.AskJoobyRunId!.Value)).DeleteAsync();
                }

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
                await dbContext.AskJoobyMemory.Where(w => createdTenantIds.Contains(w.TenantRegistryId!.Value))
                    .DeleteAsync();

                await dbContext.AskJoobyAgentGuardRejection
                    .Where(w => createdTenantIds.Contains(w.TenantRegistryId!.Value)).DeleteAsync();

                await dbContext.TenantRegistry.Where(w => createdTenantIds.Contains(w.Id)).DeleteAsync();
            }
        }

        private async Task<(int TenantId, string UserName)> SeedPermittedUserAsync(
            DbContext dbContext)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];

            var tenantId = await dbContext.InsertWithInt32IdentityAsync(new TenantRegistry
            {
                Name = $"{DatabaseFixture.Prefix}AskJoobyFeedbackTenant{suffix}", Active = 1, Locked = 0,
                Deleted = 0, Landlord = 0, Version = 1, CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            createdTenantIds.Add(tenantId);

            var roleGuid = Guid.NewGuid();

            var roleId = await dbContext.InsertWithInt32IdentityAsync(new RoleRegistry
            {
                Guid = roleGuid, Name = $"{DatabaseFixture.Prefix}AskJoobyFeedbackRole{suffix}", Active = 1,
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

            var userName = $"{DatabaseFixture.Prefix}AskJoobyFeedbackUser{suffix}";
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

        private static async Task<(int RunId, int MessageId)> SeedAnsweredTurnAsync(
            DbContext dbContext, int conversationId, string question, string reply)
        {
            var runId = await dbContext.InsertWithInt32IdentityAsync(new AskJoobyRun
            {
                AskJoobyConversationId = conversationId, AgentType = "general", Depth = 0,
                StatusId = (byte)AskJoobyRunStatus.Completed,
                Task = question, CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
            });

            var messageId = await dbContext.InsertWithInt32IdentityAsync(new AskJoobyMessage
            {
                AskJoobyConversationId = conversationId, RoleId = (byte)AskJoobyMessageRole.Assistant, Content = reply,
                AskJoobyRunId = runId, CreatedDate = DateTime.UtcNow,
                EntryTypeId = AskJoobyMessageRepository.EntryTypeMessage
            });

            return (runId, messageId);
        }

        [Fact]
        public async Task ThumbsUpStoresAUserScopedMemoryTaggedPositiveAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) = await SeedPermittedUserAsync(dbContext);
            var conversationId = await SeedConversationAsync(dbContext, tenantId, userName);

            var (_, messageId) = await SeedAnsweredTurnAsync(dbContext, conversationId,
                "How do I create a velocity rule?", "Use a TTL Counter with an Abstraction Rule.");

            var service = await AskJoobyConversationService.CreateAsync(dbContext, userName, TestLog.NoOp,
                localizers, new NullServiceChangeBus(), new NoOpChatClient(), null);

            var confirmation = await service.SubmitFeedbackAsync(conversationId, messageId, true);

            confirmation.Should().Contain("correct example");

            var memories = await dbContext.AskJoobyMemory
                .Where(w => w.TenantRegistryId == tenantId).ToListAsync();
            memories.Should().ContainSingle();

            memories[0].ScopeId.Should().Be((byte)AskJoobyMemoryScope.User,
                "a thumb is one person's judgement of one answer, often about their own preferences or "
                + "their own corner of the configuration, so it must not steer every other reader in the "
                + "tenant; Recall unions System with the caller's own, so they still get the benefit");

            memories[0].StatusId.Should().Be(1);

            memories[0].Content.Should().Contain("How do I create a velocity rule?")
                .And.Contain("Use a TTL Counter with an Abstraction Rule.");
        }

        [Fact]
        public async Task ThumbsDownStoresAUserScopedMemoryTaggedNegativeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) = await SeedPermittedUserAsync(dbContext);
            var conversationId = await SeedConversationAsync(dbContext, tenantId, userName);

            var (_, messageId) = await SeedAnsweredTurnAsync(dbContext, conversationId,
                "How do I increment a TTL Counter?", "Call IncrementTTLCounter() in the Activation Rule.");

            var service = await AskJoobyConversationService.CreateAsync(dbContext, userName, TestLog.NoOp,
                localizers, new NullServiceChangeBus(), new NoOpChatClient(), null);

            var confirmation = await service.SubmitFeedbackAsync(conversationId, messageId, false);

            confirmation.Should().Contain("incorrect example");

            var memories = await dbContext.AskJoobyMemory
                .Where(w => w.TenantRegistryId == tenantId).ToListAsync();
            memories.Should().ContainSingle();
            memories[0].StatusId.Should().Be(0);
        }

        [Fact]
        public async Task ThumbsUpLeavesAStepEntryInTheTranscriptAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) = await SeedPermittedUserAsync(dbContext);
            var conversationId = await SeedConversationAsync(dbContext, tenantId, userName);

            var (_, messageId) = await SeedAnsweredTurnAsync(dbContext, conversationId,
                "What is a Bite?", "A Bite is a chunk of the Pasture's indexed documentation.");

            var service = await AskJoobyConversationService.CreateAsync(dbContext, userName, TestLog.NoOp,
                localizers, new NullServiceChangeBus(), new NoOpChatClient(), null);

            await service.SubmitFeedbackAsync(conversationId, messageId, true);

            var messageRepository = new AskJoobyMessageRepository(dbContext);
            var entries = await messageRepository.ListEntriesByConversationAsync(conversationId);

            entries.Should().Contain(e => e.EntryTypeId == AskJoobyMessageRepository.EntryTypeStep,
                "the caller should see the same confirmation the agent's own Remember calls already produce");
        }

        [Fact]
        public async Task ThumbsUpOnAReplyHoldingPersonalDataStoresARedactedMemoryAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) = await SeedPermittedUserAsync(dbContext);
            var conversationId = await SeedConversationAsync(dbContext, tenantId, userName);

            var (_, messageId) = await SeedAnsweredTurnAsync(dbContext, conversationId,
                "Why was the applicant's onboarding declined?",
                "The applicant's PAN is ABCDE1234F for verification.");

            var service = await AskJoobyConversationService.CreateAsync(dbContext, userName, TestLog.NoOp,
                localizers, new NullServiceChangeBus(), new NoOpChatClient(), null);

            var confirmation = await service.SubmitFeedbackAsync(conversationId, messageId, true);

            confirmation.Should().Contain("correct example").And.Contain("personal data redacted",
                "a thumb is an automatic capture of the whole exchange, so the caller cannot rephrase the "
                + "personal data away the way the agent's own Remember calls can; redacting keeps the thumb "
                + "working without the Salt Lick ever holding the personal data itself");

            var memories = await dbContext.AskJoobyMemory
                .Where(w => w.TenantRegistryId == tenantId).ToListAsync();
            memories.Should().ContainSingle();

            memories[0].Content.Should().NotContain("ABCDE1234F")
                .And.Contain("Why was the applicant's onboarding declined?");

            memories[0].StatusId.Should().Be(1);
        }

        [Fact]
        public async Task ThumbsUpOnAReplyHoldingASecretIsRefusedWithTheGuardsExplanationAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) = await SeedPermittedUserAsync(dbContext);
            var conversationId = await SeedConversationAsync(dbContext, tenantId, userName);

            var (_, messageId) = await SeedAnsweredTurnAsync(dbContext, conversationId,
                "How do I call the adaptation endpoint?",
                "Send the header token=ghp_16C7e42F292c6912E7710c838347Ae178B4a with the request.");

            var service = await AskJoobyConversationService.CreateAsync(dbContext, userName, TestLog.NoOp,
                localizers, new NullServiceChangeBus(), new NoOpChatClient(), null);

            var act = async () => await service.SubmitFeedbackAsync(conversationId, messageId, true);

            (await act.Should().ThrowAsync<DtoValidationException>(
                    "a secret must never be stored, redacted or not, and the caller deserves the guard's own "
                    + "explanation rather than an opaque status code"))
                .Which.Result.Errors.Should().ContainSingle(e => e.ErrorMessage.StartsWith("Not remembered:"));

            var memories = await dbContext.AskJoobyMemory
                .Where(w => w.TenantRegistryId == tenantId).ToListAsync();
            memories.Should().BeEmpty();
        }

        [Fact]
        public async Task RejectsFeedbackForAMessageThatIsNotAnAssistantReplyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) = await SeedPermittedUserAsync(dbContext);
            var conversationId = await SeedConversationAsync(dbContext, tenantId, userName);
            var messageRepository = new AskJoobyMessageRepository(dbContext);

            var userMessage = await messageRepository.InsertAsync(new AskJoobyMessage
            {
                AskJoobyConversationId = conversationId, RoleId = (byte)AskJoobyMessageRole.User,
                Content = "How do I create a velocity rule?"
            });

            var service = await AskJoobyConversationService.CreateAsync(dbContext, userName, TestLog.NoOp,
                localizers, new NullServiceChangeBus(), new NoOpChatClient(), null);

            var act = async () => await service.SubmitFeedbackAsync(conversationId, userMessage.Id, true);

            await act.Should().ThrowAsync<NotFoundException>(
                "feedback only ever makes sense against what Jooby said, not what the caller asked");
        }

        private sealed class NoOpChatClient : IChatClient
        {
            public Task<ChatResponse> GetResponseAsync(
                IEnumerable<ChatMessage> messages, ChatOptions? options = null,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
            }

            public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
                IEnumerable<ChatMessage> messages, ChatOptions? options = null,
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
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
    }
}