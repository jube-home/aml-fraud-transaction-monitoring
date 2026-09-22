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
            public Flow<string?> MatchLengthInRange(int minimum, int maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringLengthInRange(flow.Value, minimum, maximum));
            }

            public Flow<string?> RejectLengthInRange(int minimum, int maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringLengthInRange(flow.Value, minimum, maximum));
            }

            public Flow<string?> BreakLengthInRange(int minimum, int maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringLengthInRange(flow.Value, minimum, maximum));
            }

            public Flow<string?> RequireLengthInRange(int minimum, int maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringLengthInRange(flow.Value, minimum, maximum));
            }

            public Flow<string?> EnsureLengthInRange(int minimum, int maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringLengthInRange(flow.Value, minimum, maximum));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchLengthInRange(int minimum, int maximum)
            {
                return value.Start().MatchLengthInRange(minimum, maximum);
            }

            public Flow<string?> RejectLengthInRange(int minimum, int maximum)
            {
                return value.Start().RejectLengthInRange(minimum, maximum);
            }

            public Flow<string?> BreakLengthInRange(int minimum, int maximum)
            {
                return value.Start().BreakLengthInRange(minimum, maximum);
            }

            public Flow<string?> RequireLengthInRange(int minimum, int maximum)
            {
                return value.Start().RequireLengthInRange(minimum, maximum);
            }

            public Flow<string?> EnsureLengthInRange(int minimum, int maximum)
            {
                return value.Start().EnsureLengthInRange(minimum, maximum);
            }
        }
    }
}
