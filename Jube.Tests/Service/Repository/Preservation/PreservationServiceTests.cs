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
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Accord.Neuro;
using FluentAssertions;
using Jube.Analytics.Categorical;
using Jube.Cryptography;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Data.Repository.Models;
using Jube.Dto.Repository.Preservation;
using Jube.Dto.Repository.PreservationSnapshot;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions;
using Jube.Engine.Exhaustive.Extensions;
using Jube.Engine.Helpers;
using Jube.Preservation.Exceptions;
using Jube.Preservation.Models;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Repository.Preservation;
using Jube.Service.Exceptions.Repository.PreservationSnapshot;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Reactivity.Models;
using Jube.Service.Repository.Preservation;
using Jube.Service.Repository.PreservationSnapshot;
using Jube.Test.Analytics.Categorical;
using Jube.Test.Infrastructure;
using Jube.Test.Preservation;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.Repository.Preservation.Models;
using LinqToDB;
using LinqToDB.Data;
using log4net;
using MessagePack;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Jube.Data.Query.GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery;

namespace Jube.Test.Service.Repository.Preservation
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class PreservationServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const string Password = "correct horse";

        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdTenantRegistryIds = [];
        private readonly List<string> createdUserNames = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

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

            var exhaustiveInstanceIds = await dbContext.GetTable<Data.Poco.ExhaustiveSearchInstance>()
                .Where(w => modelIdsNullable.Contains(w.EntityAnalysisModelId)).Select(s => s.Id).ToListAsync();

            var exhaustiveInstanceIdsNullable = exhaustiveInstanceIds.Select(id => (int?)id).ToList();

            var exhaustiveVariableIds = await dbContext.GetTable<ExhaustiveSearchInstanceVariable>()
                .Where(w => exhaustiveInstanceIdsNullable.Contains(w.ExhaustiveSearchInstanceId)).Select(s => s.Id)
                .ToListAsync();

            var exhaustiveVariableIdsNullable = exhaustiveVariableIds.Select(id => (int?)id).ToList();

            await dbContext.GetTable<ExhaustiveSearchInstanceVariableHistogramClass>()
                .Where(w => exhaustiveVariableIdsNullable.Contains(w.ExhaustiveSearchInstanceVariableId)).DeleteAsync();
            await dbContext.GetTable<ExhaustiveSearchInstanceVariableHistogram>()
                .Where(w => exhaustiveVariableIdsNullable.Contains(w.ExhaustiveSearchInstanceVariableId)).DeleteAsync();
            await dbContext.GetTable<ExhaustiveSearchInstanceVariable>()
                .Where(w => exhaustiveVariableIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<ExhaustiveSearchInstanceVersion>()
                .Where(w => exhaustiveInstanceIdsNullable.Contains(w.ExhaustiveSearchInstanceId)).DeleteAsync();

            await dbContext.GetTable<Data.Poco.ExhaustiveSearchInstance>()
                .Where(w => exhaustiveInstanceIds.Contains(w.Id)).DeleteAsync();

            await dbContext.GetTable<EntityAnalysisModelDictionaryKvpVersion>()
                .Where(w => dictionaryIdsNullable.Contains(w.EntityAnalysisModelDictionaryId)).DeleteAsync();

            await dbContext.GetTable<Data.Poco.EntityAnalysisModelDictionaryKvp>()
                .Where(w => dictionaryIdsNullable.Contains(w.EntityAnalysisModelDictionaryId)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelDictionary>()
                .Where(w => dictionaryIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelListValue>()
                .Where(w => listIdsNullable.Contains(w.EntityAnalysisModelListId)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelList>()
                .Where(w => listIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelOverride>()
                .Where(w => modelGuids.Contains(w.EntityAnalysisModelGuid)).DeleteAsync();

            await dbContext.GetTable<EntityAnalysisModelRequestXpathVersion>()
                .Where(w => modelIdsNullable.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.GetTable<EntityAnalysisModelRequestXpath>()
                .Where(w => modelIdsNullable.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.GetTable<EntityAnalysisModelVersion>()
                .Where(w => modelIdsNullable.Contains(w.EntityAnalysisModelId)).DeleteAsync();

            var instanceIds = await dbContext.ExhaustiveSearchInstance
                .Where(w => modelIdsNullable.Contains(w.EntityAnalysisModelId)).Select(s => s.Id).ToListAsync();

            await dbContext.GetTable<ExhaustiveSearchInstanceVersion>()
                .Where(w => instanceIds.Contains(w.ExhaustiveSearchInstanceId)).DeleteAsync();

            await dbContext.ExhaustiveSearchInstance.Where(w => instanceIds.Contains(w.Id)).DeleteAsync();

            var datasetIds = await dbContext.ExhaustiveInstanceCsv
                .Where(w => tenantIds.Contains(w.TenantRegistryId)).Select(s => s.Id).ToListAsync();

            await dbContext.ExhaustiveInstanceCsvRow
                .Where(w => datasetIds.Contains(w.DatasetId)).DeleteAsync();
            await dbContext.ExhaustiveInstanceCsv.Where(w => datasetIds.Contains(w.Id)).DeleteAsync();

            var jsonDatasetIds = await dbContext.ExhaustiveInstanceJson
                .Where(w => tenantIds.Contains(w.TenantRegistryId)).Select(s => s.Id).ToListAsync();

            await dbContext.ExhaustiveInstanceJsonRow
                .Where(w => jsonDatasetIds.Contains(w.DatasetId)).DeleteAsync();
            await dbContext.ExhaustiveInstanceJson.Where(w => jsonDatasetIds.Contains(w.Id)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => modelIds.Contains(w.Id)).DeleteAsync();

            var roleIds = await dbContext.RoleRegistry
                .Where(w => tenantIdsNullable.Contains(w.TenantRegistryId)).Select(s => s.Id).ToListAsync();

            var roleIdsNullable = roleIds.Select(id => (int?)id).ToList();

            var userIds = await dbContext.UserRegistry.Where(w => userNames.Contains(w.Name))
                .Select(s => s.Id).ToListAsync();

            await dbContext.GetTable<UserRegistryVersion>()
                .Where(w => userIds.Contains(w.UserRegistryId)).DeleteAsync();

            await dbContext.UserInTenant.Where(w => userNames.Contains(w.User)).DeleteAsync();
            await dbContext.UserRegistry.Where(w => userNames.Contains(w.Name)).DeleteAsync();
            await dbContext.RoleRegistryPermission
                .Where(w => roleIdsNullable.Contains(w.RoleRegistryId)).DeleteAsync();
            await dbContext.RoleRegistry.Where(w => roleIds.Contains(w.Id)).DeleteAsync();

            await dbContext.EntityApproval.Where(w => tenantIdsNullable.Contains(w.TenantRegistryId)).DeleteAsync();
            await dbContext.Import.Where(w => tenantIds.Contains(w.TenantRegistryId)).DeleteAsync();
            await dbContext.Export.Where(w => tenantIds.Contains(w.TenantRegistryId)).DeleteAsync();
            await dbContext.PreservationDiff.Where(w => tenantIds.Contains(w.TenantRegistryId)).DeleteAsync();
            await dbContext.TenantRegistry.Where(w => tenantIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<PreservationService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return PreservationService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
        }

        private async Task GrantPermissionAsync(Tenant tenant, int permissionSpecificationId)
        {
            await using var dbContext = fx.GetDbContext();

            var roleRegistryId = await dbContext.RoleRegistry
                .Where(w => w.Guid == tenant.RoleGuid)
                .Select(s => s.Id).FirstOrDefaultAsync();

            await dbContext.InsertAsync(new Data.Poco.RoleRegistryPermission
            {
                RoleRegistryId = roleRegistryId,
                PermissionSpecificationId = permissionSpecificationId,
                Active = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = tenant.UserName,
                Version = 1,
                Guid = Guid.NewGuid()
            });
        }

        private static Task<PreservationSnapshotService> BuildSnapshotServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return PreservationSnapshotService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
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

            await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModelRequestXpath
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
            ILog? auditLog = null, IServiceChangeBus? bus = null,
            CancellationToken token = default)
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, tenant.UserName, log, auditLog, bus);
            await using var stream = new MemoryStream(bytes);

            await service.ImportAsync([new PreservationImportFileDto { FileName = "x.jemp", Content = stream }],
                password, token);
        }

        private static Wrapper Decrypt(byte[] encrypted, string password = Password)
        {
            var bytes = new JempAesEncryption(password).Decrypt(encrypted);

            return MessagePackSerializer.Deserialize<Wrapper>(bytes,
                MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray));
        }

        private static byte[] Encrypt(Wrapper wrapper)
        {
            var bytes = MessagePackSerializer.Serialize(wrapper,
                MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray));

            return new JempAesEncryption(Password).Encrypt(bytes);
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

            var peek = await Assert.ThrowsAsync<ForbiddenException>(() => service.SummaryAsync());

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
            (await dbContext.PreservationDiff.CountAsync(w => w.TenantRegistryId == tenant.Id)).Should().Be(0);
        }

        [Fact]
        public async Task ExportContainsOnlyCallersTenantAndTheLivePayloadMatchesAsync()
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
            var peekA = await PreservationHarness.LiveJsonAsync(dbContext, tenantA.UserName);

            peekA.Should().Contain(seededA.ModelName).And.Contain(seededA.ListName)
                .And.Contain(seededA.DictionaryName);

            peekA.Should().NotContain(seededB.ModelName).And.NotContain(seededB.ListName)
                .And.NotContain(seededB.DictionaryName).And.NotContain(seededB.XpathName)
                .And.NotContain(seededB.KvpKey);

            var peekB = await PreservationHarness.LiveJsonAsync(dbContext, tenantB.UserName);
            peekB.Should().Contain(seededB.ModelName).And.NotContain(seededA.ModelName);
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

            var xpaths = await dbContext.GetTable<EntityAnalysisModelRequestXpath>()
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

            (await dbContext.GetTable<EntityAnalysisModelRequestXpath>()
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
        public async Task ExhaustiveClassHistogramsAreExportedAndRoundTripIntoASecondTenantAsync()
        {
            var source = await CreateTenantAsync("ChS");
            var target = await CreateTenantAsync("ChT");
            var seeded = await SeedTenantAsync(source, "ChS");

            int variableId;

            await using (var seedContext = fx.GetDbContext())
            {
                var instance =
                    await new ExhaustiveSearchInstanceRepository(seedContext, source.UserName)
                        .InsertAsync(
                            new Data.Poco.ExhaustiveSearchInstance
                            {
                                Name = $"{DatabaseFixture.Prefix}ChI{Guid.NewGuid():N}"[..40],
                                Guid = Guid.NewGuid(),
                                EntityAnalysisModelId = seeded.ModelId,
                                Active = 1,
                                Locked = 0,
                                Deleted = 0,
                                StatusId = 0
                            });

                variableId = await seedContext.InsertWithInt32IdentityAsync(
                    new ExhaustiveSearchInstanceVariable
                    {
                        ExhaustiveSearchInstanceId = instance.Id, Name = "ChVar", Mean = 1
                    });

                foreach (var label in new[] { 0, 1 })
                {
                    for (var bin = 0; bin < 3; bin++)
                    {
                        await seedContext.InsertWithInt32IdentityAsync(
                            new ExhaustiveSearchInstanceVariableHistogramClass
                            {
                                ExhaustiveSearchInstanceVariableId = variableId, ClassLabel = label,
                                BinSequence = bin, BinRangeStart = bin, BinRangeEnd = bin + 1,
                                Frequency = label * 100 + bin, Deleted = 0
                            });
                    }
                }
            }

            var options = FullOptions();
            options.Exhaustive = true;
            var bytes = await ExportAsync(source, options);

            var payload = MessagePackSerializer.Deserialize<Wrapper>(
                new JempAesEncryption(Password).Decrypt(bytes),
                MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray));

            var exportedVariable = payload.Payload!.EntityAnalysisModel!.Single(m => m.Guid == seeded.ModelGuid)
                .ExhaustiveSearchInstance!.Single().ExhaustiveSearchInstanceVariable!.Single();

            exportedVariable.ExhaustiveSearchInstanceVariableHistogramClass!.Select(c => (c.ClassLabel, c.Frequency))
                .Should().Equal((0, 0), (0, 1), (0, 2), (1, 100), (1, 101), (1, 102));

            await ImportAsync(target, bytes);

            await using var dbContext = fx.GetDbContext();

            var importedModel = await dbContext.EntityAnalysisModel
                .SingleAsync(w => w.TenantRegistryId == target.Id && w.Name == seeded.ModelName);

            var importedVariable = await dbContext.GetTable<ExhaustiveSearchInstanceVariable>()
                .SingleAsync(w => w.Name == "ChVar" && w.ExhaustiveSearchInstance!.EntityAnalysisModelId ==
                    importedModel.Id);

            importedVariable.Id.Should().NotBe(variableId);

            var rows = await dbContext.GetTable<ExhaustiveSearchInstanceVariableHistogramClass>()
                .Where(w => w.ExhaustiveSearchInstanceVariableId == importedVariable.Id).OrderBy(o => o.Id)
                .ToListAsync();

            rows.Select(r => (r.ClassLabel, r.BinSequence, r.Frequency)).Should()
                .Equal((0, 0, 0), (0, 1, 1), (0, 2, 2), (1, 0, 100), (1, 1, 101), (1, 2, 102));

            rows.Should().OnlyContain(r => r.ImportId != null && (r.Deleted ?? 0) == 0);

            (await dbContext.GetTable<ExhaustiveSearchInstanceVariableHistogramClass>()
                .CountAsync(w => w.ExhaustiveSearchInstanceVariableId == variableId)).Should().Be(6);
        }

        [Fact]
        public async Task ExhaustiveUploadedDatasetsRoundTripWithRowsAndRemappedInstanceReferenceAsync()
        {
            var source = await CreateTenantAsync("DsS");
            var target = await CreateTenantAsync("DsT");
            var seeded = await SeedTenantAsync(source, "DsS");
            var targetSeeded = await SeedTenantAsync(target, "DsT");

            await using var dbContext = fx.GetDbContext();
            var sourceDatasetRepository = new ExhaustiveInstanceDatasetRepository(dbContext, source.Id);

            var sourceDataset = await sourceDatasetRepository.InsertAsync(new ExhaustiveInstanceCsv
            {
                Name = $"{DatabaseFixture.Prefix}PrDataset{Guid.NewGuid():N}"[..40],
                FileName = "x.csv",
                ColumnsJson = "[]",
#pragma warning disable CS0618
                DependentColumnIndex = 2,
#pragma warning restore CS0618
                RowCount = 30,
                StatusId = 1
            });

            var sourceGuids = Enumerable.Range(0, 30).Select(_ => Guid.NewGuid()).ToList();

            await sourceDatasetRepository.InsertRowsAsync(DatasetKind.Csv, sourceGuids.Select((g, i) =>
                new ExhaustiveInstanceCsvRow
                {
                    DatasetId = sourceDataset.Id,
                    EntityAnalysisModelInstanceEntryGuid = g,
                    Json = $"{{\"values\":{{\"c0\":{i},\"c1\":{i * 2},\"c2\":{(i % 3 == 0 ? 1 : 0)}}}}}"
                }));

            var sourceInstance = await new ExhaustiveSearchInstanceRepository(dbContext, source.UserName).InsertAsync(
                new Data.Poco.ExhaustiveSearchInstance
                {
                    Name = $"{DatabaseFixture.Prefix}PrInst{Guid.NewGuid():N}"[..40],
                    Guid = Guid.NewGuid(),
                    EntityAnalysisModelId = seeded.ModelId,
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                    StatusId = 16,
                    DataSourceId = 2,
                    ExhaustiveInstanceCsvId = sourceDataset.Id
                });

            var targetDataset = await new ExhaustiveInstanceDatasetRepository(dbContext, target.Id).InsertAsync(
                new ExhaustiveInstanceCsv
                {
                    Name = $"{DatabaseFixture.Prefix}PrOld{Guid.NewGuid():N}"[..40], ColumnsJson = "[]", StatusId = 1
                });

            await new ExhaustiveInstanceDatasetRepository(dbContext, target.Id).InsertRowsAsync(DatasetKind.Csv,
            [
                new ExhaustiveInstanceCsvRow
                {
                    DatasetId = targetDataset.Id,
                    EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(), Json = "{}"
                }
            ]);

            var options = FullOptions();
            options.Exhaustive = true;
            var bytes = await ExportAsync(source, options);
            var wrapper = Decrypt(bytes);

            wrapper.Payload!.ExhaustiveInstanceCsv.Should().ContainSingle()
                .Which.Rows.Should().HaveCount(30);

            await ImportAsync(target, bytes);

            var imported = await dbContext.ExhaustiveInstanceCsv
                .Where(w => w.TenantRegistryId == target.Id && w.Name == sourceDataset.Name && w.Deleted != 1)
                .ToListAsync();
            imported.Should().ContainSingle();
            var dataset = imported[0];
            dataset.Id.Should().NotBe(sourceDataset.Id);
            dataset.TenantRegistryId.Should().Be(target.Id);
            dataset.ImportId.Should().NotBeNull();
            dataset.Guid.Should().Be(sourceDataset.Guid);
#pragma warning disable CS0618
            dataset.DependentColumnIndex.Should().Be(2);
#pragma warning restore CS0618
            dataset.StatusId.Should().Be(1);

            var rows = await dbContext.ExhaustiveInstanceCsvRow
                .Where(w => w.DatasetId == dataset.Id).ToListAsync();
            rows.Should().HaveCount(30);
            rows.Select(r => r.EntityAnalysisModelInstanceEntryGuid).Should().BeEquivalentTo(sourceGuids);

            var importedInstance = await dbContext.ExhaustiveSearchInstance
                .Where(w => w.EntityAnalysisModel.TenantRegistryId == target.Id && w.Name == sourceInstance.Name
                    && w.Deleted != 1)
                .SingleAsync();
            importedInstance.DataSourceId.Should().Be(2);
            importedInstance.ExhaustiveInstanceCsvId.Should().Be(dataset.Id);

            (await dbContext.ExhaustiveInstanceCsv.SingleAsync(w => w.Id == targetDataset.Id)).Deleted
                .Should().Be(1);

            (await dbContext.ExhaustiveInstanceCsvRow.CountAsync(w =>
                w.DatasetId == targetDataset.Id)).Should().Be(0);
            (await dbContext.ExhaustiveInstanceCsvRow.CountAsync(w =>
                w.DatasetId == sourceDataset.Id)).Should().Be(30);

            (await dbContext.ExhaustiveInstanceCsv.SingleAsync(w => w.Id == sourceDataset.Id)).TenantRegistryId
                .Should().Be(source.Id);

            targetSeeded.ModelId.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task ExhaustiveUploadedJsonDatasetsRoundTripWithRowsAndRemappedInstanceReferenceAsync()
        {
            var source = await CreateTenantAsync("JsS");
            var target = await CreateTenantAsync("JsT");
            var seeded = await SeedTenantAsync(source, "JsS");
            await SeedTenantAsync(target, "JsT");

            await using var dbContext = fx.GetDbContext();
            var sourceRepository = new ExhaustiveInstanceDatasetRepository(dbContext, source.Id);

            var sourceDataset = await sourceRepository.InsertAsync(new ExhaustiveInstanceJson
            {
                Name = $"{DatabaseFixture.Prefix}PrJson{Guid.NewGuid():N}"[..40],
                SourceLabel = "label", ContentHash = "ab12", ColumnsJson = "[]",
#pragma warning disable CS0618
                DependentColumnIndex = 2,
#pragma warning restore CS0618
                RowCount = 30, StatusId = 1
            });

            var pending = await sourceRepository.InsertAsync(new ExhaustiveInstanceJson
            {
                Name = $"{DatabaseFixture.Prefix}PrJsonP{Guid.NewGuid():N}"[..40], ColumnsJson = "[]", StatusId = 0
            });

            var sourceGuids = Enumerable.Range(0, 30).Select(_ => Guid.NewGuid()).ToList();

            await sourceRepository.InsertRowsAsync(DatasetKind.Json, sourceGuids.Select((g, i) =>
                new ExhaustiveInstanceJsonRow
                {
                    DatasetId = sourceDataset.Id, EntityAnalysisModelInstanceEntryGuid = g,
                    Json = $"{{\"values\":{{\"c0\":{i},\"c1\":{i * 2},\"c2\":{(i % 3 == 0 ? 1 : 0)}}}}}"
                }));

            var sourceInstance = await new ExhaustiveSearchInstanceRepository(dbContext, source.UserName).InsertAsync(
                new Data.Poco.ExhaustiveSearchInstance
                {
                    Name = $"{DatabaseFixture.Prefix}PrJInst{Guid.NewGuid():N}"[..40], Guid = Guid.NewGuid(),
                    EntityAnalysisModelId = seeded.ModelId, Active = 1, Locked = 0, Deleted = 0, StatusId = 16,
                    DataSourceId = 2, ExhaustiveInstanceJsonId = sourceDataset.Id
                });

            var options = FullOptions();
            options.Exhaustive = true;
            var bytes = await ExportAsync(source, options);
            var wrapper = Decrypt(bytes);

            wrapper.Payload!.ExhaustiveInstanceJson.Should().ContainSingle()
                .Which.Rows.Should().HaveCount(30);

            wrapper.Payload.ExhaustiveInstanceJson!.Should().NotContain(e => e.Dataset!.Id == pending.Id);

            await ImportAsync(target, bytes);

            var imported = await dbContext.ExhaustiveInstanceJson
                .Where(w => w.TenantRegistryId == target.Id && w.Name == sourceDataset.Name && w.Deleted != 1)
                .SingleAsync();

            imported.Id.Should().NotBe(sourceDataset.Id);
            imported.TenantRegistryId.Should().Be(target.Id);
            imported.ImportId.Should().NotBeNull();
            imported.SourceLabel.Should().Be("label");
            imported.ContentHash.Should().Be("ab12");
#pragma warning disable CS0618
            imported.DependentColumnIndex.Should().Be(2);
#pragma warning restore CS0618
            imported.StatusId.Should().Be(1);

            (await dbContext.ExhaustiveInstanceJsonRow.Where(w => w.DatasetId == imported.Id).ToListAsync())
                .Select(r => r.EntityAnalysisModelInstanceEntryGuid).Should().BeEquivalentTo(sourceGuids);

            (await dbContext.ExhaustiveInstanceJson.CountAsync(w => w.TenantRegistryId == target.Id
                                                                    && w.Name == pending.Name)).Should().Be(0);

            var importedInstance = await dbContext.ExhaustiveSearchInstance
                .Where(w => w.EntityAnalysisModel.TenantRegistryId == target.Id && w.Name == sourceInstance.Name
                    && w.Deleted != 1)
                .SingleAsync();
            importedInstance.DataSourceId.Should().Be(2);
            importedInstance.ExhaustiveInstanceJsonId.Should().Be(imported.Id);
            importedInstance.ExhaustiveInstanceCsvId.Should().BeNull();

            (await dbContext.ExhaustiveInstanceJson.SingleAsync(w => w.Id == sourceDataset.Id)).TenantRegistryId
                .Should().Be(source.Id);
        }

        [Fact]
        public async Task ExhaustiveInstanceWhoseDatasetIsAbsentFromTheFileFallsBackToArchiveAsync()
        {
            var source = await CreateTenantAsync("JsF");
            var target = await CreateTenantAsync("JsG");
            var seeded = await SeedTenantAsync(source, "JsF");
            await SeedTenantAsync(target, "JsG");

            await using var dbContext = fx.GetDbContext();
            var repository = new ExhaustiveInstanceDatasetRepository(dbContext, source.Id);

            var pendingJson = await repository.InsertAsync(new ExhaustiveInstanceJson
            {
                Name = $"{DatabaseFixture.Prefix}PrFbJ{Guid.NewGuid():N}"[..40], ColumnsJson = "[]", StatusId = 0
            });
            var pendingCsv = await repository.InsertAsync(new ExhaustiveInstanceCsv
            {
                Name = $"{DatabaseFixture.Prefix}PrFbC{Guid.NewGuid():N}"[..40], ColumnsJson = "[]", StatusId = 0
            });

            foreach (var (label, json, csv) in new[] { ("J", pendingJson.Id, (int?)null), ("C", 0, pendingCsv.Id) })
            {
                await new ExhaustiveSearchInstanceRepository(dbContext, source.UserName).InsertAsync(
                    new Data.Poco.ExhaustiveSearchInstance
                    {
                        Name = $"{DatabaseFixture.Prefix}PrFb{label}{Guid.NewGuid():N}"[..40], Guid = Guid.NewGuid(),
                        EntityAnalysisModelId = seeded.ModelId, Active = 1, Locked = 0, Deleted = 0, StatusId = 16,
                        DataSourceId = 2, ExhaustiveInstanceJsonId = csv.HasValue ? null : json,
                        ExhaustiveInstanceCsvId = csv
                    });
            }

            var options = FullOptions();
            options.Exhaustive = true;
            await ImportAsync(target, await ExportAsync(source, options));

            var importedInstances = await dbContext.ExhaustiveSearchInstance
                .Where(w => w.EntityAnalysisModel.TenantRegistryId == target.Id && w.Name.Contains("PrFb")
                    && w.Deleted != 1).ToListAsync();
            importedInstances.Should().HaveCount(2);

            importedInstances.Should().OnlyContain(i => i.DataSourceId == 0 && i.ExhaustiveInstanceCsvId == null
                                                                            && i.ExhaustiveInstanceJsonId == null);
        }

        [Fact]
        public async Task ExhaustiveJsonDatasetExportKeepsOnlyTheRowsTrainingCanReadAsync()
        {
            var tenant = await CreateTenantAsync("JsC");
            await using var dbContext = fx.GetDbContext();
            var repository = new ExhaustiveInstanceDatasetRepository(dbContext, tenant.Id);

            var dataset = await repository.InsertAsync(new ExhaustiveInstanceJson
            {
                Name = $"{DatabaseFixture.Prefix}PrJCap{Guid.NewGuid():N}"[..40], ColumnsJson = "[]", StatusId = 1,
#pragma warning disable CS0618
                DependentColumnIndex = 0
#pragma warning restore CS0618
            });

            await repository.InsertRowsAsync(DatasetKind.Json, Enumerable.Range(0, 40).Select(i =>
                new ExhaustiveInstanceJsonRow
                {
                    DatasetId = dataset.Id, EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                    Json = $"{{\"values\":{{\"c0\":{(i % 4 == 0 ? 1 : 0)}}}}}"
                }));

            var exported = await repository.GetForExportAsync(DatasetKind.Json, 5);

            var rows = exported.Should().ContainSingle(e => e.Dataset.Id == dataset.Id).Subject.Rows;
            rows.Should().HaveCount(10);
            (await repository.GetForExportAsync(DatasetKind.Csv, 5)).Should().BeEmpty();
        }

        [Fact]
        public void PreservationCoversEveryExhaustiveInstanceTable()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Jube.sln")))
            {
                directory = directory.Parent;
            }

            var pocos = string.Concat(Directory
                .EnumerateFiles(Path.Combine(directory!.FullName, "Jube.Data/Poco"), "*.cs")
                .Select(File.ReadAllText));

            var preservation = File.ReadAllText(Path.Combine(directory.FullName, "Jube.Preservation/Preservation.cs"))
                               + File.ReadAllText(Path.Combine(directory.FullName,
                                   "Jube.Preservation/Models/Payload.cs"));

            var tables = Regex
                .Matches(pocos, @"public class (ExhaustiveInstance[A-Za-z]+)\b")
                .Select(m => m.Groups[1].Value).Where(n => !n.EndsWith("Row")).ToList();

            tables.Should().Contain(["ExhaustiveInstanceCsv", "ExhaustiveInstanceJson"]);

            foreach (var table in tables)
            {
                preservation.Should().Contain(table, $"{table} must be exported and imported by Preservation");
            }
        }

        [Fact]
        public async Task ExhaustiveDatasetExportKeepsOnlyTheRowsTrainingCanReadAsync()
        {
            var tenant = await CreateTenantAsync("DsC");
            await using var dbContext = fx.GetDbContext();
            var repository = new ExhaustiveInstanceDatasetRepository(dbContext, tenant.Id);

            var dataset = await repository.InsertAsync(new ExhaustiveInstanceCsv
            {
                Name = $"{DatabaseFixture.Prefix}PrCap{Guid.NewGuid():N}"[..40], ColumnsJson = "[]", StatusId = 1,
#pragma warning disable CS0618
                DependentColumnIndex = 0
#pragma warning restore CS0618
            });

            await repository.InsertRowsAsync(DatasetKind.Csv, Enumerable.Range(0, 40).Select(i =>
                new ExhaustiveInstanceCsvRow
                {
                    DatasetId = dataset.Id,
                    EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                    Json = $"{{\"values\":{{\"c0\":{(i % 4 == 0 ? 1 : 0)}}}}}"
                }));

            var pending = await repository.InsertAsync(new ExhaustiveInstanceCsv
            {
                Name = $"{DatabaseFixture.Prefix}PrPend{Guid.NewGuid():N}"[..40], ColumnsJson = "[]", StatusId = 0
            });

            var exported = await repository.GetForExportAsync(DatasetKind.Csv, 5);

            exported.Should().ContainSingle(e => e.Dataset.Id == dataset.Id);
            exported.Should().NotContain(e => e.Dataset.Id == pending.Id);
            var rows = exported.Single(e => e.Dataset.Id == dataset.Id).Rows;

            rows.Should().HaveCount(10, "the export is a capped sample of size 2 x maxRowsPerClass, " +
                                        "not a class-balanced split");

            (await new ExhaustiveInstanceDatasetRepository(dbContext, 0).GetForExportAsync(DatasetKind.Csv)).Should()
                .BeEmpty();
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
        public async Task TamperedFileIsInvalidFileAsync()
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

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("elevenchars")]
        public async Task ExportWithAPasswordShorterThanTheMinimumIsRefusedAndRecordsNothingAsync(string? password)
        {
            var tenant = await CreateTenantAsync("WkP");
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, tenant.UserName);
            var before = await dbContext.Export.CountAsync(w => w.TenantRegistryId == tenant.Id);
            var options = FullOptions();
            options.Password = password;

            var ex = await Assert.ThrowsAsync<WeakPasswordException>(() => service.ExportAsync(options));

            ex.Code.Should().Be("WeakPassword");
            ex.Message.Should().Contain("12");
            (await dbContext.Export.CountAsync(w => w.TenantRegistryId == tenant.Id)).Should().Be(before);
        }

        [Fact]
        public async Task ImportOfAFileFromAnEarlierReleaseIsAnInvalidFileAsync()
        {
            var target = await CreateTenantAsync("ErT");
            var targetSeed = await SeedTenantAsync(target, "ErT");

            await Assert.ThrowsAsync<InvalidPreservationFileException>(() =>
                ImportAsync(target, System.Security.Cryptography.RandomNumberGenerator.GetBytes(4096)));

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

            (await dbContext.GetTable<EntityAnalysisModelRequestXpath>()
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
                service.SummaryAsync(cts.Token));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                service.ExportAsync(FullOptions(), cts.Token));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                ImportAsync(tenant, bytes, token: cts.Token));

            await using var check = fx.GetDbContext();

            (await check.EntityAnalysisModel.SingleAsync(w => w.Id == seeded.ModelId, CancellationToken.None))
                .Deleted
                .Should().Be(0);
        }

        [Fact]
        public async Task ExportAndSummaryWriteExactlyOneAuditRecordEachAndPublishNothingAsync()
        {
            var tenant = await CreateTenantAsync("Au");
            var audit = new TestLog();
            var bus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, tenant.UserName, auditLog: audit, serviceChangeBus: bus);

            await service.SummaryAsync();
            await service.ExportAsync(FullOptions());

            audit.Entries.Count(e => e.Message.Contains("area=Preservation op=Summary ")).Should().Be(1);
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

        private async Task<int> SeedCategoricalInstanceAsync(int modelId,
            CategoricalFieldEncoder encoder,
            string userName)
        {
            await using var dbContext = fx.GetDbContext();

            var instanceId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.ExhaustiveSearchInstance
            {
                Guid = Guid.NewGuid(),
                Name = $"{DatabaseFixture.Prefix}CatX{Guid.NewGuid():N}"[..40],
                EntityAnalysisModelId = modelId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                StatusId = 16,
                Anomaly = 0,
                Filter = 0,
                CategoricalOptionsJson = "{\"enabled\":true}",
                BaseRate = 0.031,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = userName,
                Version = 1
            });

            var fieldId = await dbContext.InsertWithInt32IdentityAsync(
                new ExhaustiveSearchInstanceCategoricalField
                {
                    ExhaustiveSearchInstanceId = instanceId,
                    SourceName = encoder.SourceName,
                    ProcessingTypeId = 1,
                    VocabularyJson = encoder.ToJson(),
                    CreatedDate = DateTime.UtcNow,
                    Deleted = 0
                });

            var trialInstanceId = await dbContext.InsertWithInt32IdentityAsync(
                new ExhaustiveSearchInstanceTrialInstance
                {
                    ExhaustiveSearchInstanceId = instanceId,
                    CreatedDate = DateTime.UtcNow
                });

            var sequence = 0;

            var numericId = await dbContext.InsertWithInt32IdentityAsync(new ExhaustiveSearchInstanceVariable
            {
                ExhaustiveSearchInstanceId = instanceId,
                VariableSequence = sequence,
                Name = "Abstraction.A",
                ProcessingTypeId = 5,
                Mean = 4,
                StandardDeviation = 2,
                Minimum = 0,
                Maximum = 9,
                NormalisationTypeId = 2,
                Deleted = 0
            });

            await dbContext.InsertWithInt32IdentityAsync(new ExhaustiveSearchInstanceTrialInstanceVariable
            {
                ExhaustiveSearchInstanceVariableId = numericId,
                ExhaustiveSearchInstanceTrialInstanceId = trialInstanceId,
                VariableSequence = sequence,
                Removed = 0
            });

            for (var i = 0; i < encoder.Indicators.Count; i++)
            {
                sequence++;

                var variableId = await dbContext.InsertWithInt32IdentityAsync(
                    new ExhaustiveSearchInstanceVariable
                    {
                        ExhaustiveSearchInstanceId = instanceId,
                        VariableSequence = sequence,
                        Name = encoder.Indicators[i].Name,
                        ProcessingTypeId = 1,
                        Mean = 0.1,
                        StandardDeviation = 0.3,
                        Minimum = 0,
                        Maximum = 1,
                        NormalisationTypeId = 1,
                        CategoricalFieldId = fieldId,
                        CategoricalKind = (byte)encoder.Indicators[i].Kind,
                        CategoricalLevel = encoder.Indicators[i].Label,
                        CategoricalIndicatorIndex = i,
                        Deleted = 0
                    });

                await dbContext.InsertWithInt32IdentityAsync(new ExhaustiveSearchInstanceTrialInstanceVariable
                {
                    ExhaustiveSearchInstanceVariableId = variableId,
                    ExhaustiveSearchInstanceTrialInstanceId = trialInstanceId,
                    VariableSequence = sequence,
                    Removed = 0
                });
            }

            await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.ExhaustiveSearchInstancePromotedTrialInstance
            {
                ExhaustiveSearchInstanceTrialInstanceId = trialInstanceId,
                Json = "{}",
                Active = 1,
                CreatedDate = DateTime.UtcNow,
                CalibrationMethodId = ExhaustiveCalibrationMethod.Platt,
                CalibrationInputId = ExhaustiveCalibrationInput.Logit,
                CalibrationSlope = 1.9138919318703835,
                CalibrationIntercept = 0.03593553619719747,
                CalibrationEpsilon = 1e-6,
                TrainingPrior = 0.5,
                CalibrationPrior = 0.535,
                BaseRate = 0.031,
                BaseRateSourceId = ExhaustiveBaseRateSource.Override,
                CalibrationSampleSize = 400,
                CalibrationStatusId = ExhaustiveCalibrationStatus.Calibrated,
                CalibrationReason = null,
                CalibrationQualityJson = "{\"Test\":{\"Rows\":400}}"
            });

            return instanceId;
        }

        private async Task<List<double[]>> CategoricalRecallInputsAsync(int instanceId, IEnumerable<string?> strings)
        {
            await using var dbContext = fx.GetDbContext();

            var rows = await new GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery(dbContext)
                .ExecuteByExhaustiveSearchInstanceIdAsync(instanceId);

            var helper = new JsonSerializationHelper();
            var network = new ActivationNetwork(new SigmoidFunction(), 1, null, 1);

            var model = SyncExhaustiveSearchInstancesExtensions.BuildExhaustiveModel(
                new Data.Poco.ExhaustiveSearchInstance { Id = instanceId, Guid = Guid.NewGuid(), Name = "Rt" },
                new Data.Poco.ExhaustiveSearchInstancePromotedTrialInstance
                {
                    Json = JsonConvert.SerializeObject(network,
                        helper.TopologyNetworkJsonSerializerSettings)
                }, rows, helper.DeserializeTopologyNetworkJsonSerializerSettings);

            return
            [
                .. strings.Select(s => ExhaustiveExtensions.BuildScoreInputs(
                    model.NetworkVariablesInOrder,
                    new JObject
                    {
                        ["Abstraction.A"] = 6, ["Payload.Country"] = s
                    }))
            ];
        }

        [Fact]
        public async Task RoundTripKeepsTheCategoricalVocabularyAndTheRecallInputsAreIdenticalAsync()
        {
            var source = await CreateTenantAsync("CatS");
            var target = await CreateTenantAsync("CatT");
            var seeded = await SeedTenantAsync(source, "CatS");
            var encoder = CategoricalTestData.Country();
            var sourceInstanceId = await SeedCategoricalInstanceAsync(seeded.ModelId, encoder, source.UserName);
            var strings = new[] { "GB", "us", " NG ", "FR", "XX", "unseen", "", null, "\uFF35\uFF33" };
            var before = await CategoricalRecallInputsAsync(sourceInstanceId, strings);

            try
            {
                var options = FullOptions();
                options.Exhaustive = true;
                var bytes = await ExportAsync(source, options);
                var wrapper = Decrypt(bytes);

                wrapper.Payload.Required().EntityAnalysisModel.Required().Single().ExhaustiveSearchInstance.Required()
                    .Single().ExhaustiveSearchInstanceCategoricalField.Required().Single().VocabularyJson
                    .Should().Be(encoder.ToJson());

                await ImportAsync(target, bytes);

                await using var dbContext = fx.GetDbContext();

                var importedModel = await dbContext.EntityAnalysisModel
                    .SingleAsync(w => w.TenantRegistryId == target.Id && w.Name == seeded.ModelName);

                var imported = await dbContext.ExhaustiveSearchInstance
                    .SingleAsync(w => w.EntityAnalysisModelId == importedModel.Id);

                imported.Id.Should().NotBe(sourceInstanceId);
                imported.CategoricalOptionsJson.Should().Be("{\"enabled\":true}");
                imported.BaseRate.Should().Be(0.031);

                var promotedTrial = await dbContext.ExhaustiveSearchInstancePromotedTrialInstance
                    .SingleAsync(w =>
                        w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstanceId == imported.Id);
                promotedTrial.CalibrationMethodId.Should().Be(ExhaustiveCalibrationMethod.Platt);
                promotedTrial.CalibrationInputId.Should().Be(ExhaustiveCalibrationInput.Logit);
                promotedTrial.CalibrationSlope.Should().Be(1.9138919318703835);
                promotedTrial.CalibrationIntercept.Should().Be(0.03593553619719747);
                promotedTrial.CalibrationEpsilon.Should().Be(1e-6);
                promotedTrial.TrainingPrior.Should().Be(0.5);
                promotedTrial.CalibrationPrior.Should().Be(0.535);
                promotedTrial.BaseRate.Should().Be(0.031);
                promotedTrial.BaseRateSourceId.Should().Be(ExhaustiveBaseRateSource.Override);
                promotedTrial.CalibrationSampleSize.Should().Be(400);
                promotedTrial.CalibrationStatusId.Should().Be(ExhaustiveCalibrationStatus.Calibrated);
                promotedTrial.CalibrationQualityJson.Should().Be("{\"Test\":{\"Rows\":400}}");

                var field = await dbContext.ExhaustiveSearchInstanceCategoricalField
                    .SingleAsync(w => w.ExhaustiveSearchInstanceId == imported.Id);

                field.VocabularyJson.Should().Be(encoder.ToJson());
                field.ImportId.Should().NotBeNull();

                var variables = await dbContext.ExhaustiveSearchInstanceVariable
                    .Where(w => w.ExhaustiveSearchInstanceId == imported.Id).OrderBy(o => o.VariableSequence)
                    .ToListAsync();

                variables.Should().HaveCount(1 + encoder.Indicators.Count);
                variables.Skip(1).Should().OnlyContain(v => v.CategoricalFieldId == field.Id);

                variables.Skip(1).Select(s => s.CategoricalIndicatorIndex).Should()
                    .Equal(Enumerable.Range(0, encoder.Indicators.Count).Select(i => (int?)i));

                variables.Skip(1).Select(s => s.CategoricalLevel).Should()
                    .Equal(encoder.Indicators.Select(s => s.Label));

                var importedVariableIds = variables.Select(s => (int?)s.Id).ToList();

                var links = await dbContext.ExhaustiveSearchInstanceTrialInstanceVariable
                    .Where(w => w.ExhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstanceId == imported.Id)
                    .ToListAsync();

                links.Should().HaveCount(variables.Count);

                links.Should().OnlyContain(l => importedVariableIds.Contains(l.ExhaustiveSearchInstanceVariableId),
                    "the trial variables must point at the imported variables and not at the source ones");

                var after = await CategoricalRecallInputsAsync(imported.Id, strings);
                after.Should().HaveCount(before.Count);

                for (var i = 0; i < before.Count; i++)
                {
                    after[i].Should().Equal(before[i], $"string {strings[i]}");
                }
            }
            finally
            {
                await using var cleanup = fx.GetDbContext();

                var modelIds = await cleanup.EntityAnalysisModel
                    .Where(w => w.TenantRegistryId == source.Id || w.TenantRegistryId == target.Id)
                    .Select(s => (int?)s.Id).ToListAsync();

                var instanceIds = await cleanup.ExhaustiveSearchInstance
                    .Where(w => modelIds.Contains(w.EntityAnalysisModelId)).Select(s => (int?)s.Id).ToListAsync();
                var trialIds = await cleanup.ExhaustiveSearchInstanceTrialInstance
                    .Where(w => instanceIds.Contains(w.ExhaustiveSearchInstanceId)).Select(s => (int?)s.Id)
                    .ToListAsync();

                await cleanup.ExhaustiveSearchInstancePromotedTrialInstance
                    .Where(w => trialIds.Contains(w.ExhaustiveSearchInstanceTrialInstanceId)).DeleteAsync();
                await cleanup.ExhaustiveSearchInstanceTrialInstanceVariable
                    .Where(w => trialIds.Contains(w.ExhaustiveSearchInstanceTrialInstanceId)).DeleteAsync();
                await cleanup.ExhaustiveSearchInstanceTrialInstance
                    .Where(w => instanceIds.Contains(w.ExhaustiveSearchInstanceId)).DeleteAsync();
                await cleanup.ExhaustiveSearchInstanceVariable
                    .Where(w => instanceIds.Contains(w.ExhaustiveSearchInstanceId)).DeleteAsync();
                await cleanup.ExhaustiveSearchInstanceCategoricalField
                    .Where(w => instanceIds.Contains(w.ExhaustiveSearchInstanceId)).DeleteAsync();
                await cleanup.ExhaustiveSearchInstance.Where(w => instanceIds.Contains(w.Id)).DeleteAsync();
            }
        }

        [Fact]
        public void ToolCatalogueDoesNotExposeWholeTenantExportOrImport()
        {
            ServiceToolCatalogue.All.Select(t => t.Name)
                .Should().NotContain(n => n.Contains("Preservation", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task SnapshotStoresTheExportBodyAsQueryableJsonAndReturnsItsMetadataAsync()
        {
            var tenant = await CreateTenantAsync("SnapTake");
            var seeded = await SeedTenantAsync(tenant, "SnapTake");

            await using var dbContext = fx.GetDbContext();
            var service = await BuildSnapshotServiceAsync(dbContext, tenant.UserName);

            var dto = await service.CreateAsync(new PreservationSnapshotRequestDto
            {
                Name = "before a change",
                SnapshotSourceId = PreservationSnapshotSource.AskJooby
            });

            dto.Id.Should().BeGreaterThan(0, "the Id is the handle a rollback is quoted against");
            dto.SnapshotSourceId.Should().Be(PreservationSnapshotSource.AskJooby);
            dto.Name.Should().Be("before a change");
            dto.InError.Should().BeFalse();
            dto.CompletedDate.Should().NotBeNull();
            dto.EntityAnalysisModelCount.Should().Be(1);
            dto.Bytes.Should().BeGreaterThan(0);

            var stored = await dbContext.PreservationSnapshot.FirstOrDefaultAsync(w => w.Id == dto.Id);
            stored.Should().NotBeNull();
            stored!.Json.Should().NotBeNullOrEmpty();
            stored.TenantRegistryId.Should().Be(tenant.Id);

            using var document = JsonDocument.Parse(stored.Json!);
            document.RootElement.TryGetProperty("Payload", out var payload).Should().BeTrue();
            payload.TryGetProperty("EntityAnalysisModel", out var models).Should().BeTrue();

            models.EnumerateArray().Select(m => m.GetProperty("Name").GetString()).Should()
                .Contain(seeded.ModelName, "the body must be readable as JSON, which is the point of storing it so");
        }

        [Fact]
        public async Task SnapshotBodyIsQueryableInPostgresAsync()
        {
            var tenant = await CreateTenantAsync("SnapQuery");
            var seeded = await SeedTenantAsync(tenant, "SnapQuery");

            await using var dbContext = fx.GetDbContext();
            var service = await BuildSnapshotServiceAsync(dbContext, tenant.UserName);
            var dto = await service.CreateAsync(new PreservationSnapshotRequestDto());

            var containment = "[{\"Name\":\"" + seeded.ModelName + "\"}]";

            var sql = "SELECT count(*)::int FROM \"PreservationSnapshot\" "
                      + "WHERE \"Id\" = " + dto.Id + " "
                      + "AND \"Json\" -> 'Payload' -> 'EntityAnalysisModel' @> '" + containment + "'::jsonb";

            var found = await dbContext.ExecuteAsync<int>(sql);

            found.Should().Be(1,
                "the snapshot is stored as jsonb precisely so the estate can be queried at a point in time, "
                + "so a containment query over the body must find the model it captured");
        }

        [Fact]
        public async Task SnapshotListReturnsMetadataOnlyAndIsTenantScopedAsync()
        {
            var tenantA = await CreateTenantAsync("SnapA");
            await SeedTenantAsync(tenantA, "SnapA");
            var tenantB = await CreateTenantAsync("SnapB");
            await SeedTenantAsync(tenantB, "SnapB");

            await using var dbContext = fx.GetDbContext();

            var serviceA = await BuildSnapshotServiceAsync(dbContext, tenantA.UserName);
            var created = await serviceA.CreateAsync(new PreservationSnapshotRequestDto { Name = "tenant a" });

            var listA = await serviceA.GetAsync();
            listA.Should().ContainSingle(s => s.Id == created.Id);

            var serviceB = await BuildSnapshotServiceAsync(dbContext, tenantB.UserName);
            var listB = await serviceB.GetAsync();

            listB.Should().NotContain(s => s.Id == created.Id,
                "another tenant must never see this tenant's snapshots");

            var fromB = async () => await serviceB.GetByIdAsync(created.Id);

            await fromB.Should().ThrowAsync<NotFoundException>(
                "a snapshot of another tenant must be indistinguishable from one that does not exist");
        }

        [Fact]
        public async Task SnapshotImportRestoresTheConfigurationItHoldsAsync()
        {
            var tenant = await CreateTenantAsync("SnapBack");
            var seeded = await SeedTenantAsync(tenant, "SnapBack");
            await GrantPermissionAsync(tenant, 63);

            await using var dbContext = fx.GetDbContext();
            var service = await BuildSnapshotServiceAsync(dbContext, tenant.UserName);

            var snapshot = await service.CreateAsync(new PreservationSnapshotRequestDto { Name = "rollback point" });

            var renamed = $"{DatabaseFixture.Prefix}Changed{Guid.NewGuid():N}"[..40];

            await dbContext.EntityAnalysisModel.Where(w => w.Id == seeded.ModelId)
                .Set(s => s.Name, renamed).UpdateAsync();

            (await dbContext.EntityAnalysisModel.CountAsync(w => w.TenantRegistryId == tenant.Id
                                                                 && w.Name == seeded.ModelName))
                .Should().Be(0, "the model was renamed, so the original name is gone before the rollback");

            var imported = await service.ImportAsync(snapshot.Id);
            imported.Id.Should().Be(snapshot.Id);

            (await dbContext.EntityAnalysisModel.CountAsync(w => w.TenantRegistryId == tenant.Id
                                                                 && w.Name == seeded.ModelName))
                .Should().Be(1, "importing the snapshot must restore the configuration it captured");
        }

        [Fact]
        public async Task SnapshotOperationsAreRefusedWithoutThePreservationPermissionAsync()
        {
            var tenant = await CreateTenantAsync("SnapNoPerm", false);
            await SeedTenantAsync(tenant, "SnapNoPerm");

            await using var dbContext = fx.GetDbContext();
            var service = await BuildSnapshotServiceAsync(dbContext, tenant.UserName);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync());

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.CreateAsync(new PreservationSnapshotRequestDto()));

            await Assert.ThrowsAsync<ForbiddenException>(() => service.ImportAsync(1));
            await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteAsync(1));

            (await dbContext.PreservationSnapshot.CountAsync(w => w.TenantRegistryId == tenant.Id)).Should().Be(0);
        }

        [Fact]
        public async Task DeletedSnapshotStopsBeingListedAsync()
        {
            var tenant = await CreateTenantAsync("SnapDel");
            await SeedTenantAsync(tenant, "SnapDel");

            await using var dbContext = fx.GetDbContext();
            var service = await BuildSnapshotServiceAsync(dbContext, tenant.UserName);

            var created = await service.CreateAsync(new PreservationSnapshotRequestDto());
            (await service.GetAsync()).Should().ContainSingle(s => s.Id == created.Id);

            await service.DeleteAsync(created.Id);

            (await service.GetAsync()).Should().NotContain(s => s.Id == created.Id);
            var gone = async () => await service.GetByIdAsync(created.Id);
            await gone.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task SnapshotImportIsRefusedWithoutItsOwnPermissionEvenWithPreservationAsync()
        {
            var tenant = await CreateTenantAsync("SnapImpPerm");
            var seeded = await SeedTenantAsync(tenant, "SnapImpPerm");

            await using var dbContext = fx.GetDbContext();
            var service = await BuildSnapshotServiceAsync(dbContext, tenant.UserName);

            var snapshot = await service.CreateAsync(new PreservationSnapshotRequestDto());

            (await service.GetAsync()).Should().ContainSingle(x => x.Id == snapshot.Id,
                "listing needs only the Preservation permission");

            await Assert.ThrowsAsync<ForbiddenException>(() => service.ImportAsync(snapshot.Id));

            var renamed = $"{DatabaseFixture.Prefix}Untouched{Guid.NewGuid():N}"[..40];

            await dbContext.EntityAnalysisModel.Where(w => w.Id == seeded.ModelId)
                .Set(s => s.Name, renamed).UpdateAsync();

            await Assert.ThrowsAsync<ForbiddenException>(() => service.ImportAsync(snapshot.Id));

            (await dbContext.EntityAnalysisModel.CountAsync(w => w.Id == seeded.ModelId && w.Name == renamed))
                .Should().Be(1,
                    "a refused import must not have replaced any configuration");

            await GrantPermissionAsync(tenant, 63);

            await using var permittedContext = fx.GetDbContext();
            var permitted = await BuildSnapshotServiceAsync(permittedContext, tenant.UserName);
            await permitted.ImportAsync(snapshot.Id);

            (await permittedContext.EntityAnalysisModel.CountAsync(w => w.TenantRegistryId == tenant.Id
                                                                        && w.Name == seeded.ModelName))
                .Should().Be(1, "granting the import permission must let the rollback through");
        }

        [Fact]
        public async Task ProcessSnapshotBypassesPermissionButStillRecordsTheUserAndStaysInTenantAsync()
        {
            var tenant = await CreateTenantAsync("SnapProc", false);
            var seeded = await SeedTenantAsync(tenant, "SnapProc");

            await using var dbContext = fx.GetDbContext();
            var service = await BuildSnapshotServiceAsync(dbContext, tenant.UserName);

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.CreateAsync(new PreservationSnapshotRequestDto()));

            var snapshot = await service.CreateForProcessAsync(new PreservationSnapshotRequestDto
            {
                SnapshotSourceId = PreservationSnapshotSource.ModelSync,
                Name = "model sync"
            });

            snapshot.Id.Should().BeGreaterThan(0,
                "a process has already decided to act, so it is not gated by the user's permission");

            snapshot.SnapshotSourceId.Should().Be(PreservationSnapshotSource.ModelSync);

            snapshot.CreatedUser.Should().Be(tenant.UserName,
                "the user is recorded even where the process, not the user, forced the snapshot");

            var stored = await dbContext.PreservationSnapshot.FirstOrDefaultAsync(w => w.Id == snapshot.Id);
            stored.Should().NotBeNull();

            stored!.TenantRegistryId.Should().Be(tenant.Id,
                "bypassing permission must never bypass tenant scoping");

            stored.Json.Should().Contain(seeded.ModelName);
        }

        [Fact]
        public async Task ProcessSnapshotRefusesToActForThePreservationPageSourceAsync()
        {
            var tenant = await CreateTenantAsync("SnapProcSrc", false);
            await SeedTenantAsync(tenant, "SnapProcSrc");

            await using var dbContext = fx.GetDbContext();
            var service = await BuildSnapshotServiceAsync(dbContext, tenant.UserName);

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                service.CreateForProcessAsync(new PreservationSnapshotRequestDto
                {
                    SnapshotSourceId = PreservationSnapshotSource.PreservationPage
                }));

            (await dbContext.PreservationSnapshot.CountAsync(w => w.TenantRegistryId == tenant.Id))
                .Should().Be(0,
                    "the Preservation page always means a person acting directly, so it may not use the "
                    + "process path to sidestep the permission");
        }

        [Fact]
        public async Task ProcessImportBypassesThePermissionAndRestoresAsync()
        {
            var tenant = await CreateTenantAsync("SnapProcImp", false);
            var seeded = await SeedTenantAsync(tenant, "SnapProcImp");

            await using var dbContext = fx.GetDbContext();
            var service = await BuildSnapshotServiceAsync(dbContext, tenant.UserName);

            var snapshot = await service.CreateForProcessAsync(new PreservationSnapshotRequestDto
            {
                SnapshotSourceId = PreservationSnapshotSource.AskJooby
            });

            var renamed = $"{DatabaseFixture.Prefix}Changed{Guid.NewGuid():N}"[..40];

            await dbContext.EntityAnalysisModel.Where(w => w.Id == seeded.ModelId)
                .Set(s => s.Name, renamed).UpdateAsync();

            await Assert.ThrowsAsync<ForbiddenException>(() => service.ImportAsync(snapshot.Id));

            await service.ImportForProcessAsync(snapshot.Id);

            (await dbContext.EntityAnalysisModel.CountAsync(w => w.TenantRegistryId == tenant.Id
                                                                 && w.Name == seeded.ModelName))
                .Should().Be(1, "AskJooby must be able to roll back the change it made");
        }

        [Fact]
        public async Task SnapshotBodyIsReturnedWholeAndIsTenantScopedAsync()
        {
            var tenantA = await CreateTenantAsync("SnapBodyA");
            var seededA = await SeedTenantAsync(tenantA, "SnapBodyA");
            var tenantB = await CreateTenantAsync("SnapBodyB");
            await SeedTenantAsync(tenantB, "SnapBodyB");

            await using var dbContext = fx.GetDbContext();
            var serviceA = await BuildSnapshotServiceAsync(dbContext, tenantA.UserName);
            var snapshot = await serviceA.CreateAsync(new PreservationSnapshotRequestDto());

            var body = await serviceA.GetBodyAsync(snapshot.Id);

            body.Should().NotBeNullOrEmpty();
            using var document = JsonDocument.Parse(body);

            document.RootElement.GetProperty("Payload").GetProperty("EntityAnalysisModel")
                .EnumerateArray().Select(m => m.GetProperty("Name").GetString()).Should()
                .Contain(seededA.ModelName, "the whole captured body must come back, not a summary of it");

            var serviceB = await BuildSnapshotServiceAsync(dbContext, tenantB.UserName);
            var fromB = async () => await serviceB.GetBodyAsync(snapshot.Id);

            await fromB.Should().ThrowAsync<NotFoundException>(
                "the body of another tenant's snapshot is the whole of its configuration and must never be served");
        }

        [Fact]
        public async Task SnapshotBodyIsRefusedWithoutThePreservationPermissionAsync()
        {
            var tenant = await CreateTenantAsync("SnapBodyPerm", false);
            await SeedTenantAsync(tenant, "SnapBodyPerm");

            await using var dbContext = fx.GetDbContext();
            var service = await BuildSnapshotServiceAsync(dbContext, tenant.UserName);

            var snapshot = await service.CreateForProcessAsync(new PreservationSnapshotRequestDto
            {
                SnapshotSourceId = PreservationSnapshotSource.ModelSync
            });

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetBodyAsync(snapshot.Id));
        }
    }
}