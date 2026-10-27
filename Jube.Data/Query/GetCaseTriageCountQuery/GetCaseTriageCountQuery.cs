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

namespace Jube.Data.Query.GetCaseTriageCountQuery
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using GetCaseTriagePageQuery;
    using log4net;
    using Models;
    using Reporting;

    public class GetCaseTriageCountQuery(string connectionString, ILog log)
    {
        public async Task<long> ExecuteAsync(string whereSql, List<object> tokens, string extraCondition,
            string userName, CancellationToken token = default)
        {
            var parameters = new List<object>(tokens) { userName };

            using var postgres = new Postgres(connectionString, log);

            var rows = await postgres.ExecuteByOrderedParametersAsync(CaseTriageSql.Count(whereSql,
                CaseTriageSql.Resolve(extraCondition, parameters.Count)), parameters, token).ConfigureAwait(false);

            return rows.Select(r => RowReader.Long(r, "Total")).FirstOrDefault();
        }
    }
}
