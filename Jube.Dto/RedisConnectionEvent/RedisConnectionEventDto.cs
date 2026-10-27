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

namespace Jube.Dto.RedisConnectionEvent
{
    [GridEndpoint("RedisConnectionEvent", Title = "Redis connection event", DefaultSort = "occurredDate")]
    [GridFilter(Param = "eventTypeId", Label = "Event", Choices = ["1=ConnectionFailed", "2=ConnectionRestored", "3=ErrorMessage", "4=InternalError", "5=ConfigurationChanged", "6=ConfigurationChangedBroadcast", "7=ReconnectRetry"])]
    [GridFilter(Param = "connectionTypeId", Label = "Connection", Choices = ["0=None", "1=Interactive", "2=Subscription"])]
    [GridFilter(Param = "failureTypeId", Label = "Failure", Choices = ["0=None", "1=UnableToResolvePhysicalConnection", "2=SocketFailure", "3=AuthenticationFailure", "4=ProtocolFailure", "5=InternalFailure", "6=SocketClosed", "7=ConnectionDisposed", "8=Loading", "9=UnableToConnect", "10=ResponseIntegrityFailure"])]
    [GridSearch(Label = "Endpoint, message contains")]
    public sealed record RedisConnectionEventDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        [property: ListColumn(Hidden = true)]
        int Id,
        [property: Description("UTC timestamp the event was raised by the ConnectionMultiplexer.")]
        [property: ListColumn(Order = 10)]
        [property: GridTimeRange]
        DateTime OccurredDate,
        [property:
            Description(
                "Which event this row captures -- see EventTypeName for the human-readable form.")]
        [property: ListColumn(Hidden = true)]
        int EventTypeId,
        [property:
            Description(
                "Name of the event this row captures: 'ConnectionFailed', 'ConnectionRestored', 'ErrorMessage', 'InternalError', 'ConfigurationChanged' or 'ConfigurationChangedBroadcast' (raised by the ConnectionMultiplexer), or 'ReconnectRetry' (raised from inside StackExchange.Redis' own exponential-backoff decision point each time it actually retries the connection -- see RetryCount/BackoffMilliseconds/TransactionsImpacted). RedisConnectionMultiplexerMetric's per-minute counts of the first six event types (reset each sample) answer whether one tripped; this row is the detail behind it.")]
        [property: ListColumn(Order = 20, Title = "Event Type")]
        string EventTypeName,
        [property: Description("The Redis endpoint this event relates to.")]
        [property: ListColumn(Order = 30)]
        string EndPoint,
        [property:
            Description(
                "Which connection (Interactive/command, or Subscription/pub-sub) this event relates to -- see ConnectionTypeName for the human-readable form. Only populated for ConnectionFailed, ConnectionRestored and InternalError.")]
        [property: ListColumn(Hidden = true)]
        int? ConnectionTypeId,
        [property:
            Description(
                "Name of the connection kind (StackExchange.Redis' own ConnectionType enum, e.g. Interactive, Subscription). Only populated for ConnectionFailed, ConnectionRestored and InternalError.")]
        [property: ListColumn(Order = 40, Title = "Connection Type")]
        string? ConnectionTypeName,
        [property:
            Description(
                "The kind of failure StackExchange.Redis detected -- see FailureTypeName for the human-readable form. Only populated for ConnectionFailed and ConnectionRestored (on Restored, the failure type that has now cleared).")]
        [property: ListColumn(Hidden = true)]
        int? FailureTypeId,
        [property:
            Description(
                "Name of the failure kind (StackExchange.Redis' own ConnectionFailureType enum, e.g. SocketClosed, SocketFailure). Only populated for ConnectionFailed and ConnectionRestored (on Restored, the failure type that has now cleared).")]
        [property: ListColumn(Order = 50, Title = "Failure Type")]
        string? FailureTypeName,
        [property:
            Description(
                "Which internal operation the client library was performing when an InternalError occurred. Only populated for InternalError.")]
        [property: ListColumn(Order = 60)]
        string Origin,
        [property: Description("The raw error text Redis returned. Only populated for ErrorMessage.")]
        [property: ListColumn(Order = 70)]
        string Message,
        [property:
            Description(
                "The full exception detail, when the underlying event carried one. Populated for ConnectionFailed and InternalError, and occasionally ConnectionRestored.")]
        [property: ListColumn(Order = 80)]
        string Exception,
        [property: Description("UTC timestamp this row was flushed to the database.")]
        [property: ListColumn(Order = 130, Title = "Flushed Date")]
        DateTime CreatedDate,
        [property: Description("Hostname of the Jube node that captured this event.")]
        [property: ListColumn(Order = 120)]
        string Instance,
        [property:
            Description(
                "Which reconnect attempt this is (1-based) since the connection entered the connecting state. Only populated for ReconnectRetry.")]
        [property: ListColumn(Order = 90, Title = "Retry #", Format = GridFormat.Integer)]
        int? RetryCount,
        [property:
            Description(
                "How long, in milliseconds, StackExchange.Redis actually waited (the exponential-backoff interval) before making this retry. Only populated for ReconnectRetry.")]
        [property: ListColumn(Order = 100, Title = "Backoff", Unit = "ms", Format = GridFormat.Integer)]
        int? BackoffMilliseconds,
        [property:
            Description(
                "A snapshot of how many Jube.Cache Redis calls were awaiting completion at the moment this retry fired -- i.e. how many in-flight transactions this outage was stalling. Only populated for ReconnectRetry.")]
        [property: ListColumn(Order = 110, Title = "Txns Impacted", Format = GridFormat.Integer)]
        int? TransactionsImpacted);
}