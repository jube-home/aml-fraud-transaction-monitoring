/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Jube.AskJooby.Routing;
using log4net;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Hosting;

namespace Jube.App.Code.AskJooby
{
    public sealed class RoutingExemplarWarmupService(
        DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
        ILog log,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!"True".Equals(dynamicEnvironment.AppSettings("EnableAskJooby"),
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                var stopwatch = Stopwatch.StartNew();

                var warmed = await AgentRouter.WarmExemplarCacheAsync(embeddingGenerator, stoppingToken)
                    .ConfigureAwait(false);

                stopwatch.Stop();

                if (log.IsInfoEnabled)
                {
                    log.Info($"AskJooby: warmed {warmed} routing exemplar embeddings in "
                             + $"{stopwatch.ElapsedMilliseconds}ms, so the first conversation of this "
                             + "process does not pay for them.");
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                log.Warn("AskJooby: could not warm the routing exemplar embeddings, so the first "
                         + "conversation will fall back to the model routing stage. Check "
                         + "AskJoobyEmbeddingApiEndpoint, AskJoobyEmbeddingApiKey and "
                         + $"AskJoobyEmbeddingModel. The failure was {ex}.");
            }
        }
    }
}