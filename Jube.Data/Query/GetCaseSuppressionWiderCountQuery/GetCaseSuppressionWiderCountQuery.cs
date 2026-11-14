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

namespace Jube.Data.Query.GetCaseSuppressionWiderCountQuery
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using ArchiveKeyLookup;
    using log4net;
    using Reporting;

    public class GetCaseSuppressionWiderCountQuery(string connectionString, ILog log)
    {
        public const int Cap = 1000;

        public async Task<GetCaseSuppressionWiderCountQueryDto> ExecuteAsync(int entityAnalysisModelId, string user,
            ArchiveKeyPredicate overrideKey, string caseKeyName, ArchiveKeyValueColumn caseKeyColumn,
            CancellationToken token = default)
        {
            var tokens = new List<object> { entityAnalysisModelId, user };
            var scope = ArchiveKeyLookupSql.Scope(overrideKey, tokens);

            tokens.Add(caseKeyName);
            var caseKeyToken = tokens.Count;
            tokens.Add(Cap + 1);

            var sql = "select count(*) as \"Transactions\", count(distinct t.\"Key\") as \"Keys\" from (" +
                      $"select {ArchiveKeyLookupSql.ValueText("c", caseKeyColumn)} as \"Key\" " +
                      scope.Replace(" where ",
                          " left join \"ArchiveKey\" c on c.\"EntityAnalysisModelInstanceEntryGuid\" = a.\"EntityAnalysisModelInstanceEntryGuid\" " +
                          $"and c.\"ProcessingTypeId\" = 1 and c.\"Key\" = (@{caseKeyToken}) where ", StringComparison.Ordinal) +
                      $"order by a.\"Id\" desc limit (@{tokens.Count})) t";

            using var postgres = new Postgres(connectionString, log);

            await postgres.PrepareAsync(sql, tokens, token).ConfigureAwait(false);

            var records = await postgres.ExecuteByOrderedParametersAsync(sql, tokens, token).ConfigureAwait(false);
            var record = records[0];
            var transactions = Convert.ToInt32(record["Transactions"], CultureInfo.InvariantCulture);
            var keys = Convert.ToInt32(record["Keys"], CultureInfo.InvariantCulture);

            return new GetCaseSuppressionWiderCountQueryDto
            {
                Transactions = Math.Min(transactions, Cap),
                Keys = keys,
                Capped = transactions > Cap
            };
        }
    }
}
