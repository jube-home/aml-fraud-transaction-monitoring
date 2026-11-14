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
            [Description("True when: a weekend in that time zone, or outside the hours there. An unknown zone is false. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchOutsideWeekdayBusinessHoursInZone([Description("The IANA or Windows time zone id, for example Europe/London.")] string zoneId,
                [Description("The first hour of the working day, 0 to 23.")] int startHour,
                [Description("The hour the working day ends, 1 to 24, excluded.")] int endHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeOutsideWeekdayBusinessHoursInZone(flow.Value, zoneId, startHour, endHour));
            }

            [Description("True when: a weekend in that time zone, or outside the hours there. An unknown zone is false. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectOutsideWeekdayBusinessHoursInZone([Description("The IANA or Windows time zone id, for example Europe/London.")] string zoneId,
                [Description("The first hour of the working day, 0 to 23.")] int startHour,
                [Description("The hour the working day ends, 1 to 24, excluded.")] int endHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeOutsideWeekdayBusinessHoursInZone(flow.Value, zoneId, startHour, endHour));
            }

            [Description("True when: a weekend in that time zone, or outside the hours there. An unknown zone is false. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakOutsideWeekdayBusinessHoursInZone([Description("The IANA or Windows time zone id, for example Europe/London.")] string zoneId,
                [Description("The first hour of the working day, 0 to 23.")] int startHour,
                [Description("The hour the working day ends, 1 to 24, excluded.")] int endHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeOutsideWeekdayBusinessHoursInZone(flow.Value, zoneId, startHour, endHour));
            }

            [Description("True when: a weekend in that time zone, or outside the hours there. An unknown zone is false. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireOutsideWeekdayBusinessHoursInZone([Description("The IANA or Windows time zone id, for example Europe/London.")] string zoneId,
                [Description("The first hour of the working day, 0 to 23.")] int startHour,
                [Description("The hour the working day ends, 1 to 24, excluded.")] int endHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeOutsideWeekdayBusinessHoursInZone(flow.Value, zoneId, startHour, endHour));
            }

            [Description("True when: a weekend in that time zone, or outside the hours there. An unknown zone is false. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureOutsideWeekdayBusinessHoursInZone([Description("The IANA or Windows time zone id, for example Europe/London.")] string zoneId,
                [Description("The first hour of the working day, 0 to 23.")] int startHour,
                [Description("The hour the working day ends, 1 to 24, excluded.")] int endHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeOutsideWeekdayBusinessHoursInZone(flow.Value, zoneId, startHour, endHour));
            }
        }

        extension(DateTime value)
        {
            [Description("Begins a pipeline from the value. True when: a weekend in that time zone, or outside the hours there. An unknown zone is false. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchOutsideWeekdayBusinessHoursInZone([Description("The IANA or Windows time zone id, for example Europe/London.")] string zoneId,
                [Description("The first hour of the working day, 0 to 23.")] int startHour,
                [Description("The hour the working day ends, 1 to 24, excluded.")] int endHour)
            {
                return value.Start().MatchOutsideWeekdayBusinessHoursInZone(zoneId, startHour, endHour);
            }

            [Description("Begins a pipeline from the value. True when: a weekend in that time zone, or outside the hours there. An unknown zone is false. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectOutsideWeekdayBusinessHoursInZone([Description("The IANA or Windows time zone id, for example Europe/London.")] string zoneId,
                [Description("The first hour of the working day, 0 to 23.")] int startHour,
                [Description("The hour the working day ends, 1 to 24, excluded.")] int endHour)
            {
                return value.Start().RejectOutsideWeekdayBusinessHoursInZone(zoneId, startHour, endHour);
            }

            [Description("Begins a pipeline from the value. True when: a weekend in that time zone, or outside the hours there. An unknown zone is false. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakOutsideWeekdayBusinessHoursInZone([Description("The IANA or Windows time zone id, for example Europe/London.")] string zoneId,
                [Description("The first hour of the working day, 0 to 23.")] int startHour,
                [Description("The hour the working day ends, 1 to 24, excluded.")] int endHour)
            {
                return value.Start().BreakOutsideWeekdayBusinessHoursInZone(zoneId, startHour, endHour);
            }

            [Description("Begins a pipeline from the value. True when: a weekend in that time zone, or outside the hours there. An unknown zone is false. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireOutsideWeekdayBusinessHoursInZone([Description("The IANA or Windows time zone id, for example Europe/London.")] string zoneId,
                [Description("The first hour of the working day, 0 to 23.")] int startHour,
                [Description("The hour the working day ends, 1 to 24, excluded.")] int endHour)
            {
                return value.Start().RequireOutsideWeekdayBusinessHoursInZone(zoneId, startHour, endHour);
            }

            [Description("Begins a pipeline from the value. True when: a weekend in that time zone, or outside the hours there. An unknown zone is false. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureOutsideWeekdayBusinessHoursInZone([Description("The IANA or Windows time zone id, for example Europe/London.")] string zoneId,
                [Description("The first hour of the working day, 0 to 23.")] int startHour,
                [Description("The hour the working day ends, 1 to 24, excluded.")] int endHour)
            {
                return value.Start().EnsureOutsideWeekdayBusinessHoursInZone(zoneId, startHour, endHour);
            }
        }
    }
}
