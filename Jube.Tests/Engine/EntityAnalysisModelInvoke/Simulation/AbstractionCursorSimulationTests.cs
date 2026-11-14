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
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Simulation;
using Jube.Engine.EntityAnalysisModelInvoke.Simulation.Models;
using Jube.Parser;
using Jube.Parser.Models;
using Jube.Test.Infrastructure;
using Jube.Test.RuleParser;
using Xunit;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke.Simulation
{
    [Trait("Category", "Unit")]
    public sealed class AbstractionCursorSimulationTests
    {
        private const string ReferenceDateName = "TxDate";
        private const int Count = 1;
        private const int Sum = 3;
        private const int ActualValue = 13;
        private const int CursorResult = 46;
        private const int Ascending = 1;
        private const int Descending = 2;
        private const string All = "Matched = True";
        private static readonly DateTime now = new(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc);

        private static readonly InvocationContextField[] fields =
        [
            new("Payload.Amount", "Payload", "double"),
            new("Payload.Country", "Payload", "string")
        ];

        private static readonly SearchKeySettings accountKey = new("AccountId", "d", 30, 100, false);

        private static RuleParseResult Parse(string text, bool tryCatchWrap = false)
        {
            var parsed = RuleParse.Execute(text, RuleParse.AbstractionRule, RuleParseTests.Environment(),
                TestLog.NoOp, RuleParse.EngineReferences(RuleParse.AbstractionRule), tryCatchWrap,
                engineWrap: true);

            parsed.Compiled.Should().BeTrue(parsed.Message);

            return parsed;
        }

        private static RuleRunInputs Current(double amount, string country)
        {
            var context = InvocationContextBuilder.Blank(1, fields, [], false, now);

            InvocationContextBuilder.Overlay(context, new Dictionary<string, string>
            {
                ["Payload.Amount"] = amount.ToString(CultureInfo.InvariantCulture),
                ["Payload.Country"] = country
            });

            return RuleRunner.ToInputs(context, new Dictionary<string, List<string>>());
        }

        private static DictionaryNoBoxing<string> Past(double amount, string country, double hoursAgo)
        {
            var document = new DictionaryNoBoxing<string>();
            document.TryAdd("Amount", amount);
            document.TryAdd("Country", country);
            document.TryAdd(ReferenceDateName, now.AddHours(-hoursAgo));

            return document;
        }

        private static List<DictionaryNoBoxing<string>> History()
        {
            return
            [
                Past(10, "GB", 5), Past(20, "FR", 4), Past(30, "GB", 3), Past(40, "GB", 2), Past(50, "GB", 1)
            ];
        }

        private static async Task<AbstractionSimulationResult> RunAsync(string rule, int functionType,
            int iterationOrder = Ascending, List<DictionaryNoBoxing<string>>? history = null,
            bool tryCatchWrap = false, string? parameters = null)
        {
            var settings = AbstractionSettings.FromRecord(7, "Cursor", "AccountId", "Amount", functionType, "h", 24,
                false, null, null, iterationOrder, parameters);

            var result = await AbstractionAggregationSimulator.SimulateAsync(Parse(rule, tryCatchWrap), settings,
                accountKey, Current(60, "GB"), history ?? History(), now, ReferenceDateName);

            result.Error.Should().BeNull();

            return result;
        }

        private static double[] Amounts(AbstractionSimulationResult result)
        {
            return [.. result.MatchedInWindow.Select(d => d["Amount"].AsDouble())];
        }

        [Fact]
        public async Task TheThirdAndFifthTransactionAreSelectedByPositionAsync()
        {
            var result = await RunAsync(
                "Matched = Cursor.Position = 3 OR Cursor.Position = 5\nIf Cursor.Position >= 5 Then Exit For", Sum);

            Amounts(result).Should().Equal(30, 50);
            result.Value.Should().Be(80);
        }

        [Fact]
        public async Task DescendingOrderCountsPositionsFromTheMostRecentTransactionAsync()
        {
            var result = await RunAsync(
                "Matched = Cursor.Position = 3 OR Cursor.Position = 5\nIf Cursor.Position >= 5 Then Exit For", Sum,
                Descending);

            Amounts(result).Should().Equal(40, 20);
            result.Value.Should().Be(60);
        }

        [Fact]
        public async Task TheThirdAndFifthMatchingTransactionAreSelectedWithAScanVariableAsync()
        {
            const string rule = "Static hits As Integer\n" +
                                "If Payload.Country <> \"GB\" Then Continue For\n" +
                                "hits += 1\n" +
                                "Matched = hits = 3 OR hits = 5\n" +
                                "If hits >= 5 Then Exit For";

            foreach (var tryCatchWrap in new[] { false, true })
            {
                var result = await RunAsync(rule, Sum, tryCatchWrap: tryCatchWrap);

                Amounts(result).Should().Equal(40, 60);
                result.Value.Should().Be(100);
            }
        }

        [Fact]
        public async Task AScanVariableCountsFromTheMostRecentTransactionUnderDescendingOrderAsync()
        {
            var result = await RunAsync("Static seen As Integer\nseen += 1\nMatched = seen <= 2", Sum, Descending);

            Amounts(result).Should().Equal(60, 50);
        }

        [Fact]
        public async Task ARunningTotalInAScanVariableEndsTheScanAtItsLimitAsync()
        {
            var result = await RunAsync(
                "Static total As Double\ntotal += Payload.Amount\nMatched = True\nIf total >= 60 Then Exit For", Sum);

            Amounts(result).Should().Equal(10, 20, 30);
            result.Value.Should().Be(60);
        }

        [Fact]
        public async Task ATextScanVariableRemembersThePreviousTransactionAsync()
        {
            var result = await RunAsync(
                "Static previous As String = \"\"\n" +
                "Matched = previous <> \"\" AND previous <> Payload.Country\n" +
                "previous = Payload.Country", Count);

            Amounts(result).Should().Equal(20, 30);
        }

        [Fact]
        public async Task AScanVariableStartsFromItsLiteralAsync()
        {
            var result = await RunAsync(
                "Static remaining As Integer = 2\nMatched = remaining > 0\nremaining = remaining - 1", Count);

            Amounts(result).Should().Equal(10, 20);
        }

        [Fact]
        public async Task ARecordVariableStartsAfreshForEveryTransactionAsync()
        {
            var counted = await RunAsync("Dim seen As Integer\nseen += 1\nMatched = seen = 1", Count);
            var doubled = await RunAsync("Dim doubled As Double = Payload.Amount * 2\nMatched = doubled >= 100", Sum);

            counted.Value.Should().Be(6);
            Amounts(doubled).Should().Equal(50, 60);
        }

        [Fact]
        public async Task AScanVariableFeedsTheCursorResultAsync()
        {
            var result = await RunAsync(
                "Static biggest As Double\n" +
                "If Payload.Amount > biggest Then biggest = Payload.Amount\n" +
                "Cursor.Result = biggest", CursorResult);

            result.Value.Should().Be(60);
        }

        [Fact]
        public async Task ExitForAfterSettingMatchedAcceptsTheRecordAndEndsTheScanAsync()
        {
            const string rule = "If Payload.Amount >= 30 Then\n   Matched = True\n   Exit For\nEnd If";

            Amounts(await RunAsync(rule, Count)).Should().Equal(30);
            Amounts(await RunAsync(rule, Count, Descending)).Should().Equal(60);
        }

        [Fact]
        public async Task ExitForWithoutMatchedEndsTheScanWithoutAcceptingTheRecordAsync()
        {
            var result = await RunAsync("If Payload.Amount >= 30 Then Exit For\nMatched = True", Count);

            Amounts(result).Should().Equal(10, 20);
        }

        [Fact]
        public async Task ContinueForSkipsTheRecordAndCarriesOnAsync()
        {
            var result = await RunAsync("If Payload.Country = \"FR\" Then Continue For\nMatched = True", Count);

            Amounts(result).Should().Equal(10, 30, 40, 50, 60);
        }

        [Fact]
        public async Task ReturnStillAnswersForTheRecordWithoutEndingTheScanAsync()
        {
            var result = await RunAsync("If Cursor.Position = 2 Then\n   Return True\nEnd If\nReturn False", Count);

            Amounts(result).Should().Equal(20);
        }

        [Fact]
        public async Task PositionAndSizeDescribeTheRulesWindowNotTheWholeFetchAsync()
        {
            var history = History();
            history.Insert(0, Past(999, "GB", 48));

            Amounts(await RunAsync("Matched = Cursor.Position = 1", Sum, history: history)).Should().Equal(10);
            (await RunAsync("Matched = Cursor.Size = 6", Count, history: history)).Value.Should().Be(6);
        }

        [Fact]
        public async Task FirstLastAndCurrentAreKnownToTheCursorAsync()
        {
            Amounts(await RunAsync("Matched = Cursor.IsFirst OR Cursor.IsLast", Sum)).Should().Equal(10, 60);
            Amounts(await RunAsync("Matched = Cursor.IsCurrent", Sum)).Should().Equal(60);
            Amounts(await RunAsync("Matched = Cursor.IsCurrent", Sum, Descending)).Should().Equal(60);
        }

        [Fact]
        public async Task MatchesCountsTheRecordsAcceptedSoFarAsync()
        {
            var result = await RunAsync("If Cursor.Matches >= 2 Then Exit For\nMatched = Payload.Country = \"GB\"",
                Count);

            Amounts(result).Should().Equal(10, 30);
        }

        [Fact]
        public async Task ARuleWithoutTheCursorKeepsItsMatchesAndOnlyChangesOrderWhenDescendingAsync()
        {
            const string rule = "If (Payload.Country = \"GB\") Then\n   Return True\nEnd If";

            var ascending = await RunAsync(rule, ActualValue);
            var descending = await RunAsync(rule, ActualValue, Descending);

            Amounts(ascending).Should().Equal(10, 30, 40, 50, 60);
            Amounts(descending).Should().Equal(60, 50, 40, 30, 10);
            ascending.Value.Should().Be(60);
            descending.Value.Should().Be(10);
        }

        [Fact]
        public async Task AnExceptionInOneRecordIsLoggedAndTheScanCarriesOnAsync()
        {
            var result = await RunAsync(
                "Matched = True\nIf Cursor.Position = 2 Then\n   Matched = List.HighRiskTerms.Contains(Payload.Country)\nEnd If",
                Count, tryCatchWrap: true);

            result.Value.Should().Be(6);
        }

        [Fact]
        public async Task ACatalogueFunctionAggregatesTheMatchesWithItsDefaultParametersAsync()
        {
            (await RunAsync(All, 24)).Value.Should().Be(50);
            (await RunAsync(All, 26)).Value.Should().BeApproximately(57.5, 1e-9);
            (await RunAsync(All, 44)).Value.Should().BeApproximately(10, 1e-9);
        }

        [Fact]
        public async Task ACatalogueFunctionTakesItsStaticParametersFromTheRuleAsync()
        {
            (await RunAsync(All, 26, parameters: "{\"percentile\": 50}")).Value.Should().BeApproximately(35, 1e-9);
            (await RunAsync(All, 35, parameters: "{\"threshold\": 35}")).Value.Should().Be(3);
            (await RunAsync(All, 19, parameters: "{\"trimFraction\": 0.34}")).Value.Should().BeApproximately(35, 1e-9);
        }

        [Fact]
        public async Task AnOrderSensitiveFunctionFollowsTheIterationOrderAsync()
        {
            const string second = "{\"position\": 2}";

            (await RunAsync(All, 32, parameters: second)).Value.Should().Be(20);
            (await RunAsync(All, 32, Descending, parameters: second)).Value.Should().Be(50);
            (await RunAsync(All, 33)).Value.Should().Be(50);
            (await RunAsync(All, 33, Descending)).Value.Should().Be(-50);
        }

        [Fact]
        public async Task TheCursorAndACatalogueFunctionComposeAsync()
        {
            var result = await RunAsync("Matched = Cursor.Position <= 3\nIf Cursor.Position >= 3 Then Exit For", 24,
                Descending);

            Amounts(result).Should().Equal(60, 50, 40);
            result.Value.Should().Be(20);
        }

        [Fact]
        public async Task AFunctionThatCannotBeComputedAggregatesToZeroAsync()
        {
            (await RunAsync("Matched = Cursor.Position = 1", 40)).Value.Should().Be(0);
        }

        [Fact]
        public async Task ARuleReturnsItsOwnAggregationThroughTheCursorResultAsync()
        {
            (await RunAsync("Cursor.Result = Cursor.Result + Payload.Amount * 0.5", CursorResult)).Value.Should()
                .BeApproximately(105, 1e-9);

            (await RunAsync(
                "If Payload.Country <> \"GB\" Then Continue For\nCursor.Result = Cursor.Result + 1", CursorResult))
                .Value.Should().Be(5);
        }

        [Fact]
        public async Task TheCursorResultStopsGrowingWhenTheScanIsEndedAndFollowsTheIterationOrderAsync()
        {
            const string rule = "Cursor.Result = Cursor.Result + Payload.Amount\nIf Cursor.Position >= 2 Then Exit For";

            (await RunAsync(rule, CursorResult)).Value.Should().Be(30);
            (await RunAsync(rule, CursorResult, Descending)).Value.Should().Be(110);
        }

        [Fact]
        public async Task TheCursorResultIsZeroWhenTheRuleNeverSetsItAndIgnoresWhichRecordsMatchedAsync()
        {
            (await RunAsync(All, CursorResult)).Value.Should().Be(0);

            var result = await RunAsync("Cursor.Result = 7\nMatched = Cursor.Position = 1", CursorResult);

            Amounts(result).Should().Equal(10);
            result.Value.Should().Be(7);
        }

        [Fact]
        public void TheParseResultSaysWhetherTheRuleUsesTheCursor()
        {
            Parse("Matched = Cursor.Position = 1").UsesCursor.Should().BeTrue();
            Parse("If Payload.Amount > 1 Then Exit For\nMatched = True").UsesCursor.Should().BeTrue();
            Parse("Matched = Payload.Amount > 1").UsesCursor.Should().BeFalse();
            Parse("Static seen As Integer\nseen += 1\nMatched = seen = 1").UsesCursor.Should().BeTrue();
            Parse("Dim seen As Integer\nseen += 1\nMatched = seen = 1").UsesCursor.Should().BeFalse();
        }
    }
}
