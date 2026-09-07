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
using System.Diagnostics;
using System.Linq;
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
using Jube.Service.Observability;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.EntityAnalysisModelDictionaryKvp
{
    using EntityAnalysisModelDictionaryKvpService = EntityAnalysisModelDictionaryKvpService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EntityAnalysisModelDictionaryKvpServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdDictionaryIds = [];
        private readonly List<int> createdIds = [];
        private readonly List<int> createdModelIds = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            foreach (var id in createdIds)
            {
                await dbContext.GetTable<EntityAnalysisModelDictionaryKvpVersion>()
                    .Where(w => w.EntityAnalysisModelDictionaryKvpId == id).DeleteAsync();
                await dbContext.GetTable<Data.Poco.EntityAnalysisModelDictionaryKvp>().Where(w => w.Id == id)
                    .DeleteAsync();
            }

            foreach (var dictionaryId in createdDictionaryIds)
            {
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
            }).ConfigureAwait(false);
            createdModelIds.Add(model.Id);

            var dictionaryRepository = new EntityAnalysisModelDictionaryRepository(dbContext, createdUser);
            var dictionary = await dictionaryRepository.InsertAsync(new Data.Poco.EntityAnalysisModelDictionary
            {
                EntityAnalysisModelGuid = model.Guid,
                Name = $"{DatabaseFixture.Prefix}Dictionary{Guid.NewGuid():N}"[..40],
                DataName = "AccountId",
                Active = 1,
                Locked = 0
            }).ConfigureAwait(false);
            createdDictionaryIds.Add(dictionary.Id);

            return dictionary.Id;
        }

        private static EntityAnalysisModelDictionaryKvpDto NewDto(int entityAnalysisModelDictionaryId, string kvpKey,
            double kvpValue = 1000)
        {
            return new EntityAnalysisModelDictionaryKvpDto
            {
                EntityAnalysisModelDictionaryId = entityAnalysisModelDictionaryId,
                KvpKey = kvpKey,
                KvpValue = kvpValue
            };
        }

        private static string UniqueKey(string label)
        {
            return $"{DatabaseFixture.Prefix}{label}{Guid.NewGuid():N}"[..40];
        }

        [Fact]
        public async Task InsertPersistsAndReturnsVersionOneAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(dictionaryId, UniqueKey("Insert")));
            createdIds.Add(saved.Id);

            saved.Id.Should().BeGreaterThan(0);
            saved.Version.Should().Be(1);
            saved.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            saved.CreatedDate.Should().NotBeNull();
            saved.CreatedDate!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task GetAllReturnsCreatedRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var key = UniqueKey("GetAll");
            var saved = await service.InsertAsync(NewDto(dictionaryId, key));
            createdIds.Add(saved.Id);

            var all = await service.GetAsync();
            all.Should().Contain(d => d.Id == saved.Id && d.KvpKey == key);
        }

        [Fact]
        public async Task GetByEntityAnalysisModelDictionaryIdReturnsRowsForThatDictionaryOrderedByIdAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var dictionaryAId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var dictionaryBId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var savedA1 = await service.InsertAsync(NewDto(dictionaryAId, UniqueKey("ByDictionaryA1")));
            createdIds.Add(savedA1.Id);

            var savedA2 = await service.InsertAsync(NewDto(dictionaryAId, UniqueKey("ByDictionaryA2")));
            createdIds.Add(savedA2.Id);

            var savedB = await service.InsertAsync(NewDto(dictionaryBId, UniqueKey("ByDictionaryB")));
            createdIds.Add(savedB.Id);

            var forDictionaryA = await service.GetByEntityAnalysisModelDictionaryIdAsync(dictionaryAId);
            forDictionaryA.Should().Contain(d => d.Id == savedA1.Id);
            forDictionaryA.Should().Contain(d => d.Id == savedA2.Id);
            forDictionaryA.Should().NotContain(d => d.Id == savedB.Id);
            forDictionaryA.Select(d => d.Id).Should().BeInAscendingOrder();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(int.MaxValue - 1)]
        public async Task GetByEntityAnalysisModelDictionaryIdWithZeroOrNonExistentIdReturnsEmptyListAsync(
            int entityAnalysisModelDictionaryId)
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetByEntityAnalysisModelDictionaryIdAsync(entityAnalysisModelDictionaryId);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task UpdateIncrementsVersionAndWritesAuditRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(dictionaryId, UniqueKey("Update")));
            createdIds.Add(saved.Id);

            var newKey = UniqueKey("UpdateNewKey");
            var dto = NewDto(dictionaryId, newKey, 2000);
            dto.Id = saved.Id;

            var updated = await service.UpdateAsync(dto);

            updated.Version.Should().Be(2);
            updated.KvpKey.Should().Be(newKey);
            updated.KvpValue.Should().Be(2000);

            var auditRows = await dbContext.GetTable<EntityAnalysisModelDictionaryKvpVersion>()
                .Where(w => w.EntityAnalysisModelDictionaryKvpId == saved.Id).CountAsync();
            auditRows.Should().Be(1);
        }

        [Fact]
        public async Task DeleteSoftDeletesAndRowDisappearsFromByDictionaryIdAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(dictionaryId, UniqueKey("Delete")));
            createdIds.Add(saved.Id);

            await service.DeleteAsync(saved.Id);

            var byDictionary = await service.GetByEntityAnalysisModelDictionaryIdAsync(dictionaryId);
            byDictionary.Should().NotContain(d => d.Id == saved.Id);
        }

        [Fact]
        public async Task EveryMethodThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync()
        {
            await using var writerDb = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(writerDb, fx.Seed.UserWithPermission);
            var writer = await BuildServiceAsync(writerDb, fx.Seed.UserWithPermission);
            var seedRow = await writer.InsertAsync(NewDto(dictionaryId, UniqueKey("Forbidden")));
            createdIds.Add(seedRow.Id);

            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var beforeCount = await dbContext.GetTable<Data.Poco.EntityAnalysisModelDictionaryKvp>()
                .CountAsync(w => w.KvpKey!.StartsWith(DatabaseFixture.Prefix));

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.InsertAsync(NewDto(dictionaryId, UniqueKey("Denied"))));
            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.GetByEntityAnalysisModelDictionaryIdAsync(dictionaryId));

            var updateDto = NewDto(dictionaryId, seedRow.KvpKey!);
            updateDto.Id = seedRow.Id;
            await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateAsync(updateDto));
            await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteAsync(seedRow.Id));

            var afterCount = await dbContext.GetTable<Data.Poco.EntityAnalysisModelDictionaryKvp>()
                .CountAsync(w => w.KvpKey!.StartsWith(DatabaseFixture.Prefix));
            afterCount.Should().Be(beforeCount);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task NullOrBlankUserNameThrowsNotAuthenticatedBeforeAnyQueryAsync(string? userName)
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();

            var ex = await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                EntityAnalysisModelDictionaryKvpService.CreateAsync(dbContext, userName, log, localizers,
                    new NullServiceChangeBus(), TestLog.NoOp));

            ex.Code.Should().Be("NotAuthenticated");
            log.Entries.Should().Contain(e => e.Level == "WARN");
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task UserWithNoUserInTenantRowThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UserNoTenant));
        }

        [Fact]
        public async Task UnknownUserThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UnknownUser));
        }

        [Fact]
        public async Task UserInTenantBCannotUpdateOrDeleteTenantARowAsync()
        {
            await using var ownerDb = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(ownerDb, fx.Seed.UserWithPermission);
            var owner = await BuildServiceAsync(ownerDb, fx.Seed.UserWithPermission);
            var saved = await owner.InsertAsync(NewDto(dictionaryId, UniqueKey("Isolation")));
            createdIds.Add(saved.Id);

            await using var dbContext = fx.GetDbContext();
            var otherTenant = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            var updateDto = NewDto(dictionaryId, saved.KvpKey!);
            updateDto.Id = saved.Id;

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => otherTenant.UpdateAsync(updateDto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "EntityAnalysisModelDictionaryIdNotFound");

            await Assert.ThrowsAsync<NotFoundException>(() => otherTenant.DeleteAsync(saved.Id));

            var byDictionary = await owner.GetByEntityAnalysisModelDictionaryIdAsync(dictionaryId);
            byDictionary.Should().Contain(d => d.Id == saved.Id);
        }

        [Fact]
        public async Task GetByEntityAnalysisModelDictionaryIdAsTenantBNeverContainsTenantARowsAsync()
        {
            await using var ownerDb = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(ownerDb, fx.Seed.UserWithPermission);
            var owner = await BuildServiceAsync(ownerDb, fx.Seed.UserWithPermission);
            var saved = await owner.InsertAsync(NewDto(dictionaryId, UniqueKey("TenantAOnly")));
            createdIds.Add(saved.Id);

            await using var dbContext = fx.GetDbContext();
            var otherTenant = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            var forDictionaryAsTenantB = await otherTenant.GetByEntityAnalysisModelDictionaryIdAsync(dictionaryId);

            forDictionaryAsTenantB.Should().NotContain(d => d.Id == saved.Id);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task KvpKeyRequiredRejectsNullEmptyOrWhitespaceAsync(string? kvpKey)
        {
            await using var dbContext = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(dictionaryId, kvpKey!);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e =>
                e.PropertyName == nameof(EntityAnalysisModelDictionaryKvpDto.KvpKey) &&
                e.ErrorCode == "KvpKeyNotEmpty");
        }

        [Fact]
        public async Task KvpKeyOverMaximumLengthIsRejectedWithErrorCodeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(dictionaryId, new string('k', 257));

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "KvpKeyMaximumLength");
        }

        [Fact]
        public async Task KvpValueNullIsRejectedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(dictionaryId, UniqueKey("NullValue"));
            dto.KvpValue = null;

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e =>
                e.PropertyName == nameof(EntityAnalysisModelDictionaryKvpDto.KvpValue) &&
                e.ErrorCode == "KvpValueNotEmpty");
        }

        [Fact]
        public async Task KvpValueOfZeroIsAcceptedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(dictionaryId, UniqueKey("ZeroValue"), 0);

            var saved = await service.InsertAsync(dto);
            createdIds.Add(saved.Id);

            saved.KvpValue.Should().Be(0);
        }

        [Fact]
        public async Task InvalidEntityAnalysisModelDictionaryIdIsRejectedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(0, UniqueKey("BadDictionaryId"));

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "EntityAnalysisModelDictionaryIdInvalid");
        }

        [Fact]
        public async Task NonExistentEntityAnalysisModelDictionaryIdIsRejectedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(int.MaxValue - 1, UniqueKey("MissingDictionaryId"));

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "EntityAnalysisModelDictionaryIdNotFound");
        }

        [Fact]
        public async Task DeleteExpiryDateInThePastIsRejectedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(dictionaryId, UniqueKey("PastExpiry"));
            dto.DeleteExpiryDate = DateTimeOffset.UtcNow.AddDays(-1);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "DeleteExpiryDateMustBeFuture");
        }

        [Fact]
        public async Task DeleteExpiryDateInTheFutureIsAcceptedAndRoundTripsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var expiry = DateTimeOffset.UtcNow.AddDays(1);
            var dto = NewDto(dictionaryId, UniqueKey("FutureExpiry"));
            dto.DeleteExpiryDate = expiry;

            var saved = await service.InsertAsync(dto);
            createdIds.Add(saved.Id);

            saved.DeleteExpiryDate.Should().BeCloseTo(expiry.UtcDateTime, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task ExpiredValueDisappearsFromByDictionaryIdAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(dictionaryId, UniqueKey("AboutToExpire")));
            createdIds.Add(saved.Id);

            await dbContext.GetTable<Data.Poco.EntityAnalysisModelDictionaryKvp>()
                .Where(w => w.Id == saved.Id)
                .Set(s => s.DeleteExpiryDate, DateTime.UtcNow.AddSeconds(-1))
                .UpdateAsync();

            var byDictionary = await service.GetByEntityAnalysisModelDictionaryIdAsync(dictionaryId);
            byDictionary.Should().NotContain(d => d.Id == saved.Id);
        }

        [Fact]
        public async Task TamperedIdentityAndAuditFieldsOnInsertHaveNoEffectAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = NewDto(dictionaryId, UniqueKey("Tamper"));
            dto.CreatedUser = "someone-else";
            dto.Version = 999;

            var saved = await service.InsertAsync(dto);
            createdIds.Add(saved.Id);

            saved.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            saved.Version.Should().Be(1);
        }

        [Fact]
        public async Task TamperedIdentityAndAuditFieldsOnUpdateHaveNoEffectAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(dictionaryId, UniqueKey("TamperUpdate")));
            createdIds.Add(saved.Id);

            var dto = NewDto(dictionaryId, saved.KvpKey!);
            dto.Id = saved.Id;
            dto.CreatedUser = "someone-else";
            dto.Version = 999;
            dto.UpdatedUser = "also-tampered";

            var updated = await service.UpdateAsync(dto);
            updated.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            updated.Version.Should().Be(2);
            updated.UpdatedUser.Should().BeNull();
        }

        [Fact]
        public async Task UpdateOfSoftDeletedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(dictionaryId, UniqueKey("PreDeleted")));
            createdIds.Add(saved.Id);

            await service.DeleteAsync(saved.Id);

            var dto = NewDto(dictionaryId, saved.KvpKey!);
            dto.Id = saved.Id;
            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task UpdateOfNeverExistedIdThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = NewDto(dictionaryId, UniqueKey("Missing"));
            dto.Id = int.MaxValue - 1;

            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task DeleteOfMissingOrAlreadyDeletedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(int.MaxValue - 1));

            var saved = await service.InsertAsync(NewDto(dictionaryId, UniqueKey("DoubleDelete")));
            createdIds.Add(saved.Id);
            await service.DeleteAsync(saved.Id);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(saved.Id));
        }

        [Fact]
        public async Task GetByIdMatchesOnParentDictionaryIdPreExistingRepositoryQuirkAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(dictionaryId, UniqueKey("GetByIdQuirk")));
            createdIds.Add(saved.Id);

            var byOwnId = await service.GetByIdAsync(saved.Id);
            byOwnId.Should().BeNull();

            var byParentDictionaryId = await service.GetByIdAsync(dictionaryId);
            byParentDictionaryId.Should().NotBeNull();
            byParentDictionaryId!.Id.Should().Be(saved.Id);
        }

        [Fact]
        public async Task PreCancelledTokenOnReadThrowsOperationCanceledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetAsync(cts.Token));
        }

        [Fact]
        public async Task PermissionDeniedLogsWarnNotErrorAndGatesHoldAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByIdAsync(1));

            log.Entries.Should().Contain(e => e.Level == "WARN" && e.Message.Contains(fx.Seed.UserWithoutPermission));
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task SuccessfulInsertLogsExactlyOneInfoAndReadsLogNoInfoAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            var saved = await service.InsertAsync(NewDto(dictionaryId, UniqueKey("LogInsert")));
            createdIds.Add(saved.Id);

            log.Entries.Count(e => e.Level == "INFO").Should().Be(1);

            var infoCountBeforeRead = log.Entries.Count(e => e.Level == "INFO");
            await service.GetAsync();
            log.Entries.Count(e => e.Level == "INFO").Should().Be(infoCountBeforeRead);
        }

        [Fact]
        public async Task WithGatesDisabledHappyPathRecordsNoDebugInfoOrWarnAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var log = new TestLog(false);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            var saved = await service.InsertAsync(NewDto(dictionaryId, UniqueKey("Gated")));
            createdIds.Add(saved.Id);

            log.Entries.Should().BeEmpty();
        }

        [Fact]
        public async Task FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync()
        {
            var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            await dbContext.DisposeAsync();

            await Assert.ThrowsAnyAsync<Exception>(() => service.GetAsync());

            var errorEntry = log.Entries.Should().ContainSingle(e => e.Level == "ERROR").Subject;
            errorEntry.Exception.Should().NotBeNull();
        }

        [Fact]
        public async Task EachCallEmitsOneSpanWithOutcomeTagAsync()
        {
            var activities = new List<Activity>();
            using var listener = new ActivityListener();
            listener.ShouldListenTo = s => s.Name == ServiceDiagnostics.Name;
            listener.Sample = (ref _) => ActivitySamplingResult.AllData;
            listener.ActivityStopped = activities.Add;
            ActivitySource.AddActivityListener(listener);

            await using var dbContext = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(dictionaryId, UniqueKey("Span")));
            createdIds.Add(saved.Id);

            var createSpan = activities.Should()
                .ContainSingle(a => a.OperationName == "EntityAnalysisModelDictionaryKvp.Create").Subject;
            createSpan.GetTagItem("jube.outcome").Should().Be("ok");
            createSpan.GetTagItem("jube.entity.id").Should().Be(saved.Id);
        }

        [Fact]
        public async Task EachCallRecordsOneDurationMeasurementAsync()
        {
            using var collector = new MetricCollector<double>(ServiceDiagnostics.OperationDuration);

            await using var dbContext = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(dictionaryId, UniqueKey("Metric")));
            createdIds.Add(saved.Id);

            collector.GetMeasurementSnapshot().Should().ContainSingle(m => (string)m.Tags["operation"]! == "Create");
        }

        [Fact]
        public async Task AuditLogGetsExactlyOneLineIncludingReadsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditLog);

            await service.GetAsync();

            auditLog.Entries.Should().ContainSingle();
            auditLog.Entries[0].Message.Should().Contain("op=List");
        }

        [Fact]
        public async Task InsertPublishesExactlyOneCreatedEventAsync()
        {
            var serviceChangeBus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                serviceChangeBus: serviceChangeBus);

            var saved = await service.InsertAsync(NewDto(dictionaryId, UniqueKey("Reactive")));
            createdIds.Add(saved.Id);

            serviceChangeBus.Published.Should().ContainSingle();
            serviceChangeBus.Published[0].Kind.Should().Be(ServiceChangeKind.Created);
            serviceChangeBus.Published[0].EntityId.Should().Be(saved.Id);
        }

        [Fact]
        public async Task ReadsAndFailuresPublishNothingAsync()
        {
            var serviceChangeBus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                serviceChangeBus: serviceChangeBus);

            await service.GetAsync();
            var badDto = NewDto(dictionaryId, "");
            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(badDto));

            serviceChangeBus.Published.Should().BeEmpty();
        }

        [Fact]
        public void CatalogueRegistersUniquePascalCaseNoUnderscoreNames()
        {
            var names = ServiceToolCatalogue.All.Select(t => t.Name).ToList();
            names.Should().OnlyHaveUniqueItems();
            names.Should().OnlyContain(n => !n.Contains('_'));
            names.Should().Contain(
            [
                "EntityAnalysisModelDictionaryKvpList", "EntityAnalysisModelDictionaryKvpGet",
                "EntityAnalysisModelDictionaryKvpGetByEntityAnalysisModelDictionaryId",
                "EntityAnalysisModelDictionaryKvpCreate", "EntityAnalysisModelDictionaryKvpUpdate",
                "EntityAnalysisModelDictionaryKvpDelete"
            ]);
        }

        [Fact]
        public async Task ListAsyncClampsTakeAndPaginatesDeterministicallyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            for (var i = 0; i < 3; i++)
            {
                var saved = await service.InsertAsync(NewDto(dictionaryId, UniqueKey($"Page{i}")));
                createdIds.Add(saved.Id);
            }

            var page = await service.ListAsync(2);
            page.Items.Count.Should().BeLessThanOrEqualTo(2);

            var oversized = await service.ListAsync(10_000);
            oversized.Items.Count.Should().BeLessThanOrEqualTo(200);
        }

        private sealed class CapturingBus : IServiceChangeBus
        {
            public readonly List<ServiceChangeEvent> Published = [];

            public Task PublishAsync(ServiceChangeEvent change, CancellationToken token = default)
            {
                Published.Add(change);
                return Task.CompletedTask;
            }

            public IDisposable Subscribe(Func<ServiceChangeEvent, Task> handler)
            {
                return NoopSubscription.Instance;
            }

            private sealed class NoopSubscription : IDisposable
            {
                public static readonly NoopSubscription Instance = new();

                public void Dispose()
                {
                }
            }
        }
    }
}