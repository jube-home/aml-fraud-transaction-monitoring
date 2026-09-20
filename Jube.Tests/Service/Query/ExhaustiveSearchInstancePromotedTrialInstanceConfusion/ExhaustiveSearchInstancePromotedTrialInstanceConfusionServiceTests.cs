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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceConfusion;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Query.ExhaustiveSearchInstancePromotedTrialInstanceConfusion;
using Jube.Service.Observability;
using Jube.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceConfusion;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceConfusion
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ExhaustiveSearchInstancePromotedTrialInstanceConfusionServiceTests(DatabaseFixture fx)
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
                await dbContext.GetTable<Data.Poco.ExhaustiveSearchInstancePromotedTrialInstance>()
                    .Where(w => w.Id == id).DeleteAsync();
            }

            foreach (var id in createdTrialInstanceIds)
            {
                await dbContext.GetTable<Data.Poco.ExhaustiveSearchInstanceTrialInstance>().Where(w => w.Id == id)
                    .DeleteAsync();
            }

            foreach (var id in createdSearchInstanceIds)
            {
                await dbContext.GetTable<Data.Poco.ExhaustiveSearchInstanceVersion>()
                    .Where(w => w.ExhaustiveSearchInstanceId == id).DeleteAsync();
                await dbContext.GetTable<Data.Poco.ExhaustiveSearchInstance>().Where(w => w.Id == id).DeleteAsync();
            }

            foreach (var modelId in createdModelIds)
            {
                await dbContext.GetTable<Data.Poco.EntityAnalysisModelVersion>()
                    .Where(w => w.EntityAnalysisModelId == modelId).DeleteAsync();
                await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
            }
        }

        private static Task<ExhaustiveSearchInstancePromotedTrialInstanceConfusionService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return ExhaustiveSearchInstancePromotedTrialInstanceConfusionService.CreateAsync(dbContext, userName,
                log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
        }

        private async Task<int> SeedSearchInstanceAsync(DbContext dbContext, string createdUser, int truePositive = 10,
            int trueNegative = 20, int falsePositive = 5, int falseNegative = 15, double score = 0.7,
            bool withActive = true, bool withInactive = false)
        {
            var model = await new EntityAnalysisModelRepository(dbContext, createdUser).InsertAsync(
                new Data.Poco.EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                    Guid = Guid.NewGuid(),
                    Active = 1,
                    Locked = 0,
                    Deleted = 0
                }).ConfigureAwait(false);
            createdModelIds.Add(model.Id);

            var searchInstance = await new ExhaustiveSearchInstanceRepository(dbContext, createdUser).InsertAsync(
                new Data.Poco.ExhaustiveSearchInstance
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

            var trial = await new ExhaustiveSearchInstanceTrialInstanceRepository(dbContext).InsertAsync(
                new Data.Poco.ExhaustiveSearchInstanceTrialInstance
                {
                    ExhaustiveSearchInstanceId = searchInstance.Id,
                    CreatedDate = DateTime.UtcNow
                }).ConfigureAwait(false);
            createdTrialInstanceIds.Add(trial.Id);

            if (withInactive)
            {
                createdPromotedIds.Add(await InsertPromotedAsync(dbContext, trial.Id, 0, 99, 99, 99, 99, 0.1));
            }

            if (withActive)
            {
                createdPromotedIds.Add(await InsertPromotedAsync(dbContext, trial.Id, 1, truePositive, trueNegative,
                    falsePositive, falseNegative, score));
            }

            return searchInstance.Id;
        }

        private static Task<int> InsertPromotedAsync(DbContext dbContext, int trialId, byte active, int tp, int tn,
            int fp, int fn, double score)
        {
            return dbContext.InsertWithInt32IdentityAsync(new Data.Poco.ExhaustiveSearchInstancePromotedTrialInstance
            {
                ExhaustiveSearchInstanceTrialInstanceId = trialId,
                Active = active,
                Score = score,
                TruePositive = tp,
                TrueNegative = tn,
                FalsePositive = fp,
                FalseNegative = fn,
                Json = "{}",
                CreatedDate = DateTime.UtcNow
            });
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
                ExhaustiveSearchInstancePromotedTrialInstanceConfusionService.CreateAsync(dbContext, userName, log,
                    localizers, new NullServiceChangeBus(),
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
            var id = await SeedSearchInstanceAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync(id));

            ex.Code.Should().Be("PermissionDenied");
            ex.RequiredSpecifications.Should().BeEquivalentTo([16]);
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
        public async Task GetAsyncReturnsExactFieldByFieldMappingOfTheQueryAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var id = await SeedSearchInstanceAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = await service.GetAsync(id);

            dto.Id.Should().Be(0);
            dto.Score.Should().Be(0.7);
            dto.TruePositive.Should().Be(10);
            dto.TrueNegative.Should().Be(20);
            dto.FalsePositive.Should().Be(5);
            dto.FalseNegative.Should().Be(15);
            dto.TableTotal.Should().Be(50);
            dto.PositiveRowTotal.Should().Be(25);
            dto.PositiveColumnTotal.Should().Be(15);
            dto.NegativeRowTotal.Should().Be(25);
            dto.NegativeColumnTotal.Should().Be(35);
            dto.PositiveRowTableTotal.Should().BeApproximately(0.5, 1e-9);
            dto.PositiveColumnTableTotal.Should().BeApproximately(0.3, 1e-9);
            dto.NegativeRowTableTotal.Should().BeApproximately(0.5, 1e-9);
            dto.NegativeColumnTableTotal.Should().BeApproximately(0.7, 1e-9);
            dto.TruePositiveRowTotal.Should().BeApproximately(0.4, 1e-9);
            dto.TruePositiveColumnTotal.Should().BeApproximately(0.67, 1e-9);
            dto.TruePositiveTableTotal.Should().BeApproximately(0.2, 1e-9);
            dto.FalsePositiveRowTotal.Should().BeApproximately(0.2, 1e-9);
            dto.FalsePositiveColumnTotal.Should().BeApproximately(0.33, 1e-9);
            dto.FalsePositiveTableTotal.Should().BeApproximately(0.1, 1e-9);
            dto.FalseNegativeRowTotal.Should().BeApproximately(0.6, 1e-9);
            dto.FalseNegativeColumnTotal.Should().BeApproximately(0.43, 1e-9);
            dto.FalseNegativeTableTotal.Should().BeApproximately(0.3, 1e-9);
            dto.TrueNegativeRowTotal.Should().BeApproximately(0.8, 1e-9);
            dto.TrueNegativeColumnTotal.Should().BeApproximately(0.57, 1e-9);
            dto.TrueNegativeTableTotal.Should().BeApproximately(0.4, 1e-9);
        }

        [Fact]
        public async Task GetAsyncAllCountsZeroReturnsZeroRatiosNotNaNAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var id = await SeedSearchInstanceAsync(dbContext, fx.Seed.UserWithPermission, 0, 0, 0, 0);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = await service.GetAsync(id);

            dto.TableTotal.Should().Be(0);
            dto.Score.Should().Be(0.7);
            AssertNoNaN(dto);
            dto.TruePositiveRowTotal.Should().Be(0);
            dto.FalseNegativeTableTotal.Should().Be(0);
            dto.TrueNegativeColumnTotal.Should().Be(0);
            dto.PositiveRowTableTotal.Should().Be(0);
        }

        [Fact]
        public async Task GetAsyncOnlyTrueNegativesHasZeroDenominatorsForPositiveSideAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var id = await SeedSearchInstanceAsync(dbContext, fx.Seed.UserWithPermission, 0, 10, 0, 0);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = await service.GetAsync(id);

            AssertNoNaN(dto);
            dto.TableTotal.Should().Be(10);
            dto.PositiveRowTotal.Should().Be(0);
            dto.PositiveColumnTotal.Should().Be(0);
            dto.NegativeRowTotal.Should().Be(10);
            dto.NegativeColumnTotal.Should().Be(10);
            dto.TruePositiveRowTotal.Should().Be(0);
            dto.TruePositiveColumnTotal.Should().Be(0);
            dto.FalseNegativeRowTotal.Should().Be(0);
            dto.FalsePositiveColumnTotal.Should().Be(0);
            dto.FalseNegativeColumnTotal.Should().Be(0);
            dto.PositiveRowTableTotal.Should().Be(0);
            dto.NegativeRowTableTotal.Should().Be(1);
            dto.NegativeColumnTableTotal.Should().Be(1);
            dto.TrueNegativeRowTotal.Should().Be(1);
            dto.TrueNegativeColumnTotal.Should().Be(1);
            dto.TrueNegativeTableTotal.Should().Be(1);
        }

        [Fact]
        public async Task GetAsyncOnlyTruePositivesHasZeroDenominatorsForNegativeSideAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var id = await SeedSearchInstanceAsync(dbContext, fx.Seed.UserWithPermission, 4, 0, 0, 0);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = await service.GetAsync(id);

            AssertNoNaN(dto);
            dto.TableTotal.Should().Be(4);
            dto.NegativeRowTotal.Should().Be(0);
            dto.NegativeColumnTotal.Should().Be(0);
            dto.TruePositiveRowTotal.Should().Be(1);
            dto.TruePositiveColumnTotal.Should().Be(1);
            dto.TruePositiveTableTotal.Should().Be(1);
            dto.FalsePositiveRowTotal.Should().Be(0);
            dto.TrueNegativeRowTotal.Should().Be(0);
            dto.TrueNegativeColumnTotal.Should().Be(0);
            dto.FalseNegativeColumnTotal.Should().Be(0);
        }

        [Fact]
        public async Task GetAsyncFalseNegativeRatiosUseFalseNegativeCountAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var id = await SeedSearchInstanceAsync(dbContext, fx.Seed.UserWithPermission, 2, 3, 1, 4);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = await service.GetAsync(id);

            dto.TableTotal.Should().Be(10);
            dto.FalseNegativeRowTotal.Should().BeApproximately(0.67, 1e-9);
            dto.FalseNegativeColumnTotal.Should().BeApproximately(0.57, 1e-9);
            dto.FalseNegativeTableTotal.Should().BeApproximately(0.4, 1e-9);
            dto.FalsePositiveTableTotal.Should().BeApproximately(0.1, 1e-9);
        }

        private static void AssertNoNaN(ExhaustiveSearchInstancePromotedTrialInstanceConfusionDto dto)
        {
            foreach (var p in typeof(ExhaustiveSearchInstancePromotedTrialInstanceConfusionDto).GetProperties()
                         .Where(w => w.PropertyType == typeof(double)))
            {
                double.IsNaN((double)p.GetValue(dto).Required()).Should().BeFalse(p.Name);
            }
        }

        [Fact]
        public async Task GetAsyncIgnoresInactivePromotedTrialInstancesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var id = await SeedSearchInstanceAsync(dbContext, fx.Seed.UserWithPermission, withInactive: true);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = await service.GetAsync(id);

            dto.TruePositive.Should().Be(10);
            dto.Score.Should().Be(0.7);
        }

        [Fact]
        public async Task GetAsyncReturnsAllZeroWhenOnlyInactiveExistsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var id = await SeedSearchInstanceAsync(dbContext, fx.Seed.UserWithPermission, withActive: false,
                withInactive: true);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = await service.GetAsync(id);

            dto.Should().BeEquivalentTo(new ExhaustiveSearchInstancePromotedTrialInstanceConfusionDto());
        }

        [Fact]
        public async Task GetAsyncReturnsAllZeroForUnknownSearchInstanceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = await service.GetAsync(int.MaxValue);

            dto.Should().BeEquivalentTo(new ExhaustiveSearchInstancePromotedTrialInstanceConfusionDto());
        }

        [Fact]
        public async Task TenantBCannotSeeTenantASearchInstanceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var idA = await SeedSearchInstanceAsync(dbContext, fx.Seed.UserWithPermission);
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            var dto = await serviceB.GetAsync(idA);

            dto.Should().BeEquivalentTo(new ExhaustiveSearchInstancePromotedTrialInstanceConfusionDto());
        }

        [Fact]
        public async Task TenantACannotSeeTenantBSearchInstanceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var idB = await SeedSearchInstanceAsync(dbContext, fx.Seed.UserTenantB, truePositive: 7);
            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = await serviceA.GetAsync(idB);

            dto.Should().BeEquivalentTo(new ExhaustiveSearchInstancePromotedTrialInstanceConfusionDto());
        }

        [Fact]
        public async Task EachTenantSeesOnlyItsOwnDataAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var idA = await SeedSearchInstanceAsync(dbContext, fx.Seed.UserWithPermission, truePositive: 11);
            var idB = await SeedSearchInstanceAsync(dbContext, fx.Seed.UserTenantB, truePositive: 22);
            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await serviceA.GetAsync(idA)).TruePositive.Should().Be(11);
            (await serviceB.GetAsync(idB)).TruePositive.Should().Be(22);
            (await serviceA.GetAsync(idB)).TruePositive.Should().Be(0);
            (await serviceB.GetAsync(idA)).TruePositive.Should().Be(0);
        }

        [Fact]
        public async Task PreCancelledTokenThrowsOperationCanceledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var id = await SeedSearchInstanceAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetAsync(id, cts.Token));
        }

        [Fact]
        public async Task EachCallEmitsOneSpanAndOneDurationMeasurementAsync()
        {
            var activities = new List<Activity>();
            using var listener = new ActivityListener();
            listener.ShouldListenTo = s => s.Name == ServiceDiagnostics.Name;
            listener.Sample = (ref _) => ActivitySamplingResult.AllData;
            listener.ActivityStopped = activities.Add;
            ActivitySource.AddActivityListener(listener);
            using var collector = new MetricCollector<double>(ServiceDiagnostics.OperationDuration);

            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            await service.GetAsync(int.MaxValue);

            activities.Should().ContainSingle(a =>
                    a.OperationName == "ExhaustiveSearchInstancePromotedTrialInstanceConfusion.Get")
                .Subject.GetTagItem("jube.outcome").Should().Be("ok");
            collector.GetMeasurementSnapshot().Should()
                .ContainSingle(m => (string)m.Tags["operation"].Required() == "Get");
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
        public void GetAsyncDeclaresUniqueReadIdempotentServiceOperation()
        {
            var attribute = typeof(ExhaustiveSearchInstancePromotedTrialInstanceConfusionService)
                .GetMethod(nameof(ExhaustiveSearchInstancePromotedTrialInstanceConfusionService.GetAsync)).Required()
                .GetCustomAttribute<ServiceOperationAttribute>();
            attribute = attribute.Required();

            attribute.Should().NotBeNull();
            attribute.Name.Should().Be("ExhaustiveSearchInstancePromotedTrialInstanceConfusionGet");
            attribute.Kind.Should().Be(OperationKind.Read);
            attribute.Idempotent.Should().BeTrue();
            attribute.Destructive.Should().BeFalse();
        }

        [Fact]
        public async Task PermissionDeniedMessageComesFromResourcesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync(1));

            ex.Message.Should().Be(
                localizers.Create(typeof(ExhaustiveSearchInstancePromotedTrialInstanceConfusionResources))
                    [ExhaustiveSearchInstancePromotedTrialInstanceConfusionResources.PermissionDenied].Value);
        }
    }
}