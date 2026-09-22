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
            public Flow<string?> MatchIsNotValidIban()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringIsNotValidIban(flow.Value));
            }

            public Flow<string?> RejectIsNotValidIban()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringIsNotValidIban(flow.Value));
            }

            public Flow<string?> BreakIsNotValidIban()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringIsNotValidIban(flow.Value));
            }

            public Flow<string?> RequireIsNotValidIban()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringIsNotValidIban(flow.Value));
            }

            public Flow<string?> EnsureIsNotValidIban()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringIsNotValidIban(flow.Value));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchIsNotValidIban()
            {
                return value.Start().MatchIsNotValidIban();
            }

            public Flow<string?> RejectIsNotValidIban()
            {
                return value.Start().RejectIsNotValidIban();
            }

            public Flow<string?> BreakIsNotValidIban()
            {
                return value.Start().BreakIsNotValidIban();
            }

            public Flow<string?> RequireIsNotValidIban()
            {
                return value.Start().RequireIsNotValidIban();
            }

            public Flow<string?> EnsureIsNotValidIban()
            {
                return value.Start().EnsureIsNotValidIban();
            }
        }
    }
}
