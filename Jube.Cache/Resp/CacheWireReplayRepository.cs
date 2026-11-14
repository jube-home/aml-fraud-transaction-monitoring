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
    public class CacheWireReplayRepository(
        IHybridResilientRespDatabase resilientRespResilientRespDatabase,
        ILog log) : ICacheWireReplayRepository
    {
        public Task<bool> TryClaimAsync(string fingerprint, TimeSpan retention)
        {
            return CacheDiagnostics.RecordAsync("CacheWireReplayRepository.TryClaimAsync", async () =>
            {
                try
                {
                    return await resilientRespResilientRespDatabase
                        .StringSetIfAbsentAsync($"WireReplay:{fingerprint}", 1, retention)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    log.Error($"Cache RESP: Has created an exception as {ex}.");

                    return false;
                }
            });
        }
    }
}
