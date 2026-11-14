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

    public class AmqpTaskStarter(Context context)
    {
        public const string QueueName = "jubeInbound";

        private readonly RabbitMQ.Client.IModel channel = context.Services.RabbitMqConnection.CreateModel();

        public async Task StartAsync()
        {
            try
            {
                var environment = context.Services.DynamicEnvironment;

                var consumer = new AmqpInboundConsumer(channel, context.Services.Log, QueueName,
                    "True".Equals(environment.AppSettings("AmqpInboundQueueDurable"),
                        StringComparison.OrdinalIgnoreCase),
                    ushort.Parse(environment.AppSettings("AmqpPrefetch"), CultureInfo.InvariantCulture),
                    () => context.Tasks.EntityAnalysisModelManager.Context.EntityAnalysisModels
                        .EntityModelsHasLoadedForStartup,
                    StateOf, InvokeAsync, context.Services.TaskCoordinator.CancellationToken);

                consumer.Start();

                _ = context.Services.TaskCoordinator.CancellationToken.Register(() =>
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            context.Services.Log.Info("Cancellation requested: stopping RabbitMQ consumer...");
                            await consumer.StopAsync().ConfigureAwait(false);
                            context.Services.RabbitMqConnection.Close();
                            context.Services.Log.Info(
                                "RabbitMQ channel closed safely after in-flight messages completed.");
                        }
                        catch (Exception ex)
                        {
                            context.Services.Log.Error("Error during RabbitMQ teardown: " + ex);
                        }
                    }, CancellationToken.None);
                });

                await Task.Delay(Timeout.Infinite, context.Services.TaskCoordinator.CancellationToken);
            }
            catch (OperationCanceledException ex)
            {
                context.Services.Log.Info($"Graceful Cancellation StartAmqp: has produced an error {ex}");
            }
            catch (Exception ex)
            {
                context.Services.Log.Error($"StartAmqp: has produced an error {ex}");
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
