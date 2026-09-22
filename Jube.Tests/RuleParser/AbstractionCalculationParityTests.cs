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
using FluentAssertions;
using Jube.Dictionary;
using Jube.Parser;
using Jube.Parser.Compiler;
using Jube.Test.Infrastructure;
using Xunit;
using Calculation = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelAbstractionCalculation;

namespace Jube.Test.RuleParser
{
    [Trait("Category", "Unit")]
    [Collection("EvalExpressionRegistry")]
    public sealed class AbstractionCalculationParityTests
    {
        private static Parser.Parser NewParser()
        {
            return new Parser.Parser(TestLog.NoOp, ["Matched"])
            {
                EntityAnalysisModelRequestXPaths = new Dictionary<string, EntityAnalysisModelRequestXPath>
                {
                    ["Amount"] = new() { DataTypeId = 3, Cache = true },
                    ["Count"] = new() { DataTypeId = 2, Cache = true }
                },
                EntityAnalysisModelInlineScriptProperties = new Dictionary<string, int> { ["ScriptScore"] = 3 },
                EntityAnalysisModelsInlineFunctions = new Dictionary<string, int> { ["FunctionScore"] = 3 },
                EntityAnalysisModelsTtlCounters = ["Declined", "Total"],
                EntityAnalysisModelsAbstractionRule = ["Velocity"],
                EntityAnalysisModelAbstractionCalculations = ["Earlier"],
                EntityAnalysisModelsSanctions = ["Party"],
                EntityAnalysisModelsDictionaries = ["CountryRisk"],
                EntityAnalysisModelsLists = ["Terms"]
            };
        }

        private static Calculation.Match CompileAsTheEngineDoes(string ruleText, Parser.Parser? parser = null)
        {
            parser ??= NewParser();
            var rule = parser.TranslateFromDotNotation(new ParsedRule
            {
                ErrorSpans = [],
                OriginalRuleText = ruleText,
                ParsedRuleText = ruleText
            });
            rule.ErrorSpans.Should().BeEmpty();
            rule = parser.Parse(rule);
            rule.ErrorSpans.Should().BeEmpty();
            rule = parser.WrapAbstractionCalculation(rule, true);

            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => a.Location)
                .ToArray();

            var compile = new Compile();
            compile.CompileCode(rule.ParsedRuleText, TestLog.NoOp, references, Compile.Language.Vb);
            compile.Success.Should().BeTrue(compile.ErrorsSummary);

            var method = compile.CompiledAssembly.GetType("CalculationRule")!.GetMethod("Match")!;
            return (Calculation.Match)Delegate.CreateDelegate(typeof(Calculation.Match), method);
        }

        private static PooledDictionary<string, double> Numbers(params (string, double)[] values)
        {
            var dictionary = new PooledDictionary<string, double>();
            foreach (var (name, value) in values)
            {
                dictionary.TryAdd(name, value);
            }

            return dictionary;
        }

        private static double Run(Calculation.Match match, double amount = 100)
        {
            var data = new DictionaryNoBoxing<string>();
            data.TryAdd("Amount", amount);
            data.TryAdd("Count", 4);
            data.TryAdd("ScriptScore", 7d);
            data.TryAdd("FunctionScore", 5d);

            return match(data, Numbers(("Declined", 30), ("Total", 120)), Numbers(("Velocity", 8)),
                new Dictionary<string, List<string>> { ["Terms"] = ["a"] }, Numbers(("Earlier", 3)),
                Numbers(("Party", 2)), Numbers(("CountryRisk", 4)), TestLog.NoOp);
        }

        [Fact]
        public void TheCalculationWrapperMatchesTheDelegateTheEngineInvokes()
        {
            var match = CompileAsTheEngineDoes("Matched = 1");

            Run(match).Should().Be(1);
        }

        [Theory]
        [InlineData("Matched = Payload.Amount", 100)]
        [InlineData("Matched = Payload.Count", 4)]
        [InlineData("Matched = Payload.ScriptScore", 7)]
        [InlineData("Matched = Payload.FunctionScore", 5)]
        [InlineData("Matched = TTLCounter.Declined", 30)]
        [InlineData("Matched = Abstraction.Velocity", 8)]
        [InlineData("Matched = AbstractionCalculation.Earlier", 3)]
        [InlineData("Matched = Sanction.Party", 2)]
        [InlineData("Matched = Dictionary.CountryRisk", 4)]
        public void EveryUpstreamSourceCanBeReadByACoderRule(string rule, double expected)
        {
            Run(CompileAsTheEngineDoes(rule)).Should().Be(expected);
        }

        [Fact]
        public void AnInlineScriptPropertyAndAnInlineFunctionFeedTheSameExpression()
        {
            Run(CompileAsTheEngineDoes("Matched = Payload.ScriptScore + Payload.FunctionScore + TTLCounter.Declined"))
                .Should().Be(42);
        }

        [Fact]
        public void EveryUpstreamSourceMixesInOneRule()
        {
            const string rule = "Matched = Payload.Amount + Payload.FunctionScore + TTLCounter.Declined + " +
                                "Abstraction.Velocity + AbstractionCalculation.Earlier + Sanction.Party + Dictionary.CountryRisk";

            Run(CompileAsTheEngineDoes(rule)).Should().Be(100 + 5 + 30 + 8 + 3 + 2 + 4);
        }

        [Fact]
        public void AFluentCalculationOverCountersAndPayloadCompilesAndRuns()
        {
            var match = CompileAsTheEngineDoes("Matched = TTLCounter.Declined.RatioOf(TTLCounter.Total)");

            Run(match).Should().Be(0.25);
        }

        [Fact]
        public void AFluentCalculationMixesAPayloadFieldAnInlineFunctionAndACounter()
        {
            var match = CompileAsTheEngineDoes(
                "Matched = Payload.Amount.PercentOf(TTLCounter.Total).Minus(Payload.FunctionScore)");

            Run(match).Should().BeApproximately(100d / 120d * 100d - 5d, 1e-9);
        }

        [Fact]
        public void AFluentCalculationOverAnEarlierCalculationAndASanctionRuns()
        {
            var match = CompileAsTheEngineDoes("Matched = AbstractionCalculation.Earlier.ImbalanceWith(Sanction.Party)");

            Run(match).Should().BeApproximately((3d - 2d) / (3d + 2d), 1e-12);
        }

        [Fact]
        public void AnUndefinedFluentCalculationIsNaNAndNotAnException()
        {
            var match = CompileAsTheEngineDoes("Matched = TTLCounter.Declined.RatioOf(Payload.Amount)");

            double.IsNaN(Run(match, 0)).Should().BeTrue();
        }

        [Fact]
        public void APayloadReferenceToAnUnknownFieldIsRefusedAtParseTime()
        {
            var parser = NewParser();
            var rule = parser.TranslateFromDotNotation(new ParsedRule
            {
                ErrorSpans = [],
                OriginalRuleText = "Matched = Payload.NotAField",
                ParsedRuleText = "Matched = Payload.NotAField"
            });

            rule.ErrorSpans.Should().NotBeEmpty();
        }

        [Fact]
        public void ATtlCounterReferenceToAnUnknownCounterIsRefusedAtParseTime()
        {
            var parser = NewParser();
            var rule = parser.TranslateFromDotNotation(new ParsedRule
            {
                ErrorSpans = [],
                OriginalRuleText = "Matched = TTLCounter.NotACounter",
                ParsedRuleText = "Matched = TTLCounter.NotACounter"
            });

            rule.ErrorSpans.Should().NotBeEmpty();
        }
    }
}
