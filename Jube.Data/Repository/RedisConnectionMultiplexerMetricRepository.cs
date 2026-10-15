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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Helpers;
using Jube.Data.Poco;
using Jube.Dto.Payload;
using LinqToDB;

namespace Jube.Data.Repository
{
    public class RedisConnectionMultiplexerMetricRepository(DbContext dbContext)
    {
        public async Task<RedisConnectionMultiplexerMetric> InsertAsync(RedisConnectionMultiplexerMetric model,
            CancellationToken token = default)
        {
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);

            return model;
        }

        public async Task<IEnumerable<RedisConnectionMultiplexerMetric>> GetLastAsync(int take, DateTime? from,
            DateTime? to, string search, double? samplePercentage, string sortField, string sortDirection,
            CancellationToken token = default)
        {
            var query = BuildFilteredQuery(from, to, search, samplePercentage);

            return await ApplySort(query, sortField, sortDirection)
                .Take(take)
                .ToListAsync(token).ConfigureAwait(false);
        }

        public Task<int> CountAsync(DateTime? from, DateTime? to, string search, double? samplePercentage,
            CancellationToken token = default)
        {
            return BuildFilteredQuery(from, to, search, samplePercentage).CountAsync(token);
        }

        public async Task<PayloadStatistics> GetStatisticsAsync(DateTime? from, DateTime? to, string search,
            double? samplePercentage, int statisticsCap, CancellationToken token = default)
        {
            var rows = await BuildFilteredQuery(from, to, search, samplePercentage)
                .OrderByDescending(o => o.Id)
                .Take(statisticsCap)
                .ToListAsync(token).ConfigureAwait(false);

            return SummaryStatistics.Build(new Dictionary<string, double[]>
            {
                ["timeoutMilliseconds"] =
                [
                    .. rows.Where(w => w.TimeoutMilliseconds.HasValue)
                        .Select(s => (double)s.TimeoutMilliseconds!.Value)
                ],
                ["endPointCount"] =
                [
                    .. rows.Where(w => w.EndPointCount.HasValue)
                        .Select(s => (double)s.EndPointCount!.Value)
                ],
                ["connectedEndPointCount"] =
                [
                    .. rows.Where(w => w.ConnectedEndPointCount.HasValue)
                        .Select(s => (double)s.ConnectedEndPointCount!.Value)
                ],
                ["totalOperationCount"] =
                [
                    .. rows.Where(w => w.TotalOperationCount.HasValue)
                        .Select(s => (double)s.TotalOperationCount!.Value)
                ],
                ["totalOutstanding"] =
                [
                    .. rows.Where(w => w.TotalOutstanding.HasValue)
                        .Select(s => (double)s.TotalOutstanding!.Value)
                ],
                ["interactivePendingUnsentItems"] =
                [
                    .. rows.Where(w => w.InteractivePendingUnsentItems.HasValue)
                        .Select(s => (double)s.InteractivePendingUnsentItems!.Value)
                ],
                ["interactiveSentItemsAwaitingResponse"] =
                [
                    .. rows
                        .Where(w => w.InteractiveSentItemsAwaitingResponse.HasValue)
                        .Select(s => (double)s.InteractiveSentItemsAwaitingResponse!.Value)
                ],
                ["interactiveResponsesAwaitingAsyncCompletion"] =
                [
                    .. rows
                        .Where(w => w.InteractiveResponsesAwaitingAsyncCompletion.HasValue)
                        .Select(s => (double)s.InteractiveResponsesAwaitingAsyncCompletion!.Value)
                ],
                ["interactiveTotalOutstanding"] =
                [
                    .. rows.Where(w => w.InteractiveTotalOutstanding.HasValue)
                        .Select(s => (double)s.InteractiveTotalOutstanding!.Value)
                ],
                ["interactiveCompletedAsynchronously"] =
                [
                    .. rows
                        .Where(w => w.InteractiveCompletedAsynchronously.HasValue)
                        .Select(s => (double)s.InteractiveCompletedAsynchronously!.Value)
                ],
                ["interactiveCompletedSynchronously"] =
                [
                    .. rows
                        .Where(w => w.InteractiveCompletedSynchronously.HasValue)
                        .Select(s => (double)s.InteractiveCompletedSynchronously!.Value)
                ],
                ["interactiveFailedAsynchronously"] =
                [
                    .. rows.Where(w => w.InteractiveFailedAsynchronously.HasValue)
                        .Select(s => (double)s.InteractiveFailedAsynchronously!.Value)
                ],
                ["interactiveNonPreferredEndpointCount"] =
                [
                    .. rows
                        .Where(w => w.InteractiveNonPreferredEndpointCount.HasValue)
                        .Select(s => (double)s.InteractiveNonPreferredEndpointCount!.Value)
                ],
                ["interactiveSocketCount"] =
                [
                    .. rows.Where(w => w.InteractiveSocketCount.HasValue)
                        .Select(s => (double)s.InteractiveSocketCount!.Value)
                ],
                ["interactiveWriterCount"] =
                [
                    .. rows.Where(w => w.InteractiveWriterCount.HasValue)
                        .Select(s => (double)s.InteractiveWriterCount!.Value)
                ],
                ["subscriptionPendingUnsentItems"] =
                [
                    .. rows.Where(w => w.SubscriptionPendingUnsentItems.HasValue)
                        .Select(s => (double)s.SubscriptionPendingUnsentItems!.Value)
                ],
                ["subscriptionSentItemsAwaitingResponse"] =
                [
                    .. rows
                        .Where(w => w.SubscriptionSentItemsAwaitingResponse.HasValue)
                        .Select(s => (double)s.SubscriptionSentItemsAwaitingResponse!.Value)
                ],
                ["subscriptionResponsesAwaitingAsyncCompletion"] =
                [
                    .. rows
                        .Where(w => w.SubscriptionResponsesAwaitingAsyncCompletion.HasValue)
                        .Select(s => (double)s.SubscriptionResponsesAwaitingAsyncCompletion!.Value)
                ],
                ["subscriptionTotalOutstanding"] =
                [
                    .. rows.Where(w => w.SubscriptionTotalOutstanding.HasValue)
                        .Select(s => (double)s.SubscriptionTotalOutstanding!.Value)
                ],
                ["subscriptionCompletedAsynchronously"] =
                [
                    .. rows
                        .Where(w => w.SubscriptionCompletedAsynchronously.HasValue)
                        .Select(s => (double)s.SubscriptionCompletedAsynchronously!.Value)
                ],
                ["subscriptionCompletedSynchronously"] =
                [
                    .. rows
                        .Where(w => w.SubscriptionCompletedSynchronously.HasValue)
                        .Select(s => (double)s.SubscriptionCompletedSynchronously!.Value)
                ],
                ["subscriptionFailedAsynchronously"] =
                [
                    .. rows.Where(w => w.SubscriptionFailedAsynchronously.HasValue)
                        .Select(s => (double)s.SubscriptionFailedAsynchronously!.Value)
                ],
                ["subscriptionSocketCount"] =
                [
                    .. rows.Where(w => w.SubscriptionSocketCount.HasValue)
                        .Select(s => (double)s.SubscriptionSocketCount!.Value)
                ],
                ["subscriptionCount"] =
                [
                    .. rows.Where(w => w.SubscriptionCount.HasValue)
                        .Select(s => (double)s.SubscriptionCount!.Value)
                ],
                ["connectionFailedCount"] =
                [
                    .. rows.Where(w => w.ConnectionFailedCount.HasValue)
                        .Select(s => (double)s.ConnectionFailedCount!.Value)
                ],
                ["connectionRestoredCount"] =
                [
                    .. rows.Where(w => w.ConnectionRestoredCount.HasValue)
                        .Select(s => (double)s.ConnectionRestoredCount!.Value)
                ],
                ["errorMessageCount"] =
                [
                    .. rows.Where(w => w.ErrorMessageCount.HasValue)
                        .Select(s => (double)s.ErrorMessageCount!.Value)
                ],
                ["internalErrorCount"] =
                [
                    .. rows.Where(w => w.InternalErrorCount.HasValue)
                        .Select(s => (double)s.InternalErrorCount!.Value)
                ],
                ["configurationChangedCount"] =
                [
                    .. rows.Where(w => w.ConfigurationChangedCount.HasValue)
                        .Select(s => (double)s.ConfigurationChangedCount!.Value)
                ],
                ["configurationChangedBroadcastCount"] =
                [
                    .. rows
                        .Where(w => w.ConfigurationChangedBroadcastCount.HasValue)
                        .Select(s => (double)s.ConfigurationChangedBroadcastCount!.Value)
                ]
            });
        }

        private IQueryable<RedisConnectionMultiplexerMetric> BuildFilteredQuery(DateTime? from, DateTime? to,
            string search, double? samplePercentage)
        {
            var query = dbContext.RedisConnectionMultiplexerMetric.AsQueryable();

            if (from.HasValue)
            {
                query = query.Where(w => w.CreatedDate >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(w => w.CreatedDate <= to.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();

                query = query.Where(w =>
                    w.Instance.ToLower().Contains(lowerSearch) ||
                    (w.ClientName != null && w.ClientName.ToLower().Contains(lowerSearch)));
            }

            if (samplePercentage.HasValue)
            {
                query = query.Where(w => RandomSample.Predicate(samplePercentage.Value));
            }

            return query;
        }

        private static IOrderedQueryable<RedisConnectionMultiplexerMetric> ApplySort(
            IQueryable<RedisConnectionMultiplexerMetric> query, string sortField, string sortDirection)
        {
            var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

            return sortField switch
            {
                "instance" => query.OrderByField(o => o.Instance, descending),
                "clientName" => query.OrderByField(o => o.ClientName, descending),
                "timeoutMilliseconds" => query.OrderByField(o => o.TimeoutMilliseconds, descending),
                "isConnected" => query.OrderByField(o => o.IsConnected, descending),
                "isConnecting" => query.OrderByField(o => o.IsConnecting, descending),
                "endPointCount" => query.OrderByField(o => o.EndPointCount, descending),
                "connectedEndPointCount" => query.OrderByField(o => o.ConnectedEndPointCount, descending),
                "totalOperationCount" => query.OrderByField(o => o.TotalOperationCount, descending),
                "totalOutstanding" => query.OrderByField(o => o.TotalOutstanding, descending),
                "interactivePendingUnsentItems" => query.OrderByField(o => o.InteractivePendingUnsentItems,
                    descending),
                "interactiveSentItemsAwaitingResponse" => query.OrderByField(
                    o => o.InteractiveSentItemsAwaitingResponse, descending),
                "interactiveResponsesAwaitingAsyncCompletion" => query.OrderByField(
                    o => o.InteractiveResponsesAwaitingAsyncCompletion, descending),
                "interactiveTotalOutstanding" => query.OrderByField(o => o.InteractiveTotalOutstanding,
                    descending),
                "interactiveCompletedAsynchronously" => query.OrderByField(
                    o => o.InteractiveCompletedAsynchronously, descending),
                "interactiveCompletedSynchronously" => query.OrderByField(o => o.InteractiveCompletedSynchronously,
                    descending),
                "interactiveFailedAsynchronously" => query.OrderByField(o => o.InteractiveFailedAsynchronously,
                    descending),
                "interactiveNonPreferredEndpointCount" => query.OrderByField(
                    o => o.InteractiveNonPreferredEndpointCount, descending),
                "interactiveSocketCount" => query.OrderByField(o => o.InteractiveSocketCount, descending),
                "interactiveWriterCount" => query.OrderByField(o => o.InteractiveWriterCount, descending),
                "subscriptionPendingUnsentItems" => query.OrderByField(o => o.SubscriptionPendingUnsentItems,
                    descending),
                "subscriptionSentItemsAwaitingResponse" => query.OrderByField(
                    o => o.SubscriptionSentItemsAwaitingResponse, descending),
                "subscriptionResponsesAwaitingAsyncCompletion" => query.OrderByField(
                    o => o.SubscriptionResponsesAwaitingAsyncCompletion, descending),
                "subscriptionTotalOutstanding" => query.OrderByField(o => o.SubscriptionTotalOutstanding,
                    descending),
                "subscriptionCompletedAsynchronously" => query.OrderByField(
                    o => o.SubscriptionCompletedAsynchronously, descending),
                "subscriptionCompletedSynchronously" => query.OrderByField(
                    o => o.SubscriptionCompletedSynchronously, descending),
                "subscriptionFailedAsynchronously" => query.OrderByField(o => o.SubscriptionFailedAsynchronously,
                    descending),
                "subscriptionSocketCount" => query.OrderByField(o => o.SubscriptionSocketCount, descending),
                "subscriptionCount" => query.OrderByField(o => o.SubscriptionCount, descending),
                "connectionFailedCount" => query.OrderByField(o => o.ConnectionFailedCount, descending),
                "connectionRestoredCount" => query.OrderByField(o => o.ConnectionRestoredCount, descending),
                "errorMessageCount" => query.OrderByField(o => o.ErrorMessageCount, descending),
                "internalErrorCount" => query.OrderByField(o => o.InternalErrorCount, descending),
                "configurationChangedCount" => query.OrderByField(o => o.ConfigurationChangedCount, descending),
                "configurationChangedBroadcastCount" => query.OrderByField(
                    o => o.ConfigurationChangedBroadcastCount, descending),
                "createdDate" => query.OrderByField(o => o.CreatedDate, descending),
                _ => query.OrderByField(o => o.Id, true)
            };
        }
    }
}