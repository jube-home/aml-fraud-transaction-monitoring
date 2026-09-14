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
using Jube.ResilientRedisConnection;
using log4net;

namespace Jube.Cache.Redis
{
    public class CacheTtlCounterIdempotencyRepository(
        IHybridResilientRedisDatabase resilientRedisResilientRedisDatabase,
        ILog log) : ICacheTtlCounterIdempotencyRepository
    {
        public Task<bool> CheckAndClaimIdempotencyAsync(int tenantRegistryId,
            Guid entityAnalysisModelGuid,
            Guid ttlCounterGuid, Guid entityAnalysisModelInstanceEntryGuid)
        {
            return CacheDiagnostics.RecordAsync("CacheTtlCounterIdempotencyRepository.CheckAndClaimIdempotencyAsync",
                async () =>
                {
                    try
                    {
                        var redisKey =
                            $"TtlCounterIdempotency:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{ttlCounterGuid:N}";
                        var redisJournal = $"IdempotencyJournal:{tenantRegistryId}:{entityAnalysisModelGuid:N}";
                        var redisSetKey = $"{entityAnalysisModelInstanceEntryGuid:N}";

                        if (await resilientRedisResilientRedisDatabase.SetContainsAsync(redisKey, redisSetKey))
                        {
                            return false;
                        }

                        await resilientRedisResilientRedisDatabase.SetAddAsync(redisKey, redisSetKey)
                            .ConfigureAwait(false);
                        await resilientRedisResilientRedisDatabase.SetAddAsync(redisJournal, $"{redisKey}")
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Cache Redis: Has created an exception as {ex}.");

                        return false;
                    }

                    return true;
                });
        }
    }
}