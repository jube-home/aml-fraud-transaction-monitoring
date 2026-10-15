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
    public sealed class AskJoobyConversationReviseStreamingTests(DatabaseFixture fx) : IAsyncLifetime
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
                await dbContext.TenantRegistry.Where(w => createdTenantIds.Contains(w.Id)).DeleteAsync();
            }
        }

        [Fact]
        public async Task AStepFromTheReviseSubAgentsOwnToolCallReachesTheLiveStreamAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) = await SeedPermittedUserAsync(dbContext);
            var conversationId = await SeedConversationAsync(dbContext, tenantId, userName);

            var fakeChatClient = new RejectThenReviseFakeChatClient();

            var service = await AskJoobyConversationService.CreateAsync(dbContext, userName, TestLog.NoOp,
                localizers, new NullServiceChangeBus(), fakeChatClient, null,
                true, connectionString: connectionString);

            var stepTexts = new List<string>();
            var outcomeTexts = new List<string>();
            var revisionTexts = new List<string>();
            Exception? thrown = null;

            try
            {
                await foreach (var item in service.SendMessageAsync(
                                   conversationId, "What is the answer?", null, null, CancellationToken.None))
                {
                    if (item.Kind == "Step")
                    {
                        stepTexts.Add(item.Text);
                    }
                    else if (item.Kind == "Validation")
                    {
                        outcomeTexts.Add(item.Text);
                    }
                    else if (item.Kind == "Revision")
                    {
                        revisionTexts.Add(item.Text);
                    }
                }
            }
            catch (Exception ex)
            {
                thrown = ex;
            }

            thrown.Should().BeNull();

            outcomeTexts.Should().Contain(s => s.Contains("sending it back to the agent"),
                "the validator must genuinely reject the first draft for this scenario to exercise revise");

            stepTexts.Should().Contain(s => s.Contains("Grazing the Pasture") || s.Contains("Grazed the Pasture"),
                "the revise sub-agent's own SearchRag call must now reach the live stream as a Step " +
                "item -- before this fix, RunAsync never produced one at all, so the caller saw " +
                "nothing during the whole revise pass");

            revisionTexts.Should().Contain(s => s.Contains("Handing this over to the agent"),
                "a marker must announce the handoff to the revise sub-agent even when it does no " +
                "visible tool work of its own");

            revisionTexts.Should().Contain(s => s.Contains("Handing the revision back to"),
                "a marker must announce the second handoff back to the validator for the re-check");
        }

        private async Task<(int TenantId, string UserName)> SeedPermittedUserAsync(
            DbContext dbContext)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];

            var tenantId = await dbContext.InsertWithInt32IdentityAsync(new TenantRegistry
            {
                Name = $"{DatabaseFixture.Prefix}AskJoobyReviseStreamTenant{suffix}", Active = 1, Locked = 0,
                Deleted = 0, Landlord = 0, Version = 1, CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            createdTenantIds.Add(tenantId);

            var roleGuid = Guid.NewGuid();

            var roleId = await dbContext.InsertWithInt32IdentityAsync(new RoleRegistry
            {
                Guid = roleGuid, Name = $"{DatabaseFixture.Prefix}AskJoobyReviseStreamRole{suffix}", Active = 1,
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

            var userName = $"{DatabaseFixture.Prefix}AskJoobyReviseStreamUser{suffix}";
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

        private sealed class RejectThenReviseFakeChatClient : IChatClient
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
                var messageList = messages.ToList();
                var allText = string.Join("\n", messageList.Select(m => m.Text));
                await Task.Delay(10, cancellationToken);

                if (allText.Contains("Generated reply to check:"))
                {
                    yield return new ChatResponseUpdate(ChatRole.Assistant,
                        allText.Contains("Grounded via a real Pasture search this time.")
                            ? "STATUS: PASSED\nREASON:\nKIND:"
                            : "STATUS: FAILED\nREASON: The claim isn't backed by any Bite.\nKIND: ERROR");

                    yield break;
                }

                if (allText.Contains("An independent review of your reply against the Pasture found:"))
                {
                    var alreadySearched = messageList.Any(m => m.Role == ChatRole.Tool);

                    if (!alreadySearched)
                    {
                        yield return new ChatResponseUpdate(ChatRole.Assistant,
                        [
                            new FunctionCallContent("call-revise-search", "SearchRag", new Dictionary<string, object?>
                            {
                                ["query"] = "the flagged claim"
                            })
                        ]);

                        yield break;
                    }

                    yield return new ChatResponseUpdate(ChatRole.Assistant,
                        "Grounded via a real Pasture search this time.");

                    yield break;
                }

                yield return new ChatResponseUpdate(ChatRole.Assistant, "The answer is 42.");
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