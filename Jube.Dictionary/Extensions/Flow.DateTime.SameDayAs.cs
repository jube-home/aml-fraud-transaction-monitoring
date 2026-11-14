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
            [Description("True when: on the same calendar date as the argument. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchSameDayAs([Description("The other date.")] DateTime other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeSameDayAs(flow.Value, other));
            }

            [Description("True when: on the same calendar date as the argument. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectSameDayAs([Description("The other date.")] DateTime other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeSameDayAs(flow.Value, other));
            }

            [Description("True when: on the same calendar date as the argument. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakSameDayAs([Description("The other date.")] DateTime other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeSameDayAs(flow.Value, other));
            }

            [Description("True when: on the same calendar date as the argument. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireSameDayAs([Description("The other date.")] DateTime other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeSameDayAs(flow.Value, other));
            }

            [Description("True when: on the same calendar date as the argument. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureSameDayAs([Description("The other date.")] DateTime other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeSameDayAs(flow.Value, other));
            }
        }

        extension(DateTime value)
        {
            [Description("Begins a pipeline from the value. True when: on the same calendar date as the argument. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchSameDayAs([Description("The other date.")] DateTime other)
            {
                return value.Start().MatchSameDayAs(other);
            }

            [Description("Begins a pipeline from the value. True when: on the same calendar date as the argument. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectSameDayAs([Description("The other date.")] DateTime other)
            {
                return value.Start().RejectSameDayAs(other);
            }

            [Description("Begins a pipeline from the value. True when: on the same calendar date as the argument. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakSameDayAs([Description("The other date.")] DateTime other)
            {
                return value.Start().BreakSameDayAs(other);
            }

            [Description("Begins a pipeline from the value. True when: on the same calendar date as the argument. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireSameDayAs([Description("The other date.")] DateTime other)
            {
                return value.Start().RequireSameDayAs(other);
            }

            [Description("Begins a pipeline from the value. True when: on the same calendar date as the argument. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureSameDayAs([Description("The other date.")] DateTime other)
            {
                return value.Start().EnsureSameDayAs(other);
            }
        }
    }
}
