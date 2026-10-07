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

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Helpers
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Data.Context;
    using Data.Query.GetEntityAnalysisModelSyncWatermarkQuery;
    using Data.Query.Models;
    using log4net;

    public static class SyncWatermarkHelpers
    {
        public static async Task<EntityAnalysisModelSyncWatermark> GetAsync(ILog log, DbContext dbContext,
            int tenantRegistryId, CancellationToken token)
        {
            try
            {
                return await new GetEntityAnalysisModelSyncWatermarkQuery(dbContext)
                    .ExecuteAsync(tenantRegistryId, token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.Error($"GetSyncWatermark: tenant {tenantRegistryId} has produced an error {ex}," +
                          " will synchronise.");

                return null;
            }
        }
    }
}