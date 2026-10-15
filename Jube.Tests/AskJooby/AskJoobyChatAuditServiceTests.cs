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
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.AskJooby.Security;
using Jube.Data.Context;
using Jube.Data.Poco;
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
    public sealed class AskJoobyChatAuditServiceTests(DatabaseFixture fx) : IAsyncLifetime
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

        private async Task<(int TenantId, string UserName)> SeedPermittedUserAsync(
            DbContext dbContext, int? tenantId = null)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];

            tenantId ??= await dbContext.InsertWithInt32IdentityAsync(new TenantRegistry
            {
                Name = $"{DatabaseFixture.Prefix}AskJoobyAuditTenant{suffix}", Active = 1, Locked = 0, Deleted = 0,
                Landlord = 1, Version = 1, CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
            });

            createdTenantIds.Add(tenantId.Value);

            var roleGuid = Guid.NewGuid();

            var roleId = await dbContext.InsertWithInt32IdentityAsync(new RoleRegistry
            {
                Guid = roleGuid, Name = $"{DatabaseFixture.Prefix}AskJoobyAuditRole{suffix}", Active = 1, Locked = 0,
                Deleted = 0, TenantRegistryId = tenantId.Value, Version = 1, CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            createdRoleIds.Add(roleId);

            await dbContext.InsertAsync(new RoleRegistryPermission
            {
                Guid = Guid.NewGuid(), PermissionSpecificationId = AskJoobyPermissions.Access, RoleRegistryId = roleId,
                Active = 1, Locked = 0, Deleted = 0, Version = 1, CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });

            var userName = $"{DatabaseFixture.Prefix}AskJoobyAuditUser{suffix}";
            createdUsers.Add(userName);

            await dbContext.InsertAsync(new UserRegistry
            {
                Guid = Guid.NewGuid(), RoleRegistryGuid = roleGuid, Name = userName,
                Email = $"{userName}@example.invalid", Password = "not-used-by-permission-checks", Active = 1,
                PasswordLocked = 0, Deleted = 0, Version = 1, CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });

            await dbContext.InsertAsync(new UserInTenant { User = userName, TenantRegistryId = tenantId.Value });

            return (tenantId.Value, userName);
        }

        private async Task<(int TenantId, string UserName)> SeedNonLandlordUserWithAccessAsync(
            DbContext dbContext)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];

            var tenantId = await dbContext.InsertWithInt32IdentityAsync(new TenantRegistry
            {
                Name = $"{DatabaseFixture.Prefix}AskJoobyAuditNonLandlordTenant{suffix}", Active = 1, Locked = 0,
                Deleted = 0, Landlord = 0, Version = 1, CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            createdTenantIds.Add(tenantId);

            var roleGuid = Guid.NewGuid();

            var roleId = await dbContext.InsertWithInt32IdentityAsync(new RoleRegistry
            {
                Guid = roleGuid, Name = $"{DatabaseFixture.Prefix}AskJoobyAuditNonLandlordRole{suffix}", Active = 1,
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

            var userName = $"{DatabaseFixture.Prefix}AskJoobyAuditNonLandlordUser{suffix}";
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
                TenantRegistryId = tenantRegistryId, Guid = Guid.NewGuid(), Title = "Test conversation",
                CreatedDate = DateTime.UtcNow, CreatedUser = createdUser, Version = 1
            });
            createdConversationIds.Add(id);

            return id;
        }

        [Fact]
        public async Task GenerateAsyncSucceedsForTheConversationsOwnCreatorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) = await SeedPermittedUserAsync(dbContext);
            var conversationId = await SeedConversationAsync(dbContext, tenantId, userName);

            var service = await AskJoobyChatAuditService.CreateAsync(
                dbContext, userName, TestLog.NoOp, localizers, new NullServiceChangeBus());

            var (zip, fileName) = await service.GenerateAsync(conversationId);

            zip.Should().NotBeNullOrEmpty();
            fileName.Should().EndWith(".zip");

            using var zipStream = new MemoryStream(zip);
            await using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

            archive.GetEntry("technical-log.md").Should().NotBeNull(
                "the bundle's lean narrative must always be present");
            archive.GetEntry("manifest.json").Should().NotBeNull(
                "tooling that wants structured access to the bundle needs the manifest to exist");
            archive.GetEntry("health.json").Should().NotBeNull(
                "the rule-based health scorecard must always be present");
            archive.GetEntry("rag-gaps.json").Should().NotBeNull(
                "the RAG/tooling coverage-gap report must always be present");
            archive.GetEntry("rag-performance.json").Should().NotBeNull(
                "the RAG response-size/duration performance report must always be present");
            archive.GetEntry("context-efficiency.json").Should().NotBeNull(
                "the context-efficiency report must always be present");
        }

        [Fact]
        public async Task GenerateAsyncIsForbiddenForADifferentUserInTheSameTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, creator) = await SeedPermittedUserAsync(dbContext);
            var (_, colleague) = await SeedPermittedUserAsync(dbContext, tenantId);
            var conversationId = await SeedConversationAsync(dbContext, tenantId, creator);

            var service = await AskJoobyChatAuditService.CreateAsync(
                dbContext, colleague, TestLog.NoOp, localizers, new NullServiceChangeBus());

            var act = () => service.GenerateAsync(conversationId);

            await act.Should().ThrowAsync<ForbiddenException>(
                "the audit export is a standalone file leaving the app, so it's creator-only even " +
                "though the same colleague could still open and read this conversation live in the " +
                "chat UI");
        }

        [Fact]
        public async Task GenerateAsyncIsForbiddenForANonLandlordUserEvenHoldingAccessAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) = await SeedNonLandlordUserWithAccessAsync(dbContext);
            var conversationId = await SeedConversationAsync(dbContext, tenantId, userName);

            var service = await AskJoobyChatAuditService.CreateAsync(
                dbContext, userName, TestLog.NoOp, localizers, new NullServiceChangeBus());

            var act = () => service.GenerateAsync(conversationId);

            await act.Should().ThrowAsync<ForbiddenException>(
                "the Technical Audit is Landlord-only -- holding the ordinary AskJoobyPermissions.Access " +
                "permission is not a substitute, unlike its Transcript sibling's Landlord-or-Access rule");
        }

        [Fact]
        public async Task GenerateAsyncIsNotFoundForAUserInADifferentTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, creator) = await SeedPermittedUserAsync(dbContext);
            var (_, stranger) = await SeedPermittedUserAsync(dbContext);
            var conversationId = await SeedConversationAsync(dbContext, tenantId, creator);

            var service = await AskJoobyChatAuditService.CreateAsync(
                dbContext, stranger, TestLog.NoOp, localizers, new NullServiceChangeBus());

            var act = () => service.GenerateAsync(conversationId);

            await act.Should().ThrowAsync<NotFoundException>(
                "a different tenant's conversation must not even be confirmed to exist");
        }
    }
}