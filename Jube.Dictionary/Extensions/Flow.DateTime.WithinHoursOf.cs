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
            [Description("True when: within the hours of the argument, in either direction. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchWithinHoursOf([Description("The other date.")] DateTime other,
                [Description("The number of hours either side.")] double hours)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeWithinHoursOf(flow.Value, other, hours));
            }

            [Description("True when: within the hours of the argument, in either direction. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectWithinHoursOf([Description("The other date.")] DateTime other,
                [Description("The number of hours either side.")] double hours)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeWithinHoursOf(flow.Value, other, hours));
            }

            [Description("True when: within the hours of the argument, in either direction. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakWithinHoursOf([Description("The other date.")] DateTime other,
                [Description("The number of hours either side.")] double hours)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeWithinHoursOf(flow.Value, other, hours));
            }

            [Description("True when: within the hours of the argument, in either direction. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireWithinHoursOf([Description("The other date.")] DateTime other,
                [Description("The number of hours either side.")] double hours)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeWithinHoursOf(flow.Value, other, hours));
            }

            [Description("True when: within the hours of the argument, in either direction. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureWithinHoursOf([Description("The other date.")] DateTime other,
                [Description("The number of hours either side.")] double hours)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeWithinHoursOf(flow.Value, other, hours));
            }
        }

        extension(DateTime value)
        {
            [Description("Begins a pipeline from the value. True when: within the hours of the argument, in either direction. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchWithinHoursOf([Description("The other date.")] DateTime other,
                [Description("The number of hours either side.")] double hours)
            {
                return value.Start().MatchWithinHoursOf(other, hours);
            }

            [Description("Begins a pipeline from the value. True when: within the hours of the argument, in either direction. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectWithinHoursOf([Description("The other date.")] DateTime other,
                [Description("The number of hours either side.")] double hours)
            {
                return value.Start().RejectWithinHoursOf(other, hours);
            }

            [Description("Begins a pipeline from the value. True when: within the hours of the argument, in either direction. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakWithinHoursOf([Description("The other date.")] DateTime other,
                [Description("The number of hours either side.")] double hours)
            {
                return value.Start().BreakWithinHoursOf(other, hours);
            }

            [Description("Begins a pipeline from the value. True when: within the hours of the argument, in either direction. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireWithinHoursOf([Description("The other date.")] DateTime other,
                [Description("The number of hours either side.")] double hours)
            {
                return value.Start().RequireWithinHoursOf(other, hours);
            }

            [Description("Begins a pipeline from the value. True when: within the hours of the argument, in either direction. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureWithinHoursOf([Description("The other date.")] DateTime other,
                [Description("The number of hours either side.")] double hours)
            {
                return value.Start().EnsureWithinHoursOf(other, hours);
            }
        }
    }
}
