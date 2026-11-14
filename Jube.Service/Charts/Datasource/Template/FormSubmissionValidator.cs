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
using System.Net.Mail;
using System.Text.RegularExpressions;
using Jube.Service.Charts.Datasource.Template.Models;

namespace Jube.Service.Charts.Datasource.Template
{
    public static class FormSubmissionValidator
    {
        public const string Unknown = "unknown";
        public const string Type = "type";
        public const string Option = "option";
        public const string Length = "length";

        private const int MaximumTextLength = 4000;
        private const int MaximumTextAreaLength = 20000;

        private static readonly Regex Telephone = new(@"^\+?[0-9 ()\-]{3,32}$", RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(1));

        private static readonly string[] DateTimeFormats =
            ["yyyy-MM-dd'T'HH:mm", "yyyy-MM-dd'T'HH:mm:ss", "yyyy-MM-dd'T'HH:mm:ss.FFF"];

        public static FormSubmissionResult Validate(FormSchema schema, IReadOnlyCollection<string> visibleFields,
            IReadOnlyDictionary<string, string?> submitted)
        {
            var errors = new List<FormFieldError>();
            var accepted = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var fields = schema.Fields.ToDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var key in submitted.Keys.Where(k => !fields.ContainsKey(k)))
            {
                errors.Add(new FormFieldError(key, Unknown));
            }

            foreach (var field in schema.Fields)
            {
                if (!visibleFields.Contains(field.Name, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!submitted.TryGetValue(field.Name, out var raw) || string.IsNullOrEmpty(raw))
                {
                    continue;
                }

                var code = Check(field, raw);

                if (code == null)
                {
                    accepted[field.Name] = raw;
                }
                else
                {
                    errors.Add(new FormFieldError(field.Name, code));
                }
            }

            return new FormSubmissionResult(accepted, errors);
        }

        private static string? Check(FormField field, string value)
        {
            switch (field.Kind)
            {
                case FormFieldKind.Text:
                    return value.Length > MaximumTextLength ? Length : null;
                case FormFieldKind.TextArea:
                    return value.Length > MaximumTextAreaLength ? Length : null;
                case FormFieldKind.Number:
                    return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _)
                        ? null
                        : Type;
                case FormFieldKind.Date:
                    return DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out _)
                        ? null
                        : Type;
                case FormFieldKind.DateTime:
                    return DateTime.TryParseExact(value, DateTimeFormats, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out _)
                        ? null
                        : Type;
                case FormFieldKind.Email:
                    return value.Length <= 320 && MailAddress.TryCreate(value, out var address) &&
                           address.Address == value
                        ? null
                        : Type;
                case FormFieldKind.Telephone:
                    return Telephone.IsMatch(value) ? null : Type;
                case FormFieldKind.Checkbox:
                    return value is "true" or "false" ? null : Type;
                default:
                    return field.Options.Contains(value, StringComparer.Ordinal) ? null : Option;
            }
        }
    }
}
