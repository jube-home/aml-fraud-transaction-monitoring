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


namespace Jube.Data.Query.GetVisualisationRegistrySummaryQuery
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using Poco;

    public class GetVisualisationRegistrySummaryQuery(DbContext dbContext)
    {
        public async Task<List<GetVisualisationRegistrySummaryQueryDto>> ExecuteAsync(
            IReadOnlyCollection<int> registryIds, DateTime since, CancellationToken token = default)
        {
            if (registryIds.Count == 0)
            {
                return [];
            }

            var ids = registryIds.ToList();

            var datasources = await dbContext.VisualisationRegistryDatasource
                .Where(w => w.VisualisationRegistryId != null
                            && ids.Contains(w.VisualisationRegistryId.Value)
                            && w.Active == 1
                            && (w.Deleted == 0 || w.Deleted == null))
                .Select(s => new { RegistryId = s.VisualisationRegistryId.Value, s.DisplayKindId })
                .ToListAsync(token).ConfigureAwait(false);

            var parameters = await dbContext.VisualisationRegistryParameter
                .Where(w => w.VisualisationRegistryId != null
                            && ids.Contains(w.VisualisationRegistryId.Value)
                            && w.Active == 1
                            && (w.Deleted == 0 || w.Deleted == null))
                .Select(s => s.VisualisationRegistryId.Value)
                .ToListAsync(token).ConfigureAwait(false);

            var runs = await (from l in dbContext.GetTable<VisualisationRegistryDatasourceExecutionLog>()
                    join d in dbContext.VisualisationRegistryDatasource on l.VisualisationRegistryDatasourceId equals d.Id
                    where d.VisualisationRegistryId != null && ids.Contains(d.VisualisationRegistryId.Value)
                    group l by d.VisualisationRegistryId.Value
                    into g
                    select new
                    {
                        RegistryId = g.Key,
                        Last = g.Max(x => x.CreatedDate),
                        Executions = g.Count(x => x.CreatedDate >= since),
                        Failures = g.Count(x => x.CreatedDate >= since && x.Error != null && x.Error != ""),
                        Average = g.Average(x => x.CreatedDate >= since ? (double?)x.ResponseTime : null)
                    })
                .ToListAsync(token).ConfigureAwait(false);

            var lastUsers = new Dictionary<int, string>();

            foreach (var run in runs.Where(w => w.Last.HasValue))
            {
                var user = await (from l in dbContext.GetTable<VisualisationRegistryDatasourceExecutionLog>()
                        join d in dbContext.VisualisationRegistryDatasource on l.VisualisationRegistryDatasourceId equals d.Id
                        where d.VisualisationRegistryId == run.RegistryId
                        orderby l.CreatedDate descending, l.Id descending
                        select l.CreatedUser)
                    .FirstOrDefaultAsync(token).ConfigureAwait(false);

                if (user != null)
                {
                    lastUsers[run.RegistryId] = user;
                }
            }

            return ids.Select(id =>
            {
                var run = runs.FirstOrDefault(f => f.RegistryId == id);
                var own = datasources.Where(w => w.RegistryId == id).ToList();

                return new GetVisualisationRegistrySummaryQueryDto
                {
                    RegistryId = id,
                    DatasourceCount = own.Count,
                    ParameterCount = parameters.Count(c => c == id),
                    DisplayKindIds = own.Where(w => w.DisplayKindId.HasValue)
                        .Select(s => s.DisplayKindId.Value).Distinct().Order().ToArray(),
                    Executions = run?.Executions ?? 0,
                    Failures = run?.Failures ?? 0,
                    AverageResponseMilliseconds = run?.Average,
                    LastExecutionDate = run?.Last,
                    LastExecutionUser = lastUsers.GetValueOrDefault(id)
                };
            }).ToList();
        }
    }
}
