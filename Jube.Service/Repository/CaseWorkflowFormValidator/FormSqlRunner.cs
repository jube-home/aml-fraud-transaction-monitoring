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

using Jube.Data.Reporting;
using Jube.Service.DynamicSql;
using Jube.Service.Repository.CaseWorkflowFormValidator.Interfaces;
using log4net;

namespace Jube.Service.Repository.CaseWorkflowFormValidator
{
    public sealed class FormSqlRunner(string connectionString, string userName, ILog log) : IFormSqlRunner
    {
        public async Task<bool> PassesAsync(string sql, IReadOnlyDictionary<string, object> parameters,
            CancellationToken token)
        {
            var allowed = DynamicSqlTableAccess.BuildVisualisationAllowedTables(userName, _ => true);

            using var postgres = new Postgres(connectionString, log);

            var result = await postgres
                .ExecuteLimitedByNamedParametersAsync(sql, new Dictionary<string, object>(parameters), allowed, 2,
                    token).ConfigureAwait(false);

            return result is { Columns.Count: 1, Rows.Count: 1 } && result.Rows[0][0] is true;
        }
    }
}
