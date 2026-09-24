/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
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
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Dictionary;
using Jube.Dto.Query.EntityAnalysisModelInvocationContext;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.Helpers;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Query.EntityAnalysisModelInvocationContext;
using Jube.Service.Query.EntityAnalysisModelInvocationContext;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Query.EntityAnalysisModelInvocationContext
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EntityAnalysisModelInvocationContextServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<Guid> createdArchiveGuids = [];
        private readonly List<int> createdModelIds = [];
        private readonly List<int> createdRoleRegistryIds = [];
        private readonly List<int> createdTenantRegistryIds = [];
        private readonly List<string> createdUserNames = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var roleIds = createdRoleRegistryIds.Select(id => (int?)id).ToList();
            var modelIds = createdModelIds.Select(id => (int?)id).ToList();

            await dbContext.Archive.Where(w => createdArchiveGuids.Contains(w.EntityAnalysisModelInstanceEntryGuid))
                .DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelTtlCounter>()
                .Where(w => modelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModelRequestXpath.Where(w => modelIds.Contains(w.EntityAnalysisModelId))
                .DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => createdModelIds.Contains(w.Id)).DeleteAsync();
            await dbContext.RoleRegistryPermission.Where(w => roleIds.Contains(w.RoleRegistryId)).DeleteAsync();
            await dbContext.UserInTenant.Where(w => createdUserNames.Contains(w.User)).DeleteAsync();
            await dbContext.UserRegistry.Where(w => createdUserNames.Contains(w.Name)).DeleteAsync();
            await dbContext.RoleRegistry.Where(w => createdRoleRegistryIds.Contains(w.Id)).DeleteAsync();
            await dbContext.TenantRegistry.Where(w => createdTenantRegistryIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<EntityAnalysisModelInvocationContextService> BuildServiceAsync(DbContext dbContext,
            string userName)
        {
            return EntityAnalysisModelInvocationContextService.CreateAsync(dbContext, userName, TestLog.NoOp,
                localizers, new NullServiceChangeBus(), TestLog.NoOp);
        }

        private async Task<(string UserName, int TenantId)> CreateUserAsync(DbContext dbContext,
            params int[] permissionSpecificationIds)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var tenantRegistryId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.TenantRegistry
            {
                Name = $"{DatabaseFixture.Prefix}IcTenant{suffix}", Active = 1, Locked = 0, Deleted = 0,
                Landlord = 0, Version = 1, CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
            });
            createdTenantRegistryIds.Add(tenantRegistryId);

            var roleRegistryGuid = Guid.NewGuid();
            var roleRegistryId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.RoleRegistry
            {
                Guid = roleRegistryGuid, Name = $"{DatabaseFixture.Prefix}IcRole{suffix}", Active = 1, Locked = 0,
                Deleted = 0, TenantRegistryId = tenantRegistryId, Version = 1, CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            createdRoleRegistryIds.Add(roleRegistryId);

            foreach (var permissionSpecificationId in permissionSpecificationIds)
            {
                await dbContext.InsertAsync(new Data.Poco.RoleRegistryPermission
                {
                    Guid = Guid.NewGuid(), PermissionSpecificationId = permissionSpecificationId,
                    RoleRegistryId = roleRegistryId, Active = 1, Locked = 0, Deleted = 0, Version = 1,
                    CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
                });
            }

            var userName = $"{DatabaseFixture.Prefix}IcUser{suffix}";
            await dbContext.InsertAsync(new Data.Poco.UserRegistry
            {
                Guid = Guid.NewGuid(), RoleRegistryGuid = roleRegistryGuid, Name = userName,
                Email = $"{userName}@example.invalid", Password = "not-used-by-permission-checks", Active = 1,
                PasswordLocked = 0, Deleted = 0, Version = 1, CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            createdUserNames.Add(userName);
            await dbContext.InsertAsync(new Data.Poco.UserInTenant
                { User = userName, TenantRegistryId = tenantRegistryId });

            return (userName, tenantRegistryId);
        }

        private async Task<int> CreateModelAsync(DbContext dbContext, int tenantRegistryId)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}IcModel{Guid.NewGuid():N}"[..40], Guid = Guid.NewGuid(),
                TenantRegistryId = tenantRegistryId, EntryXPath = "$.id", ReferenceDateXPath = "$.at",
                ReferenceDatePayloadLocationTypeId = 1, Active = 1, Locked = 0, Deleted = 0, Version = 1,
                CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
            });
            createdModelIds.Add(id);

            foreach (var (name, dataTypeId, defaultValue) in new[]
                         { ("Amount", 3, ""), ("Country", 1, "GB"), ("AccountId", 1, "") })
            {
                await dbContext.InsertAsync(new Data.Poco.EntityAnalysisModelRequestXpath
                {
                    EntityAnalysisModelId = id, Name = name, DataTypeId = dataTypeId, XPath = $"$.{name}",
                    DefaultValue = defaultValue, Active = 1, Locked = 0, Deleted = 0, Version = 1, Cache = 1,
                    Guid = Guid.NewGuid(), CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
                });
            }

            await dbContext.InsertAsync(new Data.Poco.EntityAnalysisModelTtlCounter
            {
                EntityAnalysisModelId = id, Name = "PerAccount", TtlCounterDataName = "AccountId", Active = 1,
                Deleted = 0, Guid = Guid.NewGuid()
            });

            return id;
        }

        private async Task<Guid> ArchiveAsync(DbContext dbContext, int modelId)
        {
            var payload = new EntityAnalysisModelInstanceEntryPayload
            {
                Payload = new DictionaryNoBoxing<string>(), EntityInstanceEntryId = "TX-ARCH",
                ReferenceDate = new DateTime(2026, 9, 2, 8, 30, 0, DateTimeKind.Utc),
                TtlCounter = new PooledDictionary<string, double> { ["PerAccount"] = 3 }
            };
            payload.Payload.TryAdd("Amount", 99.5);
            payload.Payload.TryAdd("Country", "FR");
            var guid = Guid.NewGuid();

            await dbContext.InsertAsync(new Data.Poco.Archive
            {
                EntityAnalysisModelInstanceEntryGuid = guid, EntityAnalysisModelId = modelId,
                Json = Encoding.UTF8.GetString(BuildJsonResponses.BuildFullJson(payload,
                    new JsonSerializationHelper().ArchiveJsonSerializer)),
                EntryKeyValue = "TX-ARCH", CreatedDate = DateTime.UtcNow, ReferenceDate = payload.ReferenceDate
            });
            createdArchiveGuids.Add(guid);
            return guid;
        }

        [Fact]
        public async Task ABlankContextListsEveryRuleNameWithOnlyDefaultsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (user, tenant) = await CreateUserAsync(dbContext, 26);
            var modelId = await CreateModelAsync(dbContext, tenant);
            var service = await BuildServiceAsync(dbContext, user);

            var context = await service.BlankAsync(modelId);

            context.Source.Should().Be("Blank");
            context.Values.Select(v => v.Name).Should()
                .Contain(["Payload.Amount", "Payload.Country", "Payload.AccountId", "TTLCounter.PerAccount"]);
            context.Values.Where(v => v.Origin == "Default").Should().ContainSingle()
                .Which.Should().Match<InvocationContextValueDto>(v => v.Name == "Payload.Country" && v.Value == "GB");
        }

        [Fact]
        public async Task RequestJsonIsExtractedWithTheModelsEntryAndReferenceDateAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (user, tenant) = await CreateUserAsync(dbContext, 26);
            var modelId = await CreateModelAsync(dbContext, tenant);
            var service = await BuildServiceAsync(dbContext, user);

            var context = await service.FromRequestJsonAsync(new InvocationContextFromRequestJsonDto
            {
                EntityAnalysisModelId = modelId,
                RequestJson = "{\"id\":\"TX1\",\"at\":\"2026-09-01T10:00:00Z\",\"Amount\":12.5,\"AccountId\":\"A7\"}"
            });

            context.EntryId.Should().Be("TX1");
            context.ReferenceDate.Should().Be(new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc));
            context.Values.Single(v => v.Name == "Payload.Amount").Should().Match<InvocationContextValueDto>(v =>
                v.Value == "12.5" && v.Origin == "Extracted" && v.DataType == "double");
            context.Values.Single(v => v.Name == "TTLCounter.PerAccount").Origin.Should().Be("Unset");
            context.Stages.Single(s => s.Stage == "Extraction").Status.Should().Be("Computed");
            context.Stages.Single(s => s.Stage == "CaseCreation").Status.Should().Be("Excluded");
        }

        [Fact]
        public async Task UnreadableRequestJsonIsRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (user, tenant) = await CreateUserAsync(dbContext, 26);
            var modelId = await CreateModelAsync(dbContext, tenant);
            var service = await BuildServiceAsync(dbContext, user);

            await Assert.ThrowsAsync<InvalidRequestException>(() => service.FromRequestJsonAsync(
                new InvocationContextFromRequestJsonDto
                    { EntityAnalysisModelId = modelId, RequestJson = "{not json" }));
        }

        [Fact]
        public async Task AnArchivedTransactionIsReadBackWithItsValuesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (user, tenant) = await CreateUserAsync(dbContext, 1);
            var modelId = await CreateModelAsync(dbContext, tenant);
            var guid = await ArchiveAsync(dbContext, modelId);
            var service = await BuildServiceAsync(dbContext, user);

            var context = await service.FromArchiveAsync(new InvocationContextFromArchiveDto
                { EntityAnalysisModelId = modelId, EntityAnalysisModelInstanceEntryGuid = guid });

            context.Source.Should().Be("Archive");
            context.EntryId.Should().Be("TX-ARCH");
            context.Values.ToDictionary(v => v.Name, v => v.Value).Should().Contain(new Dictionary<string, string?>
                { ["Payload.Amount"] = "99.5", ["Payload.Country"] = "FR", ["TTLCounter.PerAccount"] = "3" });
        }

        [Fact]
        public async Task AnotherTenantsArchiveOrAnotherModelsArchiveIsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (user, tenant) = await CreateUserAsync(dbContext, 1);
            var (_, otherTenant) = await CreateUserAsync(dbContext, 1);
            var ownModel = await CreateModelAsync(dbContext, tenant);
            var otherOwnModel = await CreateModelAsync(dbContext, tenant);
            var foreignModel = await CreateModelAsync(dbContext, otherTenant);
            var foreignGuid = await ArchiveAsync(dbContext, foreignModel);
            var ownGuid = await ArchiveAsync(dbContext, ownModel);
            var service = await BuildServiceAsync(dbContext, user);

            await Assert.ThrowsAsync<NotFoundException>(() => service.FromArchiveAsync(
                new InvocationContextFromArchiveDto
                    { EntityAnalysisModelId = foreignModel, EntityAnalysisModelInstanceEntryGuid = foreignGuid }));
            await Assert.ThrowsAsync<NotFoundException>(() => service.FromArchiveAsync(
                new InvocationContextFromArchiveDto
                    { EntityAnalysisModelId = otherOwnModel, EntityAnalysisModelInstanceEntryGuid = ownGuid }));
        }

        [Fact]
        public async Task AnOverlayRoundTripsTheContextAndReportsBadNamesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (user, tenant) = await CreateUserAsync(dbContext, 26);
            var modelId = await CreateModelAsync(dbContext, tenant);
            var service = await BuildServiceAsync(dbContext, user);
            var blank = await service.BlankAsync(modelId);

            var context = await service.OverlayAsync(new InvocationContextOverlayDto
            {
                Context = blank,
                Values = new Dictionary<string, string?>
                    { ["TTLCounter.PerAccount"] = "5", ["Payload.Amount"] = "abc", ["Nope.Thing"] = "1" }
            });

            context.Values.Single(v => v.Name == "TTLCounter.PerAccount").Should()
                .Match<InvocationContextValueDto>(v => v.Value == "5" && v.Origin == "Overlay");
            context.Values.Single(v => v.Name == "Payload.Country").Value.Should().Be("GB");
            context.Errors.Should().HaveCount(2);
        }

        [Fact]
        public async Task PermissionsFollowTheirPurposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (authorOnly, tenant) = await CreateUserAsync(dbContext, 26);
            var (nobody, _) = await CreateUserAsync(dbContext);
            var modelId = await CreateModelAsync(dbContext, tenant);
            var guid = await ArchiveAsync(dbContext, modelId);

            var author = await BuildServiceAsync(dbContext, authorOnly);
            await Assert.ThrowsAsync<ForbiddenException>(() => author.FromArchiveAsync(
                new InvocationContextFromArchiveDto
                    { EntityAnalysisModelId = modelId, EntityAnalysisModelInstanceEntryGuid = guid }));

            var none = await BuildServiceAsync(dbContext, nobody);
            await Assert.ThrowsAsync<ForbiddenException>(() => none.BlankAsync(modelId));
        }

        [Fact]
        public void TheCatalogueListsTheFourContextOperations()
        {
            ServiceToolCatalogue.All.Where(t => t.Name.StartsWith("EntityAnalysisModelInvocationContext"))
                .Select(t => t.Name).Should().BeEquivalentTo([
                    "EntityAnalysisModelInvocationContextBlank", "EntityAnalysisModelInvocationContextFromRequestJson",
                    "EntityAnalysisModelInvocationContextFromArchive", "EntityAnalysisModelInvocationContextOverlay"
                ]);
        }
    }
}