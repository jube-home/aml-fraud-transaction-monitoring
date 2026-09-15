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
using System.Threading.Tasks;
using Jube.Cache;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using Jube.Extensions;
using Jube.TaskCancellation.TaskHelper;

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.ActivationRules
{
    public static class ActivationRuleTtlCounterExtensions
    {
        public static async Task ActivationRuleTtlCounterAsync(this Context context,
            EntityAnalysisModelActivationRule evaluateActivationRule,
            Dictionary<int, EntityAnalysisModel> availableModels, CacheService cacheService)
        {
            if (!evaluateActivationRule.EnableTtlCounter || context.EntityAnalysisModelInstanceEntryPayload
                    .EntityAnalysisModelReprocessingRuleInstanceId.HasValue)
            {
                return;
            }

            context.TraceLog(
                $"is incrementing TTL counter {evaluateActivationRule.EntityAnalysisModelTtlCounterGuid} as this is enabled in the activation rule.");

            var target = FindTargetTtlCounter(evaluateActivationRule, availableModels);
            if (target == null)
            {
                return;
            }

            var (value, foundTtlCounter) = target.Value;

            try
            {
                context.TraceLog(
                    $"has matched the name in the activation rule to the TTL counters loaded for {context.EntityAnalysisModel.Instance.Name} in model id {value.Instance.Id}.");

                if (!context.EntityAnalysisModelInstanceEntryPayload.Payload.ContainsKey(foundTtlCounter
                        .TtlCounterDataName))
                {
                    context.TraceLog(
                        $"could not find a value for TTL counter name {foundTtlCounter.Name}.");

                    return;
                }

                context.TraceLog(
                    $"found a value for TTL counter name {foundTtlCounter.Name} as {context.EntityAnalysisModelInstanceEntryPayload.Payload[foundTtlCounter.TtlCounterDataName]}.");

                if (!value.Flags.EnableTtlCounter)
                {
                    context.TraceLog(
                        $"cannot create a TTL counter for name {value.Instance.Name} as TTL Counter Storage is disabled for the model id {value.Instance.Id}.");

                    return;
                }

                if (evaluateActivationRule.EntityAnalysisModelGuidTtlCounter != value.Instance.Guid)
                {
                    return;
                }

                if (context.Environment.AppSettings("ActivationRuleIdempotency")
                    .Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    if (!await cacheService.CacheTtlCounterIdempotencyRepository
                            .CheckAndClaimIdempotencyAsync(
                                context.EntityAnalysisModel.Instance.TenantRegistryId,
                                context.EntityAnalysisModel.Instance.Guid,
                                context.EntityAnalysisModelInstanceEntryPayload
                                    .EntityAnalysisModelInstanceEntryGuid,
                                foundTtlCounter.Guid))
                    {
                        context.TraceLog(
                            $"has built a TTL Counter insert payload of TTLCounterName as {foundTtlCounter.Name}, TTLCounterDataName as {foundTtlCounter.TtlCounterDataName} and TTLCounterDataNameValue as {context.EntityAnalysisModelInstanceEntryPayload.Payload[foundTtlCounter.TtlCounterDataName]} but failed idempotency check.");

                        return;
                    }
                }
                else
                {
                    context.TraceLog(
                        $"has built a TTL Counter insert payload of TTLCounterName as {foundTtlCounter.Name}, TTLCounterDataName as {foundTtlCounter.TtlCounterDataName} and TTLCounterDataNameValue as {context.EntityAnalysisModelInstanceEntryPayload.Payload[foundTtlCounter.TtlCounterDataName]} won't check ActivationRuleIdempotency.");
                }

                context.TraceLog(
                    $"has built a TTL Counter insert payload of TTLCounterName as {foundTtlCounter.Name}, TTLCounterDataName as {foundTtlCounter.TtlCounterDataName} and TTLCounterDataNameValue as {context.EntityAnalysisModelInstanceEntryPayload.Payload[foundTtlCounter.TtlCounterDataName]}. Is about to insert the entry.");

                var incrementValue = ResolveIncrementValue(foundTtlCounter,
                    context.EntityAnalysisModelInstanceEntryPayload.Payload);
                if (foundTtlCounter.EnableSum)
                {
                    context.TraceLog(
                        $"has built a TTL Counter insert payload of TTLCounterName as {foundTtlCounter.Name}, TTLCounterDataName as {foundTtlCounter.TtlCounterDataName} and TTLCounterDataNameValue as {context.EntityAnalysisModelInstanceEntryPayload.Payload[foundTtlCounter.TtlCounterDataName]}. Has incremented based on sum for value {foundTtlCounter.TtlCounterDataValue} with an increment value of {incrementValue}.");
                }

                if (!foundTtlCounter.EnableLiveForever)
                {
                    var resolution = ResolveResolution(foundTtlCounter,
                        context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate);

                    context.TraceLog(
                        $"has built a TTL Counter insert payload of TTLCounterName as {foundTtlCounter.Name}, TTLCounterDataName as {foundTtlCounter.TtlCounterDataName} and TTLCounterDataNameValue as {context.EntityAnalysisModelInstanceEntryPayload.Payload[foundTtlCounter.TtlCounterDataName]}. Is about to insert the entry with a resolution of {resolution}.");

                    context.PendingWriteTasks.Add(
                        TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(
                            TaskType.CacheTtlCounterEntryUpsertAsync, async () =>
                                await cacheService.CacheTtlCounterEntryRepository.UpsertAsync(
                                    context.EntityAnalysisModel.Instance.TenantRegistryId,
                                    evaluateActivationRule.EntityAnalysisModelGuidTtlCounter,
                                    foundTtlCounter.TtlCounterDataName,
                                    context.EntityAnalysisModelInstanceEntryPayload
                                        .Payload[foundTtlCounter.TtlCounterDataName]
                                        .AsString(),
                                    foundTtlCounter.Guid,
                                    resolution, incrementValue).ConfigureAwait(false),
                            context.Log));
                }
                else
                {
                    context.TraceLog(
                        $"has built a TTL Counter insert payload of TTLCounterName as {foundTtlCounter.Name}, TTLCounterDataName as {foundTtlCounter.TtlCounterDataName} and TTLCounterDataNameValue as {context.EntityAnalysisModelInstanceEntryPayload.Payload[foundTtlCounter.TtlCounterDataName]} is set to live forever so no entry has been made to wind back counters.");
                }

                if (!context.EntityAnalysisModelInstanceEntryPayload.Payload.TryGetValue(
                        foundTtlCounter.TtlCounterDataName, out var payloadValue))
                {
                    return;
                }

                context.PendingWriteTasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(
                    TaskType.CacheTtlCounterEntryIncrementAsync, async () => await cacheService
                        .CacheTtlCounterRepository
                        .IncrementTtlCounterCacheAsync(
                            context.EntityAnalysisModel.Instance.TenantRegistryId,
                            context.EntityAnalysisModel.Instance.Guid,
                            foundTtlCounter.TtlCounterDataName,
                            payloadValue.AsString(),
                            foundTtlCounter.Guid, incrementValue,
                            context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate
                        ).ConfigureAwait(false), context.Log));

                if (context.EntityAnalysisModelInstanceEntryPayload.TtlCounter.TryGetValue(
                        foundTtlCounter.Name, out var ttlCounterValue))
                {
                    ttlCounterValue += incrementValue;
                    context.EntityAnalysisModelInstanceEntryPayload.TtlCounter[
                        foundTtlCounter.Name] = ttlCounterValue;
                }
                else
                {
                    context.EntityAnalysisModelInstanceEntryPayload.TtlCounter.Add(
                        foundTtlCounter.Name, incrementValue);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.TraceLog(
                    $"error performing insertion on match for a TTL Counter by name of {foundTtlCounter.Name} and id of {foundTtlCounter.Id} with exception message of {ex.Message}.");
            }
            finally
            {
                context.TraceLog(
                    $"has matched the name in the activation rule to the TTL counters loaded for {context.EntityAnalysisModel.Instance.Name} and has finished processing.");
            }
        }

        private static (EntityAnalysisModel Model, EntityAnalysisModelTtlCounter TtlCounter)?
            FindTargetTtlCounter(EntityAnalysisModelActivationRule evaluateActivationRule,
                Dictionary<int, EntityAnalysisModel> availableModels)
        {
            foreach (var (_, model) in availableModels)
            {
                if (evaluateActivationRule.EntityAnalysisModelGuidTtlCounter != model.Instance.Guid)
                {
                    continue;
                }

                foreach (var candidate in model.Collections.ModelTtlCounters)
                {
                    if (evaluateActivationRule.EntityAnalysisModelTtlCounterGuid == candidate.Guid)
                    {
                        return (model, candidate);
                    }
                }
            }

            return null;
        }

        private static double ResolveIncrementValue(EntityAnalysisModelTtlCounter foundTtlCounter,
            DictionaryNoBoxing<string> payload)
        {
            return foundTtlCounter.EnableSum ? payload[foundTtlCounter.TtlCounterDataValue] : 1d;
        }

        private static DateTime ResolveResolution(EntityAnalysisModelTtlCounter foundTtlCounter,
            DateTime referenceDate)
        {
            return foundTtlCounter.ResolutionInterval switch
            {
                "n" => referenceDate.Floor(TimeSpan.FromMinutes(1)),
                "h" => referenceDate.Floor(TimeSpan.FromHours(1)),
                "d" => referenceDate.Floor(TimeSpan.FromDays(1)),
                _ => referenceDate.Floor(TimeSpan.FromMinutes(1))
            };
        }
    }
}