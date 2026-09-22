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
            public Flow<string?> MatchHasSequentialDigits(int minimumRunLength)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringHasSequentialDigits(flow.Value, minimumRunLength));
            }

            public Flow<string?> RejectHasSequentialDigits(int minimumRunLength)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringHasSequentialDigits(flow.Value, minimumRunLength));
            }

            public Flow<string?> BreakHasSequentialDigits(int minimumRunLength)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringHasSequentialDigits(flow.Value, minimumRunLength));
            }

            public Flow<string?> RequireHasSequentialDigits(int minimumRunLength)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringHasSequentialDigits(flow.Value, minimumRunLength));
            }

            public Flow<string?> EnsureHasSequentialDigits(int minimumRunLength)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringHasSequentialDigits(flow.Value, minimumRunLength));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchHasSequentialDigits(int minimumRunLength)
            {
                return value.Start().MatchHasSequentialDigits(minimumRunLength);
            }

            public Flow<string?> RejectHasSequentialDigits(int minimumRunLength)
            {
                return value.Start().RejectHasSequentialDigits(minimumRunLength);
            }

            public Flow<string?> BreakHasSequentialDigits(int minimumRunLength)
            {
                return value.Start().BreakHasSequentialDigits(minimumRunLength);
            }

            public Flow<string?> RequireHasSequentialDigits(int minimumRunLength)
            {
                return value.Start().RequireHasSequentialDigits(minimumRunLength);
            }

            public Flow<string?> EnsureHasSequentialDigits(int minimumRunLength)
            {
                return value.Start().EnsureHasSequentialDigits(minimumRunLength);
            }
        }
    }
}
