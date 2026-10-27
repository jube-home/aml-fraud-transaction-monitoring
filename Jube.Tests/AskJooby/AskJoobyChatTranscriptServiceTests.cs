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
using Jube.AskJooby.Security;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Service.AskJoobyConversation;
using Jube.Service.Exceptions.AskJoobyConversation;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    [Collection("Database")]
    public sealed class AskJoobyChatTranscriptServiceTests(DatabaseFixture fx) : IAsyncLifetime
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
                await dbContext.AskJoobyMessage
                    .Where(w => createdConversationIds.Contains(w.AskJoobyConversationId!.Value)).DeleteAsync();

                await dbContext.AskJoobyCompaction
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

        private async Task<(int TenantId, string UserName)> SeedUserAsync(
            DbContext dbContext, bool landlord, bool grantAccess, int? tenantId = null, bool userActive = true,
            bool passwordLocked = false)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];

            tenantId ??= await dbContext.InsertWithInt32IdentityAsync(new TenantRegistry
            {
                Name = $"{DatabaseFixture.Prefix}AskJoobyTranscriptTenant{suffix}", Active = 1, Locked = 0,
                Deleted = 0, Landlord = landlord ? (byte)1 : (byte)0, Version = 1, CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });

            createdTenantIds.Add(tenantId.Value);

            var roleGuid = Guid.NewGuid();

            var roleId = await dbContext.InsertWithInt32IdentityAsync(new RoleRegistry
            {
                Guid = roleGuid, Name = $"{DatabaseFixture.Prefix}AskJoobyTranscriptRole{suffix}", Active = 1,
                Locked = 0, Deleted = 0, TenantRegistryId = tenantId.Value, Version = 1,
                CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
            });
            createdRoleIds.Add(roleId);

            if (grantAccess)
            {
                await dbContext.InsertAsync(new RoleRegistryPermission
                {
                    Guid = Guid.NewGuid(), PermissionSpecificationId = AskJoobyPermissions.Access,
                    RoleRegistryId = roleId, Active = 1, Locked = 0, Deleted = 0, Version = 1,
                    CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
                });
            }

            var userName = $"{DatabaseFixture.Prefix}AskJoobyTranscriptUser{suffix}";
            createdUsers.Add(userName);

            await dbContext.InsertAsync(new UserRegistry
            {
                Guid = Guid.NewGuid(), RoleRegistryGuid = roleGuid, Name = userName,
                Email = $"{userName}@example.invalid", Password = "not-used-by-permission-checks", Active = userActive ? (byte)1 : (byte)0,
                PasswordLocked = passwordLocked ? (byte)1 : (byte)0, Deleted = 0, Version = 1, CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });

            await dbContext.InsertAsync(new UserInTenant { User = userName, TenantRegistryId = tenantId.Value });

            return (tenantId.Value, userName);
        }

        private async Task<int> SeedConversationAsync(
            DbContext dbContext, int tenantRegistryId, string createdUser)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new AskJoobyConversation
            {
                TenantRegistryId = tenantRegistryId, Guid = Guid.NewGuid(), Title = "Test conversation",
                CreatedDate = DateTime.UtcNow, CreatedUser = createdUser, Version = 1
            });
            createdConversationIds.Add(id);

            return id;
        }

        [Fact]
        public async Task GenerateAsyncSucceedsForANonLandlordUserHoldingAccessAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) = await SeedUserAsync(dbContext, landlord: false, grantAccess: true);
            var conversationId = await SeedConversationAsync(dbContext, tenantId, userName);

            var service = await AskJoobyChatTranscriptService.CreateAsync(
                dbContext, userName, TestLog.NoOp, localizers, new NullServiceChangeBus());

            var (markdown, fileName) = await service.GenerateAsync(conversationId);

            markdown.Should().NotBeNullOrEmpty();
            fileName.Should().EndWith(".md");
        }

        [Fact]
        public async Task GenerateAsyncSucceedsForALandlordUserWithNoOrdinaryPermissionsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) = await SeedUserAsync(dbContext, landlord: true, grantAccess: false);
            var conversationId = await SeedConversationAsync(dbContext, tenantId, userName);

            var service = await AskJoobyChatTranscriptService.CreateAsync(
                dbContext, userName, TestLog.NoOp, localizers, new NullServiceChangeBus());

            var (markdown, _) = await service.GenerateAsync(conversationId);

            markdown.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task GenerateAsyncSucceedsForAnActiveUserHoldingNoPermissionsAtAllAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) = await SeedUserAsync(dbContext, landlord: false, grantAccess: false);
            var conversationId = await SeedConversationAsync(dbContext, tenantId, userName);

            var service = await AskJoobyChatTranscriptService.CreateAsync(
                dbContext, userName, TestLog.NoOp, localizers, new NullServiceChangeBus());

            var (markdown, _) = await service.GenerateAsync(conversationId);

            markdown.Should().NotBeNullOrEmpty(
                "every active user has Ask Jooby in some form, so no explicit Access grant is required");
        }

        [Fact]
        public async Task GenerateAsyncIsForbiddenForAnInactiveUserAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) = await SeedUserAsync(dbContext, landlord: false, grantAccess: true,
                userActive: false);
            var conversationId = await SeedConversationAsync(dbContext, tenantId, userName);

            var service = await AskJoobyChatTranscriptService.CreateAsync(
                dbContext, userName, TestLog.NoOp, localizers, new NullServiceChangeBus());

            var act = () => service.GenerateAsync(conversationId);

            await act.Should().ThrowAsync<ForbiddenException>(
                "implicit access applies to active users only, even when an explicit grant is present");
        }

        [Fact]
        public async Task GenerateAsyncIsForbiddenForAPasswordLockedUserAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) = await SeedUserAsync(dbContext, landlord: false, grantAccess: false,
                passwordLocked: true);
            var conversationId = await SeedConversationAsync(dbContext, tenantId, userName);

            var service = await AskJoobyChatTranscriptService.CreateAsync(
                dbContext, userName, TestLog.NoOp, localizers, new NullServiceChangeBus());

            var act = () => service.GenerateAsync(conversationId);

            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task ActiveUserIsTrueForAnActiveUserWithoutAnExplicitAccessGrantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await SeedUserAsync(dbContext, landlord: false, grantAccess: false);

            var dto = await new Jube.Data.Security.PermissionValidation()
                .GetPermissionsAsync(dbContext, userName, TestLog.NoOp);

            dto.ActiveUser.Should().BeTrue();
            dto.Permissions.Should().NotContain(AskJoobyPermissions.Access,
                "implicit chat access must not leak into the explicit permission list that gates operational data");
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public async Task ActiveUserIsFalseForAnInactiveOrPasswordLockedUserAsync(bool inactive, bool locked)
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await SeedUserAsync(dbContext, landlord: false, grantAccess: true,
                userActive: !inactive, passwordLocked: locked);

            var dto = await new Jube.Data.Security.PermissionValidation()
                .GetPermissionsAsync(dbContext, userName, TestLog.NoOp);

            dto.ActiveUser.Should().BeFalse();
        }

        [Fact]
        public async Task ActiveUserIsFalseForAnUnknownUserAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var dto = await new Jube.Data.Security.PermissionValidation()
                .GetPermissionsAsync(dbContext, $"{DatabaseFixture.Prefix}NoSuchUser", TestLog.NoOp);

            dto.ActiveUser.Should().BeFalse();
        }

        [Fact]
        public async Task GenerateAsyncIsForbiddenForADifferentUserInTheSameTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, creator) = await SeedUserAsync(dbContext, landlord: false, grantAccess: true);
            var (_, colleague) = await SeedUserAsync(dbContext, landlord: false, grantAccess: true, tenantId);
            var conversationId = await SeedConversationAsync(dbContext, tenantId, creator);

            var service = await AskJoobyChatTranscriptService.CreateAsync(
                dbContext, colleague, TestLog.NoOp, localizers, new NullServiceChangeBus());

            var act = () => service.GenerateAsync(conversationId);

            await act.Should().ThrowAsync<ForbiddenException>(
                "the transcript export is a standalone file leaving the app, so it's creator-only even " +
                "though the same colleague could still open and read this conversation live in the " +
                "chat UI, exactly like the Technical Audit's own guarantee");
        }

        [Fact]
        public async Task GenerateAsyncIsNotFoundForAUserInADifferentTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, creator) = await SeedUserAsync(dbContext, landlord: false, grantAccess: true);
            var (_, stranger) = await SeedUserAsync(dbContext, landlord: false, grantAccess: true);
            var conversationId = await SeedConversationAsync(dbContext, tenantId, creator);

            var service = await AskJoobyChatTranscriptService.CreateAsync(
                dbContext, stranger, TestLog.NoOp, localizers, new NullServiceChangeBus());

            var act = () => service.GenerateAsync(conversationId);

            await act.Should().ThrowAsync<NotFoundException>(
                "a different tenant's conversation must not even be confirmed to exist");
        }

        [Fact]
        public async Task GenerateAsyncStatesHistoryWasNeverFoldedWhenThereWasNoCompactionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) = await SeedUserAsync(dbContext, landlord: false, grantAccess: true);
            var conversationId = await SeedConversationAsync(dbContext, tenantId, userName);

            var service = await AskJoobyChatTranscriptService.CreateAsync(
                dbContext, userName, TestLog.NoOp, localizers, new NullServiceChangeBus());

            var (markdown, _) = await service.GenerateAsync(conversationId);

            markdown.Should().Contain(
                "This conversation's history was never folded to fit the model's context window; " +
                "every message below is the complete, original record.",
                "a regulator reading only the Transcript must be told, in the Transcript itself, that " +
                "nothing was silently dropped, without having to cross-reference the Technical Audit");
        }

        [Fact]
        public async Task GenerateAsyncStatesHistoryWasFoldedButNeverLostWhenCompactionRanAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) = await SeedUserAsync(dbContext, landlord: false, grantAccess: true);
            var conversationId = await SeedConversationAsync(dbContext, tenantId, userName);

            await new AskJoobyCompactionRepository(dbContext, tenantId, userName).InsertWithEmbeddingAsync(
                new AskJoobyCompaction
                {
                    AskJoobyConversationId = conversationId, MessageCount = 5, BeforeCharacters = 400,
                    AfterCharacters = 40, Before = "User: hello\n\nAssistant: hi", After = "Greeting exchanged.",
                    DurationMs = 12
                }, null);

            var service = await AskJoobyChatTranscriptService.CreateAsync(
                dbContext, userName, TestLog.NoOp, localizers, new NullServiceChangeBus());

            var (markdown, _) = await service.GenerateAsync(conversationId);

            markdown.Should().Contain(
                "This conversation's history was folded 1 time(s) to fit the model's context window " +
                "during later turns.",
                "the Transcript must tell the reader folding happened, matching what the Technical " +
                "Audit's own History Compaction section already discloses");

            markdown.Should().Contain(
                "Folding only ever shortened what later turns of Jooby could see internally -- it " +
                "never removed or altered a message. Every message below is the complete, original " +
                "record.",
                "the completeness guarantee must be stated plainly, not just implied, since this file " +
                "can be the one held out for regulator use");
        }
    }
}