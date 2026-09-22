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
            public Flow<string?> MatchIsAllSameCharacter()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringIsAllSameCharacter(flow.Value));
            }

            public Flow<string?> RejectIsAllSameCharacter()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringIsAllSameCharacter(flow.Value));
            }

            public Flow<string?> BreakIsAllSameCharacter()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringIsAllSameCharacter(flow.Value));
            }

            public Flow<string?> RequireIsAllSameCharacter()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringIsAllSameCharacter(flow.Value));
            }

            public Flow<string?> EnsureIsAllSameCharacter()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringIsAllSameCharacter(flow.Value));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchIsAllSameCharacter()
            {
                return value.Start().MatchIsAllSameCharacter();
            }

            public Flow<string?> RejectIsAllSameCharacter()
            {
                return value.Start().RejectIsAllSameCharacter();
            }

            public Flow<string?> BreakIsAllSameCharacter()
            {
                return value.Start().BreakIsAllSameCharacter();
            }

            public Flow<string?> RequireIsAllSameCharacter()
            {
                return value.Start().RequireIsAllSameCharacter();
            }

            public Flow<string?> EnsureIsAllSameCharacter()
            {
                return value.Start().EnsureIsAllSameCharacter();
            }
        }
    }
}
