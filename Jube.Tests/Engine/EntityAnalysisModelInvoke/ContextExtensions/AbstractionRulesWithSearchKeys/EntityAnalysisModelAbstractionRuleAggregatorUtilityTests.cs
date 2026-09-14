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
using System.Globalization;
using FluentAssertions;
using Jube.Dictionary;
using Jube.Dictionary.Models;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.AbstractionRulesWithSearchKeys;
using Jube.Engine.EntityAnalysisModelInvoke.Helpers;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using Jube.Test.Infrastructure;
using Xunit;

// ReSharper disable LocalizableElement

namespace Jube.Test.Engine.EntityAnalysisModelInvoke.ContextExtensions.AbstractionRulesWithSearchKeys
{
    [Trait("Category", "Unit")]
    public sealed class EntityAnalysisModelAbstractionRuleAggregatorUtilityTests
    {
        private static EntityAnalysisModelInstanceEntryPayload NewPayload(DictionaryNoBoxing<string>? fields = null)
        {
            return new EntityAnalysisModelInstanceEntryPayload
            {
                Payload = fields ?? new DictionaryNoBoxing<string>(),
                EntityAnalysisModelInstanceGuid = Guid.NewGuid()
            };
        }

        private static EntityAnalysisModelAbstractionRule NewRule(int functionType, string searchFunctionKey = "Amount",
            int id = 1, bool enableOffset = false, int offsetType = 0, int offsetValue = 0, string? intervalType = null)
        {
            return new EntityAnalysisModelAbstractionRule
            {
                Id = id,
                SearchFunctionKey = searchFunctionKey,
                AbstractionRuleAggregationFunctionType = functionType,
                EnableOffset = enableOffset,
                OffsetType = offsetType,
                OffsetValue = offsetValue,
                AbstractionRuleAggregationFunctionIntervalType = intervalType
            };
        }

        private static DictionaryNoBoxing<string> Doc(params (string Key, object Value)[] fields)
        {
            var document = new DictionaryNoBoxing<string>();
            foreach (var (key, value) in fields)
            {
                switch (value)
                {
                    case double d:
                        document.Add(key, d);
                        break;
                    case DateTime dt:
                        document.Add(key, dt);
                        break;
                    case string s:
                        document.Add(key, s);
                        break;
                    case int i:
                        document.Add(key, i);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value), value,
                            "Unsupported test fixture value type.");
                }
            }

            return document;
        }

        private static ConcurrentDictionary<int, List<DictionaryNoBoxing<string>>> MatchesFor(int ruleId,
            params DictionaryNoBoxing<string>[] documents)
        {
            var dictionary = new ConcurrentDictionary<int, List<DictionaryNoBoxing<string>>>
            {
                [ruleId] = [.. documents]
            };
            return dictionary;
        }

        private static ConcurrentDictionary<int, List<DictionaryNoBoxing<string>>> FiveAscendingAmounts(int ruleId)
        {
            return MatchesFor(ruleId,
                Doc(("Amount", 10d)),
                Doc(("Amount", 20d)),
                Doc(("Amount", 30d)),
                Doc(("Amount", 40d)),
                Doc(("Amount", 50d)));
        }

        [Fact]
        public void AggregateReturnsZeroWhenNoMatchesWereRecordedForTheAbstractionRule()
        {
            var rule = NewRule(1);
            var noMatches = new ConcurrentDictionary<int, List<DictionaryNoBoxing<string>>>();

            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.Aggregate(
                NewPayload(), noMatches, rule, TestLog.NoOp);

            result.Should().Be(0);
        }

        [Fact]
        public void AggregateReturnsZeroWhenTheMatchedListForTheRuleIsPresentButEmpty()
        {
            var rule = NewRule(1);
            var matches = MatchesFor(rule.Id);

            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.Aggregate(
                NewPayload(), matches, rule, TestLog.NoOp);

            result.Should().Be(0);
        }

        [Fact]
        public void AggregateThrowsInsteadOfDegradingToZeroWhenTheAbstractionRuleItselfIsNull()
        {
            var payload = NewPayload();
            var matches = new ConcurrentDictionary<int, List<DictionaryNoBoxing<string>>>();

            Action act = () => EntityAnalysisModelAbstractionRuleAggregatorUtility.Aggregate(
                payload, matches, null!, TestLog.NoOp);

            act.Should().Throw<NullReferenceException>();
        }

        [Theory]
        [InlineData(0, 3)]
        [InlineData(1, 3)]
        [InlineData(2, 3)]
        [InlineData(3, 3)]
        [InlineData(4, 3)]
        public void ResolveSkipAndFetchIgnoresTheOffsetConfigurationEntirelyWhenOffsetIsDisabled(int offsetType,
            int offsetValue)
        {
            var rule = NewRule(1, enableOffset: false, offsetType: offsetType, offsetValue: offsetValue);

            var (skip, fetch) = EntityAnalysisModelAbstractionRuleAggregatorUtility.ResolveSkipAndFetch(rule, 5);

            skip.Should().Be(0);
            fetch.Should().Be(5);
        }

        [Theory]
        [InlineData(0, 0, 0, 5)]
        [InlineData(1, 2, 2, 1)]
        [InlineData(2, 1, 3, 1)]
        [InlineData(3, 2, 2, 3)]
        [InlineData(4, 2, 3, 2)]
        [InlineData(99, 2, 0, 5)]
        public void ResolveSkipAndFetchAppliesTheConfiguredOffsetTypeWhenOffsetIsEnabled(int offsetType,
            int offsetValue, int expectedSkip, int expectedFetch)
        {
            var rule = NewRule(1, enableOffset: true, offsetType: offsetType, offsetValue: offsetValue);

            var (skip, fetch) = EntityAnalysisModelAbstractionRuleAggregatorUtility.ResolveSkipAndFetch(rule, 5);

            skip.Should().Be(expectedSkip);
            fetch.Should().Be(expectedFetch);
        }

        [Fact]
        public void OffsetFirstSelectsTheRecordAtThePositionGivenByTheOffsetValue()
        {
            var rule = NewRule(13, enableOffset: true, offsetType: 1, offsetValue: 2);
            var matches = FiveAscendingAmounts(rule.Id);

            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.Aggregate(
                NewPayload(), matches, rule, TestLog.NoOp);

            result.Should().Be(30d);
        }

        [Fact]
        public void OffsetLastCountsBackwardsFromTheMostRecentRecord()
        {
            var rule = NewRule(13, enableOffset: true, offsetType: 2, offsetValue: 1);
            var matches = FiveAscendingAmounts(rule.Id);

            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.Aggregate(
                NewPayload(), matches, rule, TestLog.NoOp);

            result.Should().Be(40d);
        }

        [Fact]
        public void OffsetSkipFirstSumsEverythingExceptTheOldestRecords()
        {
            var rule = NewRule(3, enableOffset: true, offsetType: 3, offsetValue: 2);
            var matches = FiveAscendingAmounts(rule.Id);

            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.Aggregate(
                NewPayload(), matches, rule, TestLog.NoOp);

            result.Should().Be(120d);
        }

        [Fact]
        public void OffsetTakeLastSumsOnlyTheMostRecentRecords()
        {
            var rule = NewRule(3, enableOffset: true, offsetType: 4, offsetValue: 2);
            var matches = FiveAscendingAmounts(rule.Id);

            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.Aggregate(
                NewPayload(), matches, rule, TestLog.NoOp);

            result.Should().Be(90d);
        }

        [Fact]
        public void OffsetSkipFirstBeyondTheAvailableRecordsYieldsAnEmptyRangeRatherThanThrowing()
        {
            var rule = NewRule(3, enableOffset: true, offsetType: 3, offsetValue: 10);
            var matches = FiveAscendingAmounts(rule.Id);

            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.Aggregate(
                NewPayload(), matches, rule, TestLog.NoOp);

            result.Should().Be(0d);
        }

        [Fact]
        public void OffsetTakeLastBeyondTheAvailableRecordsFallsBackToEverything()
        {
            var rule = NewRule(3, enableOffset: true, offsetType: 4, offsetValue: 10);
            var matches = FiveAscendingAmounts(rule.Id);

            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.Aggregate(
                NewPayload(), matches, rule, TestLog.NoOp);

            result.Should().Be(150d);
        }

        [Fact]
        public void AggregateCountReturnsTheNumberOfDocumentsAfterOffsetReduction()
        {
            var rule = NewRule(1);
            var docs = new List<DictionaryNoBoxing<string>> { Doc(), Doc(), Doc() };

            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.AggregateCount(
                NewPayload(), docs, rule, TestLog.NoOp);

            result.Should().Be(3);
        }

        [Fact]
        public void AggregateCountReturnsZeroForAnEmptyDocumentList()
        {
            var rule = NewRule(1);

            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.AggregateCount(
                NewPayload(), [], rule, TestLog.NoOp);

            result.Should().Be(0);
        }

        [Fact]
        public void AggregateDistinctCountCountsUniqueValuesForTheSearchFunctionKey()
        {
            var rule = NewRule(2, "Country");
            var docs = new List<DictionaryNoBoxing<string>>
            {
                Doc(("Country", "US")), Doc(("Country", "UK")), Doc(("Country", "US"))
            };

            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.AggregateDistinctCount(
                NewPayload(), docs, rule, TestLog.NoOp);

            result.Should().Be(2);
        }

        [Fact]
        public void AggregateDistinctCountMatchesTheSearchFunctionKeyCaseInsensitively()
        {
            var rule = NewRule(2, "country");
            var docs = new List<DictionaryNoBoxing<string>> { Doc(("Country", "US")) };

            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.AggregateDistinctCount(
                NewPayload(), docs, rule, TestLog.NoOp);

            result.Should().Be(1);
        }

        [Fact]
        public void AggregateDistinctCountTreatsValuesDifferingOnlyByCaseAsTheSameDistinctEntry()
        {
            var rule = NewRule(2, "Country");
            var docs = new List<DictionaryNoBoxing<string>> { Doc(("Country", "US")), Doc(("Country", "us")) };

            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.AggregateDistinctCount(
                NewPayload(), docs, rule, TestLog.NoOp);

            result.Should().Be(1);
        }

        [Fact]
        public void AggregateDistinctCountSkipsDocumentsThatDoNotHaveTheSearchFunctionKeyAtAll()
        {
            var rule = NewRule(2, "Country");
            var docs = new List<DictionaryNoBoxing<string>> { Doc(("Other", "US")), Doc(("Country", "UK")) };

            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.AggregateDistinctCount(
                NewPayload(), docs, rule, TestLog.NoOp);

            result.Should().Be(1);
        }

        [Fact]
        public void AggregateSameCountCountsDocumentsMatchingTheCurrentPayloadValue()
        {
            var payload = NewPayload(Doc(("Status", "Active")));
            var rule = NewRule(12, "Status");
            var docs = new List<DictionaryNoBoxing<string>>
            {
                Doc(("Status", "Active")), Doc(("Status", "Active")), Doc(("Status", "Inactive"))
            };

            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.AggregateSameCount(
                payload, docs, rule, TestLog.NoOp);

            result.Should().Be(2);
        }

        [Fact]
        public void AggregateSameCountComparesCaseInsensitively()
        {
            var payload = NewPayload(Doc(("Status", "ACTIVE")));
            var rule = NewRule(12, "Status");
            var docs = new List<DictionaryNoBoxing<string>> { Doc(("Status", "active")) };

            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.AggregateSameCount(
                payload, docs, rule, TestLog.NoOp);

            result.Should().Be(1);
        }

        [Fact]
        public void AggregateSameCountDoesNotCountDocumentsThatLackTheSearchFunctionKey()
        {
            var payload = NewPayload(Doc(("Status", "Active")));
            var rule = NewRule(12, "Status");
            var docs = new List<DictionaryNoBoxing<string>> { Doc(("Other", "Active")) };

            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.AggregateSameCount(
                payload, docs, rule, TestLog.NoOp);

            result.Should().Be(0);
        }

        [Fact]
        public void
            AggregateSameCountTreatsAMissingPayloadFieldAsTheLiteralStringNoneWhichCanAccidentallyMatchHistoricData()
        {
            var payload = NewPayload();
            var rule = NewRule(12, "Status");
            var docs = new List<DictionaryNoBoxing<string>> { Doc(("Status", "None")), Doc(("Status", "Active")) };

            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.AggregateSameCount(
                payload, docs, rule, TestLog.NoOp);

            result.Should().Be(1);
        }

        [Fact]
        public void AggregateRawReturnsTheDoubleValueOfTheSearchFunctionKeyOnTheLastDocument()
        {
            var rule = NewRule(13);
            var docs = new List<DictionaryNoBoxing<string>> { Doc(("Amount", 10d)), Doc(("Amount", 42.5d)) };

            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.AggregateRaw(
                NewPayload(), docs, rule, TestLog.NoOp);

            result.Should().Be(42.5d);
        }

        [Fact]
        public void AggregateRawReturnsZeroWhenTheLastDocumentDoesNotHaveTheSearchFunctionKey()
        {
            var rule = NewRule(13);
            var docs = new List<DictionaryNoBoxing<string>> { Doc(("Other", 10d)) };

            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.AggregateRaw(
                NewPayload(), docs, rule, TestLog.NoOp);

            result.Should().Be(0d);
        }

        [Fact]
        public void AggregateRawReturnsZeroRatherThanThrowingWhenTheDocumentListIsEmpty()
        {
            var rule = NewRule(13);

            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.AggregateRaw(
                NewPayload(), [], rule, TestLog.NoOp);

            result.Should().Be(0d);
        }

        [Fact]
        public void AggregateRawReturnsZeroWhenTheSearchFunctionKeyWasStoredAsAStringRatherThanADouble()
        {
            var rule = NewRule(13);
            var docs = new List<DictionaryNoBoxing<string>> { Doc(("Amount", "42.5")) };

            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.AggregateRaw(
                NewPayload(), docs, rule, TestLog.NoOp);

            result.Should().Be(0d);
        }

        [Fact]
        public void RawGracefullyReturnsZeroWhenAnOffsetLeavesAnEmptyWindow()
        {
            var rule = NewRule(13, enableOffset: true, offsetType: 1, offsetValue: 10);
            var matches = MatchesFor(rule.Id, Doc(("Amount", 10d)), Doc(("Amount", 20d)));
            var log = new TestLog();

            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.Aggregate(
                NewPayload(), matches, rule, log);

            result.Should().Be(0d);
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public void BuildValuesArrayReadsTheSearchFunctionKeyFromEachDocumentInOrder()
        {
            var rule = NewRule(3);
            var docs = new List<DictionaryNoBoxing<string>>
            {
                Doc(("Amount", 10d)), Doc(("Amount", 20d)), Doc(("Amount", 30d))
            };

            var values = EntityAnalysisModelAbstractionRuleAggregatorUtility.BuildValuesArray(
                NewPayload(), docs, rule, TestLog.NoOp);

            values.Should().Equal(10d, 20d, 30d);
        }

        [Fact]
        public void BuildValuesArraySubstitutesZeroRatherThanSkippingWhenADocumentLacksTheSearchFunctionKey()
        {
            var rule = NewRule(3);
            var docs = new List<DictionaryNoBoxing<string>>
            {
                Doc(("Amount", 10d)), Doc(("Other", 99d)), Doc(("Amount", 30d))
            };

            var values = EntityAnalysisModelAbstractionRuleAggregatorUtility.BuildValuesArray(
                NewPayload(), docs, rule, TestLog.NoOp);

            values.Should().Equal(10d, 0d, 30d);
        }

        [Fact]
        public void BuildValuesArrayTreatsAStringTypedFieldAsZero()
        {
            var rule = NewRule(3);
            var docs = new List<DictionaryNoBoxing<string>> { Doc(("Amount", "10.5")) };

            var values = EntityAnalysisModelAbstractionRuleAggregatorUtility.BuildValuesArray(
                NewPayload(), docs, rule, TestLog.NoOp);

            values.Should().Equal(0d);
        }

        [Fact]
        public void BuildValuesArrayIsEmptyWhenTheDocumentListIsEmpty()
        {
            var rule = NewRule(3);

            var values = EntityAnalysisModelAbstractionRuleAggregatorUtility.BuildValuesArray(
                NewPayload(), [], rule, TestLog.NoOp);

            values.Should().BeEmpty();
        }

        [Fact]
        public void ComputeExtremeStatisticSumsTheValues()
        {
            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.ComputeExtremeStatistic(
                3, [1d, 2d, 3d, 4d, 5d]);

            result.Should().Be(15d);
        }

        [Fact]
        public void ComputeExtremeStatisticAveragesTheValues()
        {
            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.ComputeExtremeStatistic(
                4, [1d, 2d, 3d, 4d, 5d]);

            result.Should().Be(3d);
        }

        [Fact]
        public void ComputeExtremeStatisticFindsTheMedian()
        {
            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.ComputeExtremeStatistic(
                5, [1d, 2d, 3d, 4d, 5d]);

            result.Should().Be(3d);
        }

        [Fact]
        public void ComputeExtremeStatisticComputesKurtosis()
        {
            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.ComputeExtremeStatistic(
                6, [1d, 2d, 3d, 4d, 5d]);

            result.Should().BeApproximately(-1.2, 1e-9);
        }

        [Fact]
        public void ComputeExtremeStatisticComputesSkewness()
        {
            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.ComputeExtremeStatistic(
                7, [1d, 2d, 3d, 4d, 5d]);

            result.Should().BeApproximately(0, 1e-9);
        }

        [Fact]
        public void ComputeExtremeStatisticComputesStandardDeviation()
        {
            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.ComputeExtremeStatistic(
                8, [1d, 2d, 3d, 4d, 5d]);

            result.Should().BeApproximately(Math.Sqrt(2.5), 1e-9);
        }

        [Fact]
        public void ComputeExtremeStatisticAddsStandardDeviationToTheMean()
        {
            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.ComputeExtremeStatistic(
                9, [1d, 2d, 3d, 4d, 5d]);

            result.Should().BeApproximately(Math.Sqrt(2.5) + 3d, 1e-9);
        }

        [Fact]
        public void ComputeExtremeStatisticDoublesStandardDeviationBeforeAddingTheMeanForFunctionType10()
        {
            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.ComputeExtremeStatistic(
                10, [1d, 2d, 3d, 4d, 5d]);

            result.Should().BeApproximately(Math.Sqrt(2.5) * 2 + 3d, 1e-9);
        }

        [Fact]
        public void ComputeExtremeStatisticTreatsFunctionType12IdenticallyToFunctionType10WhenCalledDirectly()
        {
            var values = new[] { 1d, 2d, 3d, 4d, 5d };

            var resultTen = EntityAnalysisModelAbstractionRuleAggregatorUtility.ComputeExtremeStatistic(10, values);
            var resultTwelve = EntityAnalysisModelAbstractionRuleAggregatorUtility.ComputeExtremeStatistic(12, values);

            resultTwelve.Should().Be(resultTen);
        }

        [Fact]
        public void FunctionType12IsInterceptedAsSameCountBeforeReachingTheExtremeStatisticSwitch()
        {
            var rule = NewRule(12);
            var payload = NewPayload();
            var matches = MatchesFor(rule.Id, Doc(("Amount", 5d)));

            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.Aggregate(
                payload, matches, rule, TestLog.NoOp);

            result.Should().Be(0);
        }

        [Fact]
        public void ComputeExtremeStatisticFindsTheMode()
        {
            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.ComputeExtremeStatistic(
                11, [1d, 2d, 2d, 3d, 3d, 3d]);

            result.Should().Be(3d);
        }

        [Fact]
        public void ComputeExtremeStatisticFindsTheMaximum()
        {
            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.ComputeExtremeStatistic(
                14, [3d, 1d, 4d, 1d, 5d, 9d, 2d, 6d]);

            result.Should().Be(9d);
        }

        [Fact]
        public void ComputeExtremeStatisticFindsTheMinimum()
        {
            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.ComputeExtremeStatistic(
                15, [3d, 1d, 4d, 1d, 5d, 9d, 2d, 6d]);

            result.Should().Be(1d);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        [InlineData(17)]
        [InlineData(-1)]
        public void ComputeExtremeStatisticReturnsZeroForAnUnrecognisedFunctionType(int functionType)
        {
            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.ComputeExtremeStatistic(
                functionType, [1d, 2d, 3d]);

            result.Should().Be(0);
        }

        [Fact]
        public void AggregateReturnsZeroWhenTheMeanOfAnEmptyRangeIsNaN()
        {
            var rule = NewRule(4);
            var matches = MatchesFor(rule.Id);

            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.Aggregate(
                NewPayload(), matches, rule, TestLog.NoOp);

            result.Should().Be(0);
        }

        [Fact]
        public void AggregateReturnsZeroWhenStandardDeviationOfASingleValueIsNaN()
        {
            var rule = NewRule(8);
            var matches = MatchesFor(rule.Id, Doc(("Amount", 5d)));

            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.Aggregate(
                NewPayload(), matches, rule, TestLog.NoOp);

            result.Should().Be(0);
        }

        [Fact]
        public void AggregateReturnsZeroWhenModeThrowsForAnEmptyRangeRatherThanPropagating()
        {
            var rule = NewRule(11);
            var matches = MatchesFor(rule.Id);

            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.Aggregate(
                NewPayload(), matches, rule, TestLog.NoOp);

            result.Should().Be(0);
        }

        [Fact]
        public void AggregateReturnsZeroWhenMaxThrowsForAnEmptyRangeRatherThanPropagating()
        {
            var rule = NewRule(14);
            var matches = MatchesFor(rule.Id);

            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.Aggregate(
                NewPayload(), matches, rule, TestLog.NoOp);

            result.Should().Be(0);
        }

        [Fact]
        public void SinceIsZeroWhenNoOffsetIsConfiguredBecauseTheCurrentAndTestDateComeFromTheSameLastRecord()
        {
            var earliest = Doc(("CreatedDate", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
            var latest = Doc(("CreatedDate", new DateTime(2024, 1, 10, 0, 0, 0, DateTimeKind.Utc)));
            var matches = new List<DictionaryNoBoxing<string>> { earliest, latest };
            var rule = NewRule(16, "EventDate", intervalType: "d");

            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.AggregateSince(
                NewPayload(), matches, matches, rule, TestLog.NoOp);

            result.Should().Be(0);
        }

        [Theory]
        [InlineData("s", 172800)]
        [InlineData("h", 48)]
        [InlineData("m", 2880)]
        [InlineData("d", 2)]
        [InlineData("y", 0)]
        [InlineData(null, 0)]
        public void SinceConvertsTheGapBetweenTheCurrentAndTestRecordUsingTheConfiguredIntervalType(
            string intervalType, double expected)
        {
            var testRecord = Doc(("CreatedDate", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
            var currentRecord = Doc(("CreatedDate", new DateTime(2024, 1, 3, 0, 0, 0, DateTimeKind.Utc)));
            var matches = new List<DictionaryNoBoxing<string>> { testRecord, currentRecord };
            var offsetReduced = new List<DictionaryNoBoxing<string>> { testRecord };
            var rule = NewRule(16, "EventDate", intervalType: intervalType);

            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.AggregateSince(
                NewPayload(), matches, offsetReduced, rule, TestLog.NoOp);

            result.Should().Be(expected);
        }

        [Fact]
        public void SinceParsesTheSearchFunctionKeyOnBothRecordsWhenItIsPresentAndParsable()
        {
            var currentRaw = new DateTime(2024, 1, 10, 6, 30, 0, DateTimeKind.Utc);
            var testRaw = new DateTime(2024, 1, 1, 6, 30, 0, DateTimeKind.Utc);

            var testRecord = Doc(("EventDate", testRaw));
            var currentRecord = Doc(("EventDate", currentRaw));
            var matches = new List<DictionaryNoBoxing<string>> { testRecord, currentRecord };
            var offsetReduced = new List<DictionaryNoBoxing<string>> { testRecord };
            var rule = NewRule(16, "EventDate", intervalType: "d");

            DateTime.TryParse(new InternalValue(currentRaw).ToString(), out var expectedCurrent);
            DateTimeOffset.TryParse(new InternalValue(testRaw).ToString(), CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out var expectedTestOffset);
            var expected =
                DateHelper.DateDiff(DateHelper.DateInterval.Day, expectedTestOffset.UtcDateTime, expectedCurrent);

            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.AggregateSince(
                NewPayload(), matches, offsetReduced, rule, TestLog.NoOp);

            result.Should().Be(expected);
        }

        [Fact]
        public void SinceDefaultsToDateTimeMinValueWhenNeitherTheSearchFunctionKeyNorCreatedDateArePresent()
        {
            var testRecord = Doc();
            var currentDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var currentRecord = Doc(("CreatedDate", currentDate));
            var matches = new List<DictionaryNoBoxing<string>> { testRecord, currentRecord };
            var offsetReduced = new List<DictionaryNoBoxing<string>> { testRecord };
            var rule = NewRule(16, "EventDate", intervalType: "d");

            var expected = (int)(currentDate - DateTime.MinValue).TotalDays;

            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.AggregateSince(
                NewPayload(), matches, offsetReduced, rule, TestLog.NoOp);

            result.Should().Be(expected);
        }

        [Fact]
        public void SinceThrowsWhenTheFullMatchesListIsEmptyRatherThanDegradingGracefully()
        {
            var rule = NewRule(16, "EventDate", intervalType: "d");
            var emptyList = new List<DictionaryNoBoxing<string>>();

            Action act = () => EntityAnalysisModelAbstractionRuleAggregatorUtility.AggregateSince(
                NewPayload(), emptyList, emptyList, rule, TestLog.NoOp);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void SinceHasNoLocalGuardSoAnEmptyOffsetWindowIsCaughtOnlyByTheOuterErrorHandler()
        {
            var rule = NewRule(16, enableOffset: true, offsetType: 1, offsetValue: 10,
                searchFunctionKey: "EventDate", intervalType: "d");
            var matches = MatchesFor(rule.Id,
                Doc(("EventDate", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc))),
                Doc(("EventDate", new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc))));
            var log = new TestLog();

            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.Aggregate(
                NewPayload(), matches, rule, log);

            result.Should().Be(0d);
            log.Entries.Should().Contain(e => e.Level == "ERROR");
        }

        [Theory]
        [MemberData(nameof(NonFiniteValues))]
        public void SanitizeNumericResultZeroesOutNonFiniteValues(double input)
        {
            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.SanitizeNumericResult(
                NewPayload(), input, TestLog.NoOp);

            result.Should().Be(0);
        }

        public static IEnumerable<object[]> NonFiniteValues()
        {
            yield return [double.NaN];
            yield return [double.PositiveInfinity];
            yield return [double.NegativeInfinity];
        }

        [Fact]
        public void SanitizeNumericResultLeavesFiniteValuesUnchanged()
        {
            var result = EntityAnalysisModelAbstractionRuleAggregatorUtility.SanitizeNumericResult(
                NewPayload(), 42.5, TestLog.NoOp);

            result.Should().Be(42.5);
        }
    }
}