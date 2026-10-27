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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jube.Service.Query.CaseKeyJournal
{
    public sealed record ArchiveFilterField(string Name, string Sql, string Type);

    public sealed class ArchiveFilter
    {
        public const int MaximumLength = 20000;
        public const int MaximumDepth = 6;
        public const int MaximumRules = 100;

        private static readonly HashSet<string> orderOperators =
            ["equal", "not_equal", "less", "less_or_equal", "greater", "greater_or_equal"];

        private static readonly HashSet<string> textOperators = [..orderOperators, "like", "not_like"];

        private readonly Node root;

        private ArchiveFilter(Node root, string text)
        {
            this.root = root;
            Text = text;
        }

        public string Text { get; }

        public static ArchiveFilter? Parse(string? json, IReadOnlyDictionary<string, ArchiveFilterField> fields)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            if (json.Length > MaximumLength)
            {
                throw new ArgumentException("The filter is too long.");
            }

            JObject document;

            try
            {
                document = JsonConvert.DeserializeObject<JObject>(json,
                               new JsonSerializerSettings { DateParseHandling = DateParseHandling.None })
                           ?? throw new ArgumentException("The filter is not valid JSON.");
            }
            catch (JsonException)
            {
                throw new ArgumentException("The filter is not valid JSON.");
            }

            var count = 0;
            var root = ParseGroup(document, 0, fields, ref count);

            return count == 0 ? null : new ArchiveFilter(root, root.ToToken().ToString(Formatting.None));
        }

        public string Apply(List<object> tokens)
        {
            return root.Sql(tokens);
        }

        private static Node ParseGroup(JToken? token, int depth, IReadOnlyDictionary<string, ArchiveFilterField> fields,
            ref int count)
        {
            if (token is not JObject group || depth > MaximumDepth)
            {
                throw new ArgumentException("The filter group is not valid.");
            }

            var condition = group["condition"]?.Value<string>();

            if (condition is not ("AND" or "OR") || group["rules"] is not JArray rules)
            {
                throw new ArgumentException("The filter group is not valid.");
            }

            var children = new List<Node>();

            foreach (var child in rules)
            {
                if (child is JObject nestedGroup && nestedGroup.ContainsKey("rules"))
                {
                    var nested = ParseGroup(child, depth + 1, fields, ref count);

                    if (nested.Children.Count > 0)
                    {
                        children.Add(nested);
                    }

                    continue;
                }

                count++;

                if (count > MaximumRules)
                {
                    throw new ArgumentException("The filter has too many rules.");
                }

                children.Add(ParseRule(child, fields));
            }

            return new Node(condition, children, null, null, null);
        }

        private static Node ParseRule(JToken token, IReadOnlyDictionary<string, ArchiveFilterField> fields)
        {
            if (token is not JObject rule)
            {
                throw new ArgumentException("The filter rule is not valid.");
            }

            var id = rule["id"]?.Type == JTokenType.String ? rule["id"]!.Value<string>() : null;
            var op = rule["operator"]?.Type == JTokenType.String ? rule["operator"]!.Value<string>() : null;

            if (id is null || op is null || !fields.TryGetValue(id, out var field))
            {
                throw new ArgumentException($"The filter field {id} is not recognised.");
            }

            var allowed = field.Type switch
            {
                "string" => textOperators,
                "boolean" => new HashSet<string> { "equal", "not_equal" },
                _ => orderOperators
            };

            if (!allowed.Contains(op))
            {
                throw new ArgumentException($"The operator {op} cannot be used on {id}.");
            }

            var value = rule["value"] switch
            {
                null => null,
                JValue { Type: JTokenType.Null } => null,
                JValue scalar => Convert.ToString(scalar.Value, CultureInfo.InvariantCulture),
                _ => throw new ArgumentException($"The value for {id} is not valid.")
            };

            if (value is null)
            {
                throw new ArgumentException($"The value for {id} is missing.");
            }

            return new Node(null, [], id, op, new Leaf(field, op, Typed(field, value)));
        }

        private static object Typed(ArchiveFilterField field, string value)
        {
            switch (field.Type)
            {
                case "integer" when int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var whole):
                    return whole;
                case "double" when double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                       out var number) && double.IsFinite(number):
                    return number;
                case "datetime" when DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal, out var moment):
                    return DateTime.SpecifyKind(moment.UtcDateTime, DateTimeKind.Unspecified);
                case "boolean" when bool.TryParse(value, out var flag):
                    return flag;
                case "string":
                    return value.Length > JournalMerge.MaximumValueLength || value.Contains('\0')
                        ? throw new ArgumentException($"The value for {field.Name} is not valid.")
                        : value;
                default:
                    throw new ArgumentException($"The value for {field.Name} is not a valid {field.Type}.");
            }
        }

        private sealed record Leaf(ArchiveFilterField Field, string Operator, object Value);

        private sealed record Node(string? Condition, List<Node> Children, string? Id, string? Operator, Leaf? Leaf)
        {
            public JToken ToToken()
            {
                if (Leaf is null)
                {
                    return new JObject
                    {
                        ["condition"] = Condition,
                        ["rules"] = new JArray(Children.Select(child => child.ToToken()))
                    };
                }

                var raw = Leaf.Value switch
                {
                    DateTime moment => moment.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture),
                    bool flag => flag ? "True" : "False",
                    IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                    var other => other.ToString()
                };

                return new JObject { ["id"] = Id, ["operator"] = Operator, ["value"] = raw };
            }

            public string Sql(List<object> tokens)
            {
                if (Leaf is null)
                {
                    var parts = Children.Select(child => child.Sql(tokens)).ToList();

                    return parts.Count == 0 ? "1 = 1" : "(" + string.Join(Condition == "OR" ? " or " : " and ", parts) + ")";
                }

                tokens.Add(Leaf.Value);

                var position = $"(@{tokens.Count})";
                var path = Leaf.Field.Sql;

                return Leaf.Operator switch
                {
                    "equal" => $"{path} = {position}",
                    "not_equal" => $"{path} <> {position}",
                    "less" => $"{path} < {position}",
                    "less_or_equal" => $"{path} <= {position}",
                    "greater" => $"{path} > {position}",
                    "greater_or_equal" => $"{path} >= {position}",
                    "like" => $"{path} like {position}",
                    "not_like" => $"{path} not like {position}",
                    _ => throw new InvalidOperationException("The operator is not supported.")
                };
            }
        }
    }
}
