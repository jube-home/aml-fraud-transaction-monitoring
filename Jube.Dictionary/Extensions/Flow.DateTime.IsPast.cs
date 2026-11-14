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
            [Description("True when: earlier than now. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchIsPast()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeIsPast(flow.Value));
            }

            [Description("True when: earlier than now. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectIsPast()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeIsPast(flow.Value));
            }

            [Description("True when: earlier than now. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakIsPast()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeIsPast(flow.Value));
            }

            [Description("True when: earlier than now. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireIsPast()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeIsPast(flow.Value));
            }

            [Description("True when: earlier than now. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureIsPast()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeIsPast(flow.Value));
            }
        }

        extension(DateTime value)
        {
            [Description("Begins a pipeline from the value. True when: earlier than now. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchIsPast()
            {
                return value.Start().MatchIsPast();
            }

            [Description("Begins a pipeline from the value. True when: earlier than now. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectIsPast()
            {
                return value.Start().RejectIsPast();
            }

            [Description("Begins a pipeline from the value. True when: earlier than now. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakIsPast()
            {
                return value.Start().BreakIsPast();
            }

            [Description("Begins a pipeline from the value. True when: earlier than now. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireIsPast()
            {
                return value.Start().RequireIsPast();
            }

            [Description("Begins a pipeline from the value. True when: earlier than now. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureIsPast()
            {
                return value.Start().EnsureIsPast();
            }
        }
    }
}
