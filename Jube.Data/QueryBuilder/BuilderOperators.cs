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
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using X = Dictionary.Extensions.Extensions;

    public sealed record BuilderArgument(string Name, BuilderArgumentKind Kind, bool Many = false);

    public sealed record BuilderOperator(
        string Type,
        string Label,
        string Group,
        IReadOnlyList<BuilderArgument> Arguments,
        string Template,
        IReadOnlyList<BuilderFieldType> RuleTextTypes,
        IReadOnlyDictionary<BuilderFieldType, Func<object, IReadOnlyList<object>, bool>> Evaluators)
    {
        public int Inputs => Arguments.Count;

        public IEnumerable<BuilderFieldType> ApplyTo => RuleTextTypes.Union(Evaluators.Keys).Distinct();

        public bool InMemoryCapable =>
            Arguments.All(a => a.Kind is not (BuilderArgumentKind.List or BuilderArgumentKind.Field));

        public bool Supports(BuilderFieldType type, BuilderTarget target)
        {
            return target == BuilderTarget.RuleText
                ? Template != null && RuleTextTypes.Contains(type)
                : InMemoryCapable && Evaluators.ContainsKey(type);
        }
    }

    public static class BuilderOperators
    {
        public const string Basic = "Basic";
        public const string Lists = "Lists";
        public const string CompareToField = "Compare to another field";
        public const string TextComparison = "Text: comparison";
        public const string TextContains = "Text: contains and patterns";
        public const string TextLists = "Text: model lists";
        public const string TextShape = "Text: shape and length";
        public const string TextValidation = "Text: validation";
        public const string TextEmail = "Text: email";
        public const string TextNetwork = "Text: network";
        public const string TextFuzzy = "Text: fuzzy similarity";
        public const string NumberComparison = "Number: comparison and value";
        public const string NumberThresholds = "Number: thresholds and limits";
        public const string NumberChange = "Number: change and growth";
        public const string NumberRatios = "Number: ratios and shares";
        public const string NumberStatistics = "Number: statistics";
        public const string NumberGeography = "Number: geography";
        public const string DateComparison = "Date: comparison and windows";
        public const string DateCalendar = "Date: calendar";
        public const string DateTimeOfDay = "Date: time of day and business hours (UTC)";
        public const string DateZones = "Date: time zones";
        public const string DateRelative = "Date: relative to when the rule runs";
        public const string Boolean = "Boolean";

        public static IReadOnlyList<string> GroupOrder { get; } =
        [
            Basic, Lists, CompareToField, TextComparison, TextContains, TextLists, TextShape, TextValidation,
            TextEmail, TextNetwork, TextFuzzy, NumberComparison, NumberThresholds, NumberChange, NumberRatios,
            NumberStatistics, NumberGeography, DateComparison, DateCalendar, DateTimeOfDay, DateZones, DateRelative,
            Boolean
        ];

        private static readonly BuilderFieldType[] strings = [BuilderFieldType.String];

        private static readonly BuilderFieldType[] numbersAndDates =
            [BuilderFieldType.Integer, BuilderFieldType.Double, BuilderFieldType.DateTime];

        private static readonly BuilderFieldType[] scalars =
            [BuilderFieldType.String, BuilderFieldType.Integer, BuilderFieldType.Double, BuilderFieldType.DateTime];

        private static readonly BuilderFieldType[] listable =
            [BuilderFieldType.String, BuilderFieldType.Integer, BuilderFieldType.Double];

        private static readonly BuilderArgument valueArgument = new("value", BuilderArgumentKind.Value);
        private static readonly BuilderArgument valuesArgument = new("values", BuilderArgumentKind.Value, true);

        private static readonly HashSet<string> basicSteps =
        [
            "Equal", "NotEqual", "Greater", "GreaterOrEqual", "Less", "LessOrEqual", "In", "NotIn", "InRange",
            "OutsideRange", "Contains", "StartsWith", "EndsWith", "IsNullOrEmpty", "IsNotNullOrEmpty", "IsNull"
        ];

        private static readonly Dictionary<string, string> renamed = new(StringComparer.Ordinal)
        {
            ["Between"] = "between_exclusive",
            ["IsEmpty"] = "is_empty_string"
        };

        private static readonly Dictionary<string, string> words = new(StringComparer.OrdinalIgnoreCase)
        {
            ["iban"] = "IBAN", ["bic"] = "BIC", ["ip"] = "IP", ["km"] = "km", ["mph"] = "mph", ["utc"] = "UTC",
            ["luhn"] = "Luhn", ["soundex"] = "Soundex", ["jaro"] = "Jaro", ["winkler"] = "Winkler",
            ["levenshtein"] = "Levenshtein", ["damerau"] = "Damerau", ["dice"] = "Dice", ["shannon"] = "Shannon",
            ["herfindahl"] = "Herfindahl", ["nan"] = "NaN"
        };

        private static readonly Dictionary<Type, BuilderFieldType> receivers = new()
        {
            [typeof(string)] = BuilderFieldType.String,
            [typeof(int)] = BuilderFieldType.Integer,
            [typeof(double)] = BuilderFieldType.Double,
            [typeof(DateTime)] = BuilderFieldType.DateTime,
            [typeof(bool)] = BuilderFieldType.Boolean
        };

        public static IReadOnlyList<BuilderOperator> All { get; } = Build();

        public static IReadOnlyDictionary<string, BuilderOperator> ByType { get; } =
            All.ToDictionary(o => o.Type, StringComparer.Ordinal);

        public static IReadOnlyList<BuilderOperator> For(BuilderFieldType type, BuilderTarget target)
        {
            return All.Where(o => o.Supports(type, target)).ToList();
        }

        private static List<BuilderOperator> Build()
        {
            var operators = new List<BuilderOperator>();
            operators.AddRange(BasicOperators());
            operators.Add(new BuilderOperator(BuilderProfile.Has, "has", Lists,
                [new BuilderArgument("field", BuilderArgumentKind.Field)], ".contains(?)", [BuilderFieldType.List],
                Evaluate()));
            operators.AddRange(FieldComparisons());
            operators.AddRange(PipelineOperators());
            operators.AddRange(ExtensionOperators());

            var duplicate = operators.GroupBy(o => o.Type).FirstOrDefault(g => g.Count() > 1);
            if (duplicate != null)
            {
                throw new InvalidOperationException($"The operator {duplicate.Key} is defined twice.");
            }

            var order = GroupOrder.ToList();
            return operators
                .Select((o, i) => (Operator: o, Index: i))
                .OrderBy(x => order.IndexOf(x.Operator.Group))
                .ThenBy(x => x.Operator.Group == Basic ? x.Index : 0)
                .ThenBy(x => x.Operator.Label, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.Operator)
                .ToList();
        }

        private static IEnumerable<BuilderOperator> BasicOperators()
        {
            BuilderFieldType[] comparable =
            [
                BuilderFieldType.String, BuilderFieldType.Integer, BuilderFieldType.Double,
                BuilderFieldType.DateTime, BuilderFieldType.Boolean, BuilderFieldType.Guid
            ];
            BuilderFieldType[] equatable =
            [
                BuilderFieldType.String, BuilderFieldType.Integer, BuilderFieldType.Double,
                BuilderFieldType.DateTime, BuilderFieldType.Boolean
            ];
            BuilderArgument[] range =
                [new("from", BuilderArgumentKind.Value), new("to", BuilderArgumentKind.Value)];

            yield return new BuilderOperator(BuilderProfile.Equal, "equal", Basic, [valueArgument], " = ?", equatable,
                Evaluate(comparable, (v, a) => Compare(v, a[0]) == 0));
            yield return new BuilderOperator(BuilderProfile.NotEqual, "not equal", Basic, [valueArgument], " <> ?",
                equatable, Evaluate(comparable, (v, a) => Compare(v, a[0]) != 0));
            yield return new BuilderOperator(BuilderProfile.In, "in", Basic, [valuesArgument],
                ".MatchIn(?).ToBoolean()",
                listable, Evaluate(listable, (v, a) => Many(a[0]).Any(x => Compare(v, x) == 0)));
            yield return new BuilderOperator(BuilderProfile.NotIn, "not in", Basic, [valuesArgument],
                ".MatchNotIn(?).ToBoolean()", listable,
                Evaluate(listable, (v, a) => !Many(a[0]).Any(x => Compare(v, x) == 0)));
            yield return new BuilderOperator(BuilderProfile.Less, "less", Basic, [valueArgument], " < ?",
                numbersAndDates,
                Evaluate(numbersAndDates, (v, a) => Compare(v, a[0]) < 0));
            yield return new BuilderOperator(BuilderProfile.LessOrEqual, "less or equal", Basic, [valueArgument],
                " <= ?",
                numbersAndDates, Evaluate(numbersAndDates, (v, a) => Compare(v, a[0]) <= 0));
            yield return new BuilderOperator(BuilderProfile.Greater, "greater", Basic, [valueArgument], " > ?",
                numbersAndDates, Evaluate(numbersAndDates, (v, a) => Compare(v, a[0]) > 0));
            yield return new BuilderOperator(BuilderProfile.GreaterOrEqual, "greater or equal", Basic, [valueArgument],
                " >= ?", numbersAndDates, Evaluate(numbersAndDates, (v, a) => Compare(v, a[0]) >= 0));
            yield return new BuilderOperator(BuilderProfile.Between, "between", Basic, range,
                ".MatchInRange(?).ToBoolean()", numbersAndDates,
                Evaluate(numbersAndDates, (v, a) => Compare(v, a[0]) >= 0 && Compare(v, a[1]) <= 0));
            yield return new BuilderOperator(BuilderProfile.NotBetween, "not between", Basic, range,
                ".MatchInRange(?).ToBoolean() = False", numbersAndDates,
                Evaluate(numbersAndDates, (v, a) => !(Compare(v, a[0]) >= 0 && Compare(v, a[1]) <= 0)));
            yield return new BuilderOperator(BuilderProfile.BeginsWith, "begins with", Basic, [valueArgument],
                ".StartsWith(?)", strings,
                Evaluate(strings, (v, a) => S(v).StartsWith(S(a[0]), StringComparison.Ordinal)));
            yield return new BuilderOperator(BuilderProfile.NotBeginsWith, "not begins with", Basic, [valueArgument],
                ".StartsWith(?) = False", strings,
                Evaluate(strings, (v, a) => !S(v).StartsWith(S(a[0]), StringComparison.Ordinal)));
            yield return new BuilderOperator(BuilderProfile.Contains, "contains", Basic, [valueArgument],
                ".Contains(?)",
                strings, Evaluate(strings, (v, a) => S(v).Contains(S(a[0]), StringComparison.Ordinal)));
            yield return new BuilderOperator(BuilderProfile.NotContains, "not contains", Basic, [valueArgument],
                ".MatchContainsNone(?).ToBoolean()", strings,
                Evaluate(strings, (v, a) => !S(v).Contains(S(a[0]), StringComparison.Ordinal)));
            yield return new BuilderOperator(BuilderProfile.EndsWith, "ends with", Basic, [valueArgument],
                ".EndsWith(?)",
                strings, Evaluate(strings, (v, a) => S(v).EndsWith(S(a[0]), StringComparison.Ordinal)));
            yield return new BuilderOperator(BuilderProfile.NotEndsWith, "not ends with", Basic, [valueArgument],
                ".EndsWith(?) = False", strings,
                Evaluate(strings, (v, a) => !S(v).EndsWith(S(a[0]), StringComparison.Ordinal)));
            yield return new BuilderOperator(BuilderProfile.IsEmpty, "is empty", Basic, [],
                ".MatchIsNullOrEmpty().ToBoolean()", strings,
                Evaluate(strings, (v, _) => string.IsNullOrEmpty(v as string)));
            yield return new BuilderOperator(BuilderProfile.IsNotEmpty, "is not empty", Basic, [],
                ".MatchIsNotNullOrEmpty().ToBoolean()", strings,
                Evaluate(strings, (v, _) => !string.IsNullOrEmpty(v as string)));
            yield return new BuilderOperator(BuilderProfile.IsNull, "is null", Basic, [],
                ".MatchIsNull().ToBoolean()", strings, Evaluate(strings, (v, _) => v == null));
            yield return new BuilderOperator(BuilderProfile.IsNotNull, "is not null", Basic, [], ".IsNotNull()",
                strings, Evaluate(strings, (v, _) => v != null));
        }

        private static IEnumerable<BuilderOperator> FieldComparisons()
        {
            BuilderArgument[] field = [new("field", BuilderArgumentKind.Field)];
            yield return new BuilderOperator("equal_field", "Equals another field", CompareToField, field, " = ?",
                scalars, Evaluate());
            yield return new BuilderOperator("not_equal_field", "Does not equal another field", CompareToField,
                field, " <> ?", scalars, Evaluate());
            yield return new BuilderOperator("greater_field", "Greater than another field", CompareToField, field,
                " > ?", numbersAndDates, Evaluate());
            yield return new BuilderOperator("greater_or_equal_field", "Greater than or equal to another field",
                CompareToField, field, " >= ?", numbersAndDates, Evaluate());
            yield return new BuilderOperator("less_field", "Less than another field", CompareToField, field, " < ?",
                numbersAndDates, Evaluate());
            yield return new BuilderOperator("less_or_equal_field", "Less than or equal to another field",
                CompareToField, field, " <= ?", numbersAndDates, Evaluate());
        }

        private static IEnumerable<BuilderOperator> ExtensionOperators()
        {
            BuilderArgument[] none = [];
            BuilderArgument[] cidr = [new("CIDR range", BuilderArgumentKind.Text)];
            BuilderArgument[] path = [new("JSON path", BuilderArgumentKind.Text)];
            (string Type, string Label, string Group, BuilderArgument[] Arguments, string Method,
                Func<string, IReadOnlyList<object>, bool> Test)[] definitions =
                [
                    ("is_valid_aba_routing_number", "Is a valid ABA routing number", TextValidation, none,
                        "IsValidAbaRoutingNumber", (v, _) => X.IsValidAbaRoutingNumber(v)),
                    ("is_valid_country_code", "Is a valid ISO country code", TextValidation, none,
                        "IsValidCountryCode", (v, _) => X.IsValidCountryCode(v)),
                    ("is_valid_currency_code", "Is a valid ISO currency code", TextValidation, none,
                        "IsValidCurrencyCode", (v, _) => X.IsValidCurrencyCode(v)),
                    ("is_valid_cusip", "Is a valid CUSIP", TextValidation, none, "IsValidCusip",
                        (v, _) => X.IsValidCusip(v)),
                    ("is_valid_domain_name", "Is a valid domain name", TextValidation, none, "IsValidDomainName",
                        (v, _) => X.IsValidDomainName(v)),
                    ("is_valid_isin", "Is a valid ISIN", TextValidation, none, "IsValidIsin",
                        (v, _) => X.IsValidIsin(v)),
                    ("is_valid_json", "Is valid JSON", TextValidation, none, "IsValidJson",
                        (v, _) => X.IsValidJson(v)),
                    ("is_valid_lei", "Is a valid LEI", TextValidation, none, "IsValidLei", (v, _) => X.IsValidLei(v)),
                    ("is_valid_sedol", "Is a valid SEDOL", TextValidation, none, "IsValidSedol",
                        (v, _) => X.IsValidSedol(v)),
                    ("is_valid_uuid", "Is a valid UUID", TextValidation, none, "IsValidUuid",
                        (v, _) => X.IsValidUuid(v)),
                    ("is_valid_xml", "Is valid XML", TextValidation, none, "IsValidXml", (v, _) => X.IsValidXml(v)),
                    ("json_has_path", "Is JSON with the path", TextContains, path, "JsonHasPath",
                        (v, a) => X.JsonHasPath(v, S(a[0]))),
                    ("is_in_cidr", "Is an IP address in the CIDR range", TextNetwork, cidr, "IsInCidr",
                        (v, a) => X.IsInCidr(v, S(a[0]))),
                    ("is_loopback_ip_address", "Is a loopback IP address", TextNetwork, none, "IsLoopbackIpAddress",
                        (v, _) => X.IsLoopbackIpAddress(v)),
                    ("is_private_ip_address", "Is a private IP address", TextNetwork, none, "IsPrivateIpAddress",
                        (v, _) => X.IsPrivateIpAddress(v)),
                    ("is_public_ip_address", "Is a public IP address", TextNetwork, none, "IsPublicIpAddress",
                        (v, _) => X.IsPublicIpAddress(v)),
                    ("is_reserved_ip_address", "Is a reserved IP address", TextNetwork, none, "IsReservedIpAddress",
                        (v, _) => X.IsReservedIpAddress(v))
                ];

            foreach (var definition in definitions)
            {
                var test = definition.Test;
                yield return new BuilderOperator(definition.Type, definition.Label, definition.Group,
                    definition.Arguments,
                    "." + definition.Method + (definition.Arguments.Length == 0 ? "()" : "(?)"), strings,
                    Evaluate(strings, (v, a) => test(v as string ?? string.Empty, a)));
            }
        }

        private static IEnumerable<BuilderOperator> PipelineOperators()
        {
            var steps = typeof(X).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name.Length > 5 && m.Name.StartsWith("Match", StringComparison.Ordinal) &&
                            char.IsUpper(m.Name[5]) && m.Name != "Matches")
                .Select(m => (Method: m, Parameters: m.GetParameters()))
                .Where(m => m.Parameters.Length > 0 && receivers.ContainsKey(m.Parameters[0].ParameterType))
                .Select(m => (m.Method, m.Parameters, Name: m.Method.Name[5..],
                    Receiver: receivers[m.Parameters[0].ParameterType], Arguments: Arguments(m.Parameters)))
                .Where(m => m.Arguments != null && !basicSteps.Contains(m.Name))
                .ToList();

            foreach (var family in steps.GroupBy(s =>
                         (s.Name, Signature: Signature(s.Arguments), Group: Group(s.Receiver, s.Name))))
            {
                var name = family.Key.Name;
                var shared = steps.Count(s => s.Name == name) != family.Count();
                var key = (renamed.TryGetValue(name, out var rename) ? rename : Snake(name)) +
                          (shared ? "_" + Snake(family.Key.Group.Split(':')[0]) : string.Empty);
                var arguments = family.First().Arguments;
                var evaluators = family.ToDictionary(s => s.Receiver, s => Invoker(s.Method, s.Parameters));

                yield return new BuilderOperator(key, Label(name), family.Key.Group, arguments,
                    ".Match" + name + (arguments.Count == 0 ? "()" : "(?)") + ".ToBoolean()",
                    family.Select(s => s.Receiver).Distinct().ToList(), evaluators);
            }
        }

        private static List<BuilderArgument> Arguments(ParameterInfo[] parameters)
        {
            var receiver = parameters[0].ParameterType;
            var arguments = new List<BuilderArgument>();
            foreach (var parameter in parameters.Skip(1))
            {
                var many = parameter.GetCustomAttribute<ParamArrayAttribute>() != null;
                var type = many ? parameter.ParameterType.GetElementType()! : parameter.ParameterType;
                BuilderArgumentKind? kind = type == receiver ? BuilderArgumentKind.Value
                    : type == typeof(double) ? BuilderArgumentKind.Number
                    : type == typeof(int) ? BuilderArgumentKind.Integer
                    : type == typeof(string) ? BuilderArgumentKind.Text
                    : type == typeof(DateTime) ? BuilderArgumentKind.Date
                    : type == typeof(IEnumerable<string>) ? BuilderArgumentKind.List
                    : null;
                if (kind == null)
                {
                    return null;
                }

                arguments.Add(new BuilderArgument(Words(parameter.Name!), kind.Value, many));
            }

            return arguments;
        }

        private static string Signature(IEnumerable<BuilderArgument> arguments)
        {
            return string.Join(",", arguments.Select(a => a.Kind + (a.Many ? "*" : string.Empty)));
        }

        private static Func<object, IReadOnlyList<object>, bool> Invoker(MethodInfo method,
            ParameterInfo[] parameters)
        {
            var toBoolean = method.ReturnType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == "op_Implicit" && m.ReturnType == typeof(bool));

            return (value, arguments) =>
            {
                var call = new object[parameters.Length];
                call[0] = To(value, parameters[0].ParameterType);
                for (var i = 1; i < parameters.Length; i++)
                {
                    var type = parameters[i].ParameterType;
                    if (type.IsArray)
                    {
                        var element = type.GetElementType()!;
                        var items = Many(arguments[i - 1]).Select(x => To(x, element)).ToArray();
                        var array = Array.CreateInstance(element, items.Length);
                        Array.Copy(items, array, items.Length);
                        call[i] = array;
                    }
                    else
                    {
                        call[i] = To(arguments[i - 1], type);
                    }
                }

                return (bool)toBoolean.Invoke(null, [method.Invoke(null, call)])!;
            };
        }

        private static object To(object value, Type type)
        {
            if (value == null || type.IsInstanceOfType(value))
            {
                return value;
            }

            return type == typeof(DateTime) && value is string date
                ? X.ToIsoDateTime(date)
                : Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }

        private static string Group(BuilderFieldType receiver, string name)
        {
            bool Has(params string[] parts)
            {
                return parts.Any(p => name.Contains(p, StringComparison.Ordinal));
            }

            switch (receiver)
            {
                case BuilderFieldType.Boolean:
                    return Boolean;
                case BuilderFieldType.DateTime:
                    if (Has("InZone"))
                    {
                        return DateZones;
                    }

                    if (Has("IsPast", "IsFuture", "IsToday", "OlderThan", "YoungerThan"))
                    {
                        return DateRelative;
                    }

                    if (Has("IsMorning", "IsAfternoon", "BusinessHours"))
                    {
                        return DateTimeOfDay;
                    }

                    return Has("IsWeekday", "IsWeekend", "OfMonth", "DayOfWeek") ? DateCalendar : DateComparison;
                case BuilderFieldType.String:
                    if (Has("Fuzzy", "Soundex", "JaroWinkler", "Levenshtein", "Similar", "Dice", "Damerau",
                            "TokenSet", "TokenSort", "Somehow", "Initials", "Normalised"))
                    {
                        return TextFuzzy;
                    }

                    if (Has("Email"))
                    {
                        return TextEmail;
                    }

                    if (Has("InList"))
                    {
                        return TextLists;
                    }

                    if (Has("Valid"))
                    {
                        return Has("ValidIp") ? TextNetwork : TextValidation;
                    }

                    if (Has("IsEmpty", "IsNull", "IsNotNull", "IsNumeric", "IsAlpha", "IsAllSame", "Length",
                            "Sequential", "Entropy"))
                    {
                        return TextShape;
                    }

                    return Has("Contains", "StartsWith", "EndsWith", "Matches") ? TextContains : TextComparison;
                default:
                    if (Has("DistanceKm", "DistanceMiles", "Radius", "ImpliedSpeed"))
                    {
                        return NumberGeography;
                    }

                    if (Has("ZScore", "CoefficientOfVariation", "DeviationFromMean", "MinMaxNormalise",
                            "PercentOfRange", "MeanWith", "MinWith", "MaxWith", "SumWith", "SpreadWith",
                            "EntropyOfShares", "Herfindahl"))
                    {
                        return NumberStatistics;
                    }

                    if (Has("IsJust", "IsRoundAmount", "Threshold", "ExcessOver", "HeadroomTo", "ShortfallBelow"))
                    {
                        return NumberThresholds;
                    }

                    if (Has("IncreasedBy", "DecreasedBy", "DeviatesFrom", "WithinPercentOf", "ChangeFrom",
                            "GrowthFactor", "GrowthRate", "SlopeFrom", "RetentionAfter", "DifferenceFrom"))
                    {
                        return NumberChange;
                    }

                    return Has("Ratio", "PercentOf", "ShareOf", "Complement", "Imbalance")
                        ? NumberRatios
                        : NumberComparison;
            }
        }

        private static string Label(string name)
        {
            if (name.Contains("IgnoreCase", StringComparison.Ordinal))
            {
                return Label(name.Replace("IgnoreCase", string.Empty, StringComparison.Ordinal)) + ", ignoring case";
            }

            foreach (var suffix in new[] { "OutsideRange", "InRange", "Outside", "Above", "Below" })
            {
                if (name.Length > suffix.Length && name.EndsWith(suffix, StringComparison.Ordinal) &&
                    !name.StartsWith("Is", StringComparison.Ordinal) && name != "Ratio" + suffix)
                {
                    return Capital(Words(name[..^suffix.Length])) + ": " + Words(suffix);
                }
            }

            var label = Capital(Words(name));
            return name == "Between" ? label + " (exclusive)" : label;
        }

        private static string Words(string name)
        {
            var parts = Regex.Matches(name, "[A-Z]?[a-z]+|[A-Z]+(?![a-z])|[0-9]+").Select(m => m.Value);
            return string.Join(" ",
                parts.Select(p => words.TryGetValue(p, out var word) ? word : p.ToLowerInvariant()));
        }

        private static string Capital(string text)
        {
            return text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];
        }

        private static string Snake(string name)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < name.Length; i++)
            {
                if (char.IsUpper(name[i]) && i > 0 &&
                    (!char.IsUpper(name[i - 1]) || (i + 1 < name.Length && char.IsLower(name[i + 1]))))
                {
                    sb.Append('_');
                }

                sb.Append(char.ToLowerInvariant(name[i]));
            }

            return sb.ToString();
        }

        private static IReadOnlyDictionary<BuilderFieldType, Func<object, IReadOnlyList<object>, bool>> Evaluate(
            IEnumerable<BuilderFieldType> types = null, Func<object, IReadOnlyList<object>, bool> test = null)
        {
            return test == null
                ? new Dictionary<BuilderFieldType, Func<object, IReadOnlyList<object>, bool>>()
                : types!.ToDictionary(t => t, _ => test);
        }

        public static IEnumerable<object> Many(object value)
        {
            return value switch
            {
                null => [],
                string s => [s],
                IEnumerable items => items.Cast<object>(),
                _ => [valueArgument]
            };
        }

        private static string S(object value)
        {
            return value as string ?? string.Empty;
        }

        private static double D(object value)
        {
            return Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }

        private static int Compare(object value, object argument)
        {
            return value switch
            {
                string s => string.CompareOrdinal(s, argument as string),
                bool b => b.CompareTo(argument is bool other && other),
                Guid g => argument is Guid other ? g.CompareTo(other) : 1,
                DateTime t => argument is DateTime other ? t.CompareTo(other) : 1,
                _ => D(value).CompareTo(D(argument))
            };
        }
    }
}