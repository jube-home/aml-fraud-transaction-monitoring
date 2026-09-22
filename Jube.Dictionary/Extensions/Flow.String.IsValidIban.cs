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
            public Flow<string?> MatchIsValidIban()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringIsValidIban(flow.Value));
            }

            public Flow<string?> RejectIsValidIban()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringIsValidIban(flow.Value));
            }

            public Flow<string?> BreakIsValidIban()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringIsValidIban(flow.Value));
            }

            public Flow<string?> RequireIsValidIban()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringIsValidIban(flow.Value));
            }

            public Flow<string?> EnsureIsValidIban()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringIsValidIban(flow.Value));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchIsValidIban()
            {
                return value.Start().MatchIsValidIban();
            }

            public Flow<string?> RejectIsValidIban()
            {
                return value.Start().RejectIsValidIban();
            }

            public Flow<string?> BreakIsValidIban()
            {
                return value.Start().BreakIsValidIban();
            }

            public Flow<string?> RequireIsValidIban()
            {
                return value.Start().RequireIsValidIban();
            }

            public Flow<string?> EnsureIsValidIban()
            {
                return value.Start().EnsureIsValidIban();
            }
        }
    }
}
