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
using LinqToDB.Data;

namespace Jube.Data.Repository
{
    public class HaProxyServerStatusRepository(DbContext dbContext)
    {
        public Task BulkCopyAsync(List<HaProxyServerStatus> models, CancellationToken token = default)
        {
            return dbContext.BulkCopyAsync(models, token);
        }

        public async Task<IEnumerable<HaProxyServerStatus>> GetLastAsync(int take, DateTime? from, DateTime? to,
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
                ["chkFail"] = rows.Where(w => w.ChkFail.HasValue).Select(s => (double)s.ChkFail!.Value).ToArray(),
                ["chkDown"] = rows.Where(w => w.ChkDown.HasValue).Select(s => (double)s.ChkDown!.Value).ToArray(),
                ["lastChg"] = rows.Where(w => w.LastChg.HasValue).Select(s => (double)s.LastChg!.Value).ToArray(),
                ["scur"] = rows.Where(w => w.Scur.HasValue).Select(s => (double)s.Scur!.Value).ToArray(),
                ["qcur"] = rows.Where(w => w.Qcur.HasValue).Select(s => (double)s.Qcur!.Value).ToArray(),
                ["weight"] = rows.Where(w => w.Weight.HasValue).Select(s => (double)s.Weight!.Value).ToArray(),
                ["hrsp2Xx"] = rows.Where(w => w.Hrsp2Xx.HasValue).Select(s => (double)s.Hrsp2Xx!.Value).ToArray(),
                ["hrsp5Xx"] = rows.Where(w => w.Hrsp5Xx.HasValue).Select(s => (double)s.Hrsp5Xx!.Value).ToArray()
            });
        }

        private IQueryable<HaProxyServerStatus> BuildFilteredQuery(DateTime? from, DateTime? to, string search,
            double? samplePercentage)
        {
            var query = dbContext.HaProxyServerStatus.AsQueryable();

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
                    (w.PxName != null && w.PxName.ToLower().Contains(lowerSearch)) ||
                    (w.SvName != null && w.SvName.ToLower().Contains(lowerSearch)) ||
                    (w.Status != null && w.Status.ToLower().Contains(lowerSearch)));
            }

            if (samplePercentage.HasValue)
            {
                query = query.Where(w => RandomSample.Predicate(samplePercentage.Value));
            }

            return query;
        }

        private static IOrderedQueryable<HaProxyServerStatus> ApplySort(IQueryable<HaProxyServerStatus> query,
            string sortField, string sortDirection)
        {
            var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
            return sortField switch
            {
                "pxName" => query.OrderByField(o => o.PxName, descending),
                "svName" => query.OrderByField(o => o.SvName, descending),
                "status" => query.OrderByField(o => o.Status, descending),
                "addr" => query.OrderByField(o => o.Addr, descending),
                "checkStatus" => query.OrderByField(o => o.CheckStatus, descending),
                "checkCode" => query.OrderByField(o => o.CheckCode, descending),
                "chkFail" => query.OrderByField(o => o.ChkFail, descending),
                "chkDown" => query.OrderByField(o => o.ChkDown, descending),
                "lastChg" => query.OrderByField(o => o.LastChg, descending),
                "scur" => query.OrderByField(o => o.Scur, descending),
                "qcur" => query.OrderByField(o => o.Qcur, descending),
                "weight" => query.OrderByField(o => o.Weight, descending),
                "act" => query.OrderByField(o => o.Act, descending),
                "bck" => query.OrderByField(o => o.Bck, descending),
                "hrsp2Xx" => query.OrderByField(o => o.Hrsp2Xx, descending),
                "hrsp5Xx" => query.OrderByField(o => o.Hrsp5Xx, descending),
                "mode" => query.OrderByField(o => o.Mode, descending),
                "occurredDate" => query.OrderByField(o => o.OccurredDate, descending),
                "createdDate" => query.OrderByField(o => o.CreatedDate, descending),
                "instance" => query.OrderByField(o => o.Instance, descending),
                _ => query.OrderByField(o => o.Id, true)
            };
        }
    }
}