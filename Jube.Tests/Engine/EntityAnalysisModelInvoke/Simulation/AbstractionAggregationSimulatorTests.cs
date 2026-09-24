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
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.AbstractionRulesWithSearchKeys;
using Jube.Engine.EntityAnalysisModelInvoke.Simulation;
using Jube.Parser;
using Jube.Test.Infrastructure;
using Jube.Test.RuleParser;
using Xunit;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke.Simulation
{
    [Trait("Category", "Unit")]
    public sealed class AbstractionAggregationSimulatorTests
    {
        private const string ReferenceDateName = "TxDate";
        private static readonly DateTime now = new(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc);

        private static readonly InvocationContextField[] fields =
        [
            new("Payload.Amount", "Payload", "double"),
            new("Payload.Country", "Payload", "string")
        ];

        private static readonly SearchKeySettings accountKey = new("AccountId", "d", 30, 100, false);

        private static RuleParseResult Parse(string text)
        {
            var parsed = RuleParse.Execute(text, RuleParse.AbstractionRule, RuleParseTests.Environment(),
                TestLog.NoOp, RuleParse.EngineReferences(RuleParse.AbstractionRule), engineWrap: true);
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

        private static AbstractionSettings Settings(int functionType, string interval = "h", int value = 24,
            string functionKey = "Amount")
        {
            return AbstractionSettings.FromRecord(7, "Velocity", "AccountId", functionKey, functionType, interval,
                value, false, null, null);
        }

        [Fact]
        public async Task CountsTheMatchingTransactionsInsideTheWindowIncludingTheCurrentOneAsync()
        {
            var result = await AbstractionAggregationSimulator.SimulateAsync(
                Parse("If (Payload.Country = \"GB\") Then\n   Return True\nEnd If"), Settings(1), accountKey,
                Current(10, "GB"),
                [Past(5, "GB", 1), Past(5, "FR", 2), Past(5, "GB", 30), Past(5, "GB", 3)], now, ReferenceDateName);

            result.Error.Should().BeNull();
            result.DocumentsEvaluated.Should().Be(5);
            result.Matched.Should().Be(4);
            result.InWindow.Should().Be(3);
            result.Value.Should().Be(3);
            result.WindowFrom.Should().Be(now.AddHours(-24));
        }

        [Fact]
        public async Task SumsTheFunctionKeyOverTheMatchesInTheWindowAsync()
        {
            var result = await AbstractionAggregationSimulator.SimulateAsync(
                Parse("If (Payload.Country = \"GB\") Then\n   Return True\nEnd If"), Settings(3), accountKey,
                Current(10, "GB"), [Past(5, "GB", 1), Past(7, "GB", 2), Past(100, "GB", 48)], now,
                ReferenceDateName);

            result.Value.Should().Be(22);
        }

        [Fact]
        public async Task ASearchKeyWithAShorterTtlShortensTheWindowAndSaysSoAsync()
        {
            var result = await AbstractionAggregationSimulator.SimulateAsync(
                Parse("If (Payload.Country = \"GB\") Then\n   Return True\nEnd If"), Settings(1, "d", 30),
                accountKey with { TtlInterval = "d", TtlIntervalValue = 7 }, Current(10, "GB"),
                [Past(5, "GB", 24 * 3), Past(5, "GB", 24 * 10)], now, ReferenceDateName);

            result.WindowShortenedBySearchKey.Should().BeTrue();
            result.WindowFrom.Should().Be(now.AddDays(-7));
            result.InWindow.Should().Be(2);
        }

        [Fact]
        public async Task ARuleThatThrowsIsReportedAsync()
        {
            var parsed = Parse("If (List.HighRiskTerms.Contains(Payload.Country)) Then\n   Return True\nEnd If");

            var result = await AbstractionAggregationSimulator.SimulateAsync(parsed, Settings(1), accountKey,
                Current(10, "GB"), [Past(5, "GB", 1)], now, ReferenceDateName);

            result.Error.Should().BeOfType<KeyNotFoundException>();
            result.Value.Should().BeNull();
        }

        [Fact]
        public void EngineDefaultsApplyWhenTheRecordLeavesFieldsUnset()
        {
            var settings = AbstractionSettings.FromRecord(1, "Has Space", "K", "F", null, null, 12, null, null, null);

            settings.Should().Be(new AbstractionSettings(1, "Has_Space", "K", "F", 1, "d", 0, false, 0, 0));
        }

        [Fact]
        public void CachedPayloadsDecodeByIndexWithTheReferenceDateAtMinusOne()
        {
            var raw = new DictionaryNoBoxing<int>();
            raw.TryAdd(-1, now);
            raw.TryAdd(3, 12.5);
            raw.TryAdd(9, "unknown index");
            raw.TryAdd(-2, "ignored");

            var decoded = CachedPayloadDecoder.Decode(
                new Dictionary<string, DictionaryNoBoxing<int>> { ["p1"] = raw },
                new Dictionary<int, string> { [3] = "Amount" }, ReferenceDateName);

            decoded["p1"][ReferenceDateName].AsDateTime().Should().Be(now);
            decoded["p1"]["Amount"].AsDouble().Should().Be(12.5);
            decoded["p1"].Count.Should().Be(2);
        }
    }
}