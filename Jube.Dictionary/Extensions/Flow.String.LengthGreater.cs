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
            public Flow<string?> MatchLengthGreater(int length)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringLengthGreater(flow.Value, length));
            }

            public Flow<string?> RejectLengthGreater(int length)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringLengthGreater(flow.Value, length));
            }

            public Flow<string?> BreakLengthGreater(int length)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringLengthGreater(flow.Value, length));
            }

            public Flow<string?> RequireLengthGreater(int length)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringLengthGreater(flow.Value, length));
            }

            public Flow<string?> EnsureLengthGreater(int length)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringLengthGreater(flow.Value, length));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchLengthGreater(int length)
            {
                return value.Start().MatchLengthGreater(length);
            }

            public Flow<string?> RejectLengthGreater(int length)
            {
                return value.Start().RejectLengthGreater(length);
            }

            public Flow<string?> BreakLengthGreater(int length)
            {
                return value.Start().BreakLengthGreater(length);
            }

            public Flow<string?> RequireLengthGreater(int length)
            {
                return value.Start().RequireLengthGreater(length);
            }

            public Flow<string?> EnsureLengthGreater(int length)
            {
                return value.Start().EnsureLengthGreater(length);
            }
        }
    }
}
