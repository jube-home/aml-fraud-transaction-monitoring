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

namespace Jube.Engine.BackgroundTasks.TaskStarters
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using EntityAnalysisModelInvoke;
    using InboundQueue;

    public class InboundQueueTaskStarter(Context context)
    {
        public async Task StartAsync()
        {
            var environment = context.Services.DynamicEnvironment;

            int Setting(string name)
            {
                return int.Parse(environment.AppSettings(name), CultureInfo.InvariantCulture);
            }

            var consumer = new InboundQueueConsumer(environment.AppSettings("ConnectionString"), context.Services.Log,
                new InboundQueueOptions(Setting("InboundQueueBatchSize"), Setting("InboundQueueConcurrency"),
                    Setting("InboundQueuePollMilliseconds"), Setting("InboundQueueMaximumAttempts"),
                    Setting("InboundQueueVisibilitySeconds")),
                $"{Environment.MachineName}:{Environment.ProcessId}",
                () => context.Tasks.EntityAnalysisModelManager.Context.EntityAnalysisModels
                    .EntityModelsHasLoadedForStartup,
                StateOf,
                InvokeAsync);

            try
            {
                await consumer.RunAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                context.Services.Log.Info("Graceful Cancellation InboundQueue.");
            }
            catch (Exception ex)
            {
                context.Services.Log.Error($"InboundQueue: has produced an error {ex}");
            }
        }

        private InboundModelState StateOf(Guid guid)
        {
            var model = context.Tasks.EntityAnalysisModelManager.Context.EntityAnalysisModels
                .ActiveEntityAnalysisModels.Where(kvp => kvp.Value.Instance.Guid == guid)
                .Select(kvp => kvp.Value).FirstOrDefault();

            return model is null
                ? InboundModelState.Unknown
                : model.Started
                    ? InboundModelState.Ready
                    : InboundModelState.NotStarted;
        }

        private async Task<bool> InvokeAsync(Guid guid, byte[] payload, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var model = context.Tasks.EntityAnalysisModelManager.Context.EntityAnalysisModels
                .ActiveEntityAnalysisModels.Where(kvp => kvp.Value.Instance.Guid == guid)
                .Select(kvp => kvp.Value).FirstOrDefault();

            if (model is not { Started: true })
            {
                return false;
            }

            using var input = new MemoryStream(payload);
            var result = await EntityAnalysisModelInvoke.InvokeAsync(model, input).ConfigureAwait(false);

            var payloadResult = result.EntityAnalysisModelInstanceEntryPayload;

            return payloadResult.ResponseJson is { Length: > 0 } || payloadResult.ArchiveJson is { Length: > 0 };
        }
    }
}
