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
using Jube.Data.Context;
using Jube.Data.Query.GetGraphRecallTenantsQuery;
using Jube.Engine.GraphRecall;

namespace Jube.Engine.BackgroundTasks.TaskStarters
{
    public class GraphRecallTaskStarter(Context.Context context)
    {
        public const int FailureRetryWait = 30000;

        public async Task StartAsync()
        {
            try
            {
                while (!context.Services.TaskCoordinator.CancellationToken.IsCancellationRequested)
                {
                    var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                        context.Services.DynamicEnvironment.AppSettings("ConnectionString"), context.Services.Log);

                    var loaded = false;

                    try
                    {
                        loaded = await LoadAllTenantsAsync(dbContext).ConfigureAwait(false);

                        if (loaded)
                        {
                            context.GraphRecall.LoadedForStartup = true;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        context.Services.Log.Error($"Graph Recall Loader: Error {ex}");
                    }
                    finally
                    {
                        try
                        {
                            await dbContext.CloseAsync().ConfigureAwait(false);
                        }
                        finally
                        {
                            await dbContext.DisposeAsync().ConfigureAwait(false);
                        }
                    }

                    var wait = ResolveWait(context.Services.DynamicEnvironment.AppSettings("GraphRecallLoaderWait"));

                    await Task.Delay(NextWait(loaded, wait),
                        context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException ex)
            {
                context.Services.Log.Info($"Graceful Cancellation GraphRecallAsync: has produced an error {ex}");
            }
            catch (Exception ex)
            {
                context.Services.Log.Error($"GraphRecallAsync: has produced an error {ex}");
            }
        }

        public static bool Succeeded(int failures, int successes)
        {
            return failures == 0 || successes > 0;
        }

        public static int NextWait(bool loaded, int wait)
        {
            return loaded ? wait : Math.Min(wait, FailureRetryWait);
        }

        public static int ResolveWait(string setting)
        {
            return int.TryParse(setting, out var wait) && wait > 0 ? wait : 900000;
        }

        public static GraphRecallLoaderOptions ResolveOptions(DynamicEnvironment.DynamicEnvironment environment)
        {
            var hashKeys = string.Equals(environment.AppSettings("GraphRecallHashKeys"), "True",
                StringComparison.OrdinalIgnoreCase);

            var maximumNodes = int.TryParse(environment.AppSettings("GraphRecallMaximumNodesPerTenant"),
                out var cap) && cap > 0
                ? cap
                : 2000000;

            var maximumEdges = int.TryParse(environment.AppSettings("GraphRecallMaximumEdgesPerTenant"),
                out var edgeCap) && edgeCap > 0
                ? edgeCap
                : 4000000;

            return new GraphRecallLoaderOptions(hashKeys, maximumNodes, maximumEdges,
                GraphRecallPlan.ResolveDistanceCap(environment.AppSettings("GraphDistanceCap")));
        }

        private async Task<bool> LoadAllTenantsAsync(DbContext dbContext)
        {
            var token = context.Services.TaskCoordinator.CancellationToken;
            var options = ResolveOptions(context.Services.DynamicEnvironment);

            var tenants = (await new GetGraphRecallTenantsQuery(dbContext).ExecuteAsync(token).ConfigureAwait(false))
                .Select(t => t.TenantRegistryId).ToList();

            foreach (var stale in context.GraphRecall.Tenants.Where(t => !tenants.Contains(t)).ToList())
            {
                context.GraphRecall.Remove(stale);
            }

            var failures = 0;
            var successes = 0;

            foreach (var tenantRegistryId in tenants)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    var snapshot = await GraphRecallSnapshotLoader.LoadAsync(dbContext, tenantRegistryId, options,
                        context.Services.Log, token).ConfigureAwait(false);

                    context.GraphRecall.Publish(snapshot);
                    successes++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    failures++;
                    context.Services.Log.Error($"Graph Recall Loader: tenant {tenantRegistryId} has produced an error {ex}");
                }
            }

            return Succeeded(failures, successes);
        }
    }
}
