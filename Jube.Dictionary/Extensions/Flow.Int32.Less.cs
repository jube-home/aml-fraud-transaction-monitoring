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
            [Description("True when: less than the argument. Decides matched if so; otherwise continues.")]
            public Flow<int> MatchLess([Description("The other integer.")] int other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.Int32Less(flow.Value, other));
            }

            [Description("True when: less than the argument. Decides rejected if so; otherwise continues.")]
            public Flow<int> RejectLess([Description("The other integer.")] int other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.Int32Less(flow.Value, other));
            }

            [Description("True when: less than the argument. Decides broken if so; otherwise continues.")]
            public Flow<int> BreakLess([Description("The other integer.")] int other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.Int32Less(flow.Value, other));
            }

            [Description("True when: less than the argument. Continues if so; otherwise decides rejected.")]
            public Flow<int> RequireLess([Description("The other integer.")] int other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.Int32Less(flow.Value, other));
            }

            [Description("True when: less than the argument. Continues if so; otherwise decides broken.")]
            public Flow<int> EnsureLess([Description("The other integer.")] int other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.Int32Less(flow.Value, other));
            }
        }

        extension(int value)
        {
            [Description("Begins a pipeline from the value. True when: less than the argument. Decides matched if so; otherwise continues.")]
            public Flow<int> MatchLess([Description("The other integer.")] int other)
            {
                return value.Start().MatchLess(other);
            }

            [Description("Begins a pipeline from the value. True when: less than the argument. Decides rejected if so; otherwise continues.")]
            public Flow<int> RejectLess([Description("The other integer.")] int other)
            {
                return value.Start().RejectLess(other);
            }

            [Description("Begins a pipeline from the value. True when: less than the argument. Decides broken if so; otherwise continues.")]
            public Flow<int> BreakLess([Description("The other integer.")] int other)
            {
                return value.Start().BreakLess(other);
            }

            [Description("Begins a pipeline from the value. True when: less than the argument. Continues if so; otherwise decides rejected.")]
            public Flow<int> RequireLess([Description("The other integer.")] int other)
            {
                return value.Start().RequireLess(other);
            }

            [Description("Begins a pipeline from the value. True when: less than the argument. Continues if so; otherwise decides broken.")]
            public Flow<int> EnsureLess([Description("The other integer.")] int other)
            {
                return value.Start().EnsureLess(other);
            }
        }
    }
}
