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
            public Flow<string?> MatchContainsNone(params string[] values)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringContainsNone(flow.Value, values));
            }

            public Flow<string?> RejectContainsNone(params string[] values)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringContainsNone(flow.Value, values));
            }

            public Flow<string?> BreakContainsNone(params string[] values)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringContainsNone(flow.Value, values));
            }

            public Flow<string?> RequireContainsNone(params string[] values)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringContainsNone(flow.Value, values));
            }

            public Flow<string?> EnsureContainsNone(params string[] values)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringContainsNone(flow.Value, values));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchContainsNone(params string[] values)
            {
                return value.Start().MatchContainsNone(values);
            }

            public Flow<string?> RejectContainsNone(params string[] values)
            {
                return value.Start().RejectContainsNone(values);
            }

            public Flow<string?> BreakContainsNone(params string[] values)
            {
                return value.Start().BreakContainsNone(values);
            }

            public Flow<string?> RequireContainsNone(params string[] values)
            {
                return value.Start().RequireContainsNone(values);
            }

            public Flow<string?> EnsureContainsNone(params string[] values)
            {
                return value.Start().EnsureContainsNone(values);
            }
        }
    }
}
