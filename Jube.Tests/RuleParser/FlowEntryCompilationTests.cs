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

namespace Jube.Test.RuleParser
{
    [Trait("Category", "Unit")]
    [Collection("EvalExpressionRegistry")]
    public sealed class FlowEntryCompilationTests
    {
        public enum Context
        {
            Gateway,
            Abstraction,
            Activation
        }

        private sealed record Values
        {
            public double Amount { get; init; } = 150;
            public int Count { get; init; } = 7;
            public bool Flag { get; init; } = true;
            public DateTime Date { get; init; } = new(2024, 3, 15, 10, 0, 0, DateTimeKind.Utc);
            public string? Narrative { get; init; } = "Jon Smith";
            public string? Name { get; init; } = "  ABC ";
            public string? Text { get; init; } = "42";
        }

        private static Parser.Parser NewParser()
        {
            return new Parser.Parser(TestLog.NoOp, ["Matched"])
            {
                EntityAnalysisModelRequestXPaths = new Dictionary<string, EntityAnalysisModelRequestXPath>
                {
                    ["Amount"] = new() { DataTypeId = 3, Cache = true },
                    ["Count"] = new() { DataTypeId = 2, Cache = true },
                    ["Flag"] = new() { DataTypeId = 5, Cache = true },
                    ["Date"] = new() { DataTypeId = 4, Cache = true },
                    ["Narrative"] = new() { DataTypeId = 1, Cache = true },
                    ["Name"] = new() { DataTypeId = 1, Cache = true },
                    ["Text"] = new() { DataTypeId = 1, Cache = true }
                },
                EntityAnalysisModelsLists = ["HighRiskTerms"],
                EntityAnalysisModelsTtlCounters = ["CardPayments", "DeclinedSpend", "TotalSpend", "ApprovedSpend"],
                EntityAnalysisModelsAbstractionRule = ["Velocity"],
                EntityAnalysisModelAbstractionCalculations = ["Ratio"],
                EntityAnalysisModelsDictionaries = ["CountryRisk"]
            };
        }

        private static PooledDictionary<string, double> Numbers(string name, double value)
        {
            var dictionary = new PooledDictionary<string, double>();
            dictionary.TryAdd(name, value);
            return dictionary;
        }

        private static bool Evaluate(Context context, string ruleText, Values? values = null)
        {
            values ??= new Values();
            var parser = NewParser();
            var rule = parser.TranslateFromDotNotation(new ParsedRule
            {
                ErrorSpans = [],
                OriginalRuleText = ruleText,
                ParsedRuleText = ruleText
            });
            rule.ErrorSpans.Should().BeEmpty();
            rule = parser.Parse(rule);
            rule.ErrorSpans.Should().BeEmpty();
            rule = context switch
            {
                Context.Gateway => parser.WrapGatewayRule(rule, true),
                Context.Abstraction => parser.WrapAbstractionRule(rule, true),
                _ => parser.WrapActivationRule(rule, true)
            };

            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => a.Location)
                .ToArray();

            var compile = new Compile();
            compile.CompileCode(rule.ParsedRuleText, TestLog.NoOp, references, Compile.Language.Vb);
            compile.Success.Should().BeTrue(compile.ErrorsSummary);

            var data = new DictionaryNoBoxing<string>();
            data.TryAdd("Amount", values.Amount);
            data.TryAdd("Count", values.Count);
            data.TryAdd("Flag", values.Flag);
            data.TryAdd("Date", values.Date);
            data.TryAdd("Narrative", values.Narrative);
            data.TryAdd("Name", values.Name);
            data.TryAdd("Text", values.Text);

            var wrapperClassName = context == Context.Activation ? "ActivationRule" : "GatewayRule";
            var match = compile.CompiledAssembly.GetType(wrapperClassName)!.GetMethod("Match")!;
            var arguments = match.GetParameters().Select(p => Argument(p, data)).ToArray();

            return (bool)match.Invoke(null, arguments)!;
        }

        private static object? Argument(ParameterInfo parameter, DictionaryNoBoxing<string> data)
        {
            switch (parameter.Name)
            {
                case "Data":
                    return data;
                case "Log":
                    return TestLog.NoOp;
                case "TTLCounter":
                {
                    var counters = Numbers("CardPayments", 6);
                    counters.TryAdd("DeclinedSpend", 300);
                    counters.TryAdd("TotalSpend", 1000);
                    counters.TryAdd("ApprovedSpend", 700);
                    return counters;
                }
                case "Abstraction":
                    return Numbers("Velocity", 3);
                case "Calculation":
                    return Numbers("Ratio", 0.5);
                case "KVP":
                    return Numbers("CountryRisk", 4);
                case "Activation":
                    return new List<string>();
                case "List":
                {
                    var instance = Activator.CreateInstance(parameter.ParameterType)!;
                    ((IDictionary<string, List<string>>)instance).Add("HighRiskTerms", ["John Smith"]);
                    return instance;
                }
                default:
                    return Activator.CreateInstance(parameter.ParameterType);
            }
        }

        public static IEnumerable<object[]> AllContexts()
        {
            yield return [Context.Gateway];
            yield return [Context.Abstraction];
            yield return [Context.Activation];
        }

        [Theory]
        [MemberData(nameof(AllContexts))]
        public void ADoubleFieldStartsAPipelineWithNoStart(Context context)
        {
            Evaluate(context, "Matched = Payload.Amount.MatchGreater(100)").Should().BeTrue();
            Evaluate(context, "Matched = Payload.Amount.MatchLess(100)").Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(AllContexts))]
        public void AnIntegerFieldStartsAPipelineWithNoStart(Context context)
        {
            Evaluate(context, "Matched = Payload.Count.MatchGreater(5)").Should().BeTrue();
            Evaluate(context, "Matched = Payload.Count.RequireInRange(1, 10).OtherwiseMatch()").Should().BeTrue();
            Evaluate(context, "Matched = Payload.Count.RequireInRange(8, 10).OtherwiseMatch()").Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(AllContexts))]
        public void AStringFieldStartsAPipelineWithNoStart(Context context)
        {
            Evaluate(context, "Matched = Payload.Narrative.MatchContains(\"Smith\")").Should().BeTrue();
            Evaluate(context, "Matched = Payload.Narrative.RequireIsNotNullOrEmpty().MatchStartsWith(\"Jon\")")
                .Should().BeTrue();
            Evaluate(context, "Matched = Payload.Narrative.RequireIsNotNullOrEmpty().MatchStartsWith(\"Bob\")")
                .Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(AllContexts))]
        public void ADateTimeFieldStartsAPipelineWithNoStart(Context context)
        {
            Evaluate(context, "Matched = Payload.Date.MatchIsWeekday()").Should().BeTrue();
            Evaluate(context, "Matched = Payload.Date.MatchIsWeekend()").Should().BeFalse();
            Evaluate(context, "Matched = Payload.Date.MatchWithinBusinessHoursInZone(\"UTC\", 9, 17)")
                .Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(AllContexts))]
        public void ABooleanFieldStartsAPipelineWithNoStart(Context context)
        {
            Evaluate(context, "Matched = Payload.Flag.MatchIsTrue()").Should().BeTrue();
            Evaluate(context, "Matched = Payload.Flag.MatchIsFalse()").Should().BeFalse();
            Evaluate(context, "Matched = Payload.Flag.RequireIsTrue().OtherwiseMatch()").Should().BeTrue();
            Evaluate(context, "Matched = Payload.Flag.RequireIsTrue().OtherwiseMatch()", new Values { Flag = false })
                .Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(AllContexts))]
        public void APlainTransformerCanBeTheFirstStepAndTheNextStepContinuesTheChain(Context context)
        {
            Evaluate(context, "Matched = Payload.Amount.Abs().MatchGreater(100)").Should().BeTrue();
            Evaluate(context, "Matched = Payload.Amount.Round().MatchEqual(150)").Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(AllContexts))]
        public void ATextLengthStepCanFeedAnIntegerStep(Context context)
        {
            Evaluate(context, "Matched = Payload.Narrative.TextLength().MatchEqual(9)").Should().BeTrue();
        }

        [Fact]
        public void TheBarePropertyNameLengthIsRefusedAtParseTimeSoItCannotBypassTheAllowList()
        {
            var parser = NewParser();
            var rule = parser.TranslateFromDotNotation(new ParsedRule
            {
                ErrorSpans = [],
                OriginalRuleText = "Matched = Payload.Narrative.Length.MatchEqual(9)",
                ParsedRuleText = "Matched = Payload.Narrative.Length.MatchEqual(9)"
            });

            parser.Parse(rule).ErrorSpans.Should().NotBeEmpty();
        }

        [Fact]
        public void TheBareMethodNameTrimIsRefusedAtParseTimeSoItCannotBypassTheAllowList()
        {
            var parser = NewParser();
            var rule = parser.TranslateFromDotNotation(new ParsedRule
            {
                ErrorSpans = [],
                OriginalRuleText = "Matched = Payload.Narrative.Trim().MatchEqual(\"Smith\")",
                ParsedRuleText = "Matched = Payload.Narrative.Trim().MatchEqual(\"Smith\")"
            });

            parser.Parse(rule).ErrorSpans.Should().NotBeEmpty();
        }

        [Theory]
        [MemberData(nameof(AllContexts))]
        public void AFlowTransformerCanBeTheFirstStep(Context context)
        {
            Evaluate(context, "Matched = Payload.Name.Lower().MatchContains(\"abc\")").Should().BeTrue();
            Evaluate(context, "Matched = Payload.Narrative.Upper().MatchEqual(\"JON SMITH\")").Should().BeTrue();
            Evaluate(context, "Matched = Payload.Text.ParseDouble().MatchGreater(40)").Should().BeTrue();
            Evaluate(context, "Matched = Payload.Text.ParseInteger().MatchEqual(42)").Should().BeTrue();
            Evaluate(context, "Matched = Payload.Amount.Plus(10).MatchEqual(160)").Should().BeTrue();
            Evaluate(context, "Matched = Payload.Amount.Minus(10).MatchEqual(140)").Should().BeTrue();
            Evaluate(context, "Matched = Payload.Amount.Times(2).MatchEqual(300)").Should().BeTrue();
            Evaluate(context, "Matched = Payload.Amount.DividedBy(3).MatchEqual(50)").Should().BeTrue();
            Evaluate(context, "Matched = Payload.Date.HourOfDay().MatchEqual(10)").Should().BeTrue();
        }

        [Fact]
        public void ARoundWithDigitsOnARawDoubleBindsToTheStaticDoubleRoundInVbSoStartFirst()
        {
            Evaluate(Context.Gateway, "Matched = Payload.Amount.Round(1) = 150").Should().BeFalse();
            Evaluate(Context.Gateway, "Matched = Payload.Amount.Start().Round(1).MatchEqual(150)").Should().BeTrue();
        }

        [Fact]
        public void ADateTransformerThatAlsoExistsAsAPlainMethodStillChains()
        {
            Evaluate(Context.Gateway, "Matched = Payload.Date.AgeInDays().MatchGreater(0)").Should().BeTrue();
        }

        [Fact]
        public void AZoneConversionCanBeTheFirstStepAndFeedsAnHourStep()
        {
            Evaluate(Context.Gateway,
                    "Matched = Payload.Date.ToZone(\"America/New_York\").HourOfDay().MatchEqual(6)")
                .Should().BeTrue();
        }

        [Fact]
        public void ParsedDoubleErrorsRatherThanThrowingOnBadText()
        {
            Evaluate(Context.Gateway, "Matched = Payload.Text.ParseDouble().RequireGreater(0).OtherwiseMatch()",
                new Values { Text = "abc" }).Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(AllContexts))]
        public void AgainstSwitchesFieldAfterAnImplicitStart(Context context)
        {
            const string rule = "Matched = Payload.Amount.RequireGreater(100).Against(Payload.Narrative).MatchContains(\"Smith\")";

            Evaluate(context, rule).Should().BeTrue();
            Evaluate(context, rule, new Values { Amount = 50 }).Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(AllContexts))]
        public void AnExplicitStartStillWorksAndIsHarmlessToRepeat(Context context)
        {
            Evaluate(context, "Matched = Payload.Amount.Start().MatchGreater(100)").Should().BeTrue();
            Evaluate(context, "Matched = Payload.Amount.Start().Start().MatchGreater(100)").Should().BeTrue();
            Evaluate(context, "Matched = Payload.Amount.MatchGreater(100).Start()").Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(AllContexts))]
        public void AListTokenCanBeAnArgumentOfAnImplicitStart(Context context)
        {
            Evaluate(context, "Matched = Payload.Narrative.MatchSomehowInList(List.HighRiskTerms)").Should().BeTrue();
            Evaluate(context, "Matched = Payload.Narrative.MatchSomehowInList(List.HighRiskTerms)",
                new Values { Narrative = "Bob Marley" }).Should().BeFalse();
        }

        [Fact]
        public void ATtlCounterTokenCanBeTheReceiver()
        {
            Evaluate(Context.Activation, "Matched = TTLCounter.CardPayments.MatchGreater(5)").Should().BeTrue();
            Evaluate(Context.Activation, "Matched = TTLCounter.CardPayments.MatchGreater(6)").Should().BeFalse();
        }

        [Fact]
        public void AnAbstractionTokenCanBeTheReceiver()
        {
            Evaluate(Context.Activation, "Matched = Abstraction.Velocity.MatchGreater(2)").Should().BeTrue();
        }

        [Fact]
        public void AnAbstractionCalculationTokenCanBeTheReceiver()
        {
            Evaluate(Context.Activation, "Matched = AbstractionCalculation.Ratio.MatchLess(1)").Should().BeTrue();
        }

        [Fact]
        public void ADictionaryTokenCanBeTheReceiver()
        {
            Evaluate(Context.Activation, "Matched = Dictionary.CountryRisk.MatchGreaterOrEqual(4)").Should().BeTrue();
            Evaluate(Context.Activation, "Matched = Dictionary.CountryRisk.MatchGreaterOrEqual(5)").Should().BeFalse();
        }

        [Fact]
        public void ACounterGuardThenAPayloadStructuringTestReadsAsOneChain()
        {
            const string rule = "Matched = TTLCounter.CardPayments.RequireGreater(5)" +
                                ".Against(Payload.Amount).MatchIsJustBelowThreshold(160, 10)";

            Evaluate(Context.Activation, rule).Should().BeTrue();
            Evaluate(Context.Activation, rule, new Values { Amount = 100 }).Should().BeFalse();
        }

        [Fact]
        public void ADeclineRatioIsARatioOfTwoCountersComparedInTheSameChain()
        {
            Evaluate(Context.Activation,
                "Matched = TTLCounter.DeclinedSpend.RatioOf(TTLCounter.TotalSpend).MatchGreater(0.25)").Should().BeTrue();
            Evaluate(Context.Activation,
                "Matched = TTLCounter.DeclinedSpend.RatioOf(TTLCounter.TotalSpend).MatchGreater(0.35)").Should().BeFalse();
        }

        [Fact]
        public void AShareOfSumIsTheDeclineRatioOverDeclinedPlusApproved()
        {
            Evaluate(Context.Activation,
                "Matched = TTLCounter.DeclinedSpend.ShareOfSumWith(TTLCounter.ApprovedSpend).MatchInRange(0.29, 0.31)")
                .Should().BeTrue();
            Evaluate(Context.Activation,
                "Matched = TTLCounter.DeclinedSpend.MatchShareOfSumWithAbove(TTLCounter.ApprovedSpend, 0.25)")
                .Should().BeTrue();
        }

        [Fact]
        public void ACalculationCanBeChainedIntoAnother()
        {
            Evaluate(Context.Activation,
                "Matched = TTLCounter.DeclinedSpend.RatioOf(TTLCounter.TotalSpend).Complement().MatchLess(0.8)")
                .Should().BeTrue();
            Evaluate(Context.Activation,
                "Matched = TTLCounter.DeclinedSpend.PercentOf(TTLCounter.TotalSpend).MatchGreaterOrEqual(30)")
                .Should().BeTrue();
        }

        [Fact]
        public void APredicateOnACalculationTakesTheBoundsAfterTheOtherValues()
        {
            Evaluate(Context.Activation,
                "Matched = TTLCounter.DeclinedSpend.MatchRatioOfInRange(TTLCounter.TotalSpend, 0.2, 0.4)")
                .Should().BeTrue();
            Evaluate(Context.Activation,
                "Matched = TTLCounter.DeclinedSpend.MatchRatioOfOutsideRange(TTLCounter.TotalSpend, 0.2, 0.4)")
                .Should().BeFalse();
        }

        [Fact]
        public void AParamsCalculationTakesTheBoundFirst()
        {
            Evaluate(Context.Activation,
                "Matched = TTLCounter.DeclinedSpend.MatchSumWithAbove(900, TTLCounter.ApprovedSpend)").Should().BeTrue();
            Evaluate(Context.Activation,
                "Matched = TTLCounter.DeclinedSpend.MatchSumWithAbove(1100, TTLCounter.ApprovedSpend)").Should().BeFalse();
            Evaluate(Context.Activation,
                "Matched = TTLCounter.DeclinedSpend.MaxWith(TTLCounter.ApprovedSpend, TTLCounter.TotalSpend).MatchEqual(1000)")
                .Should().BeTrue();
        }

        [Fact]
        public void ADivisionByAZeroFieldIsUndefinedAndFailsClosed()
        {
            var zero = new Values { Amount = 0 };

            Evaluate(Context.Activation, "Matched = TTLCounter.DeclinedSpend.RatioOf(Payload.Amount).MatchGreater(0)",
                zero).Should().BeFalse();
            Evaluate(Context.Activation, "Matched = TTLCounter.DeclinedSpend.RatioOf(Payload.Amount).MatchLess(1000000)",
                zero).Should().BeFalse();
            Evaluate(Context.Activation, "Matched = TTLCounter.DeclinedSpend.MatchRatioOfAbove(Payload.Amount, 0.1)",
                zero).Should().BeFalse();
            Evaluate(Context.Activation,
                "Matched = TTLCounter.DeclinedSpend.MatchRatioOfOutsideRange(Payload.Amount, 0, 1)", zero)
                .Should().BeFalse();
        }

        [Fact]
        public void ACalculationComposesWithGuardsAndAnotherFieldThroughAgainst()
        {
            const string rule = "Matched = TTLCounter.TotalSpend.RequireGreater(500)" +
                                ".Against(TTLCounter.DeclinedSpend.RatioOf(TTLCounter.TotalSpend))" +
                                ".MatchGreater(0.25)";

            Evaluate(Context.Activation, rule).Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(AllContexts))]
        public void OrBetweenTwoPipelinesOfTheSameTypeCompiles(Context context)
        {
            Evaluate(context, "Matched = Payload.Amount.MatchGreater(100) Or Payload.Amount.MatchLess(10)")
                .Should().BeTrue();
            Evaluate(context, "Matched = Payload.Amount.MatchLess(100) Or Payload.Amount.MatchLess(10)")
                .Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(AllContexts))]
        public void AndBetweenTwoPipelinesOfTheSameTypeCompiles(Context context)
        {
            Evaluate(context, "Matched = Payload.Amount.MatchGreater(100) And Payload.Amount.MatchLess(200)")
                .Should().BeTrue();
            Evaluate(context, "Matched = Payload.Amount.MatchGreater(100) And Payload.Amount.MatchLess(120)")
                .Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(AllContexts))]
        public void OrAndAndBetweenPipelinesOfDifferentTypesCompile(Context context)
        {
            Evaluate(context, "Matched = Payload.Amount.MatchGreater(1000) Or Payload.Narrative.MatchContains(\"Smith\")")
                .Should().BeTrue();
            Evaluate(context, "Matched = Payload.Amount.MatchGreater(100) And Payload.Count.MatchGreater(5)")
                .Should().BeTrue();
            Evaluate(context, "Matched = Payload.Amount.MatchGreater(100) And Payload.Flag.MatchIsFalse()")
                .Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(AllContexts))]
        public void APipelineCombinesWithAPlainBooleanExpression(Context context)
        {
            Evaluate(context, "Matched = Payload.Amount.MatchGreater(1000) Or Payload.Count > 5").Should().BeTrue();
            Evaluate(context, "Matched = Payload.Amount.MatchGreater(100) And Payload.Count > 5").Should().BeTrue();
            Evaluate(context, "Matched = Payload.Amount.MatchGreater(1000) Or Payload.Count > 50").Should().BeFalse();
        }

        [Fact]
        public void APlainBooleanOnTheLeftOfAPipelineDoesNotCompileSoThePipelineGoesFirst()
        {
            var act = () => Evaluate(Context.Gateway, "Matched = Payload.Count > 5 And Payload.Amount.MatchGreater(100)");

            act.Should().Throw<Exception>();
        }

        [Theory]
        [MemberData(nameof(AllContexts))]
        public void NotAndGroupingWorkOnPipelines(Context context)
        {
            Evaluate(context, "Matched = Not Payload.Amount.MatchGreater(1000)").Should().BeTrue();
            Evaluate(context, "Matched = Not Payload.Amount.MatchGreater(100)").Should().BeFalse();
            Evaluate(context,
                    "Matched = (Payload.Amount.MatchGreater(1000) Or Payload.Count.MatchGreater(5)) And Not Payload.Flag.MatchIsFalse()")
                .Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(AllContexts))]
        public void AnUndecidedPipelineIsFalseInACombination(Context context)
        {
            Evaluate(context, "Matched = Payload.Amount.RequireGreater(100) Or Payload.Amount.RequireGreater(1)")
                .Should().BeFalse();
            Evaluate(context, "Matched = Payload.Amount.RequireGreater(100) Or Payload.Amount.MatchGreater(1)")
                .Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(AllContexts))]
        public void APipelineCanBeUsedDirectlyInAnIfWithOr(Context context)
        {
            const string rule = "If Payload.Amount.MatchGreater(1000) Or Payload.Count.MatchGreater(5) Then\r\nMatched = True\r\nEnd If";

            Evaluate(context, rule).Should().BeTrue();
        }

        [Fact]
        public void TheIsMatchedFormStillWorks()
        {
            Evaluate(Context.Gateway,
                    "Matched = Payload.Amount.MatchGreater(1000).IsMatched() Or Payload.Count.MatchGreater(5).IsMatched()")
                .Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(AllContexts))]
        public void ANaNInputFailsClosed(Context context)
        {
            var values = new Values { Amount = double.NaN };

            Evaluate(context, "Matched = Payload.Amount.MatchGreater(100)", values).Should().BeFalse();
            Evaluate(context, "Matched = Payload.Amount.MatchLessOrEqual(100)", values).Should().BeFalse();
            Evaluate(context, "Matched = Payload.Amount.RequireHasValue().OtherwiseMatch()", values).Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(AllContexts))]
        public void ANullStringFailsClosedRatherThanThrowing(Context context)
        {
            var values = new Values { Narrative = null };

            Evaluate(context, "Matched = Payload.Narrative.MatchContains(\"Smith\")", values).Should().BeFalse();
            Evaluate(context, "Matched = Payload.Narrative.MatchEqual(\"Smith\")", values).Should().BeFalse();
            Evaluate(context, "Matched = Payload.Narrative.RequireIsNotNullOrEmpty().OtherwiseMatch()", values)
                .Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(AllContexts))]
        public void ATerminalAloneOnAFieldIsNotAnEntryPoint(Context context)
        {
            var parser = NewParser();
            var rule = parser.TranslateFromDotNotation(new ParsedRule
            {
                ErrorSpans = [],
                OriginalRuleText = "Matched = Payload.Amount.Match()",
                ParsedRuleText = "Matched = Payload.Amount.Match()"
            });
            rule = parser.Parse(rule);
            rule = context == Context.Gateway ? parser.WrapGatewayRule(rule, true) : parser.WrapActivationRule(rule, true);

            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => a.Location)
                .ToArray();
            var compile = new Compile();
            compile.CompileCode(rule.ParsedRuleText, TestLog.NoOp, references, Compile.Language.Vb);

            compile.Success.Should().BeFalse();
        }
    }
}
