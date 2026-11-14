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
            [Description("True when: strictly between the bounds. Decides matched if so; otherwise continues.")]
            public Flow<int> MatchBetween([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.Int32Between(flow.Value, minimum, maximum));
            }

            [Description("True when: strictly between the bounds. Decides rejected if so; otherwise continues.")]
            public Flow<int> RejectBetween([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.Int32Between(flow.Value, minimum, maximum));
            }

            [Description("True when: strictly between the bounds. Decides broken if so; otherwise continues.")]
            public Flow<int> BreakBetween([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.Int32Between(flow.Value, minimum, maximum));
            }

            [Description("True when: strictly between the bounds. Continues if so; otherwise decides rejected.")]
            public Flow<int> RequireBetween([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.Int32Between(flow.Value, minimum, maximum));
            }

            [Description("True when: strictly between the bounds. Continues if so; otherwise decides broken.")]
            public Flow<int> EnsureBetween([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.Int32Between(flow.Value, minimum, maximum));
            }
        }

        extension(int value)
        {
            [Description("Begins a pipeline from the value. True when: strictly between the bounds. Decides matched if so; otherwise continues.")]
            public Flow<int> MatchBetween([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return value.Start().MatchBetween(minimum, maximum);
            }

            [Description("Begins a pipeline from the value. True when: strictly between the bounds. Decides rejected if so; otherwise continues.")]
            public Flow<int> RejectBetween([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return value.Start().RejectBetween(minimum, maximum);
            }

            [Description("Begins a pipeline from the value. True when: strictly between the bounds. Decides broken if so; otherwise continues.")]
            public Flow<int> BreakBetween([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return value.Start().BreakBetween(minimum, maximum);
            }

            [Description("Begins a pipeline from the value. True when: strictly between the bounds. Continues if so; otherwise decides rejected.")]
            public Flow<int> RequireBetween([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return value.Start().RequireBetween(minimum, maximum);
            }

            [Description("Begins a pipeline from the value. True when: strictly between the bounds. Continues if so; otherwise decides broken.")]
            public Flow<int> EnsureBetween([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return value.Start().EnsureBetween(minimum, maximum);
            }
        }
    }
}
