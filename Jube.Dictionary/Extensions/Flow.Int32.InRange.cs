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
            [Description("True when: between the bounds, both included. Decides matched if so; otherwise continues.")]
            public Flow<int> MatchInRange([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.Int32InRange(flow.Value, minimum, maximum));
            }

            [Description("True when: between the bounds, both included. Decides rejected if so; otherwise continues.")]
            public Flow<int> RejectInRange([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.Int32InRange(flow.Value, minimum, maximum));
            }

            [Description("True when: between the bounds, both included. Decides broken if so; otherwise continues.")]
            public Flow<int> BreakInRange([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.Int32InRange(flow.Value, minimum, maximum));
            }

            [Description("True when: between the bounds, both included. Continues if so; otherwise decides rejected.")]
            public Flow<int> RequireInRange([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.Int32InRange(flow.Value, minimum, maximum));
            }

            [Description("True when: between the bounds, both included. Continues if so; otherwise decides broken.")]
            public Flow<int> EnsureInRange([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.Int32InRange(flow.Value, minimum, maximum));
            }
        }

        extension(int value)
        {
            [Description("Begins a pipeline from the value. True when: between the bounds, both included. Decides matched if so; otherwise continues.")]
            public Flow<int> MatchInRange([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return value.Start().MatchInRange(minimum, maximum);
            }

            [Description("Begins a pipeline from the value. True when: between the bounds, both included. Decides rejected if so; otherwise continues.")]
            public Flow<int> RejectInRange([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return value.Start().RejectInRange(minimum, maximum);
            }

            [Description("Begins a pipeline from the value. True when: between the bounds, both included. Decides broken if so; otherwise continues.")]
            public Flow<int> BreakInRange([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return value.Start().BreakInRange(minimum, maximum);
            }

            [Description("Begins a pipeline from the value. True when: between the bounds, both included. Continues if so; otherwise decides rejected.")]
            public Flow<int> RequireInRange([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return value.Start().RequireInRange(minimum, maximum);
            }

            [Description("Begins a pipeline from the value. True when: between the bounds, both included. Continues if so; otherwise decides broken.")]
            public Flow<int> EnsureInRange([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return value.Start().EnsureInRange(minimum, maximum);
            }
        }
    }
}
