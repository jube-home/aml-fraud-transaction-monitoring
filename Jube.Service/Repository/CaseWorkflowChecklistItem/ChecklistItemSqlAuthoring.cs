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
using Jube.Service.DynamicSql;
using Jube.Dto.Repository.CaseWorkflowChecklistItem;
using Jube.Resources;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Repository.CaseWorkflowChecklistItem
{
    public sealed class ChecklistItemSqlAuthoring(IStringLocalizer strings)
    {
        private static readonly Regex parameterPattern = new(@"(?<![\w:@])@([A-Za-z_]\w*)",
            RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

        private static readonly string[] builtInParameters = ["Id", "CaseKeyValue"];

        public List<ValidationFailure> Check(CaseWorkflowChecklistItemDto model,
            IReadOnlyDictionary<string, string> authorTables)
        {
            var failures = new List<ValidationFailure>();

            Sql(model.ShowSql, nameof(CaseWorkflowChecklistItemDto.ShowSql), authorTables, failures);
            Sql(model.ValidateSql, nameof(CaseWorkflowChecklistItemDto.ValidateSql), authorTables, failures);

            return failures;
        }

        private void Sql(string? sql, string property, IReadOnlyDictionary<string, string> tables,
            List<ValidationFailure> failures)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return;
            }

            var unknown = false;

            foreach (var name in parameterPattern.Matches(sql).Select(m => m.Groups[1].Value).Distinct())
            {
                if (builtInParameters.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                unknown = true;
                failures.Add(Failure(property,
                    string.Format(strings[CaseWorkflowChecklistItemResources.SqlParameterUnknown], name),
                    "SqlParameterUnknown"));
            }

            if (unknown)
            {
                return;
            }

            var finding = SqlAuthoringCheck.Describe(sql, tables);

            if (finding != null)
            {
                failures.Add(Failure(property,
                    string.Format(strings[CaseWorkflowChecklistItemResources.SqlRejected], finding),
                    "SqlRejected"));

                return;
            }

            try
            {
                PostgresSqlValidator.RewriteTableReferencesPreservingNamedParameters(sql, tables);
            }
            catch (Exception e) when (e is InvalidOperationException or ArgumentException)
            {
                failures.Add(Failure(property,
                    string.Format(strings[CaseWorkflowChecklistItemResources.SqlRejected], e.Message),
                    "SqlRejected"));
            }
        }

        private static ValidationFailure Failure(string property, string message, string code)
        {
            return new ValidationFailure(property, message) { ErrorCode = code };
        }
    }
}
