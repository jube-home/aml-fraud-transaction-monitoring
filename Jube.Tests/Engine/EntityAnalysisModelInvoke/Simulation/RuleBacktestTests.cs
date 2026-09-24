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
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Query.Models;
using Jube.Data.QueryBuilder;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelInvoke.Simulation;
using Jube.Engine.Helpers;
using Jube.Parser;
using Jube.Test.Infrastructure;
using Jube.Test.RuleParser;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke.Simulation
{
    [Trait("Category", "Unit")]
    public sealed class RuleBacktestTests
    {
        private static readonly DateTime start = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

        private static readonly InvocationContextField[] fields =
        [
            new("Payload.Amount", "Payload", "double"),
            new("Payload.Country", "Payload", "string"),
            new("Abstraction.Velocity", "Abstraction", "double")
        ];

        private static RuleParseEnvironmentDto Environment()
        {
            var environment = RuleParseTests.Environment();
            environment.TtlCounters = [];
            environment.AbstractionRules = ["Velocity"];
            environment.Sanctions = [];
            environment.AbstractionCalculations = [];
            environment.HttpAdaptations = [];
            environment.ExhaustiveAdaptations = [];
            environment.ActivationRules = [];
            return environment;
        }

        private static BacktestRow Row(int index, double amount, double velocity, params string[] tags)
        {
            var payload = new EntityAnalysisModelInstanceEntryPayload
            {
                Payload = new DictionaryNoBoxing<string>(),
                EntityInstanceEntryId = "TX" + index,
                ReferenceDate = start.AddHours(index),
                Abstraction = new PooledDictionary<string, double> { ["Velocity"] = velocity }
            };
            payload.Payload.TryAdd("Amount", amount);
            payload.Payload.TryAdd("Country", "GB");
            var json = JObject.Parse(Encoding.UTF8.GetString(
                BuildJsonResponses.BuildFullJson(payload, new JsonSerializationHelper().ArchiveJsonSerializer)));
            return new BacktestRow(Guid.NewGuid(), "TX" + index, payload.ReferenceDate, json, tags);
        }

        private static readonly BacktestRow[] rows =
        [
            Row(0, 500, 9, "Fraud"),
            Row(1, 700, 1),
            Row(2, 50, 8, "Fraud", "Reviewed"),
            Row(3, 20, 0),
            Row(4, 900, 7, "Fraud"),
            Row(5, 10, 1, "Reviewed")
        ];

        private static RuleBacktest Backtest(string rule, int type = RuleParse.ActivationRule, string? filter = null,
            string? classJson = null, int sampleSize = 20)
        {
            return new RuleBacktest(Plan(rule, type, filter, classJson, sampleSize));
        }

        private static BacktestPlan Plan(string rule, int type = RuleParse.ActivationRule, string? filter = null,
            string? classJson = null, int sampleSize = 20)
        {
            var fieldTypes = new Dictionary<string, BuilderField>
            {
                ["Payload.Amount"] = new("Payload.Amount", BuilderFieldType.Double),
                ["Payload.Country"] = new("Payload.Country", BuilderFieldType.String),
                ["Abstraction.Velocity"] = new("Abstraction.Velocity", BuilderFieldType.Double),
                ["Tag.Fraud"] = new("Tag.Fraud", BuilderFieldType.Boolean),
                ["Tag.Reviewed"] = new("Tag.Reviewed", BuilderFieldType.Boolean)
            };
            Func<IReadOnlyDictionary<string, object>, bool>? classPredicate = null;
            if (classJson != null)
            {
                var parsed = BuilderProfile.Parse(classJson, fieldTypes);
                parsed.Valid.Should().BeTrue(string.Join("; ", parsed.Errors.Select(e => e.Message)));
                classPredicate = BuilderFilter.CompileValues(parsed.Group);
            }

            return new BacktestPlan(1, Parse(rule, type, false), type, false,
                filter == null ? null : Parse(filter, RuleParse.GatewayRule, true), classPredicate,
                ["Fraud", "Reviewed"], fields, [], sampleSize);
        }

        private static RuleParseResult Parse(string text, int type, bool reprocessing)
        {
            var parsed = RuleParse.Execute(text, type, Environment(), TestLog.NoOp, RuleParse.EngineReferences(type),
                engineWrap: true, reprocessing: reprocessing);
            parsed.Compiled.Should().BeTrue(parsed.Message);
            return parsed;
        }

        private const string Fraud =
            "{\"condition\":\"AND\",\"rules\":[{\"id\":\"Tag.Fraud\",\"operator\":\"equal\",\"value\":\"True\"}]}";

        [Theory]
        [InlineData(true, true, BacktestOutcome.TruePositive)]
        [InlineData(true, false, BacktestOutcome.FalsePositive)]
        [InlineData(false, true, BacktestOutcome.FalseNegative)]
        [InlineData(false, false, BacktestOutcome.TrueNegative)]
        public void EachOutcomeIsTheCellOfTheConfusionMatrix(bool fired, bool positive, BacktestOutcome expected)
        {
            RuleBacktest.Classify(fired, positive).Should().Be(expected);
        }

        [Fact]
        public async Task AnActivationRuleIsCountedAgainstATagClassFromArchivedValuesAsync()
        {
            var backtest = Backtest(
                "If (Payload.Amount > 100 AND Abstraction.Velocity > 5) Then\n   Return True\nEnd If",
                classJson: Fraud);

            (await backtest.AddAsync(rows)).Should().BeTrue();
            var result = backtest.Result;

            (result.Scanned, result.Evaluated, result.FilteredOut).Should().Be((6, 6, 0));
            (result.Fired, result.NotFired, result.Positives).Should().Be((2, 4, 3));
            (result.TruePositives, result.FalsePositives, result.FalseNegatives, result.TrueNegatives).Should()
                .Be((2, 0, 1, 3));
            result.ClassDefined.Should().BeTrue();
            result.TruePositiveSamples.Select(s => s.EntryKeyValue).Should().Equal("TX0", "TX4");
            result.FalseNegativeSamples.Should().ContainSingle().Which.EntryKeyValue.Should().Be("TX2");
            result.TagsInSample.Should().Equal(new Dictionary<string, long> { ["Fraud"] = 3, ["Reviewed"] = 2 });
            (result.EarliestReferenceDate, result.LatestReferenceDate).Should().Be((start, start.AddHours(5)));
        }

        [Fact]
        public async Task TheClassCanCombineTagsAndValuesAsync()
        {
            const string classJson = "{\"condition\":\"OR\",\"rules\":[" +
                                     "{\"id\":\"Tag.Reviewed\",\"operator\":\"equal\",\"value\":\"True\"}," +
                                     "{\"id\":\"Payload.Amount\",\"operator\":\"greater\",\"value\":800}]}";
            var backtest = Backtest("If (Payload.Amount > 0) Then\n   Return True\nEnd If", classJson: classJson);

            await backtest.AddAsync(rows);

            backtest.Result.Positives.Should().Be(3);
            backtest.Result.TruePositives.Should().Be(3);
            backtest.Result.FalsePositives.Should().Be(3);
        }

        [Fact]
        public async Task TheFilterRunsAsAReprocessingRuleAndOnlyKeptRowsAreEvaluatedAsync()
        {
            var backtest = Backtest("If (Payload.Amount > 100) Then\n   Return True\nEnd If",
                filter: "If (Payload.Amount >= 50) Then\n   Return True\nEnd If", classJson: Fraud);

            await backtest.AddAsync(rows);

            (backtest.Result.Scanned, backtest.Result.FilteredOut, backtest.Result.Evaluated).Should().Be((6, 2, 4));
            (backtest.Result.TruePositives, backtest.Result.FalsePositives, backtest.Result.FalseNegatives,
                backtest.Result.TrueNegatives).Should().Be((2, 1, 1, 0));
        }

        [Fact]
        public async Task WithoutAClassOnlyFiringIsCountedAndPagesAccumulateAsync()
        {
            var backtest = Backtest("If (Payload.Amount > 100) Then\n   Return True\nEnd If", RuleParse.GatewayRule);

            await backtest.AddAsync(rows.Take(3));
            await backtest.AddAsync(rows.Skip(3));

            backtest.Result.Scanned.Should().Be(6);
            backtest.Result.Fired.Should().Be(3);
            backtest.Result.ClassDefined.Should().BeFalse();
            (backtest.Result.TruePositives + backtest.Result.FalsePositives + backtest.Result.FalseNegatives +
             backtest.Result.TrueNegatives).Should().Be(0);
        }

        [Fact]
        public async Task SamplesAreCappedButCountsAreNotAsync()
        {
            var backtest = Backtest("If (Payload.Amount > 0) Then\n   Return True\nEnd If", classJson: Fraud,
                sampleSize: 1);

            await backtest.AddAsync(rows);

            backtest.Result.FalsePositives.Should().Be(3);
            backtest.Result.FalsePositiveSamples.Should().ContainSingle();
        }

        [Fact]
        public async Task ARuntimeErrorCountsAsNotFiredAsTheEngineTreatsItAsync()
        {
            var backtest = Backtest("If (List.HighRiskTerms.Contains(Payload.Country)) Then\n   Return True\nEnd If",
                RuleParse.GatewayRule, classJson: Fraud);

            await backtest.AddAsync(rows.Take(2));

            backtest.Result.RuntimeErrors.Should().Be(2);
            backtest.Result.NotFired.Should().Be(2);
            (backtest.Result.FalseNegatives, backtest.Result.TrueNegatives).Should().Be((1, 1));
            backtest.Result.ErrorSamples.Should().HaveCount(2).And
                .OnlyContain(s => s.Error.StartsWith("KeyNotFoundException"));
        }

        [Fact]
        public async Task AnExplanationGivesTheOutcomeAndTheValuesReadAsync()
        {
            var plan = Plan("If (Payload.Amount > 100) Then\n   Return True\nEnd If",
                filter: "If (Payload.Amount >= 50) Then\n   Return True\nEnd If", classJson: Fraud);

            var explained = await RuleBacktest.ExplainAsync(plan, rows[2]);

            explained.PassedFilter.Should().BeTrue();
            explained.Fired.Should().BeFalse();
            explained.Positive.Should().BeTrue();
            explained.Tags.Should().BeEquivalentTo("Fraud", "Reviewed");
            explained.Values.Should().ContainSingle(v => v.Name == "Payload.Amount" && v.Value == "50");
        }
    }
}