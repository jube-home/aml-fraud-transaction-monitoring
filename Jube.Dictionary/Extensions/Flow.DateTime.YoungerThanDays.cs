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
            public Flow<DateTime> MatchYoungerThanDays(int days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeYoungerThanDays(flow.Value, days));
            }

            public Flow<DateTime> RejectYoungerThanDays(int days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeYoungerThanDays(flow.Value, days));
            }

            public Flow<DateTime> BreakYoungerThanDays(int days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeYoungerThanDays(flow.Value, days));
            }

            public Flow<DateTime> RequireYoungerThanDays(int days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeYoungerThanDays(flow.Value, days));
            }

            public Flow<DateTime> EnsureYoungerThanDays(int days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeYoungerThanDays(flow.Value, days));
            }
        }

        extension(DateTime value)
        {
            public Flow<DateTime> MatchYoungerThanDays(int days)
            {
                return value.Start().MatchYoungerThanDays(days);
            }

            public Flow<DateTime> RejectYoungerThanDays(int days)
            {
                return value.Start().RejectYoungerThanDays(days);
            }

            public Flow<DateTime> BreakYoungerThanDays(int days)
            {
                return value.Start().BreakYoungerThanDays(days);
            }

            public Flow<DateTime> RequireYoungerThanDays(int days)
            {
                return value.Start().RequireYoungerThanDays(days);
            }

            public Flow<DateTime> EnsureYoungerThanDays(int days)
            {
                return value.Start().EnsureYoungerThanDays(days);
            }
        }
    }
}
