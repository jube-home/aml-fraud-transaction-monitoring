/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.Text;
using Jube.Dictionary.Extensions;
using Jube.Service.Agent.RuleExtensionTools.Models;

namespace Jube.Service.Agent.RuleExtensionTools
{
    public static class RuleExtensionChainValidator
    {
        public static RuleExtensionValidationResult Validate(string expression, string receiverType = "String")
        {
            var problems = new List<string>();
            var steps = new List<RuleExtensionValidationStep>();

            if (string.IsNullOrWhiteSpace(expression))
            {
                return new RuleExtensionValidationResult(false, expression, receiverType,
                    receiverType, steps, ["There is no expression to check."]);
            }

            var catalogue = RuleExtensionCatalogue.All;
            var segments = Split(expression);
            var current = Normalise(receiverType);
            var receiver = new List<string>();
            var started = false;

            foreach (var segment in segments)
            {
                var name = NameOf(segment);

                if (!started && !segment.Contains('(') && catalogue.All(candidate =>
                        !string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase)))
                {
                    receiver.Add(segment);
                    continue;
                }

                started = true;
                var step = Resolve(catalogue, segment, name, current);
                steps.Add(step);

                if (step.Problem is not null)
                {
                    problems.Add(step.Problem);
                }

                current = step.Returns;
            }

            if (!started)
            {
                problems.Add($"Nothing in \"{expression}\" is a method this rule language has. Search the "
                             + "extension surface for the operation wanted rather than inventing a name.");
            }

            return new RuleExtensionValidationResult(problems.Count == 0, expression,
                receiver.Count > 0 ? string.Join(".", receiver) : receiverType, current, steps, problems);
        }

        private static RuleExtensionValidationStep Resolve(IReadOnlyList<RuleExtensionSignature> catalogue,
            string segment, string name, string instance)
        {
            var named = catalogue
                .Where(candidate => string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (named.Count == 0)
            {
                var near = catalogue.Select(candidate => candidate.Name).Distinct(StringComparer.Ordinal)
                    .OrderBy(candidate => candidate.LevenshteinDistance(name))
                    .Take(5).ToList();

                return new RuleExtensionValidationStep(segment, instance, instance, null,
                    $"There is no method called {name}. The closest names are {string.Join(", ", near)}.");
            }

            var reachable = named
                .Where(candidate => string.Equals(Normalise(candidate.Instance), instance,
                    StringComparison.OrdinalIgnoreCase)).ToList();

            if (reachable.Count == 0)
            {
                var elsewhere = named.Select(candidate => candidate.Instance)
                    .Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal);

                return new RuleExtensionValidationStep(segment, instance, instance, null,
                    $"{name} exists, but on {string.Join(" and ", elsewhere)} and not on {instance}, which is "
                    + "what the expression holds at this point. Convert first, or use a method that takes "
                    + $"{instance}.");
            }

            var arguments = ArgumentsOf(segment);
            var fitting = reachable.Where(candidate => Accepts(candidate, arguments)).ToList();

            if (fitting.Count == 0)
            {
                var shapes = reachable.Select(candidate => candidate.Declaration);

                return new RuleExtensionValidationStep(segment, instance, reachable[0].Returns, null,
                    $"{name} was given {arguments} argument(s), which no overload takes. The shapes available "
                    + $"are: {string.Join(" | ", shapes)}.");
            }

            var chosen = fitting[0];

            return new RuleExtensionValidationStep(segment, instance, chosen.Returns, chosen.Declaration, null);
        }

        private static bool Accepts(RuleExtensionSignature signature, int arguments)
        {
            var required = signature.Parameters.Count(parameter => !parameter.Optional && !parameter.ParamArray);

            if (signature.Parameters.Any(parameter => parameter.ParamArray))
            {
                return arguments >= required;
            }

            return arguments >= required && arguments <= signature.Parameters.Count;
        }

        private static string Normalise(string type)
        {
            return type.TrimEnd('?');
        }

        private static string NameOf(string segment)
        {
            var open = segment.IndexOf('(');

            return (open < 0 ? segment : segment[..open]).Trim();
        }

        private static int ArgumentsOf(string segment)
        {
            var open = segment.IndexOf('(');

            if (open < 0)
            {
                return 0;
            }

            var inner = segment[(open + 1)..].TrimEnd();

            if (inner.EndsWith(')'))
            {
                inner = inner[..^1];
            }

            if (string.IsNullOrWhiteSpace(inner))
            {
                return 0;
            }

            var count = 1;
            var depth = 0;
            var quoted = false;

            foreach (var character in inner)
            {
                if (character == '"')
                {
                    quoted = !quoted;
                }
                else if (!quoted && character is '(' or '[')
                {
                    depth++;
                }
                else if (!quoted && character is ')' or ']')
                {
                    depth--;
                }
                else if (!quoted && depth == 0 && character == ',')
                {
                    count++;
                }
            }

            return count;
        }

        private static List<string> Split(string expression)
        {
            var segments = new List<string>();
            var builder = new StringBuilder();
            var depth = 0;
            var quoted = false;

            foreach (var character in expression.Trim())
            {
                if (character == '"')
                {
                    quoted = !quoted;
                }
                else if (!quoted && character is '(' or '[')
                {
                    depth++;
                }
                else if (!quoted && character is ')' or ']')
                {
                    depth--;
                }
                else if (!quoted && depth == 0 && character == '.')
                {
                    if (builder.Length > 0)
                    {
                        segments.Add(builder.ToString().Trim());
                        builder.Clear();
                    }

                    continue;
                }

                builder.Append(character);
            }

            if (builder.Length > 0)
            {
                segments.Add(builder.ToString().Trim());
            }

            return segments;
        }
    }
}