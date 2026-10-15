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
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using EntityAnalysisModelInvoke;
    using EntityAnalysisModelManager.EntityAnalysisModel;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;

    public class AmqpTaskStarter(Context context)
    {
        private readonly IModel channel = context.Services.RabbitMqConnection.CreateModel();
        private EventingBasicConsumer consumer;
        private string consumerTag;
        private int inFlight;

        public async Task StartAsync()
        {
            try
            {
                channel.QueueDeclare("jubeInbound", false, false, false, null);

                consumer = new EventingBasicConsumer(channel);

                consumer.Received += (o, ea) =>
                {
                    Interlocked.Increment(ref inFlight);

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            if (context.Services.TaskCoordinator.CancellationToken.IsCancellationRequested)
                            {
                                context.Services.Log.Info(
                                    "Cancellation requested before processing message; requeueing.");

                                return;
                            }

                            if (context.Services.Log.IsInfoEnabled)
                            {
                                context.Services.Log.Info("AMQP Inbound: Received message, checking headers.");
                            }

                            if (ea.BasicProperties.Headers == null)
                            {
                                context.Services.Log.Info("AMQP Inbound: Header is null, rejecting message.");

                                return;
                            }

                            if (!ea.BasicProperties.Headers.TryGetValue("EntityAnalysisModelGuid", out var header))
                            {
                                context.Services.Log.Info(
                                    "AMQP Inbound: EntityAnalysisModelGuid header missing, rejecting message.");

                                return;
                            }

                            var entityAnalysisModelGuid = Encoding.UTF8.GetString((byte[])header);
                            EntityAnalysisModel entityAnalysisModel = null;

                            foreach (var (_, value) in
                                     from modelKvp in context.Tasks.EntityAnalysisModelManager.Context
                                         .EntityAnalysisModels.ActiveEntityAnalysisModels
                                     where entityAnalysisModelGuid == modelKvp.Value.Instance.Guid.ToString()
                                     select modelKvp)
                            {
                                if (!context.Tasks.EntityAnalysisModelManager.Context.EntityAnalysisModels
                                        .EntityModelsHasLoadedForStartup)
                                {
                                    channel.BasicReject(ea.DeliveryTag, false);
                                    context.Services.Log.Info("Models not ready; requeueing.");

                                    return;
                                }


                                entityAnalysisModel = value;
                                break;
                            }

                            channel.BasicAck(ea.DeliveryTag, false);

                            if (entityAnalysisModel is not { Started: true })
                            {
                                context.Services.Log.Info(
                                    $"AMQP Inbound: Model not found or not started, rejecting message for GUID {entityAnalysisModelGuid}.");

                                return;
                            }

                            try
                            {
                                using var inputStream = new MemoryStream(ea.Body.ToArray());

                                await EntityAnalysisModelInvoke.InvokeAsync(entityAnalysisModel, inputStream)
                                    .ConfigureAwait(false);

                                context.Services.Log.Info(
                                    $"AMQP Inbound: Successfully processed message for GUID {entityAnalysisModelGuid}.");
                            }
                            catch (Exception ex) when (ex is not OperationCanceledException)
                            {
                                context.Services.Log.Error(
                                    $"AMQP Inbound: Error during processing message for GUID {entityAnalysisModelGuid}: {ex}");
                            }
                        }
                        finally
                        {
                            Interlocked.Decrement(ref inFlight);
                        }
                    });
                };

                consumerTag = channel.BasicConsume(
                    "jubeInbound",
                    false,
                    consumer
                );

                _ = context.Services.TaskCoordinator.CancellationToken.Register(() =>
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            context.Services.Log.Info("Cancellation requested: stopping RabbitMQ consumer...");
                            channel.BasicCancel(consumerTag);

                            while (Volatile.Read(ref inFlight) > 0)
                            {
                                await Task.Delay(100, CancellationToken.None).ConfigureAwait(false);
                            }

                            context.Services.RabbitMqConnection.Close();
                            context.Services.Log.Info(
                                "RabbitMQ channel closed safely after in-flight messages completed.");
                        }
                        catch (Exception ex)
                        {
                            context.Services.Log.Error("Error during RabbitMQ teardown: " + ex);
                        }
                    }, context.Services.TaskCoordinator.CancellationToken);
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
    }
}