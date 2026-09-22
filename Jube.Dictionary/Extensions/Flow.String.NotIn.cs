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
            public Flow<string?> MatchNotIn(params string[] values)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringNotIn(flow.Value, values));
            }

            public Flow<string?> RejectNotIn(params string[] values)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringNotIn(flow.Value, values));
            }

            public Flow<string?> BreakNotIn(params string[] values)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringNotIn(flow.Value, values));
            }

            public Flow<string?> RequireNotIn(params string[] values)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringNotIn(flow.Value, values));
            }

            public Flow<string?> EnsureNotIn(params string[] values)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringNotIn(flow.Value, values));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchNotIn(params string[] values)
            {
                return value.Start().MatchNotIn(values);
            }

            public Flow<string?> RejectNotIn(params string[] values)
            {
                return value.Start().RejectNotIn(values);
            }

            public Flow<string?> BreakNotIn(params string[] values)
            {
                return value.Start().BreakNotIn(values);
            }

            public Flow<string?> RequireNotIn(params string[] values)
            {
                return value.Start().RequireNotIn(values);
            }

            public Flow<string?> EnsureNotIn(params string[] values)
            {
                return value.Start().EnsureNotIn(values);
            }
        }
    }
}
