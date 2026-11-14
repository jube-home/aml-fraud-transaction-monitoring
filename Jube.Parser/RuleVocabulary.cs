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
    using GraphRecallProtocol;
    using log4net;
    using Models;

    public sealed record RuleWordParameter(
        string Name,
        string Type,
        bool Optional,
        string DefaultValue,
        string Description = null);

    public sealed record RuleWordOverload(
        string Signature,
        string ReceiverType,
        string ReturnType,
        IReadOnlyList<RuleWordParameter> Parameters,
        string Description = null);

    public sealed record RuleWord(
        string Name,
        RuleWordKind Kind,
        IReadOnlyList<RuleWordOverload> Overloads,
        string Description = null);

    public static class RuleVocabulary
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(RuleVocabulary));
        private static readonly Lazy<Dictionary<string, List<RuleWordOverload>>> functions = new(Functions);
        private static readonly Lazy<Dictionary<string, List<RuleWordOverload>>> graphWords = new(GraphWords);
        private static readonly Lazy<List<RuleWord>> cursorWords = new(CursorWords);

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

        public static IReadOnlyList<RuleWord> Build(IEnumerable<string> installationTokens, int? ruleParseType)
        {
            var words = Build(installationTokens).ToList();

            if (ruleParseType != RuleParse.AbstractionRule)
            {
                return words;
            }

            foreach (var cursorWord in cursorWords.Value)
            {
                var existing = words.FindIndex(word =>
                    string.Equals(word.Name, cursorWord.Name, StringComparison.OrdinalIgnoreCase));

                if (existing < 0)
                {
                    words.Add(cursorWord);
                }
                else
                {
                    words[existing] = words[existing] with
                    {
                        Overloads = [.. words[existing].Overloads, .. cursorWord.Overloads],
                        Description = words[existing].Description ?? cursorWord.Description
                    };
                }
            }

            return [.. words.OrderBy(word => word.Name, StringComparer.OrdinalIgnoreCase)];
        }

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
                    words.Add(new RuleWord(name, RuleWordKind.Function, overloads,
                        overloads.Select(o => o.Description).FirstOrDefault(d => d != null)));
                }
                else if (graphWords.Value.TryGetValue(name, out var graphOverloads))
                {
                    words.Add(new RuleWord(name, RuleWordKind.Keyword, graphOverloads));
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

        private static List<RuleWord> CursorWords()
        {
            const string receiver = "Cursor";

            var type = typeof(AbstractionCursor);
            var allowed = Parser.CursorTokens.ToHashSet(StringComparer.OrdinalIgnoreCase);

            var words = new List<RuleWord>
            {
                new(receiver, RuleWordKind.Keyword,
                    [new RuleWordOverload($"{receiver} As {type.Name}", string.Empty, type.Name, [])],
                    "Where the rule is in its walk through the history for the Search Key. Abstraction rules only.")
            };

            words.AddRange(type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => allowed.Contains(p.Name))
                .Select(property => CursorPropertyWord(property, receiver, type.Name)));

            words.AddRange(type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName && allowed.Contains(m.Name))
                .Select(method => CursorMethodWord(method, receiver, type.Name)));

            words.AddRange(Parser.CursorTokens.Where(token => token.Contains(' '))
                .Select(token => new RuleWord(token, RuleWordKind.Keyword, [], ScanControlDescription(token))));

            words.Add(new RuleWord("Dim", RuleWordKind.Keyword, [],
                $"Declares a variable that lives for the transaction being tested and starts afresh for the next: Dim name As Type, on a line of its own, optionally followed by = and a starting value. Type is {VariableTypes}. Abstraction rules only."));

            words.Add(new RuleWord("Static", RuleWordKind.Keyword, [],
                $"Declares a variable that keeps its value from one transaction to the next for the length of the walk and starts afresh for every walk: Static name As Type, on a line of its own, optionally followed by = and a literal starting value. Type is {VariableTypes}. Abstraction rules only."));

            words.Add(new RuleWord("As", RuleWordKind.Keyword, [],
                "Names the type of a variable in a Dim or Static declaration."));

            words.AddRange(RuleVariableDeclarations.Types.Select(name => new RuleWord(name, RuleWordKind.Keyword,
                [], "A type a Dim or Static variable can have.")));

            return words;
        }

        private static string VariableTypes => string.Join(", ", RuleVariableDeclarations.Types);

        private static string ScanControlDescription(string token)
        {
            return token.StartsWith("Exit", StringComparison.OrdinalIgnoreCase)
                ? "Finishes with this transaction and ends the walk; no further transactions are tested. The transaction is accepted if Matched was already set to True."
                : "Finishes with this transaction and moves to the next. The transaction is accepted if Matched was already set to True.";
        }

        private static string DescriptionOf(MemberInfo member)
        {
            return member.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description;
        }

        private static string DescriptionOf(ParameterInfo parameter)
        {
            return parameter.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description;
        }

        private static RuleWord CursorPropertyWord(PropertyInfo property, string receiver, string receiverType)
        {
            var returnType = TypeName(property.PropertyType);

            return new RuleWord(property.Name, RuleWordKind.Keyword,
                [new RuleWordOverload($"{receiver}.{property.Name} As {returnType}", receiverType, returnType, [])],
                DescriptionOf(property));
        }

        private static RuleWord CursorMethodWord(MethodInfo method, string receiver, string receiverType)
        {
            var parameters = method.GetParameters()
                .Select(p => new RuleWordParameter(p.Name ?? string.Empty, TypeName(p.ParameterType), false, null))
                .ToList();

            var returnType = TypeName(method.ReturnType);

            var signature =
                $"{receiver}.{method.Name}({string.Join(", ", parameters.Select(p => $"{p.Name} As {p.Type}"))}) As {returnType}";

            return new RuleWord(method.Name, RuleWordKind.Function,
                [new RuleWordOverload(signature, receiverType, returnType, parameters)], DescriptionOf(method));
        }

        private static Dictionary<string, List<RuleWordOverload>> GraphWords()
        {
            var words = new Dictionary<string, List<RuleWordOverload>>(StringComparer.Ordinal);

            foreach (var (type, receiver) in new[]
                     {
                         (typeof(GraphEntity), "Graph.<Entity>"), (typeof(GraphTagProximity), "Graph.<Entity>.Tag(<Name>)"),
                         (typeof(GraphEdgeFacts), "Graph.<Entity>.Edge")
                     })
            {
                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                             .Where(p => p.Name is not (nameof(GraphEntity.Beyond) or nameof(GraphEntity.ReportTable))))
                {
                    var returnType = TypeName(property.PropertyType);

                    Add(words, property.Name,
                        new RuleWordOverload($"{receiver}.{property.Name} As {returnType}", receiver, returnType, []));
                }

                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                             .Where(m => !m.IsSpecialName && GraphEntityMembers.IsMethod(m.Name)))
                {
                    var parameters = method.GetParameters()
                        .Select(p => new RuleWordParameter(p.Name ?? string.Empty, TypeName(p.ParameterType), false, null))
                        .ToList();

                    var returnType = TypeName(method.ReturnType);

                    var signature =
                        $"{receiver}.{method.Name}({string.Join(", ", parameters.Select(p => $"{p.Name} As {p.Type}"))}) As {returnType}";

                    Add(words, method.Name, new RuleWordOverload(signature, receiver, returnType, parameters));
                }
            }

            return words;
        }

        private static void Add(Dictionary<string, List<RuleWordOverload>> words, string name, RuleWordOverload overload)
        {
            if (!words.TryGetValue(name, out var overloads))
            {
                words[name] = overloads = [];
            }

            overloads.Add(overload);
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
                p.IsOptional ? DefaultText(p.DefaultValue) : null, DescriptionOf(p))).ToList();

            var returns = TypeName(method.ReturnType);

            var generic = method.IsGenericMethodDefinition
                ? "(Of " + string.Join(", ", method.GetGenericArguments().Select(a => a.Name)) + ")"
                : string.Empty;

            var list = string.Join(", ", parameters.Select(p =>
                (p.Optional ? "Optional " : string.Empty) + p.Name + " As " + p.Type +
                (p.Optional ? " = " + p.DefaultValue : string.Empty)));

            return new RuleWordOverload($"<{receiver}>.{method.Name}{generic}({list}) As {returns}", receiver,
                returns, parameters, DescriptionOf(method));
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