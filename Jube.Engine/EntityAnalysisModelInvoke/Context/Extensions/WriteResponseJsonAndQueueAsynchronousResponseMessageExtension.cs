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
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Jube.Engine.Observability;
using RabbitMQ.Client;

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions
{
    public static class WriteResponseJsonAndQueueAsynchronousResponseMessageExtension
    {
        public static async Task WriteResponseJsonAndQueueAsynchronousResponseMessageAsync(this Context context)
        {
            var stopwatch = Stopwatch.StartNew();

            context.EntityAnalysisModelInstanceEntryPayload.ArchiveJson = BuildJsonResponses.BuildFullJson(
                context.EntityAnalysisModelInstanceEntryPayload,
                context.EntityAnalysisModel.JsonSerializationHelper.ArchiveJsonSerializer);

            if (!context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelReprocessingRuleInstanceId
                    .HasValue)
            {
                await context.EntityAnalysisModel.Services.CacheService.CacheWalRepository.InsertAsync(
                    context.EntityAnalysisModelInstanceEntryPayload.TenantRegistryId,
                    context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelGuid,
                    context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid,
                    Dns.GetHostName(), context.EntityAnalysisModelInstanceEntryPayload.ArchiveJson);
            }

            if (context.Environment.AppSettings("PartialResponseMessageSerialisation")
                .Equals("True", StringComparison.CurrentCultureIgnoreCase))
            {
                context.TraceLog(
                    $"has partial response serialisation enabled and will serialise a partial response.");

                context.EntityAnalysisModelInstanceEntryPayload.ResponseJson =
                    BuildJsonResponses.BuildPartialResponsePayloadJson(context,
                        context.EntityAnalysisModel.JsonSerializationHelper.ArchiveJsonSerializer);
            }
            else
            {
                context.TraceLog(
                    $"has partial response serialisation disabled and will serialise a full response.");

                context.EntityAnalysisModelInstanceEntryPayload.ResponseJson =
                    BuildFullResponseJsonRedactingStagesUnlessTraceEnabled(context);
            }

            if (context.Environment.AppSettings("AMQP").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                PublishToAmqp(context);
            }
            else
            {
                context.TraceLog($"does not have AMQP configured to dispatch messages to an exchange.");
            }

            if (context.Async)
            {
                await PublishCallbackViaPostgresAsync(context).ConfigureAwait(false);
            }

            stopwatch.Stop();

            if (context.LogSampled)
            {
                var stages = context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages ??=
                    new InvokeStagePerformance();
                stages.WriteResponse = new StageDuration
                {
                    DurationMicroseconds = (long)(stopwatch.ElapsedTicks * (1_000_000.0 / Stopwatch.Frequency))
                };
            }
        }

        private static byte[] BuildFullResponseJsonRedactingStagesUnlessTraceEnabled(Context context)
        {
            var payload = context.EntityAnalysisModelInstanceEntryPayload;

            if (context.EntityAnalysisModel.Flags.EnableTrace && context.EntityAnalysisModel.Flags.EnableLogsInResponse)
            {
                return BuildJsonResponses.BuildFullJson(payload,
                    context.EntityAnalysisModel.JsonSerializationHelper.ArchiveJsonSerializer);
            }

            var stagesForArchive = payload.InvokeTaskPerformance.Stages;
            var logsForArchive = payload.Logs;
            try
            {
                if (!context.EntityAnalysisModel.Flags.EnableTrace)
                {
                    payload.InvokeTaskPerformance.Stages = null;
                }

                if (!context.EntityAnalysisModel.Flags.EnableLogsInResponse)
                {
                    payload.Logs = null;
                }

                return BuildJsonResponses.BuildFullJson(payload,
                    context.EntityAnalysisModel.JsonSerializationHelper.ArchiveJsonSerializer);
            }
            finally
            {
                payload.InvokeTaskPerformance.Stages = stagesForArchive;
                payload.Logs = logsForArchive;
            }
        }

        private static async Task PublishCallbackViaPostgresAsync(Context context)
        {
            await context.EntityAnalysisModel.Services.CacheService.CacheCallbackPublishSubscribe.PublishAsync(
                    context.EntityAnalysisModelInstanceEntryPayload.ResponseJson.Length > 0
                        ? context.EntityAnalysisModelInstanceEntryPayload.ResponseJson
                        : context.EntityAnalysisModelInstanceEntryPayload.ArchiveJson,
                    context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid,
                    tenantRegistryId: context.EntityAnalysisModel.Instance.TenantRegistryId)
                .ConfigureAwait(false);

            context.TraceLog($"will store the callback in the database.");
        }

        private static void PublishToAmqp(Context context)
        {
            context.TraceLog($"is about to publish the response to the Outbound Exchange.");

            using var activity = EngineDiagnostics.ActivitySource.StartActivity("PublishToAmqp",
                ActivityKind.Producer);
            EngineDiagnostics.TagCurrentCodeLocation(activity);
            activity?.SetTag("messaging.system", "rabbitmq");
            activity?.SetTag("messaging.destination.name", "jubeOutbound");
            activity?.SetTag("messaging.operation", "publish");
            activity?.SetTag("jube.entity_analysis_model_instance_entry_guid",
                context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid);

            var props = context.EntityAnalysisModel.Services.RabbitMqChannel.CreateBasicProperties();
            props.Headers = new PooledDictionary<string, object>();

            var body = context.EntityAnalysisModelInstanceEntryPayload.ResponseJson.Length > 0
                ? context.EntityAnalysisModelInstanceEntryPayload.ResponseJson
                : context.EntityAnalysisModelInstanceEntryPayload.ArchiveJson;
            activity?.SetTag("messaging.message.body.size", body.Length);

            context.EntityAnalysisModel.Services.RabbitMqChannel.BasicPublish("jubeOutbound", "", props, body);

            context.TraceLog($"has published the response to the Outbound Exchange.");
        }
    }
}