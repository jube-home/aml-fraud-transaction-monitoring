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
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Data.Repository;
    using Integrity;
    using Newtonsoft.Json;

    public static class PersistEngineSnapshotExtensions
    {
        public static async Task<Context> PersistEngineSnapshotAsync(this Context context, int tenantRegistryId)
        {
            try
            {
                var repository = new EntityAnalysisModelEngineSnapshotRepository(context.Services.DbContext);
                var instance = Dns.GetHostName();
                var now = DateTime.UtcNow;

                foreach (var model in context.EntityAnalysisModels.ActiveEntityAnalysisModels.Values
                             .Where(m => m.Instance.TenantRegistryId == tenantRegistryId).ToList())
                {
                    var state = EngineModelStateBuilder.FromModel(model, instance, now);
                    await repository.UpsertAsync(instance, tenantRegistryId, model.Instance.Id,
                        JsonConvert.SerializeObject(state), context.Services.CancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"PersistEngineSnapshotAsync: Has produced an error {ex}");
            }

            return context;
        }
    }
}