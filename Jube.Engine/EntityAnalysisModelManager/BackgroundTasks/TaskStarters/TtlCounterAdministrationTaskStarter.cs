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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Cache.Observability.CacheCallCounters;
using Jube.Cache.Redis.Models;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters.TtlCounterAdministration;
using Jube.Extensions;
using Jube.TaskCancellation.TaskHelper;

namespace Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters
{
    using EntityAnalysisModel = EntityAnalysisModel.EntityAnalysisModel;
    using EntityAnalysisModelTtlCounter = EntityAnalysisModel.Models.Models.EntityAnalysisModelTtlCounter;

    public class TtlCounterAdministrationTaskStarter(Context.Context context)
    {
        public async Task StartAsync()
        {
            try
            {
                while (!context.Services.TaskCoordinator.CancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var activeModelsForLoopWithoutEnumError =
                            context.EntityAnalysisModels.ActiveEntityAnalysisModels.ToList();
                        foreach (var (key, value) in
                                 from modelEntityKvp in activeModelsForLoopWithoutEnumError
                                 where modelEntityKvp.Value.Started
                                 select modelEntityKvp)
                        {
                            context.Services.TaskCoordinator.CancellationToken.ThrowIfCancellationRequested();

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity TTL Counter Administration: Entity Model {key} is being started.");
                            }

                            await ProcessAsync(context, value).ConfigureAwait(false);

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity TTL Counter Administration: Entity Model {key} has finished will wait for {context.Services.DynamicEnvironment.AppSettings("WaitTtlCounterDecrement")} milliseconds.");
                            }
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        context.Services.Log.Error(ex.ToString());
                    }
                    finally
                    {
                        await Task.Delay(
                            int.Parse(context.Services.DynamicEnvironment.AppSettings("WaitTtlCounterDecrement")),
                            context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                context.Services.Log.Info(
                    $"Graceful Cancellation TtlCounterAdministrationAsync: has produced an error {ex}");
            }
            catch (Exception ex)
            {
                context.Services.Log.Error($"TtlCounterAdministrationAsync: has produced an error {ex}");
            }
        }

        internal static async Task ProcessAsync(Context.Context context, EntityAnalysisModel entityAnalysisModel)
        {
            try
            {
                if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                {
                    entityAnalysisModel.Services.Log.Info(
                        $"TTL Counter Administration: has started for {entityAnalysisModel.Instance.Id}.  Is about to loop around all TTL Counters.");
                }

                foreach (var ttlCounterWithinLoop in entityAnalysisModel.Collections.ModelTtlCounters)
                {
                    context.Services.TaskCoordinator.CancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                        {
                            entityAnalysisModel.Services.Log.Info(
                                $"TTL Counter Administration: has started for {entityAnalysisModel.Instance.Id} is about to process TTL Counter {ttlCounterWithinLoop.Name} and data name {ttlCounterWithinLoop.TtlCounterDataName}.");
                        }

                        var ttlCounterAdministrationCacheService =
                            new TtlCounterAdministrationCacheService(entityAnalysisModel);

                        var referenceDate = await ttlCounterAdministrationCacheService
                            .CacheServiceGetReferenceDateAsync().ConfigureAwait(false);

                        if (referenceDate.HasValue)
                        {
                            if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                            {
                                entityAnalysisModel.Services.Log.Info(
                                    $"TTL Counter Administration: has started for {entityAnalysisModel.Instance.Id} is about to process TTL Counter {ttlCounterWithinLoop.Name} obtained reference date of {referenceDate}.");
                            }

                            var adjustedTtlCounterDate =
                                ttlCounterAdministrationCacheService.GetAdjustedTtlCounterDate(ttlCounterWithinLoop,
                                    referenceDate.Value);

                            var ttlCounterEntryDeleteLimit =
                                int.Parse(context.Services.DynamicEnvironment.AppSettings(
                                    "TtlCounterEntryDeleteLimit"));

                            var expiredTtlCounterEntries = await ttlCounterAdministrationCacheService
                                .GetAllExpiredByTtlCounterAsync(
                                    entityAnalysisModel.Services.CacheService.CacheTtlCounterEntryRepository,
                                    ttlCounterWithinLoop,
                                    adjustedTtlCounterDate, ttlCounterEntryDeleteLimit).ConfigureAwait(false);

                            if (!expiredTtlCounterEntries.Any())
                            {
                                continue;
                            }

                            var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                                context.Services.DynamicEnvironment.AppSettings("ConnectionString"),
                                context.Services.Log);

                            try
                            {
                                var sortedSetExpiredCount = expiredTtlCounterEntries.Count;
                                var expiredSortedSetMinTimestamp = expiredTtlCounterEntries.FirstOrDefault()
                                    .ReferenceDate.ToUnixTimeMilliSeconds();
                                var expiredSortedSetMaxTimestamp = expiredTtlCounterEntries.LastOrDefault()
                                    .ReferenceDate.ToUnixTimeMilliSeconds();

                                var cacheTtlCounterEntryRemovalBatchRepository =
                                    new CacheTtlCounterEntryRemovalBatchRepository(dbContext);
                                var cacheTtlCounterEntryRemovalBatch = await cacheTtlCounterEntryRemovalBatchRepository
                                    .InsertAsync(new CacheTtlCounterEntryRemovalBatch
                                    {
                                        EntityAnalysisModelTtlCounterGuid = ttlCounterWithinLoop.Guid,
                                        ReferenceDate = referenceDate,
                                        ExpiredHashSetCount = sortedSetExpiredCount,
                                        FirstExpiredHashSetReferenceDate =
                                            expiredSortedSetMinTimestamp.FromUnixTimeMilliSeconds(),
                                        LastExpiredHashSetReferenceDate =
                                            expiredSortedSetMaxTimestamp.FromUnixTimeMilliSeconds()
                                    }, context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                                var maxConcurrency =
                                    int.Parse(context.Services.DynamicEnvironment.AppSettings(
                                        "TtlCounterAdministrationMaxConcurrency"));
                                var concurrencyLimiter = new SemaphoreSlim(maxConcurrency, maxConcurrency);
                                var cacheTtlCounterEntryRemovalBatchEntryList =
                                    new ConcurrentBag<CacheTtlCounterEntryRemovalBatchEntry>();
                                var tasks = new List<Task<TimedTaskResult>>();
                                foreach (var expiredTtlCounterEntry in expiredTtlCounterEntries)
                                {
                                    context.Services.TaskCoordinator.CancellationToken.ThrowIfCancellationRequested();

                                    tasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(
                                        TaskType.ProcessTtlCounterDeprecation, () =>
                                            ProcessTtlCounterDeprecationWithConcurrencyLimitAsync(concurrencyLimiter,
                                                context.Services.TaskCoordinator.CancellationToken, entityAnalysisModel,
                                                ttlCounterAdministrationCacheService, ttlCounterWithinLoop,
                                                expiredTtlCounterEntry,
                                                cacheTtlCounterEntryRemovalBatchEntryList,
                                                cacheTtlCounterEntryRemovalBatch)));
                                }

                                await Task.WhenAll(tasks).ConfigureAwait(false);
                                var cacheTtlCounterEntryRemovalBatchEntryRepository =
                                    new CacheTtlCounterEntryRemovalBatchEntryRepository(dbContext);
                                await cacheTtlCounterEntryRemovalBatchEntryRepository
                                    .BulkCopyAsync(cacheTtlCounterEntryRemovalBatchEntryList).ConfigureAwait(false);

                                var completedTasks = await Task.WhenAll(tasks).ConfigureAwait(false);
                                var cacheTtlCounterEntryRemovalBatchResponseTimeRepository =
                                    new CacheTtlCounterEntryRemovalBatchResponseTimeRepository(dbContext);
                                // ReSharper disable once MethodSupportsCancellation
                                await TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(
                                    TaskType.BulkInsertTtlCounterEntryRemovalBatchResponseTime,
                                    async () => await cacheTtlCounterEntryRemovalBatchResponseTimeRepository
                                        .BulkCopyAsync(AggregateResponseTimesForBulkInsert(completedTasks,
                                            cacheTtlCounterEntryRemovalBatch)).ConfigureAwait(false));
                                await cacheTtlCounterEntryRemovalBatchRepository
                                    .FinishAsync(cacheTtlCounterEntryRemovalBatch.Id).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                entityAnalysisModel.Services.Log.Error(
                                    $"TTL Counter Administration: has started for {entityAnalysisModel.Instance.Id}.  Has created an error {ex}.");
                            }
                            finally
                            {
                                await dbContext.CloseAsync();
                                await dbContext.DisposeAsync();
                            }
                        }
                        else
                        {
                            if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                            {
                                entityAnalysisModel.Services.Log.Info(
                                    $"TTL Counter Administration: Reference Date returned for {entityAnalysisModel.Instance.Id} and {ttlCounterWithinLoop.Name} as null.");
                            }
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        entityAnalysisModel.Services.Log.Error(
                            $"TTL Counter Administration: has produced an error for {ttlCounterWithinLoop.Name} and Data Name {ttlCounterWithinLoop.TtlCounterDataName} as {ex}.");
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                entityAnalysisModel.Services.Log.Error($"TTL Counter Administration: has produced an error as {ex}");
            }
            finally
            {
                if (entityAnalysisModel.Services.Log.IsInfoEnabled)
                {
                    entityAnalysisModel.Services.Log.Info(
                        $"TTL Counter Administration: Model TTL Counter processing for model id {entityAnalysisModel.Instance.Id} has finished.");
                }
            }
        }

        internal static async Task ProcessTtlCounterDeprecationWithConcurrencyLimitAsync(
            SemaphoreSlim concurrencyLimiter, CancellationToken token, EntityAnalysisModel entityAnalysisModel,
            TtlCounterAdministrationCacheService ttlCounterAdministrationCacheService,
            EntityAnalysisModelTtlCounter ttlCounterWithinLoop, ExpiredTtlCounterEntry expiredTtlCounterEntry,
            ConcurrentBag<CacheTtlCounterEntryRemovalBatchEntry> cacheTtlCounterEntryRemovalBatchEntryList,
            CacheTtlCounterEntryRemovalBatch cacheTtlCounterEntryRemovalBatch)
        {
            var waitStopwatch = Stopwatch.StartNew();
            await concurrencyLimiter.WaitAsync(token).ConfigureAwait(false);
            waitStopwatch.Stop();
            CacheCallCounters.Record("TtlCounterAdministrationTaskStarter.ConcurrencyWait",
                waitStopwatch.Elapsed.TotalMilliseconds);

            try
            {
                await ProcessTtlCounterDeprecationAsync(entityAnalysisModel, ttlCounterAdministrationCacheService,
                    ttlCounterWithinLoop, expiredTtlCounterEntry, cacheTtlCounterEntryRemovalBatchEntryList,
                    cacheTtlCounterEntryRemovalBatch).ConfigureAwait(false);
            }
            finally
            {
                concurrencyLimiter.Release();
            }
        }

        internal static async Task ProcessTtlCounterDeprecationAsync(EntityAnalysisModel entityAnalysisModel,
            TtlCounterAdministrationCacheService ttlCounterAdministrationCacheService,
            EntityAnalysisModelTtlCounter ttlCounterWithinLoop, ExpiredTtlCounterEntry expiredTtlCounterEntry,
            ConcurrentBag<CacheTtlCounterEntryRemovalBatchEntry> cacheTtlCounterEntryRemovalBatchEntryList,
            CacheTtlCounterEntryRemovalBatch cacheTtlCounterEntryRemovalBatch)
        {
            var revisedCount = await ttlCounterAdministrationCacheService
                .CacheServiceDecrementTtlCounterAsync(ttlCounterWithinLoop, expiredTtlCounterEntry.DataValue,
                    expiredTtlCounterEntry.Value).ConfigureAwait(false);

            await ttlCounterAdministrationCacheService.CacheServiceDeleteTtlCounterEntryAsync(
                entityAnalysisModel.Services.CacheService.CacheTtlCounterEntryRepository, ttlCounterWithinLoop,
                expiredTtlCounterEntry.DataValue,
                expiredTtlCounterEntry.ReferenceDate).ConfigureAwait(false);

            cacheTtlCounterEntryRemovalBatchEntryList.Add(new CacheTtlCounterEntryRemovalBatchEntry
            {
                CacheTtlCounterEntryRemovalBatchId = cacheTtlCounterEntryRemovalBatch.Id,
                Value = expiredTtlCounterEntry.DataValue,
                DecrementCount = expiredTtlCounterEntry.Value,
                RevisedCount = revisedCount,
                ReferenceDate = expiredTtlCounterEntry.ReferenceDate
            });
        }

        internal static List<CacheTtlCounterEntryRemovalBatchResponseTime> AggregateResponseTimesForBulkInsert(
            TimedTaskResult[] tasks, CacheTtlCounterEntryRemovalBatch cacheTtlCounterEntryRemovalBatch)
        {
            var groupByComputeTime = tasks.GroupBy(g => g.TaskType).Select(s =>
                new CacheTtlCounterEntryRemovalBatchResponseTime
                {
                    TaskTypeId = (int)s.Key,
                    ResponseTime = s.Sum(a => a.ComputeTime),
                    CacheTtlCounterEntryRemovalBatchId = cacheTtlCounterEntryRemovalBatch.Id
                }).ToList();
            return groupByComputeTime;
        }
    }
}