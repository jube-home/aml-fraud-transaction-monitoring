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
        extension(Flow<string?> flow)
        {
            [Description("True when: length between the bounds, both included. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchLengthInRange([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringLengthInRange(flow.Value, minimum, maximum));
            }

            [Description("True when: length between the bounds, both included. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectLengthInRange([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringLengthInRange(flow.Value, minimum, maximum));
            }

            [Description("True when: length between the bounds, both included. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakLengthInRange([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringLengthInRange(flow.Value, minimum, maximum));
            }

            [Description("True when: length between the bounds, both included. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireLengthInRange([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringLengthInRange(flow.Value, minimum, maximum));
            }

            [Description("True when: length between the bounds, both included. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureLengthInRange([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringLengthInRange(flow.Value, minimum, maximum));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: length between the bounds, both included. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchLengthInRange([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return value.Start().MatchLengthInRange(minimum, maximum);
            }

            [Description("Begins a pipeline from the value. True when: length between the bounds, both included. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectLengthInRange([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return value.Start().RejectLengthInRange(minimum, maximum);
            }

            [Description("Begins a pipeline from the value. True when: length between the bounds, both included. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakLengthInRange([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return value.Start().BreakLengthInRange(minimum, maximum);
            }

            [Description("Begins a pipeline from the value. True when: length between the bounds, both included. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireLengthInRange([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return value.Start().RequireLengthInRange(minimum, maximum);
            }

            [Description("Begins a pipeline from the value. True when: length between the bounds, both included. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureLengthInRange([Description("The lower bound.")] int minimum,
                [Description("The upper bound.")] int maximum)
            {
                return value.Start().EnsureLengthInRange(minimum, maximum);
            }
        }
    }
}
