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

using System.Globalization;
using System.Text.RegularExpressions;
using Jube.Dto.Options;
using Jube.Service.Charts.Datasource.Template.Models;
using Jube.Service.Repository.CaseWorkflowFormValidator.Interfaces;
using Jube.Service.Repository.CaseWorkflowFormValidator.Models;

namespace Jube.Service.Repository.CaseWorkflowFormValidator
{
    public static class FormValidatorEngine
    {
        public const string Required = "required";
        public const string Range = "range";
        public const string Length = "length";
        public const string Pattern = "pattern";
        public const string Compare = "compare";
        public const string List = "list";
        public const string Dictionary = "dictionary";
        public const string Sql = "sql";
        public const string SqlFailed = "sqlFailed";

        private static readonly TimeSpan PatternTimeout = TimeSpan.FromMilliseconds(250);
        private static readonly TimeSpan SqlTimeout = TimeSpan.FromSeconds(3);

        public static async Task<List<FormFieldError>> ValidateAsync(FormSchema schema,
            IReadOnlyCollection<string> visibleFields, IReadOnlyDictionary<string, string> values,
            IReadOnlyList<FormValidatorRule> rules, string caseKeyValue, int caseId, IFormValidatorLookups lookups,
            IFormSqlRunner sqlRunner, CancellationToken token)
        {
            var errors = new List<FormFieldError>();
            var fields = schema.Fields.ToDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase);
            var failed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var input = new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);

            foreach (var rule in rules.OrderBy(r => r.Id))
            {
                token.ThrowIfCancellationRequested();

                if (!fields.TryGetValue(rule.FieldName, out var field) ||
                    !visibleFields.Contains(field.Name, StringComparer.OrdinalIgnoreCase) ||
                    failed.Contains(field.Name))
                {
                    continue;
                }

                input.TryGetValue(field.Name, out var value);

                var code = await CheckAsync(rule, field, value, input, caseKeyValue, caseId, lookups, sqlRunner,
                    token).ConfigureAwait(false);

                if (code == null)
                {
                    continue;
                }

                failed.Add(field.Name);
                errors.Add(new FormFieldError(field.Name, code, string.IsNullOrWhiteSpace(rule.Message) ? null : rule.Message));
            }

            return errors;
        }

        public static async Task<FormValidatorOutcome> TestAsync(FormSchema schema, FormValidatorRule rule,
            IReadOnlyDictionary<string, string> values, string caseKeyValue, int caseId, IFormValidatorLookups lookups,
            IFormSqlRunner sqlRunner, CancellationToken token)
        {
            var clock = System.Diagnostics.Stopwatch.StartNew();
            var field = schema.Fields.FirstOrDefault(f => string.Equals(f.Name, rule.FieldName,
                StringComparison.OrdinalIgnoreCase));

            if (field == null)
            {
                return new FormValidatorOutcome(false, null, 0);
            }

            var input = new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);

            input.TryGetValue(field.Name, out var value);

            var code = await CheckAsync(rule, field, value, input, caseKeyValue, caseId, lookups, sqlRunner, token)
                .ConfigureAwait(false);

            return new FormValidatorOutcome(code == null, code, (int)clock.ElapsedMilliseconds);
        }

        private static async Task<string?> CheckAsync(FormValidatorRule rule, FormField field, string? value,
            IReadOnlyDictionary<string, string> input, string caseKeyValue, int caseId, IFormValidatorLookups lookups,
            IFormSqlRunner sqlRunner, CancellationToken token)
        {
            var present = !string.IsNullOrEmpty(value);

            switch (rule.Kind)
            {
                case CaseWorkflowFormValidatorKind.Required:
                    return MissingValue(field, value) ? Required : null;
                case CaseWorkflowFormValidatorKind.RequiredWhen:
                    return !present && input.TryGetValue(rule.Argument1 ?? string.Empty, out var other) &&
                           string.Equals(other, rule.Argument2, StringComparison.OrdinalIgnoreCase)
                        ? Required
                        : null;
            }

            if (!present)
            {
                return null;
            }

            switch (rule.Kind)
            {
                case CaseWorkflowFormValidatorKind.Range:
                    return InRange(field, value!, rule.Argument1, rule.Argument2) ? null : Range;
                case CaseWorkflowFormValidatorKind.Length:
                    return Between(value!.Length, rule.Argument1, rule.Argument2) ? null : Length;
                case CaseWorkflowFormValidatorKind.Pattern:
                    return Matches(value!, rule.Argument1) ? null : Pattern;
                case CaseWorkflowFormValidatorKind.Compare:
                    return input.TryGetValue(rule.Argument2 ?? string.Empty, out var against) &&
                           !string.IsNullOrEmpty(against) && !Related(field, value!, against, rule.Argument1)
                        ? Compare
                        : null;
                case CaseWorkflowFormValidatorKind.InList:
                    return await lookups.ListContainsAsync(rule.Argument1 ?? string.Empty, value!, token)
                        .ConfigureAwait(false)
                        ? null
                        : List;
                case CaseWorkflowFormValidatorKind.InDictionary:
                    return await lookups.DictionaryContainsAsync(rule.Argument1 ?? string.Empty, value!, token)
                        .ConfigureAwait(false)
                        ? null
                        : Dictionary;
                case CaseWorkflowFormValidatorKind.Sql:
                    return await RunSqlAsync(rule, value!, input, caseKeyValue, caseId, sqlRunner, token)
                        .ConfigureAwait(false);
                default:
                    return null;
            }
        }

        private static bool MissingValue(FormField field, string? value)
        {
            return field.Kind == FormFieldKind.Checkbox ? value != "true" : string.IsNullOrEmpty(value);
        }

        private static bool Between(decimal actual, string? minimum, string? maximum)
        {
            return (!Decimal(minimum, out var low) || actual >= low) &&
                   (!Decimal(maximum, out var high) || actual <= high);
        }

        private static bool Decimal(string? text, out decimal number)
        {
            return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out number);
        }

        private static bool Time(string? text, out DateTime time)
        {
            return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out time);
        }

        private static bool InRange(FormField field, string value, string? minimum, string? maximum)
        {
            if (field.Kind == FormFieldKind.Number)
            {
                return Decimal(value, out var number) && Between(number, minimum, maximum);
            }

            if (field.Kind is FormFieldKind.Date or FormFieldKind.DateTime)
            {
                return Time(value, out var when) &&
                       (!Time(minimum, out var low) || when >= low) &&
                       (!Time(maximum, out var high) || when <= high);
            }

            return true;
        }

        private static bool Matches(string value, string? pattern)
        {
            try
            {
                return Regex.IsMatch(value, pattern ?? string.Empty,
                    RegexOptions.NonBacktracking | RegexOptions.CultureInvariant, PatternTimeout);
            }
            catch (Exception e) when (e is RegexMatchTimeoutException or ArgumentException)
            {
                return false;
            }
        }

        private static bool Related(FormField field, string left, string right, string? relation)
        {
            int order;

            if (Decimal(left, out var a) && Decimal(right, out var b) && field.Kind != FormFieldKind.Text)
            {
                order = a.CompareTo(b);
            }
            else if (field.Kind is FormFieldKind.Date or FormFieldKind.DateTime && Time(left, out var x) &&
                     Time(right, out var y))
            {
                order = x.CompareTo(y);
            }
            else
            {
                order = Math.Sign(string.CompareOrdinal(left, right));
            }

            return relation switch
            {
                "eq" => order == 0,
                "ne" => order != 0,
                "lt" => order < 0,
                "le" => order <= 0,
                "gt" => order > 0,
                "ge" => order >= 0,
                _ => false
            };
        }

        private static async Task<string?> RunSqlAsync(FormValidatorRule rule, string value,
            IReadOnlyDictionary<string, string> input, string caseKeyValue, int caseId, IFormSqlRunner sqlRunner,
            CancellationToken token)
        {
            var parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (var (name, text) in input)
            {
                parameters[name] = text;
            }

            parameters["value"] = value;
            parameters["CaseId"] = caseId;
            parameters["CaseKeyValue"] = caseKeyValue;

            using var limit = CancellationTokenSource.CreateLinkedTokenSource(token);

            limit.CancelAfter(SqlTimeout);

            try
            {
                return await sqlRunner.PassesAsync(rule.Sql ?? string.Empty, parameters, limit.Token)
                    .ConfigureAwait(false)
                    ? null
                    : Sql;
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                return SqlFailed;
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                return SqlFailed;
            }
        }
    }
}
