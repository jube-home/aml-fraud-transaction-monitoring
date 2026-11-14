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
            [Description("True when: before 12:00. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchIsMorning()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeIsMorning(flow.Value));
            }

            [Description("True when: before 12:00. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectIsMorning()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeIsMorning(flow.Value));
            }

            [Description("True when: before 12:00. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakIsMorning()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeIsMorning(flow.Value));
            }

            [Description("True when: before 12:00. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireIsMorning()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeIsMorning(flow.Value));
            }

            [Description("True when: before 12:00. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureIsMorning()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeIsMorning(flow.Value));
            }
        }

        extension(DateTime value)
        {
            [Description("Begins a pipeline from the value. True when: before 12:00. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchIsMorning()
            {
                return value.Start().MatchIsMorning();
            }

            [Description("Begins a pipeline from the value. True when: before 12:00. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectIsMorning()
            {
                return value.Start().RejectIsMorning();
            }

            [Description("Begins a pipeline from the value. True when: before 12:00. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakIsMorning()
            {
                return value.Start().BreakIsMorning();
            }

            [Description("Begins a pipeline from the value. True when: before 12:00. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireIsMorning()
            {
                return value.Start().RequireIsMorning();
            }

            [Description("Begins a pipeline from the value. True when: before 12:00. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureIsMorning()
            {
                return value.Start().EnsureIsMorning();
            }
        }
    }
}
