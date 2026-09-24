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

namespace Jube.Parser
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using Data.QueryBuilder;
    using Newtonsoft.Json.Linq;

    public static class BuilderRuleTranslator
    {
        public static string Translate(BuilderGroup group)
        {
            ArgumentNullException.ThrowIfNull(group);

            return "If (" + Group(group) + ") Then\n  Return True\nEnd If";
        }

        private static string Group(BuilderGroup group)
        {
            var parts = new List<string>();
            foreach (var node in group.Rules)
            {
                switch (node)
                {
                    case BuilderGroup nested:
                        parts.Add("( " + Group(nested) + " ) ");
                        break;
                    case BuilderRule rule:
                        parts.Add(Rule(rule));
                        break;
                }
            }

            var expression = string.Join(" " + group.Condition + " ", parts);
            return group.Not ? "NOT ( " + expression + " )" : expression;
        }

        private static string Rule(BuilderRule rule)
        {
            var definition = BuilderOperators.ByType[rule.Operator];
            var values = string.Join(", ", rule.Arguments.Select(a => Render(rule.Type, a)));
            return rule.Id + definition.Template.Replace("?", values);
        }

        private static string Render(BuilderFieldType type, BuilderValue value)
        {
            if (value.IsField)
            {
                return string.Join(", ", value.Fields);
            }

            var kind = value.Argument.Kind != BuilderArgumentKind.Value
                ? value.Argument.Kind
                : type switch
                {
                    BuilderFieldType.Integer => BuilderArgumentKind.Integer,
                    BuilderFieldType.Double => BuilderArgumentKind.Number,
                    BuilderFieldType.DateTime => BuilderArgumentKind.Date,
                    BuilderFieldType.String => BuilderArgumentKind.Text,
                    _ => BuilderArgumentKind.Value
                };

            return string.Join(", ", value.Literals.Select(l => Literal(kind, l)));
        }

        private static string Literal(BuilderArgumentKind kind, JToken value)
        {
            return kind switch
            {
                BuilderArgumentKind.Integer => value.Value<long>().ToString(CultureInfo.InvariantCulture),
                BuilderArgumentKind.Number => value.Type == JTokenType.Integer
                    ? value.Value<long>().ToString(CultureInfo.InvariantCulture)
                    : value.Value<double>().ToString("R", CultureInfo.InvariantCulture),
                BuilderArgumentKind.Date => Quote(value.Value<string>()) + ".ToIsoDateTime()",
                BuilderArgumentKind.Value => value.Value<string>() == "True" ? "True"
                    : value.Value<string>() == "False" ? "False" : Quote(value.Value<string>()),
                _ => Quote(value.Value<string>())
            };
        }

        private static string Quote(string value)
        {
            var sb = new StringBuilder(value.Length + 2);
            sb.Append('"');
            foreach (var c in value)
            {
                sb.Append(c == '"' ? "\"\"" : c.ToString());
            }

            sb.Append('"');
            return sb.ToString();
        }

        public static IReadOnlyDictionary<string, BuilderField> Fields(
            IEnumerable<(string Name, string DataType)> completions)
        {
            ArgumentNullException.ThrowIfNull(completions);

            return completions
                .Select(c => (c.Name, Type: BuilderProfile.FromCompletionDataType(c.DataType)))
                .Where(c => c.Type != null && c.Name.Contains('.'))
                .GroupBy(c => c.Name)
                .ToDictionary(g => g.Key, g => new BuilderField(g.Key, g.First().Type!.Value), StringComparer.Ordinal);
        }
    }
}