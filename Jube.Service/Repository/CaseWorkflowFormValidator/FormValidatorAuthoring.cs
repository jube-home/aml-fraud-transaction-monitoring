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
using Jube.Dto.Options;
using Jube.Dto.Repository.CaseWorkflowFormValidator;
using Jube.Resources;
using Jube.Service.Charts.Datasource;
using Jube.Service.Charts.Datasource.Template;
using Jube.Service.Charts.Datasource.Template.Models;
using Jube.Service.Repository.CaseWorkflowFormValidator.Interfaces;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Repository.CaseWorkflowFormValidator
{
    public sealed class FormValidatorAuthoring(
        IStringLocalizer strings,
        DatasourceChartText chartText)
    {
        private static readonly Regex ParameterPattern = new(@"(?<![\w:@])@([A-Za-z_]\w*)",
            RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

        private static readonly string[] BuiltInParameters = ["value", "CaseId", "CaseKeyValue"];

        public async Task<(FormSchema? Schema, List<ValidationFailure> Failures)> CheckAsync(
            CaseWorkflowFormValidatorDto model, string? formHtml, IFormValidatorLookups lookups,
            IReadOnlyDictionary<string, string>? authorTables, CancellationToken token)
        {
            var failures = new List<ValidationFailure>();
            FormSchema schema;

            try
            {
                schema = DatasourceTemplate.CompileForm(formHtml);
            }
            catch (DatasourceChartException ex)
            {
                failures.Add(Failure(nameof(model.CaseWorkflowFormGuid),
                    string.Format(strings[CaseWorkflowFormValidatorResources.TemplateInvalid], chartText.Describe(ex)),
                    "TemplateInvalid"));

                return (null, failures);
            }

            var field = schema.Fields.FirstOrDefault(f =>
                string.Equals(f.Name, model.FieldName, StringComparison.OrdinalIgnoreCase));

            if (field == null)
            {
                failures.Add(Failure(nameof(model.FieldName), strings[CaseWorkflowFormValidatorResources.FieldNotInForm],
                    "FieldNotInForm"));

                return (schema, failures);
            }

            var kind = (CaseWorkflowFormValidatorKind)model.KindId;

            if (!Applies(kind, field.Kind))
            {
                failures.Add(Failure(nameof(model.KindId), strings[CaseWorkflowFormValidatorResources.KindNotForField],
                    "KindNotForField"));
            }

            switch (kind)
            {
                case CaseWorkflowFormValidatorKind.RequiredWhen:
                    Other(schema, model.Argument1, nameof(model.Argument1), failures);

                    break;
                case CaseWorkflowFormValidatorKind.Compare:
                    Other(schema, model.Argument2, nameof(model.Argument2), failures);

                    break;
                case CaseWorkflowFormValidatorKind.InList:
                    if (!await lookups.ListExistsAsync(model.Argument1!, token).ConfigureAwait(false))
                    {
                        failures.Add(Failure(nameof(model.Argument1),
                            strings[CaseWorkflowFormValidatorResources.ListNotFound], "ListNotFound"));
                    }

                    break;
                case CaseWorkflowFormValidatorKind.InDictionary:
                    if (!await lookups.DictionaryExistsAsync(model.Argument1!, token).ConfigureAwait(false))
                    {
                        failures.Add(Failure(nameof(model.Argument1),
                            strings[CaseWorkflowFormValidatorResources.DictionaryNotFound], "DictionaryNotFound"));
                    }

                    break;
                case CaseWorkflowFormValidatorKind.Sql:
                    Sql(model.Sql!, schema, authorTables ?? new Dictionary<string, string>(), failures);

                    break;
            }

            return (schema, failures);
        }

        private static bool Applies(CaseWorkflowFormValidatorKind kind, FormFieldKind field)
        {
            return kind switch
            {
                CaseWorkflowFormValidatorKind.Range => field is FormFieldKind.Number or FormFieldKind.Date
                    or FormFieldKind.DateTime,
                CaseWorkflowFormValidatorKind.Length or CaseWorkflowFormValidatorKind.Pattern => field is
                    FormFieldKind.Text or FormFieldKind.TextArea or FormFieldKind.Email or FormFieldKind.Telephone,
                CaseWorkflowFormValidatorKind.InList or CaseWorkflowFormValidatorKind.InDictionary => field is
                    FormFieldKind.Text or FormFieldKind.Email or FormFieldKind.Telephone or FormFieldKind.Number,
                _ => true
            };
        }

        private void Other(FormSchema schema, string? name, string property, List<ValidationFailure> failures)
        {
            if (schema.Fields.All(f => !string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                failures.Add(Failure(property,
                    string.Format(strings[CaseWorkflowFormValidatorResources.OtherFieldNotInForm], name), "OtherFieldNotInForm"));
            }
        }

        private void Sql(string sql, FormSchema schema, IReadOnlyDictionary<string, string> tables,
            List<ValidationFailure> failures)
        {
            var known = new HashSet<string>(BuiltInParameters.Concat(schema.Fields.Select(f => f.Name)),
                StringComparer.OrdinalIgnoreCase);

            foreach (var name in ParameterPattern.Matches(sql).Select(m => m.Groups[1].Value).Distinct())
            {
                if (!known.Contains(name))
                {
                    failures.Add(Failure(nameof(CaseWorkflowFormValidatorDto.Sql),
                        string.Format(strings[CaseWorkflowFormValidatorResources.SqlParameterUnknown], name),
                        "SqlParameterUnknown"));
                }
            }

            if (failures.Count > 0)
            {
                return;
            }

            try
            {
                PostgresSqlValidator.RewriteTableReferencesPreservingNamedParameters(sql, tables);
            }
            catch (Exception e) when (e is InvalidOperationException or ArgumentException)
            {
                failures.Add(Failure(nameof(CaseWorkflowFormValidatorDto.Sql),
                    string.Format(strings[CaseWorkflowFormValidatorResources.SqlRejected], e.Message), "SqlRejected"));
            }
        }

        private static ValidationFailure Failure(string property, string message, string code)
        {
            return new ValidationFailure(property, message) { ErrorCode = code };
        }
    }
}
