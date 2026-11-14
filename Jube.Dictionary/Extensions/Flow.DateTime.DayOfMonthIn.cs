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

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        extension(Flow<DateTime> flow)
        {
            [Description("True when: day of the month is one of the numbers. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchDayOfMonthIn([Description("The days of the month, 1 to 31.")] params int[] days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeDayOfMonthIn(flow.Value, days));
            }

            [Description("True when: day of the month is one of the numbers. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectDayOfMonthIn([Description("The days of the month, 1 to 31.")] params int[] days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeDayOfMonthIn(flow.Value, days));
            }

            [Description("True when: day of the month is one of the numbers. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakDayOfMonthIn([Description("The days of the month, 1 to 31.")] params int[] days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeDayOfMonthIn(flow.Value, days));
            }

            [Description("True when: day of the month is one of the numbers. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireDayOfMonthIn([Description("The days of the month, 1 to 31.")] params int[] days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeDayOfMonthIn(flow.Value, days));
            }

            [Description("True when: day of the month is one of the numbers. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureDayOfMonthIn([Description("The days of the month, 1 to 31.")] params int[] days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeDayOfMonthIn(flow.Value, days));
            }
        }

        extension(DateTime value)
        {
            [Description("Begins a pipeline from the value. True when: day of the month is one of the numbers. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchDayOfMonthIn([Description("The days of the month, 1 to 31.")] params int[] days)
            {
                return value.Start().MatchDayOfMonthIn(days);
            }

            [Description("Begins a pipeline from the value. True when: day of the month is one of the numbers. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectDayOfMonthIn([Description("The days of the month, 1 to 31.")] params int[] days)
            {
                return value.Start().RejectDayOfMonthIn(days);
            }

            [Description("Begins a pipeline from the value. True when: day of the month is one of the numbers. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakDayOfMonthIn([Description("The days of the month, 1 to 31.")] params int[] days)
            {
                return value.Start().BreakDayOfMonthIn(days);
            }

            [Description("Begins a pipeline from the value. True when: day of the month is one of the numbers. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireDayOfMonthIn([Description("The days of the month, 1 to 31.")] params int[] days)
            {
                return value.Start().RequireDayOfMonthIn(days);
            }

            [Description("Begins a pipeline from the value. True when: day of the month is one of the numbers. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureDayOfMonthIn([Description("The days of the month, 1 to 31.")] params int[] days)
            {
                return value.Start().EnsureDayOfMonthIn(days);
            }
        }
    }
}