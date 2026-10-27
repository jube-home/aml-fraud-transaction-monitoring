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
using System.Threading;
using System.Threading.Tasks;
using Jube.Cache;
using Jube.Cache.Redis;
using Jube.Data.Poco;
using Newtonsoft.Json;

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.ActivationRules
{
    using EntityAnalysisModelActivationRule =
        EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelActivationRule;

    public static class ActivationRuleActivationWatcherExtensions
    {
        public static async Task ActivationRuleActivationWatcherAsync(this Context context,
            EntityAnalysisModelActivationRule evaluateActivationRule,
            bool suppressed, CacheService cacheService)
        {
            if (!evaluateActivationRule.SendToActivationWatcher || suppressed || context
                    .EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelReprocessingRuleInstanceId.HasValue ||
                !context.EntityAnalysisModel.Flags.EnableActivationWatcher)
            {
                return;
            }

            try
            {
                var activationWatcher = BuildActivationWatcher(context, evaluateActivationRule);
                var previous = await ExchangeLastLocationAsync(context, activationWatcher, cacheService);

                context.TraceLog(
                    $"the current activation watch count is {context.EntityAnalysisModel.Counters.ActivationWatcherCount} which will be tested against the threshold {context.EntityAnalysisModel.Counters.MaxActivationWatcherThreshold}.");

                if (!(context.EntityAnalysisModel.Counters.ActivationWatcherCount <
                      context.EntityAnalysisModel.Counters.MaxActivationWatcherThreshold) ||
                    !(context.EntityAnalysisModel.Counters.ActivationWatcherSample >= context.Random.NextDouble()))
                {
                    return;
                }

                context.TraceLog(
                    $"the current activation watch count is {context.EntityAnalysisModel.Counters.ActivationWatcherCount} which will be tested against the threshold {context.EntityAnalysisModel.Counters.MaxActivationWatcherThreshold} and selected via random sampling.");

                if (previous != null)
                {
                    activationWatcher.PreviousLatitude = previous.Latitude;
                    activationWatcher.PreviousLongitude = previous.Longitude;
                    activationWatcher.PreviousCreatedDate = previous.SeenAt;
                }

                var jsonString = JsonConvert.SerializeObject(activationWatcher,
                    context.EntityAnalysisModel.JsonSerializationHelper.DefaultJsonSerializerSettingsSettings);

                context.TraceLog($"has serialized the Activation Watcher Object to be dispatched.");

                if (context.Environment.AppSettings("ActivationWatcherAllowPersist")
                    .Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    context.EntityAnalysisModel.ConcurrentQueues.PersistToActivationWatcherAsync.Enqueue(
                        activationWatcher);

                    context.TraceLog(
                        $"replay is allowed so it has been sent to the database. {context.EntityAnalysisModel.Counters.ActivationWatcherCount}.");
                }
                else
                {
                    context.TraceLog(
                        $"replay is not allowed so it has not been sent to the database. {context.EntityAnalysisModel.Counters.ActivationWatcherCount}.");
                }

                if (context.Environment.AppSettings("StreamingActivationWatcher")
                    .Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    await cacheService.CacheActivationWatcherPublishSubscribe.PublishAsync(
                        context.EntityAnalysisModel.Instance.TenantRegistryId, jsonString);

                    Interlocked.Increment(ref context.EntityAnalysisModel.Counters.ActivationWatcherCount);

                    context.TraceLog(
                        $"streaming is allowed so it has been sent to the database as a notification in the activation channel. {context.EntityAnalysisModel.Counters.ActivationWatcherCount}.");
                }
                else
                {
                    context.TraceLog(
                        $"streaming is not allowed so it has not been sent to the database as a notification in the activation channel. {context.EntityAnalysisModel.Counters.ActivationWatcherCount}.");
                }

                context.TraceLog(
                    $"has sent a message to the watcher as {jsonString} the activation watcher counter has been incremented and is currently {context.EntityAnalysisModel.Counters.ActivationWatcherCount}.");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.TraceLog($"there has been an error in Activation Watcher processing {ex}.");
            }
        }

        private static async Task<LastLocation> ExchangeLastLocationAsync(Context context,
            ActivationWatcher activationWatcher, CacheService cacheService)
        {
            if (cacheService?.CacheActivationWatcherLastLocationRepository == null ||
                !HasLocation(activationWatcher) || !HasEntity(activationWatcher))
            {
                return null;
            }

            var previous = await cacheService.CacheActivationWatcherLastLocationRepository.ExchangeAsync(
                activationWatcher.TenantRegistryId.GetValueOrDefault(), activationWatcher.Key,
                activationWatcher.KeyValue, activationWatcher.Latitude.GetValueOrDefault(),
                activationWatcher.Longitude.GetValueOrDefault(),
                activationWatcher.CreatedDate.GetValueOrDefault(DateTime.UtcNow)).ConfigureAwait(false);

            if (previous == null)
            {
                context.TraceLog(
                    $"has recorded the first known location of {activationWatcher.Key}:{activationWatcher.KeyValue}.");
            }
            else
            {
                context.TraceLog(
                    $"has found a previous location of {previous.Latitude},{previous.Longitude} seen at {previous.SeenAt.ToString("O")} for {activationWatcher.Key}:{activationWatcher.KeyValue}.");
            }

            return previous;
        }

        private static bool HasLocation(ActivationWatcher activationWatcher)
        {
            return activationWatcher.Latitude.HasValue && activationWatcher.Longitude.HasValue &&
                   (activationWatcher.Latitude.Value != 0d || activationWatcher.Longitude.Value != 0d);
        }

        private static bool HasEntity(ActivationWatcher activationWatcher)
        {
            return !string.IsNullOrWhiteSpace(activationWatcher.Key) &&
                   !string.IsNullOrWhiteSpace(activationWatcher.KeyValue) &&
                   !activationWatcher.KeyValue.Equals("Missing", StringComparison.OrdinalIgnoreCase);
        }

        private static ActivationWatcher BuildActivationWatcher(Context context,
            EntityAnalysisModelActivationRule evaluateActivationRule)
        {
            var activationWatcher = new ActivationWatcher
            {
                BackColor = evaluateActivationRule.ResponseElevationBackColor,
                ForeColor = evaluateActivationRule.ResponseElevationForeColor,
                ResponseElevation = evaluateActivationRule.ResponseElevation,
                ResponseElevationContent = evaluateActivationRule.ResponseElevationContent,
                ResponseElevationName = context.Snapshot.EntityAnalysisModelResponseElevationRanges
                    .FirstOrDefault(f => f.Contains((int)evaluateActivationRule.ResponseElevation))?.Name,
                ActivationRuleSummary = evaluateActivationRule.Name,
                TenantRegistryId = context.EntityAnalysisModel.Instance.TenantRegistryId,
                CreatedDate = DateTime.UtcNow,
                Latitude = GetLatitude(context),
                Longitude = GetLongitude(context),
                Key = evaluateActivationRule.ResponseElevationKey,
                KeyValue = ""
            };

            if (context.EntityAnalysisModelInstanceEntryPayload.Payload.ContainsKey(evaluateActivationRule
                    .ResponseElevationKey))
            {
                activationWatcher.KeyValue =
                    context.EntityAnalysisModelInstanceEntryPayload.Payload[evaluateActivationRule
                        .ResponseElevationKey].AsString();

                context.TraceLog(
                    $"found key of {activationWatcher.Key} and key value of {activationWatcher.KeyValue}.");
            }
            else
            {
                activationWatcher.KeyValue = "Missing";

                context.TraceLog(
                    $"fallen back to key of {activationWatcher.Key} and key value of {activationWatcher.KeyValue}.");
            }

            return activationWatcher;
        }

        private static double GetLongitude(Context context)
        {
            var longitudeFieldName = context.Snapshot.EntityAnalysisModelRequestXPaths
                .FirstOrDefault(f => f.DataTypeId == 7)?.Name;

            if (string.IsNullOrEmpty(longitudeFieldName))
            {
                longitudeFieldName = context.Snapshot.EntityAnalysisModelInlineScripts
                    .SelectMany(entityAnalysisModelInlineScript => entityAnalysisModelInlineScript
                        .EntityAnalysisModelInlineScriptPropertyAttributes
                        .Where(entityAnalysisModelInlineScriptPropertyAttribute =>
                            entityAnalysisModelInlineScriptPropertyAttribute.Value.Longitude))
                    .Select(entityAnalysisModelInlineScriptPropertyAttribute =>
                        entityAnalysisModelInlineScriptPropertyAttribute.Key)
                    .FirstOrDefault();
            }

            if (string.IsNullOrEmpty(longitudeFieldName))
            {
                return 0d;
            }

            return context.EntityAnalysisModelInstanceEntryPayload.Payload.TryGetValue(longitudeFieldName,
                out var value)
                ? value
                : 0d;
        }

        private static double GetLatitude(Context context)
        {
            var latitudeFieldName = context.Snapshot.EntityAnalysisModelRequestXPaths
                .FirstOrDefault(f => f.DataTypeId == 6)?.Name;

            if (string.IsNullOrEmpty(latitudeFieldName))
            {
                latitudeFieldName = context.Snapshot.EntityAnalysisModelInlineScripts
                    .SelectMany(entityAnalysisModelInlineScript => entityAnalysisModelInlineScript
                        .EntityAnalysisModelInlineScriptPropertyAttributes
                        .Where(entityAnalysisModelInlineScriptPropertyAttribute =>
                            entityAnalysisModelInlineScriptPropertyAttribute.Value.Latitude))
                    .Select(entityAnalysisModelInlineScriptPropertyAttribute =>
                        entityAnalysisModelInlineScriptPropertyAttribute.Key)
                    .FirstOrDefault();
            }

            if (string.IsNullOrEmpty(latitudeFieldName))
            {
                return 0d;
            }

            return context.EntityAnalysisModelInstanceEntryPayload.Payload.TryGetValue(latitudeFieldName,
                out var value)
                ? value
                : 0d;
        }
    }
}