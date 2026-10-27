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


using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Query.GetCaseKeyJournalPeerSampleQuery;
using Jube.Data.Query.GetCaseWorkflowXPathByCaseWorkflowIdQuery;
using Jube.Data.Repository;
using LinqToDB;
using log4net;
using Newtonsoft.Json;

namespace Jube.Service.Query.CaseKeyJournal
{
    public sealed class CaseKeyJournalPeerJob(string connectionString, ILog log, int windowDays, int maximumRows,
        TimeSpan refreshAfter)
    {
        public const int PageSize = 5000;
        public const int MaximumCaseKeysPerWorkflow = 10;

        public async Task<CaseKeyJournalPeerResult> RunOnceAsync(CancellationToken token = default)
        {
            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                connectionString, log);

            var workflows = await dbContext.CaseWorkflow
                .Where(w => w.Active == 1 && (w.Deleted == null || w.Deleted == 0) && w.EntityAnalysisModelId != null)
                .Select(w => new { w.Id, w.Guid, ModelId = w.EntityAnalysisModelId!.Value })
                .ToListAsync(token).ConfigureAwait(false);

            var rebuilt = 0;
            var fresh = 0;
            var distributions = 0;

            foreach (var workflow in workflows)
            {
                var caseKeys = await dbContext.Case
                    .Where(c => c.CaseWorkflowGuid == workflow.Guid && c.CaseKey != null)
                    .Select(c => c.CaseKey).Distinct().Take(MaximumCaseKeysPerWorkflow)
                    .ToListAsync(token).ConfigureAwait(false);

                foreach (var caseKey in caseKeys.OrderBy(key => key, StringComparer.Ordinal))
                {
                    token.ThrowIfCancellationRequested();

                    var built = await RebuildAsync(dbContext, workflow.Id, workflow.Guid, workflow.ModelId, caseKey,
                        token).ConfigureAwait(false);

                    if (built is null)
                    {
                        fresh++;
                    }
                    else
                    {
                        rebuilt++;
                        distributions += built.Value;
                    }
                }
            }

            return new CaseKeyJournalPeerResult(rebuilt, fresh, distributions);
        }

        public async Task<int?> RebuildAsync(DbContext dbContext, int workflowId, Guid workflow, int modelId,
            string caseKey, CancellationToken token = default)
        {
            var repository = new CaseKeyJournalPeerRepository(dbContext);
            var newest = await repository.NewestAsync(workflow, caseKey, token).ConfigureAwait(false);

            if (newest.HasValue && DateTime.UtcNow - newest.Value < refreshAfter)
            {
                return null;
            }

            var xPaths = await dbContext.CaseWorkflowXPath
                .Where(x => x.CaseWorkflowId == workflowId && (x.Deleted == null || x.Deleted == 0)
                                                              && x.Name != null && x.XPath != null)
                .ToListAsync(token).ConfigureAwait(false);

            var accumulator = new CaseKeyJournalPeerAccumulator(xPaths.Select(x =>
                new GetCaseWorkflowXPathByCaseWorkflowIdQueryDto
                {
                    Name = x.Name,
                    XPath = x.XPath,
                    Drill = x.Drill == 1,
                    RoleId = x.RoleId,
                    Segment = x.Segment == 1
                }).ToList());

            var from = DateTime.UtcNow.AddDays(-windowDays);
            var query = new GetCaseKeyJournalPeerSampleQuery(connectionString, log);
            var before = long.MaxValue;

            while (accumulator.Rows < maximumRows)
            {
                var size = Math.Min(PageSize, maximumRows - accumulator.Rows);
                var page = await query.ExecuteAsync(modelId, from, caseKey, before, size, token)
                    .ConfigureAwait(false);

                foreach (var row in page)
                {
                    accumulator.Add(row.KeyValue, row.Json);
                }

                if (page.Count < size)
                {
                    break;
                }

                before = page[^1].Id;
            }

            var utc = DateTime.UtcNow;
            var now = new DateTime(utc.Ticks - utc.Ticks % 10, DateTimeKind.Utc);
            var built = accumulator.Build();

            foreach (var peer in built)
            {
                await repository.UpsertAsync(new CaseKeyJournalPeer
                {
                    CaseWorkflowGuid = workflow,
                    CaseKey = caseKey,
                    Field = peer.Field,
                    Segment = peer.Segment,
                    KeyCount = peer.KeyCount,
                    RowCount = peer.RowCount,
                    Median = peer.Median,
                    Scale = peer.Scale,
                    Q1 = peer.Q1,
                    Q3 = peer.Q3,
                    Minimum = peer.Minimum,
                    Maximum = peer.Maximum,
                    Mean = peer.Mean,
                    Bins = JsonConvert.SerializeObject(peer.Bins),
                    WindowDays = windowDays,
                    ComputedDate = now
                }, token).ConfigureAwait(false);
            }

            await repository.MarkNotRebuiltDeletedAsync(workflow, caseKey, now, token).ConfigureAwait(false);

            return built.Count;
        }
    }
}
