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


namespace Jube.Data.Query.GetCaseKeyJournalPeerSampleQuery
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using log4net;
    using Reporting;

    public class GetCaseKeyJournalPeerSampleQuery(string connectionString, ILog log)
    {
        private const string Sql =
            "select a.\"Id\", (a.\"Json\" -> 'payload' ->> (@3)) as \"KeyValue\", a.\"Json\" " +
            "from \"Archive\" a " +
            "where a.\"EntityAnalysisModelId\" = (@1) " +
            "and a.\"CreatedDate\" >= (@2) " +
            "and a.\"Id\" < (@4) " +
            "and jsonb_exists(a.\"Json\" -> 'payload', (@3)) " +
            "order by a.\"Id\" desc " +
            "limit (@5)";

        public async Task<List<GetCaseKeyJournalPeerSampleQueryDto>> ExecuteAsync(int entityAnalysisModelId,
            DateTime from, string key, long beforeId, int pageSize, CancellationToken token = default)
        {
            var tokens = new List<object> { entityAnalysisModelId, from, key, beforeId, pageSize };

            using var postgres = new Postgres(connectionString, log);

            await postgres.PrepareAsync(Sql, tokens, token).ConfigureAwait(false);

            var records = await postgres.ExecuteByOrderedParametersAsync(Sql, tokens, token).ConfigureAwait(false);

            return records.Select(record => new GetCaseKeyJournalPeerSampleQueryDto
            {
                Id = Convert.ToInt64(record["Id"], CultureInfo.InvariantCulture),
                KeyValue = record["KeyValue"] as string,
                Json = record["Json"]?.ToString()
            }).ToList();
        }
    }
}
