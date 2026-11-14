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


namespace Jube.Data.Query.GetCaseKeyJournalPinnedRowsQuery
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

    public class GetCaseKeyJournalPinnedRowsQuery(string connectionString, ILog log)
    {
        private const string Select =
            "select a.\"Id\", a.\"EntityAnalysisModelInstanceEntryGuid\", a.\"ReferenceDate\", " +
            "a.\"CreatedDate\", a.\"ResponseElevation\", a.\"EntryKeyValue\", a.\"ActivationRuleCount\", " +
            "a.\"Json\" " +
            "from \"Archive\" a " +
            "inner join \"EntityAnalysisModel\" m on m.\"Id\" = a.\"EntityAnalysisModelId\" " +
            "where a.\"EntityAnalysisModelId\" = (@1) " +
            "and exists (select 1 from \"UserInTenant\" u " +
            "           where u.\"TenantRegistryId\" = m.\"TenantRegistryId\" and u.\"User\" = (@2)) " +
            "and a.\"EntityAnalysisModelInstanceEntryGuid\" in ";

        public async Task<List<GetCaseKeyJournalRowsQueryDto>> ExecuteAsync(int entityAnalysisModelId, string user,
            IReadOnlyList<Guid> transactions, CancellationToken token = default,
            Func<List<object>, string> filter = null)
        {
            if (transactions.Count == 0)
            {
                return [];
            }

            var tokens = new List<object> { entityAnalysisModelId, user };
            tokens.AddRange(transactions.Cast<object>());

            var placeholders = string.Join(", ", Enumerable.Range(3, transactions.Count).Select(i => $"(@{i})"));
            var sql = $"{Select}({placeholders}) {GetCaseKeyJournalRowsQuery.FilterPredicate(filter, tokens)}order by a.\"Id\" desc";

            using var postgres = new Postgres(connectionString, log);

            await postgres.PrepareAsync(sql, tokens, token).ConfigureAwait(false);

            var records = await postgres.ExecuteByOrderedParametersAsync(sql, tokens, token).ConfigureAwait(false);

            return records.Select(record => new GetCaseKeyJournalRowsQueryDto
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
            }).ToList();
        }
    }
}
