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
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.Repository.ExhaustiveSearchInstancePromotedTrialInstance;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Repository.ExhaustiveSearchInstancePromotedTrialInstance;
using Jube.Service.Observability;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.Repository.ExhaustiveSearchInstancePromotedTrialInstance.Models;
using LinqToDB;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using log4net;

namespace Jube.Test.Service.Repository.ExhaustiveSearchInstancePromotedTrialInstance
{
    using ExhaustiveSearchInstancePromotedTrialInstanceService =
        global::Jube.Service.Repository.ExhaustiveSearchInstancePromotedTrialInstance.
        ExhaustiveSearchInstancePromotedTrialInstanceService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ExhaustiveSearchInstancePromotedTrialInstanceServiceTests(DatabaseFixture fx)
        : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdModelIds = [];
        private readonly List<int> createdPromotedTrialInstanceIds = [];
        private readonly List<int> createdSearchInstanceIds = [];
        private readonly List<int> createdTrialInstanceIds = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            foreach (var id in createdPromotedTrialInstanceIds)
            {
                await dbContext.GetTable<Data.Poco.ExhaustiveSearchInstancePromotedTrialInstance>()
                    .Where(w => w.Id == id).DeleteAsync();
            }

            foreach (var id in createdTrialInstanceIds)
            {
                await dbContext.GetTable<ExhaustiveSearchInstanceTrialInstance>().Where(w => w.Id == id)
                    .DeleteAsync();
            }

            foreach (var id in createdSearchInstanceIds)
            {
                await dbContext.GetTable<ExhaustiveSearchInstanceVersion>()
                    .Where(w => w.ExhaustiveSearchInstanceId == id).DeleteAsync();
                await dbContext.GetTable<Data.Poco.ExhaustiveSearchInstance>().Where(w => w.Id == id).DeleteAsync();
            }

            foreach (var modelId in createdModelIds)
            {
                await dbContext.GetTable<EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == modelId)
                    .DeleteAsync();
                await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
            }
        }

        private static Task<ExhaustiveSearchInstancePromotedTrialInstanceService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return ExhaustiveSearchInstancePromotedTrialInstanceService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task<int> CreatePromotedTrialInstanceAsync(DbContext dbContext, string createdUser,
            bool active = false)
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

            var searchInstanceRepository = new ExhaustiveSearchInstanceRepository(dbContext, createdUser);
            var searchInstance = await searchInstanceRepository.InsertAsync(new Data.Poco.ExhaustiveSearchInstance
            {
                Name = $"{DatabaseFixture.Prefix}Search{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                EntityAnalysisModelId = model.Id,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                StatusId = 0
            }).ConfigureAwait(false);
            createdSearchInstanceIds.Add(searchInstance.Id);

            var trialInstanceRepository = new ExhaustiveSearchInstanceTrialInstanceRepository(dbContext);
            var trialInstance = await trialInstanceRepository.InsertAsync(new ExhaustiveSearchInstanceTrialInstance
            {
                ExhaustiveSearchInstanceId = searchInstance.Id,
                CreatedDate = DateTime.UtcNow
            }).ConfigureAwait(false);
            createdTrialInstanceIds.Add(trialInstance.Id);

            var promotedTrialInstanceRepository =
                new ExhaustiveSearchInstancePromotedTrialInstanceRepository(dbContext);
            var promotedTrialInstance = await promotedTrialInstanceRepository.InsertAsync(
                new Data.Poco.ExhaustiveSearchInstancePromotedTrialInstance
                {
                    ExhaustiveSearchInstanceTrialInstanceId = trialInstance.Id,
                    Active = (byte)(active ? 1 : 0),
                    Score = 0.5,
                    Json = "{}"
                }).ConfigureAwait(false);
            createdPromotedTrialInstanceIds.Add(promotedTrialInstance.Id);

            return promotedTrialInstance.Id;
        }

        private async Task<byte?> ReadActiveAsync(DbContext dbContext, int id)
        {
            var row = await dbContext.GetTable<Data.Poco.ExhaustiveSearchInstancePromotedTrialInstance>()
                .FirstAsync(w => w.Id == id).ConfigureAwait(false);
            return row.Active;
        }

        [Fact]
        public async Task UpdateActiveSetsActiveTrueAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var id = await CreatePromotedTrialInstanceAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await service.UpdateActiveAsync(
                new ExhaustiveSearchInstancePromotedTrialInstanceDto { Id = id, Active = true });

            var active = await ReadActiveAsync(dbContext, id);
            active.Should().Be(1);
        }

        [Fact]
        public async Task UpdateActiveSetsActiveFalseAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var id = await CreatePromotedTrialInstanceAsync(dbContext, fx.Seed.UserWithPermission, true);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await service.UpdateActiveAsync(
                new ExhaustiveSearchInstancePromotedTrialInstanceDto { Id = id, Active = false });

            var active = await ReadActiveAsync(dbContext, id);
            active.Should().Be(0);
        }

        [Fact]
        public async Task UpdateActiveIsIdempotentAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var id = await CreatePromotedTrialInstanceAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await service.UpdateActiveAsync(
                new ExhaustiveSearchInstancePromotedTrialInstanceDto { Id = id, Active = true });
            await service.UpdateActiveAsync(
                new ExhaustiveSearchInstancePromotedTrialInstanceDto { Id = id, Active = true });

            var active = await ReadActiveAsync(dbContext, id);
            active.Should().Be(1);
        }

        [Fact]
        public async Task UpdateOfSoftDeletedRowStillSucceedsBecauseTheRepositoryDoesNotFilterDeletedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var id = await CreatePromotedTrialInstanceAsync(dbContext, fx.Seed.UserWithPermission);

            await dbContext.GetTable<Data.Poco.ExhaustiveSearchInstancePromotedTrialInstance>()
                .Where(w => w.Id == id).Set(s => s.Deleted, (byte)1).UpdateAsync();

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await service.UpdateActiveAsync(
                new ExhaustiveSearchInstancePromotedTrialInstanceDto { Id = id, Active = true });

            var active = await ReadActiveAsync(dbContext, id);
            active.Should().Be(1);
        }

        [Fact]
        public async Task UpdateOfMissingIdThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
                service.UpdateActiveAsync(
                    new ExhaustiveSearchInstancePromotedTrialInstanceDto { Id = int.MaxValue - 1, Active = true }));

            ex.Code.Should().Be("NotFound");
        }

        [Fact]
        public async Task UserInTenantBCannotUpdateTenantARowAsync()
        {
            await using var ownerDb = fx.GetDbContext();
            var id = await CreatePromotedTrialInstanceAsync(ownerDb, fx.Seed.UserWithPermission);

            await using var dbContext = fx.GetDbContext();
            var otherTenant = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                otherTenant.UpdateActiveAsync(
                    new ExhaustiveSearchInstancePromotedTrialInstanceDto { Id = id, Active = true }));

            var active = await ReadActiveAsync(ownerDb, id);
            active.Should().Be(0);
        }

        [Fact]
        public async Task EveryCallThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync()
        {
            await using var ownerDb = fx.GetDbContext();
            var id = await CreatePromotedTrialInstanceAsync(ownerDb, fx.Seed.UserWithPermission);

            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.UpdateActiveAsync(
                    new ExhaustiveSearchInstancePromotedTrialInstanceDto { Id = id, Active = true }));
            ex.Code.Should().Be("PermissionDenied");

            var active = await ReadActiveAsync(ownerDb, id);
            active.Should().Be(0);
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
                ExhaustiveSearchInstancePromotedTrialInstanceService.CreateAsync(dbContext, userName, log,
                    localizers, new NullServiceChangeBus(), TestLog.NoOp));

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

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task InvalidIdIsRejectedWithErrorCodeAsync(int id)
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.UpdateActiveAsync(
                    new ExhaustiveSearchInstancePromotedTrialInstanceDto { Id = id, Active = true }));

            ex.Result.Errors.Should().Contain(e =>
                e.PropertyName == nameof(ExhaustiveSearchInstancePromotedTrialInstanceDto.Id) &&
                e.ErrorCode == "IdInvalid");
        }

        [Fact]
        public async Task NullModelThrowsArgumentNullExceptionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<ArgumentNullException>(() => service.UpdateActiveAsync(null));
        }

        [Fact]
        public async Task PreCancelledTokenThrowsOperationCanceledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var id = await CreatePromotedTrialInstanceAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                service.UpdateActiveAsync(
                    new ExhaustiveSearchInstancePromotedTrialInstanceDto { Id = id, Active = true }, cts.Token));
        }

        [Fact]
        public async Task PermissionDeniedLogsWarnNotErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var id = await CreatePromotedTrialInstanceAsync(dbContext, fx.Seed.UserWithPermission);
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log);

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.UpdateActiveAsync(
                    new ExhaustiveSearchInstancePromotedTrialInstanceDto { Id = id, Active = true }));

            log.Entries.Should().Contain(e => e.Level == "WARN" && e.Message.Contains(fx.Seed.UserWithoutPermission));
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task NotFoundLogsWarnNotErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.UpdateActiveAsync(
                    new ExhaustiveSearchInstancePromotedTrialInstanceDto { Id = int.MaxValue - 1, Active = true }));

            log.Entries.Should().Contain(e => e.Level == "WARN");
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task SuccessfulUpdateLogsExactlyOneInfoAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var id = await CreatePromotedTrialInstanceAsync(dbContext, fx.Seed.UserWithPermission);
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            await service.UpdateActiveAsync(
                new ExhaustiveSearchInstancePromotedTrialInstanceDto { Id = id, Active = true });

            log.Entries.Count(e => e.Level == "INFO").Should().Be(1);
        }

        [Fact]
        public async Task WithGatesDisabledHappyPathRecordsNoDebugInfoOrWarnAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var id = await CreatePromotedTrialInstanceAsync(dbContext, fx.Seed.UserWithPermission);
            var log = new TestLog(false);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            await service.UpdateActiveAsync(
                new ExhaustiveSearchInstancePromotedTrialInstanceDto { Id = id, Active = true });

            log.Entries.Should().BeEmpty();
        }

        [Fact]
        public async Task FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync()
        {
            var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            await dbContext.DisposeAsync();

            await Assert.ThrowsAnyAsync<Exception>(() =>
                service.UpdateActiveAsync(
                    new ExhaustiveSearchInstancePromotedTrialInstanceDto { Id = 1, Active = true }));

            var errorEntry = log.Entries.Should().ContainSingle(e => e.Level == "ERROR").Subject;
            errorEntry.Exception.Should().NotBeNull();
        }

        [Fact]
        public async Task CallEmitsOneSpanWithOutcomeTagAsync()
        {
            var activities = new List<Activity>();
            using var listener = new ActivityListener();
            listener.ShouldListenTo = s => s.Name == ServiceDiagnostics.Name;
            listener.Sample = (ref _) => ActivitySamplingResult.AllData;
            listener.ActivityStopped = activities.Add;
            ActivitySource.AddActivityListener(listener);

            await using var dbContext = fx.GetDbContext();
            var id = await CreatePromotedTrialInstanceAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await service.UpdateActiveAsync(
                new ExhaustiveSearchInstancePromotedTrialInstanceDto { Id = id, Active = true });

            var span = activities.Should()
                .ContainSingle(a => a.OperationName == "ExhaustiveSearchInstancePromotedTrialInstance.Update")
                .Subject;
            span.GetTagItem("jube.outcome").Should().Be("ok");
            span.GetTagItem("jube.entity.id").Should().Be(id);
        }

        [Fact]
        public async Task CallRecordsOneDurationMeasurementAsync()
        {
            using var collector = new MetricCollector<double>(ServiceDiagnostics.OperationDuration);

            await using var dbContext = fx.GetDbContext();
            var id = await CreatePromotedTrialInstanceAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await service.UpdateActiveAsync(
                new ExhaustiveSearchInstancePromotedTrialInstanceDto { Id = id, Active = true });

            collector.GetMeasurementSnapshot().Should().ContainSingle(m => (string)m.Tags["operation"].Required() == "Update");
        }

        [Fact]
        public async Task AuditLogGetsExactlyOneLineAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var id = await CreatePromotedTrialInstanceAsync(dbContext, fx.Seed.UserWithPermission);
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditLog);

            await service.UpdateActiveAsync(
                new ExhaustiveSearchInstancePromotedTrialInstanceDto { Id = id, Active = true });

            auditLog.Entries.Should().ContainSingle();
            auditLog.Entries[0].Message.Should().Contain("op=Update");
        }

        [Fact]
        public async Task SuccessfulUpdatePublishesExactlyOneUpdatedEventAsync()
        {
            var serviceChangeBus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var id = await CreatePromotedTrialInstanceAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                serviceChangeBus: serviceChangeBus);

            await service.UpdateActiveAsync(
                new ExhaustiveSearchInstancePromotedTrialInstanceDto { Id = id, Active = true });

            serviceChangeBus.Published.Should().ContainSingle();
            serviceChangeBus.Published[0].Kind.Should().Be(ServiceChangeKind.Updated);
            serviceChangeBus.Published[0].EntityId.Should().Be(id);
        }

        [Fact]
        public async Task FailedUpdatePublishesNothingAsync()
        {
            var serviceChangeBus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                serviceChangeBus: serviceChangeBus);

            await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.UpdateActiveAsync(
                    new ExhaustiveSearchInstancePromotedTrialInstanceDto { Id = 0, Active = true }));

            serviceChangeBus.Published.Should().BeEmpty();
        }

        [Fact]
        public void CatalogueRegistersUniquePascalCaseNoUnderscoreName()
        {
            var names = ServiceToolCatalogue.All.Select(t => t.Name).ToList();
            names.Should().OnlyHaveUniqueItems();
            names.Should().OnlyContain(n => !n.Contains('_'));
            names.Should().Contain("ExhaustiveSearchInstancePromotedTrialInstanceUpdate");
        }

        [Fact]
        public async Task IdInvalidMessageResolvesFromFrenchResourceAsync()
        {
            var originalCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo("fr");

                await using var dbContext = fx.GetDbContext();
                var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

                var ex = await Assert.ThrowsAsync<DtoValidationException>(() =>
                    service.UpdateActiveAsync(
                        new ExhaustiveSearchInstancePromotedTrialInstanceDto { Id = 0, Active = true }));

                ex.Result.Errors.Single(e => e.ErrorCode == "IdInvalid").ErrorMessage
                    .Should().Be("Un identifiant d'essai promu valide est requis.");
            }
            finally
            {
                CultureInfo.CurrentUICulture = originalCulture;
            }
        }
    }
}