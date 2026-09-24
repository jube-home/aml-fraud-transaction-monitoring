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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions;
using Jube.TaskCancellation;
using Jube.Test.Infrastructure;
using LinqToDB;
using Xunit;
using BackgroundContext = Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.Context.Context;
using EntityAnalysisModelDomain = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;
using SyncContext = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Context;

namespace Jube.Test.Engine.TtlCounter
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class TtlCounterModelSyncTests : IAsyncLifetime
    {
        private readonly List<Guid> createdCounterGuidsForCleanup = [];
        private readonly List<int> createdModelIdsForCleanup = [];

        private static string ConnectionString =>
            Environment.GetEnvironmentVariable("JubeTestConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionString")
            ??
            "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=SuperSecretPasswordToChangeForPg;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;";

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext =
                DataConnectionDbContext.GetResilientDbContextDataConnection(ConnectionString, TestLog.NoOp);

            if (createdCounterGuidsForCleanup.Count > 0)
            {
                var batchIds = await dbContext.CacheTtlCounterEntryRemovalBatch
                    .Where(w => createdCounterGuidsForCleanup.Contains(w.EntityAnalysisModelTtlCounterGuid))
                    .Select(s => s.Id).ToListAsync();

                if (batchIds.Count > 0)
                {
                    await dbContext.GetTable<CacheTtlCounterEntryRemovalBatchResponseTime>()
                        .Where(w => batchIds.Contains(w.CacheTtlCounterEntryRemovalBatchId)).DeleteAsync();
                    await dbContext.CacheTtlCounterEntryRemovalBatchEntry
                        .Where(w => batchIds.Contains(w.CacheTtlCounterEntryRemovalBatchId)).DeleteAsync();
                    await dbContext.CacheTtlCounterEntryRemovalBatch
                        .Where(w => batchIds.Contains(w.Id)).DeleteAsync();
                }
            }

            foreach (var modelId in createdModelIdsForCleanup)
            {
                await dbContext.GetTable<EntityAnalysisModelTtlCounter>()
                    .Where(w => w.EntityAnalysisModelId == modelId).DeleteAsync();
                await dbContext.GetTable<EntityAnalysisModelVersion>()
                    .Where(w => w.EntityAnalysisModelId == modelId).DeleteAsync();
                await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
            }
        }

        private async Task<int> CreateModelAsync(DbContext dbContext)
        {
            var saved = await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModel
            {
                Name = $"ZzTestSyncModel{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0
            });
            createdModelIdsForCleanup.Add(saved);
            return saved;
        }

        [Fact]
        public async Task SyncPopulatesTheInMemoryCounterFromARealRowIncludingNullFieldDefaultsAsync()
        {
            await using var dbContext =
                DataConnectionDbContext.GetResilientDbContextDataConnection(ConnectionString, TestLog.NoOp);

            var modelId = await CreateModelAsync(dbContext);
            var counterGuid = Guid.NewGuid();

            await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModelTtlCounter
            {
                EntityAnalysisModelId = modelId,
                Name = "Sync Test Counter",
                TtlCounterDataName = "Account Id",
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Guid = counterGuid,
                TtlCounterInterval = null,
                TtlCounterValue = null,
                ResolutionInterval = null,
                OnlineAggregation = null,
                EnableSum = null,
                EnableLiveForever = null,
                ResponsePayload = null,
                ReportTable = null,
                TtlCounterDataValue = null
            });
            createdCounterGuidsForCleanup.Add(counterGuid);

            var model = new EntityAnalysisModelDomain
            {
                Instance = { Id = modelId, Guid = Guid.NewGuid() }
            };

            var context = new SyncContext
            {
                Services =
                {
                    DbContext = dbContext,
                    Log = TestLog.NoOp,
                    Parser = new Parser.Parser(TestLog.NoOp, [])
                },
                EntityAnalysisModels =
                {
                    ActiveEntityAnalysisModels = new Dictionary<int, EntityAnalysisModelDomain> { [modelId] = model }
                }
            };

            await context.SyncEntityAnalysisModelTtlCountersAsync();

            model.Collections.ModelTtlCounters.Should().ContainSingle(
                "sync should have loaded the one active TTL Counter definition for this model");
            var synced = model.Collections.ModelTtlCounters.Single();

            synced.Guid.Should().Be(counterGuid);
            synced.Name.Should().Be("Sync_Test_Counter", "sync replaces spaces in the name with underscores");
            synced.TtlCounterDataName.Should().Be("Account_Id",
                "sync replaces spaces in the data name with underscores too");
            synced.TtlCounterInterval.Should().Be("d", "a null TtlCounterInterval defaults to days");
            synced.TtlCounterValue.Should().Be(0, "a null TtlCounterValue defaults to zero");
            synced.ResolutionInterval.Should().Be("d", "a null ResolutionInterval defaults to days");
            synced.OnlineAggregation.Should().BeTrue(
                "sync defaults OnlineAggregation to true when the database column is null");
            synced.EnableSum.Should().BeFalse();
            synced.EnableLiveForever.Should().BeFalse();
            synced.ResponsePayload.Should().BeFalse();
            synced.ReportTable.Should().BeFalse();
        }

        [Fact]
        public async Task SyncSkipsAnInactiveCounterDefinitionAsync()
        {
            await using var dbContext =
                DataConnectionDbContext.GetResilientDbContextDataConnection(ConnectionString, TestLog.NoOp);

            var modelId = await CreateModelAsync(dbContext);
            var counterGuid = Guid.NewGuid();

            await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModelTtlCounter
            {
                EntityAnalysisModelId = modelId,
                Name = "Inactive Counter",
                TtlCounterDataName = "AccountId",
                Active = 0,
                Locked = 0,
                Deleted = 0,
                Guid = counterGuid,
                TtlCounterInterval = "d",
                TtlCounterValue = 1
            });
            createdCounterGuidsForCleanup.Add(counterGuid);

            var model = new EntityAnalysisModelDomain
            {
                Instance = { Id = modelId, Guid = Guid.NewGuid() }
            };
            var context = new SyncContext
            {
                Services =
                {
                    DbContext = dbContext,
                    Log = TestLog.NoOp,
                    Parser = new Parser.Parser(TestLog.NoOp, [])
                },
                EntityAnalysisModels =
                {
                    ActiveEntityAnalysisModels = new Dictionary<int, EntityAnalysisModelDomain> { [modelId] = model }
                }
            };

            await context.SyncEntityAnalysisModelTtlCountersAsync();

            model.Collections.ModelTtlCounters.Should().BeEmpty(
                "an inactive TTL Counter definition must never be loaded into the live model");
        }

        [Fact]
        public async Task ASyncedCounterIsPickedUpByTheRealAdministrationLoopAndDeprecatesOnScheduleAsync()
        {
            const int tenant = 910201;
            const string dataValue = "ACC-SYNCED";
            const double incrementValue = 9;
            const int ttlCounterValueSeconds = 2;

            await using var dbContext =
                DataConnectionDbContext.GetResilientDbContextDataConnection(ConnectionString, TestLog.NoOp);

            var modelId = await CreateModelAsync(dbContext);
            var counterGuid = Guid.NewGuid();

            await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModelTtlCounter
            {
                EntityAnalysisModelId = modelId,
                Name = "Synced Batch Counter",
                TtlCounterDataName = "AccountId",
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Guid = counterGuid,
                TtlCounterInterval = "s",
                TtlCounterValue = ttlCounterValueSeconds,
                OnlineAggregation = 0,
                EnableSum = 0
            });
            createdCounterGuidsForCleanup.Add(counterGuid);

            var model = new EntityAnalysisModelDomain
            {
                Started = true,
                Instance = { Id = modelId, TenantRegistryId = tenant, Guid = Guid.NewGuid() },
                Services = { Log = TestLog.NoOp, CacheService = TestCacheService.Create(out _) }
            };

            var syncContext = new SyncContext
            {
                Services =
                {
                    DbContext = dbContext,
                    Log = TestLog.NoOp,
                    Parser = new Parser.Parser(TestLog.NoOp, [])
                },
                EntityAnalysisModels =
                {
                    ActiveEntityAnalysisModels = new Dictionary<int, EntityAnalysisModelDomain> { [modelId] = model }
                }
            };

            await syncContext.SyncEntityAnalysisModelTtlCountersAsync();

            model.Collections.ModelTtlCounters.Should().ContainSingle();
            var syncedCounter = model.Collections.ModelTtlCounters.Single();
            var dataName = syncedCounter.TtlCounterDataName;

            var pollMs = Math.Max(50,
                int.Parse(TestDynamicEnvironment.Create().AppSettings("WaitTtlCounterDecrement")) / 5);
            var dynamicEnvironment = TestDynamicEnvironment.Create(new Dictionary<string, string>
            {
                ["ConnectionString"] = ConnectionString,
                ["WaitTtlCounterDecrement"] = pollMs.ToString()
            });
            var cancellationTokenProvider = new CancellationTokenProvider();
            var backgroundContext = new BackgroundContext
            {
                Services =
                {
                    Log = TestLog.NoOp,
                    DynamicEnvironment = dynamicEnvironment,
                    TaskCoordinator = new TaskCoordinator(cancellationTokenProvider)
                }
            };
            backgroundContext.EntityAnalysisModels.ActiveEntityAnalysisModels[model.Instance.Id] = model;

            var startedAt = DateTime.UtcNow;
            await model.Services.CacheService.CacheReferenceDateRepository.UpsertReferenceDateAsync(
                tenant, model.Instance.Guid, startedAt);
            await model.Services.CacheService.CacheTtlCounterRepository.IncrementTtlCounterCacheAsync(
                tenant, model.Instance.Guid, dataName, dataValue, syncedCounter.Guid, incrementValue, startedAt);
            await model.Services.CacheService.CacheTtlCounterEntryRepository.UpsertAsync(
                tenant, model.Instance.Guid, dataName, dataValue, syncedCounter.Guid, startedAt, incrementValue);

            var starter = new TtlCounterAdministrationTaskStarter(backgroundContext);
            var loopTask = starter.StartAsync();

            var pumpCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenProvider.Token);
            var pumpTask = Task.Run(async () =>
            {
                while (!pumpCts.IsCancellationRequested)
                {
                    await model.Services.CacheService.CacheReferenceDateRepository.UpsertReferenceDateAsync(
                        tenant, model.Instance.Guid, DateTime.UtcNow);
                    try
                    {
                        await Task.Delay(pollMs, pumpCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }, pumpCts.Token);

            await Task.Delay(TimeSpan.FromSeconds(ttlCounterValueSeconds) + TimeSpan.FromMilliseconds(pollMs * 4),
                pumpCts.Token);

            cancellationTokenProvider.Cancel();
            await pumpCts.CancelAsync();
            await Task.WhenAny(Task.WhenAll(loopTask, pumpTask),
                Task.Delay(TimeSpan.FromSeconds(5), CancellationToken.None));

            var remaining = await model.Services.CacheService.CacheTtlCounterRepository
                .GetByNameDataNameDataValueAsync(tenant, model.Instance.Guid, syncedCounter.Guid, dataName,
                    dataValue);
            remaining.Should().Be(0,
                "a counter definition loaded through the real sync call stack must be pruned by the real " +
                "administration loop just like a hand-built one");

            var batch = await dbContext.CacheTtlCounterEntryRemovalBatch
                .Where(w => w.EntityAnalysisModelTtlCounterGuid == syncedCounter.Guid)
                .OrderByDescending(o => o.Id)
                .FirstOrDefaultAsync(token: CancellationToken.None);
            batch.Should().NotBeNull("the removal batch audit table must reflect the synced counter's Guid");
            batch!.FinishedDate.Should().NotBeNull();
        }
    }
}