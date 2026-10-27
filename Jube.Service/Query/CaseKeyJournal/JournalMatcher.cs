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


using Jube.Data.Query.GetCaseKeyJournalMatchQuery;
using Jube.Data.Query.GetCaseKeyJournalRowsQuery;
using log4net;

namespace Jube.Service.Query.CaseKeyJournal
{
    public static class JournalMatcher
    {
        public const int PageSize = 5000;

        public static async Task<List<Guid>> GuidsAsync(string connectionString, ILog log, int modelId, string user,
            JournalMatchFilter filter, ISet<string> indexedKeys, int maximum, CancellationToken token)
        {
            var query = new GetCaseKeyJournalMatchQuery(connectionString, log);
            var seen = new HashSet<Guid>();
            var found = new List<Guid>();
            var lanes = new List<(string Key, string Value)> { (filter.Key, filter.Value) };

            lanes.AddRange(filter.Merge);

            foreach (var (key, value) in lanes)
            {
                var before = long.MaxValue;

                while (true)
                {
                    token.ThrowIfCancellationRequested();

                    var page = await query.ExecuteAsync(modelId, user, key, value, filter.From, filter.To,
                        filter.ResponseElevation, filter.ActivationsOnly ? 1 : 0, before, PageSize, token,
                        indexedKeys.Contains(key), filter.Predicate).ConfigureAwait(false);

                    foreach (var row in page)
                    {
                        if (seen.Add(row.EntityAnalysisModelInstanceEntryGuid))
                        {
                            found.Add(row.EntityAnalysisModelInstanceEntryGuid);
                        }
                    }

                    if (found.Count > maximum)
                    {
                        return found;
                    }

                    if (page.Count < PageSize)
                    {
                        break;
                    }

                    before = page[^1].Id;
                }
            }

            return found;
        }

        public static async Task<int> CountAsync(string connectionString, ILog log, int modelId, string user,
            JournalMatchFilter filter, ISet<string> indexedKeys, int maximum, CancellationToken token)
        {
            var query = new GetCaseKeyJournalMatchQuery(connectionString, log);
            var lanes = new List<(string Key, string Value)> { (filter.Key, filter.Value) };
            var total = 0;

            lanes.AddRange(filter.Merge);

            foreach (var (key, value) in lanes)
            {
                total += await query.CountAsync(modelId, user, key, value, filter.From, filter.To,
                    filter.ResponseElevation, filter.ActivationsOnly ? 1 : 0, maximum, token,
                    indexedKeys.Contains(key), filter.Predicate).ConfigureAwait(false);

                if (total >= maximum)
                {
                    return maximum;
                }
            }

            return total;
        }

        public static async IAsyncEnumerable<(GetCaseKeyJournalRowsQueryDto Record, string? Lane)> RowsAsync(
            string connectionString, ILog log, int modelId, string user, JournalMatchFilter filter,
            ISet<string> indexedKeys, bool withLanes,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token)
        {
            var query = new GetCaseKeyJournalMatchQuery(connectionString, log);
            var lanes = new List<(string Key, string Value)> { (filter.Key, filter.Value) };

            lanes.AddRange(filter.Merge);

            Dictionary<Guid, string>? letters = null;

            if (withLanes && filter.Merge.Count > 0)
            {
                letters = new Dictionary<Guid, string>();

                for (var index = 0; index < lanes.Count; index++)
                {
                    foreach (var guid in await GuidsOfLaneAsync(query, modelId, user, filter, lanes[index], indexedKeys,
                                 token).ConfigureAwait(false))
                    {
                        letters[guid] = letters.GetValueOrDefault(guid, string.Empty) + (char)('A' + index);
                    }
                }
            }

            var seen = new HashSet<Guid>();

            foreach (var (key, value) in lanes)
            {
                var before = long.MaxValue;

                while (true)
                {
                    token.ThrowIfCancellationRequested();

                    var page = await query.ExecuteRowsAsync(modelId, user, key, value, filter.From, filter.To,
                        filter.ResponseElevation, filter.ActivationsOnly ? 1 : 0, before, PageSize, token,
                        indexedKeys.Contains(key), filter.Predicate).ConfigureAwait(false);

                    foreach (var record in page)
                    {
                        if (seen.Add(record.EntityAnalysisModelInstanceEntryGuid))
                        {
                            yield return (record,
                                letters?.GetValueOrDefault(record.EntityAnalysisModelInstanceEntryGuid, string.Empty));
                        }
                    }

                    if (page.Count < PageSize)
                    {
                        break;
                    }

                    before = page[^1].Id;
                }
            }
        }

        private static async Task<List<Guid>> GuidsOfLaneAsync(GetCaseKeyJournalMatchQuery query, int modelId,
            string user, JournalMatchFilter filter, (string Key, string Value) lane, ISet<string> indexedKeys,
            CancellationToken token)
        {
            var guids = new List<Guid>();
            var before = long.MaxValue;

            while (true)
            {
                token.ThrowIfCancellationRequested();

                var page = await query.ExecuteAsync(modelId, user, lane.Key, lane.Value, filter.From, filter.To,
                    filter.ResponseElevation, filter.ActivationsOnly ? 1 : 0, before, PageSize, token,
                    indexedKeys.Contains(lane.Key), filter.Predicate).ConfigureAwait(false);

                guids.AddRange(page.Select(row => row.EntityAnalysisModelInstanceEntryGuid));

                if (page.Count < PageSize || guids.Count > CaseKeyJournalTagJobService.MaximumMatches * 4)
                {
                    return guids;
                }

                before = page[^1].Id;
            }
        }
    }
}
