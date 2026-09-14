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

using System.Linq;
using Jube.Cache;
using Jube.Data.Poco;
using StackExchange.Redis;

namespace Jube.Engine.BackgroundTasks.TaskStarters.Metrics
{
    public sealed class RedisConnectionMultiplexerMetricSampler
    {
        public RedisConnectionMultiplexerMetric Sample(IConnectionMultiplexer multiplexer,
            RedisConnectionDiagnosticsCounters diagnostics)
        {
            var counters = multiplexer.GetCounters();
            var endPoints = multiplexer.GetEndPoints();
            var connectedEndPointCount = endPoints.Count(endPoint => multiplexer.GetServer(endPoint).IsConnected);
            var diagnosticsSnapshot = diagnostics.TakeSnapshot();

            return new RedisConnectionMultiplexerMetric
            {
                ClientName = multiplexer.ClientName,
                TimeoutMilliseconds = multiplexer.TimeoutMilliseconds,
                IsConnected = multiplexer.IsConnected,
                IsConnecting = multiplexer.IsConnecting,
                EndPointCount = endPoints.Length,
                ConnectedEndPointCount = connectedEndPointCount,
                TotalOperationCount = multiplexer.OperationCount,
                TotalOutstanding = (int)counters.TotalOutstanding,
                InteractivePendingUnsentItems = counters.Interactive.PendingUnsentItems,
                InteractiveSentItemsAwaitingResponse = counters.Interactive.SentItemsAwaitingResponse,
                InteractiveResponsesAwaitingAsyncCompletion = counters.Interactive.ResponsesAwaitingAsyncCompletion,
                InteractiveTotalOutstanding = counters.Interactive.TotalOutstanding,
                InteractiveCompletedAsynchronously = counters.Interactive.CompletedAsynchronously,
                InteractiveCompletedSynchronously = counters.Interactive.CompletedSynchronously,
                InteractiveFailedAsynchronously = counters.Interactive.FailedAsynchronously,
                InteractiveNonPreferredEndpointCount = counters.Interactive.NonPreferredEndpointCount,
                InteractiveSocketCount = counters.Interactive.SocketCount,
                InteractiveWriterCount = counters.Interactive.WriterCount,
                SubscriptionPendingUnsentItems = counters.Subscription.PendingUnsentItems,
                SubscriptionSentItemsAwaitingResponse = counters.Subscription.SentItemsAwaitingResponse,
                SubscriptionResponsesAwaitingAsyncCompletion = counters.Subscription.ResponsesAwaitingAsyncCompletion,
                SubscriptionTotalOutstanding = counters.Subscription.TotalOutstanding,
                SubscriptionCompletedAsynchronously = counters.Subscription.CompletedAsynchronously,
                SubscriptionCompletedSynchronously = counters.Subscription.CompletedSynchronously,
                SubscriptionFailedAsynchronously = counters.Subscription.FailedAsynchronously,
                SubscriptionSocketCount = counters.Subscription.SocketCount,
                SubscriptionCount = counters.Subscription.Subscriptions,
                ConnectionFailedCount = diagnosticsSnapshot.ConnectionFailedCount,
                ConnectionRestoredCount = diagnosticsSnapshot.ConnectionRestoredCount,
                ErrorMessageCount = diagnosticsSnapshot.ErrorMessageCount,
                InternalErrorCount = diagnosticsSnapshot.InternalErrorCount,
                ConfigurationChangedCount = diagnosticsSnapshot.ConfigurationChangedCount,
                ConfigurationChangedBroadcastCount = diagnosticsSnapshot.ConfigurationChangedBroadcastCount
            };
        }
    }
}