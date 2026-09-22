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
            public Flow<string?> MatchContainsSomehowInListWith(IEnumerable<string> list, string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringContainsSomehowInListWith(flow.Value, list, options));
            }

            public Flow<string?> RejectContainsSomehowInListWith(IEnumerable<string> list, string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringContainsSomehowInListWith(flow.Value, list, options));
            }

            public Flow<string?> BreakContainsSomehowInListWith(IEnumerable<string> list, string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringContainsSomehowInListWith(flow.Value, list, options));
            }

            public Flow<string?> RequireContainsSomehowInListWith(IEnumerable<string> list, string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringContainsSomehowInListWith(flow.Value, list, options));
            }

            public Flow<string?> EnsureContainsSomehowInListWith(IEnumerable<string> list, string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringContainsSomehowInListWith(flow.Value, list, options));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchContainsSomehowInListWith(IEnumerable<string> list, string? options)
            {
                return value.Start().MatchContainsSomehowInListWith(list, options);
            }

            public Flow<string?> RejectContainsSomehowInListWith(IEnumerable<string> list, string? options)
            {
                return value.Start().RejectContainsSomehowInListWith(list, options);
            }

            public Flow<string?> BreakContainsSomehowInListWith(IEnumerable<string> list, string? options)
            {
                return value.Start().BreakContainsSomehowInListWith(list, options);
            }

            public Flow<string?> RequireContainsSomehowInListWith(IEnumerable<string> list, string? options)
            {
                return value.Start().RequireContainsSomehowInListWith(list, options);
            }

            public Flow<string?> EnsureContainsSomehowInListWith(IEnumerable<string> list, string? options)
            {
                return value.Start().EnsureContainsSomehowInListWith(list, options);
            }
        }
    }
}
