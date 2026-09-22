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
            public Flow<string?> MatchIsNotValidBic()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringIsNotValidBic(flow.Value));
            }

            public Flow<string?> RejectIsNotValidBic()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringIsNotValidBic(flow.Value));
            }

            public Flow<string?> BreakIsNotValidBic()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringIsNotValidBic(flow.Value));
            }

            public Flow<string?> RequireIsNotValidBic()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringIsNotValidBic(flow.Value));
            }

            public Flow<string?> EnsureIsNotValidBic()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringIsNotValidBic(flow.Value));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchIsNotValidBic()
            {
                return value.Start().MatchIsNotValidBic();
            }

            public Flow<string?> RejectIsNotValidBic()
            {
                return value.Start().RejectIsNotValidBic();
            }

            public Flow<string?> BreakIsNotValidBic()
            {
                return value.Start().BreakIsNotValidBic();
            }

            public Flow<string?> RequireIsNotValidBic()
            {
                return value.Start().RequireIsNotValidBic();
            }

            public Flow<string?> EnsureIsNotValidBic()
            {
                return value.Start().EnsureIsNotValidBic();
            }
        }
    }
}
