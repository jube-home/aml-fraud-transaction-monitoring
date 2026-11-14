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
            [Description("True when: later than now. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchIsFuture()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeIsFuture(flow.Value));
            }

            [Description("True when: later than now. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectIsFuture()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeIsFuture(flow.Value));
            }

            [Description("True when: later than now. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakIsFuture()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeIsFuture(flow.Value));
            }

            [Description("True when: later than now. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireIsFuture()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeIsFuture(flow.Value));
            }

            [Description("True when: later than now. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureIsFuture()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeIsFuture(flow.Value));
            }
        }

        extension(DateTime value)
        {
            [Description("Begins a pipeline from the value. True when: later than now. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchIsFuture()
            {
                return value.Start().MatchIsFuture();
            }

            [Description("Begins a pipeline from the value. True when: later than now. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectIsFuture()
            {
                return value.Start().RejectIsFuture();
            }

            [Description("Begins a pipeline from the value. True when: later than now. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakIsFuture()
            {
                return value.Start().BreakIsFuture();
            }

            [Description("Begins a pipeline from the value. True when: later than now. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireIsFuture()
            {
                return value.Start().RequireIsFuture();
            }

            [Description("Begins a pipeline from the value. True when: later than now. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureIsFuture()
            {
                return value.Start().EnsureIsFuture();
            }
        }
    }
}
