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

using System.Text.RegularExpressions;
using FluentValidation.Results;
using Jube.Data.Reporting;
using Jube.Resources;
using Jube.Service.DynamicSql;
using Jube.Service.Security;
using Microsoft.Extensions.Localization;

namespace Jube.Service.CaseDispatch
{
    public sealed class CaseSqlGuard(IStringLocalizer strings, PermissionValidation permissionValidation, string userName)
    {
        public const int SqlPermission = 67;
        public const int MaximumLength = 8000;

        private static readonly int[] sqlPermissions = [SqlPermission];

        private static readonly Regex ParameterPattern = new(@"(?<![\w:@])@([A-Za-z_]\w*)",
            RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

        private static readonly string[] Allowed = ["Id", "CaseKeyValue"];

        public static IReadOnlyList<int> RequiredPermissions => sqlPermissions;

        public bool MayAuthor(string? newSql, string? oldSql)
        {
            return string.Equals(newSql?.Trim() ?? string.Empty, oldSql?.Trim() ?? string.Empty,
                       StringComparison.Ordinal) || permissionValidation.Validate(sqlPermissions);
        }

        public void Append(ValidationResult results, string? sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return;
            }

            if (sql.Length > MaximumLength)
            {
                Add(results, strings[CaseWorkflowSqlResources.SqlTooLong], "SqlMaximumLength");

                return;
            }

            foreach (var name in ParameterPattern.Matches(sql).Select(m => m.Groups[1].Value).Distinct()
                         .Where(n => !Allowed.Contains(n, StringComparer.OrdinalIgnoreCase)))
            {
                Add(results, string.Format(strings[CaseWorkflowSqlResources.SqlParameterUnknown], name),
                    "SqlParameterUnknown");
            }

            if (results.Errors.Any(e => e.PropertyName == "Sql"))
            {
                return;
            }

            try
            {
                PostgresSqlValidator.RewriteTableReferencesPreservingNamedParameters(sql,
                    DynamicSqlTableAccess.BuildVisualisationAllowedTables(userName, permissionValidation.Validate));
            }
            catch (Exception e) when (e is InvalidOperationException or ArgumentException)
            {
                Add(results, string.Format(strings[CaseWorkflowSqlResources.SqlRejected], e.Message), "SqlRejected");
            }
        }

        private static void Add(ValidationResult results, string message, string code)
        {
            results.Errors.Add(new ValidationFailure("Sql", message) { ErrorCode = code });
        }
    }
}
