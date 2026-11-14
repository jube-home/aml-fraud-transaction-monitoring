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
using System.Linq;
using System.Threading.Tasks;
using Jube.Data.Repository;
using Jube.Engine.GraphRecall;

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions
{
    public static class SyncEntityAnalysisModelGraphRecallExtensions
    {
        public static async Task<Context> SyncEntityAnalysisModelGraphRecallAsync(this Context context)
        {
            try
            {
                context.Services.Parser.EntityAnalysisModelsGraph = [];

                var distanceCap =
                    GraphRecallPlan.ResolveDistanceCap(context.Services.DynamicEnvironment?.AppSettings("GraphDistanceCap"));

                foreach (var (key, value) in context.ModelsInScope())
                {
                    context.Services.CancellationToken.ThrowIfCancellationRequested();

                    var records = await new EntityAnalysisModelGraphRecallRepository(context.Services.DbContext,
                            value.Instance.TenantRegistryId)
                        .GetApprovedByEntityAnalysisModelIdAsync(key, context.ApprovalsRequired,
                            context.Services.CancellationToken)
                        .ConfigureAwait(false);

                    var plan = GraphRecallPlan.Build(records, distanceCap);

                    context.Snapshots.Builder(key, value).GraphRecall = plan;

                    foreach (var name in plan.Names)
                    {
                        if (!context.Services.Parser.EntityAnalysisModelsGraph.Contains(name))
                        {
                            context.Services.Parser.EntityAnalysisModelsGraph.Add(name);
                        }
                    }

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Graph recall for model {key} has {plan.Entities.Count} entities ({string.Join(", ", plan.Entities.Select(e => e.CounterpartName == null ? e.Name : $"{e.Name} with {e.CounterpartName}"))}) with a distance cap of {plan.DistanceCap}.");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                context.Services.Log.Error($"Entity Start: Graph recall sync has produced an error {ex}");
            }

            return context;
        }
    }
}
