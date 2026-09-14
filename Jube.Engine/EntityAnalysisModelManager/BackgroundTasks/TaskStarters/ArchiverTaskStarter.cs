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
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Repository;
using Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters.Archiver;
using Jube.Engine.Observability;

namespace Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters
{
    using EntityAnalysisModel = EntityAnalysisModel.EntityAnalysisModel;

    public class ArchiverTaskStarter(Context.Context context, int threadSequence)
    {
        public async Task StartAsync()
        {
            while (true)
            {
                try
                {
                    if (context.EntityAnalysisModels.ActiveEntityAnalysisModels is { Count: > 0 })
                    {
                        var processedAny = false;

                        foreach (var model in context.EntityAnalysisModels.ActiveEntityAnalysisModels.Values)
                        {
                            try
                            {
                                if (await TryProcessSingleDequeueForCaseCreationAndArchiverDrainOnCancellationAsync(
                                        model,
                                        context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false))
                                {
                                    processedAny = true;
                                }
                            }
                            catch (Exception ex) when (ex is not OperationCanceledException)
                            {
                                context.Services.Log.Error(
                                    $"TryProcessSingleDequeueForCaseCreationAndArchiverDrainOnCancellationAsync threw an error as {ex}");
                            }
                        }

                        if (!processedAny)
                        {
                            // ReSharper disable once MethodSupportsCancellation
                            await Task.Delay(100).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        if (context.Services.TaskCoordinator.CancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        // ReSharper disable once MethodSupportsCancellation
                        await Task.Delay(100).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException ex)
                {
                    context.Services.Log.Info(
                        $"Graceful Cancellation PersistToActivationWatcherPollingAsync: has produced an error {ex}");
                    break;
                }
                catch (Exception ex)
                {
                    context.Services.Log.Error($"All Entity Models Database Storage: Error {ex}");

                    // ReSharper disable once MethodSupportsCancellation
                    await Task.Delay(100).ConfigureAwait(false);
                }
            }
        }

        private async Task<bool> TryProcessSingleDequeueForCaseCreationAndArchiverDrainOnCancellationAsync(
            EntityAnalysisModel entityAnalysisModel, CancellationToken token = default)
        {
            var found = false;

            try
            {
                ArchiveBuffer buffer;
                if (!entityAnalysisModel.Dependencies.BulkInsertMessageBuffers.TryGetValue(threadSequence, out buffer))
                {
                    token.ThrowIfCancellationRequested();

                    return false;
                }

                if (token.IsCancellationRequested
                    && buffer.Archive.Count == 0
                    && entityAnalysisModel.ConcurrentQueues.PersistToDatabaseAsync.IsEmpty)
                {
                    token.ThrowIfCancellationRequested();
                }

                if (entityAnalysisModel.ConcurrentQueues.PersistToDatabaseAsync.TryDequeue(out var payload))
                {
                    found = true;
                    buffer.LastMessage = DateTime.UtcNow;

                    try
                    {
                        await ArchiverProcessing.CaseCreationAndArchiveStorageAsync(payload,
                            entityAnalysisModel,
                            context.JsonSerializationHelper,
                            buffer,
                            context.ConcurrentQueues.PendingCases,
                            context.Services.DynamicEnvironment, context.Services.CacheService,
                            context.Services.Log, token).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        entityAnalysisModel.Services.Log.Error($"Database Persist: Error processing payload {ex}");
                    }
                }

                var flushDueToTimeout = buffer.LastMessage.AddSeconds(10) <= DateTime.UtcNow;
                var flushDueToThreshold = buffer.Archive.Count >
                                          int.Parse(
                                              context.Services.DynamicEnvironment.AppSettings("BulkCopyThreshold"));
                var flushDueToBufferHavingValues = buffer.Archive.Count > 0 || buffer.ArchiveKeys.Count > 0;

                if ((flushDueToTimeout || flushDueToThreshold) && flushDueToBufferHavingValues)
                {
                    var stopwatch = Stopwatch.StartNew();
                    await ArchiverArchiveRepository.BulkCopyArchiveBufferAsync(
                        entityAnalysisModel.Instance.TenantRegistryId,
                        entityAnalysisModel.Instance.Guid
                        , buffer, context.Services.DynamicEnvironment, context.Services.CacheService,
                        context.Services.Log, token).ConfigureAwait(false);
                    var bulkCopyDuration = (long)(stopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency));
                    entityAnalysisModel.ArchiverStagePerformanceCounters.Record(
                        nameof(ArchiverStage.BulkCopyArchiveBuffer), bulkCopyDuration);
                    EngineDiagnostics.ArchiverStageDuration.Record(bulkCopyDuration / 1000.0,
                        new KeyValuePair<string, object>("stage", nameof(ArchiverStage.BulkCopyArchiveBuffer)));

                    var warnThresholdMicroseconds = int.Parse(
                                                        context.Services.DynamicEnvironment.AppSettings(
                                                            "ArchiverWarnThresholdMilliseconds")) *
                                                    1000L;
                    if (bulkCopyDuration >= warnThresholdMicroseconds)
                    {
                        ArchiverWarningCapture.Enqueue(new ArchiverWarningCaptureRecord(
                            DateTime.UtcNow, entityAnalysisModel.Instance.Guid, entityAnalysisModel.Instance.Name,
                            null, ArchiverStage.BulkCopyArchiveBuffer, bulkCopyDuration));
                        EngineDiagnostics.ArchiverWarnCount.Add(1,
                            new KeyValuePair<string, object>("stage", nameof(ArchiverStage.BulkCopyArchiveBuffer)));
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error(
                    $"TryProcessSingleDequeueForCaseCreationAndArchiverDrainOnCancellationAsync: has produced an error {ex} on thread {threadSequence}.");
            }

            return found;
        }
    }
}