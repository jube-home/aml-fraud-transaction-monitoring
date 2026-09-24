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
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using Dictionary;
    using log4net;

    public sealed record RuleWordParameter(string Name, string Type, bool Optional, string DefaultValue);

    public sealed record RuleWordOverload(
        string Signature,
        string ReceiverType,
        string ReturnType,
        IReadOnlyList<RuleWordParameter> Parameters);

    public sealed record RuleWord(string Name, RuleWordKind Kind, IReadOnlyList<RuleWordOverload> Overloads);

    public static class RuleVocabulary
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(RuleVocabulary));

        private static readonly Lazy<Dictionary<string, List<RuleWordOverload>>> functions = new(Functions);

        private static readonly Dictionary<Type, string> vbTypeNames = new()
        {
            [typeof(string)] = "String",
            [typeof(int)] = "Integer",
            [typeof(long)] = "Long",
            [typeof(short)] = "Short",
            [typeof(byte)] = "Byte",
            [typeof(double)] = "Double",
            [typeof(float)] = "Single",
            [typeof(decimal)] = "Decimal",
            [typeof(bool)] = "Boolean",
            [typeof(char)] = "Char",
            [typeof(DateTime)] = "DateTime",
            [typeof(object)] = "Object",
            [typeof(void)] = "Nothing"
        };

        public static IReadOnlyList<RuleWord> Build(IEnumerable<string> installationTokens)
        {
            var installation = (installationTokens ?? []).Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
            var builtIn = new Parser(log, []).RuleScriptTokens.ToHashSet(StringComparer.Ordinal);
            var allowed = new Parser(log, [.. installation]).RuleScriptTokens;

            var curated = EvalExpressionRegistry.Enabled
                ? EvalExpressionRegistry.Entries
                : new Dictionary<string, int>();

            var words = new List<RuleWord>();
            var builtInFolded = builtIn.ToHashSet(StringComparer.OrdinalIgnoreCase);

            var names = allowed
                .GroupBy(n => n, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.FirstOrDefault(functions.Value.ContainsKey) ?? g.FirstOrDefault(n => n.Any(char.IsUpper))
                    ?? char.ToUpperInvariant(g.Key[0]) + g.Key[1..])
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase);

            foreach (var name in names)
            {
                if (functions.Value.TryGetValue(name, out var overloads))
                {
                    words.Add(new RuleWord(name, RuleWordKind.Function, overloads));
                }
                else if (curated.TryGetValue(name, out var resultTypeId))
                {
                    var resultType = EvalExpressionRegistry.ResultTypes[resultTypeId].VbKeyword;
                    words.Add(new RuleWord(name, RuleWordKind.CuratedExpression,
                    [
                        new RuleWordOverload($"<String>.{name}() As {resultType}", "String", resultType, [])
                    ]));
                }
                else if (!IsGeneratedName(name))
                {
                    words.Add(new RuleWord(name,
                        builtInFolded.Contains(name) ? RuleWordKind.Keyword : RuleWordKind.AllowedWord, []));
                }
            }

            return words;
        }

        private static bool IsGeneratedName(string name)
        {
            return name.Contains('<') || name.Contains('$') || name.StartsWith("get_", StringComparison.Ordinal) ||
                   name.StartsWith("set_", StringComparison.Ordinal);
        }

        private static Dictionary<string, List<RuleWordOverload>> Functions()
        {
            return typeof(Dictionary.Extensions.Extensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name != "Eval" && !m.IsSpecialName && !IsGeneratedName(m.Name) &&
                            m.IsDefined(typeof(ExtensionAttribute), false))
                .GroupBy(m => m.Name, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g
                    .Select(Overload)
                    .OrderBy(o => o.ReceiverType, StringComparer.Ordinal)
                    .ThenBy(o => o.Parameters.Count)
                    .ThenBy(o => o.Signature, StringComparer.Ordinal)
                    .ToList(), StringComparer.Ordinal);
        }

        private static RuleWordOverload Overload(MethodInfo method)
        {
            var all = method.GetParameters();
            var receiver = TypeName(all[0].ParameterType);
            var parameters = all.Skip(1).Select(p => new RuleWordParameter(p.Name ?? string.Empty,
                TypeName(p.ParameterType), p.IsOptional,
                p.IsOptional ? DefaultText(p.DefaultValue) : null)).ToList();
            var returns = TypeName(method.ReturnType);
            var generic = method.IsGenericMethodDefinition
                ? "(Of " + string.Join(", ", method.GetGenericArguments().Select(a => a.Name)) + ")"
                : string.Empty;
            var list = string.Join(", ", parameters.Select(p =>
                (p.Optional ? "Optional " : string.Empty) + p.Name + " As " + p.Type +
                (p.Optional ? " = " + p.DefaultValue : string.Empty)));

            return new RuleWordOverload($"<{receiver}>.{method.Name}{generic}({list}) As {returns}", receiver,
                returns, parameters);
        }

        private static string DefaultText(object value)
        {
            return value switch
            {
                null => "Nothing",
                string text => "\"" + text + "\"",
                bool flag => flag ? "True" : "False",
                IFormattable formattable => formattable.ToString(null,
                    System.Globalization.CultureInfo.InvariantCulture),
                _ => value.ToString()
            };
        }

        private static string TypeName(Type type)
        {
            if (type.IsByRef)
            {
                type = type.GetElementType()!;
            }

            if (vbTypeNames.TryGetValue(type, out var name))
            {
                return name;
            }

            var underlying = Nullable.GetUnderlyingType(type);
            if (underlying != null)
            {
                return TypeName(underlying) + "?";
            }

            if (type.IsArray)
            {
                return TypeName(type.GetElementType()!) + "()";
            }

            if (type.IsGenericType)
            {
                var baseName = type.Name[..type.Name.IndexOf('`')];
                return baseName + "(Of " + string.Join(", ", type.GetGenericArguments().Select(TypeName)) + ")";
            }

            return type.Name;
        }
    }
}