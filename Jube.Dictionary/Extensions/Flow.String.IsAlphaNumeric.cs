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
            public Flow<string?> MatchIsAlphaNumeric()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringIsAlphaNumeric(flow.Value));
            }

            public Flow<string?> RejectIsAlphaNumeric()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringIsAlphaNumeric(flow.Value));
            }

            public Flow<string?> BreakIsAlphaNumeric()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringIsAlphaNumeric(flow.Value));
            }

            public Flow<string?> RequireIsAlphaNumeric()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringIsAlphaNumeric(flow.Value));
            }

            public Flow<string?> EnsureIsAlphaNumeric()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringIsAlphaNumeric(flow.Value));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchIsAlphaNumeric()
            {
                return value.Start().MatchIsAlphaNumeric();
            }

            public Flow<string?> RejectIsAlphaNumeric()
            {
                return value.Start().RejectIsAlphaNumeric();
            }

            public Flow<string?> BreakIsAlphaNumeric()
            {
                return value.Start().BreakIsAlphaNumeric();
            }

            public Flow<string?> RequireIsAlphaNumeric()
            {
                return value.Start().RequireIsAlphaNumeric();
            }

            public Flow<string?> EnsureIsAlphaNumeric()
            {
                return value.Start().EnsureIsAlphaNumeric();
            }
        }
    }
}
