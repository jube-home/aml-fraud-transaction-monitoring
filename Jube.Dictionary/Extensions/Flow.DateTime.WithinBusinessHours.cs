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
            [Description("True when: the hour is from the start hour up to, but not including, the end hour. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchWithinBusinessHours([Description("The first hour (0-23) considered within business hours.")] int startHour,
                [Description("The first hour (0-23) no longer considered within business hours.")] int endHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeWithinBusinessHours(flow.Value, startHour, endHour));
            }

            [Description("True when: the hour is from the start hour up to, but not including, the end hour. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectWithinBusinessHours([Description("The first hour (0-23) considered within business hours.")] int startHour,
                [Description("The first hour (0-23) no longer considered within business hours.")] int endHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeWithinBusinessHours(flow.Value, startHour, endHour));
            }

            [Description("True when: the hour is from the start hour up to, but not including, the end hour. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakWithinBusinessHours([Description("The first hour (0-23) considered within business hours.")] int startHour,
                [Description("The first hour (0-23) no longer considered within business hours.")] int endHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeWithinBusinessHours(flow.Value, startHour, endHour));
            }

            [Description("True when: the hour is from the start hour up to, but not including, the end hour. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireWithinBusinessHours([Description("The first hour (0-23) considered within business hours.")] int startHour,
                [Description("The first hour (0-23) no longer considered within business hours.")] int endHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeWithinBusinessHours(flow.Value, startHour, endHour));
            }

            [Description("True when: the hour is from the start hour up to, but not including, the end hour. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureWithinBusinessHours([Description("The first hour (0-23) considered within business hours.")] int startHour,
                [Description("The first hour (0-23) no longer considered within business hours.")] int endHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeWithinBusinessHours(flow.Value, startHour, endHour));
            }
        }

        extension(DateTime value)
        {
            [Description("Begins a pipeline from the value. True when: the hour is from the start hour up to, but not including, the end hour. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchWithinBusinessHours([Description("The first hour (0-23) considered within business hours.")] int startHour,
                [Description("The first hour (0-23) no longer considered within business hours.")] int endHour)
            {
                return value.Start().MatchWithinBusinessHours(startHour, endHour);
            }

            [Description("Begins a pipeline from the value. True when: the hour is from the start hour up to, but not including, the end hour. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectWithinBusinessHours([Description("The first hour (0-23) considered within business hours.")] int startHour,
                [Description("The first hour (0-23) no longer considered within business hours.")] int endHour)
            {
                return value.Start().RejectWithinBusinessHours(startHour, endHour);
            }

            [Description("Begins a pipeline from the value. True when: the hour is from the start hour up to, but not including, the end hour. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakWithinBusinessHours([Description("The first hour (0-23) considered within business hours.")] int startHour,
                [Description("The first hour (0-23) no longer considered within business hours.")] int endHour)
            {
                return value.Start().BreakWithinBusinessHours(startHour, endHour);
            }

            [Description("Begins a pipeline from the value. True when: the hour is from the start hour up to, but not including, the end hour. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireWithinBusinessHours([Description("The first hour (0-23) considered within business hours.")] int startHour,
                [Description("The first hour (0-23) no longer considered within business hours.")] int endHour)
            {
                return value.Start().RequireWithinBusinessHours(startHour, endHour);
            }

            [Description("Begins a pipeline from the value. True when: the hour is from the start hour up to, but not including, the end hour. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureWithinBusinessHours([Description("The first hour (0-23) considered within business hours.")] int startHour,
                [Description("The first hour (0-23) no longer considered within business hours.")] int endHour)
            {
                return value.Start().EnsureWithinBusinessHours(startHour, endHour);
            }
        }
    }
}
