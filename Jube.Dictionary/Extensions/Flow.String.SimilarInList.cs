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
            public Flow<string?> MatchSimilarInList(IEnumerable<string> list, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringSimilarInList(flow.Value, list, threshold));
            }

            public Flow<string?> RejectSimilarInList(IEnumerable<string> list, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringSimilarInList(flow.Value, list, threshold));
            }

            public Flow<string?> BreakSimilarInList(IEnumerable<string> list, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringSimilarInList(flow.Value, list, threshold));
            }

            public Flow<string?> RequireSimilarInList(IEnumerable<string> list, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringSimilarInList(flow.Value, list, threshold));
            }

            public Flow<string?> EnsureSimilarInList(IEnumerable<string> list, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringSimilarInList(flow.Value, list, threshold));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchSimilarInList(IEnumerable<string> list, double threshold)
            {
                return value.Start().MatchSimilarInList(list, threshold);
            }

            public Flow<string?> RejectSimilarInList(IEnumerable<string> list, double threshold)
            {
                return value.Start().RejectSimilarInList(list, threshold);
            }

            public Flow<string?> BreakSimilarInList(IEnumerable<string> list, double threshold)
            {
                return value.Start().BreakSimilarInList(list, threshold);
            }

            public Flow<string?> RequireSimilarInList(IEnumerable<string> list, double threshold)
            {
                return value.Start().RequireSimilarInList(list, threshold);
            }

            public Flow<string?> EnsureSimilarInList(IEnumerable<string> list, double threshold)
            {
                return value.Start().EnsureSimilarInList(list, threshold);
            }
        }
    }
}
