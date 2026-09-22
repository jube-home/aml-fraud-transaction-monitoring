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
            public Flow<string?> MatchContainsAll(params string[] values)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringContainsAll(flow.Value, values));
            }

            public Flow<string?> RejectContainsAll(params string[] values)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringContainsAll(flow.Value, values));
            }

            public Flow<string?> BreakContainsAll(params string[] values)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringContainsAll(flow.Value, values));
            }

            public Flow<string?> RequireContainsAll(params string[] values)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringContainsAll(flow.Value, values));
            }

            public Flow<string?> EnsureContainsAll(params string[] values)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringContainsAll(flow.Value, values));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchContainsAll(params string[] values)
            {
                return value.Start().MatchContainsAll(values);
            }

            public Flow<string?> RejectContainsAll(params string[] values)
            {
                return value.Start().RejectContainsAll(values);
            }

            public Flow<string?> BreakContainsAll(params string[] values)
            {
                return value.Start().BreakContainsAll(values);
            }

            public Flow<string?> RequireContainsAll(params string[] values)
            {
                return value.Start().RequireContainsAll(values);
            }

            public Flow<string?> EnsureContainsAll(params string[] values)
            {
                return value.Start().EnsureContainsAll(values);
            }
        }
    }
}
