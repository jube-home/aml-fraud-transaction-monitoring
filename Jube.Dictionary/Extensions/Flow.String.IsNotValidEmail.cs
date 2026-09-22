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
            public Flow<string?> MatchIsNotValidEmail()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringIsNotValidEmail(flow.Value));
            }

            public Flow<string?> RejectIsNotValidEmail()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringIsNotValidEmail(flow.Value));
            }

            public Flow<string?> BreakIsNotValidEmail()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringIsNotValidEmail(flow.Value));
            }

            public Flow<string?> RequireIsNotValidEmail()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringIsNotValidEmail(flow.Value));
            }

            public Flow<string?> EnsureIsNotValidEmail()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringIsNotValidEmail(flow.Value));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchIsNotValidEmail()
            {
                return value.Start().MatchIsNotValidEmail();
            }

            public Flow<string?> RejectIsNotValidEmail()
            {
                return value.Start().RejectIsNotValidEmail();
            }

            public Flow<string?> BreakIsNotValidEmail()
            {
                return value.Start().BreakIsNotValidEmail();
            }

            public Flow<string?> RequireIsNotValidEmail()
            {
                return value.Start().RequireIsNotValidEmail();
            }

            public Flow<string?> EnsureIsNotValidEmail()
            {
                return value.Start().EnsureIsNotValidEmail();
            }
        }
    }
}
