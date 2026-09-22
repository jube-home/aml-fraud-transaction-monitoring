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
            public Flow<DateTime> MatchMoreThanDaysAfter(DateTime other, double days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeMoreThanDaysAfter(flow.Value, other, days));
            }

            public Flow<DateTime> RejectMoreThanDaysAfter(DateTime other, double days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeMoreThanDaysAfter(flow.Value, other, days));
            }

            public Flow<DateTime> BreakMoreThanDaysAfter(DateTime other, double days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeMoreThanDaysAfter(flow.Value, other, days));
            }

            public Flow<DateTime> RequireMoreThanDaysAfter(DateTime other, double days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeMoreThanDaysAfter(flow.Value, other, days));
            }

            public Flow<DateTime> EnsureMoreThanDaysAfter(DateTime other, double days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeMoreThanDaysAfter(flow.Value, other, days));
            }
        }

        extension(DateTime value)
        {
            public Flow<DateTime> MatchMoreThanDaysAfter(DateTime other, double days)
            {
                return value.Start().MatchMoreThanDaysAfter(other, days);
            }

            public Flow<DateTime> RejectMoreThanDaysAfter(DateTime other, double days)
            {
                return value.Start().RejectMoreThanDaysAfter(other, days);
            }

            public Flow<DateTime> BreakMoreThanDaysAfter(DateTime other, double days)
            {
                return value.Start().BreakMoreThanDaysAfter(other, days);
            }

            public Flow<DateTime> RequireMoreThanDaysAfter(DateTime other, double days)
            {
                return value.Start().RequireMoreThanDaysAfter(other, days);
            }

            public Flow<DateTime> EnsureMoreThanDaysAfter(DateTime other, double days)
            {
                return value.Start().EnsureMoreThanDaysAfter(other, days);
            }
        }
    }
}
