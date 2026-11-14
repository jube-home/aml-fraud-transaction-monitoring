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
using Jube.Cache.Resp.Interfaces;
using Jube.Extensions;
using Jube.ResilientRespConnection.Interfaces;
using log4net;
using StackExchange.Redis;

namespace Jube.Cache.Resp
{
    public class CacheReferenceDateRepository(
        IHybridResilientRespDatabase resilientRespResilientRespDatabase,
        ILog log) : ICacheReferenceDate
    {
        public Task UpsertReferenceDateAsync(int tenantRegistryId, Guid entityAnalysisModelGuid, DateTime referenceDate)
        {
            return CacheDiagnostics.RecordAsync("CacheReferenceDateRepository.UpsertReferenceDateAsync", async () =>
            {
                try
                {
                    var respKey = $"ReferenceDate:{tenantRegistryId}";
                    var respHSetKey = $"{entityAnalysisModelGuid:N}";

                    await resilientRespResilientRespDatabase.HashSetAsync(respKey, respHSetKey,
                        referenceDate.ToUnixTimeMilliSeconds()).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    log.Error($"Cache RESP: Has created an exception as {ex}.");
                }
            });
        }

        public Task<DateTime?> GetReferenceDateAsync(int tenantRegistryId, Guid entityAnalysisModelGuid)
        {
            return CacheDiagnostics.RecordAsync("CacheReferenceDateRepository.GetReferenceDateAsync", async () =>
            {
                try
                {
                    var respKey = $"ReferenceDate:{tenantRegistryId}";
                    var respHSetKey = $"{entityAnalysisModelGuid:N}";

                    var respValue = await resilientRespResilientRespDatabase
                        .HashGetAsync(respKey, respHSetKey).ConfigureAwait(false);

                    if (!respValue.HasValue)
                    {
                        return null;
                    }

                    return ((long)respValue).FromUnixTimeMilliSeconds();
                }
                catch (Exception ex)
                {
                    log.Error($"Cache RESP: Has created an exception as {ex}.");
                }

                return (DateTime?)null;
            });
        }

        public Task<DateTime?> GetReferenceDatePreferReplicaAsync(int tenantRegistryId, Guid entityAnalysisModelGuid)
        {
            return CacheDiagnostics.RecordAsync("CacheReferenceDateRepository.GetReferenceDatePreferReplicaAsync",
                async () =>
                {
                    try
                    {
                        var respKey = $"ReferenceDate:{tenantRegistryId}";
                        var respHSetKey = $"{entityAnalysisModelGuid:N}";

                        var respValue = await resilientRespResilientRespDatabase
                            .HashGetAsync(respKey, respHSetKey, CommandFlags.PreferReplica).ConfigureAwait(false);

                        if (!respValue.HasValue)
                        {
                            return null;
                        }

                        return ((long)respValue).FromUnixTimeMilliSeconds();
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Cache RESP: Has created an exception as {ex}.");
                    }

                    return (DateTime?)null;
                });
        }
    }
}