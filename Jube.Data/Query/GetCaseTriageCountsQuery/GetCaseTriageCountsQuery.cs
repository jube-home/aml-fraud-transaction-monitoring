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

namespace Jube.Data.Query.GetCaseTriageCountsQuery
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using GetCaseTriagePageQuery;
    using log4net;
    using Models;
    using Reporting;

    public class GetCaseTriageCountsQuery(string connectionString, ILog log)
    {
        public async Task<List<long>> ExecuteAsync(string whereSql, List<object> tokens,
            IReadOnlyList<string> extraConditions, string userName, CancellationToken token = default)
        {
            if (extraConditions.Count == 0)
            {
                return [];
            }

            var parameters = new List<object>(tokens) { userName };
            var resolved = extraConditions.Select(condition => CaseTriageSql.Resolve(condition, parameters.Count))
                .ToList();

            using var postgres = new Postgres(connectionString, log);

            var rows = await postgres.ExecuteByOrderedParametersAsync(CaseTriageSql.Counts(whereSql, resolved),
                parameters, token).ConfigureAwait(false);

            var row = rows.FirstOrDefault();

            return Enumerable.Range(0, extraConditions.Count)
                .Select(index => row is null ? 0 : RowReader.Long(row, "C" + index)).ToList();
        }
    }
}
