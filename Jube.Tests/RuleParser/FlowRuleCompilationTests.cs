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
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Jube.Dictionary;
using Jube.Parser;
using Jube.Parser.Compiler;
using Jube.Test.Infrastructure;
using Xunit;
using FlowExtensions = global::Jube.Dictionary.Extensions.Extensions;

namespace Jube.Test.RuleParser
{
    [Trait("Category", "Unit")]
    [Collection("EvalExpressionRegistry")]
    public sealed class FlowRuleCompilationTests
    {
        private static Parser.Parser NewParser()
        {
            return new Parser.Parser(TestLog.NoOp, ["Matched"])
            {
                EntityAnalysisModelRequestXPaths = new Dictionary<string, EntityAnalysisModelRequestXPath>
                {
                    ["Amount"] = new() { DataTypeId = 3, Cache = true },
                    ["Country"] = new() { DataTypeId = 1, Cache = true },
                    ["Narrative"] = new() { DataTypeId = 1, Cache = true },
                    ["Email"] = new() { DataTypeId = 1, Cache = true }
                },
                EntityAnalysisModelsLists = ["HighRiskTerms"]
            };
        }

        private static ParsedRule Translate(Parser.Parser parser, string text)
        {
            var rule = new ParsedRule
            {
                ErrorSpans = [],
                OriginalRuleText = text,
                ParsedRuleText = text
            };

            rule = parser.TranslateFromDotNotation(rule);
            rule.ErrorSpans.Should().BeEmpty();

            return rule;
        }

        private static bool Evaluate(string ruleText, double amount, string country = "GB", string narrative = "",
            string email = "", params string[] highRiskTerms)
        {
            var parser = NewParser();
            var rule = Translate(parser, ruleText);
            rule = parser.Parse(rule);
            rule.ErrorSpans.Should().BeEmpty();

            rule = parser.WrapGatewayRule(rule, true);

            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => a.Location)
                .ToArray();

            var compile = new Compile();
            compile.CompileCode(rule.ParsedRuleText, TestLog.NoOp, references, Compile.Language.Vb);
            compile.Success.Should().BeTrue(compile.ErrorsSummary);

            var data = new DictionaryNoBoxing<string>();
            data.TryAdd("Amount", amount);
            data.TryAdd("Country", country);
            data.TryAdd("Narrative", narrative);
            data.TryAdd("Email", email);

            var lists = new Dictionary<string, List<string>> { ["HighRiskTerms"] = highRiskTerms.ToList() };
            var kvp = new PooledDictionary<string, double>();

            var match = compile.CompiledAssembly.GetType("GatewayRule")!.GetMethod("Match")!;
            return (bool)match.Invoke(null, [data, lists, kvp, TestLog.NoOp])!;
        }

        [Theory]
        [InlineData(150, true)]
        [InlineData(100, false)]
        [InlineData(50, false)]
        public void AFlowAssignedDirectlyToMatchedCompilesAndRuns(double amount, bool expected)
        {
            Evaluate("Matched = Payload.Amount.Start().MatchGreater(100)", amount).Should().Be(expected);
        }

        [Theory]
        [InlineData(200, "FR", true)]
        [InlineData(200, "GB", false)]
        [InlineData(200, "IE", false)]
        [InlineData(50, "FR", false)]
        [InlineData(600, "FR", false)]
        public void AGuardChainWithAParamsArrayAndAnAgainstSwitchOfValueRuns(double amount, string country,
            bool expected)
        {
            const string rule = "Matched = Payload.Amount.Start().RequireInRange(123.45, 543.21)" +
                                ".Against(Payload.Country).RequireNotIn(\"GB\", \"IE\").OtherwiseMatch()";

            Evaluate(rule, amount, country).Should().Be(expected);
        }

        [Theory]
        [InlineData("pay in crypto", true)]
        [InlineData("PAY IN CRYPTO", true)]
        [InlineData("pay in cash", false)]
        public void AStepThatTakesAListTokenRuns(string narrative, bool expected)
        {
            const string rule = "Matched = Payload.Narrative.Start().MatchContainsAnyInListIgnoreCase(List.HighRiskTerms)";

            Evaluate(rule, 0, narrative: narrative, highRiskTerms: ["crypto", "gift card"]).Should().Be(expected);
        }

        [Theory]
        [InlineData("a@gmail.com", true)]
        [InlineData("a@corp.com", false)]
        [InlineData("not-an-email", false)]
        public void AnExistingBooleanExtensionCanGuardAFlow(string email, bool expected)
        {
            const string rule = "Matched = Payload.Email.Start().RequireThat(Payload.Email.IsValidEmail())" +
                                ".MatchEndsWith(\"gmail.com\")";

            Evaluate(rule, 0, email: email).Should().Be(expected);
        }

        [Theory]
        [InlineData(150, true)]
        [InlineData(50, false)]
        public void AScoreCanBeAccumulatedAndThresholded(double amount, bool expected)
        {
            const string rule = "Matched = Payload.Amount.Start().AddScoreWhen(Payload.Amount > 100, 0.5)" +
                                ".AddScore(0.25).MatchIfScoreAtLeast(0.75)";

            Evaluate(rule, amount).Should().Be(expected);
        }

        [Theory]
        [InlineData(150, true)]
        [InlineData(50, false)]
        public void AFlowCanBeUsedDirectlyAsAnIfCondition(double amount, bool expected)
        {
            const string rule = "If Payload.Amount.Start().MatchGreater(100) Then\r\nMatched = True\r\nEnd If";

            Evaluate(rule, amount).Should().Be(expected);
        }

        [Theory]
        [InlineData(500, true)]
        [InlineData(50, false)]
        public void AGuardClauseIfSelectsBetweenFlows(double amount, bool expected)
        {
            const string rule = "If Payload.Country = \"GB\" Then\r\n" +
                                "Matched = Payload.Amount.Start().MatchGreater(400)\r\n" +
                                "End If";

            Evaluate(rule, amount).Should().Be(expected);
        }

        [Theory]
        [InlineData("150", true)]
        [InlineData("50", false)]
        [InlineData("abc", false)]
        public void AParsedStringFlowFailsClosedOnBadInput(string narrative, bool expected)
        {
            const string rule = "Matched = Payload.Narrative.Start().ParseDouble().RequireGreater(100).OtherwiseMatch()";

            Evaluate(rule, 0, narrative: narrative).Should().Be(expected);
        }

        [Theory]
        [InlineData("Jon Smith", true)]
        [InlineData("Smith John", true)]
        [InlineData("Bob Marley", false)]
        public void TheUserFacingFuzzyListExtensionCompilesAndRuns(string narrative, bool expected)
        {
            const string rule = "Matched = Payload.Narrative.IsSomehowInList(List.HighRiskTerms)";

            Evaluate(rule, 0, narrative: narrative, highRiskTerms: ["John Smith"]).Should().Be(expected);
        }

        [Theory]
        [InlineData("Dr Jon Smyth", true)]
        [InlineData("Dr Bob Smyth", false)]
        public void AnOptionsStringWithCommasAndEqualsSurvivesTranslationAndParsing(string narrative, bool expected)
        {
            const string rule = "Matched = Payload.Narrative.IsSomehowInList(List.HighRiskTerms, \"lev=2,notitles,minlength=3\")";

            Evaluate(rule, 0, narrative: narrative, highRiskTerms: ["John Smith"]).Should().Be(expected);
        }

        [Theory]
        [InlineData("Jon Smith", true)]
        [InlineData("Bob Marley", false)]
        public void AFuzzyScoreCanBeComparedInsideARule(string narrative, bool expected)
        {
            const string rule = "Matched = Payload.Narrative.FuzzyBestScore(List.HighRiskTerms, \"jw\") > 0.9";

            Evaluate(rule, 0, narrative: narrative, highRiskTerms: ["John Smith"]).Should().Be(expected);
        }

        [Theory]
        [InlineData("Jhon Smith", true)]
        [InlineData("Jhon Smyth", false)]
        public void AFuzzyPipelineStepWithOptionsCompilesAndRuns(string narrative, bool expected)
        {
            const string rule = "Matched = Payload.Narrative.Start().MatchSomehowInListWith(List.HighRiskTerms, \"damerau=1\")";

            Evaluate(rule, 0, narrative: narrative, highRiskTerms: ["John Smith"]).Should().Be(expected);
        }

        [Theory]
        [InlineData("Payment to Jon Smith today", true)]
        [InlineData("Payment to Bob today", false)]
        public void AFuzzyEntryFoundInsideFreeTextCompilesAndRuns(string narrative, bool expected)
        {
            const string rule = "Matched = Payload.Narrative.ContainsSomehowInList(List.HighRiskTerms)";

            Evaluate(rule, 0, narrative: narrative, highRiskTerms: ["John Smith"]).Should().Be(expected);
        }

        [Fact]
        public void AnInvalidOptionsStringCompilesAndFailsClosedAtRuntime()
        {
            const string rule = "Matched = Payload.Narrative.IsSomehowInList(List.HighRiskTerms, \"wibble\")";

            Evaluate(rule, 0, narrative: "John Smith", highRiskTerms: ["John Smith"]).Should().BeFalse();
        }

        [Fact]
        public void ALambdaIsStillRefusedByTheTokenAllowList()
        {
            var parser = NewParser();
            var rule = Translate(parser, "Matched = Payload.Amount.Start().Match()\r\nDim f = Sub() Matched = True");

            parser.Parse(rule).ErrorSpans.Should().NotBeEmpty();
        }

        [Fact]
        public void EveryFlowMethodIsAcceptedAsARuleToken()
        {
            var flowMethods = typeof(FlowExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.GetParameters().Length > 0 &&
                            m.GetParameters()[0].ParameterType.IsGenericType &&
                            m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() ==
                            typeof(global::Jube.Dictionary.Extensions.Flow<>))
                .Select(m => m.Name)
                .Distinct()
                .ToList();

            flowMethods.Should().NotBeEmpty();

            var parser = NewParser();
            var failures = new List<string>();
            foreach (var name in flowMethods)
            {
                var rule = new ParsedRule
                {
                    ErrorSpans = [],
                    OriginalRuleText = "Matched = x." + name,
                    ParsedRuleText = "Matched = x." + name
                };

                var parsed = parser.Parse(rule);
                if (parsed.ErrorSpans.Any(e => e.Message.Contains("'" + name + "'")))
                {
                    failures.Add(name);
                }
            }

            failures.Should().BeEmpty();
        }

        [Fact]
        public void NoFlowMethodAcceptsADelegateSoRulesCannotSmuggleInCode()
        {
            var offenders = typeof(FlowExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.GetParameters().Length > 0 &&
                            m.GetParameters()[0].ParameterType.IsGenericType &&
                            m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() ==
                            typeof(global::Jube.Dictionary.Extensions.Flow<>))
                .Where(m => m.GetParameters().Any(p => typeof(Delegate).IsAssignableFrom(p.ParameterType) ||
                                                       p.ParameterType == typeof(object) ||
                                                       p.ParameterType == typeof(Type)))
                .Select(m => m.Name)
                .Distinct()
                .ToList();

            offenders.Should().BeEmpty();
        }

        [Fact]
        public void TheFlowStructAndItsHelpersAreNotRuleTokens()
        {
            var tokenNames = typeof(FlowExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Select(m => m.Name)
                .ToHashSet();

            tokenNames.Should().NotContain(["Apply", "Decide", "Carry", "WithScore", "WithLabel"]);
        }
    }
}
