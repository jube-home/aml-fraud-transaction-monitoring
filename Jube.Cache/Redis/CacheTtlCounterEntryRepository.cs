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

using Jube.Cache.Observability;
using Jube.Cache.Redis.Interfaces;
using Jube.Cache.Redis.Models;
using Jube.Extensions;
using Jube.ResilientRedisConnection;
using log4net;
using StackExchange.Redis;

namespace Jube.Cache.Redis
{
    public class CacheTtlCounterEntryRepository(
        IHybridResilientRedisDatabase resilientRedisResilientRedisDatabase,
        ILog log) : ICacheTtlCounterEntryRepository
    {
        public Task<List<ExpiredTtlCounterEntry>>
            GetAllExpiredByTtlCounterPreferReplicaAsync(int tenantRegistryId, Guid entityAnalysisModelGuid,
                Guid entityAnalysisModelTtlCounterGuid, string dataName, DateTime referenceDate, int limit)
        {
            return CacheDiagnostics.RecordAsync(
                "CacheTtlCounterEntryRepository.GetAllExpiredByTtlCounterPreferReplicaAsync", async () =>
                {
                    var expired = new List<ExpiredTtlCounterEntry>();
                    try
                    {
                        var referenceDateTimestamp = referenceDate.ToUnixTimeMilliSeconds();
                        var redisKeyExpiryIndex = ExpiryIndexKey(tenantRegistryId, entityAnalysisModelGuid,
                            entityAnalysisModelTtlCounterGuid, dataName);

                        var expiredMembers = await resilientRedisResilientRedisDatabase
                            .SortedSetRangeByScoreWithScoresAsync(
                                redisKeyExpiryIndex,
                                long.MinValue,
                                referenceDateTimestamp,
                                Exclude.Stop,
                                skip: 0,
                                take: limit,
                                flags: CommandFlags.PreferReplica
                            ).ConfigureAwait(false);

                        foreach (var expiredMember in expiredMembers)
                        {
                            var parts = ((string)expiredMember.Element)?.Split(':', 2);
                            if (parts == null || parts.Length != 2 || !long.TryParse(parts[0], out var entryTimestamp))
                            {
                                continue;
                            }

                            var dataValue = parts[1];

                            var redisKeyTtlCounterEntry = $"TtlCounterEntry:{tenantRegistryId}" +
                                                          $":{entityAnalysisModelGuid:N}:{entityAnalysisModelTtlCounterGuid:N}" +
                                                          $":{dataName}:{dataValue}";

                            var value = await resilientRedisResilientRedisDatabase.HashGetAsync(redisKeyTtlCounterEntry,
                                $"{entryTimestamp}",
                                CommandFlags.PreferReplica).ConfigureAwait(false);

                            if (!value.HasValue)
                            {
                                continue;
                            }

                            expired.Add(new ExpiredTtlCounterEntry
                            {
                                Value = (double)value,
                                DataName = dataName,
                                DataValue = dataValue,
                                ReferenceDate = entryTimestamp.FromUnixTimeMilliSeconds()
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Cache Redis: Has created an exception as {ex}.");
                    }

                    return expired;
                });
        }

        public Task<double> GetAggregationPreferReplicaAsync(int tenantRegistryId,
            Guid entityAnalysisModelGuid, Guid entityAnalysisModelTtlCounterGuid,
            string dataName, string dataValue,
            DateTime referenceDateFrom, DateTime referenceDateTo)
        {
            return CacheDiagnostics.RecordAsync("CacheTtlCounterEntryRepository.GetAggregationPreferReplicaAsync",
                async () =>
                {
                    try
                    {
                        var referenceDateFromTimestamp = referenceDateFrom.ToUnixTimeMilliSeconds();
                        var referenceDateToTimestamp = referenceDateTo.ToUnixTimeMilliSeconds();

                        var redisKey =
                            $"TtlCounterEntry:{tenantRegistryId}:{entityAnalysisModelGuid:N}" +
                            $":{entityAnalysisModelTtlCounterGuid:N}:{dataName}:{dataValue}";

                        var sum = 0d;
                        await foreach (var hashEntry in resilientRedisResilientRedisDatabase.HashScanAsync(redisKey,
                                           flags: CommandFlags.PreferReplica))
                        {
                            var timestamp = (long)hashEntry.Name;
                            if (timestamp >= referenceDateFromTimestamp && timestamp <= referenceDateToTimestamp)
                            {
                                sum += (double)hashEntry.Value;
                            }
                        }

                        return sum;
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Cache Redis: Has created an exception as {ex}.");
                        return 0d;
                    }
                });
        }

        public Task UpsertAsync(int tenantRegistryId, Guid entityAnalysisModelGuid, string dataName, string dataValue,
            Guid entityAnalysisModelTtlCounterGuid, DateTime referenceDate, double increment)
        {
            return CacheDiagnostics.RecordAsync("CacheTtlCounterEntryRepository.UpsertAsync", async () =>
            {
                try
                {
                    var referenceDateTimestamp = referenceDate.ToUnixTimeMilliSeconds();

                    var redisKey =
                        $"TtlCounterEntry:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{entityAnalysisModelTtlCounterGuid:N}:{dataName}:{dataValue}";
                    var redisHSetKey = $"{referenceDateTimestamp}";

                    var redisKeyExpiryIndex = ExpiryIndexKey(tenantRegistryId, entityAnalysisModelGuid,
                        entityAnalysisModelTtlCounterGuid, dataName);
                    var expiryIndexMember = $"{referenceDateTimestamp}:{dataValue}";

                    await Task.WhenAll(
                        resilientRedisResilientRedisDatabase.HashIncrementAsync(redisKey, redisHSetKey, increment),
                        resilientRedisResilientRedisDatabase.SortedSetAddAsync(redisKeyExpiryIndex, expiryIndexMember,
                            referenceDateTimestamp)
                    ).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    log.Error($"Cache Redis: Has created an exception as {ex}.");
                }
            });
        }

        public Task DeleteAsync(int tenantRegistryId, Guid entityAnalysisModelGuid,
            Guid entityAnalysisModelTtlCounterGuid,
            string dataName,
            string dataValue, DateTime referenceDate)
        {
            return CacheDiagnostics.RecordAsync("CacheTtlCounterEntryRepository.DeleteAsync", async () =>
            {
                try
                {
                    var referenceDateTimestamp = referenceDate.ToUnixTimeMilliSeconds();

                    var redisKey =
                        $"TtlCounterEntry:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{entityAnalysisModelTtlCounterGuid:N}:{dataName}:{dataValue}";
                    var redisHSetKey = $"{referenceDateTimestamp}";

                    var redisKeyExpiryIndex = ExpiryIndexKey(tenantRegistryId, entityAnalysisModelGuid,
                        entityAnalysisModelTtlCounterGuid, dataName);
                    var expiryIndexMember = $"{referenceDateTimestamp}:{dataValue}";

                    await Task.WhenAll(
                        resilientRedisResilientRedisDatabase.HashDeleteAsync(redisKey, redisHSetKey),
                        resilientRedisResilientRedisDatabase.SortedSetRemoveAsync(redisKeyExpiryIndex,
                            expiryIndexMember)
                    ).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    log.Error($"Cache Redis: Has created an exception as {ex}.");
                }
            });
        }

        private static string ExpiryIndexKey(int tenantRegistryId, Guid entityAnalysisModelGuid,
            Guid entityAnalysisModelTtlCounterGuid, string dataName)
        {
            return
                $"TtlCounterEntryExpiry:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{entityAnalysisModelTtlCounterGuid:N}:{dataName}";
        }
    }
}