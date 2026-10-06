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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Dto.Repository.EntityAnalysisModelSynchronisationSchedule;
using Jube.Service.Query.EntityAnalysisModelIntegrity;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Infrastructure.ModelScaffolding;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using ScheduleService =
    Jube.Service.Repository.EntityAnalysisModelSynchronisationSchedule.
    EntityAnalysisModelSynchronisationScheduleService;

namespace Jube.Test.Service.ModelScaffolding
{
    [Collection("Database")]
    public sealed class SyncScheduleEngineTests(ModelScaffoldFixture fixture, DatabaseFixture database)
        : IClassFixture<ModelScaffoldFixture>, IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static readonly TimeSpan patience = TimeSpan.FromSeconds(45);

        private readonly List<string> deadInstances = [];
        private readonly List<int> scheduleIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = database.GetDbContext();
            foreach (var instance in deadInstances)
            {
                await dbContext.EntityAnalysisModelEngineSnapshot.Where(w => w.Instance == instance).DeleteAsync();
                await dbContext.EntityAnalysisModelSynchronisationNodeStatusEntry
                    .Where(w => w.Instance == instance).DeleteAsync();
            }

            foreach (var id in scheduleIds)
            {
                await dbContext.EntityAnalysisModelSynchronisationSchedule.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private async Task<ModelScaffold> CreateModelAsync()
        {
            int tenantRegistryId;
            await using (var dbContext = database.GetDbContext())
            {
                tenantRegistryId = await dbContext.UserInTenant
                    .Where(w => w.User == database.Seed.UserWithPermission)
                    .Select(s => s.TenantRegistryId)
                    .FirstAsync();
            }

            var model = await ModelScaffold.CreateAsync(new ModelScaffoldOptions
            {
                ModelGuid = null,
                TenantRegistryId = tenantRegistryId
            });
            await ModelSync.SyncAsync(fixture.Engine, model);
            return model;
        }

        private async Task<DateTime?> SynchronisedDateAsync(ModelScaffold model)
        {
            await using var dbContext = database.GetDbContext();
            return await dbContext.EntityAnalysisModelSynchronisationNodeStatusEntry
                .Where(w => w.TenantRegistryId == model.TenantRegistryId)
                .Select(s => s.SynchronisedDate)
                .MaxAsync();
        }

        private async Task ScheduleAsync(DateTimeOffset? scheduleDate)
        {
            await using var dbContext = database.GetDbContext();
            var service = await ScheduleService.CreateAsync(dbContext, database.Seed.UserWithPermission,
                TestLog.NoOp, localizers, new NullServiceChangeBus(), TestLog.NoOp);
            var saved = await service.InsertAsync(new EntityAnalysisModelSynchronisationScheduleDto
            {
                ScheduleDate = scheduleDate
            });
            scheduleIds.Add(saved.Id);
        }

        private async Task<DateTime> WaitForSynchronisationAfterAsync(ModelScaffold model, DateTime? baseline)
        {
            var deadline = DateTime.UtcNow + patience;
            while (DateTime.UtcNow < deadline)
            {
                var current = await SynchronisedDateAsync(model);
                if (current is { } moved && (baseline is null || moved > baseline))
                {
                    return moved;
                }

                await Task.Delay(250);
            }

            throw new TimeoutException("The engine did not synchronise the scheduled model in time.");
        }

        [Fact]
        public async Task ASyncNowIsPickedUpByTheEngineAsync()
        {
            await using var model = await CreateModelAsync();
            var baseline = await SynchronisedDateAsync(model);

            await ScheduleAsync(null);

            var moved = await WaitForSynchronisationAfterAsync(model, baseline);
            moved.Should().BeAfter(baseline ?? DateTime.MinValue);
        }

        [Fact]
        public async Task AFutureScheduleIsNotSynchronisedBeforeItsTimeAsync()
        {
            await using var model = await CreateModelAsync();
            var baseline = await SynchronisedDateAsync(model);
            var due = DateTimeOffset.UtcNow.AddSeconds(12);

            await ScheduleAsync(due);

            while (DateTime.UtcNow < due.UtcDateTime.AddSeconds(-2))
            {
                (await SynchronisedDateAsync(model)).Should().Be(baseline,
                    "a schedule that is not yet due is not a pending synchronisation");
                await Task.Delay(250);
            }

            var moved = await WaitForSynchronisationAfterAsync(model, baseline);
            moved.Should().BeOnOrAfter(due.UtcDateTime.AddSeconds(-1),
                "the engine synchronises once the scheduled time has passed");
        }

        [Fact]
        public async Task ASyncOnTheScheduleKeepsTheModelLoadedAsync()
        {
            await using var model = await CreateModelAsync();

            await ScheduleAsync(null);
            await WaitForSynchronisationAfterAsync(model, await SynchronisedDateAsync(model));

            fixture.Engine.FindActiveModel(model.ModelGuid).Should().NotBeNull(
                "a synchronisation on the schedule reloads the model and does not drop it");
        }

        [Fact]
        public async Task ADeadEngineHasItsSnapshotRemovedByTheNextLiveSyncAsync()
        {
            await using var model = await CreateModelAsync();
            var dead = Instance("ZzDeadSnap");
            await SeedDeadEngineAsync(model, dead, TimeSpan.FromMinutes(30));

            await ScheduleAsync(null);
            await WaitForSynchronisationAfterAsync(model, await SynchronisedDateAsync(model));

            await UntilAsync(async () => await SnapshotCountAsync(dead) == 0,
                "the live engine prunes the snapshot of an instance that has stopped reporting");
        }

        [Fact]
        public async Task AWarningForADeadEngineIsRaisedWhileItIsWithinTheRetentionAsync()
        {
            await using var model = await CreateModelAsync();
            var dead = Instance("ZzDeadWarn");
            await SeedDeadEngineAsync(model, dead, TimeSpan.FromMinutes(30));

            var report = await IntegrityAsync(model);

            var warning = report.Checks.Should().ContainSingle(c => c.Code == "EngineNodeStale"
                                                                    && c.Message.Contains(dead)).Subject;
            warning.Severity.Should().Be("Error");
        }

        [Fact]
        public async Task AWarningForADeadEngineDropsOutAfterTheRetentionAsync()
        {
            await using var model = await CreateModelAsync();
            var dead = Instance("ZzDeadGone");
            await SeedDeadEngineAsync(model, dead, TimeSpan.FromMinutes(90));

            var report = await IntegrityAsync(model);

            report.Checks.Should().NotContain(c => c.Code == "EngineNodeStale" && c.Message.Contains(dead));
        }

        [Fact]
        public async Task AWarningForADeadEngineIsClearedByTheNextLiveSyncAsync()
        {
            await using var model = await CreateModelAsync();
            var dead = Instance("ZzDeadSync");
            await SeedDeadEngineAsync(model, dead, TimeSpan.FromMinutes(30));
            (await IntegrityAsync(model)).Checks.Should().ContainSingle(c => c.Code == "EngineNodeStale"
                                                                             && c.Message.Contains(dead));

            await ScheduleAsync(null);
            await WaitForSynchronisationAfterAsync(model, await SynchronisedDateAsync(model));

            await UntilAsync(async () => await NodeRowCountAsync(dead) == 0,
                "the live engine removes the node row of an instance that has stopped reporting");

            var report = await IntegrityAsync(model);

            report.Checks.Should().NotContain(c => c.Code == "EngineNodeStale" && c.Message.Contains(dead));
        }

        [Fact]
        public async Task AnEngineThatStillReportsIsKeptThroughALiveSyncAsync()
        {
            await using var model = await CreateModelAsync();
            var alive = Instance("ZzAliveSync");
            await SeedDeadEngineAsync(model, alive, TimeSpan.FromMinutes(1));

            await ScheduleAsync(null);
            await WaitForSynchronisationAfterAsync(model, await SynchronisedDateAsync(model));

            await using var dbContext = database.GetDbContext();
            (await dbContext.EntityAnalysisModelSynchronisationNodeStatusEntry.CountAsync(w => w.Instance == alive))
                .Should().Be(1);
        }

        private static async Task UntilAsync(Func<Task<bool>> condition, string because)
        {
            var deadline = DateTime.UtcNow + patience;
            while (DateTime.UtcNow < deadline)
            {
                if (await condition())
                {
                    return;
                }

                await Task.Delay(250);
            }

            throw new TimeoutException($"Timed out waiting until {because}.");
        }

        private async Task<int> SnapshotCountAsync(string instance)
        {
            await using var dbContext = database.GetDbContext();
            return await dbContext.EntityAnalysisModelEngineSnapshot.CountAsync(w => w.Instance == instance);
        }

        private async Task<int> NodeRowCountAsync(string instance)
        {
            await using var dbContext = database.GetDbContext();
            return await dbContext.EntityAnalysisModelSynchronisationNodeStatusEntry
                .CountAsync(w => w.Instance == instance);
        }

        private string Instance(string prefix)
        {
            var name = $"{prefix}{Guid.NewGuid():N}"[..24];
            deadInstances.Add(name);
            return name;
        }

        private async Task SeedDeadEngineAsync(ModelScaffold model, string instance, TimeSpan age)
        {
            var heartbeat = DateTime.UtcNow - age;
            await using var dbContext = database.GetDbContext();
            await dbContext.InsertAsync(new EntityAnalysisModelSynchronisationNodeStatusEntry
            {
                Instance = instance,
                TenantRegistryId = model.TenantRegistryId,
                HeartbeatDate = heartbeat,
                SynchronisedDate = heartbeat
            });
            await dbContext.InsertAsync(new EntityAnalysisModelEngineSnapshot
            {
                Instance = instance,
                TenantRegistryId = model.TenantRegistryId,
                EntityAnalysisModelId = model.ModelId,
                Json = "{}",
                CreatedDate = heartbeat
            });
        }

        private async Task<Jube.Dto.Query.EntityAnalysisModelIntegrity.ModelIntegrityReportDto> IntegrityAsync(
            ModelScaffold model)
        {
            await using var dbContext = database.GetDbContext();
            var service = await EntityAnalysisModelIntegrityService.CreateAsync(dbContext, null,
                database.Seed.UserWithPermission, TestLog.NoOp, localizers, new NullServiceChangeBus());
            return await service.CheckAsync(model.ModelId);
        }
    }
}