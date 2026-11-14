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
            [Description("True when: below the minimum or above the maximum. Decides matched if so; otherwise continues.")]
            public Flow<int> MatchOutsideRange([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.Int32OutsideRange(flow.Value, minimum, maximum));
            }

            [Description("True when: below the minimum or above the maximum. Decides rejected if so; otherwise continues.")]
            public Flow<int> RejectOutsideRange([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.Int32OutsideRange(flow.Value, minimum, maximum));
            }

            [Description("True when: below the minimum or above the maximum. Decides broken if so; otherwise continues.")]
            public Flow<int> BreakOutsideRange([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.Int32OutsideRange(flow.Value, minimum, maximum));
            }

            [Description("True when: below the minimum or above the maximum. Continues if so; otherwise decides rejected.")]
            public Flow<int> RequireOutsideRange([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.Int32OutsideRange(flow.Value, minimum, maximum));
            }

            [Description("True when: below the minimum or above the maximum. Continues if so; otherwise decides broken.")]
            public Flow<int> EnsureOutsideRange([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.Int32OutsideRange(flow.Value, minimum, maximum));
            }
        }

        extension(int value)
        {
            [Description("Begins a pipeline from the value. True when: below the minimum or above the maximum. Decides matched if so; otherwise continues.")]
            public Flow<int> MatchOutsideRange([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return value.Start().MatchOutsideRange(minimum, maximum);
            }

            [Description("Begins a pipeline from the value. True when: below the minimum or above the maximum. Decides rejected if so; otherwise continues.")]
            public Flow<int> RejectOutsideRange([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return value.Start().RejectOutsideRange(minimum, maximum);
            }

            [Description("Begins a pipeline from the value. True when: below the minimum or above the maximum. Decides broken if so; otherwise continues.")]
            public Flow<int> BreakOutsideRange([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return value.Start().BreakOutsideRange(minimum, maximum);
            }

            [Description("Begins a pipeline from the value. True when: below the minimum or above the maximum. Continues if so; otherwise decides rejected.")]
            public Flow<int> RequireOutsideRange([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return value.Start().RequireOutsideRange(minimum, maximum);
            }

            [Description("Begins a pipeline from the value. True when: below the minimum or above the maximum. Continues if so; otherwise decides broken.")]
            public Flow<int> EnsureOutsideRange([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return value.Start().EnsureOutsideRange(minimum, maximum);
            }
        }
    }
}
