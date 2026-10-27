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
    using GetCaseKeyJournalRowsQuery;
    using log4net;
    using Reporting;

    public class GetCaseKeyJournalMatchQuery(string connectionString, ILog log)
    {
        public async Task<List<GetCaseKeyJournalMatchQueryDto>> ExecuteAsync(int entityAnalysisModelId, string user,
            string key, string value, DateTime from, DateTime to, double responseElevation, int activationRuleCount,
            long beforeId, int pageSize, CancellationToken token = default, bool keyIndexed = false,
            Func<List<object>, string> filter = null)
        {
            var tokens = new List<object> { entityAnalysisModelId, user };
            var predicate = GetCaseKeyJournalRowsQuery.KeyPredicate(key, value, keyIndexed, tokens) +
                            GetCaseKeyJournalRowsQuery.FilterPredicate(filter, tokens);
            var next = tokens.Count + 1;

            tokens.AddRange([from, to, responseElevation, activationRuleCount, beforeId, pageSize]);

            var sql = "select a.\"Id\", a.\"EntityAnalysisModelInstanceEntryGuid\" " +
                      GetCaseKeyJournalRowsQuery.Scope + predicate +
                      $"and a.\"CreatedDate\" >= (@{next}) and a.\"CreatedDate\" <= (@{next + 1}) " +
                      $"and a.\"ResponseElevation\" >= (@{next + 2}) " +
                      $"and a.\"ActivationRuleCount\" >= (@{next + 3}) " +
                      $"and a.\"Id\" < (@{next + 4}) " +
                      "order by a.\"Id\" desc " +
                      $"limit (@{next + 5})";

            using var postgres = new Postgres(connectionString, log);

            await postgres.PrepareAsync(sql, tokens, token).ConfigureAwait(false);

            var records = await postgres.ExecuteByOrderedParametersAsync(sql, tokens, token).ConfigureAwait(false);

            return records.Select(record => new GetCaseKeyJournalMatchQueryDto
            {
                Id = Convert.ToInt64(record["Id"], CultureInfo.InvariantCulture),
                EntityAnalysisModelInstanceEntryGuid = (Guid)record["EntityAnalysisModelInstanceEntryGuid"]
            }).ToList();
        }

        public async Task<int> CountAsync(int entityAnalysisModelId, string user, string key, string value,
            DateTime from, DateTime to, double responseElevation, int activationRuleCount, int maximum,
            CancellationToken token = default, bool keyIndexed = false, Func<List<object>, string> filter = null)
        {
            var tokens = new List<object> { entityAnalysisModelId, user };
            var predicate = GetCaseKeyJournalRowsQuery.KeyPredicate(key, value, keyIndexed, tokens) +
                            GetCaseKeyJournalRowsQuery.FilterPredicate(filter, tokens);
            var next = tokens.Count + 1;

            tokens.AddRange([from, to, responseElevation, activationRuleCount, maximum]);

            var sql = "select count(*) as \"Count\" from (select 1 " +
                      GetCaseKeyJournalRowsQuery.Scope + predicate +
                      $"and a.\"CreatedDate\" >= (@{next}) and a.\"CreatedDate\" <= (@{next + 1}) " +
                      $"and a.\"ResponseElevation\" >= (@{next + 2}) " +
                      $"and a.\"ActivationRuleCount\" >= (@{next + 3}) " +
                      $"limit (@{next + 4})) counted";

            using var postgres = new Postgres(connectionString, log);

            await postgres.PrepareAsync(sql, tokens, token).ConfigureAwait(false);

            var records = await postgres.ExecuteByOrderedParametersAsync(sql, tokens, token).ConfigureAwait(false);

            return records.Count == 0 ? 0 : Convert.ToInt32(records[0]["Count"], CultureInfo.InvariantCulture);
        }

        public async Task<List<GetCaseKeyJournalRowsQueryDto>> ExecuteRowsAsync(int entityAnalysisModelId,
            string user, string key, string value, DateTime from, DateTime to, double responseElevation,
            int activationRuleCount, long beforeId, int pageSize, CancellationToken token = default,
            bool keyIndexed = false, Func<List<object>, string> filter = null)
        {
            var tokens = new List<object> { entityAnalysisModelId, user };
            var predicate = GetCaseKeyJournalRowsQuery.KeyPredicate(key, value, keyIndexed, tokens) +
                            GetCaseKeyJournalRowsQuery.FilterPredicate(filter, tokens);
            var next = tokens.Count + 1;

            tokens.AddRange([from, to, responseElevation, activationRuleCount, beforeId, pageSize]);

            var sql = GetCaseKeyJournalRowsQuery.SelectFrom + predicate +
                      $"and a.\"CreatedDate\" >= (@{next}) and a.\"CreatedDate\" <= (@{next + 1}) " +
                      $"and a.\"ResponseElevation\" >= (@{next + 2}) " +
                      $"and a.\"ActivationRuleCount\" >= (@{next + 3}) " +
                      $"and a.\"Id\" < (@{next + 4}) " +
                      "order by a.\"Id\" desc " +
                      $"limit (@{next + 5})";

            using var postgres = new Postgres(connectionString, log);

            await postgres.PrepareAsync(sql, tokens, token).ConfigureAwait(false);

            var records = await postgres.ExecuteByOrderedParametersAsync(sql, tokens, token).ConfigureAwait(false);

            return records.Select(GetCaseKeyJournalRowsQuery.Map).ToList();
        }
    }
}
