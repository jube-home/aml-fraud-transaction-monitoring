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
using Jube.Cache.Redis.Serialization;
using Jube.ResilientRedisConnection;
using log4net;
using MessagePack;

namespace Jube.Cache.Redis
{
    public class CacheSanctionRepository(
        IHybridResilientRedisDatabase resilientRedisResilientRedisDatabase,
        ILog log) : ICacheSanctionRepository
    {
        public Task<CacheSanction> GetByMultiPartStringDistanceThresholdAsync(int tenantRegistryId,
            Guid entityAnalysisModelGuid, string multiPartString,
            int distanceThreshold)
        {
            return CacheDiagnostics.RecordAsync("CacheSanctionRepository.GetByMultiPartStringDistanceThresholdAsync",
                async () =>
                {
                    try
                    {
                        var redisKey = $"Sanction:{tenantRegistryId}:{entityAnalysisModelGuid:N}";
                        var redisHSetKey = $"{multiPartString}:{distanceThreshold}";

                        var hashValue = await resilientRedisResilientRedisDatabase.HashGetAsync(redisKey, redisHSetKey)
                            .ConfigureAwait(false);

                        if (!hashValue.HasValue)
                        {
                            return null;
                        }

                        var sanction = MessagePackSerializer
                            .Deserialize<Sanction>(hashValue,
                                MessagePackSerializerOptionsHelper.StandardMessagePackSerializerWithCompressionOptions(
                                    false));

                        return new CacheSanction
                        {
                            CreatedDate = sanction.CreatedDate,
                            Value = sanction.Value
                        };
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Cache Redis: Has created an exception as {ex}.");
                    }

                    return null;
                });
        }

        public Task InsertAsync(int tenantRegistryId, Guid entityAnalysisModelGuid, string multiPartString,
            int distanceThreshold,
            double? value)
        {
            return CacheDiagnostics.RecordAsync("CacheSanctionRepository.InsertAsync", async () =>
            {
                try
                {
                    var redisKey = $"Sanction:{tenantRegistryId}:{entityAnalysisModelGuid:N}";
                    var redisHSetKey = $"{multiPartString}:{distanceThreshold}";

                    var sanction = new Sanction
                    {
                        Value = value,
                        CreatedDate = DateTime.UtcNow
                    };

                    var ms = new MemoryStream();
                    await MessagePackSerializer.SerializeAsync(ms, sanction,
                            MessagePackSerializerOptionsHelper.StandardMessagePackSerializerWithCompressionOptions(
                                false))
                        .ConfigureAwait(false);
                    await resilientRedisResilientRedisDatabase.HashSetAsync(redisKey, redisHSetKey, ms.ToArray())
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    log.Error($"Cache Redis: Has created an exception as {ex}.");
                }
            });
        }
    }
}