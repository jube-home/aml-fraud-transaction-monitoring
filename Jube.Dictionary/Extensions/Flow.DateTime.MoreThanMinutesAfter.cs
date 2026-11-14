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
            [Description("True when: more than the minutes after the argument. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchMoreThanMinutesAfter([Description("The other date.")] DateTime other,
                [Description("The number of minutes.")] double minutes)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeMoreThanMinutesAfter(flow.Value, other, minutes));
            }

            [Description("True when: more than the minutes after the argument. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectMoreThanMinutesAfter([Description("The other date.")] DateTime other,
                [Description("The number of minutes.")] double minutes)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeMoreThanMinutesAfter(flow.Value, other, minutes));
            }

            [Description("True when: more than the minutes after the argument. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakMoreThanMinutesAfter([Description("The other date.")] DateTime other,
                [Description("The number of minutes.")] double minutes)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeMoreThanMinutesAfter(flow.Value, other, minutes));
            }

            [Description("True when: more than the minutes after the argument. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireMoreThanMinutesAfter([Description("The other date.")] DateTime other,
                [Description("The number of minutes.")] double minutes)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeMoreThanMinutesAfter(flow.Value, other, minutes));
            }

            [Description("True when: more than the minutes after the argument. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureMoreThanMinutesAfter([Description("The other date.")] DateTime other,
                [Description("The number of minutes.")] double minutes)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeMoreThanMinutesAfter(flow.Value, other, minutes));
            }
        }

        extension(DateTime value)
        {
            [Description("Begins a pipeline from the value. True when: more than the minutes after the argument. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchMoreThanMinutesAfter([Description("The other date.")] DateTime other,
                [Description("The number of minutes.")] double minutes)
            {
                return value.Start().MatchMoreThanMinutesAfter(other, minutes);
            }

            [Description("Begins a pipeline from the value. True when: more than the minutes after the argument. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectMoreThanMinutesAfter([Description("The other date.")] DateTime other,
                [Description("The number of minutes.")] double minutes)
            {
                return value.Start().RejectMoreThanMinutesAfter(other, minutes);
            }

            [Description("Begins a pipeline from the value. True when: more than the minutes after the argument. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakMoreThanMinutesAfter([Description("The other date.")] DateTime other,
                [Description("The number of minutes.")] double minutes)
            {
                return value.Start().BreakMoreThanMinutesAfter(other, minutes);
            }

            [Description("Begins a pipeline from the value. True when: more than the minutes after the argument. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireMoreThanMinutesAfter([Description("The other date.")] DateTime other,
                [Description("The number of minutes.")] double minutes)
            {
                return value.Start().RequireMoreThanMinutesAfter(other, minutes);
            }

            [Description("Begins a pipeline from the value. True when: more than the minutes after the argument. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureMoreThanMinutesAfter([Description("The other date.")] DateTime other,
                [Description("The number of minutes.")] double minutes)
            {
                return value.Start().EnsureMoreThanMinutesAfter(other, minutes);
            }
        }
    }
}
