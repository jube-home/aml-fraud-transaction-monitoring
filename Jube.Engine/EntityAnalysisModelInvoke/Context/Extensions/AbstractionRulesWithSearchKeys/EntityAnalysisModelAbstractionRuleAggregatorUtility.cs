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
using System.Linq;
using Accord.Statistics;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Helpers;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using log4net;

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.AbstractionRulesWithSearchKeys
{
    public static class EntityAnalysisModelAbstractionRuleAggregatorUtility
    {
        public static double Aggregate(EntityAnalysisModelInstanceEntryPayload payload,
            ConcurrentDictionary<int, List<DictionaryNoBoxing<string>>> abstractionRuleMatches,
            EntityAnalysisModelAbstractionRule abstractionRule, ILog log)
        {
            double abstractionValue;
            try
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Abstraction Aggregation: The aggregator has been called for GUID payload {payload.EntityAnalysisModelInstanceGuid} to aggregate {abstractionRuleMatches.Count} on Abstraction rule {abstractionRule.Id}.");
                }

                if (abstractionRuleMatches.TryGetValue(abstractionRule.Id, out var matches))
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug(
                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id}.");
                    }

                    abstractionValue = AggregateMatches(payload, matches, abstractionRule, log);
                }
                else
                {
                    abstractionValue = 0;

                    if (log.IsDebugEnabled)
                    {
                        log.Debug(
                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found no matches for {abstractionRule.Id} returned zero.");
                    }
                }

                abstractionValue = SanitizeNumericResult(payload, abstractionValue, log);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.Error(
                    $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} for {abstractionRule.Id} is in error as{ex}.");

                abstractionValue = 0;
            }

            return abstractionValue;
        }

        internal static double AggregateMatches(EntityAnalysisModelInstanceEntryPayload payload,
            List<DictionaryNoBoxing<string>> matches, EntityAnalysisModelAbstractionRule abstractionRule, ILog log)
        {
            var matchesCount = matches.Count;

            var (skip, fetch) = ResolveSkipAndFetch(abstractionRule, matchesCount);

            var rangeCacheDocumentsList = matches.Skip(skip).Take(fetch).ToList();

            switch (abstractionRule.AbstractionRuleAggregationFunctionType)
            {
                case 1:
                    return AggregateCount(payload, rangeCacheDocumentsList, abstractionRule, log);
                case 2:
                    return AggregateDistinctCount(payload, rangeCacheDocumentsList, abstractionRule, log);
                case 12:
                    return AggregateSameCount(payload, rangeCacheDocumentsList, abstractionRule, log);
                default:
                    return AggregateFunction(payload, matches, rangeCacheDocumentsList, abstractionRule, log);
            }
        }

        internal static (int Skip, int Fetch) ResolveSkipAndFetch(EntityAnalysisModelAbstractionRule abstractionRule,
            int matchesCount)
        {
            return abstractionRule.EnableOffset
                ? abstractionRule.OffsetType switch
                {
                    0 => (0, matchesCount),
                    1 => (abstractionRule.OffsetValue, 1),
                    2 => (matchesCount - (1 + abstractionRule.OffsetValue), 1),
                    3 => (abstractionRule.OffsetValue, matchesCount - abstractionRule.OffsetValue),
                    4 => (matchesCount - abstractionRule.OffsetValue, abstractionRule.OffsetValue),
                    _ => (0, matchesCount)
                }
                : (0, matchesCount);
        }

        internal static double AggregateCount(EntityAnalysisModelInstanceEntryPayload payload,
            List<DictionaryNoBoxing<string>> rangeCacheDocumentsList,
            EntityAnalysisModelAbstractionRule abstractionRule,
            ILog log)
        {
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} and will count the entries in the collection.");
            }

            double abstractionValue = rangeCacheDocumentsList.Count;

            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} and{abstractionRule.Id} has a count value of {abstractionValue}.");
            }

            return abstractionValue;
        }

        internal static double AggregateDistinctCount(EntityAnalysisModelInstanceEntryPayload payload,
            List<DictionaryNoBoxing<string>> rangeCacheDocumentsList,
            EntityAnalysisModelAbstractionRule abstractionRule,
            ILog log)
        {
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} and will distinct count the entries in the collection.");
            }

            var distinctSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var cacheDocumentEntry in rangeCacheDocumentsList)
            foreach (var (key, value) in cacheDocumentEntry)
            {
                if (!string.Equals(key, abstractionRule.SearchFunctionKey,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                distinctSet.Add(value.ToString());
                break;
            }

            double abstractionValue = distinctSet.Count;

            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} and{abstractionRule.Id} has a distinct count value of {abstractionValue}.");
            }

            return abstractionValue;
        }

        internal static double AggregateSameCount(EntityAnalysisModelInstanceEntryPayload payload,
            List<DictionaryNoBoxing<string>> rangeCacheDocumentsList,
            EntityAnalysisModelAbstractionRule abstractionRule,
            ILog log)
        {
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} and will same count the entries in the collection.");
            }

            double abstractionValue = 0;

            foreach (var cacheDocumentEntry in rangeCacheDocumentsList)
            foreach (var (key, value) in cacheDocumentEntry)
            {
                if (!string.Equals(key,
                        abstractionRule.SearchFunctionKey,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.Equals(payload.Payload[key].ToString(), value.ToString(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    abstractionValue += 1;
                }

                break;
            }

            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} and{abstractionRule.Id} has a same count value of {abstractionValue}.");
            }

            return abstractionValue;
        }

        internal static double AggregateFunction(EntityAnalysisModelInstanceEntryPayload payload,
            List<DictionaryNoBoxing<string>> matches, List<DictionaryNoBoxing<string>> rangeCacheDocumentsList,
            EntityAnalysisModelAbstractionRule abstractionRule, ILog log)
        {
            var cacheDocumentsList = rangeCacheDocumentsList.ToList();

            switch (abstractionRule.AbstractionRuleAggregationFunctionType)
            {
                case 13: return AggregateRaw(payload, cacheDocumentsList, abstractionRule, log);
                case 16: return AggregateSince(payload, matches, cacheDocumentsList, abstractionRule, log);
                default:
                    return AggregateExtremeStatistic(payload, cacheDocumentsList, abstractionRule, log);
            }
        }

        internal static double AggregateRaw(EntityAnalysisModelInstanceEntryPayload payload,
            List<DictionaryNoBoxing<string>> cacheDocumentsList, EntityAnalysisModelAbstractionRule abstractionRule,
            ILog log)
        {
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} and will raw value the entries in the collection.");
            }

            double abstractionValue;
            try
            {
                var lastElement = cacheDocumentsList[^1];
                if (lastElement.TryGetValue(abstractionRule.SearchFunctionKey, out var value))
                {
                    abstractionValue = value.AsDouble();

                    if (log.IsDebugEnabled)
                    {
                        log.Debug(
                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} found the key for raw value {abstractionRule.SearchFunctionKey}.");
                    }
                }
                else
                {
                    abstractionValue = 0;

                    if (log.IsDebugEnabled)
                    {
                        log.Debug(
                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} Could not find the key for raw value {abstractionRule.SearchFunctionKey}.");
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                abstractionValue = 0;
            }

            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} and{abstractionRule.Id} has a raw value of {abstractionValue}.");
            }

            return abstractionValue;
        }

        internal static double AggregateSince(EntityAnalysisModelInstanceEntryPayload payload,
            List<DictionaryNoBoxing<string>> matches, List<DictionaryNoBoxing<string>> cacheDocumentsList,
            EntityAnalysisModelAbstractionRule abstractionRule, ILog log)
        {
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} and will Since date the entries in the collection.");
            }

            var lastMatch = matches[^1];

            if (lastMatch.TryGetValue(abstractionRule.SearchFunctionKey, out var currentDateValue) &&
                DateTime.TryParse(currentDateValue.ToString(), out var sinceCurrentDate))
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} is using the {abstractionRule.SearchFunctionKey} for the Current Date Value.");
                }
            }
            else
            {
                sinceCurrentDate = lastMatch["CreatedDate"];

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} is using the CreatedDate for the Current Date Value.");
                }
            }

            var lastElement = cacheDocumentsList[^1];

            DateTime sinceTestDate;
            if (lastElement.TryGetValue(abstractionRule.SearchFunctionKey, out var testDateValue) &&
                DateTimeOffset.TryParse(testDateValue.ToString(), CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal, out var dto))
            {
                sinceTestDate = dto.UtcDateTime;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} is using the {abstractionRule.SearchFunctionKey} for the Test Date Value.");
                }
            }
            else
            {
                sinceTestDate = lastElement["CreatedDate"];

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} is using the CreatedDate for the Test Date Value.");
                }
            }

            double abstractionValue = abstractionRule.AbstractionRuleAggregationFunctionIntervalType switch
            {
                "s" => DateHelper.DateDiff(DateHelper.DateInterval.Second, sinceTestDate, sinceCurrentDate),
                "h" => DateHelper.DateDiff(DateHelper.DateInterval.Hour, sinceTestDate, sinceCurrentDate),
                "m" => DateHelper.DateDiff(DateHelper.DateInterval.Minute, sinceTestDate, sinceCurrentDate),
                "d" => DateHelper.DateDiff(DateHelper.DateInterval.Day, sinceTestDate, sinceCurrentDate),
                _ => 0
            };

            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} and{abstractionRule.Id} has a since date value of {abstractionValue}.");
            }

            return abstractionValue;
        }

        internal static double AggregateExtremeStatistic(EntityAnalysisModelInstanceEntryPayload payload,
            List<DictionaryNoBoxing<string>> cacheDocumentsList, EntityAnalysisModelAbstractionRule abstractionRule,
            ILog log)
        {
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} and will use Extreme stats on an array.");
            }

            var values = BuildValuesArray(payload, cacheDocumentsList, abstractionRule, log);

            return ComputeExtremeStatistic(abstractionRule.AbstractionRuleAggregationFunctionType, values);
        }

        internal static double[] BuildValuesArray(EntityAnalysisModelInstanceEntryPayload payload,
            List<DictionaryNoBoxing<string>> cacheDocumentsList, EntityAnalysisModelAbstractionRule abstractionRule,
            ILog log)
        {
            var values = new double[cacheDocumentsList.Count];
            for (var i = 0; i < cacheDocumentsList.Count; i++)
            {
                var document = cacheDocumentsList[i];
                if (!document.TryGetValue(abstractionRule.SearchFunctionKey, out var value))
                {
                    continue;
                }

                try
                {
                    values[i] = value.AsDouble();
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found error on {abstractionRule.Id} as {ex}.");
                    }
                }
            }

            return values;
        }

        internal static double ComputeExtremeStatistic(int abstractionRuleAggregationFunctionType, double[] values)
        {
            return abstractionRuleAggregationFunctionType switch
            {
                3 => values.Sum(),
                4 => values.Mean(),
                5 => values.Median(),
                6 => values.Kurtosis(),
                7 => values.Skewness(),
                8 => values.StandardDeviation(),
                9 => values.StandardDeviation() + values.Mean(),
                10 or 12 => values.StandardDeviation() * 2 + values.Mean(),
                11 => values.Mode(),
                14 => values.Max(),
                15 => values.Min(),
                _ => 0
            };
        }

        internal static double SanitizeNumericResult(EntityAnalysisModelInstanceEntryPayload payload,
            double abstractionValue, ILog log)
        {
            if (double.IsNaN(abstractionValue) || double.IsInfinity(abstractionValue))
            {
                abstractionValue = 0;

                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} seems to be a NaN or Infinity. Swapped to Zero.");
                }
            }

            return abstractionValue;
        }
    }
}