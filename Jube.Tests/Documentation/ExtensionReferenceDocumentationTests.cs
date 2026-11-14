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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using FluentAssertions;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.Documentation
{
    [Trait("Category", "Unit")]
    public sealed partial class ExtensionReferenceDocumentationTests
    {
        private const string MatchPrefix = "Match";
        private const string MatchSuffix = " Decides matched if so; otherwise continues.";
        private const string TrueWhen = "True when: ";

        private static readonly HashSet<string> controlStems = ["When", "IfScoreAtLeast"];
        private static readonly string[] calculationSuffixes = ["Above", "Below", "InRange", "OutsideRange"];

        private static readonly Dictionary<string, string> testSections = new()
        {
            ["Double"] = "Double",
            ["Int32"] = "Integer",
            ["String"] = "String",
            ["DateTime"] = "DateTime",
            ["Boolean"] = "Boolean"
        };

        private sealed record Row(string Name, int Count, string Description, Dictionary<string, string> Parameters);

        private sealed record Step(string Section, string Stem, string Predicate, List<string> ParameterNames);

        private static string DocsDirectory()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "docs")))
            {
                directory = directory.Parent;
            }

            return Path.Combine(directory.Required().FullName, "docs");
        }

        private static string Clean(string text)
        {
            var cleaned = MarkdownLink().Replace(text, "$1").Replace("`", "").Replace("<br>", " ").Trim();

            cleaned = Spaces().Replace(cleaned, " ").Replace(" -- ", ", ");

            return cleaned.Length > 0 && ".!?)".IndexOf(cleaned[^1]) < 0 ? cleaned + "." : cleaned;
        }

        private static int Arity(string parameters)
        {
            var depth = 0;
            var count = parameters.Trim().Length == 0 ? 0 : 1;

            foreach (var character in parameters)
            {
                depth += character switch
                {
                    '<' or '(' => 1,
                    '>' or ')' => -1,
                    _ => 0
                };
                count += character == ',' && depth == 0 ? 1 : 0;
            }

            return count;
        }

        private static string DescriptionOf(ICustomAttributeProvider provider)
        {
            return provider.GetCustomAttributes(typeof(DescriptionAttribute), false)
                .OfType<DescriptionAttribute>().FirstOrDefault()?.Description ?? string.Empty;
        }

        private static List<MethodInfo> Methods()
        {
            return
            [
                .. typeof(Jube.Dictionary.Extensions.Extensions)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(m => m.IsDefined(typeof(ExtensionAttribute), false) && !m.IsSpecialName &&
                                !m.Name.Contains('<') && !m.Name.Contains('$'))
            ];
        }

        private static bool IsPipeline(MethodInfo method)
        {
            return IsFlow(method.ReturnType) || IsFlow(method.GetParameters()[0].ParameterType);
        }

        private static bool IsFlow(Type type)
        {
            return type.IsGenericType && type.Name.StartsWith("Flow`", StringComparison.Ordinal);
        }

        private static List<MethodInfo> PlainMethods()
        {
            return [.. Methods().Where(m => !IsPipeline(m) && m.Name != "Eval")];
        }

        private static List<Row> CompilationRows()
        {
            var rows = new List<Row>();
            var pipelineGroup = false;

            foreach (var line in File.ReadAllLines(Path.Combine(DocsDirectory(), "Configuration", "Models",
                         "RuleCompilationAlgorithm", "index.md")))
            {
                if (!line.StartsWith('|'))
                {
                    pipelineGroup = false;
                    continue;
                }

                if (line.StartsWith("| **", StringComparison.Ordinal))
                {
                    pipelineGroup = line.Contains("Pipeline Extensions");
                    continue;
                }

                var cells = line.Trim().Trim('|').Split('|').Select(c => c.Trim()).ToList();

                if (pipelineGroup || cells.Count < 2 || !cells[0].StartsWith('`') || !cells[0].Contains("(this "))
                {
                    continue;
                }

                var signature = cells[0].Trim('`');

                if (signature.Contains("`, `") || signature.Contains(" / "))
                {
                    continue;
                }

                var match = Signature().Match(signature);

                if (!match.Success)
                {
                    continue;
                }

                var parameters = new Dictionary<string, string>(StringComparer.Ordinal);

                if (cells.Count > 2)
                {
                    foreach (Match parameter in ParameterText().Matches(cells[2]))
                    {
                        parameters[parameter.Groups[1].Value] = Clean(parameter.Groups[2].Value);
                    }
                }

                var arity = Arity(match.Groups["parameters"].Value) - 1;

                rows.Add(new Row(match.Groups["name"].Value, arity, Clean(cells[1]), parameters));
            }

            return rows;
        }

        private static Dictionary<string, List<(string Arguments, string Text)>> PipelineTestRows()
        {
            var rows = new Dictionary<string, List<(string, string)>>(StringComparer.Ordinal);
            string? section = null;

            foreach (var line in File.ReadAllLines(Path.Combine(DocsDirectory(), "RuleTaxonomy",
                         "RulePipelineReference.md")))
            {
                var heading = TestsHeading().Match(line);

                if (heading.Success)
                {
                    section = heading.Groups[1].Value;
                    continue;
                }

                if (line.StartsWith("## ", StringComparison.Ordinal))
                {
                    section = null;
                }

                if (section == null || !line.StartsWith("| `", StringComparison.Ordinal))
                {
                    continue;
                }

                var cells = line.Trim().Trim('|').Split('|').Select(c => c.Trim()).ToList();

                if (!rows.TryGetValue(section + " " + cells[0].Trim('`'), out var list))
                {
                    rows[section + " " + cells[0].Trim('`')] = list = [];
                }

                list.Add((cells[1].Trim('`'), Clean(cells[2])));
            }

            return rows;
        }

        private static Dictionary<string, string> CalculationRows()
        {
            var rows = new Dictionary<string, string>(StringComparer.Ordinal);
            var inside = false;

            foreach (var line in File.ReadAllLines(Path.Combine(DocsDirectory(), "RuleTaxonomy",
                         "RulePipelineReference.md")))
            {
                if (line.StartsWith("| Calculation ", StringComparison.Ordinal))
                {
                    inside = true;
                    continue;
                }

                if (inside && !line.StartsWith('|'))
                {
                    break;
                }

                if (!inside || !line.StartsWith("| `", StringComparison.Ordinal))
                {
                    continue;
                }

                var cells = line.Trim().Trim('|').Split('|').Select(c => c.Trim()).ToList();

                rows[cells[0].Trim('`')] = Clean(cells[2]);
            }

            return rows;
        }

        private static List<Step> Steps()
        {
            var calculations = CalculationRows();
            var steps = new List<Step>();

            foreach (var method in Methods().Where(m => IsFlow(m.GetParameters()[0].ParameterType) &&
                                                        m.Name.StartsWith(MatchPrefix, StringComparison.Ordinal) &&
                                                        m.Name.Length > MatchPrefix.Length))
            {
                var stem = method.Name[MatchPrefix.Length..];
                var inner = method.GetParameters()[0].ParameterType.GetGenericArguments()[0];

                if (controlStems.Contains(stem) || !testSections.TryGetValue(inner.Name, out var section))
                {
                    continue;
                }

                if (calculationSuffixes.Any(suffix =>
                        stem.EndsWith(suffix, StringComparison.Ordinal) &&
                        calculations.ContainsKey(stem[..^suffix.Length])))
                {
                    continue;
                }

                var description = DescriptionOf(method);

                description.Should().StartWith(TrueWhen, $"{method.Name} is a pipeline test");
                description.Should().EndWith(MatchSuffix, $"{method.Name} is a Match step");

                var predicate = description[TrueWhen.Length..^MatchSuffix.Length];

                steps.Add(new Step(section, stem, predicate,
                    [.. method.GetParameters().Skip(1).Select(p => p.Name ?? string.Empty)]));
            }

            return steps;
        }

        [Fact]
        public void EveryPlainExtensionMethodHasARowThatMatchesItsDescriptionAndParameters()
        {
            var rows = CompilationRows().ToLookup(row => (row.Name, row.Count));
            var problems = new List<string>();

            foreach (var method in PlainMethods())
            {
                var parameters = method.GetParameters().Skip(1).ToList();
                var candidates = rows[(method.Name, parameters.Count)].ToList();

                if (candidates.Count == 0)
                {
                    problems.Add($"{method.Name}/{parameters.Count} has no row");
                    continue;
                }

                var expected = DescriptionOf(method);

                var fits = candidates.Any(row =>
                    row.Description == expected &&
                    parameters.All(p => row.Parameters.TryGetValue(p.Name ?? string.Empty, out var text) &&
                                        text == DescriptionOf(p)));

                if (!fits)
                {
                    problems.Add($"{method.Name}/{parameters.Count}: no row carries \"{expected}\" with its parameters");
                }
            }

            problems.Should().BeEmpty(
                "docs/Configuration/Models/RuleCompilationAlgorithm/index.md must give every plain extension " +
                "method a row whose description and parameter texts are the [Description] attributes on the method");
        }

        [Fact]
        public void NoRowDocumentsAMethodThatNoLongerExists()
        {
            var live = PlainMethods().Select(m => (m.Name, m.GetParameters().Length - 1)).ToHashSet();

            CompilationRows().Where(row => !live.Contains((row.Name, row.Count)))
                .Select(row => $"{row.Name}/{row.Count}")
                .Should().BeEmpty("every documented signature must still exist with that name and arity");
        }

        [Fact]
        public void EveryPipelineTestHasARowWhoseTextAndArgumentsMatchTheStep()
        {
            var rows = PipelineTestRows();
            var steps = Steps();
            var problems = new List<string>();

            steps.Should().HaveCountGreaterThan(150);

            foreach (var step in steps)
            {
                if (!rows.TryGetValue(step.Section + " " + step.Stem, out var candidates))
                {
                    problems.Add($"{step.Section} {step.Stem} has no row");
                    continue;
                }

                var fits = candidates.Any(row =>
                    string.Equals(row.Text, step.Predicate, StringComparison.OrdinalIgnoreCase) &&
                    ArgumentNames(row.Arguments).SequenceEqual(step.ParameterNames));

                if (!fits)
                {
                    problems.Add($"{step.Section} {step.Stem}: expected \"{step.Predicate}\" with " +
                                 $"({string.Join(", ", step.ParameterNames)})");
                }
            }

            problems.Should().BeEmpty(
                "the tests tables in docs/RuleTaxonomy/RulePipelineReference.md must hold every pipeline test with " +
                "the predicate text and argument names of its [Description]");
        }

        [Fact]
        public void EveryCalculationHasARowThatMatchesItsDescription()
        {
            var rows = CalculationRows();
            var plain = PlainMethods().ToLookup(m => m.Name);

            rows.Should().HaveCountGreaterThan(30);

            rows.Where(row => plain[row.Key].All(m => DescriptionOf(m) != row.Value))
                .Select(row => $"{row.Key}: \"{row.Value}\"")
                .Should().BeEmpty("the Calculations table must carry each calculation's [Description]");
        }

        private static List<string> ArgumentNames(string arguments)
        {
            return arguments == "none"
                ? []
                : [.. arguments.Split(',').Select(a => a.Trim().Split(' ')[^1])];
        }

        [GeneratedRegex(@"\[([^\]]+)\]\([^)]*\)")]
        private static partial Regex MarkdownLink();

        [GeneratedRegex(@"\s+")]
        private static partial Regex Spaces();

        [GeneratedRegex(@"^(?<name>\w+)(?:<[^>]*>)?\((?<parameters>.*)\)$")]
        private static partial Regex Signature();

        [GeneratedRegex(@"`@?(\w+)`:\s*(.*?)(?=<br>`|$)")]
        private static partial Regex ParameterText();

        [GeneratedRegex(@"^### (\w+) tests")]
        private static partial Regex TestsHeading();
    }
}
