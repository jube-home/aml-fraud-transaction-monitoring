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
            [Description("True when: has an ascending or descending run of digits at least the minimum length (synthetic identifiers). Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchHasSequentialDigits([Description("The shortest run of consecutive digits that counts.")] int minimumRunLength)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringHasSequentialDigits(flow.Value, minimumRunLength));
            }

            [Description("True when: has an ascending or descending run of digits at least the minimum length (synthetic identifiers). Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectHasSequentialDigits([Description("The shortest run of consecutive digits that counts.")] int minimumRunLength)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringHasSequentialDigits(flow.Value, minimumRunLength));
            }

            [Description("True when: has an ascending or descending run of digits at least the minimum length (synthetic identifiers). Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakHasSequentialDigits([Description("The shortest run of consecutive digits that counts.")] int minimumRunLength)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringHasSequentialDigits(flow.Value, minimumRunLength));
            }

            [Description("True when: has an ascending or descending run of digits at least the minimum length (synthetic identifiers). Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireHasSequentialDigits([Description("The shortest run of consecutive digits that counts.")] int minimumRunLength)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringHasSequentialDigits(flow.Value, minimumRunLength));
            }

            [Description("True when: has an ascending or descending run of digits at least the minimum length (synthetic identifiers). Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureHasSequentialDigits([Description("The shortest run of consecutive digits that counts.")] int minimumRunLength)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringHasSequentialDigits(flow.Value, minimumRunLength));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: has an ascending or descending run of digits at least the minimum length (synthetic identifiers). Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchHasSequentialDigits([Description("The shortest run of consecutive digits that counts.")] int minimumRunLength)
            {
                return value.Start().MatchHasSequentialDigits(minimumRunLength);
            }

            [Description("Begins a pipeline from the value. True when: has an ascending or descending run of digits at least the minimum length (synthetic identifiers). Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectHasSequentialDigits([Description("The shortest run of consecutive digits that counts.")] int minimumRunLength)
            {
                return value.Start().RejectHasSequentialDigits(minimumRunLength);
            }

            [Description("Begins a pipeline from the value. True when: has an ascending or descending run of digits at least the minimum length (synthetic identifiers). Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakHasSequentialDigits([Description("The shortest run of consecutive digits that counts.")] int minimumRunLength)
            {
                return value.Start().BreakHasSequentialDigits(minimumRunLength);
            }

            [Description("Begins a pipeline from the value. True when: has an ascending or descending run of digits at least the minimum length (synthetic identifiers). Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireHasSequentialDigits([Description("The shortest run of consecutive digits that counts.")] int minimumRunLength)
            {
                return value.Start().RequireHasSequentialDigits(minimumRunLength);
            }

            [Description("Begins a pipeline from the value. True when: has an ascending or descending run of digits at least the minimum length (synthetic identifiers). Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureHasSequentialDigits([Description("The shortest run of consecutive digits that counts.")] int minimumRunLength)
            {
                return value.Start().EnsureHasSequentialDigits(minimumRunLength);
            }
        }
    }
}
