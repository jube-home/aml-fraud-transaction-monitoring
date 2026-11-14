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


namespace Jube.Data.Query.GetCaseKeyJournalTransactionQuery
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

    public class GetCaseKeyJournalTransactionQuery(string connectionString, ILog log)
    {
        private const string Sql =
            "select a.\"Id\", a.\"EntityAnalysisModelInstanceEntryGuid\", a.\"ReferenceDate\", " +
            "a.\"CreatedDate\", a.\"ResponseElevation\", a.\"EntryKeyValue\", a.\"ActivationRuleCount\", " +
            "a.\"Json\" " +
            "from \"Archive\" a " +
            "inner join \"EntityAnalysisModel\" m on m.\"Id\" = a.\"EntityAnalysisModelId\" " +
            "where a.\"Id\" = (@1) " +
            "and a.\"EntityAnalysisModelId\" = (@2) " +
            "and exists (select 1 from \"UserInTenant\" u " +
            "           where u.\"TenantRegistryId\" = m.\"TenantRegistryId\" and u.\"User\" = (@3)) " +
            "limit 1";

        public async Task<GetCaseKeyJournalRowsQueryDto> ExecuteAsync(long id, int entityAnalysisModelId, string user,
            CancellationToken token = default)
        {
            var tokens = new List<object> { id, entityAnalysisModelId, user };

            using var postgres = new Postgres(connectionString, log);

            await postgres.PrepareAsync(Sql, tokens, token).ConfigureAwait(false);

            var records = await postgres.ExecuteByOrderedParametersAsync(Sql, tokens, token).ConfigureAwait(false);

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
            }).FirstOrDefault();
        }
    }
}
