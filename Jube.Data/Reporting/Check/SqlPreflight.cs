/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Repository.Exceptions;

namespace Jube.Data.Reporting.Check
{
    public static class SqlPreflight
    {
        public static async Task<IReadOnlyList<SqlFinding>> CheckAsync(string sql, string connectionString,
            IReadOnlyDictionary<string, string> allowedTables, IReadOnlyDictionary<string, object> parameters = null,
            CancellationToken token = default)
        {
            var snapshot = await SqlSchemaSnapshotCache.GetAsync(connectionString, token).ConfigureAwait(false);

            return Check(sql, snapshot, allowedTables, parameters);
        }

        public static IReadOnlyList<SqlFinding> Check(string sql, SqlSchemaSnapshot snapshot,
            IReadOnlyDictionary<string, string> allowedTables, IReadOnlyDictionary<string, object> parameters = null)
        {
            if (snapshot == null)
            {
                return [];
            }

            return SqlSchemaChecker.Check(sql, snapshot, new SqlCheckOptions
            {
                AllowedTables = allowedTables,
                ParameterNames = parameters?.Keys.ToHashSet(),
                ParameterCategories = parameters == null ? null : SqlParameterCategories.Of(parameters)
            });
        }

        public static async Task AssertAsync(string sql, string connectionString,
            IReadOnlyDictionary<string, string> allowedTables, IReadOnlyDictionary<string, object> parameters = null,
            CancellationToken token = default)
        {
            var findings = await CheckAsync(sql, connectionString, allowedTables, parameters, token)
                .ConfigureAwait(false);

            if (findings.Count > 0)
            {
                throw ToException(findings[0]);
            }
        }

        public static SqlValidationFailedException ToException(SqlFinding finding)
        {
            return new SqlValidationFailedException(finding.English)
            {
                Code = finding.Code,
                Arguments = finding.Arguments,
                Position = finding.Position
            };
        }
    }
}
