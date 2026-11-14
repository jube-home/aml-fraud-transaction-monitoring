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
            [Description("True when: the date is fewer than the number of days in the past. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchYoungerThanDays([Description("The number of days.")] int days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeYoungerThanDays(flow.Value, days));
            }

            [Description("True when: the date is fewer than the number of days in the past. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectYoungerThanDays([Description("The number of days.")] int days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeYoungerThanDays(flow.Value, days));
            }

            [Description("True when: the date is fewer than the number of days in the past. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakYoungerThanDays([Description("The number of days.")] int days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeYoungerThanDays(flow.Value, days));
            }

            [Description("True when: the date is fewer than the number of days in the past. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireYoungerThanDays([Description("The number of days.")] int days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeYoungerThanDays(flow.Value, days));
            }

            [Description("True when: the date is fewer than the number of days in the past. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureYoungerThanDays([Description("The number of days.")] int days)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeYoungerThanDays(flow.Value, days));
            }
        }

        extension(DateTime value)
        {
            [Description("Begins a pipeline from the value. True when: the date is fewer than the number of days in the past. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchYoungerThanDays([Description("The number of days.")] int days)
            {
                return value.Start().MatchYoungerThanDays(days);
            }

            [Description("Begins a pipeline from the value. True when: the date is fewer than the number of days in the past. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectYoungerThanDays([Description("The number of days.")] int days)
            {
                return value.Start().RejectYoungerThanDays(days);
            }

            [Description("Begins a pipeline from the value. True when: the date is fewer than the number of days in the past. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakYoungerThanDays([Description("The number of days.")] int days)
            {
                return value.Start().BreakYoungerThanDays(days);
            }

            [Description("Begins a pipeline from the value. True when: the date is fewer than the number of days in the past. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireYoungerThanDays([Description("The number of days.")] int days)
            {
                return value.Start().RequireYoungerThanDays(days);
            }

            [Description("Begins a pipeline from the value. True when: the date is fewer than the number of days in the past. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureYoungerThanDays([Description("The number of days.")] int days)
            {
                return value.Start().EnsureYoungerThanDays(days);
            }
        }
    }
}
