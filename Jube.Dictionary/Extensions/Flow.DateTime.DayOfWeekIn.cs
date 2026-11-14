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
            [Description("True when: day of week is one of the numbers, where 0 is Sunday and 6 is Saturday. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchDayOfWeekIn([Description("The days of the week as numbers, where 0 is Sunday and 6 is Saturday.")] params int[] days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeDayOfWeekIn(flow.Value, days));
            }

            [Description("True when: day of week is one of the numbers, where 0 is Sunday and 6 is Saturday. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectDayOfWeekIn([Description("The days of the week as numbers, where 0 is Sunday and 6 is Saturday.")] params int[] days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeDayOfWeekIn(flow.Value, days));
            }

            [Description("True when: day of week is one of the numbers, where 0 is Sunday and 6 is Saturday. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakDayOfWeekIn([Description("The days of the week as numbers, where 0 is Sunday and 6 is Saturday.")] params int[] days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeDayOfWeekIn(flow.Value, days));
            }

            [Description("True when: day of week is one of the numbers, where 0 is Sunday and 6 is Saturday. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireDayOfWeekIn([Description("The days of the week as numbers, where 0 is Sunday and 6 is Saturday.")] params int[] days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeDayOfWeekIn(flow.Value, days));
            }

            [Description("True when: day of week is one of the numbers, where 0 is Sunday and 6 is Saturday. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureDayOfWeekIn([Description("The days of the week as numbers, where 0 is Sunday and 6 is Saturday.")] params int[] days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeDayOfWeekIn(flow.Value, days));
            }
        }

        extension(DateTime value)
        {
            [Description("Begins a pipeline from the value. True when: day of week is one of the numbers, where 0 is Sunday and 6 is Saturday. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchDayOfWeekIn([Description("The days of the week as numbers, where 0 is Sunday and 6 is Saturday.")] params int[] days)
            {
                return value.Start().MatchDayOfWeekIn(days);
            }

            [Description("Begins a pipeline from the value. True when: day of week is one of the numbers, where 0 is Sunday and 6 is Saturday. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectDayOfWeekIn([Description("The days of the week as numbers, where 0 is Sunday and 6 is Saturday.")] params int[] days)
            {
                return value.Start().RejectDayOfWeekIn(days);
            }

            [Description("Begins a pipeline from the value. True when: day of week is one of the numbers, where 0 is Sunday and 6 is Saturday. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakDayOfWeekIn([Description("The days of the week as numbers, where 0 is Sunday and 6 is Saturday.")] params int[] days)
            {
                return value.Start().BreakDayOfWeekIn(days);
            }

            [Description("Begins a pipeline from the value. True when: day of week is one of the numbers, where 0 is Sunday and 6 is Saturday. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireDayOfWeekIn([Description("The days of the week as numbers, where 0 is Sunday and 6 is Saturday.")] params int[] days)
            {
                return value.Start().RequireDayOfWeekIn(days);
            }

            [Description("Begins a pipeline from the value. True when: day of week is one of the numbers, where 0 is Sunday and 6 is Saturday. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureDayOfWeekIn([Description("The days of the week as numbers, where 0 is Sunday and 6 is Saturday.")] params int[] days)
            {
                return value.Start().EnsureDayOfWeekIn(days);
            }
        }
    }
}
