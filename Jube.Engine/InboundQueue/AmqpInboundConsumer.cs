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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Jube.Engine.InboundQueue
{
    public sealed class AmqpInboundConsumer(
        IModel channel,
        ILog log,
        string queue,
        bool durable,
        ushort prefetch,
        Func<bool> modelsLoaded,
        Func<Guid, InboundModelState> modelState,
        Func<Guid, byte[], CancellationToken, Task<bool>> invoke,
        CancellationToken token,
        int notReadyDelayMilliseconds = 1000)
    {
        public const string ModelHeader = "EntityAnalysisModelGuid";
        private readonly Lock channelLock = new();
        private EventingBasicConsumer consumer;
        private string consumerTag;
        private int inFlight;

        public void Start()
        {
            lock (channelLock)
            {
                channel.QueueDeclare(queue, durable, false, false, null);
                channel.BasicQos(0, prefetch, false);

                consumer = new EventingBasicConsumer(channel);
                consumer.Received += (sender, ea) =>
                {
                    Interlocked.Increment(ref inFlight);
                    _ = Task.Run(() => HandleAsync(ea));
                };

                consumerTag = channel.BasicConsume(queue, false, consumer);
            }
        }

        public async Task StopAsync()
        {
            lock (channelLock)
            {
                if (consumerTag is not null && channel.IsOpen)
                {
                    channel.BasicCancel(consumerTag);
                }
            }

            while (Volatile.Read(ref inFlight) > 0)
            {
                await Task.Delay(50, CancellationToken.None).ConfigureAwait(false);
            }
        }

        public static Guid? ParseModel(IBasicProperties properties)
        {
            if (properties?.Headers is null || !properties.Headers.TryGetValue(ModelHeader, out var header))
            {
                return null;
            }

            var text = header switch
            {
                byte[] bytes => Encoding.UTF8.GetString(bytes),
                string s => s,
                _ => null
            };

            return Guid.TryParse(text, out var guid) ? guid : null;
        }

        private async Task HandleAsync(BasicDeliverEventArgs ea)
        {
            try
            {
                if (token.IsCancellationRequested)
                {
                    Settle(ea, Disposition.Requeue, "Cancellation requested before processing.");

                    return;
                }

                var guid = ParseModel(ea.BasicProperties);

                if (guid is null)
                {
                    Settle(ea, Disposition.Drop, "The EntityAnalysisModelGuid header is missing or not a Guid.");

                    return;
                }

                if (!modelsLoaded())
                {
                    await Task.Delay(notReadyDelayMilliseconds, CancellationToken.None).ConfigureAwait(false);
                    Settle(ea, Disposition.Requeue, "The models have not loaded yet.");

                    return;
                }

                var state = modelState(guid.Value);

                if (state != InboundModelState.Ready)
                {
                    Settle(ea, ea.Redelivered ? Disposition.Drop : Disposition.Requeue,
                        $"Model {guid} is {state}.");

                    return;
                }

                bool ok;

                try
                {
                    ok = await invoke(guid.Value, ea.Body.ToArray(), token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    Settle(ea, Disposition.Requeue, "Cancelled while processing.");

                    return;
                }
                catch (Exception ex)
                {
                    log.Error($"AMQP Inbound: processing for model {guid} failed.", ex);
                    Settle(ea, ea.Redelivered ? Disposition.Drop : Disposition.Requeue, "Processing failed.");

                    return;
                }

                Settle(ea, ok ? Disposition.Ack : ea.Redelivered ? Disposition.Drop : Disposition.Requeue,
                    ok ? null : "The model produced no response.");
            }
            catch (Exception ex)
            {
                log.Error("AMQP Inbound: settling a message failed; the broker will redeliver it.", ex);
            }
            finally
            {
                Interlocked.Decrement(ref inFlight);
            }
        }

        private void Settle(BasicDeliverEventArgs ea, Disposition disposition, string reason)
        {
            if (reason is not null)
            {
                log.Info($"AMQP Inbound: {disposition}: {reason}");
            }

            lock (channelLock)
            {
                if (!channel.IsOpen)
                {
                    return;
                }

                switch (disposition)
                {
                    case Disposition.Ack:
                        channel.BasicAck(ea.DeliveryTag, false);

                        break;
                    case Disposition.Requeue:
                        channel.BasicNack(ea.DeliveryTag, false, true);

                        break;
                    case Disposition.Drop:
                        channel.BasicNack(ea.DeliveryTag, false, false);

                        break;
                }
            }
        }

        private enum Disposition
        {
            Ack,
            Requeue,
            Drop
        }
    }
}
