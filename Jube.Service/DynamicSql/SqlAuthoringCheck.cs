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

using Jube.Data.Reporting.Check;

namespace Jube.Service.DynamicSql
{
    public static class SqlAuthoringCheck
    {
        public static string? Describe(string sql, IReadOnlyDictionary<string, string>? tables,
            IReadOnlyDictionary<string, SqlTypeCategory>? parameterCategories = null)
        {
            var snapshot = SqlSchemaSnapshotCache.GetDefault();

            if (snapshot == null)
            {
                return null;
            }

            var findings = SqlSchemaChecker.Check(sql, snapshot, new SqlCheckOptions
            {
                AllowedTables = tables,
                ParameterCategories = parameterCategories
            });

            if (findings.Count == 0)
            {
                return null;
            }

            var first = findings[0];
            var text = SqlFindingText.Render(first);

            return first.Line.HasValue ? $"{text} ({first.Line}:{first.Column})" : text;
        }
    }
}
