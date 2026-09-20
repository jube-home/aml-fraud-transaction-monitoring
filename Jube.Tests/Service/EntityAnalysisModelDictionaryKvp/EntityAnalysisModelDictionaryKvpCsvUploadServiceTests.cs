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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.EntityAnalysisModelDictionaryKvp;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.EntityAnalysisModelDictionaryKvp;
using Jube.Service.Exceptions.EntityAnalysisModelDictionaryKvp;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModelDictionaryKvp.Models;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using KvpPoco = Jube.Data.Poco.EntityAnalysisModelDictionaryKvp;
using UploadPoco = Jube.Data.Poco.EntityAnalysisModelDictionaryCsvFileUpload;

namespace Jube.Test.Service.EntityAnalysisModelDictionaryKvp
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EntityAnalysisModelDictionaryKvpCsvUploadServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdDictionaryIds = [];
        private readonly List<int> createdModelIds = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            foreach (var dictionaryId in createdDictionaryIds)
            {
                var kvpIds = await dbContext.GetTable<KvpPoco>()
                    .Where(w => w.EntityAnalysisModelDictionaryId == dictionaryId).Select(s => s.Id).ToListAsync();
                foreach (var kvpId in kvpIds)
                {
                    await dbContext.GetTable<EntityAnalysisModelDictionaryKvpVersion>()
                        .Where(w => w.EntityAnalysisModelDictionaryKvpId == kvpId).DeleteAsync();
                }

                await dbContext.GetTable<KvpPoco>().Where(w => w.EntityAnalysisModelDictionaryId == dictionaryId)
                    .DeleteAsync();
                await dbContext.GetTable<UploadPoco>()
                    .Where(w => w.EntityAnalysisModelDictionaryId == dictionaryId).DeleteAsync();
                await dbContext.GetTable<EntityAnalysisModelDictionaryVersion>()
                    .Where(w => w.EntityAnalysisModelDictionaryId == dictionaryId).DeleteAsync();
                await dbContext.GetTable<Data.Poco.EntityAnalysisModelDictionary>().Where(w => w.Id == dictionaryId)
                    .DeleteAsync();
            }

            foreach (var modelId in createdModelIds)
            {
                await dbContext.GetTable<EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == modelId)
                    .DeleteAsync();
                await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
            }
        }

        private static Task<EntityAnalysisModelDictionaryKvpService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return EntityAnalysisModelDictionaryKvpService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task<int> CreateParentDictionaryAsync(DbContext dbContext, string createdUser)
        {
            var modelRepository = new EntityAnalysisModelRepository(dbContext, createdUser);
            var model = await modelRepository.InsertAsync(new Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0
            });
            createdModelIds.Add(model.Id);

            var dictionaryRepository = new EntityAnalysisModelDictionaryRepository(dbContext, createdUser);
            var dictionary = await dictionaryRepository.InsertAsync(new Data.Poco.EntityAnalysisModelDictionary
            {
                EntityAnalysisModelGuid = model.Guid,
                Name = $"{DatabaseFixture.Prefix}Dictionary{Guid.NewGuid():N}"[..40],
                DataName = "AccountId",
                Active = 1,
                Locked = 0
            });
            createdDictionaryIds.Add(dictionary.Id);

            return dictionary.Id;
        }

        private static EntityAnalysisModelDictionaryKvpCsvUploadFileDto File(string name, string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            return new EntityAnalysisModelDictionaryKvpCsvUploadFileDto
            {
                FileName = name, Length = bytes.Length, Content = new MemoryStream(bytes)
            };
        }

        private static string Key(string label)
        {
            var key = $"{DatabaseFixture.Prefix}{label}{Guid.NewGuid():N}";
            return key.Length > 40 ? key[..40] : key;
        }

        private async Task<List<KvpPoco>> KvpsAsync(int dictionaryId)
        {
            await using var dbContext = fx.GetDbContext();
            return await dbContext.GetTable<KvpPoco>().Where(w => w.EntityAnalysisModelDictionaryId == dictionaryId)
                .OrderBy(o => o.Id).ToListAsync();
        }

        private async Task<List<UploadPoco>> UploadsAsync(int dictionaryId)
        {
            await using var dbContext = fx.GetDbContext();
            return await dbContext.GetTable<UploadPoco>()
                .Where(w => w.EntityAnalysisModelDictionaryId == dictionaryId).OrderBy(o => o.Id).ToListAsync();
        }

        [Fact]
        public async Task ValidCsvInsertsRowsAndRecordsUploadAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var k1 = Key("A");
            var k2 = Key("B");
            var expiry = DateTime.UtcNow.AddDays(10);
            var csv = $"{k1},10.5\n{k2},7,{expiry:O}\n";

            var results = await service.UploadCsvAsync([File("valid.csv", csv)], dictionaryId);

            results.Should().ContainSingle();
            results[0].Records.Should().Be(2);
            results[0].Errors.Should().Be(0);
            results[0].FileName.Should().Be("valid.csv");

            var kvps = await KvpsAsync(dictionaryId);
            kvps.Should().HaveCount(2);
            kvps.Single(s => s.KvpKey == k1).KvpValue.Should().Be(10.5);
            kvps.Single(s => s.KvpKey == k1).DeleteExpiryDate.Should().BeNull();
            kvps.Single(s => s.KvpKey == k2).KvpValue.Should().Be(7);
            kvps.Single(s => s.KvpKey == k2).DeleteExpiryDate.Should().NotBeNull()
                .And.BeCloseTo(expiry, TimeSpan.FromSeconds(1));
            kvps.Should().OnlyContain(o => o.CreatedUser == fx.Seed.UserWithPermission);

            var uploads = await UploadsAsync(dictionaryId);
            uploads.Should().ContainSingle();
            uploads[0].Id.Should().Be(results[0].Id);
            uploads[0].FileName.Should().Be("valid.csv");
            uploads[0].Records.Should().Be(2);
            uploads[0].Errors.Should().Be(0);
            uploads[0].Length.Should().Be(Encoding.UTF8.GetByteCount(csv));
            uploads[0].CreatedUser.Should().Be(fx.Seed.UserWithPermission);
        }

        [Fact]
        public async Task ExistingKeyIsUpdatedAndExpiryKeptWhenNotSpecifiedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var key = Key("Upsert");
            var expiry = DateTime.UtcNow.AddDays(5);

            await service.UploadCsvAsync([File("one.csv", $"{key},1,{expiry:O}")], dictionaryId);
            await service.UploadCsvAsync([File("two.csv", $"{key},2")], dictionaryId);

            var kvps = await KvpsAsync(dictionaryId);
            kvps.Should().ContainSingle();
            kvps[0].KvpValue.Should().Be(2);
            kvps[0].DeleteExpiryDate.Should().BeCloseTo(expiry, TimeSpan.FromSeconds(1));

            await service.UploadCsvAsync([File("three.csv", $"{key},3,not-a-date")], dictionaryId);
            (await KvpsAsync(dictionaryId)).Single().DeleteExpiryDate.Should().BeNull();
            (await UploadsAsync(dictionaryId)).Should().HaveCount(3);
        }

        [Fact]
        public async Task MalformedRowsAreLoggedSkippedAndNotCountedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);
            var good = Key("Good");
            var bad = Key("Bad");
            var single = Key("Single");

            var results = await service.UploadCsvAsync(
                [File("mixed.csv", $"{good},1\n{bad},abc\n{single}\n{Key("Good2")},2")], dictionaryId);

            results[0].Records.Should().Be(3);
            results[0].Errors.Should().Be(0);
            var kvps = await KvpsAsync(dictionaryId);
            kvps.Should().HaveCount(2);
            kvps.Should().NotContain(c => c.KvpKey == bad || c.KvpKey == single);
            log.Entries.Should().Contain(e => e.Level == "ERROR");
            (await UploadsAsync(dictionaryId)).Should().ContainSingle().Which.Records.Should().Be(3);
        }

        [Fact]
        public async Task EmptyFileWritesUploadRecordWithZeroRecordsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var results = await service.UploadCsvAsync([File("empty.csv", "")], dictionaryId);

            results.Should().ContainSingle().Which.Records.Should().Be(0);
            (await KvpsAsync(dictionaryId)).Should().BeEmpty();
            var uploads = await UploadsAsync(dictionaryId);
            uploads.Should().ContainSingle();
            uploads[0].Records.Should().Be(0);
            uploads[0].Length.Should().Be(0);
        }

        [Fact]
        public async Task NoFilesIsANoOpAsync()
        {
            var bus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);

            (await service.UploadCsvAsync([], dictionaryId)).Should().BeEmpty();

            (await UploadsAsync(dictionaryId)).Should().BeEmpty();
            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task MultipleFilesEachGetAnUploadRecordAndOneEventAsync()
        {
            var bus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);

            var results = await service.UploadCsvAsync(
                [File("a.csv", $"{Key("F1")},1\n{Key("F2")},2"), File("b.csv", $"{Key("F3")},3")], dictionaryId);

            results.Select(s => s.FileName).Should().Equal("a.csv", "b.csv");
            results.Select(s => s.Records).Should().Equal(2, 1);
            (await KvpsAsync(dictionaryId)).Should().HaveCount(3);
            (await UploadsAsync(dictionaryId)).Select(s => s.FileName).Should().Equal("a.csv", "b.csv");

            bus.Published.Should().ContainSingle();
            bus.Published[0].Kind.Should().Be(ServiceChangeKind.Created);
            bus.Published[0].EntityId.Should().Be(results[1].Id);
            bus.Published[0].Area.Should().Be("EntityAnalysisModelDictionaryKvp");
        }

        [Fact]
        public async Task ParentInOtherTenantBehavesAsNotFoundAndWritesNothingAsync()
        {
            var bus = new CapturingBus();
            await using var ownerDb = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(ownerDb, fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB, serviceChangeBus: bus);

            var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
                service.UploadCsvAsync([File("x.csv", $"{Key("Foreign")},1")], dictionaryId));

            ex.Code.Should().Be("NotFound");
            (await KvpsAsync(dictionaryId)).Should().BeEmpty();
            (await UploadsAsync(dictionaryId)).Should().BeEmpty();
            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task UnknownDictionaryThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.UploadCsvAsync([File("x.csv", "a,1")], int.MaxValue - 1));
        }

        [Fact]
        public async Task WithoutPermissionThrowsForbiddenAndWritesNothingAsync()
        {
            var bus = new CapturingBus();
            var log = new TestLog();
            await using var ownerDb = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(ownerDb, fx.Seed.UserWithPermission);
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log,
                serviceChangeBus: bus);

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.UploadCsvAsync([File("x.csv", $"{Key("Denied")},1")], dictionaryId));

            ex.Code.Should().Be("PermissionDenied");
            ex.RequiredSpecifications.Should().Equal(4);
            log.Entries.Should().Contain(e => e.Level == "WARN");
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
            (await KvpsAsync(dictionaryId)).Should().BeEmpty();
            (await UploadsAsync(dictionaryId)).Should().BeEmpty();
            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task NullFilesThrowsArgumentNullAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<ArgumentNullException>(() => service.UploadCsvAsync(null, 1));
        }

        [Fact]
        public async Task CancelledTokenThrowsAndPublishesNothingAsync()
        {
            var bus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                service.UploadCsvAsync([File("x.csv", $"{Key("Cancel")},1")], dictionaryId, cts.Token));

            (await UploadsAsync(dictionaryId)).Should().BeEmpty();
            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task WritesExactlyOneAuditRecordAsync()
        {
            var audit = new TestLog();
            await using var dbContext = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: audit);

            await service.UploadCsvAsync([File("x.csv", $"{Key("Audit")},1")], dictionaryId);

            audit.Entries.Should().ContainSingle();
            audit.Entries[0].Message.Should().Contain("op=UploadCsv").And.Contain("outcome=ok");
        }

        [Fact]
        public void CatalogueListsUploadToolOnce()
        {
            ServiceToolCatalogue.All.Count(t => t.Name == "EntityAnalysisModelDictionaryKvpUploadCsv")
                .Should().Be(1);
        }
    }
}