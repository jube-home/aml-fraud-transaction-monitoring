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
            public Flow<DateTime> MatchDayOfMonthIn(params int[] days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeDayOfMonthIn(flow.Value, days));
            }

            public Flow<DateTime> RejectDayOfMonthIn(params int[] days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeDayOfMonthIn(flow.Value, days));
            }

            public Flow<DateTime> BreakDayOfMonthIn(params int[] days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeDayOfMonthIn(flow.Value, days));
            }

            public Flow<DateTime> RequireDayOfMonthIn(params int[] days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeDayOfMonthIn(flow.Value, days));
            }

            public Flow<DateTime> EnsureDayOfMonthIn(params int[] days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeDayOfMonthIn(flow.Value, days));
            }
        }

        extension(DateTime value)
        {
            public Flow<DateTime> MatchDayOfMonthIn(params int[] days)
            {
                return value.Start().MatchDayOfMonthIn(days);
            }

            public Flow<DateTime> RejectDayOfMonthIn(params int[] days)
            {
                return value.Start().RejectDayOfMonthIn(days);
            }

            public Flow<DateTime> BreakDayOfMonthIn(params int[] days)
            {
                return value.Start().BreakDayOfMonthIn(days);
            }

            public Flow<DateTime> RequireDayOfMonthIn(params int[] days)
            {
                return value.Start().RequireDayOfMonthIn(days);
            }

            public Flow<DateTime> EnsureDayOfMonthIn(params int[] days)
            {
                return value.Start().EnsureDayOfMonthIn(days);
            }
        }
    }
}