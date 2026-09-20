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
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Query.ExhaustiveSearchInstancePromotedTrialInstanceLearningCurve;
using Jube.Service.Observability;
using Jube.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceLearningCurve;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceLearningCurve
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ExhaustiveSearchInstancePromotedTrialInstanceLearningCurveServiceTests(DatabaseFixture fx)
        : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdModelIds = [];
        private readonly List<int> createdPromotedIds = [];
        private readonly List<int> createdSearchInstanceIds = [];
        private readonly List<int> createdTrialInstanceIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            foreach (var id in createdPromotedIds)
            {
                await dbContext.GetTable<ExhaustiveSearchInstancePromotedTrialInstance>()
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

        private static Task<ExhaustiveSearchInstancePromotedTrialInstanceLearningCurveService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return ExhaustiveSearchInstancePromotedTrialInstanceLearningCurveService.CreateAsync(dbContext, userName,
                log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
        }

        private async Task<int> CreateSearchInstanceAsync(DbContext dbContext, string createdUser)
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
            return searchInstance.Id;
        }

        private async Task AddPromotedAsync(DbContext dbContext, int searchInstanceId, double score,
            DateTime createdDate)
        {
            var trialInstance = await new ExhaustiveSearchInstanceTrialInstanceRepository(dbContext).InsertAsync(
                new ExhaustiveSearchInstanceTrialInstance
                {
                    ExhaustiveSearchInstanceId = searchInstanceId,
                    CreatedDate = DateTime.UtcNow
                }).ConfigureAwait(false);
            createdTrialInstanceIds.Add(trialInstance.Id);

            var promoted = await new ExhaustiveSearchInstancePromotedTrialInstanceRepository(dbContext).InsertAsync(
                new ExhaustiveSearchInstancePromotedTrialInstance
                {
                    ExhaustiveSearchInstanceTrialInstanceId = trialInstance.Id,
                    Active = 1,
                    Score = score,
                    Json = "{}",
                    CreatedDate = createdDate
                }).ConfigureAwait(false);
            createdPromotedIds.Add(promoted.Id);

            await dbContext.GetTable<ExhaustiveSearchInstancePromotedTrialInstance>()
                .Where(w => w.Id == promoted.Id).Set(s => s.CreatedDate, createdDate).UpdateAsync()
                .ConfigureAwait(false);
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
                ExhaustiveSearchInstancePromotedTrialInstanceLearningCurveService.CreateAsync(dbContext, userName, log,
                    localizers,
                    new NullServiceChangeBus(), TestLog.NoOp));

            ex.Code.Should().Be("NotAuthenticated");
            log.Entries.Should().Contain(e => e.Level == "WARN");
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UserNoTenant));
        }

        [Fact]
        public async Task CreateWithUnknownUserThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UnknownUser));
        }

        [Fact]
        public async Task GetAsyncThrowsForbiddenWhenPermissionMissingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync(1));

            ex.Code.Should().Be("PermissionDenied");
            ex.RequiredSpecifications.Should().BeEquivalentTo([16]);
        }

        [Fact]
        public async Task GetAsyncReturnsSeededCurveOrderedByIdWithRoundedScoreAndExactMappingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchInstanceId = await CreateSearchInstanceAsync(dbContext, fx.Seed.UserWithPermission);
            var first = new DateTime(2024, 3, 1, 10, 0, 0, DateTimeKind.Utc);
            var second = new DateTime(2024, 3, 2, 11, 30, 0, DateTimeKind.Utc);
            var third = new DateTime(2024, 3, 3, 12, 45, 0, DateTimeKind.Utc);
            await AddPromotedAsync(dbContext, searchInstanceId, 0.12345, first);
            await AddPromotedAsync(dbContext, searchInstanceId, 0.5, second);
            await AddPromotedAsync(dbContext, searchInstanceId, 0.999, third);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var curve = await service.GetAsync(searchInstanceId);

            curve.Should().HaveCount(3);
            curve[0].Score.Should().Be(0.12);
            curve[0].CreatedDate.Should().BeCloseTo(first, TimeSpan.FromSeconds(1));
            curve[1].Score.Should().Be(0.5);
            curve[1].CreatedDate.Should().BeCloseTo(second, TimeSpan.FromSeconds(1));
            curve[2].Score.Should().Be(1.0);
            curve[2].CreatedDate.Should().BeCloseTo(third, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task GetAsyncOnlyReturnsPointsOfTheRequestedSearchInstanceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var wanted = await CreateSearchInstanceAsync(dbContext, fx.Seed.UserWithPermission);
            var other = await CreateSearchInstanceAsync(dbContext, fx.Seed.UserWithPermission);
            await AddPromotedAsync(dbContext, wanted, 0.25, DateTime.UtcNow);
            await AddPromotedAsync(dbContext, other, 0.75, DateTime.UtcNow);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var curve = await service.GetAsync(wanted);

            curve.Should().ContainSingle().Which.Score.Should().Be(0.25);
        }

        [Fact]
        public async Task GetAsyncReturnsEmptyForSearchInstanceWithNoPromotedTrialsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchInstanceId = await CreateSearchInstanceAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(searchInstanceId)).Should().BeEmpty();
        }

        [Fact]
        public async Task GetAsyncReturnsEmptyForUnknownSearchInstanceIdAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(int.MaxValue)).Should().BeEmpty();
        }

        [Fact]
        public async Task TenantBCannotSeeTenantACurveAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchInstanceId = await CreateSearchInstanceAsync(dbContext, fx.Seed.UserWithPermission);
            await AddPromotedAsync(dbContext, searchInstanceId, 0.4, DateTime.UtcNow);
            var tenantA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await tenantA.GetAsync(searchInstanceId)).Should().ContainSingle();
            (await tenantB.GetAsync(searchInstanceId)).Should().BeEmpty();
        }

        [Fact]
        public async Task TenantACannotSeeTenantBCurveAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchInstanceId = await CreateSearchInstanceAsync(dbContext, fx.Seed.UserTenantB);
            await AddPromotedAsync(dbContext, searchInstanceId, 0.6, DateTime.UtcNow);
            var tenantA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await tenantB.GetAsync(searchInstanceId)).Should().ContainSingle();
            (await tenantA.GetAsync(searchInstanceId)).Should().BeEmpty();
        }

        [Fact]
        public async Task PreCancelledTokenThrowsOperationCanceledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetAsync(1, cts.Token));
        }

        [Fact]
        public async Task PermissionDeniedLogsWarnNotErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync(1));

            log.Entries.Should().Contain(e => e.Level == "WARN" && e.Message.Contains(fx.Seed.UserWithoutPermission));
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task FailureLogsErrorWithExceptionAttachedAndPropagatesAsync()
        {
            var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);
            await dbContext.DisposeAsync();

            await Assert.ThrowsAnyAsync<Exception>(() => service.GetAsync(1));

            log.Entries.Should().ContainSingle(e => e.Level == "ERROR").Subject.Exception.Should().NotBeNull();
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
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            await service.GetAsync(1);

            activities.Should().ContainSingle(a =>
                    a.OperationName == "ExhaustiveSearchInstancePromotedTrialInstanceLearningCurve.Get").Subject
                .GetTagItem("jube.outcome").Should().Be("ok");
        }

        [Fact]
        public async Task AuditLogGetsExactlyOneLineAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditLog);

            await service.GetAsync(1);

            auditLog.Entries.Should().ContainSingle();
            auditLog.Entries[0].Message.Should().Contain("op=Get");
        }

        [Fact]
        public async Task ReadsAndFailuresPublishNothingAsync()
        {
            var bus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);
            var forbidden = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, serviceChangeBus: bus);

            await service.GetAsync(1);
            await Assert.ThrowsAsync<ForbiddenException>(() => forbidden.GetAsync(1));

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public void CatalogueRegistersUniqueToolName()
        {
            var names = ServiceToolCatalogue.All.Select(t => t.Name).ToList();

            names.Should().OnlyHaveUniqueItems();
            names.Should().Contain("ExhaustiveSearchInstancePromotedTrialInstanceLearningCurveGet");
        }
    }
}