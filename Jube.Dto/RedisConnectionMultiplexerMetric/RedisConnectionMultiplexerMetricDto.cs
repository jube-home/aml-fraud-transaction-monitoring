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
using Jube.Dto.Forms;
using Jube.Dto.Options;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.RedisConnectionMultiplexerMetric
{
    [GridEndpoint("RedisConnectionMultiplexerMetric", Title = "Redis connection multiplexer metric", DefaultSort = "createdDate")]
    [GridSearch(Label = "Instance or client name contains")]
    public sealed record RedisConnectionMultiplexerMetricDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        [property: ListColumn(Hidden = true)]
        int Id,
        [property: Description("UTC timestamp this row's sample was taken.")]
        [property: ListColumn(Order = 10)]
        [property: GridTimeRange]
        DateTime CreatedDate,
        [property: Description("Hostname of the Jube node this sample was taken from.")]
        [property: ListColumn(Order = 20)]
        string Instance,
        [property: Description("The StackExchange.Redis client name this multiplexer identifies itself with.")]
        [property: ListColumn(Order = 30)]
        string ClientName,
        [property: Description("Configured synchronous operation timeout, in milliseconds.")]
        [property: ListColumn(Order = 40, Title = "Timeout", Unit = "ms", Format = GridFormat.Integer)]
        int TimeoutMilliseconds,
        [property: Description("Whether the multiplexer currently considers itself connected.")]
        [property: ListColumn(Order = 50)]
        bool IsConnected,
        [property: Description("Whether the multiplexer is currently in the process of (re)connecting.")]
        [property: ListColumn(Order = 60)]
        bool IsConnecting,
        [property: Description("Number of endpoints configured for this multiplexer.")]
        [property: ListColumn(Order = 70, Title = "End Points", Format = GridFormat.Integer)]
        int EndPointCount,
        [property: Description("Number of those endpoints currently connected.")]
        [property: ListColumn(Order = 80, Title = "Connected End Points", Format = GridFormat.Integer)]
        int ConnectedEndPointCount,
        [property: Description("Cumulative number of operations issued through this multiplexer since it was created.")]
        [property: ListColumn(Order = 90, Format = GridFormat.Integer)]
        long TotalOperationCount,
        [property: Description("Total outstanding (in-flight) operations across all connection types at sample time.")]
        [property: ListColumn(Order = 100, Format = GridFormat.Integer)]
        int TotalOutstanding,
        [property:
            Description(
                "Interactive (command) connection: items queued to be sent but not yet sent -- part of this connection's queue depth.")]
        [property: ListColumn(Order = 110, Title = "Interactive Pending Unsent", Format = GridFormat.Integer)]
        int InteractivePendingUnsentItems,
        [property:
            Description(
                "Interactive connection: items sent to Redis but still awaiting a response -- part of this connection's queue depth.")]
        [property: ListColumn(Order = 120, Title = "Interactive Sent Awaiting Response", Format = GridFormat.Integer)]
        int InteractiveSentItemsAwaitingResponse,
        [property:
            Description(
                "Interactive connection: responses received but still awaiting async completion on the client side -- part of this connection's queue depth.")]
        [property: ListColumn(Order = 130, Title = "Interactive Awaiting Completion", Format = GridFormat.Integer)]
        int InteractiveResponsesAwaitingAsyncCompletion,
        [property:
            Description(
                "Interactive connection: total outstanding operations (the sum the three queue-depth figures above roughly represent).")]
        [property: ListColumn(Order = 140, Format = GridFormat.Integer)]
        int InteractiveTotalOutstanding,
        [property: Description("Interactive connection: cumulative operations completed asynchronously.")]
        [property: ListColumn(Order = 150, Title = "Interactive Completed Async", Format = GridFormat.Integer)]
        long InteractiveCompletedAsynchronously,
        [property: Description("Interactive connection: cumulative operations completed synchronously.")]
        [property: ListColumn(Order = 160, Title = "Interactive Completed Sync", Format = GridFormat.Integer)]
        long InteractiveCompletedSynchronously,
        [property:
            Description(
                "Interactive connection: cumulative operations that failed asynchronously -- non-zero and climbing is a direct sign of trouble.")]
        [property: ListColumn(Order = 170, Title = "Interactive Failed Async", Format = GridFormat.Integer)]
        long InteractiveFailedAsynchronously,
        [property:
            Description(
                "Interactive connection: cumulative operations served by a non-preferred (e.g. replica-preferred-but-served-by-primary) endpoint.")]
        [property: ListColumn(Order = 180, Title = "Interactive Non-Preferred Endpoint Count", Format = GridFormat.Integer)]
        long InteractiveNonPreferredEndpointCount,
        [property: Description("Interactive connection: number of underlying sockets in use.")]
        [property: ListColumn(Order = 190, Format = GridFormat.Integer)]
        long InteractiveSocketCount,
        [property: Description("Interactive connection: number of writer tasks currently active.")]
        [property: ListColumn(Order = 200, Format = GridFormat.Integer)]
        int InteractiveWriterCount,
        [property:
            Description(
                "Subscription (pub/sub) connection: items queued to be sent but not yet sent -- part of this connection's queue depth.")]
        [property: ListColumn(Order = 210, Title = "Subscription Pending Unsent", Format = GridFormat.Integer)]
        int SubscriptionPendingUnsentItems,
        [property:
            Description(
                "Subscription connection: items sent but still awaiting a response -- part of this connection's queue depth.")]
        [property: ListColumn(Order = 220, Title = "Subscription Sent Awaiting Response", Format = GridFormat.Integer)]
        int SubscriptionSentItemsAwaitingResponse,
        [property:
            Description(
                "Subscription connection: responses received but still awaiting async completion -- part of this connection's queue depth.")]
        [property: ListColumn(Order = 230, Title = "Subscription Awaiting Completion", Format = GridFormat.Integer)]
        int SubscriptionResponsesAwaitingAsyncCompletion,
        [property: Description("Subscription connection: total outstanding operations.")]
        [property: ListColumn(Order = 240, Format = GridFormat.Integer)]
        int SubscriptionTotalOutstanding,
        [property: Description("Subscription connection: cumulative operations completed asynchronously.")]
        [property: ListColumn(Order = 250, Title = "Subscription Completed Async", Format = GridFormat.Integer)]
        long SubscriptionCompletedAsynchronously,
        [property: Description("Subscription connection: cumulative operations completed synchronously.")]
        [property: ListColumn(Order = 260, Title = "Subscription Completed Sync", Format = GridFormat.Integer)]
        long SubscriptionCompletedSynchronously,
        [property: Description("Subscription connection: cumulative operations that failed asynchronously.")]
        [property: ListColumn(Order = 270, Title = "Subscription Failed Async", Format = GridFormat.Integer)]
        long SubscriptionFailedAsynchronously,
        [property: Description("Subscription connection: number of underlying sockets in use.")]
        [property: ListColumn(Order = 280, Format = GridFormat.Integer)]
        long SubscriptionSocketCount,
        [property: Description("Number of channels/patterns currently subscribed to through this multiplexer.")]
        [property: ListColumn(Order = 290, Format = GridFormat.Integer)]
        long SubscriptionCount,
        [property:
            Description(
                "Count of ConnectionFailed events raised by this multiplexer since the previous one-minute sample (not cumulative -- reset to zero every sample) -- any non-zero value is a direct sign of instability reaching the Redis endpoint(s) in that minute.")]
        [property: ListColumn(Order = 300, Format = GridFormat.Integer)]
        long ConnectionFailedCount,
        [property:
            Description(
                "Count of ConnectionRestored events since the previous sample (not cumulative) -- paired with ConnectionFailedCount to see how often, and how quickly, connectivity recovers.")]
        [property: ListColumn(Order = 310, Format = GridFormat.Integer)]
        long ConnectionRestoredCount,
        [property:
            Description(
                "Count of ErrorMessage events since the previous sample (not cumulative), raised when the Redis server itself returns an error reply.")]
        [property: ListColumn(Order = 320, Format = GridFormat.Integer)]
        long ErrorMessageCount,
        [property:
            Description(
                "Count of InternalError events since the previous sample (not cumulative), raised on an unexpected fault inside the client library itself.")]
        [property: ListColumn(Order = 330, Format = GridFormat.Integer)]
        long InternalErrorCount,
        [property:
            Description(
                "Count of ConfigurationChanged events since the previous sample (not cumulative), raised when the multiplexer's view of server topology changes (e.g. a replica promotion).")]
        [property: ListColumn(Order = 340, Format = GridFormat.Integer)]
        long ConfigurationChangedCount,
        [property:
            Description(
                "Count of ConfigurationChangedBroadcast events since the previous sample (not cumulative) -- a server-pushed topology change notification, which is how a Sentinel-driven failover typically shows up here.")]
        [property: ListColumn(Order = 350, Format = GridFormat.Integer)]
        long ConfigurationChangedBroadcastCount);
}