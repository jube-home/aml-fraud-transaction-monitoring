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

namespace Jube.Engine.EntityAnalysisModelInvoke.Simulation
{
    using System;
    using System.Collections.Generic;
    using Context.Extensions;
    using Context.Extensions.AbstractionRulesWithSearchKeys;
    using Microsoft.VisualBasic;

    public sealed record TimeWindowResult(
        TimeWindowKind Kind,
        string Interval,
        int Value,
        DateTime From,
        DateTime To,
        TimeSpan Length);

    public static class TimeWindow
    {
        public static IReadOnlyDictionary<TimeWindowKind, string[]> Intervals { get; } =
            new Dictionary<TimeWindowKind, string[]>
            {
                [TimeWindowKind.AbstractionRule] = ["s", "n", "h", "d"],
                [TimeWindowKind.SearchKey] = ["s", "n", "h", "d"],
                [TimeWindowKind.TtlCounter] = ["s", "n", "h", "d", "m", "y"],
                [TimeWindowKind.Calendar] = ["s", "n", "h", "d", "w", "m", "y"]
            };

        public static IReadOnlyDictionary<string, string> IntervalNames { get; } = new Dictionary<string, string>
        {
            ["s"] = "seconds",
            ["n"] = "minutes",
            ["h"] = "hours",
            ["d"] = "days",
            ["w"] = "weeks",
            ["m"] = "months",
            ["y"] = "years"
        };

        public static bool IsAllowed(TimeWindowKind kind, string interval)
        {
            return interval != null && Array.IndexOf(Intervals[kind], interval) >= 0;
        }

        public static TimeWindowResult Calculate(TimeWindowKind kind, string interval, int value,
            DateTime referenceDate)
        {
            if (!IsAllowed(kind, interval))
            {
                throw new ArgumentOutOfRangeException(nameof(interval));
            }

            var from = kind switch
            {
                TimeWindowKind.TtlCounter => TtlCounterExtensions.ApplyTtlCounterInterval(referenceDate, interval,
                    value),
                TimeWindowKind.Calendar => interval == "w"
                    ? referenceDate.AddDays(value * -7d)
                    : TtlCounterExtensions.ApplyTtlCounterInterval(referenceDate, interval, value),
                _ => DateAndTime.DateAdd(interval, value * -1, referenceDate)
            };

            return new TimeWindowResult(kind, interval, value, from, referenceDate, referenceDate - from);
        }

        public static TimeWindowResult AbstractionRule(string ruleInterval, int ruleValue,
            string searchKeyTtlInterval, int searchKeyTtlValue, DateTime referenceDate)
        {
            var from = AbstractionWindow.FromDate(ruleInterval, ruleValue, searchKeyTtlInterval, searchKeyTtlValue,
                referenceDate);
            return new TimeWindowResult(TimeWindowKind.AbstractionRule, ruleInterval, ruleValue, from, referenceDate,
                referenceDate - from);
        }
    }
}