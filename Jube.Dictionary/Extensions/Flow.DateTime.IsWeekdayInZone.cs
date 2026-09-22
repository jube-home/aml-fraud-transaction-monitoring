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
            public Flow<DateTime> MatchIsWeekdayInZone(string zoneId)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeIsWeekdayInZone(flow.Value, zoneId));
            }

            public Flow<DateTime> RejectIsWeekdayInZone(string zoneId)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeIsWeekdayInZone(flow.Value, zoneId));
            }

            public Flow<DateTime> BreakIsWeekdayInZone(string zoneId)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeIsWeekdayInZone(flow.Value, zoneId));
            }

            public Flow<DateTime> RequireIsWeekdayInZone(string zoneId)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeIsWeekdayInZone(flow.Value, zoneId));
            }

            public Flow<DateTime> EnsureIsWeekdayInZone(string zoneId)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeIsWeekdayInZone(flow.Value, zoneId));
            }
        }

        extension(DateTime value)
        {
            public Flow<DateTime> MatchIsWeekdayInZone(string zoneId)
            {
                return value.Start().MatchIsWeekdayInZone(zoneId);
            }

            public Flow<DateTime> RejectIsWeekdayInZone(string zoneId)
            {
                return value.Start().RejectIsWeekdayInZone(zoneId);
            }

            public Flow<DateTime> BreakIsWeekdayInZone(string zoneId)
            {
                return value.Start().BreakIsWeekdayInZone(zoneId);
            }

            public Flow<DateTime> RequireIsWeekdayInZone(string zoneId)
            {
                return value.Start().RequireIsWeekdayInZone(zoneId);
            }

            public Flow<DateTime> EnsureIsWeekdayInZone(string zoneId)
            {
                return value.Start().EnsureIsWeekdayInZone(zoneId);
            }
        }
    }
}
