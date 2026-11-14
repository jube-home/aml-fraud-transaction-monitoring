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
            [Description("True when: within the minutes of the argument, in either direction. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchWithinMinutesOf([Description("The other date.")] DateTime other,
                [Description("The number of minutes either side.")] double minutes)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeWithinMinutesOf(flow.Value, other, minutes));
            }

            [Description("True when: within the minutes of the argument, in either direction. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectWithinMinutesOf([Description("The other date.")] DateTime other,
                [Description("The number of minutes either side.")] double minutes)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeWithinMinutesOf(flow.Value, other, minutes));
            }

            [Description("True when: within the minutes of the argument, in either direction. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakWithinMinutesOf([Description("The other date.")] DateTime other,
                [Description("The number of minutes either side.")] double minutes)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeWithinMinutesOf(flow.Value, other, minutes));
            }

            [Description("True when: within the minutes of the argument, in either direction. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireWithinMinutesOf([Description("The other date.")] DateTime other,
                [Description("The number of minutes either side.")] double minutes)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeWithinMinutesOf(flow.Value, other, minutes));
            }

            [Description("True when: within the minutes of the argument, in either direction. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureWithinMinutesOf([Description("The other date.")] DateTime other,
                [Description("The number of minutes either side.")] double minutes)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeWithinMinutesOf(flow.Value, other, minutes));
            }
        }

        extension(DateTime value)
        {
            [Description("Begins a pipeline from the value. True when: within the minutes of the argument, in either direction. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchWithinMinutesOf([Description("The other date.")] DateTime other,
                [Description("The number of minutes either side.")] double minutes)
            {
                return value.Start().MatchWithinMinutesOf(other, minutes);
            }

            [Description("Begins a pipeline from the value. True when: within the minutes of the argument, in either direction. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectWithinMinutesOf([Description("The other date.")] DateTime other,
                [Description("The number of minutes either side.")] double minutes)
            {
                return value.Start().RejectWithinMinutesOf(other, minutes);
            }

            [Description("Begins a pipeline from the value. True when: within the minutes of the argument, in either direction. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakWithinMinutesOf([Description("The other date.")] DateTime other,
                [Description("The number of minutes either side.")] double minutes)
            {
                return value.Start().BreakWithinMinutesOf(other, minutes);
            }

            [Description("Begins a pipeline from the value. True when: within the minutes of the argument, in either direction. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireWithinMinutesOf([Description("The other date.")] DateTime other,
                [Description("The number of minutes either side.")] double minutes)
            {
                return value.Start().RequireWithinMinutesOf(other, minutes);
            }

            [Description("Begins a pipeline from the value. True when: within the minutes of the argument, in either direction. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureWithinMinutesOf([Description("The other date.")] DateTime other,
                [Description("The number of minutes either side.")] double minutes)
            {
                return value.Start().EnsureWithinMinutesOf(other, minutes);
            }
        }
    }
}
