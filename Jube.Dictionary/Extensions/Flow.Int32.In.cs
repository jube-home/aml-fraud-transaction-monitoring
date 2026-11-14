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
        extension(Flow<int> flow)
        {
            [Description("True when: one of the listed integers. Decides matched if so; otherwise continues.")]
            public Flow<int> MatchIn([Description("The value list to compare with the object.")] params int[] values)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.Int32In(flow.Value, values));
            }

            [Description("True when: one of the listed integers. Decides rejected if so; otherwise continues.")]
            public Flow<int> RejectIn([Description("The value list to compare with the object.")] params int[] values)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.Int32In(flow.Value, values));
            }

            [Description("True when: one of the listed integers. Decides broken if so; otherwise continues.")]
            public Flow<int> BreakIn([Description("The value list to compare with the object.")] params int[] values)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.Int32In(flow.Value, values));
            }

            [Description("True when: one of the listed integers. Continues if so; otherwise decides rejected.")]
            public Flow<int> RequireIn([Description("The value list to compare with the object.")] params int[] values)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.Int32In(flow.Value, values));
            }

            [Description("True when: one of the listed integers. Continues if so; otherwise decides broken.")]
            public Flow<int> EnsureIn([Description("The value list to compare with the object.")] params int[] values)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.Int32In(flow.Value, values));
            }
        }

        extension(int value)
        {
            [Description("Begins a pipeline from the value. True when: one of the listed integers. Decides matched if so; otherwise continues.")]
            public Flow<int> MatchIn([Description("The value list to compare with the object.")] params int[] values)
            {
                return value.Start().MatchIn(values);
            }

            [Description("Begins a pipeline from the value. True when: one of the listed integers. Decides rejected if so; otherwise continues.")]
            public Flow<int> RejectIn([Description("The value list to compare with the object.")] params int[] values)
            {
                return value.Start().RejectIn(values);
            }

            [Description("Begins a pipeline from the value. True when: one of the listed integers. Decides broken if so; otherwise continues.")]
            public Flow<int> BreakIn([Description("The value list to compare with the object.")] params int[] values)
            {
                return value.Start().BreakIn(values);
            }

            [Description("Begins a pipeline from the value. True when: one of the listed integers. Continues if so; otherwise decides rejected.")]
            public Flow<int> RequireIn([Description("The value list to compare with the object.")] params int[] values)
            {
                return value.Start().RequireIn(values);
            }

            [Description("Begins a pipeline from the value. True when: one of the listed integers. Continues if so; otherwise decides broken.")]
            public Flow<int> EnsureIn([Description("The value list to compare with the object.")] params int[] values)
            {
                return value.Start().EnsureIn(values);
            }
        }
    }
}
