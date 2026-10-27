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
    using System.Text;
    using System.Threading.Tasks;
    using Context;
    using EntityAnalysisModelInvoke.Models;
    using Helpers;
    using Newtonsoft.Json;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;

    public class WebhooksViaAmqpStarter(Context context)
    {
        private readonly IModel channel = context.Services.RabbitMqConnection.CreateModel();
        private EventingBasicConsumer consumer;

        public Task StartAsync()
        {
            try
            {
                channel.QueueDeclare("jubeWebhooks", false, false, false, null);

                if (!context.Services.DynamicEnvironment.AppSettings("EnableNotification")
                        .Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.CompletedTask;
                }

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug("Webhook Dispatch: Starting the Webhook Dispatch Routine.");
                }

                consumer = new EventingBasicConsumer(channel);

                consumer.Received += async (_, ea) =>
                {
                    if (context.Services.TaskCoordinator.CancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    try
                    {
                        var bodyString = Encoding.UTF8.GetString(ea.Body.ToArray());
                        var webhook = JsonConvert.DeserializeObject<Webhook>(bodyString);

                        await DispatchWebhookHelpers.DispatchWebhookAsync(context, webhook,
                            context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        context.Services.Log.Error($"Webhook Dispatch: Exception during async processing: {ex}");
                    }
                };

                var basicConsume = channel.BasicConsume("jubeWebhooks", true, consumer);

                context.Services.TaskCoordinator.CancellationToken.Register(() =>
                {
                    try
                    {
                        context.Services.Log.Info("Cancellation StartWebhooksViaAmqp.");

                        channel.BasicCancel(basicConsume);
                        channel.Close();

                        context.Services.Log.Info("Graceful Cancellation StartWebhooksViaAmqp.");
                    }
                    catch (Exception ex)
                    {
                        context.Services.Log.Error("Error during RabbitMQ teardown: " + ex);
                    }
                });
            }
            catch (OperationCanceledException ex)
            {
                context.Services.Log.Info($"Graceful Cancellation StartWebhooksViaAmqp: has produced an error {ex}");
            }
            catch (Exception ex)
            {
                context.Services.Log.Error($"StartWebhooksViaAmqp: has produced an error {ex}");
            }

            return Task.CompletedTask;
        }
    }
}
