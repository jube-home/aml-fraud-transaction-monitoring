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
            public Flow<string?> MatchIsNumeric()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringIsNumeric(flow.Value));
            }

            public Flow<string?> RejectIsNumeric()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringIsNumeric(flow.Value));
            }

            public Flow<string?> BreakIsNumeric()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringIsNumeric(flow.Value));
            }

            public Flow<string?> RequireIsNumeric()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringIsNumeric(flow.Value));
            }

            public Flow<string?> EnsureIsNumeric()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringIsNumeric(flow.Value));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchIsNumeric()
            {
                return value.Start().MatchIsNumeric();
            }

            public Flow<string?> RejectIsNumeric()
            {
                return value.Start().RejectIsNumeric();
            }

            public Flow<string?> BreakIsNumeric()
            {
                return value.Start().BreakIsNumeric();
            }

            public Flow<string?> RequireIsNumeric()
            {
                return value.Start().RequireIsNumeric();
            }

            public Flow<string?> EnsureIsNumeric()
            {
                return value.Start().EnsureIsNumeric();
            }
        }
    }
}
