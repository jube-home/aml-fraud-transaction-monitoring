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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jube.Cache;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Engine.BackgroundTasks.TaskStarters.Case;
using Jube.Engine.EntityAnalysisModelInvoke.Models.CaseManagement;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.Helpers;
using Jube.Engine.Observability;
using log4net;

namespace Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters.Archiver
{
    using EntityAnalysisModel = EntityAnalysisModel.EntityAnalysisModel;

    public static class ArchiverProcessing
    {
        public static async Task CaseCreationAndArchiveStorageAsync(EntityAnalysisModelInstanceEntryPayload payload,
            EntityAnalysisModel entityAnalysisModel,
            JsonSerializationHelper jsonSerializationHelper,
            ArchiveBuffer bulkInsertMessageBuffer,
            ConcurrentQueue<CreateCase> pendingCases,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            CacheService cacheService,
            ILog log,
            CancellationToken token = default)
        {
            try
            {
                var warnThresholdMicroseconds =
                    int.Parse(dynamicEnvironment.AppSettings("ArchiverWarnThresholdMilliseconds")) * 1000L;

                if (payload.ArchiveJson.Length == 0)
                {
                    var stopwatch = Stopwatch.StartNew();
                    payload.ArchiveJson =
                        BuildJsonResponses.BuildFullJson(payload, jsonSerializationHelper.ArchiveJsonSerializer);
                    var duration = (long)(stopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency));
                    entityAnalysisModel.ArchiverStagePerformanceCounters.Record(
                        nameof(ArchiverStage.BuildArchiveJson), duration);
                    EngineDiagnostics.ArchiverStageDuration.Record(duration / 1000.0,
                        new KeyValuePair<string, object>("stage", nameof(ArchiverStage.BuildArchiveJson)));
                    if (duration >= warnThresholdMicroseconds)
                    {
                        ArchiverWarningCapture.Enqueue(new ArchiverWarningCaptureRecord(
                            DateTime.UtcNow, entityAnalysisModel.Instance.Guid, entityAnalysisModel.Instance.Name,
                            payload.EntityAnalysisModelInstanceEntryGuid, ArchiverStage.BuildArchiveJson, duration));
                        EngineDiagnostics.ArchiverWarnCount.Add(1,
                            new KeyValuePair<string, object>("stage", nameof(ArchiverStage.BuildArchiveJson)));
                    }
                }

                var jsonString = Encoding.UTF8.GetString(payload.ArchiveJson);

                if (payload.CreateCase != null)
                {
                    var stopwatch = Stopwatch.StartNew();

                    if (payload.EntityAnalysisModelReprocessingRuleInstanceId.HasValue)
                    {
                        await CaseProcessing.CreateAsync(dynamicEnvironment, payload.CreateCase, log,
                            jsonSerializationHelper, payload, token);
                        // ReSharper disable once RedundantAssignment
                        payload.CreateCase = null;
                    }
                    else
                    {
                        payload.CreateCase.Json = jsonString;
                        pendingCases.Enqueue(payload.CreateCase);
                    }

                    var duration = (long)(stopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency));
                    entityAnalysisModel.ArchiverStagePerformanceCounters.Record(
                        nameof(ArchiverStage.CaseCreationDispatch), duration);
                    EngineDiagnostics.ArchiverStageDuration.Record(duration / 1000.0,
                        new KeyValuePair<string, object>("stage", nameof(ArchiverStage.CaseCreationDispatch)));
                    if (duration >= warnThresholdMicroseconds)
                    {
                        ArchiverWarningCapture.Enqueue(new ArchiverWarningCaptureRecord(
                            DateTime.UtcNow, entityAnalysisModel.Instance.Guid, entityAnalysisModel.Instance.Name,
                            payload.EntityAnalysisModelInstanceEntryGuid, ArchiverStage.CaseCreationDispatch,
                            duration));
                        EngineDiagnostics.ArchiverWarnCount.Add(1,
                            new KeyValuePair<string, object>("stage", nameof(ArchiverStage.CaseCreationDispatch)));
                    }
                }

                if (!payload.EnableRdbmsArchive)
                {
                    return;
                }

                var archiveStopwatch = Stopwatch.StartNew();

                if (payload.EntityAnalysisModelReprocessingRuleInstanceId.HasValue)
                {
                    await ArchiverArchiveRepository
                        .UpdateArchiveAsync(payload, jsonString, dynamicEnvironment, log, token)
                        .ConfigureAwait(false);
                }

                else if (bulkInsertMessageBuffer is null)
                {
                    log.Error("Database Persist: Not implemented bulkInsertMessageBuffer is null.");
                }
                else
                {
                    DataTableInsertToBuffer(bulkInsertMessageBuffer, payload, jsonString, log);
                }

                var archiveDuration = (long)(archiveStopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency));
                entityAnalysisModel.ArchiverStagePerformanceCounters.Record(
                    nameof(ArchiverStage.RdbmsArchiveWrite), archiveDuration);
                EngineDiagnostics.ArchiverStageDuration.Record(archiveDuration / 1000.0,
                    new KeyValuePair<string, object>("stage", nameof(ArchiverStage.RdbmsArchiveWrite)));
                if (archiveDuration >= warnThresholdMicroseconds)
                {
                    ArchiverWarningCapture.Enqueue(new ArchiverWarningCaptureRecord(
                        DateTime.UtcNow, entityAnalysisModel.Instance.Guid, entityAnalysisModel.Instance.Name,
                        payload.EntityAnalysisModelInstanceEntryGuid, ArchiverStage.RdbmsArchiveWrite,
                        archiveDuration));
                    EngineDiagnostics.ArchiverWarnCount.Add(1,
                        new KeyValuePair<string, object>("stage", nameof(ArchiverStage.RdbmsArchiveWrite)));
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.Error($"Database Persist: General exception in processing {ex}.");
            }
            finally
            {
                // ReSharper disable once RedundantAssignment
                payload = null;
            }
        }

        private static void DataTableInsertToBuffer(ArchiveBuffer bulkInsertMessageBuffer,
            EntityAnalysisModelInstanceEntryPayload payload, string jsonString, ILog log)
        {
            try
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Database Persist: Database Persist message is valid for storage with Entry GUID of {payload.EntityAnalysisModelInstanceEntryGuid}.  This is being sent for bulk insert. " +
                        $"Database Persist: The flag to promote report table has been set for this model,  will now check columns are available and add the record to the data table.");
                }

                var model = new Archive
                {
                    Json = jsonString,
                    EntityAnalysisModelInstanceEntryGuid = payload.EntityAnalysisModelInstanceEntryGuid,
                    ResponseElevation = payload.ResponseElevation.Value,
                    EntityAnalysisModelActivationRuleId = payload.PrevailingEntityAnalysisModelActivationRuleId,
                    EntityAnalysisModelId = payload.EntityAnalysisModelId,
                    ActivationRuleCount = payload.EntityAnalysisModelActivationRuleCount,
                    EntryKeyValue = payload.EntityInstanceEntryId,
                    ReferenceDate = payload.ReferenceDate,
                    CreatedDate = DateTime.UtcNow,
                    Version = 1
                };

                bulkInsertMessageBuffer.Archive.Add(model);

                foreach (var reportDatabaseValue in payload.ArchiveKeys)
                {
                    bulkInsertMessageBuffer.ArchiveKeys.Add(reportDatabaseValue);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.Error($"Database Persist: An error has occurred as {ex}");
            }
        }
    }
}