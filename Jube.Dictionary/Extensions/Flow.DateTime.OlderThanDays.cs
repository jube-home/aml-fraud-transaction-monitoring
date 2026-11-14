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
            [Description("True when: the date is more than the number of days in the past. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchOlderThanDays([Description("The number of days.")] int days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeOlderThanDays(flow.Value, days));
            }

            [Description("True when: the date is more than the number of days in the past. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectOlderThanDays([Description("The number of days.")] int days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeOlderThanDays(flow.Value, days));
            }

            [Description("True when: the date is more than the number of days in the past. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakOlderThanDays([Description("The number of days.")] int days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeOlderThanDays(flow.Value, days));
            }

            [Description("True when: the date is more than the number of days in the past. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireOlderThanDays([Description("The number of days.")] int days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeOlderThanDays(flow.Value, days));
            }

            [Description("True when: the date is more than the number of days in the past. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureOlderThanDays([Description("The number of days.")] int days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeOlderThanDays(flow.Value, days));
            }
        }

        extension(DateTime value)
        {
            [Description("Begins a pipeline from the value. True when: the date is more than the number of days in the past. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchOlderThanDays([Description("The number of days.")] int days)
            {
                return value.Start().MatchOlderThanDays(days);
            }

            [Description("Begins a pipeline from the value. True when: the date is more than the number of days in the past. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectOlderThanDays([Description("The number of days.")] int days)
            {
                return value.Start().RejectOlderThanDays(days);
            }

            [Description("Begins a pipeline from the value. True when: the date is more than the number of days in the past. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakOlderThanDays([Description("The number of days.")] int days)
            {
                return value.Start().BreakOlderThanDays(days);
            }

            [Description("Begins a pipeline from the value. True when: the date is more than the number of days in the past. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireOlderThanDays([Description("The number of days.")] int days)
            {
                return value.Start().RequireOlderThanDays(days);
            }

            [Description("Begins a pipeline from the value. True when: the date is more than the number of days in the past. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureOlderThanDays([Description("The number of days.")] int days)
            {
                return value.Start().EnsureOlderThanDays(days);
            }
        }
    }
}
