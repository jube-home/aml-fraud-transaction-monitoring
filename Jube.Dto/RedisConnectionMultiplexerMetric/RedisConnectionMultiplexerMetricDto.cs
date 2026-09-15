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

using System.ComponentModel;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace Jube.Dto.RedisConnectionMultiplexerMetric
{
    public sealed record RedisConnectionMultiplexerMetricDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        int Id,
        [property: Description("UTC timestamp this row's sample was taken.")]
        DateTime CreatedDate,
        [property: Description("Hostname of the Jube node this sample was taken from.")]
        string Instance,
        [property: Description("The StackExchange.Redis client name this multiplexer identifies itself with.")]
        string ClientName,
        [property: Description("Configured synchronous operation timeout, in milliseconds.")]
        int TimeoutMilliseconds,
        [property: Description("Whether the multiplexer currently considers itself connected.")]
        bool IsConnected,
        [property: Description("Whether the multiplexer is currently in the process of (re)connecting.")]
        bool IsConnecting,
        [property: Description("Number of endpoints configured for this multiplexer.")]
        int EndPointCount,
        [property: Description("Number of those endpoints currently connected.")]
        int ConnectedEndPointCount,
        [property: Description("Cumulative number of operations issued through this multiplexer since it was created.")]
        long TotalOperationCount,
        [property: Description("Total outstanding (in-flight) operations across all connection types at sample time.")]
        int TotalOutstanding,
        [property:
            Description(
                "Interactive (command) connection: items queued to be sent but not yet sent -- part of this connection's queue depth.")]
        int InteractivePendingUnsentItems,
        [property:
            Description(
                "Interactive connection: items sent to Redis but still awaiting a response -- part of this connection's queue depth.")]
        int InteractiveSentItemsAwaitingResponse,
        [property:
            Description(
                "Interactive connection: responses received but still awaiting async completion on the client side -- part of this connection's queue depth.")]
        int InteractiveResponsesAwaitingAsyncCompletion,
        [property:
            Description(
                "Interactive connection: total outstanding operations (the sum the three queue-depth figures above roughly represent).")]
        int InteractiveTotalOutstanding,
        [property: Description("Interactive connection: cumulative operations completed asynchronously.")]
        long InteractiveCompletedAsynchronously,
        [property: Description("Interactive connection: cumulative operations completed synchronously.")]
        long InteractiveCompletedSynchronously,
        [property:
            Description(
                "Interactive connection: cumulative operations that failed asynchronously -- non-zero and climbing is a direct sign of trouble.")]
        long InteractiveFailedAsynchronously,
        [property:
            Description(
                "Interactive connection: cumulative operations served by a non-preferred (e.g. replica-preferred-but-served-by-primary) endpoint.")]
        long InteractiveNonPreferredEndpointCount,
        [property: Description("Interactive connection: number of underlying sockets in use.")]
        long InteractiveSocketCount,
        [property: Description("Interactive connection: number of writer tasks currently active.")]
        int InteractiveWriterCount,
        [property:
            Description(
                "Subscription (pub/sub) connection: items queued to be sent but not yet sent -- part of this connection's queue depth.")]
        int SubscriptionPendingUnsentItems,
        [property:
            Description(
                "Subscription connection: items sent but still awaiting a response -- part of this connection's queue depth.")]
        int SubscriptionSentItemsAwaitingResponse,
        [property:
            Description(
                "Subscription connection: responses received but still awaiting async completion -- part of this connection's queue depth.")]
        int SubscriptionResponsesAwaitingAsyncCompletion,
        [property: Description("Subscription connection: total outstanding operations.")]
        int SubscriptionTotalOutstanding,
        [property: Description("Subscription connection: cumulative operations completed asynchronously.")]
        long SubscriptionCompletedAsynchronously,
        [property: Description("Subscription connection: cumulative operations completed synchronously.")]
        long SubscriptionCompletedSynchronously,
        [property: Description("Subscription connection: cumulative operations that failed asynchronously.")]
        long SubscriptionFailedAsynchronously,
        [property: Description("Subscription connection: number of underlying sockets in use.")]
        long SubscriptionSocketCount,
        [property: Description("Number of channels/patterns currently subscribed to through this multiplexer.")]
        long SubscriptionCount,
        [property:
            Description(
                "Count of ConnectionFailed events raised by this multiplexer since the previous one-minute sample (not cumulative -- reset to zero every sample) -- any non-zero value is a direct sign of instability reaching the Redis endpoint(s) in that minute.")]
        long ConnectionFailedCount,
        [property:
            Description(
                "Count of ConnectionRestored events since the previous sample (not cumulative) -- paired with ConnectionFailedCount to see how often, and how quickly, connectivity recovers.")]
        long ConnectionRestoredCount,
        [property:
            Description(
                "Count of ErrorMessage events since the previous sample (not cumulative), raised when the Redis server itself returns an error reply.")]
        long ErrorMessageCount,
        [property:
            Description(
                "Count of InternalError events since the previous sample (not cumulative), raised on an unexpected fault inside the client library itself.")]
        long InternalErrorCount,
        [property:
            Description(
                "Count of ConfigurationChanged events since the previous sample (not cumulative), raised when the multiplexer's view of server topology changes (e.g. a replica promotion).")]
        long ConfigurationChangedCount,
        [property:
            Description(
                "Count of ConfigurationChangedBroadcast events since the previous sample (not cumulative) -- a server-pushed topology change notification, which is how a Sentinel-driven failover typically shows up here.")]
        long ConfigurationChangedBroadcastCount);
}