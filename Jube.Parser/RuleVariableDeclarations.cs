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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Jube.Parser.Models;
using Microsoft.CodeAnalysis.VisualBasic;

namespace Jube.Parser
{
    public static class RuleVariableDeclarations
    {
        public const string DeclarationCode = "VariableDeclaration";
        public const string NameCode = "VariableName";
        public const int MaximumNameLength = 64;
        public const int MaximumVariables = 32;

        private const string RecordKeyword = "Dim";
        private const string ScanKeyword = "Static";

        private static readonly TimeSpan timeout = TimeSpan.FromSeconds(5);

        private static readonly Regex opening = new(@"^[ \t]*(?<kind>Dim|Static)(?![A-Za-z0-9_])",
            RegexOptions.IgnoreCase, timeout);

        private static readonly Regex declaration = new(
            @"^[ \t]*(?<kind>Dim|Static)[ \t]+(?<name>[A-Za-z][A-Za-z0-9]*)[ \t]+As[ \t]+(?<type>Double|Integer|String|Boolean|DateTime)[ \t]*(?:=(?<value>.*))?$",
            RegexOptions.IgnoreCase, timeout);

        private static readonly Regex stringLiteral = new("\"(?:[^\"]|\"\")*\"", RegexOptions.None, timeout);
        private static readonly Regex lineBreak = new("(\r\n|\r|\n)", RegexOptions.None, timeout);
        private static readonly Regex wholeNumber = new(@"^-?\d{1,9}$", RegexOptions.None, timeout);
        private static readonly Regex number = new(@"^-?\d{1,15}(\.\d{1,15})?$", RegexOptions.None, timeout);
        private static readonly Regex text = new("^\"(?:[^\"]|\"\")*\"$", RegexOptions.None, timeout);

        private static readonly string[] types = ["Double", "Integer", "String", "Boolean", "DateTime"];

        private static readonly HashSet<string> reserved = new(StringComparer.OrdinalIgnoreCase)
        {
            "Data", "List", "KVP", "Svirfd", "Cursor", "Log", "Matched", "CursorPass", "Match",
            RuleVariableEmitter.RecordMethod, RuleVariableEmitter.ScanName, "ex", "GatewayRule", "AbstractionRule",
            "Equals", "GetHashCode", "GetType", "ToString", "Finalize", "MemberwiseClone", "ReferenceEquals"
        };

        public static IReadOnlyList<string> Types => types;

        public static bool Any(string ruleText)
        {
            return lineBreak.Split(ruleText ?? string.Empty).Any(line => opening.IsMatch(Mask(line)));
        }

        public static RuleVariableReading Read(string ruleText)
        {
            var parts = lineBreak.Split(ruleText ?? string.Empty);
            var variables = new List<RuleVariable>();
            var errors = new List<ErrorSpan>();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var rebuilt = new StringBuilder();

            for (var part = 0; part < parts.Length; part++)
            {
                if (part % 2 == 1)
                {
                    rebuilt.Append(parts[part]);

                    continue;
                }

                rebuilt.Append(ReadLine(parts[part], part / 2, variables, errors, names));
            }

            return new RuleVariableReading(rebuilt.ToString(), variables, errors);
        }

        private static string ReadLine(string line, int lineIndex, List<RuleVariable> variables,
            List<ErrorSpan> errors, HashSet<string> names)
        {
            var masked = Mask(line);
            var opened = opening.Match(masked);

            if (!opened.Success)
            {
                return line;
            }

            var blank = new string(' ', line.Length);
            var declared = declaration.Match(masked);

            if (!declared.Success)
            {
                errors.Add(Error(DeclarationCode, lineIndex, opened.Groups["kind"].Index, line.Length,
                    $"a variable is declared on a line of its own as {RecordKeyword} name As Type or {ScanKeyword} name As Type, where the name is letters and digits starting with a letter and Type is {string.Join(", ", types)}."));

                return blank;
            }

            var name = declared.Groups["name"];
            var nameError = NameError(name.Value, names, variables.Count);

            if (nameError != null)
            {
                errors.Add(Error(NameCode, lineIndex, name.Index, name.Index + name.Length, nameError));

                return blank;
            }

            names.Add(name.Value);

            var type = types.First(t => string.Equals(t, declared.Groups["type"].Value,
                StringComparison.OrdinalIgnoreCase));

            var value = declared.Groups["value"];

            if (string.Equals(declared.Groups["kind"].Value, RecordKeyword, StringComparison.OrdinalIgnoreCase))
            {
                variables.Add(new RuleVariable(name.Value, type, RuleVariableKind.Record, null, lineIndex));

                if (!value.Success)
                {
                    return blank;
                }

                var assignment = blank.ToCharArray();

                line.CopyTo(name.Index, assignment, name.Index, name.Length);
                line.CopyTo(value.Index - 1, assignment, value.Index - 1, line.Length - value.Index + 1);

                return new string(assignment);
            }

            string initialiser = null;

            if (value.Success)
            {
                initialiser = line[value.Index..].Trim();

                if (!IsLiteralOf(type, initialiser))
                {
                    errors.Add(Error(DeclarationCode, lineIndex, value.Index, line.Length,
                        $"a {ScanKeyword} variable can only start from a literal of its own type: a number for Double or Integer, text in double quotes for String, True or False for Boolean, and nothing for DateTime. Assign anything else on a line of its own."));

                    return blank;
                }
            }

            variables.Add(new RuleVariable(name.Value, type, RuleVariableKind.Scan, initialiser, lineIndex));

            return blank;
        }

        private static string NameError(string name, HashSet<string> names, int count)
        {
            if (count >= MaximumVariables)
            {
                return $"a rule may declare at most {MaximumVariables.ToString(CultureInfo.InvariantCulture)} variables.";
            }

            if (name.Length > MaximumNameLength)
            {
                return $"the variable name is longer than {MaximumNameLength.ToString(CultureInfo.InvariantCulture)} characters.";
            }

            if (SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None || reserved.Contains(name))
            {
                return $"'{name}' is reserved and cannot be the name of a variable.";
            }

            return names.Contains(name) ? $"the variable '{name}' is declared more than once." : null;
        }

        private static bool IsLiteralOf(string type, string literal)
        {
            return type switch
            {
                "Integer" => wholeNumber.IsMatch(literal),
                "Double" => number.IsMatch(literal),
                "String" => text.IsMatch(literal),
                "Boolean" => string.Equals(literal, "True", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(literal, "False", StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }

        private static ErrorSpan Error(string code, int line, int column, int endColumn, string message)
        {
            return new ErrorSpan
            {
                Code = code,
                Line = line,
                Column = column,
                EndColumn = endColumn,
                Message = $"Line {(line + 1).ToString(CultureInfo.InvariantCulture)}: {message}"
            };
        }

        private static string Mask(string line)
        {
            return stringLiteral.Replace(line, match => new string(' ', match.Length));
        }
    }
}
