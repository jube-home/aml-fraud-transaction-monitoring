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
using System.Text;

namespace Jube.Service.Charts.Datasource.Template
{
    public static class TemplateFilters
    {
        private static readonly IReadOnlyDictionary<string, (int Minimum, int Maximum)> arities =
            new Dictionary<string, (int, int)>(StringComparer.OrdinalIgnoreCase)
            {
                ["number"] = (0, 1),
                ["percent"] = (0, 1),
                ["currency"] = (1, 1),
                ["date"] = (0, 0),
                ["dateTime"] = (0, 0),
                ["upper"] = (0, 0),
                ["lower"] = (0, 0),
                ["truncate"] = (1, 1),
                ["round"] = (0, 1),
                ["default"] = (1, 1),
                ["join"] = (0, 1)
            };

        public static bool Exists(string name)
        {
            return arities.ContainsKey(name);
        }

        public static bool AcceptsArguments(string name, int count)
        {
            return arities.TryGetValue(name, out var arity) && count >= arity.Minimum && count <= arity.Maximum;
        }

        public static object? Apply(string name, object? input, IReadOnlyList<object?> arguments)
        {
            var culture = CultureInfo.CurrentUICulture;

            switch (arities.Keys.FirstOrDefault(k => string.Equals(k, name, StringComparison.OrdinalIgnoreCase)))
            {
                case "number":
                    return TemplateValues.AsFormattable(input) is { } number
                        ? number.ToString(Digits(arguments, 0, -1) is var places and >= 0 ? "N" + places : "#,0.##",
                            culture)
                        : string.Empty;
                case "percent":
                    return TemplateValues.AsFormattable(input) is { } fraction
                        ? fraction.ToString("P" + Math.Max(Digits(arguments, 0, 1), 0), culture)
                        : string.Empty;
                case "currency":
                    return TemplateValues.AsFormattable(input) is { } amount
                        ? TemplateValues.Display(arguments[0]) + " " + amount.ToString("N2", culture)
                        : string.Empty;
                case "date":
                    return TemplateValues.AsTime(input) is { } day
                        ? day.ToString("dd MMM yyyy", culture)
                        : string.Empty;
                case "dateTime":
                    return TemplateValues.AsTime(input) is { } moment
                        ? moment.ToString("dd MMM yyyy HH:mm", culture)
                        : string.Empty;
                case "upper":
                    return TemplateValues.Display(input).ToUpper(culture);
                case "lower":
                    return TemplateValues.Display(input).ToLower(culture);
                case "truncate":
                    return Truncate(TemplateValues.Display(input), Digits(arguments, 0, 0));
                case "round":
                    return TemplateValues.AsFormattable(input) is { } value
                        ? Math.Round(value, Math.Clamp(Digits(arguments, 0, 0), 0, 10), MidpointRounding.AwayFromZero)
                        : null;
                case "default":
                    return input is null or "" ? arguments[0] : input;
                case "join":
                    return input is IEnumerable<object?> items && input is not string
                        ? string.Join(arguments.Count > 0 ? TemplateValues.Display(arguments[0]) : ", ",
                            items.Select(TemplateValues.Display))
                        : TemplateValues.Display(input);
                default:
                    return input;
            }
        }

        private static int Digits(IReadOnlyList<object?> arguments, int index, int fallback)
        {
            return arguments.Count > index && TemplateValues.AsNumber(arguments[index]) is { } n
                ? (int)Math.Clamp(n, -1, 20)
                : fallback;
        }

        private static string Truncate(string text, int length)
        {
            if (length < 0 || text.Length <= length)
            {
                return text;
            }

            return new StringBuilder(text, 0, length, length + 1).Append('\u2026').ToString();
        }
    }
}
