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
    public class EtcdMemberStatusRepository(DbContext dbContext)
    {
        public async Task<EtcdMemberStatus> InsertAsync(EtcdMemberStatus model, CancellationToken token = default)
        {
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);
            return model;
        }

        public async Task<IEnumerable<EtcdMemberStatus>> GetLastAsync(int take, DateTime? from, DateTime? to,
            string search, double? samplePercentage, string sortField, string sortDirection,
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
                ["leaderChangesTotal"] = rows.Where(w => w.LeaderChangesTotal.HasValue)
                    .Select(s => (double)s.LeaderChangesTotal!.Value).ToArray(),
                ["dbSizeBytes"] = rows.Where(w => w.DbSizeBytes.HasValue)
                    .Select(s => (double)s.DbSizeBytes!.Value).ToArray(),
                ["dbSizeInUseBytes"] = rows.Where(w => w.DbSizeInUseBytes.HasValue)
                    .Select(s => (double)s.DbSizeInUseBytes!.Value).ToArray(),
                ["proposalsCommittedTotal"] = rows.Where(w => w.ProposalsCommittedTotal.HasValue)
                    .Select(s => (double)s.ProposalsCommittedTotal!.Value).ToArray(),
                ["proposalsAppliedTotal"] = rows.Where(w => w.ProposalsAppliedTotal.HasValue)
                    .Select(s => (double)s.ProposalsAppliedTotal!.Value).ToArray(),
                ["proposalsPendingCount"] = rows.Where(w => w.ProposalsPendingCount.HasValue)
                    .Select(s => (double)s.ProposalsPendingCount!.Value).ToArray(),
                ["proposalsFailedTotal"] = rows.Where(w => w.ProposalsFailedTotal.HasValue)
                    .Select(s => (double)s.ProposalsFailedTotal!.Value).ToArray(),
                ["walFsyncAvgMicroseconds"] = rows.Where(w => w.WalFsyncAvgMicroseconds.HasValue)
                    .Select(s => s.WalFsyncAvgMicroseconds!.Value).ToArray(),
                ["backendCommitAvgMicroseconds"] = rows.Where(w => w.BackendCommitAvgMicroseconds.HasValue)
                    .Select(s => s.BackendCommitAvgMicroseconds!.Value).ToArray(),
                ["slowApplyTotal"] = rows.Where(w => w.SlowApplyTotal.HasValue)
                    .Select(s => (double)s.SlowApplyTotal!.Value).ToArray(),
                ["slowReadIndexesTotal"] = rows.Where(w => w.SlowReadIndexesTotal.HasValue)
                    .Select(s => (double)s.SlowReadIndexesTotal!.Value).ToArray(),
                ["alarmCount"] = rows.Where(w => w.AlarmCount.HasValue)
                    .Select(s => (double)s.AlarmCount!.Value).ToArray()
            });
        }

        private IQueryable<EtcdMemberStatus> BuildFilteredQuery(DateTime? from, DateTime? to, string search,
            double? samplePercentage)
        {
            var query = dbContext.EtcdMemberStatus.AsQueryable();

            if (from.HasValue)
            {
                query = query.Where(w => w.OccurredDate >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(w => w.OccurredDate <= to.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(w =>
                    (w.Endpoint != null && w.Endpoint.ToLower().Contains(lowerSearch)) ||
                    (w.Name != null && w.Name.ToLower().Contains(lowerSearch)) ||
                    (w.HealthReason != null && w.HealthReason.ToLower().Contains(lowerSearch)) ||
                    (w.Alarms != null && w.Alarms.ToLower().Contains(lowerSearch)));
            }

            if (samplePercentage.HasValue)
            {
                query = query.Where(w => RandomSample.Predicate(samplePercentage.Value));
            }

            return query;
        }

        private static IOrderedQueryable<EtcdMemberStatus> ApplySort(IQueryable<EtcdMemberStatus> query,
            string sortField, string sortDirection)
        {
            var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
            return sortField switch
            {
                "endpoint" => query.OrderByField(o => o.Endpoint, descending),
                "memberId" => query.OrderByField(o => o.MemberId, descending),
                "name" => query.OrderByField(o => o.Name, descending),
                "version" => query.OrderByField(o => o.Version, descending),
                "clusterVersion" => query.OrderByField(o => o.ClusterVersion, descending),
                "healthOk" => query.OrderByField(o => o.HealthOk, descending),
                "healthReason" => query.OrderByField(o => o.HealthReason, descending),
                "leaderId" => query.OrderByField(o => o.LeaderId, descending),
                "isLeader" => query.OrderByField(o => o.IsLeader, descending),
                "hasLeader" => query.OrderByField(o => o.HasLeader, descending),
                "leaderChangesTotal" => query.OrderByField(o => o.LeaderChangesTotal, descending),
                "dbSizeBytes" => query.OrderByField(o => o.DbSizeBytes, descending),
                "dbSizeInUseBytes" => query.OrderByField(o => o.DbSizeInUseBytes, descending),
                "raftIndex" => query.OrderByField(o => o.RaftIndex, descending),
                "raftTerm" => query.OrderByField(o => o.RaftTerm, descending),
                "raftAppliedIndex" => query.OrderByField(o => o.RaftAppliedIndex, descending),
                "proposalsCommittedTotal" => query.OrderByField(o => o.ProposalsCommittedTotal, descending),
                "proposalsAppliedTotal" => query.OrderByField(o => o.ProposalsAppliedTotal, descending),
                "proposalsPendingCount" => query.OrderByField(o => o.ProposalsPendingCount, descending),
                "proposalsFailedTotal" => query.OrderByField(o => o.ProposalsFailedTotal, descending),
                "walFsyncAvgMicroseconds" => query.OrderByField(o => o.WalFsyncAvgMicroseconds, descending),
                "backendCommitAvgMicroseconds" => query.OrderByField(o => o.BackendCommitAvgMicroseconds,
                    descending),
                "slowApplyTotal" => query.OrderByField(o => o.SlowApplyTotal, descending),
                "slowReadIndexesTotal" => query.OrderByField(o => o.SlowReadIndexesTotal, descending),
                "alarmCount" => query.OrderByField(o => o.AlarmCount, descending),
                "alarms" => query.OrderByField(o => o.Alarms, descending),
                "createdDate" => query.OrderByField(o => o.CreatedDate, descending),
                "instance" => query.OrderByField(o => o.Instance, descending),
                "occurredDate" => query.OrderByField(o => o.OccurredDate, descending),
                _ => query.OrderByField(o => o.Id, true)
            };
        }
    }
}