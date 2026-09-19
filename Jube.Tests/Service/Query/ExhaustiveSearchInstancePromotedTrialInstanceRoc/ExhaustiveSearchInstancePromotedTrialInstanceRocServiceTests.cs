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
using Jube.Service.Exceptions.Query.ExhaustiveSearchInstancePromotedTrialInstanceRoc;
using Jube.Service.Observability;
using Jube.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceRoc;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceRoc.Models;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using log4net;

namespace Jube.Test.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceRoc
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ExhaustiveSearchInstancePromotedTrialInstanceRocServiceTests(DatabaseFixture fx)
        : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdModelIds = [];
        private readonly List<int> createdPromotedIds = [];
        private readonly List<int> createdRocIds = [];
        private readonly List<int> createdSearchInstanceIds = [];
        private readonly List<int> createdTrialInstanceIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.GetTable<global::Jube.Data.Poco.ExhaustiveSearchInstancePromotedTrialInstanceRoc>()
                .Where(w => createdRocIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<ExhaustiveSearchInstancePromotedTrialInstance>()
                .Where(w => createdPromotedIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<ExhaustiveSearchInstanceTrialInstance>()
                .Where(w => createdTrialInstanceIds.Contains(w.Id)).DeleteAsync();
            foreach (var id in createdSearchInstanceIds)
            {
                await dbContext.GetTable<ExhaustiveSearchInstanceVersion>()
                    .Where(w => w.ExhaustiveSearchInstanceId == id).DeleteAsync();
                await dbContext.GetTable<global::Jube.Data.Poco.ExhaustiveSearchInstance>().Where(w => w.Id == id)
                    .DeleteAsync();
            }

            foreach (var modelId in createdModelIds)
            {
                await dbContext.GetTable<EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == modelId)
                    .DeleteAsync();
                await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
            }
        }

        private static Task<ExhaustiveSearchInstancePromotedTrialInstanceRocService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return ExhaustiveSearchInstancePromotedTrialInstanceRocService.CreateAsync(dbContext, userName,
                log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task<Seeded> CreateSearchInstanceWithTrialAsync(DbContext dbContext, string createdUser,
            bool promoteActive, params (double score, int tp, int tn, int fp, int fn)[] rocPoints)
        {
            var modelRepository = new EntityAnalysisModelRepository(dbContext, createdUser);
            var model = await modelRepository.InsertAsync(new global::Jube.Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0
            });
            createdModelIds.Add(model.Id);

            var searchInstance = await new ExhaustiveSearchInstanceRepository(dbContext, createdUser).InsertAsync(
                new global::Jube.Data.Poco.ExhaustiveSearchInstance
                {
                    Name = $"{DatabaseFixture.Prefix}Search{Guid.NewGuid():N}"[..40],
                    Guid = Guid.NewGuid(),
                    EntityAnalysisModelId = model.Id,
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                    StatusId = 0
                });
            createdSearchInstanceIds.Add(searchInstance.Id);

            var trialInstance = await new ExhaustiveSearchInstanceTrialInstanceRepository(dbContext).InsertAsync(
                new ExhaustiveSearchInstanceTrialInstance
                {
                    ExhaustiveSearchInstanceId = searchInstance.Id,
                    CreatedDate = DateTime.UtcNow
                });
            createdTrialInstanceIds.Add(trialInstance.Id);

            var promoted = await new ExhaustiveSearchInstancePromotedTrialInstanceRepository(dbContext).InsertAsync(
                new ExhaustiveSearchInstancePromotedTrialInstance
                {
                    ExhaustiveSearchInstanceTrialInstanceId = trialInstance.Id,
                    Active = (byte)(promoteActive ? 1 : 0),
                    Score = 0.5,
                    Json = "{}"
                });
            createdPromotedIds.Add(promoted.Id);

            foreach (var (score, tp, tn, fp, fn) in rocPoints)
            {
                var id = await dbContext.InsertWithInt32IdentityAsync(
                    new global::Jube.Data.Poco.ExhaustiveSearchInstancePromotedTrialInstanceRoc
                    {
                        ExhaustiveSearchInstanceTrialInstanceId = trialInstance.Id,
                        Score = score,
                        TruePositive = tp,
                        TrueNegative = tn,
                        FalsePositive = fp,
                        FalseNegative = fn,
                        Threshold = score,
                        Deleted = 0
                    });
                createdRocIds.Add(id);
            }

            return new Seeded(searchInstance.Id);
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
                ExhaustiveSearchInstancePromotedTrialInstanceRocService.CreateAsync(dbContext, userName, log,
                    localizers, new NullServiceChangeBus(), TestLog.NoOp));

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
        public async Task GetAsyncMapsRocPointsFieldByFieldOrderedByIdAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var seeded = await CreateSearchInstanceWithTrialAsync(dbContext, fx.Seed.UserWithPermission, true,
                (0.9, 10, 60, 20, 10), (0.4, 40, 40, 10, 10), (0.1, 5, 5, 5, 5));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(seeded.SearchInstanceId);

            result.Should().HaveCount(3);
            result.Select(r => r.Id).Should().BeInAscendingOrder();
            result[0].Score.Should().Be(0.9);
            result[0].Fpr.Should().BeApproximately(20d / 80d, 1e-9);
            result[0].Tpr.Should().BeApproximately(10d / 20d, 1e-9);
            result[1].Score.Should().Be(0.4);
            result[1].Fpr.Should().BeApproximately(10d / 50d, 1e-9);
            result[1].Tpr.Should().BeApproximately(40d / 50d, 1e-9);
            result[2].Fpr.Should().BeApproximately(0.5, 1e-9);
            result[2].Tpr.Should().BeApproximately(0.5, 1e-9);
            createdRocIds.Should().Contain(result.Select(r => r.Id));
        }

        [Fact]
        public async Task GetAsyncReturnsEmptyForUnknownSearchInstanceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(int.MaxValue);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAsyncReturnsEmptyWhenPromotedTrialInstanceIsNotActiveAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var seeded = await CreateSearchInstanceWithTrialAsync(dbContext, fx.Seed.UserWithPermission, false,
                (0.9, 10, 60, 20, 10));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(seeded.SearchInstanceId);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAsyncOnlyReturnsPointsOfThePromotedTrialInstanceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var one = await CreateSearchInstanceWithTrialAsync(dbContext, fx.Seed.UserWithPermission, true,
                (0.9, 10, 60, 20, 10));
            await CreateSearchInstanceWithTrialAsync(dbContext, fx.Seed.UserWithPermission, true,
                (0.1, 1, 1, 1, 1), (0.2, 1, 1, 1, 1));
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(one.SearchInstanceId);

            result.Should().ContainSingle().Which.Score.Should().Be(0.9);
        }

        [Fact]
        public async Task TenantBCannotSeeTenantARocPointsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var seeded = await CreateSearchInstanceWithTrialAsync(dbContext, fx.Seed.UserWithPermission, true,
                (0.9, 10, 60, 20, 10));
            var tenantA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await tenantA.GetAsync(seeded.SearchInstanceId)).Should().HaveCount(1);
            (await tenantB.GetAsync(seeded.SearchInstanceId)).Should().BeEmpty();
        }

        [Fact]
        public async Task TenantACannotSeeTenantBRocPointsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var seeded = await CreateSearchInstanceWithTrialAsync(dbContext, fx.Seed.UserTenantB, true,
                (0.7, 10, 60, 20, 10));
            var tenantA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await tenantA.GetAsync(seeded.SearchInstanceId)).Should().BeEmpty();
            (await tenantB.GetAsync(seeded.SearchInstanceId)).Should().HaveCount(1);
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
            await service.GetAsync(int.MaxValue);

            activities.Should().ContainSingle(a =>
                    a.OperationName == "ExhaustiveSearchInstancePromotedTrialInstanceRoc.Get")
                .Subject.GetTagItem("jube.outcome").Should().Be("ok");
        }

        [Fact]
        public async Task AuditLogGetsExactlyOneLineAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditLog);

            await service.GetAsync(int.MaxValue);

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

            await service.GetAsync(int.MaxValue);
            await Assert.ThrowsAsync<ForbiddenException>(() => forbidden.GetAsync(1));

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public void CatalogueRegistersUniqueToolName()
        {
            var names = ServiceToolCatalogue.All.Select(t => t.Name).ToList();

            names.Should().OnlyHaveUniqueItems();
            names.Should().Contain("ExhaustiveSearchInstancePromotedTrialInstanceRocGet");
        }
    }
}