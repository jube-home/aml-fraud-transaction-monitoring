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
using Jube.Dto.EntityAnalysisModelSuppression;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.EntityAnalysisModelSuppression;
using Jube.Service.Exceptions.EntityAnalysisModelSuppression;
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

namespace Jube.Test.Service.EntityAnalysisModelSuppression
{
    using EntityAnalysisModelSuppressionService = EntityAnalysisModelSuppressionService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EntityAnalysisModelSuppressionServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

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
                await dbContext.GetTable<EntityAnalysisModelSuppressionVersion>()
                    .Where(w => w.EntityAnalysisModelSuppressionId == id).DeleteAsync();
                await dbContext.GetTable<Data.Poco.EntityAnalysisModelSuppression>().Where(w => w.Id == id)
                    .DeleteAsync();
            }

            foreach (var modelId in createdModelIds)
            {
                await dbContext.GetTable<EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == modelId)
                    .DeleteAsync();
                await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
            }
        }

        private static Task<EntityAnalysisModelSuppressionService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return EntityAnalysisModelSuppressionService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task<(int Id, Guid Guid)> CreateParentModelAsync(DbContext dbContext, string createdUser)
        {
            var repository = new EntityAnalysisModelRepository(dbContext, createdUser);
            var saved = await repository.InsertAsync(new Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0
            }).ConfigureAwait(false);

            createdModelIds.Add(saved.Id);
            return (saved.Id, saved.Guid);
        }

        private static EntityAnalysisModelSuppressionDto NewDto(Guid entityAnalysisModelGuid, string key,
            string keyValue)
        {
            return new EntityAnalysisModelSuppressionDto
            {
                EntityAnalysisModelGuid = entityAnalysisModelGuid,
                SuppressionKey = key,
                SuppressionKeyValue = keyValue
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
            var (_, modelGuid) = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(modelGuid, "IP", UniqueValue("Insert")));
            createdIds.Add(saved.Id);

            saved.Id.Should().BeGreaterThan(0);
            saved.Version.Should().Be(1);
            saved.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            saved.CreatedDate.Should().NotBeNull();
            saved.CreatedDate!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task GetAllReturnsCreatedRowAndGetByIdReturnsItAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var keyValue = UniqueValue("GetAll");
            var saved = await service.InsertAsync(NewDto(modelGuid, "IP", keyValue));
            createdIds.Add(saved.Id);

            var all = await service.GetAsync();
            all.Should().Contain(d => d.Id == saved.Id && d.SuppressionKeyValue == keyValue);

            var byId = await service.GetByIdAsync(saved.Id);
            byId.Should().NotBeNull();
            byId!.SuppressionKeyValue.Should().Be(keyValue);
        }

        [Fact]
        public async Task GetByEntityAnalysisModelIdReturnsOnlyRowsForThatModelAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (modelAId, modelAGuid) = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var (_, modelBGuid) = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var savedA = await service.InsertAsync(NewDto(modelAGuid, "IP", UniqueValue("ByModelA")));
            createdIds.Add(savedA.Id);
            var savedB = await service.InsertAsync(NewDto(modelBGuid, "IP", UniqueValue("ByModelB")));
            createdIds.Add(savedB.Id);

            var forModelA = await service.GetByEntityAnalysisModelIdAsync(modelAId);
            forModelA.Should().Contain(d => d.Id == savedA.Id);
            forModelA.Should().NotContain(d => d.Id == savedB.Id);
        }

        [Fact]
        public async Task UpdateTogglesOffExistingRowAndOnANewRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var key = "IP";
            var keyValue = UniqueValue("Toggle");

            var toggleOn = NewDto(modelGuid, key, keyValue);
            var firstResult = await service.UpdateAsync(toggleOn);
            firstResult.Id.Should().BeGreaterThan(0);
            createdIds.Add(firstResult.Id);

            var stillOn = await service.GetByEntityAnalysisModelIdAsync(
                (await dbContext.EntityAnalysisModel.Where(w => w.Guid == modelGuid).SingleAsync()).Id);
            stillOn.Should().ContainSingle(d => d.SuppressionKeyValue == keyValue);

            var toggleOff = NewDto(modelGuid, key, keyValue);
            await service.UpdateAsync(toggleOff);

            var nowOff = await service.GetByEntityAnalysisModelIdAsync(
                (await dbContext.EntityAnalysisModel.Where(w => w.Guid == modelGuid).SingleAsync()).Id);
            nowOff.Should().BeEmpty();
        }

        [Fact]
        public async Task UpdateDeleteExpiryDateUpdatesActiveRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var key = "IP";
            var keyValue = UniqueValue("Expiry");
            var saved = await service.UpdateAsync(NewDto(modelGuid, key, keyValue));
            createdIds.Add(saved.Id);

            var expiry = DateTimeOffset.UtcNow.AddDays(1);
            var updateDto = NewDto(modelGuid, key, keyValue);
            updateDto.DeleteExpiryDate = expiry;

            var updated = await service.UpdateDeleteExpiryDateAsync(updateDto);
            updated.DeleteExpiryDate.Should().BeCloseTo(expiry, TimeSpan.FromSeconds(2));
            updated.Version.Should().Be(2);
        }

        [Fact]
        public async Task UpdateDeleteExpiryDateForMissingRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = NewDto(modelGuid, "IP", UniqueValue("NoExpiry"));
            dto.DeleteExpiryDate = DateTimeOffset.UtcNow.AddDays(1);

            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateDeleteExpiryDateAsync(dto));
        }

        [Fact]
        public async Task DeleteSoftDeletesAndRowDisappearsFromReadsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(modelGuid, "IP", UniqueValue("Delete")));
            createdIds.Add(saved.Id);

            await service.DeleteAsync(saved.Id);

            var byId = await service.GetByIdAsync(saved.Id);
            byId.Should().BeNull();
        }

        [Fact]
        public async Task DeleteOfMissingRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(int.MaxValue - 1));
        }

        [Fact]
        public async Task EveryMethodThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync()
        {
            await using var writerDb = fx.GetDbContext();
            var (modelId, modelGuid) = await CreateParentModelAsync(writerDb, fx.Seed.UserWithPermission);
            var writer = await BuildServiceAsync(writerDb, fx.Seed.UserWithPermission);
            var seedRow = await writer.InsertAsync(NewDto(modelGuid, "IP", UniqueValue("Forbidden")));
            createdIds.Add(seedRow.Id);

            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var beforeCount = await dbContext.GetTable<Data.Poco.EntityAnalysisModelSuppression>()
                .CountAsync(w => w.SuppressionKeyValue!.StartsWith(DatabaseFixture.Prefix));

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.InsertAsync(NewDto(modelGuid, "IP", UniqueValue("Denied"))));
            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByIdAsync(seedRow.Id));
            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync());
            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByEntityAnalysisModelIdAsync(modelId));
            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.UpdateAsync(NewDto(modelGuid, "IP", UniqueValue("DeniedUpdate"))));
            await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteAsync(seedRow.Id));

            var afterCount = await dbContext.GetTable<Data.Poco.EntityAnalysisModelSuppression>()
                .CountAsync(w => w.SuppressionKeyValue!.StartsWith(DatabaseFixture.Prefix));
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
                EntityAnalysisModelSuppressionService.CreateAsync(dbContext, userName, log, localizers,
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
        public async Task UserInTenantBCannotGetOrDeleteTenantARowAsync()
        {
            await using var ownerDb = fx.GetDbContext();
            var (_, modelGuid) = await CreateParentModelAsync(ownerDb, fx.Seed.UserWithPermission);
            var owner = await BuildServiceAsync(ownerDb, fx.Seed.UserWithPermission);
            var saved = await owner.InsertAsync(NewDto(modelGuid, "IP", UniqueValue("Isolation")));
            createdIds.Add(saved.Id);

            await using var dbContext = fx.GetDbContext();
            var otherTenant = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            var byId = await otherTenant.GetByIdAsync(saved.Id);
            byId.Should().BeNull();

            await Assert.ThrowsAsync<NotFoundException>(() => otherTenant.DeleteAsync(saved.Id));

            var stillThere = await owner.GetByIdAsync(saved.Id);
            stillThere.Should().NotBeNull();
        }

        [Fact]
        public async Task GetAllAsTenantBNeverContainsTenantARowsAsync()
        {
            await using var ownerDb = fx.GetDbContext();
            var (_, modelGuid) = await CreateParentModelAsync(ownerDb, fx.Seed.UserWithPermission);
            var owner = await BuildServiceAsync(ownerDb, fx.Seed.UserWithPermission);
            var saved = await owner.InsertAsync(NewDto(modelGuid, "IP", UniqueValue("TenantAOnly")));
            createdIds.Add(saved.Id);

            await using var dbContext = fx.GetDbContext();
            var otherTenant = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            var all = await otherTenant.GetAsync();

            all.Should().NotContain(d => d.Id == saved.Id);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SuppressionKeyRequiredRejectsNullEmptyOrWhitespaceAsync(string? key)
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelGuid, key!, UniqueValue("BadKey"));

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e =>
                e.PropertyName == nameof(EntityAnalysisModelSuppressionDto.SuppressionKey) &&
                e.ErrorCode == "SuppressionKeyNotEmpty");
        }

        [Fact]
        public async Task SuppressionKeyOverMaximumLengthIsRejectedWithErrorCodeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelGuid, new string('k', 257), UniqueValue("BadKeyLen"));

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "SuppressionKeyMaximumLength");
        }

        [Fact]
        public async Task SuppressionKeyValueRequiredRejectsEmptyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelGuid, "IP", "");

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "SuppressionKeyValueNotEmpty");
        }

        [Fact]
        public async Task SuppressionKeyValueOverMaximumLengthIsRejectedWithErrorCodeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelGuid, "IP", new string('v', 257));

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "SuppressionKeyValueMaximumLength");
        }

        [Fact]
        public async Task EmptyEntityAnalysisModelGuidIsRejectedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(Guid.Empty, "IP", UniqueValue("BadGuid"));

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "EntityAnalysisModelGuidNotEmpty");
        }

        [Fact]
        public async Task NonExistentEntityAnalysisModelGuidIsRejectedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(Guid.NewGuid(), "IP", UniqueValue("MissingModel"));

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "EntityAnalysisModelGuidNotFound");
        }

        [Fact]
        public async Task DeleteExpiryDateInThePastIsRejectedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelGuid, "IP", UniqueValue("PastExpiry"));
            dto.DeleteExpiryDate = DateTimeOffset.UtcNow.AddDays(-1);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "DeleteExpiryDateMustBeFuture");
        }

        [Fact]
        public async Task TamperedIdentityAndAuditFieldsOnInsertHaveNoEffectAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = NewDto(modelGuid, "IP", UniqueValue("Tamper"));
            dto.CreatedUser = "someone-else";
            dto.Version = 999;

            var saved = await service.InsertAsync(dto);
            createdIds.Add(saved.Id);

            saved.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            saved.Version.Should().Be(1);
        }

        [Fact]
        public async Task GetByIdForMissingIdReturnsNullNotExceptionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetByIdAsync(int.MaxValue - 1);
            result.Should().BeNull();
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
            var (_, modelGuid) = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            var saved = await service.InsertAsync(NewDto(modelGuid, "IP", UniqueValue("LogInsert")));
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
            var (_, modelGuid) = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var log = new TestLog(false);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            var saved = await service.InsertAsync(NewDto(modelGuid, "IP", UniqueValue("Gated")));
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
            var (_, modelGuid) = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(modelGuid, "IP", UniqueValue("Span")));
            createdIds.Add(saved.Id);

            var createSpan = activities.Should()
                .ContainSingle(a => a.OperationName == "EntityAnalysisModelSuppression.Create").Subject;
            createSpan.GetTagItem("jube.outcome").Should().Be("ok");
            createSpan.GetTagItem("jube.entity.id").Should().Be(saved.Id);
        }

        [Fact]
        public async Task EachCallRecordsOneDurationMeasurementAsync()
        {
            using var collector = new MetricCollector<double>(ServiceDiagnostics.OperationDuration);

            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(modelGuid, "IP", UniqueValue("Metric")));
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
            var (_, modelGuid) = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                serviceChangeBus: serviceChangeBus);

            var saved = await service.InsertAsync(NewDto(modelGuid, "IP", UniqueValue("Reactive")));
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
            var (_, modelGuid) = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                serviceChangeBus: serviceChangeBus);

            await service.GetAsync();
            await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.InsertAsync(NewDto(modelGuid, "IP", "")));

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
                "EntityAnalysisModelSuppressionList", "EntityAnalysisModelSuppressionGet",
                "EntityAnalysisModelSuppressionGetByEntityAnalysisModelId",
                "EntityAnalysisModelSuppressionCreate", "EntityAnalysisModelSuppressionUpdate",
                "EntityAnalysisModelSuppressionUpdateDeleteExpiryDate", "EntityAnalysisModelSuppressionDelete"
            ]);
        }

        [Fact]
        public async Task ListAsyncClampsTakeAndPaginatesDeterministicallyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, modelGuid) = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            for (var i = 0; i < 3; i++)
            {
                var saved = await service.InsertAsync(NewDto(modelGuid, "IP", UniqueValue($"Page{i}")));
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