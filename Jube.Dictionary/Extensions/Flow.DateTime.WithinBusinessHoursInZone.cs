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
            [Description("True when: the value, read as UTC, falls in the hours in that time zone (for example America/New_York). An unknown zone is false. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchWithinBusinessHoursInZone([Description("The IANA or Windows time zone id, for example Europe/London.")] string zoneId,
                [Description("The first hour of the working day, 0 to 23.")] int startHour,
                [Description("The hour the working day ends, 1 to 24, excluded.")] int endHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeWithinBusinessHoursInZone(flow.Value, zoneId, startHour, endHour));
            }

            [Description("True when: the value, read as UTC, falls in the hours in that time zone (for example America/New_York). An unknown zone is false. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectWithinBusinessHoursInZone([Description("The IANA or Windows time zone id, for example Europe/London.")] string zoneId,
                [Description("The first hour of the working day, 0 to 23.")] int startHour,
                [Description("The hour the working day ends, 1 to 24, excluded.")] int endHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeWithinBusinessHoursInZone(flow.Value, zoneId, startHour, endHour));
            }

            [Description("True when: the value, read as UTC, falls in the hours in that time zone (for example America/New_York). An unknown zone is false. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakWithinBusinessHoursInZone([Description("The IANA or Windows time zone id, for example Europe/London.")] string zoneId,
                [Description("The first hour of the working day, 0 to 23.")] int startHour,
                [Description("The hour the working day ends, 1 to 24, excluded.")] int endHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeWithinBusinessHoursInZone(flow.Value, zoneId, startHour, endHour));
            }

            [Description("True when: the value, read as UTC, falls in the hours in that time zone (for example America/New_York). An unknown zone is false. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireWithinBusinessHoursInZone([Description("The IANA or Windows time zone id, for example Europe/London.")] string zoneId,
                [Description("The first hour of the working day, 0 to 23.")] int startHour,
                [Description("The hour the working day ends, 1 to 24, excluded.")] int endHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeWithinBusinessHoursInZone(flow.Value, zoneId, startHour, endHour));
            }

            [Description("True when: the value, read as UTC, falls in the hours in that time zone (for example America/New_York). An unknown zone is false. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureWithinBusinessHoursInZone([Description("The IANA or Windows time zone id, for example Europe/London.")] string zoneId,
                [Description("The first hour of the working day, 0 to 23.")] int startHour,
                [Description("The hour the working day ends, 1 to 24, excluded.")] int endHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeWithinBusinessHoursInZone(flow.Value, zoneId, startHour, endHour));
            }
        }

        extension(DateTime value)
        {
            [Description("Begins a pipeline from the value. True when: the value, read as UTC, falls in the hours in that time zone (for example America/New_York). An unknown zone is false. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchWithinBusinessHoursInZone([Description("The IANA or Windows time zone id, for example Europe/London.")] string zoneId,
                [Description("The first hour of the working day, 0 to 23.")] int startHour,
                [Description("The hour the working day ends, 1 to 24, excluded.")] int endHour)
            {
                return value.Start().MatchWithinBusinessHoursInZone(zoneId, startHour, endHour);
            }

            [Description("Begins a pipeline from the value. True when: the value, read as UTC, falls in the hours in that time zone (for example America/New_York). An unknown zone is false. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectWithinBusinessHoursInZone([Description("The IANA or Windows time zone id, for example Europe/London.")] string zoneId,
                [Description("The first hour of the working day, 0 to 23.")] int startHour,
                [Description("The hour the working day ends, 1 to 24, excluded.")] int endHour)
            {
                return value.Start().RejectWithinBusinessHoursInZone(zoneId, startHour, endHour);
            }

            [Description("Begins a pipeline from the value. True when: the value, read as UTC, falls in the hours in that time zone (for example America/New_York). An unknown zone is false. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakWithinBusinessHoursInZone([Description("The IANA or Windows time zone id, for example Europe/London.")] string zoneId,
                [Description("The first hour of the working day, 0 to 23.")] int startHour,
                [Description("The hour the working day ends, 1 to 24, excluded.")] int endHour)
            {
                return value.Start().BreakWithinBusinessHoursInZone(zoneId, startHour, endHour);
            }

            [Description("Begins a pipeline from the value. True when: the value, read as UTC, falls in the hours in that time zone (for example America/New_York). An unknown zone is false. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireWithinBusinessHoursInZone([Description("The IANA or Windows time zone id, for example Europe/London.")] string zoneId,
                [Description("The first hour of the working day, 0 to 23.")] int startHour,
                [Description("The hour the working day ends, 1 to 24, excluded.")] int endHour)
            {
                return value.Start().RequireWithinBusinessHoursInZone(zoneId, startHour, endHour);
            }

            [Description("Begins a pipeline from the value. True when: the value, read as UTC, falls in the hours in that time zone (for example America/New_York). An unknown zone is false. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureWithinBusinessHoursInZone([Description("The IANA or Windows time zone id, for example Europe/London.")] string zoneId,
                [Description("The first hour of the working day, 0 to 23.")] int startHour,
                [Description("The hour the working day ends, 1 to 24, excluded.")] int endHour)
            {
                return value.Start().EnsureWithinBusinessHoursInZone(zoneId, startHour, endHour);
            }
        }
    }
}
