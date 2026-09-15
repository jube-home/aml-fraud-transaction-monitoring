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
using System.Threading;
using Jube.Cryptography;
using log4net;

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Utilities
{
    using PocoEntityAnalysisModel = Data.Poco.EntityAnalysisModel;

    public static class EntityAnalysisModelRecordMapper
    {
        public static EntityAnalysisModel CreateEntityAnalysisModel(Context context, PocoEntityAnalysisModel record)
        {
            var entityAnalysisModel = new EntityAnalysisModel
            {
                JsonSerializationHelper = context.JsonSerializationHelper,
                Instance =
                {
                    Id = record.Id,
                    EntityAnalysisInstanceGuid =
                        context.EntityAnalysisModels.EntityAnalysisInstanceGuid
                },
                Services =
                {
                    Log = context.Services.Log,
                    RabbitMqChannel = context.Services.RabbitMqChannel,
                    JubeEnvironment = context.Services.DynamicEnvironment,
                    CacheService = context.Services.CacheService,
                    ImplicitAsyncInvocationTracker =
                        context.Services.ImplicitAsyncInvocationTracker,
                    AesEncryption = new AesEncryption(
                        context.Services.DynamicEnvironment.AppSettings(
                            "ElementSymmetricEncryptionKey"), IvMode.Deterministic)
                },
                ConcurrentQueues =
                {
                    PersistToActivationWatcherAsync =
                        context.ConcurrentQueues.PersistToActivationWatcherAsync,
                    PendingTagging = context.ConcurrentQueues.PendingTagging,
                    PendingNotifications = context.ConcurrentQueues.PendingNotifications,
                    Callbacks = context.ConcurrentQueues.Callbacks,
                    PendingEntityInvoke = context.ConcurrentQueues.PendingEntityInvoke
                },
                Dependencies =
                {
                    ActiveEntityAnalysisModels =
                        context.EntityAnalysisModels.ActiveEntityAnalysisModels,
                    SanctionsEntries = context.EntityAnalysisModels.SanctionsEntries,
                    SanctionsStopTokens = context.EntityAnalysisModels.SanctionsStopTokens,
                    EntityAnalysisModelLists =
                        context.EntityAnalysisModels.EntityAnalysisModelLists,
                    KvpDictionaries = context.EntityAnalysisModels.KvpDictionaries,
                    EntityAnalysisModelSuppressionModels = context.EntityAnalysisModels
                        .EntityAnalysisModelSuppressionModels,
                    EntityAnalysisModelSuppressionRules = context.EntityAnalysisModels
                        .EntityAnalysisModelSuppressionRules
                }
            };

            DataTableBuffersUtility.CreateIfNotExists(context.Services.Log, entityAnalysisModel,
                context.Services.DynamicEnvironment);

            return entityAnalysisModel;
        }

        // ReSharper disable once CyclomaticComplexity
        public static void MapRecordFields(PocoEntityAnalysisModel record, EntityAnalysisModel entityAnalysisModel,
            ILog log)
        {
            if (record.Name == null)
            {
                entityAnalysisModel.Instance.Name = "";

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} has been set with DEFAULT Name value of {entityAnalysisModel.Instance.Name}.");
                }
            }
            else
            {
                entityAnalysisModel.Instance.Name = record.Name.Replace(" ", "_");

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} has been set with Name value of {entityAnalysisModel.Instance.Name}.");
                }
            }

            if (record.EntryXPath == null)
            {
                entityAnalysisModel.References.EntryXPath = "";

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} has been set with DEFAULT Entry XPath value of {entityAnalysisModel.References.EntryXPath}.");
                }
            }
            else
            {
                entityAnalysisModel.References.EntryXPath = record.EntryXPath;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} has been set with Entry XPath value of {entityAnalysisModel.References.EntryXPath}.");
                }
            }

            if (record.ReferenceDateXPath == null)
            {
                entityAnalysisModel.References.ReferenceDateXpath = "";

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} has been set with DEFAULT Reference Date XPath value of {entityAnalysisModel.References.ReferenceDateXpath}.");
                }
            }
            else
            {
                entityAnalysisModel.References.ReferenceDateXpath = record.ReferenceDateXPath;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} has been set with Reference Date XPath value of {entityAnalysisModel.References.ReferenceDateXpath}.");
                }
            }

            if (record.EntryName == null)
            {
                entityAnalysisModel.References.EntryName = "";
                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} has been set with DEFAULT Entry Name value of {entityAnalysisModel.References.EntryName}.");
                }
            }
            else
            {
                entityAnalysisModel.References.EntryName = record.EntryName;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} has been set with Entry Name value of {entityAnalysisModel.References.EntryName}.");
                }
            }

            if (record.ReferenceDateName == null)
            {
                entityAnalysisModel.References.ReferenceDateName = "";

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} has been set with DEFAULT Reference Data Name value of {entityAnalysisModel.References.ReferenceDateName}.");
                }
            }
            else
            {
                entityAnalysisModel.References.ReferenceDateName = record.ReferenceDateName;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} has been set with Reference Date Name value of {entityAnalysisModel.References.ReferenceDateName}.");
                }
            }

            if (record.ReferenceDatePayloadLocationTypeId.HasValue)
            {
                entityAnalysisModel.References.ReferenceDatePayloadLocationTypeId =
                    record.ReferenceDatePayloadLocationTypeId.Value;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} has been set with Reference Date Payload Location of {entityAnalysisModel.References.ReferenceDatePayloadLocationTypeId}.");
                }
            }
            else
            {
                entityAnalysisModel.References.ReferenceDatePayloadLocationTypeId = 1;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} has been set with DEFAULT Reference Date Payload Location of {entityAnalysisModel.References.ReferenceDatePayloadLocationTypeId}.");
                }
            }

            if (record.EnableCache.HasValue)
            {
                entityAnalysisModel.Flags.EnableCache = record.EnableCache == 1;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} has been set with Allow Entity Cache of {entityAnalysisModel.Flags.EnableCache}.");
                }
            }
            else
            {
                entityAnalysisModel.Flags.EnableCache = false;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} has been set with DEFAULT Allow Entity Cache of {entityAnalysisModel.Flags.EnableCache}.");
                }
            }

            if (record.EnableActivationWatcher.HasValue)
            {
                entityAnalysisModel.Flags.EnableActivationWatcher = record.EnableActivationWatcher == 1;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} has been set with Enable Activation Watcher of {entityAnalysisModel.Flags.EnableActivationWatcher}.");
                }
            }
            else
            {
                entityAnalysisModel.Flags.EnableActivationWatcher = false;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} has been set with DEFAULT Enable Activation Watcher of {entityAnalysisModel.Flags.EnableActivationWatcher}.");
                }
            }

            if (record.EnableResponseElevationLimit.HasValue)
            {
                entityAnalysisModel.Flags.EnableResponseElevationLimit =
                    record.EnableResponseElevationLimit == 1;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} has been set with Enable Response Elevation Limit of {entityAnalysisModel.Flags.EnableResponseElevationLimit}.");
                }
            }
            else
            {
                entityAnalysisModel.Flags.EnableResponseElevationLimit = false;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} has been set with DEFAULT Enable Response Elevation Limit of {entityAnalysisModel.Flags.EnableResponseElevationLimit}.");
                }
            }

            if (record.EnableImplicitAsync.HasValue)
            {
                entityAnalysisModel.Flags.EnableImplicitAsync = record.EnableImplicitAsync == 1;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} has been set with Enable Implicit Async of {entityAnalysisModel.Flags.EnableImplicitAsync}.");
                }
            }
            else
            {
                entityAnalysisModel.Flags.EnableImplicitAsync = false;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} has been set with DEFAULT Enable Implicit Async of {entityAnalysisModel.Flags.EnableImplicitAsync}.");
                }
            }

            entityAnalysisModel.Flags.ImplicitAsyncTimeoutMilliseconds =
                record.ImplicitAsyncTimeoutMilliseconds;

            entityAnalysisModel.Flags.EnableTrace = record.EnableTrace == 1;

            entityAnalysisModel.Flags.EnableLogs = record.EnableLogs == 1;
            entityAnalysisModel.Flags.EnableLogsInfo = record.EnableLogsInfo == 1;
            entityAnalysisModel.Flags.EnableLogsWarnThreshold = record.EnableLogsWarnThreshold == 1;
            entityAnalysisModel.Flags.LogsWarnThresholdMilliseconds =
                record.LogsWarnThresholdMilliseconds;
            entityAnalysisModel.Flags.EnableLogsInResponse = record.EnableLogsInResponse == 1;
            entityAnalysisModel.Flags.EnableSampling = record.EnableSampling == 1;
            entityAnalysisModel.Flags.SamplePercentage = record.SamplePercentage;

            if (record.EnableTtlCounter.HasValue)
            {
                entityAnalysisModel.Flags.EnableTtlCounter = record.EnableTtlCounter == 1;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} has been set with Enable Ttl Counter cache of {entityAnalysisModel.Flags.EnableTtlCounter}.");
                }
            }
            else
            {
                entityAnalysisModel.Flags.EnableTtlCounter = false;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} has been set with DEFAULT Enable Ttl Counter cache of {entityAnalysisModel.Flags.EnableTtlCounter}.");
                }
            }

            if (record.EnableSanctionCache.HasValue)
            {
                entityAnalysisModel.Flags.EnableSanctionCache = record.EnableSanctionCache == 1;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} has been set with Enable Sanction Cache cache of {entityAnalysisModel.Flags.EnableSanctionCache}.");
                }
            }
            else
            {
                entityAnalysisModel.Flags.EnableSanctionCache = false;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} has been set with DEFAULT Enable Sanction Cache of {entityAnalysisModel.Flags.EnableSanctionCache}.");
                }
            }

            if (record.CacheFetchLimit.HasValue)
            {
                entityAnalysisModel.Cache.CacheTtlLimit = record.CacheFetchLimit.Value;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} has been set with Case TTL Limit of {entityAnalysisModel.Cache.CacheTtlLimit}.");
                }
            }
            else
            {
                entityAnalysisModel.Cache.CacheTtlLimit = 100;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} has been set with DEFAULT Case TTL Limit of {entityAnalysisModel.Cache.CacheTtlLimit}.");
                }
            }

            if (record.MaxResponseElevation.HasValue)
            {
                entityAnalysisModel.Counters.MaxResponseElevation = record.MaxResponseElevation.Value;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} has been set with Max Response Elevation of {entityAnalysisModel.Counters.MaxResponseElevation}.");
                }
            }
            else
            {
                entityAnalysisModel.Counters.MaxResponseElevation = 0;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} has been set with DEFAULT Max Response Elevation of {entityAnalysisModel.Counters.MaxResponseElevation}.");
                }
            }

            if (record.Guid != Guid.Empty)
            {
                entityAnalysisModel.Instance.Guid = record.Guid;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} with Entity Analysis Model GUID value {entityAnalysisModel.Instance.Guid}.");
                }
            }
            else
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} with Entity Analysis Model GUID is empty.");
                }
            }

            if (record.TenantRegistryId.HasValue)
            {
                entityAnalysisModel.Instance.TenantRegistryId = record.TenantRegistryId.Value;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} with Tenant Registry ID value {entityAnalysisModel.Instance.TenantRegistryId}.");
                }
            }
            else
            {
                log.Error(
                    $"Entity Start: Model {entityAnalysisModel.Instance.Id} with Tenant Registry ID is empty,  which it cannot be.");
            }

            if (record.MaxResponseElevationInterval.HasValue)
            {
                entityAnalysisModel.Counters.MaxResponseElevationInterval =
                    record.MaxResponseElevationInterval.Value;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} with Max Response Elevation Frequency Interval value {entityAnalysisModel.Counters.MaxResponseElevationInterval}.");
                }
            }
            else
            {
                entityAnalysisModel.Counters.MaxResponseElevationInterval = 'n';

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} with DEFAULT Max Response Elevation Frequency Interval value {entityAnalysisModel.Counters.MaxResponseElevationInterval}.");
                }
            }

            if (record.MaxResponseElevationValue.HasValue)
            {
                entityAnalysisModel.Counters.MaxResponseElevationValue =
                    record.MaxResponseElevationValue.Value;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} with Max Response Elevation Frequency Value {entityAnalysisModel.Counters.MaxResponseElevationValue}.");
                }
            }
            else
            {
                entityAnalysisModel.Counters.MaxResponseElevationValue = 0;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} with DEFAULT Max Response Elevation Frequency Value {entityAnalysisModel.Counters.MaxResponseElevationValue}.");
                }
            }

            if (record.CacheTtlInterval.HasValue)
            {
                entityAnalysisModel.Cache.CacheTtlInterval =
                    record.CacheTtlInterval.Value;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} with Cache Ttl Interval value {entityAnalysisModel.Cache.CacheTtlInterval}.");
                }
            }
            else
            {
                entityAnalysisModel.Cache.CacheTtlInterval = 'd';

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} with DEFAULT Cache Ttl Interval value {entityAnalysisModel.Cache.CacheTtlInterval}.");
                }
            }

            if (record.CacheTtlIntervalValue.HasValue)
            {
                entityAnalysisModel.Cache.CacheTtlIntervalValue = record.CacheTtlIntervalValue.Value;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} with Cache Ttl Interval Value {entityAnalysisModel.Cache.CacheTtlIntervalValue}.");
                }
            }
            else
            {
                entityAnalysisModel.Cache.CacheTtlIntervalValue = 3;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} with DEFAULT Cache Ttl Interval Value {entityAnalysisModel.Cache.CacheTtlIntervalValue}.");
                }
            }

            if (record.MaxResponseElevationThreshold.HasValue)
            {
                entityAnalysisModel.Counters.MaxResponseElevationThreshold =
                    record.MaxResponseElevationThreshold.Value;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} with Max Response Elevation Frequency Threshold Value {entityAnalysisModel.Counters.MaxResponseElevationThreshold}.");
                }
            }
            else
            {
                entityAnalysisModel.Counters.MaxResponseElevationThreshold = 0;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} with DEFAULT Max Response Elevation Frequency Threshold Value {entityAnalysisModel.Counters.MaxResponseElevationThreshold}.");
                }
            }

            if (record.MaxActivationWatcherInterval.HasValue)
            {
                entityAnalysisModel.Counters.MaxActivationWatcherInterval =
                    record.MaxActivationWatcherInterval.Value;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} with Max Activation Watcher Interval value {entityAnalysisModel.Counters.MaxActivationWatcherInterval}.");
                }
            }
            else
            {
                entityAnalysisModel.Counters.MaxActivationWatcherInterval = 'n';

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} with DEFAULT Max Activation Watcher Interval value {entityAnalysisModel.Counters.MaxActivationWatcherInterval}.");
                }
            }

            if (record.MaxActivationWatcherValue.HasValue)
            {
                Interlocked.Exchange(ref entityAnalysisModel.Counters.MaxActivationWatcherValue,
                    record.MaxActivationWatcherValue.Value);

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} with Max Activation Watcher value {entityAnalysisModel.Counters.MaxActivationWatcherValue}.");
                }
            }
            else
            {
                Interlocked.Exchange(ref entityAnalysisModel.Counters.MaxActivationWatcherValue, 0);

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} with DEFAULT Max Activation Watcher value {entityAnalysisModel.Counters.MaxActivationWatcherValue}.");
                }
            }

            if (record.MaxActivationWatcherThreshold.HasValue)
            {
                entityAnalysisModel.Counters.MaxActivationWatcherThreshold =
                    record.MaxActivationWatcherThreshold.Value;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} with Max Activation Watcher Threshold value {entityAnalysisModel.Counters.MaxActivationWatcherThreshold}.");
                }
            }
            else
            {
                entityAnalysisModel.Counters.MaxActivationWatcherThreshold = 0;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} with DEFAULT Max Activation Watcher Threshold value {entityAnalysisModel.Counters.MaxActivationWatcherThreshold}.");
                }
            }

            if (record.ActivationWatcherSample.HasValue)
            {
                entityAnalysisModel.Counters.ActivationWatcherSample =
                    record.ActivationWatcherSample.Value;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} with Activation Watcher Sample value {entityAnalysisModel.Counters.ActivationWatcherSample}.");
                }
            }
            else
            {
                entityAnalysisModel.Counters.ActivationWatcherSample = 0;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} with DEFAULT Activation Watcher Sample value {entityAnalysisModel.Counters.ActivationWatcherSample}.");
                }
            }

            if (record.EnableActivationArchive.HasValue)
            {
                entityAnalysisModel.Flags.EnableActivationArchive =
                    record.EnableActivationArchive.Value == 1;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} with Promote Activation Archive value {entityAnalysisModel.Flags.EnableActivationArchive}.");
                }
            }
            else
            {
                entityAnalysisModel.Flags.EnableActivationArchive = false;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} with DEFAULT Promote Activation Archive value {entityAnalysisModel.Flags.EnableActivationArchive}.");
                }
            }

            if (record.EnableRdbmsArchive.HasValue)
            {
                entityAnalysisModel.Flags.EnableRdbmsArchive = record.EnableRdbmsArchive.Value == 1;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} with Enable Database value {entityAnalysisModel.Flags.EnableRdbmsArchive}.");
                }
            }
            else
            {
                entityAnalysisModel.Flags.EnableRdbmsArchive = true;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Entity Start: Model {entityAnalysisModel.Instance.Id} with DEFAULT Enable Database value {entityAnalysisModel.Flags.EnableRdbmsArchive}.");
                }
            }
        }
    }
}