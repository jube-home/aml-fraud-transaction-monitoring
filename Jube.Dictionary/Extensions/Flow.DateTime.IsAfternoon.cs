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
            [Description("True when: from 12:00. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchIsAfternoon()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeIsAfternoon(flow.Value));
            }

            [Description("True when: from 12:00. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectIsAfternoon()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeIsAfternoon(flow.Value));
            }

            [Description("True when: from 12:00. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakIsAfternoon()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeIsAfternoon(flow.Value));
            }

            [Description("True when: from 12:00. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireIsAfternoon()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeIsAfternoon(flow.Value));
            }

            [Description("True when: from 12:00. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureIsAfternoon()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeIsAfternoon(flow.Value));
            }
        }

        extension(DateTime value)
        {
            [Description("Begins a pipeline from the value. True when: from 12:00. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchIsAfternoon()
            {
                return value.Start().MatchIsAfternoon();
            }

            [Description("Begins a pipeline from the value. True when: from 12:00. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectIsAfternoon()
            {
                return value.Start().RejectIsAfternoon();
            }

            [Description("Begins a pipeline from the value. True when: from 12:00. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakIsAfternoon()
            {
                return value.Start().BreakIsAfternoon();
            }

            [Description("Begins a pipeline from the value. True when: from 12:00. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireIsAfternoon()
            {
                return value.Start().RequireIsAfternoon();
            }

            [Description("Begins a pipeline from the value. True when: from 12:00. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureIsAfternoon()
            {
                return value.Start().EnsureIsAfternoon();
            }
        }
    }
}
