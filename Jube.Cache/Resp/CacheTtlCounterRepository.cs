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
using Jube.ResilientRespConnection.Interfaces;
using log4net;

namespace Jube.Cache.Resp
{
    public class CacheTtlCounterRepository(
        IHybridResilientRespDatabase resilientRespResilientRespDatabase,
        ILog log) : ICacheTtlCounterRepository
    {
        public Task<double> DecrementTtlCounterCacheAsync(int tenantRegistryId, Guid entityAnalysisModelGuid,
            Guid entityAnalysisModelTtlCounterGuid,
            string dataName, string dataValue, double decrement)
        {
            return CacheDiagnostics.RecordAsync("CacheTtlCounterRepository.DecrementTtlCounterCacheAsync", async () =>
            {
                try
                {
                    var respKey =
                        $"TtlCounter:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{entityAnalysisModelTtlCounterGuid:N}:{dataName}";

                    var respHSetKey = $"{dataValue}";

                    var value = await resilientRespResilientRespDatabase
                        .HashDecrementAsync(respKey, respHSetKey, decrement).ConfigureAwait(false);

                    if (value > 0)
                    {
                        return value;
                    }

                    await resilientRespResilientRespDatabase.HashDeleteAsync(respKey, respHSetKey);

                    return 0;
                }
                catch (Exception ex)
                {
                    log.Error($"Cache RESP: Has created an exception as {ex}.");
                }

                return 0;
            });
        }

        public Task<double> GetByNameDataNameDataValueAsync(int tenantRegistryId, Guid entityAnalysisModelGuid,
            Guid entityAnalysisModelTtlCounterGuid, string dataName, string dataValue)
        {
            return CacheDiagnostics.RecordAsync("CacheTtlCounterRepository.GetByNameDataNameDataValueAsync", async () =>
            {
                try
                {
                    var respKey =
                        $"TtlCounter:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{entityAnalysisModelTtlCounterGuid:N}:{dataName}";

                    var respHSetKey = $"{dataValue}";

                    return (double)await resilientRespResilientRespDatabase.HashGetAsync(respKey, respHSetKey)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    log.Error($"Cache RESP: Has created an exception as {ex}.");
                }

                return 0;
            });
        }

        public Task IncrementTtlCounterCacheAsync(int tenantRegistryId, Guid entityAnalysisModelGuid, string dataName,
            string dataValue,
            Guid entityAnalysisModelTtlCounterGuid, double increment, DateTime referenceDate)
        {
            return CacheDiagnostics.RecordAsync("CacheTtlCounterRepository.IncrementTtlCounterCacheAsync", async () =>
            {
                try
                {
                    var respKey =
                        $"TtlCounter:{tenantRegistryId}:{entityAnalysisModelGuid:N}:{entityAnalysisModelTtlCounterGuid:N}:{dataName}";

                    var respHSetKey = $"{dataValue}";

                    await resilientRespResilientRespDatabase.HashIncrementAsync(respKey, respHSetKey, increment)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    log.Error($"Cache RESP: Has created an exception as {ex}.");
                }
            });
        }
    }
}