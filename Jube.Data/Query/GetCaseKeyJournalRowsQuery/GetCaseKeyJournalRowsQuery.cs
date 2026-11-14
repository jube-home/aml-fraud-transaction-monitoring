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

namespace Jube.Data.Query.GetCaseKeyJournalRowsQuery
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using ArchiveKeyLookup;
    using log4net;
    using Reporting;

    public class GetCaseKeyJournalRowsQuery(string connectionString, ILog log)
    {
        public static string FilterPredicate(Func<List<object>, string> filter, List<object> tokens)
        {
            return filter is null ? string.Empty : $"and {filter(tokens)} ";
        }

        public static string WindowPredicate(List<object> tokens, DateTime from, DateTime to,
            double responseElevation, int activationRuleCount)
        {
            var next = tokens.Count + 1;
            tokens.AddRange([from, to, responseElevation, activationRuleCount]);

            return $"and a.\"CreatedDate\" >= (@{next}) and a.\"CreatedDate\" <= (@{next + 1}) " +
                   $"and a.\"ResponseElevation\" >= (@{next + 2}) " +
                   $"and a.\"ActivationRuleCount\" >= (@{next + 3}) ";
        }

        public static string Statement(int entityAnalysisModelId, string user, ArchiveKeyPredicate key,
            DateTime from, DateTime to, double responseElevation, int activationRuleCount, int limit,
            List<object> tokens, Func<List<object>, string> filter = null)
        {
            tokens.AddRange([entityAnalysisModelId, user]);
            var sql = ArchiveKeyLookupSql.Select + ArchiveKeyLookupSql.Scope(key, tokens) + FilterPredicate(filter, tokens) +
                      WindowPredicate(tokens, from, to, responseElevation, activationRuleCount);

            tokens.Add(limit);

            return sql + $"order by a.\"Id\" desc limit (@{tokens.Count})";
        }

        public static GetCaseKeyJournalRowsQueryDto Map(IDictionary<string, object> record)
        {
            return new GetCaseKeyJournalRowsQueryDto
            {
                Id = Convert.ToInt64(record["Id"], CultureInfo.InvariantCulture),
                EntityAnalysisModelInstanceEntryGuid = (Guid)record["EntityAnalysisModelInstanceEntryGuid"],
                ReferenceDate = record["ReferenceDate"] as DateTime?,
                CreatedDate = record["CreatedDate"] as DateTime?,
                ResponseElevation = record["ResponseElevation"] is DBNull
                    ? 0
                    : Convert.ToDouble(record["ResponseElevation"], CultureInfo.InvariantCulture),
                EntryKeyValue = record["EntryKeyValue"] as string,
                ActivationRuleCount = record["ActivationRuleCount"] is DBNull
                    ? 0
                    : Convert.ToInt32(record["ActivationRuleCount"], CultureInfo.InvariantCulture),
                Json = record["Json"]?.ToString()
            };
        }

        public async Task<List<GetCaseKeyJournalRowsQueryDto>> ExecuteAsync(int entityAnalysisModelId, string user,
            ArchiveKeyPredicate key, DateTime from, DateTime to, double responseElevation, int activationRuleCount,
            int limit, CancellationToken token = default, Func<List<object>, string> filter = null)
        {
            var tokens = new List<object>();
            var sql = Statement(entityAnalysisModelId, user, key, from, to, responseElevation, activationRuleCount,
                limit, tokens, filter);

            using var postgres = new Postgres(connectionString, log);

            await postgres.PrepareAsync(sql, tokens, token).ConfigureAwait(false);

            var records = await postgres.ExecuteByOrderedParametersAsync(sql, tokens, token).ConfigureAwait(false);

            return records.Select(Map).ToList();
        }
    }
}
