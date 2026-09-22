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
            public Flow<DateTime> MatchDayOfWeekIn(params int[] days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeDayOfWeekIn(flow.Value, days));
            }

            public Flow<DateTime> RejectDayOfWeekIn(params int[] days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeDayOfWeekIn(flow.Value, days));
            }

            public Flow<DateTime> BreakDayOfWeekIn(params int[] days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeDayOfWeekIn(flow.Value, days));
            }

            public Flow<DateTime> RequireDayOfWeekIn(params int[] days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeDayOfWeekIn(flow.Value, days));
            }

            public Flow<DateTime> EnsureDayOfWeekIn(params int[] days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeDayOfWeekIn(flow.Value, days));
            }
        }

        extension(DateTime value)
        {
            public Flow<DateTime> MatchDayOfWeekIn(params int[] days)
            {
                return value.Start().MatchDayOfWeekIn(days);
            }

            public Flow<DateTime> RejectDayOfWeekIn(params int[] days)
            {
                return value.Start().RejectDayOfWeekIn(days);
            }

            public Flow<DateTime> BreakDayOfWeekIn(params int[] days)
            {
                return value.Start().BreakDayOfWeekIn(days);
            }

            public Flow<DateTime> RequireDayOfWeekIn(params int[] days)
            {
                return value.Start().RequireDayOfWeekIn(days);
            }

            public Flow<DateTime> EnsureDayOfWeekIn(params int[] days)
            {
                return value.Start().EnsureDayOfWeekIn(days);
            }
        }
    }
}
