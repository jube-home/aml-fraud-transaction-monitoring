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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.EntityAnalysisModelListValue;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.EntityAnalysisModelListValue;
using Jube.Service.Observability;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModelListValue.Models;
using LinqToDB;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using log4net;

namespace Jube.Test.Service.EntityAnalysisModelListValue
{
    using EntityAnalysisModelListValueService =
        global::Jube.Service.EntityAnalysisModelListValue.EntityAnalysisModelListValueService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EntityAnalysisModelListValueServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdIds = [];
        private readonly List<int> createdListIds = [];
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
                await dbContext.GetTable<EntityAnalysisModelListValueVersion>()
                    .Where(w => w.EntityAnalysisModelListValueId == id).DeleteAsync();
                await dbContext.GetTable<Data.Poco.EntityAnalysisModelListValue>().Where(w => w.Id == id)
                    .DeleteAsync();
            }

            foreach (var listId in createdListIds)
            {
                await dbContext.GetTable<Data.Poco.EntityAnalysisModelListValue>()
                    .Where(w => w.EntityAnalysisModelListId == listId).DeleteAsync();
                await dbContext.GetTable<EntityAnalysisModelListCsvFileUpload>()
                    .Where(w => w.EntityAnalysisModelListId == listId).DeleteAsync();
                await dbContext.GetTable<EntityAnalysisModelListVersion>()
                    .Where(w => w.EntityAnalysisModelListId == listId).DeleteAsync();
                await dbContext.GetTable<Data.Poco.EntityAnalysisModelList>().Where(w => w.Id == listId)
                    .DeleteAsync();
            }

            foreach (var modelId in createdModelIds)
            {
                await dbContext.GetTable<EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == modelId)
                    .DeleteAsync();
                await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
            }
        }

        private static Task<EntityAnalysisModelListValueService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return EntityAnalysisModelListValueService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task<int> CreateParentListAsync(DbContext dbContext, string createdUser)
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

            var listRepository = new EntityAnalysisModelListRepository(dbContext, createdUser);
            var list = await listRepository.InsertAsync(new Data.Poco.EntityAnalysisModelList
            {
                EntityAnalysisModelGuid = model.Guid,
                Name = $"{DatabaseFixture.Prefix}List{Guid.NewGuid():N}"[..40],
                Active = 1,
                Locked = 0
            }).ConfigureAwait(false);
            createdListIds.Add(list.Id);

            return list.Id;
        }

        private static EntityAnalysisModelListValueDto NewDto(int entityAnalysisModelListId, string? listValue)
        {
            return new EntityAnalysisModelListValueDto
            {
                EntityAnalysisModelListId = entityAnalysisModelListId,
                ListValue = listValue
            };
        }

        private static string UniqueValue(string label)
        {
            return $"{DatabaseFixture.Prefix}{label}{Guid.NewGuid():N}"[..40];
        }

        [Fact]
        public async Task InsertPersistsAndReturnsVersionOneAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(listId, UniqueValue("Insert")));
            createdIds.Add(saved.Id);

            saved.Id.Should().BeGreaterThan(0);
            saved.Version.Should().Be(1);
            saved.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            saved.CreatedDate.Should().NotBeNull();
            saved.CreatedDate.Required().Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task GetAllReturnsCreatedRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var value = UniqueValue("GetAll");
            var saved = await service.InsertAsync(NewDto(listId, value));
            createdIds.Add(saved.Id);

            var all = await service.GetAsync();
            all.Should().Contain(d => d.Id == saved.Id && d.ListValue == value);
        }

        [Fact]
        public async Task GetByEntityAnalysisModelListIdReturnsRowsForThatListOrderedByIdAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var listAId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var listBId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var savedA1 = await service.InsertAsync(NewDto(listAId, UniqueValue("ByListA1")));
            createdIds.Add(savedA1.Id);

            var savedA2 = await service.InsertAsync(NewDto(listAId, UniqueValue("ByListA2")));
            createdIds.Add(savedA2.Id);

            var savedB = await service.InsertAsync(NewDto(listBId, UniqueValue("ByListB")));
            createdIds.Add(savedB.Id);

            var forListA = await service.GetByEntityAnalysisModelListIdAsync(listAId);
            forListA.Should().Contain(d => d.Id == savedA1.Id);
            forListA.Should().Contain(d => d.Id == savedA2.Id);
            forListA.Should().NotContain(d => d.Id == savedB.Id);
            forListA.Select(d => d.Id).Should().BeInAscendingOrder();
        }

        [Fact]
        public async Task UpdateIncrementsVersionAndWritesAuditRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(listId, UniqueValue("Update")));
            createdIds.Add(saved.Id);

            var newValue = UniqueValue("UpdateNewValue");
            var dto = NewDto(listId, newValue);
            dto.Id = saved.Id;

            var updated = await service.UpdateAsync(dto);

            updated.Version.Should().Be(2);
            updated.ListValue.Should().Be(newValue);

            var auditRows = await dbContext.GetTable<EntityAnalysisModelListValueVersion>()
                .Where(w => w.EntityAnalysisModelListValueId == saved.Id).CountAsync();
            auditRows.Should().Be(1);
        }

        [Fact]
        public async Task DeleteSoftDeletesAndRowDisappearsFromByListIdAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(listId, UniqueValue("Delete")));
            createdIds.Add(saved.Id);

            await service.DeleteAsync(saved.Id);

            var byList = await service.GetByEntityAnalysisModelListIdAsync(listId);
            byList.Should().NotContain(d => d.Id == saved.Id);
        }

        [Fact]
        public async Task EveryMethodThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync()
        {
            await using var writerDb = fx.GetDbContext();
            var listId = await CreateParentListAsync(writerDb, fx.Seed.UserWithPermission);
            var writer = await BuildServiceAsync(writerDb, fx.Seed.UserWithPermission);
            var seedRow = await writer.InsertAsync(NewDto(listId, UniqueValue("Forbidden")));
            createdIds.Add(seedRow.Id);

            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var beforeCount = await dbContext.GetTable<Data.Poco.EntityAnalysisModelListValue>()
                .CountAsync(w => w.ListValue != null && w.ListValue.StartsWith(DatabaseFixture.Prefix));

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.InsertAsync(NewDto(listId, UniqueValue("Denied"))));
            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.GetByEntityAnalysisModelListIdAsync(listId));

            var updateDto = NewDto(listId, seedRow.ListValue.Required());
            updateDto.Id = seedRow.Id;
            await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateAsync(updateDto));
            await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteAsync(seedRow.Id));

            var afterCount = await dbContext.GetTable<Data.Poco.EntityAnalysisModelListValue>()
                .CountAsync(w => w.ListValue != null && w.ListValue.StartsWith(DatabaseFixture.Prefix));
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
                EntityAnalysisModelListValueService.CreateAsync(dbContext, userName, log, localizers,
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
            var listId = await CreateParentListAsync(ownerDb, fx.Seed.UserWithPermission);
            var owner = await BuildServiceAsync(ownerDb, fx.Seed.UserWithPermission);
            var saved = await owner.InsertAsync(NewDto(listId, UniqueValue("Isolation")));
            createdIds.Add(saved.Id);

            await using var dbContext = fx.GetDbContext();
            var otherTenant = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            var updateDto = NewDto(listId, saved.ListValue.Required());
            updateDto.Id = saved.Id;

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => otherTenant.UpdateAsync(updateDto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "EntityAnalysisModelListIdNotFound");

            await Assert.ThrowsAsync<NotFoundException>(() => otherTenant.DeleteAsync(saved.Id));

            var byList = await owner.GetByEntityAnalysisModelListIdAsync(listId);
            byList.Should().Contain(d => d.Id == saved.Id);
        }

        [Fact]
        public async Task GetByEntityAnalysisModelListIdAsTenantBNeverContainsTenantARowsAsync()
        {
            await using var ownerDb = fx.GetDbContext();
            var listId = await CreateParentListAsync(ownerDb, fx.Seed.UserWithPermission);
            var owner = await BuildServiceAsync(ownerDb, fx.Seed.UserWithPermission);
            var saved = await owner.InsertAsync(NewDto(listId, UniqueValue("TenantAOnly")));
            createdIds.Add(saved.Id);

            await using var dbContext = fx.GetDbContext();
            var otherTenant = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            var forListAsTenantB = await otherTenant.GetByEntityAnalysisModelListIdAsync(listId);

            forListAsTenantB.Should().NotContain(d => d.Id == saved.Id);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ListValueRequiredRejectsNullEmptyOrWhitespaceAsync(string? listValue)
        {
            await using var dbContext = fx.GetDbContext();
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(listId, listValue);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e =>
                e.PropertyName == nameof(EntityAnalysisModelListValueDto.ListValue) &&
                e.ErrorCode == "ListValueNotEmpty");
        }

        [Fact]
        public async Task ListValueOverMaximumLengthIsRejectedWithErrorCodeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(listId, new string('v', 513));

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "ListValueMaximumLength");
        }

        [Fact]
        public async Task InvalidEntityAnalysisModelListIdIsRejectedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(0, UniqueValue("BadListId"));

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "EntityAnalysisModelListIdInvalid");
        }

        [Fact]
        public async Task NonExistentEntityAnalysisModelListIdIsRejectedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(int.MaxValue - 1, UniqueValue("MissingListId"));

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "EntityAnalysisModelListIdNotFound");
        }

        [Fact]
        public async Task DeleteExpiryDateInThePastIsRejectedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(listId, UniqueValue("PastExpiry"));
            dto.DeleteExpiryDate = DateTimeOffset.UtcNow.AddDays(-1);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "DeleteExpiryDateMustBeFuture");
        }

        [Fact]
        public async Task DeleteExpiryDateInTheFutureIsAcceptedAndRoundTripsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var expiry = DateTimeOffset.UtcNow.AddDays(1);
            var dto = NewDto(listId, UniqueValue("FutureExpiry"));
            dto.DeleteExpiryDate = expiry;

            var saved = await service.InsertAsync(dto);
            createdIds.Add(saved.Id);

            saved.DeleteExpiryDate.Should().BeCloseTo(expiry.UtcDateTime, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task ExpiredValueDisappearsFromByListIdAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(listId, UniqueValue("AboutToExpire")));
            createdIds.Add(saved.Id);

            await dbContext.GetTable<Data.Poco.EntityAnalysisModelListValue>()
                .Where(w => w.Id == saved.Id)
                .Set(s => s.DeleteExpiryDate, DateTime.UtcNow.AddSeconds(-1))
                .UpdateAsync();

            var byList = await service.GetByEntityAnalysisModelListIdAsync(listId);
            byList.Should().NotContain(d => d.Id == saved.Id);
        }

        [Fact]
        public async Task TamperedIdentityAndAuditFieldsOnInsertHaveNoEffectAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = NewDto(listId, UniqueValue("Tamper"));
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
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(listId, UniqueValue("TamperUpdate")));
            createdIds.Add(saved.Id);

            var dto = NewDto(listId, saved.ListValue.Required());
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
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(listId, UniqueValue("PreDeleted")));
            createdIds.Add(saved.Id);

            await service.DeleteAsync(saved.Id);

            var dto = NewDto(listId, saved.ListValue.Required());
            dto.Id = saved.Id;
            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task UpdateOfNeverExistedIdThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = NewDto(listId, UniqueValue("Missing"));
            dto.Id = int.MaxValue - 1;

            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task DeleteOfMissingOrAlreadyDeletedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(int.MaxValue - 1));

            var saved = await service.InsertAsync(NewDto(listId, UniqueValue("DoubleDelete")));
            createdIds.Add(saved.Id);
            await service.DeleteAsync(saved.Id);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(saved.Id));
        }

        [Fact]
        public async Task GetByIdMatchesOnItsOwnIdNotItsParentListIdAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(listId, UniqueValue("GetById")));
            createdIds.Add(saved.Id);

            var byOwnId = await service.GetByIdAsync(saved.Id);
            byOwnId.Should().NotBeNull();
            byOwnId.Required().Id.Should().Be(saved.Id);

            if (listId != saved.Id)
            {
                var byParentListId = await service.GetByIdAsync(listId);
                byParentListId.Should().BeNull();
            }
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
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            var saved = await service.InsertAsync(NewDto(listId, UniqueValue("LogInsert")));
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
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var log = new TestLog(false);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            var saved = await service.InsertAsync(NewDto(listId, UniqueValue("Gated")));
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
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(listId, UniqueValue("Span")));
            createdIds.Add(saved.Id);

            var createSpan = activities.Should()
                .ContainSingle(a => a.OperationName == "EntityAnalysisModelListValue.Create").Subject;
            createSpan.GetTagItem("jube.outcome").Should().Be("ok");
            createSpan.GetTagItem("jube.entity.id").Should().Be(saved.Id);
        }

        [Fact]
        public async Task EachCallRecordsOneDurationMeasurementAsync()
        {
            using var collector = new MetricCollector<double>(ServiceDiagnostics.OperationDuration);

            await using var dbContext = fx.GetDbContext();
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(listId, UniqueValue("Metric")));
            createdIds.Add(saved.Id);

            collector.GetMeasurementSnapshot().Should()
                .ContainSingle(m => (string)m.Tags["operation"].Required() == "Create");
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
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                serviceChangeBus: serviceChangeBus);

            var saved = await service.InsertAsync(NewDto(listId, UniqueValue("Reactive")));
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
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                serviceChangeBus: serviceChangeBus);

            await service.GetAsync();
            await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.InsertAsync(NewDto(listId, "")));

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
                "EntityAnalysisModelListValueList", "EntityAnalysisModelListValueGet",
                "EntityAnalysisModelListValueGetByEntityAnalysisModelListId",
                "EntityAnalysisModelListValueCreate", "EntityAnalysisModelListValueUpdate",
                "EntityAnalysisModelListValueDelete", "EntityAnalysisModelListValueUploadCsv"
            ]);
        }

        [Fact]
        public async Task ListAsyncClampsTakeAndPaginatesDeterministicallyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            for (var i = 0; i < 3; i++)
            {
                var saved = await service.InsertAsync(NewDto(listId, UniqueValue($"Page{i}")));
                createdIds.Add(saved.Id);
            }

            var page = await service.ListAsync(2);
            page.Items.Count.Should().BeLessThanOrEqualTo(2);

            var oversized = await service.ListAsync(10_000);
            oversized.Items.Count.Should().BeLessThanOrEqualTo(200);
        }

        private static EntityAnalysisModelListValueCsvFileDto CsvFile(string name, string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            return new EntityAnalysisModelListValueCsvFileDto
            {
                FileName = name, Length = bytes.Length, Content = new MemoryStream(bytes)
            };
        }

        private static Task<List<Data.Poco.EntityAnalysisModelListValue>> ValuesAsync(DbContext dbContext,
            int listId)
        {
            return dbContext.GetTable<Data.Poco.EntityAnalysisModelListValue>()
                .Where(w => w.EntityAnalysisModelListId == listId).OrderBy(o => o.Id).ToListAsync();
        }

        private static Task<List<EntityAnalysisModelListCsvFileUpload>> UploadsAsync(DbContext dbContext,
            int listId)
        {
            return dbContext.GetTable<EntityAnalysisModelListCsvFileUpload>()
                .Where(w => w.EntityAnalysisModelListId == listId).OrderBy(o => o.Id).ToListAsync();
        }

        [Fact]
        public async Task UploadCsvLoadsValidRowsAndRecordsUploadAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var file = CsvFile("values.csv", "alpha\r\nbeta,\ngamma,not-a-date\n");

            var results = await service.UploadCsvAsync(listId, [file]);

            var result = results.Should().ContainSingle().Subject;
            result.Records.Should().Be(3);
            result.Errors.Should().Be(0);
            result.FileName.Should().Be("values.csv");
            result.Length.Should().Be(file.Length);
            result.EntityAnalysisModelListId.Should().Be(listId);

            var values = await ValuesAsync(dbContext, listId);
            values.Select(v => v.ListValue).Should().Equal("alpha", "beta", "gamma");
            values.Should().OnlyContain(v => v.DeleteExpiryDate == null && v.CreatedUser == fx.Seed.UserWithPermission);

            var uploads = await UploadsAsync(dbContext, listId);
            var upload = uploads.Should().ContainSingle().Subject;
            upload.Id.Should().Be(result.Id);
            upload.FileName.Should().Be("values.csv");
            upload.Records.Should().Be(3);
            upload.Errors.Should().Be(0);
            upload.Length.Should().Be(file.Length);
            upload.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
        }

        [Fact]
        public async Task UploadCsvParsesRoundTripExpiryDateAsUtcAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var expiry = DateTime.UtcNow.AddDays(30);

            await service.UploadCsvAsync(listId,
            [
                CsvFile("expiry.csv",
                    $"withdate,{expiry.ToString("O", System.Globalization.CultureInfo.InvariantCulture)}\n")
            ]);

            var value = (await ValuesAsync(dbContext, listId)).Should().ContainSingle().Subject;
            value.DeleteExpiryDate.Should().NotBeNull();
            value.DeleteExpiryDate.Required().Should().BeCloseTo(expiry, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task UploadCsvCountsBlankOverlongAndDatabaseRejectedRowsAsErrorsAndKeepsGoodRowsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var content = "good1\n\n   ,x\n" + new string('z', 513) + "\nbad\0nul\ngood2\n";

            var results = await service.UploadCsvAsync(listId, [CsvFile("mixed.csv", content)]);

            results.Should().ContainSingle();
            results[0].Records.Should().Be(2);
            results[0].Errors.Should().Be(4);
            (await ValuesAsync(dbContext, listId)).Select(v => v.ListValue).Should().Equal("good1", "good2");
            (await UploadsAsync(dbContext, listId)).Should().ContainSingle(u => u.Records == 2 && u.Errors == 4);
        }

        [Fact]
        public async Task UploadCsvEmptyFileRecordsZeroRowsAndStillWritesUploadAsync()
        {
            var serviceChangeBus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                serviceChangeBus: serviceChangeBus);

            var results = await service.UploadCsvAsync(listId, [CsvFile("empty.csv", "")]);

            results[0].Records.Should().Be(0);
            results[0].Errors.Should().Be(0);
            (await ValuesAsync(dbContext, listId)).Should().BeEmpty();
            (await UploadsAsync(dbContext, listId)).Should().ContainSingle(u => u.Length == 0 && u.Records == 0);
            serviceChangeBus.Published.Should().ContainSingle();
        }

        [Fact]
        public async Task UploadCsvNoFilesWritesNothingAndPublishesNothingAsync()
        {
            var serviceChangeBus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                serviceChangeBus: serviceChangeBus);

            (await service.UploadCsvAsync(listId, [])).Should().BeEmpty();
            (await service.UploadCsvAsync(listId, null)).Should().BeEmpty();

            (await UploadsAsync(dbContext, listId)).Should().BeEmpty();
            serviceChangeBus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task UploadCsvMultipleFilesLoadsAllPublishesOnceAndAuditsOnceAsync()
        {
            var serviceChangeBus = new CapturingBus();
            var auditLog = new TestLog();
            await using var dbContext = fx.GetDbContext();
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                auditLog: auditLog, serviceChangeBus: serviceChangeBus);

            var results = await service.UploadCsvAsync(listId,
                [CsvFile("one.csv", "a1\na2\n"), CsvFile("two.csv", "b1\n")]);

            results.Select(r => r.Records).Should().Equal(2, 1);
            (await ValuesAsync(dbContext, listId)).Should().HaveCount(3);
            var uploads = await UploadsAsync(dbContext, listId);
            uploads.Select(u => u.FileName).Should().Equal("one.csv", "two.csv");

            serviceChangeBus.Published.Should().ContainSingle();
            serviceChangeBus.Published[0].Kind.Should().Be(ServiceChangeKind.Created);
            serviceChangeBus.Published[0].EntityId.Should().Be(results[1].Id);
            auditLog.Entries.Should().ContainSingle();
            auditLog.Entries[0].Message.Should().Contain("op=UploadCsv").And.Contain("rows=3");
        }

        [Fact]
        public async Task UploadCsvForeignTenantParentBehavesLikeNotFoundAndWritesNothingAsync()
        {
            var serviceChangeBus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var foreignListId = await CreateParentListAsync(dbContext, fx.Seed.UserTenantB);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                serviceChangeBus: serviceChangeBus);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.UploadCsvAsync(foreignListId, [CsvFile("x.csv", "intruder\n")]));

            (await ValuesAsync(dbContext, foreignListId)).Should().BeEmpty();
            (await UploadsAsync(dbContext, foreignListId)).Should().BeEmpty();
            serviceChangeBus.Published.Should().BeEmpty();

            var ownerService = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            (await ownerService.UploadCsvAsync(foreignListId, [CsvFile("y.csv", "legit\n")]))[0].Records
                .Should().Be(1);
        }

        [Fact]
        public async Task UploadCsvMissingListIsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.UploadCsvAsync(int.MaxValue, [CsvFile("x.csv", "a\n")]));
            await Assert.ThrowsAsync<NotFoundException>(() => service.UploadCsvAsync(0, [CsvFile("x.csv", "a\n")]));
        }

        [Fact]
        public async Task UploadCsvWithoutPermissionIsForbiddenWritesNothingAndWarnsNotErrorsAsync()
        {
            var serviceChangeBus = new CapturingBus();
            var log = new TestLog();
            await using var dbContext = fx.GetDbContext();
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log,
                serviceChangeBus: serviceChangeBus);

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.UploadCsvAsync(listId, [CsvFile("x.csv", "a\n")]));

            ex.Code.Should().Be("PermissionDenied");
            ex.RequiredSpecifications.Should().Equal(3);
            (await ValuesAsync(dbContext, listId)).Should().BeEmpty();
            serviceChangeBus.Published.Should().BeEmpty();
            log.Entries.Should().Contain(e => e.Level == "WARN");
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task UploadCsvBlankUserIsNotAuthenticatedAsync(string? userName)
        {
            await using var dbContext = fx.GetDbContext();
            await Assert.ThrowsAsync<NotAuthenticatedException>(() => BuildServiceAsync(dbContext, userName));
        }

        [Fact]
        public async Task UploadCsvCancelledMidUploadRollsEverythingBackAsync()
        {
            var serviceChangeBus = new CapturingBus();
            using var cts = new CancellationTokenSource();
            await using var dbContext = fx.GetDbContext();
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                serviceChangeBus: serviceChangeBus);

            var second = CsvFile("second.csv", "b1\n");
            second.Content = new HookStream(second.Content, cts.Cancel);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                service.UploadCsvAsync(listId, [CsvFile("first.csv", "a1\na2\n"), second], cts.Token));

            (await ValuesAsync(dbContext, listId)).Should().BeEmpty();
            (await UploadsAsync(dbContext, listId)).Should().BeEmpty();
            serviceChangeBus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task UploadCsvPreCancelledTokenThrowsAndWritesNothingAsync()
        {
            var serviceChangeBus = new CapturingBus();
            using var cts = new CancellationTokenSource();
            await using var dbContext = fx.GetDbContext();
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                serviceChangeBus: serviceChangeBus);
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                service.UploadCsvAsync(listId, [CsvFile("x.csv", "a\n")], cts.Token));

            (await ValuesAsync(dbContext, listId)).Should().BeEmpty();
            serviceChangeBus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task UploadCsvUnexpectedFailureRollsBackLogsErrorAndPublishesNothingAsync()
        {
            var serviceChangeBus = new CapturingBus();
            var log = new TestLog();
            await using var dbContext = fx.GetDbContext();
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log,
                serviceChangeBus: serviceChangeBus);

            var second = CsvFile("second.csv", "b1\n");
            second.Content = new HookStream(second.Content, () => throw new IOException("disk gone"));

            await Assert.ThrowsAsync<IOException>(() =>
                service.UploadCsvAsync(listId, [CsvFile("first.csv", "a1\na2\n"), second]));

            (await ValuesAsync(dbContext, listId)).Should().BeEmpty();
            (await UploadsAsync(dbContext, listId)).Should().BeEmpty();
            serviceChangeBus.Published.Should().BeEmpty();
            log.Entries.Should().Contain(e => e.Level == "ERROR" && e.Exception != null);

            var retry = await service.UploadCsvAsync(listId, [CsvFile("retry.csv", "c1\n")]);
            retry[0].Records.Should().Be(1);
        }
    }
}