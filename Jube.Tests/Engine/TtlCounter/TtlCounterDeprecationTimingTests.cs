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
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions;
using Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters;
using Jube.TaskCancellation;
using Jube.Test.Infrastructure;
using LinqToDB;
using Xunit;
using BackgroundContext = Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.Context.Context;
using EntityAnalysisModelDomain = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;
using EntityAnalysisModelTtlCounter =
    Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelTtlCounter;

namespace Jube.Test.Engine.TtlCounter
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class TtlCounterDeprecationTimingTests : IAsyncLifetime
    {
        private readonly List<Guid> createdCounterGuidsForCleanup = [];

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
            if (createdCounterGuidsForCleanup.Count == 0)
            {
                return;
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                ConnectionString, TestLog.NoOp);

            var batchIds = await dbContext.CacheTtlCounterEntryRemovalBatch
                .Where(w => createdCounterGuidsForCleanup.Contains(w.EntityAnalysisModelTtlCounterGuid))
                .Select(s => s.Id).ToListAsync();

            if (batchIds.Count == 0)
            {
                return;
            }

            await dbContext.GetTable<CacheTtlCounterEntryRemovalBatchResponseTime>()
                .Where(w => batchIds.Contains(w.CacheTtlCounterEntryRemovalBatchId)).DeleteAsync();
            await dbContext.CacheTtlCounterEntryRemovalBatchEntry
                .Where(w => batchIds.Contains(w.CacheTtlCounterEntryRemovalBatchId)).DeleteAsync();
            await dbContext.CacheTtlCounterEntryRemovalBatch
                .Where(w => batchIds.Contains(w.Id)).DeleteAsync();
        }

        private static EntityAnalysisModelDomain NewModel(int tenantRegistryId)
        {
            var cacheService = TestCacheService.Create(out _);
            return new EntityAnalysisModelDomain
            {
                Started = true,
                Instance =
                {
                    TenantRegistryId = tenantRegistryId,
                    Guid = Guid.NewGuid(),
                    Id = tenantRegistryId
                },
                Services =
                {
                    Log = TestLog.NoOp,
                    CacheService = cacheService
                }
            };
        }

        private static int ResolveFastPollMilliseconds()
        {
            var productionDefault = int.Parse(TestDynamicEnvironment.Create().AppSettings("WaitTtlCounterDecrement"));
            return Math.Max(50, productionDefault / 5);
        }


        [Fact]
        public async Task OnlineAggregationCountExcludesAnEntryOnceItAgesPastTheIntervalAsync()
        {
            var tenant = 910001;
            var model = NewModel(tenant);
            var counter = new EntityAnalysisModelTtlCounter
            {
                Guid = Guid.NewGuid(),
                Name = "OnlineCount",
                TtlCounterDataName = "AccountId",
                TtlCounterInterval = "n",
                TtlCounterValue = 1,
                OnlineAggregation = true
            };
            const string dataValue = "ACC-ONLINE-COUNT";
            var incrementedAt = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            await model.Services.CacheService.CacheTtlCounterRepository.IncrementTtlCounterCacheAsync(
                tenant, model.Instance.Guid, counter.TtlCounterDataName, dataValue, counter.Guid, 1, incrementedAt);
            await model.Services.CacheService.CacheTtlCounterEntryRepository.UpsertAsync(
                tenant, model.Instance.Guid, counter.TtlCounterDataName, dataValue, counter.Guid, incrementedAt, 1);

            var windowStartNow = TtlCounterExtensions.ApplyTtlCounterInterval(incrementedAt,
                counter.TtlCounterInterval, counter.TtlCounterValue);
            var sumNow = await model.Services.CacheService.CacheTtlCounterEntryRepository
                .GetAggregationPreferReplicaAsync(tenant, model.Instance.Guid, counter.Guid,
                    counter.TtlCounterDataName, dataValue, windowStartNow, incrementedAt);
            sumNow.Should().Be(1, "the increment is still within its one-minute window");

            var oneMinuteLater = incrementedAt.AddMinutes(1).AddSeconds(1);
            var windowStartLater = TtlCounterExtensions.ApplyTtlCounterInterval(oneMinuteLater,
                counter.TtlCounterInterval, counter.TtlCounterValue);
            var sumLater = await model.Services.CacheService.CacheTtlCounterEntryRepository
                .GetAggregationPreferReplicaAsync(tenant, model.Instance.Guid, counter.Guid,
                    counter.TtlCounterDataName, dataValue, windowStartLater, oneMinuteLater);
            sumLater.Should().Be(0,
                "the entry has aged out of the online-aggregation window after a full interval has elapsed");
        }

        [Fact]
        public async Task OnlineAggregationSumExcludesEntriesOnceTheyAgePastTheIntervalAsync()
        {
            var tenant = 910002;
            var model = NewModel(tenant);
            var counter = new EntityAnalysisModelTtlCounter
            {
                Guid = Guid.NewGuid(),
                Name = "OnlineSum",
                TtlCounterDataName = "MerchantId",
                TtlCounterInterval = "n",
                TtlCounterValue = 1,
                EnableSum = true,
                TtlCounterDataValue = "Amount",
                OnlineAggregation = true
            };
            const string dataValue = "MER-ONLINE-SUM";
            var firstAt = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var secondAt = firstAt.AddSeconds(20);

            foreach (var (at, amount) in new[] { (firstAt, 40.25), (secondAt, 17.75) })
            {
                await model.Services.CacheService.CacheTtlCounterRepository.IncrementTtlCounterCacheAsync(
                    tenant, model.Instance.Guid, counter.TtlCounterDataName, dataValue, counter.Guid, amount, at);
                await model.Services.CacheService.CacheTtlCounterEntryRepository.UpsertAsync(
                    tenant, model.Instance.Guid, counter.TtlCounterDataName, dataValue, counter.Guid, at, amount);
            }

            var windowStart = TtlCounterExtensions.ApplyTtlCounterInterval(secondAt, counter.TtlCounterInterval,
                counter.TtlCounterValue);
            var sumBothWithinWindow = await model.Services.CacheService.CacheTtlCounterEntryRepository
                .GetAggregationPreferReplicaAsync(tenant, model.Instance.Guid, counter.Guid,
                    counter.TtlCounterDataName, dataValue, windowStart, secondAt);
            sumBothWithinWindow.Should().Be(58.0, "both sum increments (40.25 + 17.75) are still within the window");

            var oneMinuteAfterSecond = secondAt.AddMinutes(1).AddSeconds(1);
            var windowStartLater = TtlCounterExtensions.ApplyTtlCounterInterval(oneMinuteAfterSecond,
                counter.TtlCounterInterval, counter.TtlCounterValue);
            var sumAfterBothExpired = await model.Services.CacheService.CacheTtlCounterEntryRepository
                .GetAggregationPreferReplicaAsync(tenant, model.Instance.Guid, counter.Guid,
                    counter.TtlCounterDataName, dataValue, windowStartLater, oneMinuteAfterSecond);
            sumAfterBothExpired.Should().Be(0, "both entries have aged out of the window");
        }

        [Fact]
        public async Task OnlineAggregationIncludesOnlyTheEntryStillWithinTheWindowWhenAnOlderOneHasAgedOutAsync()
        {
            var tenant = 910003;
            var model = NewModel(tenant);
            var counter = new EntityAnalysisModelTtlCounter
            {
                Guid = Guid.NewGuid(),
                Name = "OnlinePartialExpiry",
                TtlCounterDataName = "AccountId",
                TtlCounterInterval = "n",
                TtlCounterValue = 1,
                OnlineAggregation = true
            };
            const string dataValue = "ACC-PARTIAL";
            var oldAt = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var newAt = oldAt.AddSeconds(50);
            var now = newAt.AddSeconds(15);

            await model.Services.CacheService.CacheTtlCounterRepository.IncrementTtlCounterCacheAsync(
                tenant, model.Instance.Guid, counter.TtlCounterDataName, dataValue, counter.Guid, 1, oldAt);
            await model.Services.CacheService.CacheTtlCounterEntryRepository.UpsertAsync(
                tenant, model.Instance.Guid, counter.TtlCounterDataName, dataValue, counter.Guid, oldAt, 1);
            await model.Services.CacheService.CacheTtlCounterRepository.IncrementTtlCounterCacheAsync(
                tenant, model.Instance.Guid, counter.TtlCounterDataName, dataValue, counter.Guid, 1, newAt);
            await model.Services.CacheService.CacheTtlCounterEntryRepository.UpsertAsync(
                tenant, model.Instance.Guid, counter.TtlCounterDataName, dataValue, counter.Guid, newAt, 1);

            var windowStart = TtlCounterExtensions.ApplyTtlCounterInterval(now, counter.TtlCounterInterval,
                counter.TtlCounterValue);

            windowStart.Should().BeAfter(oldAt, "the test setup must place the old entry before the window opens");
            windowStart.Should().BeBefore(newAt, "the test setup must keep the new entry inside the window");

            var sum = await model.Services.CacheService.CacheTtlCounterEntryRepository
                .GetAggregationPreferReplicaAsync(tenant, model.Instance.Guid, counter.Guid,
                    counter.TtlCounterDataName, dataValue, windowStart, now);
            sum.Should().Be(1, "only the newer entry is still within the one-minute window; the older one aged out");
        }

        [Fact]
        public async Task OnlineAggregationIncludesAnEntryExactlyOnTheWindowsLowerBoundaryAsync()
        {
            var tenant = 910004;
            var model = NewModel(tenant);
            var counter = new EntityAnalysisModelTtlCounter
            {
                Guid = Guid.NewGuid(),
                Name = "OnlineBoundary",
                TtlCounterDataName = "AccountId",
                TtlCounterInterval = "n",
                TtlCounterValue = 1,
                OnlineAggregation = true
            };
            const string dataValue = "ACC-BOUNDARY";
            var now = new DateTime(2025, 1, 1, 12, 1, 0, DateTimeKind.Utc);
            var windowStart = TtlCounterExtensions.ApplyTtlCounterInterval(now, counter.TtlCounterInterval,
                counter.TtlCounterValue);

            await model.Services.CacheService.CacheTtlCounterRepository.IncrementTtlCounterCacheAsync(
                tenant, model.Instance.Guid, counter.TtlCounterDataName, dataValue, counter.Guid, 1, windowStart);
            await model.Services.CacheService.CacheTtlCounterEntryRepository.UpsertAsync(
                tenant, model.Instance.Guid, counter.TtlCounterDataName, dataValue, counter.Guid, windowStart, 1);

            var sum = await model.Services.CacheService.CacheTtlCounterEntryRepository
                .GetAggregationPreferReplicaAsync(tenant, model.Instance.Guid, counter.Guid,
                    counter.TtlCounterDataName, dataValue, windowStart, now);
            sum.Should().Be(1,
                "an entry exactly on the window's lower boundary must be included (the read path is inclusive)");
        }

        [Fact]
        public async Task OnlineAggregationDoesNotCrossContaminateBetweenDifferentDataValuesAsync()
        {
            var tenant = 910005;
            var model = NewModel(tenant);
            var counter = new EntityAnalysisModelTtlCounter
            {
                Guid = Guid.NewGuid(),
                Name = "OnlineIsolation",
                TtlCounterDataName = "AccountId",
                TtlCounterInterval = "n",
                TtlCounterValue = 1,
                OnlineAggregation = true
            };
            var at = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            await model.Services.CacheService.CacheTtlCounterRepository.IncrementTtlCounterCacheAsync(
                tenant, model.Instance.Guid, counter.TtlCounterDataName, "ACC-A", counter.Guid, 3, at);
            await model.Services.CacheService.CacheTtlCounterEntryRepository.UpsertAsync(
                tenant, model.Instance.Guid, counter.TtlCounterDataName, "ACC-A", counter.Guid, at, 3);
            await model.Services.CacheService.CacheTtlCounterRepository.IncrementTtlCounterCacheAsync(
                tenant, model.Instance.Guid, counter.TtlCounterDataName, "ACC-B", counter.Guid, 9, at);
            await model.Services.CacheService.CacheTtlCounterEntryRepository.UpsertAsync(
                tenant, model.Instance.Guid, counter.TtlCounterDataName, "ACC-B", counter.Guid, at, 9);

            var windowStart = TtlCounterExtensions.ApplyTtlCounterInterval(at, counter.TtlCounterInterval,
                counter.TtlCounterValue);

            var sumA = await model.Services.CacheService.CacheTtlCounterEntryRepository
                .GetAggregationPreferReplicaAsync(tenant, model.Instance.Guid, counter.Guid,
                    counter.TtlCounterDataName, "ACC-A", windowStart, at);
            var sumB = await model.Services.CacheService.CacheTtlCounterEntryRepository
                .GetAggregationPreferReplicaAsync(tenant, model.Instance.Guid, counter.Guid,
                    counter.TtlCounterDataName, "ACC-B", windowStart, at);

            sumA.Should().Be(3, "ACC-A's aggregation must not include ACC-B's increment");
            sumB.Should().Be(9, "ACC-B's aggregation must not include ACC-A's increment");
        }


        private async Task<(EntityAnalysisModelDomain model, EntityAnalysisModelTtlCounter counter,
                CancellationTokenProvider cancellationTokenProvider, Task loopTask, CancellationTokenSource pumpCts,
                Task pumpTask, int pollMs)>
            RunBatchAdministrationAgainstAFreshlyIncrementedCounterAsync(int tenant, string ttlCounterInterval,
                int ttlCounterValue, string dataName, string dataValue, double incrementValue, bool enableSum)
        {
            var model = NewModel(tenant);
            var counter = new EntityAnalysisModelTtlCounter
            {
                Guid = Guid.NewGuid(),
                Name = enableSum ? "BatchSum" : "BatchCount",
                TtlCounterDataName = dataName,
                TtlCounterInterval = ttlCounterInterval,
                TtlCounterValue = ttlCounterValue,
                EnableSum = enableSum,
                TtlCounterDataValue = enableSum ? "Amount" : null
            };
            createdCounterGuidsForCleanup.Add(counter.Guid);
            model.Collections.ModelTtlCounters.Add(counter);

            var pollMs = ResolveFastPollMilliseconds();
            var dynamicEnvironment = TestDynamicEnvironment.Create(new Dictionary<string, string>
            {
                ["ConnectionString"] = ConnectionString,
                ["WaitTtlCounterDecrement"] = pollMs.ToString()
            });

            var cancellationTokenProvider = new CancellationTokenProvider();
            var context = new BackgroundContext
            {
                Services =
                {
                    Log = TestLog.NoOp,
                    DynamicEnvironment = dynamicEnvironment,
                    TaskCoordinator = new TaskCoordinator(cancellationTokenProvider)
                }
            };
            context.EntityAnalysisModels.ActiveEntityAnalysisModels[model.Instance.Id] = model;

            var startedAt = DateTime.UtcNow;
            await model.Services.CacheService.CacheReferenceDateRepository.UpsertReferenceDateAsync(
                tenant, model.Instance.Guid, startedAt);

            await model.Services.CacheService.CacheTtlCounterRepository.IncrementTtlCounterCacheAsync(
                tenant, model.Instance.Guid, dataName, dataValue, counter.Guid, incrementValue, startedAt);
            await model.Services.CacheService.CacheTtlCounterEntryRepository.UpsertAsync(
                tenant, model.Instance.Guid, dataName, dataValue, counter.Guid, startedAt, incrementValue);

            var immediately = await model.Services.CacheService.CacheTtlCounterRepository
                .GetByNameDataNameDataValueAsync(tenant, model.Instance.Guid, counter.Guid, dataName, dataValue);
            immediately.Should().Be(incrementValue, "nothing should be decremented before the interval has elapsed");

            var starter = new TtlCounterAdministrationTaskStarter(context);
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

            await Task.Delay(pollMs * 2, pumpCts.Token);
            var stillPresentBeforeInterval = await model.Services.CacheService.CacheTtlCounterRepository
                .GetByNameDataNameDataValueAsync(tenant, model.Instance.Guid, counter.Guid, dataName, dataValue);
            stillPresentBeforeInterval.Should().Be(incrementValue,
                "the configured interval has not elapsed yet, so the loop must not have touched it");

            return (model, counter, cancellationTokenProvider, loopTask, pumpCts, pumpTask, pollMs);
        }

        private static Task CancelBatchAdministrationAsync(CancellationTokenProvider cancellationTokenProvider,
            CancellationTokenSource pumpCts)
        {
            cancellationTokenProvider.Cancel();
            return pumpCts.CancelAsync();
        }

        [Fact]
        public async Task BatchAdministrationDecrementsAndRemovesAnExpiredCountCounterWithinAShortRealDelayAsync()
        {
            const int tenant = 910101;
            const string dataName = "AccountId";
            const string dataValue = "ACC-BATCH-COUNT";
            const double incrementValue = 5;
            const int ttlCounterValueSeconds = 2;

            var (model, counter, cancellationTokenProvider, loopTask, pumpCts, pumpTask, pollMs) =
                await RunBatchAdministrationAgainstAFreshlyIncrementedCounterAsync(tenant, "s",
                    ttlCounterValueSeconds, dataName, dataValue, incrementValue, false);

            await Task.Delay(TimeSpan.FromSeconds(ttlCounterValueSeconds) + TimeSpan.FromMilliseconds(pollMs * 4));

            await CancelBatchAdministrationAsync(cancellationTokenProvider, pumpCts);
            await Task.WhenAny(Task.WhenAll(loopTask, pumpTask), Task.Delay(TimeSpan.FromSeconds(5)));

            var remaining = await model.Services.CacheService.CacheTtlCounterRepository
                .GetByNameDataNameDataValueAsync(tenant, model.Instance.Guid, counter.Guid, dataName, dataValue);
            remaining.Should().Be(0, "the counter should have fully decremented once its interval elapsed");

            var stillExpired = await model.Services.CacheService.CacheTtlCounterEntryRepository
                .GetAllExpiredByTtlCounterPreferReplicaAsync(tenant, model.Instance.Guid, counter.Guid, dataName,
                    DateTime.UtcNow, 100);
            stillExpired.Should().BeEmpty(
                "the entry should have been removed from both the entry hash and the expiry index");

            await using var dbContext =
                DataConnectionDbContext.GetResilientDbContextDataConnection(ConnectionString, TestLog.NoOp);

            var batch = await dbContext.CacheTtlCounterEntryRemovalBatch
                .Where(w => w.EntityAnalysisModelTtlCounterGuid == counter.Guid)
                .OrderByDescending(o => o.Id)
                .FirstOrDefaultAsync();
            batch.Should().NotBeNull(
                "the background administration loop should have recorded a removal batch -- the same table " +
                "queried manually to confirm deprecation");
            batch!.FinishedDate.Should().NotBeNull("the batch should have completed, not been left hanging");
            batch.ExpiredHashSetCount.Should().Be(1);

            var entry = await dbContext.CacheTtlCounterEntryRemovalBatchEntry
                .Where(w => w.CacheTtlCounterEntryRemovalBatchId == batch.Id)
                .SingleOrDefaultAsync();
            entry.Should().NotBeNull();
            entry!.Value.Should().Be(dataValue);
            entry.DecrementCount.Should().Be(incrementValue);
            entry.RevisedCount.Should().Be(0);
        }

        [Fact]
        public async Task BatchAdministrationDecrementsAndRemovesAnExpiredSumCounterWithinAShortRealDelayAsync()
        {
            const int tenant = 910102;
            const string dataName = "MerchantId";
            const string dataValue = "MER-BATCH-SUM";
            const double incrementValue = 123.45;
            const int ttlCounterValueSeconds = 2;

            var (model, counter, cancellationTokenProvider, loopTask, pumpCts, pumpTask, pollMs) =
                await RunBatchAdministrationAgainstAFreshlyIncrementedCounterAsync(tenant, "s",
                    ttlCounterValueSeconds, dataName, dataValue, incrementValue, true);

            await Task.Delay(TimeSpan.FromSeconds(ttlCounterValueSeconds) + TimeSpan.FromMilliseconds(pollMs * 4));

            await CancelBatchAdministrationAsync(cancellationTokenProvider, pumpCts);
            await Task.WhenAny(Task.WhenAll(loopTask, pumpTask), Task.Delay(TimeSpan.FromSeconds(5)));

            var remaining = await model.Services.CacheService.CacheTtlCounterRepository
                .GetByNameDataNameDataValueAsync(tenant, model.Instance.Guid, counter.Guid, dataName, dataValue);
            remaining.Should().Be(0, "the sum counter should have fully decremented once its interval elapsed");

            await using var dbContext =
                DataConnectionDbContext.GetResilientDbContextDataConnection(ConnectionString, TestLog.NoOp);

            var batch = await dbContext.CacheTtlCounterEntryRemovalBatch
                .Where(w => w.EntityAnalysisModelTtlCounterGuid == counter.Guid)
                .OrderByDescending(o => o.Id)
                .FirstOrDefaultAsync();
            batch.Should().NotBeNull();
            batch!.FinishedDate.Should().NotBeNull();

            var entry = await dbContext.CacheTtlCounterEntryRemovalBatchEntry
                .Where(w => w.CacheTtlCounterEntryRemovalBatchId == batch.Id)
                .SingleOrDefaultAsync();
            entry.Should().NotBeNull();
            entry!.Value.Should().Be(dataValue);
            entry.DecrementCount.Should().Be(incrementValue,
                "the audit trail must record the summed amount, not a bare count of 1");
            entry.RevisedCount.Should().Be(0);
        }

        [Fact]
        public async Task BatchAdministrationDoesNotPruneACounterWellBeforeItsLongerIntervalHasElapsedAsync()
        {
            const int tenant = 910103;
            const string dataName = "AccountId";
            const string dataValue = "ACC-BATCH-NOT-YET";
            const double incrementValue = 7;
            const int ttlCounterValueSeconds = 8;

            var (model, counter, cancellationTokenProvider, loopTask, pumpCts, pumpTask, pollMs) =
                await RunBatchAdministrationAgainstAFreshlyIncrementedCounterAsync(tenant, "s",
                    ttlCounterValueSeconds, dataName, dataValue, incrementValue, false);

            await Task.Delay(pollMs * 3);
            var stillPresent = await model.Services.CacheService.CacheTtlCounterRepository
                .GetByNameDataNameDataValueAsync(tenant, model.Instance.Guid, counter.Guid, dataName, dataValue);

            await CancelBatchAdministrationAsync(cancellationTokenProvider, pumpCts);
            await Task.WhenAny(Task.WhenAll(loopTask, pumpTask), Task.Delay(TimeSpan.FromSeconds(5)));

            stillPresent.Should().Be(incrementValue,
                "the eight-second interval is nowhere near elapsed; the loop must not have touched the counter " +
                "even though it has run several real cycles");

            await using var dbContext =
                DataConnectionDbContext.GetResilientDbContextDataConnection(ConnectionString, TestLog.NoOp);
            var batchExists = await dbContext.CacheTtlCounterEntryRemovalBatch
                .AnyAsync(w => w.EntityAnalysisModelTtlCounterGuid == counter.Guid);
            batchExists.Should().BeFalse("nothing has expired yet, so no removal batch should have been recorded");
        }

        [Fact]
        public async Task BatchAdministrationNeverPrunesALiveForeverCounterAsync()
        {
            const int tenant = 910104;
            const string dataName = "AccountId";
            const string dataValue = "ACC-LIVE-FOREVER";
            const double incrementValue = 11;

            var model = NewModel(tenant);
            var counter = new EntityAnalysisModelTtlCounter
            {
                Guid = Guid.NewGuid(),
                Name = "LiveForever",
                TtlCounterDataName = dataName,
                TtlCounterInterval = "s",
                TtlCounterValue = 1,
                EnableLiveForever = true
            };
            createdCounterGuidsForCleanup.Add(counter.Guid);
            model.Collections.ModelTtlCounters.Add(counter);

            var pollMs = ResolveFastPollMilliseconds();
            var dynamicEnvironment = TestDynamicEnvironment.Create(new Dictionary<string, string>
            {
                ["ConnectionString"] = ConnectionString,
                ["WaitTtlCounterDecrement"] = pollMs.ToString()
            });
            var cancellationTokenProvider = new CancellationTokenProvider();
            var context = new BackgroundContext
            {
                Services =
                {
                    Log = TestLog.NoOp,
                    DynamicEnvironment = dynamicEnvironment,
                    TaskCoordinator = new TaskCoordinator(cancellationTokenProvider)
                }
            };
            context.EntityAnalysisModels.ActiveEntityAnalysisModels[model.Instance.Id] = model;

            var startedAt = DateTime.UtcNow;
            await model.Services.CacheService.CacheReferenceDateRepository.UpsertReferenceDateAsync(
                tenant, model.Instance.Guid, startedAt);

            await model.Services.CacheService.CacheTtlCounterRepository.IncrementTtlCounterCacheAsync(
                tenant, model.Instance.Guid, dataName, dataValue, counter.Guid, incrementValue, startedAt);

            var starter = new TtlCounterAdministrationTaskStarter(context);
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

            await Task.Delay(TimeSpan.FromSeconds(2) + TimeSpan.FromMilliseconds(pollMs * 4), pumpCts.Token);

            await CancelBatchAdministrationAsync(cancellationTokenProvider, pumpCts);
            await Task.WhenAny(Task.WhenAll(loopTask, pumpTask), Task.Delay(TimeSpan.FromSeconds(5)));

            var remaining = await model.Services.CacheService.CacheTtlCounterRepository
                .GetByNameDataNameDataValueAsync(tenant, model.Instance.Guid, counter.Guid, dataName, dataValue);
            remaining.Should().Be(incrementValue,
                "a Live Forever counter must never be decremented, however many real cycles the loop has run");

            var neverIndexed = await model.Services.CacheService.CacheTtlCounterEntryRepository
                .GetAllExpiredByTtlCounterPreferReplicaAsync(tenant, model.Instance.Guid, counter.Guid, dataName,
                    DateTime.UtcNow, 100);
            neverIndexed.Should().BeEmpty("no expiry-index entry should ever have been created for it");

            await using var dbContext =
                DataConnectionDbContext.GetResilientDbContextDataConnection(ConnectionString, TestLog.NoOp);
            var batchExists = await dbContext.CacheTtlCounterEntryRemovalBatch
                .AnyAsync(w => w.EntityAnalysisModelTtlCounterGuid == counter.Guid, token: pumpCts.Token);
            batchExists.Should().BeFalse(
                "the administration loop finds nothing expired for a Live Forever counter, so it never records " +
                "a removal batch for it");
        }

        [Fact]
        public async Task BatchAdministrationPrunesAZeroIntervalCounterOnAnEarlyCycleAsync()
        {
            const int tenant = 910105;
            const string dataName = "AccountId";
            const string dataValue = "ACC-ZERO-INTERVAL";
            const double incrementValue = 3;

            var model = NewModel(tenant);
            var counter = new EntityAnalysisModelTtlCounter
            {
                Guid = Guid.NewGuid(),
                Name = "ZeroInterval",
                TtlCounterDataName = dataName,
                TtlCounterInterval = "s",
                TtlCounterValue = 0
            };
            createdCounterGuidsForCleanup.Add(counter.Guid);
            model.Collections.ModelTtlCounters.Add(counter);

            var pollMs = ResolveFastPollMilliseconds();
            var dynamicEnvironment = TestDynamicEnvironment.Create(new Dictionary<string, string>
            {
                ["ConnectionString"] = ConnectionString,
                ["WaitTtlCounterDecrement"] = pollMs.ToString()
            });
            var cancellationTokenProvider = new CancellationTokenProvider();
            var context = new BackgroundContext
            {
                Services =
                {
                    Log = TestLog.NoOp,
                    DynamicEnvironment = dynamicEnvironment,
                    TaskCoordinator = new TaskCoordinator(cancellationTokenProvider)
                }
            };
            context.EntityAnalysisModels.ActiveEntityAnalysisModels[model.Instance.Id] = model;

            var startedAt = DateTime.UtcNow;
            await model.Services.CacheService.CacheReferenceDateRepository.UpsertReferenceDateAsync(
                tenant, model.Instance.Guid, startedAt);
            await model.Services.CacheService.CacheTtlCounterRepository.IncrementTtlCounterCacheAsync(
                tenant, model.Instance.Guid, dataName, dataValue, counter.Guid, incrementValue, startedAt);
            await model.Services.CacheService.CacheTtlCounterEntryRepository.UpsertAsync(
                tenant, model.Instance.Guid, dataName, dataValue, counter.Guid, startedAt, incrementValue);

            var starter = new TtlCounterAdministrationTaskStarter(context);
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

            await Task.Delay(pollMs * 5, pumpCts.Token);

            await CancelBatchAdministrationAsync(cancellationTokenProvider, pumpCts);
            await Task.WhenAny(Task.WhenAll(loopTask, pumpTask), Task.Delay(TimeSpan.FromSeconds(5)));

            var remaining = await model.Services.CacheService.CacheTtlCounterRepository
                .GetByNameDataNameDataValueAsync(tenant, model.Instance.Guid, counter.Guid, dataName, dataValue);
            remaining.Should().Be(0, "a zero-interval counter should be pruned on one of the very first cycles");
        }

        [Fact]
        public async Task BatchAdministrationHandlesMultipleCountersOnTheSameModelIndependentlyAsync()
        {
            const int tenant = 910106;
            const string dataName = "AccountId";
            const string shortDataValue = "ACC-SHORT";
            const string longDataValue = "ACC-LONG";
            const double incrementValue = 4;

            var model = NewModel(tenant);
            var shortCounter = new EntityAnalysisModelTtlCounter
            {
                Guid = Guid.NewGuid(),
                Name = "ShortLived",
                TtlCounterDataName = dataName,
                TtlCounterInterval = "s",
                TtlCounterValue = 2
            };
            var longCounter = new EntityAnalysisModelTtlCounter
            {
                Guid = Guid.NewGuid(),
                Name = "LongLived",
                TtlCounterDataName = dataName,
                TtlCounterInterval = "s",
                TtlCounterValue = 15
            };
            createdCounterGuidsForCleanup.Add(shortCounter.Guid);
            createdCounterGuidsForCleanup.Add(longCounter.Guid);
            model.Collections.ModelTtlCounters.Add(shortCounter);
            model.Collections.ModelTtlCounters.Add(longCounter);

            var pollMs = ResolveFastPollMilliseconds();
            var dynamicEnvironment = TestDynamicEnvironment.Create(new Dictionary<string, string>
            {
                ["ConnectionString"] = ConnectionString,
                ["WaitTtlCounterDecrement"] = pollMs.ToString()
            });
            var cancellationTokenProvider = new CancellationTokenProvider();
            var context = new BackgroundContext
            {
                Services =
                {
                    Log = TestLog.NoOp,
                    DynamicEnvironment = dynamicEnvironment,
                    TaskCoordinator = new TaskCoordinator(cancellationTokenProvider)
                }
            };
            context.EntityAnalysisModels.ActiveEntityAnalysisModels[model.Instance.Id] = model;

            var startedAt = DateTime.UtcNow;
            await model.Services.CacheService.CacheReferenceDateRepository.UpsertReferenceDateAsync(
                tenant, model.Instance.Guid, startedAt);

            await model.Services.CacheService.CacheTtlCounterRepository.IncrementTtlCounterCacheAsync(
                tenant, model.Instance.Guid, dataName, shortDataValue, shortCounter.Guid, incrementValue, startedAt);
            await model.Services.CacheService.CacheTtlCounterEntryRepository.UpsertAsync(
                tenant, model.Instance.Guid, dataName, shortDataValue, shortCounter.Guid, startedAt, incrementValue);
            await model.Services.CacheService.CacheTtlCounterRepository.IncrementTtlCounterCacheAsync(
                tenant, model.Instance.Guid, dataName, longDataValue, longCounter.Guid, incrementValue, startedAt);
            await model.Services.CacheService.CacheTtlCounterEntryRepository.UpsertAsync(
                tenant, model.Instance.Guid, dataName, longDataValue, longCounter.Guid, startedAt, incrementValue);

            var starter = new TtlCounterAdministrationTaskStarter(context);
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

            await Task.Delay(TimeSpan.FromSeconds(2) + TimeSpan.FromMilliseconds(pollMs * 4), pumpCts.Token);

            await CancelBatchAdministrationAsync(cancellationTokenProvider, pumpCts);
            await Task.WhenAny(Task.WhenAll(loopTask, pumpTask), Task.Delay(TimeSpan.FromSeconds(5)));

            var shortRemaining = await model.Services.CacheService.CacheTtlCounterRepository
                .GetByNameDataNameDataValueAsync(tenant, model.Instance.Guid, shortCounter.Guid, dataName,
                    shortDataValue);
            var longRemaining = await model.Services.CacheService.CacheTtlCounterRepository
                .GetByNameDataNameDataValueAsync(tenant, model.Instance.Guid, longCounter.Guid, dataName,
                    longDataValue);

            shortRemaining.Should().Be(0, "the two-second counter's interval has elapsed and should be pruned");
            longRemaining.Should().Be(incrementValue,
                "the fifteen-second counter on the same model must be left untouched in the same cycle");
        }

        [Fact]
        public async Task BatchAdministrationNeverTouchesAModelThatIsNotStartedAsync()
        {
            const int tenant = 910107;
            const string dataName = "AccountId";
            const string dataValue = "ACC-NOT-STARTED";
            const double incrementValue = 6;

            var model = NewModel(tenant);
            model.Started = false;
            var counter = new EntityAnalysisModelTtlCounter
            {
                Guid = Guid.NewGuid(),
                Name = "NotStarted",
                TtlCounterDataName = dataName,
                TtlCounterInterval = "s",
                TtlCounterValue = 1
            };
            createdCounterGuidsForCleanup.Add(counter.Guid);
            model.Collections.ModelTtlCounters.Add(counter);

            var pollMs = ResolveFastPollMilliseconds();
            var dynamicEnvironment = TestDynamicEnvironment.Create(new Dictionary<string, string>
            {
                ["ConnectionString"] = ConnectionString,
                ["WaitTtlCounterDecrement"] = pollMs.ToString()
            });
            var cancellationTokenProvider = new CancellationTokenProvider();
            var context = new BackgroundContext
            {
                Services =
                {
                    Log = TestLog.NoOp,
                    DynamicEnvironment = dynamicEnvironment,
                    TaskCoordinator = new TaskCoordinator(cancellationTokenProvider)
                }
            };
            context.EntityAnalysisModels.ActiveEntityAnalysisModels[model.Instance.Id] = model;

            var startedAt = DateTime.UtcNow;
            await model.Services.CacheService.CacheReferenceDateRepository.UpsertReferenceDateAsync(
                tenant, model.Instance.Guid, startedAt);
            await model.Services.CacheService.CacheTtlCounterRepository.IncrementTtlCounterCacheAsync(
                tenant, model.Instance.Guid, dataName, dataValue, counter.Guid, incrementValue, startedAt);
            await model.Services.CacheService.CacheTtlCounterEntryRepository.UpsertAsync(
                tenant, model.Instance.Guid, dataName, dataValue, counter.Guid, startedAt, incrementValue);

            var starter = new TtlCounterAdministrationTaskStarter(context);
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

            await Task.Delay(TimeSpan.FromSeconds(2) + TimeSpan.FromMilliseconds(pollMs * 4), pumpCts.Token);

            await CancelBatchAdministrationAsync(cancellationTokenProvider, pumpCts);
            await Task.WhenAny(Task.WhenAll(loopTask, pumpTask), Task.Delay(TimeSpan.FromSeconds(5)));

            var remaining = await model.Services.CacheService.CacheTtlCounterRepository
                .GetByNameDataNameDataValueAsync(tenant, model.Instance.Guid, counter.Guid, dataName, dataValue);
            remaining.Should().Be(incrementValue,
                "the model was never Started, so the administration loop's active-model filter must skip it " +
                "entirely, however long its interval has elapsed for");

            await using var dbContext =
                DataConnectionDbContext.GetResilientDbContextDataConnection(ConnectionString, TestLog.NoOp);
            var batchExists = await dbContext.CacheTtlCounterEntryRemovalBatch
                .AnyAsync(w => w.EntityAnalysisModelTtlCounterGuid == counter.Guid, token: pumpCts.Token);
            batchExists.Should().BeFalse("a model that was never started should never be processed at all");
        }


        [Theory]
        [InlineData("s", 1)]
        [InlineData("s", 30)]
        [InlineData("n", 1)]
        [InlineData("n", 15)]
        [InlineData("h", 1)]
        [InlineData("h", 6)]
        [InlineData("d", 1)]
        [InlineData("d", 30)]
        [InlineData("m", 1)]
        [InlineData("m", 3)]
        [InlineData("y", 1)]
        public async Task BatchAdministrationWindsBackEveryIntervalUnitAndValueOnceTheReferenceDateJumpsPastItAsync(
            string ttlCounterInterval, int ttlCounterValue)
        {
            var tenant = 910300 + Math.Abs((ttlCounterInterval + ttlCounterValue).GetHashCode() % 90);
            const string dataName = "AccountId";
            var dataValue = $"ACC-MATRIX-{ttlCounterInterval}-{ttlCounterValue}";
            const double incrementValue = 2;

            var model = NewModel(tenant);
            var counter = new EntityAnalysisModelTtlCounter
            {
                Guid = Guid.NewGuid(),
                Name = $"Matrix{ttlCounterInterval}{ttlCounterValue}",
                TtlCounterDataName = dataName,
                TtlCounterInterval = ttlCounterInterval,
                TtlCounterValue = ttlCounterValue
            };
            createdCounterGuidsForCleanup.Add(counter.Guid);
            model.Collections.ModelTtlCounters.Add(counter);

            var pollMs = ResolveFastPollMilliseconds();
            var dynamicEnvironment = TestDynamicEnvironment.Create(new Dictionary<string, string>
            {
                ["ConnectionString"] = ConnectionString,
                ["WaitTtlCounterDecrement"] = pollMs.ToString()
            });
            var cancellationTokenProvider = new CancellationTokenProvider();
            var context = new BackgroundContext
            {
                Services =
                {
                    Log = TestLog.NoOp,
                    DynamicEnvironment = dynamicEnvironment,
                    TaskCoordinator = new TaskCoordinator(cancellationTokenProvider)
                }
            };
            context.EntityAnalysisModels.ActiveEntityAnalysisModels[model.Instance.Id] = model;

            var startedAt = DateTime.UtcNow;
            await model.Services.CacheService.CacheReferenceDateRepository.UpsertReferenceDateAsync(
                tenant, model.Instance.Guid, startedAt);
            await model.Services.CacheService.CacheTtlCounterRepository.IncrementTtlCounterCacheAsync(
                tenant, model.Instance.Guid, dataName, dataValue, counter.Guid, incrementValue, startedAt);
            await model.Services.CacheService.CacheTtlCounterEntryRepository.UpsertAsync(
                tenant, model.Instance.Guid, dataName, dataValue, counter.Guid, startedAt, incrementValue);

            var starter = new TtlCounterAdministrationTaskStarter(context);
            var loopTask = starter.StartAsync();

            await Task.Delay(pollMs * 2);
            var stillPresent = await model.Services.CacheService.CacheTtlCounterRepository
                .GetByNameDataNameDataValueAsync(tenant, model.Instance.Guid, counter.Guid, dataName, dataValue);
            stillPresent.Should().Be(incrementValue,
                $"a {ttlCounterValue}{ttlCounterInterval} interval has not elapsed yet at the original " +
                "reference date");

            var jumpedReferenceDate = ttlCounterInterval switch
            {
                "s" => startedAt.AddSeconds(ttlCounterValue + 2),
                "n" => startedAt.AddMinutes(ttlCounterValue).AddSeconds(5),
                "h" => startedAt.AddHours(ttlCounterValue).AddMinutes(1),
                "d" => startedAt.AddDays(ttlCounterValue).AddMinutes(1),
                "m" => startedAt.AddMonths(ttlCounterValue).AddHours(1),
                "y" => startedAt.AddYears(ttlCounterValue).AddDays(1),
                _ => startedAt
            };
            await model.Services.CacheService.CacheReferenceDateRepository.UpsertReferenceDateAsync(
                tenant, model.Instance.Guid, jumpedReferenceDate);

            await Task.Delay(pollMs * 4);

            cancellationTokenProvider.Cancel();
            await Task.WhenAny(loopTask, Task.Delay(TimeSpan.FromSeconds(5)));

            var remaining = await model.Services.CacheService.CacheTtlCounterRepository
                .GetByNameDataNameDataValueAsync(tenant, model.Instance.Guid, counter.Guid, dataName, dataValue);
            remaining.Should().Be(0,
                $"a {ttlCounterValue}{ttlCounterInterval} counter must wind back once the reference date has " +
                "jumped past its interval, regardless of the unit");

            var stillExpired = await model.Services.CacheService.CacheTtlCounterEntryRepository
                .GetAllExpiredByTtlCounterPreferReplicaAsync(tenant, model.Instance.Guid, counter.Guid, dataName,
                    jumpedReferenceDate, 100);
            stillExpired.Should().BeEmpty();
        }
    }
}