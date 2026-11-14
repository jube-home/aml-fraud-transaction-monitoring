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

namespace Jube.Data.Query.GetCaseSuppressionValuesQuery
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

    public class GetCaseSuppressionValuesQuery(string connectionString, ILog log)
    {
        public const int RowCap = 5000;
        public const int ValueCap = 500;

        public async Task<List<GetCaseSuppressionValuesQueryDto>> ExecuteAsync(int entityAnalysisModelId, string user,
            ArchiveKeyPredicate lane, string overrideKey, ArchiveKeyValueColumn overrideColumn,
            CancellationToken token = default)
        {
            var tokens = new List<object> { entityAnalysisModelId, user };
            var scope = ArchiveKeyLookupSql.Scope(lane, tokens);

            tokens.Add(overrideKey);
            var overrideToken = tokens.Count;
            tokens.Add(RowCap);
            var rowCap = tokens.Count;
            tokens.Add(ValueCap);
            var valueCap = tokens.Count;

            var sql = "select v.\"Value\", count(*) as \"Count\", min(v.\"CreatedDate\") as \"First\", " +
                      "max(v.\"CreatedDate\") as \"Last\" from (" +
                      $"select {ArchiveKeyLookupSql.ValueText("o", overrideColumn)} as \"Value\", a.\"CreatedDate\" " +
                      scope.Replace(" where ",
                          " left join \"ArchiveKey\" o on o.\"EntityAnalysisModelInstanceEntryGuid\" = a.\"EntityAnalysisModelInstanceEntryGuid\" " +
                          $"and o.\"ProcessingTypeId\" = 1 and o.\"Key\" = (@{overrideToken}) where ", StringComparison.Ordinal) +
                      $"order by a.\"Id\" desc limit (@{rowCap})) v " +
                      "where v.\"Value\" is not null and v.\"Value\" <> '' " +
                      "group by v.\"Value\" order by count(*) desc, max(v.\"CreatedDate\") desc " +
                      $"limit (@{valueCap})";

            using var postgres = new Postgres(connectionString, log);

            await postgres.PrepareAsync(sql, tokens, token).ConfigureAwait(false);

            var records = await postgres.ExecuteByOrderedParametersAsync(sql, tokens, token).ConfigureAwait(false);

            return records.Select(record => new GetCaseSuppressionValuesQueryDto
            {
                Value = record["Value"]?.ToString(),
                Count = Convert.ToInt32(record["Count"], CultureInfo.InvariantCulture),
                First = record["First"] as DateTime?,
                Last = record["Last"] as DateTime?
            }).ToList();
        }
    }
}
