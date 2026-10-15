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
using LinqToDB.Mapping;
using MessagePack;

namespace Jube.Data.Poco
{
    [Table]
    [MessagePackObject]
    public class RedisConnectionMultiplexerMetric
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(2)] public string Instance { get; set; }
        [Column] [Nullable] [Key(3)] public string ClientName { get; set; }
        [Column] [Nullable] [Key(4)] public int? TimeoutMilliseconds { get; set; }
        [Column] [Nullable] [Key(5)] public bool? IsConnected { get; set; }
        [Column] [Nullable] [Key(6)] public bool? IsConnecting { get; set; }
        [Column] [Nullable] [Key(7)] public int? EndPointCount { get; set; }
        [Column] [Nullable] [Key(8)] public int? ConnectedEndPointCount { get; set; }
        [Column] [Nullable] [Key(9)] public long? TotalOperationCount { get; set; }
        [Column] [Nullable] [Key(10)] public int? TotalOutstanding { get; set; }
        [Column] [Nullable] [Key(11)] public int? InteractivePendingUnsentItems { get; set; }
        [Column] [Nullable] [Key(12)] public int? InteractiveSentItemsAwaitingResponse { get; set; }
        [Column] [Nullable] [Key(13)] public int? InteractiveResponsesAwaitingAsyncCompletion { get; set; }
        [Column] [Nullable] [Key(14)] public int? InteractiveTotalOutstanding { get; set; }
        [Column] [Nullable] [Key(15)] public long? InteractiveCompletedAsynchronously { get; set; }
        [Column] [Nullable] [Key(16)] public long? InteractiveCompletedSynchronously { get; set; }
        [Column] [Nullable] [Key(17)] public long? InteractiveFailedAsynchronously { get; set; }
        [Column] [Nullable] [Key(18)] public long? InteractiveNonPreferredEndpointCount { get; set; }
        [Column] [Nullable] [Key(19)] public long? InteractiveSocketCount { get; set; }
        [Column] [Nullable] [Key(20)] public int? InteractiveWriterCount { get; set; }
        [Column] [Nullable] [Key(21)] public int? SubscriptionPendingUnsentItems { get; set; }
        [Column] [Nullable] [Key(22)] public int? SubscriptionSentItemsAwaitingResponse { get; set; }
        [Column] [Nullable] [Key(23)] public int? SubscriptionResponsesAwaitingAsyncCompletion { get; set; }
        [Column] [Nullable] [Key(24)] public int? SubscriptionTotalOutstanding { get; set; }
        [Column] [Nullable] [Key(25)] public long? SubscriptionCompletedAsynchronously { get; set; }
        [Column] [Nullable] [Key(26)] public long? SubscriptionCompletedSynchronously { get; set; }
        [Column] [Nullable] [Key(27)] public long? SubscriptionFailedAsynchronously { get; set; }
        [Column] [Nullable] [Key(28)] public long? SubscriptionSocketCount { get; set; }
        [Column] [Nullable] [Key(29)] public long? SubscriptionCount { get; set; }
        [Column] [Nullable] [Key(30)] public long? ConnectionFailedCount { get; set; }
        [Column] [Nullable] [Key(31)] public long? ConnectionRestoredCount { get; set; }
        [Column] [Nullable] [Key(32)] public long? ErrorMessageCount { get; set; }
        [Column] [Nullable] [Key(33)] public long? InternalErrorCount { get; set; }
        [Column] [Nullable] [Key(34)] public long? ConfigurationChangedCount { get; set; }
        [Column] [Nullable] [Key(35)] public long? ConfigurationChangedBroadcastCount { get; set; }
    }
}