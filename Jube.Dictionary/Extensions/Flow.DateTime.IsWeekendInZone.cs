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
            [Description("True when: a weekend in that time zone. An unknown zone is false. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchIsWeekendInZone([Description("The IANA or Windows time zone id, for example Europe/London.")] string zoneId)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeIsWeekendInZone(flow.Value, zoneId));
            }

            [Description("True when: a weekend in that time zone. An unknown zone is false. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectIsWeekendInZone([Description("The IANA or Windows time zone id, for example Europe/London.")] string zoneId)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeIsWeekendInZone(flow.Value, zoneId));
            }

            [Description("True when: a weekend in that time zone. An unknown zone is false. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakIsWeekendInZone([Description("The IANA or Windows time zone id, for example Europe/London.")] string zoneId)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeIsWeekendInZone(flow.Value, zoneId));
            }

            [Description("True when: a weekend in that time zone. An unknown zone is false. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireIsWeekendInZone([Description("The IANA or Windows time zone id, for example Europe/London.")] string zoneId)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeIsWeekendInZone(flow.Value, zoneId));
            }

            [Description("True when: a weekend in that time zone. An unknown zone is false. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureIsWeekendInZone([Description("The IANA or Windows time zone id, for example Europe/London.")] string zoneId)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeIsWeekendInZone(flow.Value, zoneId));
            }
        }

        extension(DateTime value)
        {
            [Description("Begins a pipeline from the value. True when: a weekend in that time zone. An unknown zone is false. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchIsWeekendInZone([Description("The IANA or Windows time zone id, for example Europe/London.")] string zoneId)
            {
                return value.Start().MatchIsWeekendInZone(zoneId);
            }

            [Description("Begins a pipeline from the value. True when: a weekend in that time zone. An unknown zone is false. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectIsWeekendInZone([Description("The IANA or Windows time zone id, for example Europe/London.")] string zoneId)
            {
                return value.Start().RejectIsWeekendInZone(zoneId);
            }

            [Description("Begins a pipeline from the value. True when: a weekend in that time zone. An unknown zone is false. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakIsWeekendInZone([Description("The IANA or Windows time zone id, for example Europe/London.")] string zoneId)
            {
                return value.Start().BreakIsWeekendInZone(zoneId);
            }

            [Description("Begins a pipeline from the value. True when: a weekend in that time zone. An unknown zone is false. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireIsWeekendInZone([Description("The IANA or Windows time zone id, for example Europe/London.")] string zoneId)
            {
                return value.Start().RequireIsWeekendInZone(zoneId);
            }

            [Description("Begins a pipeline from the value. True when: a weekend in that time zone. An unknown zone is false. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureIsWeekendInZone([Description("The IANA or Windows time zone id, for example Europe/London.")] string zoneId)
            {
                return value.Start().EnsureIsWeekendInZone(zoneId);
            }
        }
    }
}
