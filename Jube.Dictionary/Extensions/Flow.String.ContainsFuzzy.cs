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
            public Flow<string?> MatchContainsFuzzy(string? term, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringContainsFuzzy(flow.Value, term, threshold));
            }

            public Flow<string?> RejectContainsFuzzy(string? term, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringContainsFuzzy(flow.Value, term, threshold));
            }

            public Flow<string?> BreakContainsFuzzy(string? term, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringContainsFuzzy(flow.Value, term, threshold));
            }

            public Flow<string?> RequireContainsFuzzy(string? term, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringContainsFuzzy(flow.Value, term, threshold));
            }

            public Flow<string?> EnsureContainsFuzzy(string? term, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringContainsFuzzy(flow.Value, term, threshold));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchContainsFuzzy(string? term, double threshold)
            {
                return value.Start().MatchContainsFuzzy(term, threshold);
            }

            public Flow<string?> RejectContainsFuzzy(string? term, double threshold)
            {
                return value.Start().RejectContainsFuzzy(term, threshold);
            }

            public Flow<string?> BreakContainsFuzzy(string? term, double threshold)
            {
                return value.Start().BreakContainsFuzzy(term, threshold);
            }

            public Flow<string?> RequireContainsFuzzy(string? term, double threshold)
            {
                return value.Start().RequireContainsFuzzy(term, threshold);
            }

            public Flow<string?> EnsureContainsFuzzy(string? term, double threshold)
            {
                return value.Start().EnsureContainsFuzzy(term, threshold);
            }
        }
    }
}
