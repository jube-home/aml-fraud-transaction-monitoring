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
using Accord.Statistics;
using Jube.Data.Extension;
using Jube.Engine.Helpers;
using Jube.Engine.Model;
using Jube.Engine.Model.Processing.Payload;
using log4net;

namespace Jube.Engine.Invoke.Abstraction;

public static class EntityAnalysisModelAbstractionRuleAggregator
{
    public static double Aggregate(EntityAnalysisModelInstanceEntryPayload payload,
        Dictionary<int, List<Dictionary<string, object>>> abstractionRuleMatches,
        EntityAnalysisModelAbstractionRule abstractionRule, ILog log)
    {
        double abstractionValue = 0;
        try
        {
            log.Debug(
                $"Abstraction Aggregation: The aggregator has been called for GUID payload {payload.EntityAnalysisModelInstanceGuid} to aggregate {abstractionRuleMatches.Count} on Abstraction rule {abstractionRule.Id}.");

            if (abstractionRuleMatches.ContainsKey(abstractionRule.Id))
            {
                var skip = 0;
                var fetch = 0;

                if (abstractionRule.EnableOffset)
                    switch (abstractionRule.OffsetType)
                    {
                        case 0: //'No Offset.
                            skip = 0;
                            fetch = abstractionRuleMatches[abstractionRule.Id].Count;
                            break;
                        case 1: //'First
                            skip = abstractionRule.OffsetValue;
                            fetch = 1;
                            break;
                        case 2: //'Last
                            skip = abstractionRuleMatches[abstractionRule.Id].Count -
                                   (1 + abstractionRule.OffsetValue);
                            fetch = 1;
                            break;
                        case 3: //'Skip First
                            skip = abstractionRule.OffsetValue;
                            fetch = abstractionRuleMatches[abstractionRule.Id].Count -
                                    abstractionRule.OffsetValue;
                            break;
                        case 4: //'Skip Last
                            skip = abstractionRuleMatches[abstractionRule.Id].Count -
                                   abstractionRule.OffsetValue;
                            fetch = abstractionRule.OffsetValue;
                            break;
                    }
                else
                    fetch = abstractionRuleMatches[abstractionRule.Id].Count;

                var rangeCacheDocumentsList =
                    abstractionRuleMatches[abstractionRule.Id].Skip(skip).Take(fetch);

                log.Debug(
                    $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id}.");

                switch (abstractionRule.AbstractionRuleAggregationFunctionType)
                {
                    //'Count
                    case 1:
                        log.Debug(
                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} and will count the entries in the collection.");

                        abstractionValue = rangeCacheDocumentsList.Count();

                        log.Debug(
                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} and{abstractionRule.Id} has a count value of {abstractionValue}.");

                        break;
                    //'Distinct Count
                    case 2:
                    {
                        log.Debug(
                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} and will distinct count the entries in the collection.");

                        var distinctList = new List<string>();

                        foreach (var cacheDocumentEntry in rangeCacheDocumentsList)
                        foreach (var (key, value) in cacheDocumentEntry)
                            if (string.Equals(key, abstractionRule.SearchFunctionKey,
                                    StringComparison.CurrentCultureIgnoreCase))
                            {
                                if (!distinctList.Contains(value.ToString()))
                                    distinctList.Add(value.ToString());
                                break;
                            }

                        abstractionValue = distinctList.Count;

                        log.Debug(
                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} and{abstractionRule.Id} has a distinct count value of {abstractionValue}.");

                        break;
                    }
                    //'Same Count
                    case 12:
                    {
                        log.Debug(
                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} and will same count the entries in the collection.");

                        foreach (var cacheDocumentEntry in rangeCacheDocumentsList)
                        foreach (var (key, value) in cacheDocumentEntry
                                     .Where(cacheElement => string.Equals(cacheElement.Key,
                                         abstractionRule.SearchFunctionKey,
                                         StringComparison.CurrentCultureIgnoreCase)))
                        {
                            if (payload.Payload[key].ToString()?.ToUpper() ==
                                value.ToString()?.ToUpper()) abstractionValue += 1;
                            break;
                        }

                        log.Debug(
                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} and{abstractionRule.Id} has a same count value of {abstractionValue}.");

                        break;
                    }
                    default:
                    {
                        var cacheDocumentsList = rangeCacheDocumentsList as Dictionary<string, object>[] ??
                                                 rangeCacheDocumentsList.ToArray();
                        var cacheDocumentAbstractionForRules =
                            rangeCacheDocumentsList as Dictionary<string, object>[] ??
                            cacheDocumentsList.ToArray();
                        switch (abstractionRule.AbstractionRuleAggregationFunctionType)
                        {
                            //Raw
                            case 13:
                                log.Debug(
                                    $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} and will raw value the entries in the collection.");

                                try
                                {
                                    var elementAt =
                                        cacheDocumentAbstractionForRules.ElementAt(
                                            cacheDocumentAbstractionForRules.Length - 1);
                                    if (elementAt.ContainsKey(abstractionRule.SearchFunctionKey))
                                    {
                                        abstractionValue = elementAt[abstractionRule.SearchFunctionKey].AsDouble();

                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} found the key for raw value {abstractionRule.SearchFunctionKey}.");
                                    }
                                    else
                                    {
                                        abstractionValue = 0;

                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} Could not find the key for raw value {abstractionRule.SearchFunctionKey}.");
                                    }
                                }
                                catch (Exception)
                                {
                                    abstractionValue = 0;
                                }

                                log.Debug(
                                    $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} and{abstractionRule.Id} has a raw value of {abstractionValue}.");

                                break;
                            //Since
                            case 16:
                            {
                                log.Debug(
                                    $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} and will Since date the entries in the collection.");

                                DateTime sinceCurrentDate;
                                if (abstractionRuleMatches[abstractionRule.Id][
                                        abstractionRuleMatches[abstractionRule.Id].Count - 1]
                                    .ContainsKey(abstractionRule.SearchFunctionKey))
                                {
                                    sinceCurrentDate =
                                        Convert.ToDateTime(
                                            abstractionRuleMatches[abstractionRule.Id][
                                                abstractionRuleMatches[abstractionRule.Id].Count -
                                                1][
                                                abstractionRule.SearchFunctionKey]);

                                    log.Debug(
                                        $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} is using the {abstractionRule.SearchFunctionKey} for the Current Date Value.");
                                }
                                else
                                {
                                    sinceCurrentDate =
                                        Convert.ToDateTime(
                                            abstractionRuleMatches[abstractionRule.Id][
                                                abstractionRuleMatches[abstractionRule.Id].Count -
                                                1][
                                                "CreatedDate"]);

                                    log.Debug(
                                        $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} is using the CreatedDate for the Current Date Value.");
                                }

                                DateTime sinceTestDate;
                                var elementAt =
                                    cacheDocumentsList.ElementAt(cacheDocumentAbstractionForRules.Length - 1);

                                if (elementAt.TryGetValue(abstractionRule.SearchFunctionKey, out var value))
                                {
                                    sinceTestDate =
                                        Convert.ToDateTime(value);

                                    log.Debug(
                                        $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} is using the {abstractionRule.SearchFunctionKey} for the Test Date Value.");
                                }
                                else
                                {
                                    sinceTestDate = Convert.ToDateTime(elementAt["CreatedDate"]);

                                    log.Debug(
                                        $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} is using the CreatedDate for the Test Date Value.");
                                }

                                switch (abstractionRule.AbstractionRuleAggregationFunctionIntervalType)
                                {
                                    case "s":
                                        //AbstractionValue = SinceCurrentDate.Subtract(SinceTestDate).Seconds
                                        abstractionValue = DateHelper.DateDiff(DateHelper.DateInterval.Second,
                                            sinceTestDate,
                                            sinceCurrentDate);

                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} and will Since date the entries in the collection using seconds and a date from {sinceTestDate} to {sinceCurrentDate}.");

                                        break;
                                    case "h":
                                        //AbstractionValue = SinceCurrentDate.Subtract(SinceTestDate).Hours
                                        DateHelper.DateDiff(DateHelper.DateInterval.Hour, sinceTestDate,
                                            sinceCurrentDate);

                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} and will Since date the entries in the collection using hours and a date from {sinceTestDate} to {sinceCurrentDate}.");

                                        break;
                                    case "m":
                                        //AbstractionValue = SinceCurrentDate.Subtract(SinceTestDate).Minutes
                                        DateHelper.DateDiff(DateHelper.DateInterval.Minute, sinceTestDate,
                                            sinceCurrentDate);

                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} and will Since date the entries in the collection using minutes and a date from {sinceTestDate} to {sinceCurrentDate}.");

                                        break;
                                    case "d":
                                        //AbstractionValue = SinceCurrentDate.Subtract(SinceTestDate).Days
                                        DateHelper.DateDiff(DateHelper.DateInterval.Day, sinceTestDate,
                                            sinceCurrentDate);

                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} and will Since date the entries in the collection using days and a date from {sinceTestDate} to {sinceCurrentDate}.");

                                        break;
                                    default:
                                        abstractionValue = 0;

                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} and will Since date the entries in the collection using nothing \\ null and a date from {sinceTestDate} to {sinceCurrentDate}.");

                                        break;
                                }

                                log.Debug(
                                    $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} and{abstractionRule.Id} has a since date value of {abstractionValue}.");

                                break;
                            }
                            default:
                            {
                                log.Debug(
                                    $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} and will use Extreme stats on an array.");

                                var i = 0;
                                var values = new double[cacheDocumentsList.Length];
                                foreach (var cacheDocumentAbstractionForRule in cacheDocumentAbstractionForRules)
                                {
                                    foreach (var cacheElement in cacheDocumentAbstractionForRule
                                                 .Where(cacheElement => string.Equals(cacheElement.Key,
                                                     abstractionRule.SearchFunctionKey,
                                                     StringComparison.CurrentCultureIgnoreCase)))
                                        try
                                        {
                                            values[i] = cacheElement.Value.AsDouble();
                                            break;
                                        }
                                        catch (Exception ex)
                                        {
                                            log.Info(
                                                $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found error on {abstractionRule.Id} as " +
                                                ex +
                                                "."); //'At Info Level because it has the potential to flood logging.
                                        }

                                    i += 1;
                                }

                                switch (abstractionRule.AbstractionRuleAggregationFunctionType)
                                {
                                    case 3: //'Sum
                                    {
                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} and will Sum the entries in the collection.");

                                        abstractionValue = values.Sum();

                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} and{abstractionRule.Id} has a Sum value of {abstractionValue}.");

                                        break;
                                    }
                                    case 4: //'Avg
                                    {
                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} and will Mean the entries in the collection.");

                                        abstractionValue = values.Mean();

                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} and{abstractionRule.Id} has a Mean value of {abstractionValue}.");

                                        break;
                                    }
                                    case 5: //'Median
                                    {
                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} and will Median the entries in the collection.");

                                        abstractionValue = values.Median();

                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} and{abstractionRule.Id} has a Median value of {abstractionValue}.");

                                        break;
                                    }
                                    case 6: //'Kurtosis
                                    {
                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} and will Kurtosis the entries in the collection.");

                                        abstractionValue = values.Kurtosis();

                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} and{abstractionRule.Id} has a Kurtosis value of {abstractionValue}.");

                                        break;
                                    }
                                    case 7: //'Skew
                                    {
                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} and will Skew the entries in the collection.");

                                        abstractionValue = values.Skewness();

                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} and{abstractionRule.Id} has a Skew value of {abstractionValue}.");
                                        break;
                                    }
                                    case 8: //'SD
                                    {
                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} and will Standard Deviation the entries in the collection.");

                                        abstractionValue = values.StandardDeviation();

                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} and{abstractionRule.Id} has a Standard Deviation value of {abstractionValue}.");
                                        break;
                                    }
                                    case 9: //'SD1
                                    {
                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} and will Standard Deviation 1 the entries in the collection.");

                                        abstractionValue = values.StandardDeviation() + values.Mean();

                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} and{abstractionRule.Id} has a Standard Deviation 1 value of {abstractionValue}.");
                                        break;
                                    }
                                    case 10: //'SD2
                                    {
                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} and will Standard Deviation 2 the entries in the collection.");

                                        var sd = values.StandardDeviation();
                                        abstractionValue = sd * 2 + values.Mean();

                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} and{abstractionRule.Id} has a Standard Deviation 2 value of {abstractionValue}.");
                                        break;
                                    }
                                    case 11: //'Mode
                                    {
                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} and will Mode the entries in the collection.");

                                        abstractionValue = values.Mode();

                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} and{abstractionRule.Id} has a Mode value of {abstractionValue}.");
                                        break;
                                    }
                                    case 12:
                                    {
                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} and will Standard Deviation 2 the entries in the collection.");

                                        var sd = values.StandardDeviation();
                                        abstractionValue = sd * 2 + values.Mean();

                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} and{abstractionRule.Id} has a Standard Deviation 2 value of {abstractionValue}.");
                                        break;
                                    }
                                    case 14:
                                    {
                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} and will Max the entries in the collection.");

                                        abstractionValue = values.Max();

                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} and{abstractionRule.Id} has a Max value of {abstractionValue}.");
                                        break;
                                    }
                                    case 15:
                                    {
                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} and will Min the entries in the collection.");

                                        abstractionValue = values.Min();

                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} and{abstractionRule.Id} has a Min value of {abstractionValue}.");
                                        break;
                                    }
                                }

                                break;
                            }
                        }

                        break;
                    }
                }
            }
            else
            {
                abstractionValue = 0;

                log.Debug(
                    $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found no matches for {abstractionRule.Id} returned zero.");
            }

            if (double.IsNaN(abstractionValue))
            {
                abstractionValue = 0;

                log.Debug(
                    $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} seems to be a NaN.  Swapped to Zero.");
            }
            else if (double.IsInfinity(abstractionValue))
            {
                abstractionValue = 0;

                log.Debug(
                    $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} seems to be Infinity.  Swapped to Zero.");
            }
        }
        catch (Exception ex)
        {
            log.Error(
                $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} for {abstractionRule.Id} is in error as{ex}.");

            abstractionValue = 0;
        }

        return abstractionValue;
    }
}