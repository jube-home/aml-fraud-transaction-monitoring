using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Jube.Case;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Query;
using Jube.Data.Repository;
using Jube.Engine.EntityAnalysisModelInvoke.Models.CaseManagement;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.Extensions;
using Jube.Engine.Helpers;
using Jube.Engine.Observability;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jube.Engine.BackgroundTasks.TaskStarters.Case
{
    public static class CaseProcessing
    {
        public static async Task CreateAsync(DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            CreateCase createCase,
            ILog log,
            JsonSerializationHelper jsonSerializationHelper,
            EntityAnalysisModelInstanceEntryPayload payload = null,
            CancellationToken token = default)
        {
            var dbContext =
                DataConnectionDbContext.GetResilientDbContextDataConnection(
                    dynamicEnvironment.AppSettings("ConnectionString"), log);

            try
            {
                var warnThresholdMicroseconds =
                    int.Parse(dynamicEnvironment.AppSettings("CaseCreationWarnThresholdMilliseconds")) * 1000L;

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Case Creation: has received a case creation message with case entry GUID of {createCase.EntityAnalysisModelInstanceEntryGuid}.");
                }

                var repositoryCase = new CaseRepository(dbContext);
                var query = new GetExistingCasePriorityQuery(dbContext);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Case Creation: connection to the database established for case entry GUID of {createCase.EntityAnalysisModelInstanceEntryGuid}.");
                }

                var model = new Data.Poco.Case
                {
                    EntityAnalysisModelInstanceEntryGuid = createCase.EntityAnalysisModelInstanceEntryGuid,
                    CaseWorkflowGuid = createCase.CaseWorkflowGuid,
                    CaseWorkflowStatusGuid = createCase.CaseWorkflowStatusGuid,
                    CaseKey = createCase.CaseKey,
                    CaseKeyValue = createCase.CaseKeyValue,
                    Locked = 0,
                    Rating = 0,
                    CreatedDate = DateTime.UtcNow
                };

                if (createCase.SuspendBypass)
                {
                    model.Diary = 1;
                    model.DiaryDate = createCase.SuspendBypassDate;
                    model.ClosedStatusId = 4;
                }
                else
                {
                    model.Diary = 0;
                    model.DiaryDate = createCase.SuspendBypassDate;
                    model.ClosedStatusId = 0;
                    model.DiaryDate = DateTime.UtcNow;
                }

                model.Json = createCase.Json;

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Case Creation: Have created a case creation SQL command with Case Entry GUID of {createCase.EntityAnalysisModelInstanceEntryGuid}, " +
                        $"Case Workflow ID of {createCase.CaseWorkflowGuid}, " +
                        $"Case Workflow Status ID of {createCase.CaseWorkflowStatusGuid}, Case Key of {createCase.CaseKeyValue}, " +
                        $"Case XML Bytes {createCase.Json.Length}");
                }

                var existingCasePriorityLookupStopwatch = Stopwatch.StartNew();
                var existing = await query
                    .ExecuteAsync(model.CaseWorkflowGuid, model.CaseKey, model.CaseKeyValue, token)
                    .ConfigureAwait(false);
                var existingCasePriorityLookupDuration =
                    (long)(existingCasePriorityLookupStopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency));
                CaseCreationStagePerformanceCounters.Instance.Record(
                    nameof(CaseCreationStage.ExistingCasePriorityLookup), existingCasePriorityLookupDuration);
                EngineDiagnostics.CaseCreationStageDuration.Record(existingCasePriorityLookupDuration / 1000.0,
                    new KeyValuePair<string, object>("stage", nameof(CaseCreationStage.ExistingCasePriorityLookup)));
                if (existingCasePriorityLookupDuration >= warnThresholdMicroseconds)
                {
                    CaseCreationWarningCapture.Enqueue(new CaseCreationWarningCaptureRecord(
                        DateTime.UtcNow, createCase.TenantRegistryId, createCase.EntityAnalysisModelInstanceEntryGuid,
                        createCase.CaseWorkflowGuid, createCase.CaseKey, createCase.CaseKeyValue,
                        CaseCreationStage.ExistingCasePriorityLookup, null, existingCasePriorityLookupDuration));
                    EngineDiagnostics.CaseCreationWarnCount.Add(1,
                        new KeyValuePair<string, object>("stage",
                            nameof(CaseCreationStage.ExistingCasePriorityLookup)));
                }

                var repositoryCasesWorkflowsStatus =
                    new CaseWorkflowStatusRepository(dbContext, createCase.TenantRegistryId);

                var workflowStatusLookupAndPersistStopwatch = Stopwatch.StartNew();
                CaseWorkflowStatus finalCasesWorkflowsStatus = null;
                if (existing == null)
                {
                    finalCasesWorkflowsStatus =
                        await repositoryCasesWorkflowsStatus.GetByGuidAsync(model.CaseWorkflowStatusGuid, token)
                            .ConfigureAwait(false);

                    await repositoryCase.InsertAsync(model, token).ConfigureAwait(false);
                }
                else
                {
                    var existingCasesWorkflowsStatus =
                        await repositoryCasesWorkflowsStatus.GetByGuidAsync(model.CaseWorkflowStatusGuid, token)
                            .ConfigureAwait(false);

                    if (existingCasesWorkflowsStatus.Priority < existing.Priority)
                    {
                        finalCasesWorkflowsStatus =
                            await repositoryCasesWorkflowsStatus.GetByGuidAsync(model.CaseWorkflowStatusGuid, token)
                                .ConfigureAwait(false);

                        model.Id = existing.CaseId;
                        model.Locked = 0;
                        model.CaseWorkflowStatusGuid = createCase.CaseWorkflowStatusGuid;
                        await repositoryCase.UpdateCaseAsync(model, token).ConfigureAwait(false);
                    }
                }

                var workflowStatusLookupAndPersistDuration =
                    (long)(workflowStatusLookupAndPersistStopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency));
                CaseCreationStagePerformanceCounters.Instance.Record(
                    nameof(CaseCreationStage.WorkflowStatusLookupAndPersist), workflowStatusLookupAndPersistDuration);
                EngineDiagnostics.CaseCreationStageDuration.Record(
                    workflowStatusLookupAndPersistDuration / 1000.0,
                    new KeyValuePair<string, object>("stage",
                        nameof(CaseCreationStage.WorkflowStatusLookupAndPersist)));
                if (workflowStatusLookupAndPersistDuration >= warnThresholdMicroseconds)
                {
                    CaseCreationWarningCapture.Enqueue(new CaseCreationWarningCaptureRecord(
                        DateTime.UtcNow, createCase.TenantRegistryId, createCase.EntityAnalysisModelInstanceEntryGuid,
                        createCase.CaseWorkflowGuid, createCase.CaseKey, createCase.CaseKeyValue,
                        CaseCreationStage.WorkflowStatusLookupAndPersist, null,
                        workflowStatusLookupAndPersistDuration));
                    EngineDiagnostics.CaseCreationWarnCount.Add(1,
                        new KeyValuePair<string, object>("stage",
                            nameof(CaseCreationStage.WorkflowStatusLookupAndPersist)));
                }

                var caseBytes = 0;
                if (createCase.Json != null)
                {
                    if (finalCasesWorkflowsStatus != null)
                    {
                        caseBytes = createCase.Json.Length;

                        if (finalCasesWorkflowsStatus.EnableNotification == 1 ||
                            finalCasesWorkflowsStatus.EnableHttpEndpoint == 1)
                        {
                            var context = payload ??
                                          JsonConvert.DeserializeObject<EntityAnalysisModelInstanceEntryPayload>(
                                              createCase.Json,
                                              jsonSerializationHelper.DefaultJsonSerializerSettingsSettings);

                            if (finalCasesWorkflowsStatus.EnableNotification == 1)
                            {
                                var notificationStopwatch = Stopwatch.StartNew();

                                var notificationSubject =
                                    context.ReplaceTokens(finalCasesWorkflowsStatus.NotificationSubject);
                                var notificationDestination =
                                    context.ReplaceTokens(finalCasesWorkflowsStatus.NotificationDestination);
                                var notificationBody =
                                    context.ReplaceTokens(finalCasesWorkflowsStatus.NotificationBody);

                                var notification = new Notification(log, dynamicEnvironment);
                                await notification.SendAsync(finalCasesWorkflowsStatus.NotificationTypeId ?? 1,
                                    notificationDestination,
                                    notificationSubject,
                                    notificationBody, token);

                                var notificationDuration =
                                    (long)(notificationStopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency));
                                CaseCreationStagePerformanceCounters.Instance.Record(
                                    nameof(CaseCreationStage.Notification), notificationDuration);
                                EngineDiagnostics.CaseCreationStageDuration.Record(notificationDuration / 1000.0,
                                    new KeyValuePair<string, object>("stage",
                                        nameof(CaseCreationStage.Notification)));
                                if (notificationDuration >= warnThresholdMicroseconds)
                                {
                                    CaseCreationWarningCapture.Enqueue(new CaseCreationWarningCaptureRecord(
                                        DateTime.UtcNow, createCase.TenantRegistryId,
                                        createCase.EntityAnalysisModelInstanceEntryGuid, createCase.CaseWorkflowGuid,
                                        createCase.CaseKey, createCase.CaseKeyValue, CaseCreationStage.Notification,
                                        notificationDestination, notificationDuration));
                                    EngineDiagnostics.CaseCreationWarnCount.Add(1,
                                        new KeyValuePair<string, object>("stage",
                                            nameof(CaseCreationStage.Notification)));
                                }
                            }

                            if (finalCasesWorkflowsStatus.EnableHttpEndpoint == 1)
                            {
                                var httpEndpointStopwatch = Stopwatch.StartNew();

                                var endpoint = context.ReplaceTokens(finalCasesWorkflowsStatus.HttpEndpoint);
                                if (finalCasesWorkflowsStatus.HttpEndpointTypeId == 1)
                                {
                                    await SendHttpEndpoint.PostAsync(endpoint,
                                        PreparePostBodyString(model, jsonSerializationHelper.ArchiveJsonSerializer,
                                            finalCasesWorkflowsStatus), log);
                                }
                                else
                                {
                                    await SendHttpEndpoint.GetAsync(endpoint, log);
                                }

                                var httpEndpointDuration =
                                    (long)(httpEndpointStopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency));
                                CaseCreationStagePerformanceCounters.Instance.Record(
                                    nameof(CaseCreationStage.HttpEndpoint), httpEndpointDuration);
                                EngineDiagnostics.CaseCreationStageDuration.Record(httpEndpointDuration / 1000.0,
                                    new KeyValuePair<string, object>("stage",
                                        nameof(CaseCreationStage.HttpEndpoint)));
                                if (httpEndpointDuration >= warnThresholdMicroseconds)
                                {
                                    CaseCreationWarningCapture.Enqueue(new CaseCreationWarningCaptureRecord(
                                        DateTime.UtcNow, createCase.TenantRegistryId,
                                        createCase.EntityAnalysisModelInstanceEntryGuid, createCase.CaseWorkflowGuid,
                                        createCase.CaseKey, createCase.CaseKeyValue, CaseCreationStage.HttpEndpoint,
                                        endpoint, httpEndpointDuration));
                                    EngineDiagnostics.CaseCreationWarnCount.Add(1,
                                        new KeyValuePair<string, object>("stage",
                                            nameof(CaseCreationStage.HttpEndpoint)));
                                }
                            }
                        }
                    }
                }

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Case Creation: Executed Case Entry GUID of {createCase.EntityAnalysisModelInstanceEntryGuid}, " +
                        $"Case Workflow ID of {createCase.EntityAnalysisModelInstanceEntryGuid}, " +
                        $"Case Workflow Status ID of {createCase.CaseWorkflowStatusGuid}, " +
                        $"Case Key of {createCase.CaseKeyValue}, " +
                        $"Case JSON Bytes {caseBytes}");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.Error($"Case Creation: error processing payload as {ex}.");
            }
            finally
            {
                await dbContext.CloseAsync(token).ConfigureAwait(false);
                await dbContext.DisposeAsync(token).ConfigureAwait(false);

                if (log.IsInfoEnabled)
                {
                    log.Info("Case Creation: closed the database connection.");
                }
            }
        }

        private static string PreparePostBodyString(Data.Poco.Case createCase, JsonSerializer jsonSerializer,
            CaseWorkflowStatus finalCasesWorkflowsStatus, EntityAnalysisModelInstanceEntryPayload payload = null)
        {
            var jObject = JObject.FromObject(createCase, jsonSerializer);

            if (payload == null)
            {
                jObject["context"] = JObject.Parse(createCase.Json);
            }
            else
            {
                jObject["context"] = JObject.FromObject(payload);
            }

            jObject["caseWorkflowStatus"] = finalCasesWorkflowsStatus.Name;
            jObject.Remove("json");
            return jObject.ToString();
        }
    }
}