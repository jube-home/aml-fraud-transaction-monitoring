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
using Jube.Resources;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Query.EntityAnalysisModelSynchronisationNodeStatusEntries;
using Jube.Service.Observability;
using Jube.Service.Query.EntityAnalysisModelSynchronisationNodeStatusEntries;
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

namespace Jube.Test.Service.Query.EntityAnalysisModelSynchronisationNodeStatusEntries
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EntityAnalysisModelSynchronisationNodeStatusEntriesServiceTests(DatabaseFixture fx)
        : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdEntryIds = [];
        private readonly List<int> createdScheduleIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.EntityAnalysisModelSynchronisationNodeStatusEntry
                .Where(w => createdEntryIds.Contains(w.Id)).DeleteAsync();
            await dbContext.EntityAnalysisModelSynchronisationSchedule
                .Where(w => createdScheduleIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<EntityAnalysisModelSynchronisationNodeStatusEntriesService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return EntityAnalysisModelSynchronisationNodeStatusEntriesService.CreateAsync(dbContext, userName,
                log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private static Task<int> TenantOfAsync(DbContext dbContext, string userName) =>
            dbContext.UserInTenant.Where(w => w.User == userName).Select(s => s.TenantRegistryId)
                .FirstAsync();

        private async Task InsertScheduleAsync(int tenantRegistryId, DateTime scheduleDate)
        {
            await using var dbContext = fx.GetDbContext();
            createdScheduleIds.Add(await dbContext.InsertWithInt32IdentityAsync(
                new Data.Poco.EntityAnalysisModelSynchronisationSchedule
                {
                    CreatedDate = DateTime.UtcNow,
                    ScheduleDate = scheduleDate,
                    TenantRegistryId = tenantRegistryId,
                    CreatedUser = DatabaseFixture.Prefix,
                }));
        }

        private async Task InsertEntryAsync(int tenantRegistryId, string instance, DateTime heartbeat,
            DateTime? synchronised)
        {
            await using var dbContext = fx.GetDbContext();
            createdEntryIds.Add(await dbContext.InsertWithInt32IdentityAsync(
                new Data.Poco.EntityAnalysisModelSynchronisationNodeStatusEntry
                {
                    Instance = instance,
                    HeartbeatDate = heartbeat,
                    SynchronisedDate = synchronised,
                    TenantRegistryId = tenantRegistryId,
                }));
        }

        private async Task<(int TenantA, int TenantB, string Tag)> SeedTwoTenantsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantA = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantB = await TenantOfAsync(dbContext, fx.Seed.UserTenantB);
            var tag = $"{DatabaseFixture.Prefix}Node{Guid.NewGuid():N}"[..24];
            var now = DateTime.UtcNow;

            await InsertScheduleAsync(tenantA, now.AddMinutes(-5));
            await InsertEntryAsync(tenantA, $"{tag}-pending", now.AddSeconds(-30), now.AddMinutes(-10));
            await InsertEntryAsync(tenantA, $"{tag}-synced-stale", now.AddMinutes(-30), now);
            await InsertEntryAsync(tenantA, $"{tag}-never", now.AddSeconds(-10), null);
            await InsertEntryAsync(tenantA, $"{tag}-old", now.AddHours(-2), now.AddHours(-3));

            await InsertScheduleAsync(tenantB, now.AddMinutes(-5));
            await InsertEntryAsync(tenantB, $"{tag}-B", now.AddSeconds(-5), now);
            return (tenantA, tenantB, tag);
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
                EntityAnalysisModelSynchronisationNodeStatusEntriesService.CreateAsync(dbContext, userName, log,
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

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync());

            ex.Code.Should().Be("PermissionDenied");
            ex.RequiredSpecifications.Should().BeEquivalentTo([5]);
        }

        [Fact]
        public async Task GetAsyncMapsEveryFieldAndComputesFlagsAsync()
        {
            var (_, _, tag) = await SeedTwoTenantsAsync();
            var now = DateTime.UtcNow;
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync();

            result.Should().NotBeNull();
            var mine = result.Required().Where(w => w.Instance != null && w.Instance.StartsWith(tag)).ToList();
            mine.Select(s => s.Instance).Should()
                .BeEquivalentTo($"{tag}-pending", $"{tag}-synced-stale", $"{tag}-never");

            var pending = mine.Single(s => s.Instance == $"{tag}-pending");
            pending.SynchronisationPending.Should().BeTrue();
            pending.InstanceAvailable.Should().BeTrue();
            pending.HeartbeatDate.Should().BeCloseTo(now.AddSeconds(-30), TimeSpan.FromSeconds(5));
            pending.SynchronisedDate.Should().BeCloseTo(now.AddMinutes(-10), TimeSpan.FromSeconds(5));

            var stale = mine.Single(s => s.Instance == $"{tag}-synced-stale");
            stale.SynchronisationPending.Should().BeFalse();
            stale.InstanceAvailable.Should().BeFalse();
            stale.SynchronisedDate.Should().BeCloseTo(now, TimeSpan.FromSeconds(5));

            var never = mine.Single(s => s.Instance == $"{tag}-never");
            never.SynchronisationPending.Should().BeTrue();
            never.InstanceAvailable.Should().BeTrue();
            never.SynchronisedDate.Should().Be(default);
        }

        [Fact]
        public async Task GetAsyncExcludesEntriesWithHeartbeatOlderThanOneHourAsync()
        {
            var (_, _, tag) = await SeedTwoTenantsAsync();
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync();

            result.Required().Should().NotContain(c => c.Instance == $"{tag}-old");
        }

        [Fact]
        public async Task GetAsyncReturnsNullWhenTenantHasNeverScheduledASynchronisationAsync()
        {
            await using var seedContext = fx.GetDbContext();
            var tenantB = await TenantOfAsync(seedContext, fx.Seed.UserTenantB);
            await InsertEntryAsync(tenantB, $"{DatabaseFixture.Prefix}NoSchedule", DateTime.UtcNow, DateTime.UtcNow);

            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await service.GetAsync()).Should().BeNull();
        }

        [Fact]
        public async Task GetAsyncReturnsEmptyListWhenScheduleExistsButNoRecentHeartbeatsAsync()
        {
            await using var seedContext = fx.GetDbContext();
            var tenantB = await TenantOfAsync(seedContext, fx.Seed.UserTenantB);
            await InsertScheduleAsync(tenantB, DateTime.UtcNow.AddMinutes(-5));
            await InsertEntryAsync(tenantB, $"{DatabaseFixture.Prefix}Ancient", DateTime.UtcNow.AddHours(-5), null);

            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await service.GetAsync()).Should().BeEmpty();
        }

        [Fact]
        public async Task TenantsOnlySeeTheirOwnNodeStatusEntriesAsync()
        {
            var (_, _, tag) = await SeedTwoTenantsAsync();
            await using var dbContext = fx.GetDbContext();

            var fromA = await (await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission)).GetAsync();
            var fromB = await (await BuildServiceAsync(dbContext, fx.Seed.UserTenantB)).GetAsync();

            fromA.Required().Where(w => w.Instance != null && w.Instance.StartsWith(tag)).Select(s => s.Instance)
                .Should()
                .NotContain($"{tag}-B").And.NotBeEmpty();
            fromB.Required().Where(w => w.Instance != null && w.Instance.StartsWith(tag)).Select(s => s.Instance)
                .Should()
                .BeEquivalentTo($"{tag}-B");
        }

        [Fact]
        public async Task ScheduleOfAnotherTenantDoesNotInfluencePendingFlagAsync()
        {
            await using var seedContext = fx.GetDbContext();
            var tenantA = await TenantOfAsync(seedContext, fx.Seed.UserWithPermission);
            var tenantB = await TenantOfAsync(seedContext, fx.Seed.UserTenantB);
            var instance = $"{DatabaseFixture.Prefix}Iso{Guid.NewGuid():N}"[..24];
            var now = DateTime.UtcNow;

            await InsertScheduleAsync(tenantA, now.AddDays(1));
            await InsertScheduleAsync(tenantB, now.AddMinutes(-5));
            await InsertEntryAsync(tenantA, instance, now, now.AddMinutes(-10));

            await using var dbContext = fx.GetDbContext();
            var result = await (await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission)).GetAsync();

            result.Required().Single(s => s.Instance == instance).SynchronisationPending.Should().BeFalse();
        }

        [Fact]
        public async Task PreCancelledTokenThrowsOperationCanceledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetAsync(cts.Token));
        }

        [Fact]
        public async Task PermissionDeniedLogsWarnNotErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync());

            log.Entries.Should().Contain(e => e.Level == "WARN" && e.Message.Contains(fx.Seed.UserWithoutPermission));
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task WithGatesDisabledHappyPathRecordsNothingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog(false);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            await service.GetAsync();

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
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            await service.GetAsync();

            var span = activities.Should()
                .ContainSingle(a => a.OperationName == "EntityAnalysisModelSynchronisationNodeStatusEntries.Get")
                .Subject;
            span.GetTagItem("jube.outcome").Should().Be("ok");
        }

        [Fact]
        public async Task EachCallRecordsOneDurationMeasurementAsync()
        {
            using var collector = new MetricCollector<double>(ServiceDiagnostics.OperationDuration);

            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            await service.GetAsync();

            collector.GetMeasurementSnapshot().Should().ContainSingle(m =>
                (string)m.Tags["operation"].Required() == "Get"
                && (string)m.Tags["area"].Required() == "EntityAnalysisModelSynchronisationNodeStatusEntries");
        }

        [Fact]
        public async Task AuditLogGetsExactlyOneLineAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditLog);

            await service.GetAsync();

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

            await service.GetAsync();
            await Assert.ThrowsAsync<ForbiddenException>(() => forbidden.GetAsync());

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public void CatalogueRegistersUniquePascalCaseNoUnderscoreNames()
        {
            var names = ServiceToolCatalogue.All.Select(t => t.Name).ToList();

            names.Should().OnlyHaveUniqueItems();
            names.Should().OnlyContain(n => !n.Contains('_'));
            names.Should().Contain("EntityAnalysisModelSynchronisationNodeStatusEntriesGet");
        }

        [Fact]
        public async Task PermissionDeniedMessageResolvesForFrenchCultureAsync()
        {
            var original = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo("fr");

                await using var dbContext = fx.GetDbContext();
                var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

                var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync());

                var frenchLocalizer =
                    localizers.Create(typeof(EntityAnalysisModelSynchronisationNodeStatusEntriesResources));
                ex.Message.Should()
                    .Be(frenchLocalizer[EntityAnalysisModelSynchronisationNodeStatusEntriesResources.PermissionDenied]
                        .Value);
            }
            finally
            {
                CultureInfo.CurrentUICulture = original;
            }
        }
    }
}