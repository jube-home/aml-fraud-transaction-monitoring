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
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Query.ExhaustiveSearchInstancePromotedTrialInstancePredictedActual;
using Jube.Service.Observability;
using Jube.Service.Query.ExhaustiveSearchInstancePromotedTrialInstancePredictedActual;
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

namespace Jube.Test.Service.Query.ExhaustiveSearchInstancePromotedTrialInstancePredictedActual
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ExhaustiveSearchInstancePromotedTrialInstancePredictedActualServiceTests(DatabaseFixture fx)
        : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdModelIds = [];
        private readonly List<int> createdSearchIds = [];
        private readonly List<int> createdTrialIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var trialIds = createdTrialIds.Select(id => (int?)id).ToList();
            await dbContext.ExhaustiveSearchInstancePromotedTrialInstancePredictedActual
                .Where(w => trialIds.Contains(w.ExhaustiveSearchInstanceTrialInstanceId)).DeleteAsync();
            await dbContext.ExhaustiveSearchInstancePromotedTrialInstance
                .Where(w => trialIds.Contains(w.ExhaustiveSearchInstanceTrialInstanceId)).DeleteAsync();
            await dbContext.ExhaustiveSearchInstanceTrialInstance.Where(w => createdTrialIds.Contains(w.Id))
                .DeleteAsync();
            await dbContext.ExhaustiveSearchInstance.Where(w => createdSearchIds.Contains(w.Id)).DeleteAsync();
            await dbContext.EntityAnalysisModelRequestXpath.Where(w => w.EntityAnalysisModelId != null
                                                                       && createdModelIds.Contains(
                                                                           w.EntityAnalysisModelId.Value))
                .DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => createdModelIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<ExhaustiveSearchInstancePromotedTrialInstancePredictedActualService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return ExhaustiveSearchInstancePromotedTrialInstancePredictedActualService.CreateAsync(dbContext, userName,
                log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
        }

        private static Task<int> TenantOfAsync(DbContext dbContext, string userName) =>
            dbContext.UserInTenant.Where(w => w.User == userName).Select(s => s.TenantRegistryId).FirstAsync();

        private async Task<int> CreateSearchAsync(DbContext dbContext, int tenantRegistryId)
        {
            var modelId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                TenantRegistryId = tenantRegistryId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix,
            });
            createdModelIds.Add(modelId);

            var searchId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.ExhaustiveSearchInstance
            {
                Name = $"{DatabaseFixture.Prefix}Search{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                EntityAnalysisModelId = modelId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix,
            });
            createdSearchIds.Add(searchId);
            return searchId;
        }

        private async Task<int> CreateTrialAsync(DbContext dbContext, int searchId, byte promotedActive,
            params (double Predicted, double Actual)[] rows)
        {
            var trialId = await dbContext.InsertWithInt32IdentityAsync(
                new Data.Poco.ExhaustiveSearchInstanceTrialInstance
                {
                    ExhaustiveSearchInstanceId = searchId,
                    CreatedDate = DateTime.UtcNow,
                });
            createdTrialIds.Add(trialId);

            await dbContext.InsertAsync(new Data.Poco.ExhaustiveSearchInstancePromotedTrialInstance
            {
                ExhaustiveSearchInstanceTrialInstanceId = trialId,
                Active = promotedActive,
                Json = "{}",
                CreatedDate = DateTime.UtcNow,
            });

            foreach (var (predicted, actual) in rows)
            {
                await dbContext.InsertAsync(new Data.Poco.ExhaustiveSearchInstancePromotedTrialInstancePredictedActual
                {
                    ExhaustiveSearchInstanceTrialInstanceId = trialId,
                    Predicted = predicted,
                    Actual = actual,
                });
            }

            return trialId;
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
                ExhaustiveSearchInstancePromotedTrialInstancePredictedActualService.CreateAsync(dbContext, userName,
                    log, localizers, new NullServiceChangeBus(),
                    TestLog.NoOp));

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
        public async Task GetAsyncReturnsSeededRowsMappedFieldByFieldInIdOrderAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var searchId = await CreateSearchAsync(dbContext, tenantId);
            await CreateTrialAsync(dbContext, searchId, 1, (0.25, 1.0), (0.75, 0.5), (0.0, 0.0));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(searchId);

            result.Should().HaveCount(3);
            result[0].Predicted.Should().BeApproximately(0.25, 1e-9);
            result[0].Actual.Should().BeApproximately(1.0, 1e-9);
            result[0].Error.Should().BeApproximately(0.75, 1e-9);
            result[1].Predicted.Should().BeApproximately(0.75, 1e-9);
            result[1].Actual.Should().BeApproximately(0.5, 1e-9);
            result[1].Error.Should().BeApproximately(-0.25, 1e-9);
            result[2].Error.Should().Be(0.0);
        }

        [Fact]
        public async Task GetAsyncReturnsEmptyForUnknownSearchInstanceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(int.MaxValue)).Should().BeEmpty();
        }

        [Fact]
        public async Task GetAsyncReturnsEmptyWhenPromotedTrialIsNotActiveAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var searchId = await CreateSearchAsync(dbContext, tenantId);
            await CreateTrialAsync(dbContext, searchId, 0, (0.1, 0.2));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(searchId)).Should().BeEmpty();
        }

        [Fact]
        public async Task GetAsyncExcludesSoftDeletedPredictedActualRowsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var searchId = await CreateSearchAsync(dbContext, tenantId);
            var trialId = await CreateTrialAsync(dbContext, searchId, 1, (1.0, 2.0), (3.0, 4.0));
            await dbContext.InsertAsync(new Data.Poco.ExhaustiveSearchInstancePromotedTrialInstancePredictedActual
            {
                ExhaustiveSearchInstanceTrialInstanceId = trialId,
                Predicted = 99.0,
                Actual = 99.0,
                Deleted = 1,
            });
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(searchId)).Select(r => r.Predicted).Should().Equal(1.0, 3.0);
        }

        [Fact]
        public async Task GetAsyncIgnoresSoftDeletedPromotedTrialAndFallsBackToTheLiveOneAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var searchId = await CreateSearchAsync(dbContext, tenantId);
            await CreateTrialAsync(dbContext, searchId, 1, (1.0, 2.0));
            var deletedTrialId = await CreateTrialAsync(dbContext, searchId, 1, (9.0, 9.0));
            await dbContext.ExhaustiveSearchInstancePromotedTrialInstance
                .Where(w => w.ExhaustiveSearchInstanceTrialInstanceId == deletedTrialId)
                .Set(w => w.Deleted, (byte?)1).UpdateAsync();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(searchId)).Select(r => r.Predicted).Should().Equal(1.0);
        }

        [Fact]
        public async Task GetAsyncOnlyReturnsRowsOfTheMostRecentlyPromotedTrialAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var searchId = await CreateSearchAsync(dbContext, tenantId);
            await CreateTrialAsync(dbContext, searchId, 1, (9.0, 9.0));
            await CreateTrialAsync(dbContext, searchId, 1, (1.0, 2.0), (3.0, 4.0));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(searchId);

            result.Select(r => r.Predicted).Should().Equal(1.0, 3.0);
        }

        [Fact]
        public async Task TenantBCannotSeeTenantASearchInstanceAndViceVersaAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantA = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantB = await TenantOfAsync(dbContext, fx.Seed.UserTenantB);
            var searchA = await CreateSearchAsync(dbContext, tenantA);
            var searchB = await CreateSearchAsync(dbContext, tenantB);
            await CreateTrialAsync(dbContext, searchA, 1, (1.0, 1.0));
            await CreateTrialAsync(dbContext, searchB, 1, (2.0, 2.0), (3.0, 3.0));
            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await serviceA.GetAsync(searchA)).Should().ContainSingle();
            (await serviceB.GetAsync(searchB)).Should().HaveCount(2);
            (await serviceA.GetAsync(searchB)).Should().BeEmpty();
            (await serviceB.GetAsync(searchA)).Should().BeEmpty();
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
        public async Task FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync()
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
                    a.OperationName == "ExhaustiveSearchInstancePromotedTrialInstancePredictedActual.Get").Subject
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
            names.Should().Contain("ExhaustiveSearchInstancePromotedTrialInstancePredictedActualGet");
        }
    }
}