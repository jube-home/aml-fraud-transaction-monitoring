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

namespace Jube.Data.Query.GetEntityAnalysisAsynchronousQueueBalanceStatisticsQuery
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using Helpers;
    using LinqToDB;
    using Poco;
    using Dto.Payload;

    public class GetEntityAnalysisAsynchronousQueueBalanceStatisticsQuery(DbContext dbContext)
    {
        public async Task<PayloadStatistics> ExecuteAsync(DateTime? from, DateTime? to, double? samplePercentage,
            int statisticsCap, CancellationToken token = default)
        {
            var query = BuildFilteredQuery(from, to, samplePercentage)
                .OrderByDescending(o => o.Id)
                .Take(statisticsCap);

            var rows = await query.ToListAsync(token).ConfigureAwait(false);

            var caseCreation = rows.Select(s => (double)s.CaseCreation.GetValueOrDefault())
                .ToArray();

            var tagging = rows.Select(s => (double)s.Tagging.GetValueOrDefault())
                .ToArray();

            var notification = rows.Select(s => (double)s.Notification.GetValueOrDefault())
                .ToArray();

            var webhook = rows.Select(s => (double)s.Webhook.GetValueOrDefault())
                .ToArray();

            var asynchronousEntityInvoke = rows.Select(_ => 0d).ToArray();

            return SummaryStatistics.Build(new Dictionary<string, double[]>
            {
                ["caseCreation"] = caseCreation,
                ["tagging"] = tagging,
                ["notification"] = notification,
                ["webhook"] = webhook,
                ["asynchronousEntityInvoke"] = asynchronousEntityInvoke
            });
        }

        private IQueryable<EntityAnalysisAsynchronousQueueBalance> BuildFilteredQuery(DateTime? from, DateTime? to,
            double? samplePercentage)
        {
            var query = dbContext.EntityAnalysisAsynchronousQueueBalance.AsQueryable();

            if (from.HasValue)
            {
                query = query.Where(w => w.CreatedDate >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(w => w.CreatedDate <= to.Value);
            }

            if (samplePercentage.HasValue)
            {
                query = query.Where(w => RandomSample.Predicate(samplePercentage.Value));
            }

            return query;
        }
    }
}