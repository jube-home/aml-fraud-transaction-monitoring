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

using FluentValidation.Results;
using Jube.Data.Reporting;
using Jube.Data.Reporting.Check;
using Jube.Service.DynamicSql;

namespace Jube.Service.Repository.ExhaustiveSearchInstance
{
    public static class ExhaustiveFilterSqlSchemaCheck
    {
        private const string Prefix = "select count(*) from \"Archive\" where (";
        private const string Suffix = ")";

        public static void Append(ValidationResult results, string? filterSql, SqlSchemaSnapshot? known = null)
        {
            if (string.IsNullOrWhiteSpace(filterSql) || results.Errors.Any(e => e.PropertyName == "FilterSql"))
            {
                return;
            }

            var snapshot = known ?? SqlSchemaSnapshotCache.GetDefault();

            if (snapshot == null)
            {
                return;
            }

            var finding = SqlSchemaChecker.Check(Prefix + filterSql + Suffix, snapshot, new SqlCheckOptions())
                .FirstOrDefault();

            if (finding == null)
            {
                return;
            }

            var message = SqlFindingText.Render(finding);
            var offset = finding.Position - Prefix.Length;

            if (offset is >= 1 && offset <= filterSql.Length)
            {
                var located = PostgresErrorPosition.LineAndColumn(filterSql, offset.Value);
                message += $" ({located.Line}:{located.Column})";
            }

            results.Errors.Add(new ValidationFailure("FilterSql", message) { ErrorCode = "FilterSqlSchema" });
        }
    }
}
