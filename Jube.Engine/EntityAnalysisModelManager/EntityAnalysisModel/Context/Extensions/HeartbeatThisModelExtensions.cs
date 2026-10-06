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

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Data.Poco;
    using Data.Repository;

    public static class HeartbeatThisModelExtensions
    {
        public static async Task<Context> HeartbeatThisModelAsync(this Context context, int tenantRegistryId)
        {
            try
            {
                var repository =
                    new EntityAnalysisModelSynchronisationNodeStatusEntryRepository(context.Services.DbContext);

                var upsert = new EntityAnalysisModelSynchronisationNodeStatusEntry
                {
                    TenantRegistryId = tenantRegistryId,
                    Instance = Dns.GetHostName()
                };

                await repository.UpsertHeartbeatAsync(upsert, context.Services.CancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"HeartbeatThisModelAsync: has produced an error {ex}");
            }

            return context;
        }
    }
}