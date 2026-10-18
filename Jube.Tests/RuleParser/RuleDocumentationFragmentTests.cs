using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Jube.Data.Query.Models;
using Jube.Parser;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.RuleParser
{
    [Trait("Category", "Unit")]
    [Collection("EvalExpressionRegistry")]
    public sealed class RuleDocumentationFragmentTests
    {
        public static IEnumerable<object[]> DocumentedFragments()
        {
            var docs = Path.Combine(RepositoryRoot(), "docs");

            foreach (var path in Directory.EnumerateFiles(docs, "*.md", SearchOption.AllDirectories).OrderBy(p => p))
            {
                var lines = File.ReadAllLines(path);

                for (var i = 0; i < lines.Length; i++)
                {
                    var row = lines[i].Trim();

                    if (row.StartsWith("| ", StringComparison.Ordinal))
                    {
                        var cells = row.Split('|');
                        var fragment = cells.Length > 6 ? cells[5].Trim() : string.Empty;
                        var identifier = cells.Length > 6 ? cells[1].Trim() : string.Empty;

                        if (Regex.IsMatch(identifier, @"^[A-Z0-9.]+-\d{2}$") && fragment.Length > 2 &&
                            fragment.StartsWith('`') && fragment.EndsWith('`'))
                        {
                            yield return [$"{Path.GetRelativePath(docs, path)}:{i + 1}", fragment[1..^1], false];
                        }

                        continue;
                    }

                    var fence = lines[i].Trim();

                    if (fence != "```vb" && fence != "```vb calculation")
                    {
                        continue;
                    }

                    var start = i + 1;
                    var body = new List<string>();

                    for (i = start; i < lines.Length && lines[i].Trim() != "```"; i++)
                    {
                        body.Add(lines[i].Trim());
                    }

                    var location = $"{Path.GetRelativePath(docs, path)}:{start}";
                    var calculation = fence == "```vb calculation";

                    yield return [location, string.Join("\n", body), calculation];
                }
            }
        }

        [Theory]
        [MemberData(nameof(DocumentedFragments))]
        public void EveryDocumentedRuleFragmentCompilesThroughTheParser(string location, string fragment,
            bool calculation)
        {
            var result = RuleParse.Execute(fragment,
                calculation ? RuleParse.AbstractionCalculation : RuleTypeFor(fragment), BuildEnvironment(fragment),
                TestLog.NoOp, RuleParse.DefaultReferences());

            result.Compiled.Should()
                .BeTrue(
                    $"{location} did not compile: {result.Message} {string.Join(" | ", result.ErrorSpans.Select(span => span.Message))}");
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Jube.sln")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new InvalidOperationException("The repository root was not found.");
        }

        private static int RuleTypeFor(string text)
        {
            return Regex.IsMatch(text,
                @"TTLCounter\.|Abstraction\.|AbstractionCalculation\.|Sanction\.|Dictionary\.|List\.|Activation\.|HTTPAdaptation\.|ExhaustiveAdaptation\.")
                ? RuleParse.ActivationRule
                : RuleParse.GatewayRule;
        }

        private static RuleParseEnvironmentDto BuildEnvironment(string text)
        {
            return new RuleParseEnvironmentDto
            {
                Tokens = ["Matched"],
                RequestXPaths = Names(text, "Payload").ToDictionary(name => name,
                    name => new RuleParseEnvironmentRequestXPathDto { DataTypeId = DataTypeFor(name), Cache = true }),
                TtlCounters = Names(text, "TTLCounter"),
                AbstractionRules = Names(text, "Abstraction"),
                AbstractionCalculations = Names(text, "AbstractionCalculation"),
                Sanctions = Names(text, "Sanction"),
                Dictionaries = Names(text, "Dictionary"),
                Lists = Names(text, "List"),
                ActivationRules = Names(text, "Activation"),
                HttpAdaptations = Names(text, "HTTPAdaptation"),
                ExhaustiveAdaptations = Names(text, "ExhaustiveAdaptation")
            };
        }

        private static List<string> Names(string text, string prefix)
        {
            return
            [
                .. Regex.Matches(text, prefix + @"\.([A-Za-z_]\w*)")
                    .Select(match => match.Groups[1].Value)
                    .Distinct()
            ];
        }

        private static int DataTypeFor(string name)
        {
            if (Regex.IsMatch(name, "Date|Time|Timestamp|Expiry|Dob"))
            {
                return 4;
            }

            if (Regex.IsMatch(name,
                    "(Amount|Value|Lat|Latitude|Lon|Longitude|Score|Count|Days|Age|Limit|Rate|Percent|Balance|Distance|Ratio|Duration|Seconds|Minutes|Hours|Speed|Volume|Weight|Fee|Price|Quantity|Number|Num|Tenure|Elapsed|Sum|Total|Average|Size|Km|Miles|Percentile|Years)$"))
            {
                return 3;
            }

            if (Regex.IsMatch(name, "^Is|Flag|Has|Enabled"))
            {
                return 5;
            }

            return 1;
        }
    }
}