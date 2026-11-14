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
            [Description("True when: earlier than the argument. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchBefore([Description("The other date.")] DateTime other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DateTimeBefore(flow.Value, other));
            }

            [Description("True when: earlier than the argument. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectBefore([Description("The other date.")] DateTime other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DateTimeBefore(flow.Value, other));
            }

            [Description("True when: earlier than the argument. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakBefore([Description("The other date.")] DateTime other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DateTimeBefore(flow.Value, other));
            }

            [Description("True when: earlier than the argument. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireBefore([Description("The other date.")] DateTime other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DateTimeBefore(flow.Value, other));
            }

            [Description("True when: earlier than the argument. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureBefore([Description("The other date.")] DateTime other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DateTimeBefore(flow.Value, other));
            }
        }

        extension(DateTime value)
        {
            [Description("Begins a pipeline from the value. True when: earlier than the argument. Decides matched if so; otherwise continues.")]
            public Flow<DateTime> MatchBefore([Description("The other date.")] DateTime other)
            {
                return value.Start().MatchBefore(other);
            }

            [Description("Begins a pipeline from the value. True when: earlier than the argument. Decides rejected if so; otherwise continues.")]
            public Flow<DateTime> RejectBefore([Description("The other date.")] DateTime other)
            {
                return value.Start().RejectBefore(other);
            }

            [Description("Begins a pipeline from the value. True when: earlier than the argument. Decides broken if so; otherwise continues.")]
            public Flow<DateTime> BreakBefore([Description("The other date.")] DateTime other)
            {
                return value.Start().BreakBefore(other);
            }

            [Description("Begins a pipeline from the value. True when: earlier than the argument. Continues if so; otherwise decides rejected.")]
            public Flow<DateTime> RequireBefore([Description("The other date.")] DateTime other)
            {
                return value.Start().RequireBefore(other);
            }

            [Description("Begins a pipeline from the value. True when: earlier than the argument. Continues if so; otherwise decides broken.")]
            public Flow<DateTime> EnsureBefore([Description("The other date.")] DateTime other)
            {
                return value.Start().EnsureBefore(other);
            }
        }
    }
}
