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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jube.Data.Repository;
using LinqToDB;
using LinqToDB.Async;
using Jube.Engine.Svirfd;

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions
{
    public static class SyncEntityAnalysisModelSvirfdExtensions
    {
        public static async Task<Context> SyncEntityAnalysisModelSvirfdAsync(this Context context)
        {
            try
            {
                context.Services.Parser.EntityAnalysisModelsSvirfd = [];

                var tenantRemoteLookups = new Dictionary<int, bool>();

                foreach (var (key, value) in context.ModelsInScope())
                {
                    context.Services.CancellationToken.ThrowIfCancellationRequested();

                    var records = await new EntityAnalysisModelSvirfdRepository(context.Services.DbContext,
                            value.Instance.TenantRegistryId)
                        .GetApprovedByEntityAnalysisModelIdAsync(key, context.ApprovalsRequired,
                            context.Services.CancellationToken)
                        .ConfigureAwait(false);

                    var tenantRegistryId = value.Instance.TenantRegistryId;

                    if (!tenantRemoteLookups.TryGetValue(tenantRegistryId, out var remoteLookupsEnabled))
                    {
                        var tenant = await context.Services.DbContext.TenantRegistry
                            .FirstOrDefaultAsync(w => w.Id == tenantRegistryId
                                                      && (w.Deleted == 0 || w.Deleted == null),
                                context.Services.CancellationToken).ConfigureAwait(false);

                        remoteLookupsEnabled = tenant?.SvirfdRemoteLookupsEnabled != 0;
                        tenantRemoteLookups[tenantRegistryId] = remoteLookupsEnabled;

                        if (!remoteLookupsEnabled)
                        {
                            context.Services.Log.Info(
                                $"Entity Start: Tenant {tenantRegistryId} has remote Svirfd lookups switched off by the landlord; no IP address of its transactions will be sent to the Svirfd service.");
                        }
                    }

                    var plan = SvirfdPlan.Build(records, remoteLookupsEnabled);

                    context.Snapshots.Builder(key, value).Svirfd = plan;

                    foreach (var name in plan.Names)
                    {
                        if (!context.Services.Parser.EntityAnalysisModelsSvirfd.Contains(name))
                        {
                            context.Services.Parser.EntityAnalysisModelsSvirfd.Add(name);
                        }
                    }

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Svirfd for model {key} has {plan.Entities.Count} entries ({string.Join(", ", plan.Entities.Select(e => $"{e.Name} from {e.Field}"))}).");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                context.Services.Log.Error($"Entity Start: Svirfd sync has produced an error {ex}");
            }

            return context;
        }
    }
}
