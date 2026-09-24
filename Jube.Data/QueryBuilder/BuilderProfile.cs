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

// ReSharper disable MemberCanBePrivate.Global

namespace Jube.Data.QueryBuilder
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public sealed record BuilderField(string Id, BuilderFieldType Type);

    public abstract record BuilderNode;

    public sealed record BuilderGroup(string Condition, bool Not, IReadOnlyList<BuilderNode> Rules) : BuilderNode;

    public sealed record BuilderValue(
        BuilderArgument Argument,
        IReadOnlyList<JToken> Literals,
        IReadOnlyList<string> Fields)
    {
        public bool IsField => Fields.Count > 0;
    }

    public sealed record BuilderRule(
        string Id,
        BuilderFieldType Type,
        string Operator,
        // ReSharper disable once NotAccessedPositionalProperty.Global
        JToken Value,
        IReadOnlyList<BuilderValue> Arguments) : BuilderNode;

    public sealed record BuilderError(string Path, string Code, string Message);

    public sealed record BuilderParseResult(BuilderGroup Group, IReadOnlyList<BuilderError> Errors)
    {
        public bool Valid => Group != null && Errors.Count == 0;
    }

    public static class BuilderProfile
    {
        public const int MaximumDepth = 8;
        public const int MaximumRules = 200;
        public const int MaximumLength = 262_144;

        public const string Equal = "equal";
        public const string Less = "less";
        public const string LessOrEqual = "less_or_equal";
        public const string Greater = "greater";
        public const string GreaterOrEqual = "greater_or_equal";
        public const string BeginsWith = "begins_with";
        public const string Contains = "contains";
        public const string EndsWith = "ends_with";
        public const string Has = "has";
        public const string NotEqual = "not_equal";
        public const string Between = "between";
        public const string NotBetween = "not_between";
        public const string In = "in";
        public const string NotIn = "not_in";
        public const string NotBeginsWith = "not_begins_with";
        public const string NotContains = "not_contains";
        public const string NotEndsWith = "not_ends_with";
        public const string IsEmpty = "is_empty";
        public const string IsNotEmpty = "is_not_empty";
        public const string IsNull = "is_null";
        public const string IsNotNull = "is_not_null";

        private static readonly HashSet<string> groupKeys = ["condition", "rules", "not", "valid", "flags", "data"];

        private static readonly HashSet<string> ruleKeys =
            ["id", "field", "type", "input", "operator", "value", "flags", "data"];

        public static IReadOnlyDictionary<BuilderFieldType, string[]> Operators { get; } =
            Enum.GetValues<BuilderFieldType>().ToDictionary(t => t,
                t => BuilderOperators.For(t, BuilderTarget.InMemory).Select(o => o.Type).ToArray());

        public static IReadOnlyDictionary<BuilderFieldType, string[]> RuleTextOperators { get; } =
            Enum.GetValues<BuilderFieldType>().ToDictionary(t => t,
                t => BuilderOperators.For(t, BuilderTarget.RuleText).Select(o => o.Type).ToArray());

        public static BuilderFieldType? FromCompletionDataType(string dataType)
        {
            return dataType switch
            {
                "string" => BuilderFieldType.String,
                "integer" => BuilderFieldType.Integer,
                "double" => BuilderFieldType.Double,
                "boolean" => BuilderFieldType.Boolean,
                "list" => BuilderFieldType.List,
                "datetime" => BuilderFieldType.DateTime,
                _ => null
            };
        }

        public static BuilderParseResult Parse(string json, IReadOnlyDictionary<string, BuilderField> fields,
            BuilderTarget target = BuilderTarget.InMemory)
        {
            ArgumentNullException.ThrowIfNull(fields);
            var errors = new List<BuilderError>();

            if (string.IsNullOrWhiteSpace(json))
            {
                errors.Add(new BuilderError("$", "BuilderJsonEmpty", "The builder JSON is empty."));
                return new BuilderParseResult(null, errors);
            }

            if (json.Length > MaximumLength)
            {
                errors.Add(new BuilderError("$", "BuilderJsonTooLarge",
                    $"The builder JSON is larger than {MaximumLength} characters."));
                return new BuilderParseResult(null, errors);
            }

            JToken root;
            try
            {
                using var reader = new JsonTextReader(new System.IO.StringReader(json));
                reader.DateParseHandling = DateParseHandling.None;
                reader.MaxDepth = MaximumDepth * 2 + 4;
                root = JToken.ReadFrom(reader);
            }
            catch (JsonException ex)
            {
                errors.Add(new BuilderError("$", "BuilderJsonInvalid",
                    $"The builder JSON could not be read: {ex.Message}"));
                return new BuilderParseResult(null, errors);
            }

            var ruleCount = 0;
            var group = ParseGroup(root, "$", 1, fields, target, errors, ref ruleCount);
            return new BuilderParseResult(errors.Count == 0 ? group : null, errors);
        }

        private static BuilderGroup ParseGroup(JToken token, string path, int depth,
            IReadOnlyDictionary<string, BuilderField> fields, BuilderTarget target, List<BuilderError> errors,
            ref int ruleCount)
        {
            if (token is not JObject group)
            {
                errors.Add(
                    new BuilderError(path, "GroupInvalid", "A group must be an object with condition and rules."));
                return null;
            }

            if (depth > MaximumDepth)
            {
                errors.Add(new BuilderError(path, "GroupTooDeep",
                    $"Groups may be nested at most {MaximumDepth} deep."));
                return null;
            }

            RejectUnknownKeys(group, groupKeys, path, errors);

            var condition = group["condition"]?.Type == JTokenType.String ? group["condition"]!.Value<string>() : "AND";
            if (!string.Equals(condition, "AND", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(condition, "OR", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(new BuilderError(path + ".condition", "ConditionInvalid",
                    $"'{condition}' is not a condition; use AND or OR."));
            }

            var not = group["not"] is { Type: JTokenType.Boolean } notToken && notToken.Value<bool>();

            if (group["rules"] is not JArray rules || rules.Count == 0)
            {
                errors.Add(new BuilderError(path + ".rules", "RulesEmpty", "A group must contain at least one rule."));
                return null;
            }

            var nodes = new List<BuilderNode>();
            for (var i = 0; i < rules.Count; i++)
            {
                var childPath = $"{path}.rules[{i}]";
                if (rules[i] is JObject child && child["rules"] is JArray { Count: > 0 })
                {
                    var nested = ParseGroup(child, childPath, depth + 1, fields, target, errors, ref ruleCount);
                    if (nested != null)
                    {
                        nodes.Add(nested);
                    }

                    continue;
                }

                if (++ruleCount > MaximumRules)
                {
                    errors.Add(
                        new BuilderError(childPath, "TooManyRules", $"At most {MaximumRules} rules are allowed."));
                    return null;
                }

                var rule = ParseRule(rules[i], childPath, fields, target, errors);
                if (rule != null)
                {
                    nodes.Add(rule);
                }
            }

            return new BuilderGroup(condition, not, nodes);
        }

        private static BuilderRule ParseRule(JToken token, string path,
            IReadOnlyDictionary<string, BuilderField> fields,
            BuilderTarget target, List<BuilderError> errors)
        {
            if (token is not JObject rule)
            {
                errors.Add(new BuilderError(path, "RuleInvalid",
                    "A rule must be an object with id, operator and value."));
                return null;
            }

            RejectUnknownKeys(rule, ruleKeys, path, errors);

            var id = rule["id"]?.Type == JTokenType.String ? rule["id"]!.Value<string>() : null;
            var fieldName = rule["field"]?.Type == JTokenType.String ? rule["field"]!.Value<string>() : null;
            if (id != null && fieldName != null && !string.Equals(id, fieldName, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(new BuilderError(path + ".field", "FieldMismatch",
                    "field must name the same field as id when both are given."));
                return null;
            }

            id ??= fieldName;
            if (id == null || !fields.TryGetValue(id, out var field))
            {
                errors.Add(new BuilderError(path + ".id", "FieldUnknown",
                    $"'{id}' is not a field that can be used here; list the fields to see which can."));
                return null;
            }

            var op = rule["operator"]?.Type == JTokenType.String ? rule["operator"]!.Value<string>() : null;
            var allowed = target == BuilderTarget.RuleText ? RuleTextOperators[field.Type] : Operators[field.Type];
            if (op == null || !allowed.Contains(op))
            {
                errors.Add(new BuilderError(path + ".operator", "OperatorInvalid",
                    $"'{op}' cannot be used with {id} ({field.Type}); use one of {string.Join(", ", allowed)}."));
                return null;
            }

            var definition = BuilderOperators.ByType[op];
            var value = rule["value"];
            var inputs = Inputs(definition, value);
            if (inputs == null)
            {
                errors.Add(new BuilderError(path + ".value", "ValueInvalid", definition.Inputs switch
                {
                    0 => $"'{definition.Label}' takes no value.",
                    1 => $"'{definition.Label}' takes one value.",
                    _ => $"'{definition.Label}' takes {definition.Inputs} values: " +
                         string.Join(", ", definition.Arguments.Select(x => x.Name)) + "."
                }));
                return null;
            }

            var arguments = new List<BuilderValue>();
            for (var i = 0; i < definition.Inputs; i++)
            {
                var argument = definition.Arguments[i];
                var argumentPath = definition.Inputs == 1 ? path + ".value" : $"{path}.value[{i}]";
                var parsed = Argument(argument, field, inputs[i], fields, target, out var message);
                if (parsed == null)
                {
                    errors.Add(new BuilderError(argumentPath, "ValueInvalid", message));
                    return null;
                }

                arguments.Add(parsed);
            }

            return new BuilderRule(id, field.Type, op, value, arguments);
        }

        private static List<JToken> Inputs(BuilderOperator definition, JToken value)
        {
            var empty = value is null or { Type: JTokenType.Null } or JArray { Count: 0 } ||
                        (value.Type == JTokenType.String && value.Value<string>() == string.Empty);

            switch (definition.Inputs)
            {
                case 0:
                    return empty ? [] : null;
                case 1:
                    if (empty)
                    {
                        return null;
                    }

                    if (value is JArray array && !definition.Arguments[0].Many)
                    {
                        return array.Count == 1 ? [array[0]] : null;
                    }

                    return [value];
                default:
                    return value is JArray inputs && inputs.Count == definition.Inputs ? inputs.ToList() : null;
            }
        }

        private static BuilderValue Argument(BuilderArgument argument, BuilderField field, JToken token,
            IReadOnlyDictionary<string, BuilderField> fields, BuilderTarget target, out string message)
        {
            message = null;
            var items = argument.Many
                ? token switch
                {
                    JArray array => array.ToList(),
                    { Type: JTokenType.String } => token.Value<string>()!.Split(',')
                        .Select(x => x.Trim()).Where(x => x.Length > 0).Select(x => (JToken)new JValue(x)).ToList(),
                    _ => [token]
                }
                : [token];

            if (items.Count == 0 || items.Count > MaximumRules)
            {
                message = $"'{argument.Name}' takes between 1 and {MaximumRules} values.";
                return null;
            }

            var literals = new List<JToken>();
            var references = new List<string>();
            foreach (var item in items)
            {
                if (Literal(argument.Kind, field, item, out var literal, out message))
                {
                    literals.Add(literal);
                    continue;
                }

                var name = item?.Type == JTokenType.String ? item.Value<string>()!.Trim() : null;
                var wanted = Referable(argument.Kind, field.Type);
                if (name == null || wanted.Count == 0 || !fields.TryGetValue(name, out var other) ||
                    !wanted.Contains(other.Type) || (argument.Kind != BuilderArgumentKind.List &&
                                                     argument.Kind != BuilderArgumentKind.Field &&
                                                     target != BuilderTarget.RuleText))
                {
                    message ??= $"'{argument.Name}' is not valid.";
                    if (wanted.Count > 0 && target == BuilderTarget.RuleText)
                    {
                        message += " It may also name a " +
                                   string.Join(" or ", wanted.Select(w => w.ToString().ToLowerInvariant())) +
                                   " field, e.g. Payload.Amount.";
                    }

                    return null;
                }

                references.Add(name);
            }

            if (literals.Count > 0 && references.Count > 0)
            {
                message = $"'{argument.Name}' cannot mix values and fields.";
                return null;
            }

            message = null;
            return new BuilderValue(argument, literals, references);
        }

        private static IReadOnlyList<BuilderFieldType> Referable(BuilderArgumentKind kind, BuilderFieldType type)
        {
            return kind switch
            {
                BuilderArgumentKind.List => [BuilderFieldType.List],
                BuilderArgumentKind.Field => type == BuilderFieldType.List ? [BuilderFieldType.String] : [type],
                BuilderArgumentKind.Number => [BuilderFieldType.Double, BuilderFieldType.Integer],
                BuilderArgumentKind.Integer => [BuilderFieldType.Integer],
                BuilderArgumentKind.Date => [BuilderFieldType.DateTime],
                BuilderArgumentKind.Value => type switch
                {
                    BuilderFieldType.Double => [BuilderFieldType.Double, BuilderFieldType.Integer],
                    BuilderFieldType.Integer => [BuilderFieldType.Integer],
                    BuilderFieldType.DateTime => [BuilderFieldType.DateTime],
                    _ => []
                },
                _ => []
            };
        }

        private static bool Literal(BuilderArgumentKind kind, BuilderField field, JToken token, out JToken literal,
            out string message)
        {
            literal = null;
            message = null;
            var resolved = kind != BuilderArgumentKind.Value
                ? kind
                : field.Type switch
                {
                    BuilderFieldType.Integer => BuilderArgumentKind.Integer,
                    BuilderFieldType.Double => BuilderArgumentKind.Number,
                    BuilderFieldType.DateTime => BuilderArgumentKind.Date,
                    BuilderFieldType.String => BuilderArgumentKind.Text,
                    _ => BuilderArgumentKind.Value
                };
            var text = token?.Type == JTokenType.String ? token.Value<string>() : null;

            switch (resolved)
            {
                case BuilderArgumentKind.Text:
                    if (token?.Type is JTokenType.Integer or JTokenType.Float)
                    {
                        text = token.ToString(Formatting.None);
                    }

                    if (text == null)
                    {
                        message = $"The value for {field.Id} must be text.";
                        return false;
                    }

                    if (text.Any(char.IsControl))
                    {
                        message = $"The value for {field.Id} cannot contain line breaks or other control characters.";
                        return false;
                    }

                    literal = new JValue(text);
                    return true;
                case BuilderArgumentKind.Integer:
                    if (token?.Type == JTokenType.Integer)
                    {
                        literal = token;
                        return true;
                    }

                    if (text != null && long.TryParse(text.Trim(), NumberStyles.AllowLeadingSign,
                            CultureInfo.InvariantCulture, out var whole))
                    {
                        literal = new JValue(whole);
                        return true;
                    }

                    message = $"The value for {field.Id} must be a whole number.";
                    return false;
                case BuilderArgumentKind.Number:
                    if (token?.Type is JTokenType.Integer or JTokenType.Float)
                    {
                        literal = token;
                        return true;
                    }

                    if (text != null && long.TryParse(text.Trim(), NumberStyles.AllowLeadingSign,
                            CultureInfo.InvariantCulture, out var integral))
                    {
                        literal = new JValue(integral);
                        return true;
                    }

                    if (text != null && double.TryParse(text.Trim(), NumberStyles.Float,
                            CultureInfo.InvariantCulture, out var number) && double.IsFinite(number))
                    {
                        literal = new JValue(number);
                        return true;
                    }

                    message = $"The value for {field.Id} must be a number.";
                    return false;
                case BuilderArgumentKind.Date:
                    if (text != null && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal, out var date))
                    {
                        literal = new JValue(date.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'",
                            CultureInfo.InvariantCulture));
                        return true;
                    }

                    message =
                        $"The value for {field.Id} must be a date and time as text, e.g. \"2026-09-01T00:00:00Z\".";
                    return false;
                case BuilderArgumentKind.List:
                    message = "The value must name one of the model's lists, e.g. List.HighRisk.";
                    return false;
                case BuilderArgumentKind.Field:
                    message = field.Type == BuilderFieldType.List
                        ? $"The value for {field.Id} must be the id of a string field whose value is looked up in " +
                          "the list, e.g. Payload.Country."
                        : $"The value for {field.Id} must be the id of another {field.Type.ToString().ToLowerInvariant()} field.";
                    return false;
                default:
                    if (field.Type == BuilderFieldType.Boolean && text is "True" or "False")
                    {
                        literal = new JValue(text);
                        return true;
                    }

                    if (field.Type == BuilderFieldType.Guid && text != null && Guid.TryParse(text, out _))
                    {
                        literal = new JValue(text);
                        return true;
                    }

                    message = field.Type == BuilderFieldType.Boolean
                        ? $"The value for {field.Id} must be \"True\" or \"False\"."
                        : $"The value for {field.Id} must be a {field.Type.ToString().ToLowerInvariant()}.";
                    return false;
            }
        }

        private static void RejectUnknownKeys(JObject node, HashSet<string> allowed, string path,
            List<BuilderError> errors)
        {
            foreach (var property in node.Properties().Where(p => !allowed.Contains(p.Name)))
            {
                errors.Add(new BuilderError($"{path}.{property.Name}", "KeyUnknown",
                    $"'{property.Name}' is not part of the builder format."));
            }
        }
    }
}