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

namespace Jube.Data.Query.GetCaseKeyJournalMatchQuery
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using ArchiveKeyLookup;
    using GetCaseKeyJournalRowsQuery;
    using log4net;
    using Reporting;

    public class GetCaseKeyJournalMatchQuery(string connectionString, ILog log)
    {
        public async Task<List<GetCaseKeyJournalMatchQueryDto>> ExecuteAsync(int entityAnalysisModelId, string user,
            ArchiveKeyPredicate key, DateTime from, DateTime to, double responseElevation, int activationRuleCount,
            long beforeId, int pageSize, CancellationToken token = default, Func<List<object>, string> filter = null)
        {
            var tokens = new List<object> { entityAnalysisModelId, user };
            var sql = "select a.\"Id\", a.\"EntityAnalysisModelInstanceEntryGuid\" " +
                      ArchiveKeyLookupSql.Scope(key, tokens) + GetCaseKeyJournalRowsQuery.FilterPredicate(filter, tokens) +
                      GetCaseKeyJournalRowsQuery.WindowPredicate(tokens, from, to, responseElevation,
                          activationRuleCount);

            tokens.Add(beforeId);
            sql += $"and a.\"Id\" < (@{tokens.Count}) ";
            tokens.Add(pageSize);
            sql += $"order by a.\"Id\" desc limit (@{tokens.Count})";

            using var postgres = new Postgres(connectionString, log);

            await postgres.PrepareAsync(sql, tokens, token).ConfigureAwait(false);

            var records = await postgres.ExecuteByOrderedParametersAsync(sql, tokens, token).ConfigureAwait(false);

            return records.Select(record => new GetCaseKeyJournalMatchQueryDto
            {
                Id = Convert.ToInt64(record["Id"], CultureInfo.InvariantCulture),
                EntityAnalysisModelInstanceEntryGuid = (Guid)record["EntityAnalysisModelInstanceEntryGuid"]
            }).ToList();
        }

        public static string CountStatement(int entityAnalysisModelId, string user, ArchiveKeyPredicate key,
            DateTime from, DateTime to, double responseElevation, int activationRuleCount, int maximum,
            List<object> tokens, Func<List<object>, string> filter = null)
        {
            tokens.AddRange([entityAnalysisModelId, user]);
            var sql = "select count(*) as \"Count\" from (select 1 " +
                      ArchiveKeyLookupSql.Scope(key, tokens) + GetCaseKeyJournalRowsQuery.FilterPredicate(filter, tokens) +
                      GetCaseKeyJournalRowsQuery.WindowPredicate(tokens, from, to, responseElevation,
                          activationRuleCount);

            tokens.Add(maximum);

            return sql + $"limit (@{tokens.Count})) counted";
        }

        public async Task<int> CountAsync(int entityAnalysisModelId, string user, ArchiveKeyPredicate key,
            DateTime from, DateTime to, double responseElevation, int activationRuleCount, int maximum,
            CancellationToken token = default, Func<List<object>, string> filter = null)
        {
            var tokens = new List<object>();
            var sql = CountStatement(entityAnalysisModelId, user, key, from, to, responseElevation,
                activationRuleCount, maximum, tokens, filter);

            using var postgres = new Postgres(connectionString, log);

            await postgres.PrepareAsync(sql, tokens, token).ConfigureAwait(false);

            var records = await postgres.ExecuteByOrderedParametersAsync(sql, tokens, token).ConfigureAwait(false);

            return records.Count == 0 ? 0 : Convert.ToInt32(records[0]["Count"], CultureInfo.InvariantCulture);
        }

        public async Task<List<GetCaseKeyJournalRowsQueryDto>> ExecuteRowsAsync(int entityAnalysisModelId,
            string user, ArchiveKeyPredicate key, DateTime from, DateTime to, double responseElevation,
            int activationRuleCount, long beforeId, int pageSize, CancellationToken token = default,
            Func<List<object>, string> filter = null)
        {
            var tokens = new List<object> { entityAnalysisModelId, user };
            var sql = ArchiveKeyLookupSql.Select + ArchiveKeyLookupSql.Scope(key, tokens) +
                      GetCaseKeyJournalRowsQuery.FilterPredicate(filter, tokens) +
                      GetCaseKeyJournalRowsQuery.WindowPredicate(tokens, from, to, responseElevation,
                          activationRuleCount);

            tokens.Add(beforeId);
            sql += $"and a.\"Id\" < (@{tokens.Count}) ";
            tokens.Add(pageSize);
            sql += $"order by a.\"Id\" desc limit (@{tokens.Count})";

            using var postgres = new Postgres(connectionString, log);

            await postgres.PrepareAsync(sql, tokens, token).ConfigureAwait(false);

            var records = await postgres.ExecuteByOrderedParametersAsync(sql, tokens, token).ConfigureAwait(false);

            return records.Select(GetCaseKeyJournalRowsQuery.Map).ToList();
        }
    }
}
