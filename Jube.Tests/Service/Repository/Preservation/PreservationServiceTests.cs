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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Cryptography;
using Jube.Data.Context;
using Jube.Dto.Repository.Preservation;
using Jube.Preservation.Exceptions;
using Jube.Preservation.Models;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Repository.Preservation;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.Preservation;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.Repository.Preservation.Models;
using LinqToDB;
using MessagePack;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using log4net;

namespace Jube.Test.Service.Repository.Preservation
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class PreservationServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const string Salt = "ZzTestPreservationSalt";
        private const string Password = "correct horse";

        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdTenantRegistryIds = [];
        private readonly List<string> createdUserNames = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var ownPrefix = $"{DatabaseFixture.Prefix}Pr";
            var tenantIds = createdTenantRegistryIds
                .Concat(await dbContext.TenantRegistry.Where(w => w.Name != null && w.Name.StartsWith(ownPrefix))
                    .Select(s => s.Id).ToListAsync()).Distinct().ToList();
            var userNames = createdUserNames
                .Concat(await dbContext.UserRegistry.Where(w => w.Name != null && w.Name.StartsWith(ownPrefix))
                    .Select(s => s.Name.Required()).ToListAsync()).Distinct().ToList();
            var tenantIdsNullable = tenantIds.Select(id => (int?)id).ToList();

            var modelIds = await dbContext.EntityAnalysisModel
                .Where(w => tenantIdsNullable.Contains(w.TenantRegistryId)).Select(s => s.Id).ToListAsync();
            var modelGuids = await dbContext.EntityAnalysisModel
                .Where(w => tenantIdsNullable.Contains(w.TenantRegistryId)).Select(s => s.Guid).ToListAsync();
            var dictionaryIds = await dbContext.GetTable<Data.Poco.EntityAnalysisModelDictionary>()
                .Where(w => modelGuids.Contains(w.EntityAnalysisModelGuid)).Select(s => s.Id).ToListAsync();
            var listIds = await dbContext.GetTable<Data.Poco.EntityAnalysisModelList>()
                .Where(w => modelGuids.Contains(w.EntityAnalysisModelGuid)).Select(s => s.Id).ToListAsync();
            var dictionaryIdsNullable = dictionaryIds.Select(id => (int?)id).ToList();
            var listIdsNullable = listIds.Select(id => (int?)id).ToList();
            var modelIdsNullable = modelIds.Select(id => (int?)id).ToList();

            await dbContext.GetTable<Data.Poco.EntityAnalysisModelDictionaryKvpVersion>()
                .Where(w => dictionaryIdsNullable.Contains(w.EntityAnalysisModelDictionaryId)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelDictionaryKvp>()
                .Where(w => dictionaryIdsNullable.Contains(w.EntityAnalysisModelDictionaryId)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelDictionary>()
                .Where(w => dictionaryIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelListValue>()
                .Where(w => listIdsNullable.Contains(w.EntityAnalysisModelListId)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelList>()
                .Where(w => listIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelSuppression>()
                .Where(w => modelGuids.Contains(w.EntityAnalysisModelGuid)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelRequestXpathVersion>()
                .Where(w => modelIdsNullable.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelRequestXpath>()
                .Where(w => modelIdsNullable.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelVersion>()
                .Where(w => modelIdsNullable.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => modelIds.Contains(w.Id)).DeleteAsync();

            var roleIds = await dbContext.RoleRegistry
                .Where(w => tenantIdsNullable.Contains(w.TenantRegistryId)).Select(s => s.Id).ToListAsync();
            var roleIdsNullable = roleIds.Select(id => (int?)id).ToList();
            var userIds = await dbContext.UserRegistry.Where(w => userNames.Contains(w.Name))
                .Select(s => s.Id).ToListAsync();
            await dbContext.GetTable<Data.Poco.UserRegistryVersion>()
                .Where(w => userIds.Contains(w.UserRegistryId)).DeleteAsync();
            await dbContext.UserInTenant.Where(w => userNames.Contains(w.User)).DeleteAsync();
            await dbContext.UserRegistry.Where(w => userNames.Contains(w.Name)).DeleteAsync();
            await dbContext.RoleRegistryPermission
                .Where(w => roleIdsNullable.Contains(w.RoleRegistryId)).DeleteAsync();
            await dbContext.RoleRegistry.Where(w => roleIds.Contains(w.Id)).DeleteAsync();

            await dbContext.Import.Where(w => tenantIds.Contains(w.TenantRegistryId)).DeleteAsync();
            await dbContext.Export.Where(w => tenantIds.Contains(w.TenantRegistryId)).DeleteAsync();
            await dbContext.ExportPeek.Where(w => tenantIds.Contains(w.TenantRegistryId)).DeleteAsync();
            await dbContext.TenantRegistry.Where(w => tenantIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<PreservationService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null, bool legacyFallback = false)
        {
            return PreservationService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp, Salt, legacyFallback);
        }

        private async Task<Tenant> CreateTenantAsync(string label, bool withPermission = true)
        {
            await using var dbContext = fx.GetDbContext();
            var suffix = Guid.NewGuid().ToString("N")[..8];

            var tenantId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.TenantRegistry
            {
                Name = $"{DatabaseFixture.Prefix}Pr{label}T{suffix}",
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Landlord = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            createdTenantRegistryIds.Add(tenantId);

            var roleGuid = Guid.NewGuid();
            var roleName = $"{DatabaseFixture.Prefix}Pr{label}R{suffix}";
            var roleId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.RoleRegistry
            {
                Guid = roleGuid,
                Name = roleName,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                TenantRegistryId = tenantId,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });

            if (withPermission)
            {
                await dbContext.InsertAsync(new Data.Poco.RoleRegistryPermission
                {
                    Guid = Guid.NewGuid(),
                    PermissionSpecificationId = 38,
                    RoleRegistryId = roleId,
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                    Version = 1,
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = DatabaseFixture.Prefix
                });
            }

            var userName = $"{DatabaseFixture.Prefix}Pr{label}U{suffix}";
            await dbContext.InsertAsync(new Data.Poco.UserRegistry
            {
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleGuid,
                Name = userName,
                Email = $"{userName}@example.invalid",
                Password = "not-used-by-permission-checks",
                Active = 1,
                PasswordLocked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            createdUserNames.Add(userName);

            await dbContext.InsertAsync(new Data.Poco.UserInTenant { User = userName, TenantRegistryId = tenantId });

            return new Tenant(tenantId, userName, roleName, roleGuid);
        }

        private async Task<Seeded> SeedTenantAsync(Tenant tenant, string tag)
        {
            await using var dbContext = fx.GetDbContext();
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var modelName = $"{DatabaseFixture.Prefix}{tag}M{suffix}";
            var modelGuid = Guid.NewGuid();

            var modelId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EntityAnalysisModel
            {
                Name = modelName,
                Guid = modelGuid,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                TenantRegistryId = tenant.Id,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = tenant.UserName
            });

            var xpathName = $"{tag}Xp{suffix}";
            await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EntityAnalysisModelRequestXpath
            {
                Guid = Guid.NewGuid(),
                EntityAnalysisModelId = modelId,
                Name = xpathName,
                DataTypeId = 1,
                XPath = "$.a.b",
                Active = 1,
                Locked = 0,
                SearchKey = 1,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = tenant.UserName
            });

            var listName = $"{tag}Li{suffix}";
            var listId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EntityAnalysisModelList
            {
                Guid = Guid.NewGuid(),
                EntityAnalysisModelGuid = modelGuid,
                Name = listName,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = tenant.UserName
            });
            var listValue = $"{tag}Lv{suffix}";
            await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EntityAnalysisModelListValue
            {
                EntityAnalysisModelListId = listId,
                ListValue = listValue,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = tenant.UserName
            });

            var dictionaryName = $"{tag}Di{suffix}";
            var dictionaryId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EntityAnalysisModelDictionary
            {
                Guid = Guid.NewGuid(),
                EntityAnalysisModelGuid = modelGuid,
                Name = dictionaryName,
                DataName = "AccountId",
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = tenant.UserName
            });
            var kvpKey = $"{tag}Kk{suffix}";
            await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EntityAnalysisModelDictionaryKvp
            {
                Guid = Guid.NewGuid(),
                EntityAnalysisModelDictionaryId = dictionaryId,
                KvpKey = kvpKey,
                KvpValue = 42.5,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = tenant.UserName
            });

            return new Seeded(modelId, modelGuid, modelName, xpathName, listName, listValue, dictionaryName, kvpKey,
                42.5);
        }

        private static ImportExportOptionsDto FullOptions(bool roles = false)
        {
            return new ImportExportOptionsDto
            {
                Password = Password,
                Lists = true,
                Dictionaries = true,
                Suppressions = true,
                Roles = roles
            };
        }

        private async Task<byte[]> ExportAsync(Tenant tenant, ImportExportOptionsDto options)
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, tenant.UserName);
            return (await service.ExportAsync(options)).EncryptedBytes;
        }

        private async Task ImportAsync(Tenant tenant, byte[] bytes, string? password = Password, ILog? log = null,
            ILog? auditLog = null, IServiceChangeBus? bus = null, bool legacyFallback = false,
            CancellationToken token = default)
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, tenant.UserName, log, auditLog, bus, legacyFallback);
            await using var stream = new MemoryStream(bytes);
            await service.ImportAsync([new PreservationImportFileDto { FileName = "x.jemp", Content = stream }],
                password, token);
        }

        private static Wrapper Decrypt(byte[] encrypted, string password = Password)
        {
            var bytes = new JempAesEncryption(password, Salt).Decrypt(encrypted);
            return MessagePackSerializer.Deserialize<Wrapper>(bytes,
                MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray));
        }

        private static byte[] Encrypt(Wrapper wrapper)
        {
            var bytes = MessagePackSerializer.Serialize(wrapper,
                MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray));
            return new JempAesEncryption(Password, Salt).Encrypt(bytes);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync(string? userName)
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();

            var ex = await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, userName, log));

            ex.Code.Should().Be("NotAuthenticated");
            log.Entries.Should().Contain(e => e.Level == "WARN");
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task CreateWithUnmappedOrTenantlessUserThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, $"{DatabaseFixture.Prefix}NoSuchUser{Guid.NewGuid():N}"));
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UserNoTenant));
        }

        [Fact]
        public async Task WithoutPermissionAllOperationsForbiddenAndNothingWrittenAsync()
        {
            var tenant = await CreateTenantAsync("NoPerm", false);
            await SeedTenantAsync(tenant, "NoPerm");
            var log = new TestLog();
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, tenant.UserName, log);

            await using var stream = new MemoryStream([1, 2, 3]);
            var import = await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.ImportAsync([new PreservationImportFileDto { Content = stream }], Password));
            var peek = await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.ExportPeekAsync(true, true, true, true, true));
            var export = await Assert.ThrowsAsync<ForbiddenException>(() => service.ExportAsync(FullOptions()));

            foreach (var ex in new[] { import, peek, export })
            {
                ex.Code.Should().Be("PermissionDenied");
                ex.RequiredSpecifications.Should().BeEquivalentTo([38]);
            }

            log.Entries.Should().Contain(e => e.Level == "WARN");
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
            (await dbContext.Import.CountAsync(w => w.TenantRegistryId == tenant.Id)).Should().Be(0);
            (await dbContext.Export.CountAsync(w => w.TenantRegistryId == tenant.Id)).Should().Be(0);
            (await dbContext.ExportPeek.CountAsync(w => w.TenantRegistryId == tenant.Id)).Should().Be(0);
        }

        [Fact]
        public async Task ExportContainsOnlyCallersTenantAndPeekMatchesAsync()
        {
            var tenantA = await CreateTenantAsync("IsoA");
            var tenantB = await CreateTenantAsync("IsoB");
            var seededA = await SeedTenantAsync(tenantA, "IsoA");
            var seededB = await SeedTenantAsync(tenantB, "IsoB");

            var wrapper = Decrypt(await ExportAsync(tenantA, FullOptions()));
            var models = wrapper.Payload.Required().EntityAnalysisModel.Required().ToList();
            models.Should().ContainSingle();
            models[0].Name.Should().Be(seededA.ModelName);
            models[0].TenantRegistryId.Should().Be(tenantA.Id);
            models[0].EntityAnalysisModelRequestXpath.Should().ContainSingle(x => x.Name == seededA.XpathName);

            var wrapperB = Decrypt(await ExportAsync(tenantB, FullOptions()));
            wrapperB.Payload.Required().EntityAnalysisModel.Required().Select(m => m.Name).Should()
                .BeEquivalentTo(seededB.ModelName);

            await using var dbContext = fx.GetDbContext();
            var serviceA = await BuildServiceAsync(dbContext, tenantA.UserName);
            var yamlA = await serviceA.ExportPeekAsync(true, true, true, true, true);
            yamlA.Should().Contain(seededA.ModelName).And.Contain(seededA.ListName)
                .And.Contain(seededA.DictionaryName);
            yamlA.Should().NotContain(seededB.ModelName).And.NotContain(seededB.ListName)
                .And.NotContain(seededB.DictionaryName).And.NotContain(seededB.XpathName)
                .And.NotContain(seededB.KvpKey);

            var serviceB = await BuildServiceAsync(dbContext, tenantB.UserName);
            var yamlB = await serviceB.ExportPeekAsync(true, true, true, true, true);
            yamlB.Should().Contain(seededB.ModelName).And.NotContain(seededA.ModelName);
        }

        [Fact]
        public async Task ExportWithDifferentPasswordCannotBeDecryptedWithOthersAsync()
        {
            var tenant = await CreateTenantAsync("Pw");
            await SeedTenantAsync(tenant, "Pw");
            var bytes = await ExportAsync(tenant, FullOptions());

            Action wrong = () => Decrypt(bytes, "another password");
            wrong.Should().Throw<Exception>();
            Decrypt(bytes).Version.Should().Be(2);
        }

        [Fact]
        public async Task ExportReturnsJempFileNameFromExportGuidAndPersistsAuditRowAsync()
        {
            var tenant = await CreateTenantAsync("Fn");
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, tenant.UserName);

            var export = await service.ExportAsync(FullOptions());

            export.FileName.Should().Be($"{export.Guid}.jemp");
            var row = await dbContext.Export.SingleAsync(w => w.Guid == export.Guid);
            row.TenantRegistryId.Should().Be(tenant.Id);
            row.CreatedUser.Should().Be(tenant.UserName);
            row.EncryptedBytes.Should().Equal(export.EncryptedBytes);
        }

        [Fact]
        public async Task RoundTripIntoSecondTenantRemapsIdsTenantAndKeepsSourceUntouchedAsync()
        {
            var source = await CreateTenantAsync("RtS");
            var target = await CreateTenantAsync("RtT");
            var seeded = await SeedTenantAsync(source, "RtS");
            var targetOriginal = await SeedTenantAsync(target, "RtT");

            var bytes = await ExportAsync(source, FullOptions());
            var bus = new CapturingBus();
            var audit = new TestLog();
            await ImportAsync(target, bytes, bus: bus, auditLog: audit);

            await using var dbContext = fx.GetDbContext();

            var imported = await dbContext.EntityAnalysisModel
                .Where(w => w.TenantRegistryId == target.Id && w.Name == seeded.ModelName).ToListAsync();
            imported.Should().ContainSingle();
            var model = imported[0];
            model.Id.Should().NotBe(seeded.ModelId);
            model.Guid.Should().NotBe(seeded.ModelGuid, "the guid exists in another tenant so it is re-keyed");
            (model.Deleted ?? 0).Should().Be(0);
            model.ImportId.Should().NotBeNull();

            var xpaths = await dbContext.GetTable<Data.Poco.EntityAnalysisModelRequestXpath>()
                .Where(w => w.EntityAnalysisModelId == model.Id).ToListAsync();
            xpaths.Should().ContainSingle();
            xpaths[0].Name.Should().Be(seeded.XpathName);
            xpaths[0].XPath.Should().Be("$.a.b");
            xpaths[0].Guid.Should().NotBe(Guid.Empty);

            var list = await dbContext.GetTable<Data.Poco.EntityAnalysisModelList>()
                .SingleAsync(w => w.EntityAnalysisModelGuid == model.Guid);
            list.Name.Should().Be(seeded.ListName);
            (await dbContext.GetTable<Data.Poco.EntityAnalysisModelListValue>()
                    .Where(w => w.EntityAnalysisModelListId == list.Id).Select(s => s.ListValue).ToListAsync())
                .Should().BeEquivalentTo(seeded.ListValue);

            var dictionary = await dbContext.GetTable<Data.Poco.EntityAnalysisModelDictionary>()
                .SingleAsync(w => w.EntityAnalysisModelGuid == model.Guid);
            dictionary.Name.Should().Be(seeded.DictionaryName);
            var kvp = await dbContext.GetTable<Data.Poco.EntityAnalysisModelDictionaryKvp>()
                .SingleAsync(w => w.EntityAnalysisModelDictionaryId == dictionary.Id);
            kvp.KvpKey.Should().Be(seeded.KvpKey);
            kvp.KvpValue.Should().Be(seeded.KvpValue);

            var original = await dbContext.EntityAnalysisModel.SingleAsync(w => w.Id == targetOriginal.ModelId);
            original.Deleted.Should().Be(1);

            var sourceModel = await dbContext.EntityAnalysisModel.SingleAsync(w => w.Id == seeded.ModelId);
            sourceModel.TenantRegistryId.Should().Be(source.Id);
            sourceModel.Guid.Should().Be(seeded.ModelGuid);
            (sourceModel.Deleted ?? 0).Should().Be(0);
            (await dbContext.GetTable<Data.Poco.EntityAnalysisModelRequestXpath>()
                .CountAsync(w => w.EntityAnalysisModelId == seeded.ModelId)).Should().Be(1);
            (await dbContext.EntityAnalysisModel.CountAsync(w => w.TenantRegistryId == source.Id)).Should().Be(1);

            var importRow = await dbContext.Import.SingleAsync(w => w.TenantRegistryId == target.Id);
            importRow.InError.GetValueOrDefault().Should().Be(0);
            importRow.CompletedDate.Should().NotBeNull();
            importRow.CreatedUser.Should().Be(target.UserName);

            bus.Published.Select(e => e.Area).Should().BeEquivalentTo("Preservation", "EntityAnalysisModel",
                "RoleRegistry", "VisualisationRegistry", "CaseWorkflow");
            bus.Published.Should().OnlyContain(e => e.TenantRegistryId == target.Id
                                                    && e.Kind == ServiceChangeKind.Updated
                                                    && e.Actor == target.UserName && e.EntityId == null);
            audit.Entries.Count(e => e.Message.Contains("area=Preservation op=Import ")).Should().Be(1);
            audit.Entries.Single(e => e.Message.Contains("area=Preservation op=Import "))
                .Message.Should().Contain("outcome=ok");
        }

        [Fact]
        public async Task RolesImportedIntoOwnTenantKeepsRoleAndPermissionAsync()
        {
            var tenant = await CreateTenantAsync("RtRo");

            await ImportAsync(tenant, await ExportAsync(tenant, FullOptions(true)));

            await using var dbContext = fx.GetDbContext();
            var live = await dbContext.RoleRegistry
                .Where(w => w.TenantRegistryId == tenant.Id && (w.Deleted == 0 || w.Deleted == null))
                .ToListAsync();
            live.Should().Contain(r => r.Guid == tenant.RoleGuid && r.Name == tenant.RoleName);
            var role = live.Single(r => r.Guid == tenant.RoleGuid);
            (await dbContext.RoleRegistryPermission.Where(w => w.RoleRegistryId == role.Id).ToListAsync())
                .Should().ContainSingle(p => p.PermissionSpecificationId == 38);
            (await dbContext.RoleRegistry.CountAsync(w => w.Guid == tenant.RoleGuid && w.Deleted == 1))
                .Should().Be(1);
        }

        [Fact]
        public async Task RolesImportedFromAnotherTenantIsRefusedWhileTargetHasUsersAndRollsBackAsync()
        {
            var source = await CreateTenantAsync("RtRS");
            var target = await CreateTenantAsync("RtRT");
            var targetSeed = await SeedTenantAsync(target, "RtRT");
            var bytes = await ExportAsync(source, FullOptions(true));
            var bus = new CapturingBus();

            await Assert.ThrowsAsync<UserRegistryRecordsExistOnRoleRegistryRekeyImportException>(() =>
                ImportAsync(target, bytes, bus: bus));

            await AssertTargetUntouchedAsync(target, targetSeed);
            await using var dbContext = fx.GetDbContext();
            (await dbContext.RoleRegistry.CountAsync(w => w.TenantRegistryId == target.Id)).Should().Be(1);
            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task ImportRestoresGuidWhenSameTenantReimportsItsOwnExportAsync()
        {
            var tenant = await CreateTenantAsync("Self");
            var seeded = await SeedTenantAsync(tenant, "Self");

            await ImportAsync(tenant, await ExportAsync(tenant, FullOptions()));

            await using var dbContext = fx.GetDbContext();
            var live = await dbContext.EntityAnalysisModel
                .Where(w => w.TenantRegistryId == tenant.Id && (w.Deleted == 0 || w.Deleted == null)).ToListAsync();
            live.Should().ContainSingle();
            live[0].Name.Should().Be(seeded.ModelName);
            live[0].Id.Should().NotBe(seeded.ModelId);
        }

        [Fact]
        public async Task WrongPasswordThrowsInvalidFileLeavesDataAndMarksImportInErrorAsync()
        {
            var source = await CreateTenantAsync("WpS");
            var target = await CreateTenantAsync("WpT");
            await SeedTenantAsync(source, "WpS");
            var targetSeed = await SeedTenantAsync(target, "WpT");
            var bytes = await ExportAsync(source, FullOptions());
            var bus = new CapturingBus();
            var log = new TestLog();

            await Assert.ThrowsAsync<InvalidPreservationFileException>(() =>
                ImportAsync(target, bytes, "not the password", log, bus: bus));

            await AssertTargetUntouchedAsync(target, targetSeed);
            bus.Published.Should().BeEmpty();
            log.Entries.Should().Contain(e => e.Level == "WARN");
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
            await using var dbContext = fx.GetDbContext();
            (await dbContext.Import.SingleAsync(w => w.TenantRegistryId == target.Id)).InError.Should().Be(1);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public async Task CorruptOrEmptyFileFailsLeavesDataUntouchedAndPublishesNothingAsync(int kind)
        {
            var target = await CreateTenantAsync("Cf");
            var targetSeed = await SeedTenantAsync(target, "Cf");
            var bytes = kind == 0 ? [] : Enumerable.Range(0, 300).Select(i => (byte)(i * 7)).ToArray();
            var bus = new CapturingBus();

            var ex = await Record.ExceptionAsync(() => ImportAsync(target, bytes, bus: bus));

            ex.Should().NotBeNull();
            await AssertTargetUntouchedAsync(target, targetSeed);
            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task TamperedFileWithLegacyFallbackDisabledIsInvalidFileAsync()
        {
            var source = await CreateTenantAsync("TpS");
            var target = await CreateTenantAsync("TpT");
            await SeedTenantAsync(source, "TpS");
            var targetSeed = await SeedTenantAsync(target, "TpT");
            var bytes = await ExportAsync(source, FullOptions());
            bytes[^1] ^= 0xFF;

            await Assert.ThrowsAsync<InvalidPreservationFileException>(() => ImportAsync(target, bytes));

            await AssertTargetUntouchedAsync(target, targetSeed);
        }

        [Fact]
        public async Task NoFilesThrowsNoFileExceptionAsync()
        {
            var tenant = await CreateTenantAsync("Nf");
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, tenant.UserName);

            var ex = await Assert.ThrowsAsync<NoFileException>(() => service.ImportAsync([], Password));

            ex.Code.Should().Be("NoFileSupplied");
            (await dbContext.Import.CountAsync(w => w.TenantRegistryId == tenant.Id)).Should().Be(0);
        }

        [Fact]
        public async Task FailedImportMidTransactionRollsBackEverythingAsync()
        {
            var source = await CreateTenantAsync("RbS");
            var target = await CreateTenantAsync("RbT");
            var seededSource = await SeedTenantAsync(source, "RbS");
            var targetSeed = await SeedTenantAsync(target, "RbT");

            var exported = Decrypt(await ExportAsync(source, FullOptions()));
            var good = exported.Payload.Required().EntityAnalysisModel.Required().Single();
            var bad = new Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}RbBad{Guid.NewGuid():N}"[..30],
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0,
                EntityAnalysisModelRequestXpath = null
            };
            var crafted = new Wrapper
            {
                Version = 2,
                Guid = Guid.NewGuid(),
                Payload = new Payload
                {
                    EntityAnalysisModel = [good, bad],
                    EntityPermission = new EntityPermission()
                }
            };
            var bus = new CapturingBus();

            var ex = await Record.ExceptionAsync(() => ImportAsync(target, Encrypt(crafted), bus: bus));

            ex.Should().NotBeNull();
            await AssertTargetUntouchedAsync(target, targetSeed);
            await using var dbContext = fx.GetDbContext();
            (await dbContext.EntityAnalysisModel.CountAsync(w => w.TenantRegistryId == target.Id
                                                                 && w.Name == seededSource.ModelName)).Should().Be(0);
            (await dbContext.EntityAnalysisModel.CountAsync(w => w.TenantRegistryId == target.Id)).Should().Be(1);
            var importRow = await dbContext.Import.SingleAsync(w => w.TenantRegistryId == target.Id);
            importRow.InError.Should().Be(1);
            importRow.ErrorStack.Should().NotBeNullOrEmpty();
            bus.Published.Should().BeEmpty();
        }

        private async Task AssertTargetUntouchedAsync(Tenant target, Seeded targetSeed)
        {
            await using var dbContext = fx.GetDbContext();
            var live = await dbContext.EntityAnalysisModel
                .Where(w => w.TenantRegistryId == target.Id && (w.Deleted == 0 || w.Deleted == null))
                .ToListAsync();
            live.Should().ContainSingle();
            live[0].Id.Should().Be(targetSeed.ModelId);
            live[0].Guid.Should().Be(targetSeed.ModelGuid);
            (await dbContext.RoleRegistry.CountAsync(w => w.Guid == target.RoleGuid && (w.Deleted ?? 0) == 0))
                .Should().Be(1);
            (await dbContext.GetTable<Data.Poco.EntityAnalysisModelRequestXpath>()
                    .CountAsync(w => w.EntityAnalysisModelId == targetSeed.ModelId && w.DeletedDate == null))
                .Should().Be(1);
        }

        [Fact]
        public async Task CancelledTokenIsHonouredAndLeavesDataUntouchedAsync()
        {
            var tenant = await CreateTenantAsync("Cx");
            var seeded = await SeedTenantAsync(tenant, "Cx");
            var bytes = await ExportAsync(tenant, FullOptions());
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, tenant.UserName);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                service.ExportPeekAsync(true, true, true, true, true, cts.Token));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                service.ExportAsync(FullOptions(), cts.Token));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                ImportAsync(tenant, bytes, token: cts.Token));

            await using var check = fx.GetDbContext();
            (await check.EntityAnalysisModel.SingleAsync(w => w.Id == seeded.ModelId, token: CancellationToken.None))
                .Deleted
                .Should().Be(0);
        }

        [Fact]
        public async Task ExportAndPeekWriteExactlyOneAuditRecordEachAndPublishNothingAsync()
        {
            var tenant = await CreateTenantAsync("Au");
            var audit = new TestLog();
            var bus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, tenant.UserName, auditLog: audit, serviceChangeBus: bus);

            await service.ExportPeekAsync(false, false, false, false, false);
            await service.ExportAsync(FullOptions());

            audit.Entries.Count(e => e.Message.Contains("area=Preservation op=ExportPeek ")).Should().Be(1);
            audit.Entries.Count(e => e.Message.Contains("area=Preservation op=Export ")).Should().Be(1);
            audit.Entries.Should().OnlyContain(e => e.Message.Contains($"tenant={tenant.Id} outcome=ok"));
            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task ForbiddenOperationAuditsForbiddenOutcomeAsync()
        {
            var tenant = await CreateTenantAsync("Fo", false);
            var audit = new TestLog();
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, tenant.UserName, auditLog: audit);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.ExportAsync(FullOptions()));

            audit.Entries.Should().ContainSingle(e => e.Message.Contains("op=Export ")
                                                      && e.Message.Contains("outcome=forbidden"));
        }

        [Fact]
        public void ToolCatalogueDoesNotExposeWholeTenantExportOrImport()
        {
            ServiceToolCatalogue.All.Select(t => t.Name)
                .Should().NotContain(n => n.Contains("Preservation", StringComparison.OrdinalIgnoreCase));
        }
    }
}