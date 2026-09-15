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
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions;
using Jube.TaskCancellation.TaskHelper;
using Microsoft.VisualBasic;

namespace Jube.Engine.BackgroundTasks.TaskStarters
{
    using EntityAnalysisModel = EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

    public class ManageCountersStarter(Context.Context context)
    {
        public async Task StartAsync()
        {
            try
            {
                while (!context.Services.TaskCoordinator.CancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        foreach (var (key, value) in
                                 from modelEntityKvp in context.Tasks.EntityAnalysisModelManager.Context
                                     .EntityAnalysisModels.ActiveEntityAnalysisModels
                                 where modelEntityKvp.Value.Counters.LastCountersChecked.AddMilliseconds(1000) <
                                       DateTime.UtcNow
                                 select modelEntityKvp)
                        {
                            ClearResponseElevationCounters(value, context.Services.TaskCoordinator.CancellationToken);
                            ClearFrequencyCounters(value, context.Services.TaskCoordinator.CancellationToken);
                            ClearActivationWatcherCounters(value, context.Services.TaskCoordinator.CancellationToken);
                            await UpdateQueueBalancesInDatabaseAtModelLevelAsync(value,
                                context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                            await UpdateCountersInDatabaseAsync(value, key,
                                context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                        }

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                "Counter Management: Counters written.");
                        }

                        await Task.Delay(1000, context.Services.TaskCoordinator.CancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        context.Services.Log.Error(
                            $"Counter Management: An error in counter management has been observed as {ex}.");
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                context.Services.Log.Info($"Graceful Cancellation ManageCountersAsync: has produced an error {ex}");
            }
            catch (Exception ex)
            {
                context.Services.Log.Error($"ManageCountersAsync: has produced an error {ex}");
            }
        }

        private void ClearResponseElevationCounters(EntityAnalysisModel value, CancellationToken token = default)
        {
            try
            {
                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug(
                        "Counter Management: Clearing Billing Response Elevation Balance Entries queue.");
                }

                var cancelled = token.IsCancellationRequested;

                while (value.ConcurrentQueues.ResponseElevationEntries.TryPeek(out var entry))
                {
                    if (cancelled)
                    {
                        value.ConcurrentQueues.ResponseElevationEntries.Clear();

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                "Counter Management: Cancellation requested. Drained Billing Response Elevation Balance Entries queue.");
                        }

                        break;
                    }

                    var expiry = DateAndTime.DateAdd(
                        value.Counters.MaxResponseElevationInterval.ToString(),
                        value.Counters.MaxResponseElevationValue,
                        entry.CreatedDate);

                    if (DateTime.UtcNow <= expiry)
                    {
                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                "Counter Management: No expired entries to remove from Billing Response Elevation Balance Entries queue.");
                        }

                        break;
                    }

                    if (!value.ConcurrentQueues.ResponseElevationEntries.TryDequeue(out entry))
                    {
                        continue;
                    }

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug($"Counter Management: Removed entry with value {entry.Value}.");
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"ClearResponseElevationCounters: has produced an error {ex}");
            }
        }

        private void ClearFrequencyCounters(EntityAnalysisModel value, CancellationToken token = default)
        {
            try
            {
                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug(
                        "Counter Management: Clearing Billing Response Elevation frequency Journal queue.");
                }

                var cancelled = token.IsCancellationRequested;

                while (value.ConcurrentQueues.BillingResponseElevationJournal.TryPeek(out var responseElevationDate))
                {
                    if (cancelled)
                    {
                        value.ConcurrentQueues.BillingResponseElevationJournal.Clear();
                        Interlocked.Exchange(ref value.Counters.ResponseElevationCount, 0);

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                "Counter Management: Cancellation requested. Drained Billing Response Elevation queue.");
                        }

                        break;
                    }

                    var expiry = DateAndTime.DateAdd(
                        value.Counters.MaxResponseElevationInterval.ToString(),
                        value.Counters.MaxResponseElevationValue,
                        responseElevationDate);

                    if (DateTime.UtcNow >= expiry)
                    {
                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                "Counter Management: No expired counters to remove from Billing Response Elevation queue.");
                        }

                        break;
                    }

                    if (!value.ConcurrentQueues.BillingResponseElevationJournal.TryDequeue(out responseElevationDate))
                    {
                        continue;
                    }

                    Interlocked.Decrement(ref value.Counters.ResponseElevationCount);

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Counter Management: Removed entry with date {responseElevationDate}. Decremented Response Elevation Count to {value.Counters.ResponseElevationCount}.");
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"ClearFrequencyCounters: has produced an error {ex}");
            }
        }

        private void ClearActivationWatcherCounters(EntityAnalysisModel value, CancellationToken token = default)
        {
            try
            {
                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug("Counter Management: Clearing Activation Watcher Count Journal queue.");
                }

                var cancelled = token.IsCancellationRequested;

                while (value.ConcurrentQueues.ActivationWatcherCountJournal.TryPeek(out var activationWatcherDate))
                {
                    if (cancelled)
                    {
                        value.ConcurrentQueues.ActivationWatcherCountJournal.Clear();

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                "Counter Management: Cancellation requested. Drained Activation Watcher queue.");
                        }

                        break;
                    }

                    var expiry = DateAndTime.DateAdd(value.Counters.MaxActivationWatcherInterval.ToString(),
                        value.Counters.MaxActivationWatcherValue, activationWatcherDate);

                    if (DateTime.UtcNow <= expiry)
                    {
                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                "Counter Management: No expired counters to remove from Activation Watcher queue.");
                        }

                        break;
                    }

                    if (!value.ConcurrentQueues.ActivationWatcherCountJournal.TryDequeue(out activationWatcherDate))
                    {
                        continue;
                    }

                    Interlocked.Decrement(ref value.Counters.ActivationWatcherCount);

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Counter Management: Removed entry with date {activationWatcherDate}. Decremented Activation Watcher Count to {value.Counters.ActivationWatcherCount}.");
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"ClearActivationWatcherCounters: has produced an error {ex}");
            }
        }

        private async Task UpdateQueueBalancesInDatabaseAtModelLevelAsync(EntityAnalysisModel value,
            CancellationToken token = default)
        {
            try
            {
                if (value.Counters.LastCountersWritten.AddSeconds(60) < DateTime.UtcNow)
                {
                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            "Counter Management: Starting to store queue balances in the database.");
                    }

                    var dbContext =
                        DataConnectionDbContext.GetResilientDbContextDataConnection(
                            context.Services.DynamicEnvironment.AppSettings("ConnectionString"), context.Services.Log);
                    try
                    {
                        var repository = new EntityAnalysisModelAsynchronousQueueBalanceRepository(dbContext);

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                "Counter Management: Has opened a database connection to invoke Insert_Entity_Analysis_Models_Asynchronous_Queue_Balances.");
                        }

                        var insert = new EntityAnalysisModelAsynchronousQueueBalance
                        {
                            Archive = value.ConcurrentQueues.PersistToDatabaseAsync.Count,
                            EntityAnalysisModelGuid = value.Instance.Guid,
                            ActivationWatcher = value.ConcurrentQueues.PersistToDatabaseAsync.Count,
                            CreatedDate = DateTime.UtcNow,
                            Instance = Dns.GetHostName(),
                            ImplicitAsyncOverdue =
                                context.Services.ImplicitAsyncInvocationTracker.PendingCountForModel(value.Instance.Id)
                        };

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Counter Management: Has built the command object to invoke Insert_Entity_Analysis_Models_Asynchronous_Queue_Balances with Entity Analysis Model ID {value.Instance.Id}, Activation Cases 0, Activation Watcher {value.ConcurrentQueues.ResponseElevationEntries.Count}, Billing Response Elevation {value.ConcurrentQueues.ActivationWatcherCountJournal.Count},Billing_Response_Elevation_Balance {value.ConcurrentQueues.BillingResponseElevationJournal.Count}, Billing_Response_Elevation_Balance {value.ConcurrentQueues.BillingResponseElevationJournal.Count}, Node {context.Services.DynamicEnvironment.AppSettings("Node")}.");
                        }

                        await repository.InsertAsync(insert, token).ConfigureAwait(false);

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                "Counter Management: Has opened a database connection to invoke Insert_Entity_Analysis_Models_Asynchronous_Queue_Balances.");
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        context.Services.Log.Error(
                            $"Counter Management: There was an error invoking Insert_Entity_Analysis_Models_Asynchronous_Queue_Balances as {ex}.");
                    }
                    finally
                    {
                        await dbContext.CloseAsync(token).ConfigureAwait(false);
                        await dbContext.DisposeAsync(token).ConfigureAwait(false);

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                "Counter Management: Closed the database connection for invoking Insert_Entity_Analysis_Models_Asynchronous_Queue_Balances.");
                        }
                    }

                    value.Counters.LastCountersWritten = DateTime.UtcNow;

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Counter Management: Updated last queue balances written {value.Counters.LastCountersWritten}.");
                    }
                }
                else
                {
                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            "Counter Management: Has not stored queue balances as the storage period has not lapsed,  every 60 seconds.");
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error(
                    $"UpdateQueueBalancesInDatabaseAtModelLevelAsync: has produced an error {ex}");
            }
        }

        private async Task UpdateCountersInDatabaseAsync(EntityAnalysisModel value, int key,
            CancellationToken token = default)
        {
            try
            {
                if (value.Counters.LastModelInvokeCountersWritten.AddSeconds(60) < DateTime.UtcNow)
                {
                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            "Counter Management: Starting to store model counters in the database.");
                    }

                    var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                        context.Services.DynamicEnvironment.AppSettings("ConnectionString"), context.Services.Log);
                    try
                    {
                        try
                        {
                            var repository = new EntityAnalysisModelProcessingCounterRepository(dbContext);

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Counter Management: Model {key} Has opened a database connection to invoke Insert_Entity_Analysis_Models_Processing_Counters.");
                            }

                            var minModelResponseTime =
                                Interlocked.Exchange(ref value.Counters.MinModelResponseTime, long.MaxValue);

                            var maxModelResponseTime =
                                Interlocked.Exchange(ref value.Counters.MaxModelResponseTime, long.MinValue);

                            int? archiveWalPendingCount = null;
                            try
                            {
                                var walSize = await context.Services.CacheService.CacheWalRepository
                                    .GetWalSizeAsync(value.Instance.TenantRegistryId, value.Instance.Guid,
                                        Dns.GetHostName())
                                    .ConfigureAwait(false);
                                archiveWalPendingCount = (int)Math.Min(walSize, int.MaxValue);
                            }
                            catch (Exception ex) when (ex is not OperationCanceledException)
                            {
                                context.Services.Log.Error(
                                    $"Counter Management: Model {key} There was an error sampling the Archive WAL queue depth as {ex}.");
                            }

                            var model = new EntityAnalysisModelProcessingCounter
                            {
                                ModelInvoke = value.Counters.ModelInvokeCounter,
                                GatewayMatch = value.Counters.ModelInvokeGatewayCounter,
                                ResponseElevation = value.Counters.ModelResponseElevationCounter,
                                ResponseElevationValueLimit = value.Counters.ResponseElevationValueLimitCounter,
                                ResponseElevationLimit = value.Counters.ResponseElevationFrequencyLimitCounter,
                                ModelTotalResponseTime = value.Counters.ModelTotalResponseTime,
                                MinResponseTimeMicroseconds =
                                    value.Counters.ModelInvokeCounter > 0 ? minModelResponseTime : null,
                                MaxResponseTimeMicroseconds =
                                    value.Counters.ModelInvokeCounter > 0 ? maxModelResponseTime : null,
                                ActivationWatcher = value.Counters.ActivationWatcherCount,
                                EntityAnalysisModelGuid = value.Instance.Guid,
                                CreatedDate = DateTime.UtcNow,
                                Instance = Dns.GetHostName(),
                                ImplicitAsyncInvoke = value.Counters.ModelImplicitAsyncInvokeCounter,
                                ImplicitAsyncTimeout = value.Counters.ModelImplicitAsyncTimeoutCounter,
                                ImplicitAsyncCompletedAfterTimeout =
                                    value.Counters.ModelImplicitAsyncCompletedAfterTimeoutCounter,
                                ImplicitAsyncFaultedAfterTimeout =
                                    value.Counters.ModelImplicitAsyncFaultedAfterTimeoutCounter,
                                ArchiveWalPendingCount = archiveWalPendingCount
                            };

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Counter Management: Model {key} Has built command for Insert_Entity_Analysis_Models_Processing_Counters with Model_Invoke_Counter {value.Counters.ModelInvokeCounter},Gateway_Match_Counter {value.Counters.ModelInvokeGatewayCounter},Response_Elevation_Counter {value.Counters.ModelResponseElevationCounter},Response_Elevation_Value_Limit_Counter {value.Counters.ResponseElevationValueLimitCounter},Response_Elevation_Frequency_Limit_Counter {value.Counters.ResponseElevationFrequencyLimitCounter},{value.Instance.Id}.");
                            }

                            await repository.InsertAsync(model, token).ConfigureAwait(false);

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Counter Management: Model {key} Has opened a database connection to invoke Insert_Entity_Analysis_Models_Processing_Counters.");
                            }
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            context.Services.Log.Error(
                                $"Counter Management: Model {key} There was an error invoking Insert_Entity_Analysis_Models_Processing_Counters as {ex}.");
                        }

                        await FlushStagePerformanceCountersAsync(dbContext, value, key, token).ConfigureAwait(false);
                        await FlushResponseTimePipelineCountersAsync(dbContext, value, key, token)
                            .ConfigureAwait(false);
                        await FlushTaskPerformanceCountersAsync(dbContext, value, key, token).ConfigureAwait(false);
                        await FlushArchiverStagePerformanceCountersAsync(dbContext, value, key, token)
                            .ConfigureAwait(false);
                    }
                    finally
                    {
                        await dbContext.CloseAsync(token).ConfigureAwait(false);
                        await dbContext.DisposeAsync(token).ConfigureAwait(false);

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Counter Management: Model {key} closed the database connection for invoking Insert_Entity_Analysis_Models_Processing_Counters.");
                        }
                    }

                    Interlocked.Exchange(ref value.Counters.ResponseElevationCount, 0);
                    Interlocked.Exchange(ref value.Counters.ModelInvokeCounter, 0);
                    Interlocked.Exchange(ref value.Counters.ModelInvokeGatewayCounter, 0);
                    Interlocked.Exchange(ref value.Counters.ModelResponseElevationCounter, 0);
                    Interlocked.Exchange(ref value.Counters.ModelTotalResponseTime, 0);
                    Interlocked.Exchange(ref value.Counters.ResponseElevationValueLimitCounter, 0);
                    Interlocked.Exchange(ref value.Counters.ResponseElevationFrequencyLimitCounter, 0);
                    Interlocked.Exchange(ref value.Counters.ActivationWatcherCount, 0);
                    Interlocked.Exchange(ref value.Counters.ModelImplicitAsyncInvokeCounter, 0);
                    Interlocked.Exchange(ref value.Counters.ModelImplicitAsyncTimeoutCounter, 0);
                    Interlocked.Exchange(ref value.Counters.ModelImplicitAsyncCompletedAfterTimeoutCounter, 0);
                    Interlocked.Exchange(ref value.Counters.ModelImplicitAsyncFaultedAfterTimeoutCounter, 0);

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Counter Management: Model {key} has reset all model counters.");
                    }

                    value.Counters.LastModelInvokeCountersWritten = DateTime.UtcNow;

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Counter Management: Model {key} updated last counters written {value.Counters.LastModelInvokeCountersWritten}.");
                    }
                }
                else
                {
                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Counter Management: Model {key} has not stored counters as the storage period has not lapsed,  every 60 seconds.");
                    }
                }

                value.Counters.LastCountersChecked = DateTime.UtcNow;

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug(
                        $"Counter Management: Model {key} updated last counters checked {value.Counters.LastCountersChecked}.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"UpdateCountersInDatabaseAsync: has produced an error {ex}");
            }
        }

        private async Task FlushStagePerformanceCountersAsync(DbContext dbContext, EntityAnalysisModel value, int key,
            CancellationToken token = default)
        {
            var stageNames = value.StagePerformanceCounters.Stages.Keys.ToList();
            if (stageNames.Count == 0)
            {
                return;
            }

            try
            {
                var repository = new EntityAnalysisModelStagePerformanceCounterRepository(dbContext);

                foreach (var stageName in stageNames)
                {
                    if (!value.StagePerformanceCounters.Stages.TryGetValue(stageName, out var accumulator))
                    {
                        continue;
                    }

                    var totalMicroseconds = Interlocked.Exchange(ref accumulator.TotalMicroseconds, 0);
                    var minMicroseconds = Interlocked.Exchange(ref accumulator.MinMicroseconds, long.MaxValue);
                    var maxMicroseconds = Interlocked.Exchange(ref accumulator.MaxMicroseconds, long.MinValue);
                    var invokeCount = Interlocked.Exchange(ref accumulator.InvokeCount, 0);

                    if (invokeCount == 0)
                    {
                        continue;
                    }

                    await repository.InsertAsync(new EntityAnalysisModelStagePerformanceCounter
                    {
                        EntityAnalysisModelGuid = value.Instance.Guid,
                        StageId = (int)Enum.Parse<InvokeStage>(stageName),
                        TotalMicroseconds = totalMicroseconds,
                        MinMicroseconds = minMicroseconds,
                        MaxMicroseconds = maxMicroseconds,
                        InvokeCount = invokeCount,
                        CreatedDate = DateTime.UtcNow,
                        Instance = Dns.GetHostName()
                    }, token).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error(
                    $"Counter Management: Model {key} There was an error invoking Insert_Entity_Analysis_Model_Stage_Performance_Counters as {ex}.");
            }
        }

        private async Task FlushArchiverStagePerformanceCountersAsync(DbContext dbContext, EntityAnalysisModel value,
            int key, CancellationToken token = default)
        {
            var stageNames = value.ArchiverStagePerformanceCounters.Stages.Keys.ToList();
            if (stageNames.Count == 0)
            {
                return;
            }

            try
            {
                var repository = new ArchiverStagePerformanceCounterRepository(dbContext);

                foreach (var stageName in stageNames)
                {
                    if (!value.ArchiverStagePerformanceCounters.Stages.TryGetValue(stageName, out var accumulator))
                    {
                        continue;
                    }

                    var totalMicroseconds = Interlocked.Exchange(ref accumulator.TotalMicroseconds, 0);
                    var minMicroseconds = Interlocked.Exchange(ref accumulator.MinMicroseconds, long.MaxValue);
                    var maxMicroseconds = Interlocked.Exchange(ref accumulator.MaxMicroseconds, long.MinValue);
                    var invokeCount = Interlocked.Exchange(ref accumulator.InvokeCount, 0);

                    if (invokeCount == 0)
                    {
                        continue;
                    }

                    await repository.InsertAsync(new ArchiverStagePerformanceCounter
                    {
                        EntityAnalysisModelGuid = value.Instance.Guid,
                        StageId = (int)Enum.Parse<ArchiverStage>(stageName),
                        TotalMicroseconds = totalMicroseconds,
                        MinMicroseconds = minMicroseconds,
                        MaxMicroseconds = maxMicroseconds,
                        InvokeCount = invokeCount,
                        CreatedDate = DateTime.UtcNow,
                        Instance = Dns.GetHostName()
                    }, token).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error(
                    $"Counter Management: Model {key} There was an error invoking Insert_Archiver_Stage_Performance_Counters as {ex}.");
            }
        }

        private async Task FlushResponseTimePipelineCountersAsync(DbContext dbContext, EntityAnalysisModel value,
            int key, CancellationToken token = default)
        {
            var stageNames = value.ResponseTimePipelineCounters.Stages.Keys.ToList();
            if (stageNames.Count == 0)
            {
                return;
            }

            try
            {
                var repository = new EntityAnalysisModelResponseTimePipelineCounterRepository(dbContext);

                foreach (var stageName in stageNames)
                {
                    if (!value.ResponseTimePipelineCounters.Stages.TryGetValue(stageName, out var accumulator))
                    {
                        continue;
                    }

                    var totalMicroseconds = Interlocked.Exchange(ref accumulator.TotalMicroseconds, 0);
                    var minMicroseconds = Interlocked.Exchange(ref accumulator.MinMicroseconds, long.MaxValue);
                    var maxMicroseconds = Interlocked.Exchange(ref accumulator.MaxMicroseconds, long.MinValue);
                    var totalAllocatedBytes = Interlocked.Exchange(ref accumulator.TotalAllocatedBytes, 0);
                    var minAllocatedBytes = Interlocked.Exchange(ref accumulator.MinAllocatedBytes, long.MaxValue);
                    var maxAllocatedBytes = Interlocked.Exchange(ref accumulator.MaxAllocatedBytes, long.MinValue);
                    var invokeCount = Interlocked.Exchange(ref accumulator.InvokeCount, 0);

                    if (invokeCount == 0)
                    {
                        continue;
                    }

                    var sequenceNumber = Array.IndexOf(ResponseTimePipelineExtensions.StageSequence, stageName);

                    await repository.InsertAsync(new EntityAnalysisModelResponseTimePipelineCounter
                    {
                        EntityAnalysisModelGuid = value.Instance.Guid,
                        StageId = (int)Enum.Parse<InvokeStage>(stageName),
                        SequenceNumber = sequenceNumber >= 0
                            ? sequenceNumber
                            : ResponseTimePipelineExtensions.StageSequence.Length,
                        TotalMicroseconds = totalMicroseconds,
                        MinMicroseconds = minMicroseconds,
                        MaxMicroseconds = maxMicroseconds,
                        TotalAllocatedBytes = totalAllocatedBytes,
                        MinAllocatedBytes = minAllocatedBytes,
                        MaxAllocatedBytes = maxAllocatedBytes,
                        InvokeCount = invokeCount,
                        CreatedDate = DateTime.UtcNow,
                        Instance = Dns.GetHostName()
                    }, token).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error(
                    $"Counter Management: Model {key} There was an error invoking Insert_Entity_Analysis_Model_Response_Time_Pipeline_Counters as {ex}.");
            }
        }

        private async Task FlushTaskPerformanceCountersAsync(DbContext dbContext, EntityAnalysisModel value, int key,
            CancellationToken token = default)
        {
            var taskKeys = value.TaskPerformanceCounters.Tasks.Keys.ToList();
            if (taskKeys.Count == 0)
            {
                return;
            }

            try
            {
                var repository = new EntityAnalysisModelTaskPerformanceCounterRepository(dbContext);

                foreach (var taskKey in taskKeys)
                {
                    if (!value.TaskPerformanceCounters.Tasks.TryGetValue(taskKey, out var accumulator))
                    {
                        continue;
                    }

                    var totalMicroseconds = Interlocked.Exchange(ref accumulator.TotalMicroseconds, 0);
                    var minMicroseconds = Interlocked.Exchange(ref accumulator.MinMicroseconds, long.MaxValue);
                    var maxMicroseconds = Interlocked.Exchange(ref accumulator.MaxMicroseconds, long.MinValue);
                    var totalAllocatedBytes = Interlocked.Exchange(ref accumulator.TotalAllocatedBytes, 0);
                    var minAllocatedBytes = Interlocked.Exchange(ref accumulator.MinAllocatedBytes, long.MaxValue);
                    var maxAllocatedBytes = Interlocked.Exchange(ref accumulator.MaxAllocatedBytes, long.MinValue);
                    var invokeCount = Interlocked.Exchange(ref accumulator.InvokeCount, 0);

                    if (invokeCount == 0)
                    {
                        continue;
                    }

                    var separatorIndex = taskKey.IndexOf('.');
                    var direction = separatorIndex >= 0 ? taskKey[..separatorIndex] : taskKey;
                    var taskName = separatorIndex >= 0 ? taskKey[(separatorIndex + 1)..] : taskKey;

                    await repository.InsertAsync(new EntityAnalysisModelTaskPerformanceCounter
                    {
                        EntityAnalysisModelGuid = value.Instance.Guid,
                        DirectionId = (int)Enum.Parse<TaskDirection>(direction),
                        TaskTypeId = (int)Enum.Parse<TaskType>(taskName),
                        TotalMicroseconds = totalMicroseconds,
                        MinMicroseconds = minMicroseconds,
                        MaxMicroseconds = maxMicroseconds,
                        TotalAllocatedBytes = totalAllocatedBytes,
                        MinAllocatedBytes = minAllocatedBytes,
                        MaxAllocatedBytes = maxAllocatedBytes,
                        InvokeCount = invokeCount,
                        CreatedDate = DateTime.UtcNow,
                        Instance = Dns.GetHostName()
                    }, token).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error(
                    $"Counter Management: Model {key} There was an error invoking Insert_Entity_Analysis_Model_Task_Performance_Counters as {ex}.");
            }
        }
    }
}