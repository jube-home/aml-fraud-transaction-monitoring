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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Engine.InboundQueue;
using Jube.Test.Infrastructure;
using RabbitMQ.Client;
using Xunit;

namespace Jube.Test.InboundQueue
{
    [Trait("Category", "Service")]
    public sealed class AmqpInboundConsumerTests : IDisposable
    {
        private static readonly Guid model = Guid.NewGuid();
        private readonly string queue = "jube-test-" + Guid.NewGuid().ToString("N");
        private readonly List<IConnection> connections = [];
        private readonly CancellationTokenSource cancellation = new();

        public void Dispose()
        {
            cancellation.Cancel();

            try
            {
                using var connection = Connect();
                using var channel = connection.CreateModel();
                channel.QueueDelete(queue);
            }
            catch (Exception)
            {
            }

            foreach (var connection in connections)
            {
                try
                {
                    connection.Dispose();
                }
                catch (Exception)
                {
                }
            }
        }

        private IConnection Connect()
        {
            var connection = new ConnectionFactory
                { Uri = new Uri(Environment.GetEnvironmentVariable(BrokerFactAttribute.Variable)!) }.CreateConnection();
            connections.Add(connection);

            return connection;
        }

        private AmqpInboundConsumer Start(Func<Guid, byte[], CancellationToken, Task<bool>> invoke,
            Func<bool>? loaded = null, ushort prefetch = 50, InboundModelState state = InboundModelState.Ready,
            IConnection? connection = null)
        {
            var channel = (connection ?? Connect()).CreateModel();
            var consumer = new AmqpInboundConsumer(channel, TestLog.NoOp, queue, false, prefetch, loaded ?? (() => true),
                _ => state, invoke, cancellation.Token, 100);
            consumer.Start();

            return consumer;
        }

        private void Publish(int count, bool header = true)
        {
            using var channel = Connect().CreateModel();
            channel.QueueDeclare(queue, false, false, false, null);

            for (var i = 0; i < count; i++)
            {
                var properties = channel.CreateBasicProperties();

                if (header)
                {
                    properties.Headers = new Dictionary<string, object>
                        { [AmqpInboundConsumer.ModelHeader] = Encoding.UTF8.GetBytes(model.ToString()) };
                }

                channel.BasicPublish("", queue, properties, Encoding.UTF8.GetBytes($"{{\"n\":{i}}}"));
            }
        }

        private (uint Messages, uint Consumers) Depth()
        {
            using var channel = Connect().CreateModel();
            var declared = channel.QueueDeclarePassive(queue);

            return (declared.MessageCount, declared.ConsumerCount);
        }

        private static async Task<bool> UntilAsync(Func<bool> condition, int seconds = 10)
        {
            var deadline = DateTime.UtcNow.AddSeconds(seconds);

            while (DateTime.UtcNow < deadline)
            {
                if (condition())
                {
                    return true;
                }

                await Task.Delay(50);
            }

            return condition();
        }

        [BrokerFact]
        public async Task EveryMessageIsAcknowledgedOnlyAfterItWasProcessedAsync()
        {
            var seen = new ConcurrentBag<string>();
            Publish(5);

            Start((_, bytes, _) =>
            {
                seen.Add(Encoding.UTF8.GetString(bytes));

                return Task.FromResult(true);
            });

            (await UntilAsync(() => seen.Count == 5)).Should().BeTrue();
            (await UntilAsync(() => Depth().Messages == 0)).Should().BeTrue();
        }

        [BrokerFact]
        public async Task AMessageBeingProcessedWhenTheNodeDiesIsDeliveredAgainAsync()
        {
            var started = new TaskCompletionSource();
            Publish(1);
            var dying = Connect();

            Start(async (_, _, token) =>
            {
                started.TrySetResult();
                await Task.Delay(Timeout.Infinite, token);

                return true;
            }, connection: dying);

            await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
            dying.Abort();

            var redelivered = new TaskCompletionSource<bool>();

            using var channel = Connect().CreateModel();
            var consumer = new RabbitMQ.Client.Events.EventingBasicConsumer(channel);
            consumer.Received += (_, ea) =>
            {
                redelivered.TrySetResult(ea.Redelivered);
                channel.BasicAck(ea.DeliveryTag, false);
            };
            channel.BasicConsume(queue, false, consumer);

            (await redelivered.Task.WaitAsync(TimeSpan.FromSeconds(10))).Should().BeTrue();
        }

        [BrokerFact]
        public async Task AFailedMessageIsTriedOnceMoreThenDroppedNotLoopedForeverAsync()
        {
            var calls = 0;
            Publish(1);

            Start((_, _, _) =>
            {
                Interlocked.Increment(ref calls);

                return Task.FromResult(false);
            });

            (await UntilAsync(() => Depth().Messages == 0 && Volatile.Read(ref calls) >= 2)).Should().BeTrue();
            await Task.Delay(500);
            Volatile.Read(ref calls).Should().Be(2);
            Depth().Messages.Should().Be(0);
        }

        [BrokerFact]
        public async Task MessagesArrivingBeforeTheModelsHaveLoadedAreKeptNotLostAsync()
        {
            var ready = false;
            var seen = new ConcurrentBag<string>();
            Publish(3);

            Start((_, bytes, _) =>
            {
                seen.Add(Encoding.UTF8.GetString(bytes));

                return Task.FromResult(true);
            }, loaded: () => Volatile.Read(ref ready));

            await Task.Delay(600);
            seen.Should().BeEmpty();

            Volatile.Write(ref ready, true);

            (await UntilAsync(() => seen.Count == 3)).Should().BeTrue();
            seen.Distinct().Should().HaveCount(3);
            (await UntilAsync(() => Depth().Messages == 0)).Should().BeTrue();
        }

        [BrokerFact]
        public async Task AtMostThePrefetchIsHeldUnacknowledgedAtOnceAsync()
        {
            var running = 0;
            var peak = 0;
            var release = new TaskCompletionSource();
            Publish(20);

            Start(async (_, _, _) =>
            {
                var now = Interlocked.Increment(ref running);

                int seenPeak;

                while (now > (seenPeak = Volatile.Read(ref peak)))
                {
                    Interlocked.CompareExchange(ref peak, now, seenPeak);
                }

                await release.Task;
                Interlocked.Decrement(ref running);

                return true;
            }, prefetch: 3);

            (await UntilAsync(() => Volatile.Read(ref running) == 3)).Should().BeTrue();
            await Task.Delay(400);
            Volatile.Read(ref peak).Should().Be(3);
            Depth().Messages.Should().Be(17);

            release.SetResult();

            (await UntilAsync(() => Depth().Messages == 0)).Should().BeTrue();
            Volatile.Read(ref peak).Should().Be(3);
        }

        [BrokerFact]
        public async Task AMessageWithoutAUsableModelHeaderIsDroppedAsync()
        {
            var calls = 0;
            Publish(2, header: false);

            Start((_, _, _) =>
            {
                Interlocked.Increment(ref calls);

                return Task.FromResult(true);
            });

            (await UntilAsync(() => Depth().Messages == 0)).Should().BeTrue();
            Volatile.Read(ref calls).Should().Be(0);
        }

        [BrokerFact]
        public async Task AnUnknownModelIsRetriedOnceThenDroppedAsync()
        {
            var calls = 0;
            Publish(1);

            Start((_, _, _) =>
            {
                Interlocked.Increment(ref calls);

                return Task.FromResult(true);
            }, state: InboundModelState.Unknown);

            (await UntilAsync(() => Depth().Messages == 0)).Should().BeTrue();
            Volatile.Read(ref calls).Should().Be(0);
        }

        [BrokerFact]
        public async Task StoppingWaitsForTheMessagesInFlightAndStopsDeliveryAsync()
        {
            var release = new TaskCompletionSource();
            var started = new TaskCompletionSource();
            Publish(1);

            var consumer = Start(async (_, _, _) =>
            {
                started.TrySetResult();
                await release.Task;

                return true;
            });

            await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var stopping = consumer.StopAsync();
            await Task.Delay(300);
            stopping.IsCompleted.Should().BeFalse();

            release.SetResult();
            await stopping.WaitAsync(TimeSpan.FromSeconds(10));

            (await UntilAsync(() => Depth().Messages == 0)).Should().BeTrue();
            Depth().Consumers.Should().Be(0);
        }
    }
}
