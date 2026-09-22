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
            public Flow<DateTime> MatchMoreThanMinutesAfter(DateTime other, double minutes)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeMoreThanMinutesAfter(flow.Value, other, minutes));
            }

            public Flow<DateTime> RejectMoreThanMinutesAfter(DateTime other, double minutes)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeMoreThanMinutesAfter(flow.Value, other, minutes));
            }

            public Flow<DateTime> BreakMoreThanMinutesAfter(DateTime other, double minutes)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeMoreThanMinutesAfter(flow.Value, other, minutes));
            }

            public Flow<DateTime> RequireMoreThanMinutesAfter(DateTime other, double minutes)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeMoreThanMinutesAfter(flow.Value, other, minutes));
            }

            public Flow<DateTime> EnsureMoreThanMinutesAfter(DateTime other, double minutes)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeMoreThanMinutesAfter(flow.Value, other, minutes));
            }
        }

        extension(DateTime value)
        {
            public Flow<DateTime> MatchMoreThanMinutesAfter(DateTime other, double minutes)
            {
                return value.Start().MatchMoreThanMinutesAfter(other, minutes);
            }

            public Flow<DateTime> RejectMoreThanMinutesAfter(DateTime other, double minutes)
            {
                return value.Start().RejectMoreThanMinutesAfter(other, minutes);
            }

            public Flow<DateTime> BreakMoreThanMinutesAfter(DateTime other, double minutes)
            {
                return value.Start().BreakMoreThanMinutesAfter(other, minutes);
            }

            public Flow<DateTime> RequireMoreThanMinutesAfter(DateTime other, double minutes)
            {
                return value.Start().RequireMoreThanMinutesAfter(other, minutes);
            }

            public Flow<DateTime> EnsureMoreThanMinutesAfter(DateTime other, double minutes)
            {
                return value.Start().EnsureMoreThanMinutesAfter(other, minutes);
            }
        }
    }
}
