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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.AbstractionRulesWithSearchKeys;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using Jube.Test.Infrastructure;
using StackExchange.Redis;
using Xunit;
using Context = Jube.Engine.EntityAnalysisModelInvoke.Context.Context;
using EntityAnalysisModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;
using EntityAnalysisModelAbstractionRule =
    Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelAbstractionRule;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke.ContextExtensions.AbstractionRulesWithSearchKeys
{
    [Trait("Category", "Unit")]
    public sealed class EntityAnalysisModelAbstractionRuleExecutionUtilityTests
    {
        private const string ReferenceDateName = "TxDate";
        private const string SearchKey = "AccountId";
        private static readonly DateTime now = new(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc);

        private static EntityAnalysisModelAbstractionRule Rule(int id, string logicHash,
            EntityAnalysisModelAbstractionRule.Match match, string interval = "h", int value = 24,
            string searchKey = SearchKey, bool search = true)
        {
            return new EntityAnalysisModelAbstractionRule
            {
                Id = id,
                Name = "Rule" + id,
                Search = search,
                SearchKey = searchKey,
                LogicHash = logicHash,
                AbstractionRuleAggregationFunctionIntervalType = interval,
                AbstractionHistoryIntervalValue = value,
                AbstractionRuleCompileDelegate = match
            };
        }

        private static DictionaryNoBoxing<string> Document(double amount, double hoursAgo)
        {
            var document = new DictionaryNoBoxing<string>();
            document.TryAdd("Amount", amount);
            document.TryAdd(ReferenceDateName, now.AddHours(-hoursAgo));

            return document;
        }

        private static async Task<ConcurrentDictionary<int, List<DictionaryNoBoxing<string>>>> RunAsync(
            IReadOnlyList<DictionaryNoBoxing<string>> history, DictionaryNoBoxing<string> current,
            EntityAnalysisModelAbstractionRule[] rules, string ttlInterval = "d", int ttlValue = 30,
            IReadOnlyCollection<int>? absentFromPayloadMap = null)
        {
            var entityAnalysisModel = new EntityAnalysisModel();
            entityAnalysisModel.Snapshot.ModelAbstractionRules.AddRange(rules);
            entityAnalysisModel.References.ReferenceDateName = ReferenceDateName;

            var payload = new EntityAnalysisModelInstanceEntryPayload
            {
                Payload = current,
                ReferenceDate = now,
                Dictionary = new PooledDictionary<string, double>(),
                Abstraction = new PooledDictionary<string, double>(),
                ArchiveKeys = [],
                InvokeTaskPerformance = new InvokeTaskPerformance(),
                EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid()
            };

            var context = new Context
            {
                EntityAnalysisModel = entityAnalysisModel,
                EntityAnalysisModelInstanceEntryPayload = payload,
                Random = new Random(),
                Log = TestLog.NoOp,
                Stopwatch = Stopwatch.StartNew(),
                LogSampled = true
            };

            var sortedSetKeys = new List<RedisValue>();
            var payloadMap = new Dictionary<string, DictionaryNoBoxing<string>>();

            for (var i = 0; i < history.Count; i++)
            {
                var key = "k" + i;
                sortedSetKeys.Add(key);

                if (absentFromPayloadMap == null || !absentFromPayloadMap.Contains(i))
                {
                    payloadMap.Add(key, history[i]);
                }
            }

            var matches = new ConcurrentDictionary<int, List<DictionaryNoBoxing<string>>>();

            var execute = new Execute
            {
                DistinctSearchKey = new DistinctSearchKey
                {
                    SearchKey = SearchKey,
                    SearchKeyTtlInterval = ttlInterval,
                    SearchKeyTtlIntervalValue = ttlValue
                },
                CachePayloadDocument = current,
                EntityAnalysisModel = entityAnalysisModel,
                EntityAnalysisModelInstanceEntryPayload = payload,
                EntityInstanceEntryDictionaryKvPs = payload.Dictionary,
                AbstractionRuleMatches = matches,
                Log = TestLog.NoOp,
                SortedSetKeys = sortedSetKeys,
                PayloadMap = payloadMap,
                Context = context
            };

            await execute.StartAsync();

            return matches;
        }

        private static double[] Amounts(IEnumerable<DictionaryNoBoxing<string>> documents)
        {
            return [.. documents.Select(d => d["Amount"].AsDouble())];
        }

        [Fact]
        public async Task MatchesAreTheDocumentsTheRuleAcceptsInFetchOrderWithTheCurrentTransactionLastAsync()
        {
            var matches = await RunAsync([Document(1, 5), Document(200, 4), Document(3, 3), Document(400, 2)],
                Document(500, 0), [Rule(1, "a", (data, _, _, _, _, _) => data["Amount"].AsDouble() > 100)]);

            Amounts(matches[1]).Should().Equal(200, 400, 500);
        }

        [Fact]
        public async Task MatchesOutsideTheRuleWindowAreDroppedAfterMatchingAsync()
        {
            var matches = await RunAsync([Document(10, 30), Document(20, 25), Document(30, 23), Document(40, 1)],
                Document(50, 0), [Rule(1, "a", (_, _, _, _, _, _) => true)]);

            Amounts(matches[1]).Should().Equal(30, 40, 50);
        }

        [Fact]
        public async Task TheSearchKeyTtlShortensTheWindowWhenItIsTighterThanTheRuleIntervalAsync()
        {
            var matches = await RunAsync([Document(10, 30), Document(20, 20), Document(30, 5), Document(40, 1)],
                Document(50, 0), [Rule(1, "a", (_, _, _, _, _, _) => true, "d", 7)], "h", 6);

            Amounts(matches[1]).Should().Equal(30, 40, 50);
        }

        [Fact]
        public async Task RulesSharingALogicHashEvaluateEachDocumentOnceAndStillApplyTheirOwnWindowsAsync()
        {
            var evaluations = 0;

            EntityAnalysisModelAbstractionRule.Match match = (data, _, _, _, _, _) =>
            {
                evaluations++;

                return data["Amount"].AsDouble() >= 20;
            };

            var matches = await RunAsync([Document(10, 30), Document(20, 20), Document(30, 5), Document(40, 1)],
                Document(50, 0), [Rule(1, "same", match), Rule(2, "same", match, value: 2)]);

            evaluations.Should().Be(5);
            Amounts(matches[1]).Should().Equal(20, 30, 40, 50);
            Amounts(matches[2]).Should().Equal(40, 50);
        }

        [Fact]
        public async Task RulesForAnotherSearchKeyOrWithoutSearchAreNotEvaluatedAsync()
        {
            var matches = await RunAsync([Document(10, 1)], Document(50, 0),
            [
                Rule(1, "a", (_, _, _, _, _, _) => true, searchKey: "Other"),
                Rule(2, "b", (_, _, _, _, _, _) => true, search: false),
                Rule(3, "c", (_, _, _, _, _, _) => true)
            ]);

            matches.Keys.Should().BeEquivalentTo([3]);
        }

        [Fact]
        public async Task ARuleThatThrowsLeavesNoEntryAndDoesNotStopTheOtherRulesAsync()
        {
            var matches = await RunAsync([Document(10, 1)], Document(50, 0),
            [
                Rule(1, "a", (_, _, _, _, _, _) => throw new InvalidOperationException("boom")),
                Rule(2, "b", (_, _, _, _, _, _) => true)
            ]);

            matches.ContainsKey(1).Should().BeFalse();
            Amounts(matches[2]).Should().Equal(10, 50);
        }

        [Fact]
        public async Task SortedSetKeysWithNoPayloadAreSkippedAsync()
        {
            var matches = await RunAsync([Document(10, 3), Document(20, 2), Document(30, 1)], Document(50, 0),
                [Rule(1, "a", (_, _, _, _, _, _) => true)], absentFromPayloadMap: [1]);

            Amounts(matches[1]).Should().Equal(10, 30, 50);
        }

        [Fact]
        public async Task ARuleWithNoMatchesStillRecordsAnEmptyEntryAsync()
        {
            var matches = await RunAsync([Document(10, 1)], Document(50, 0),
                [Rule(1, "a", (_, _, _, _, _, _) => false)]);

            matches[1].Should().BeEmpty();
        }
    }
}
