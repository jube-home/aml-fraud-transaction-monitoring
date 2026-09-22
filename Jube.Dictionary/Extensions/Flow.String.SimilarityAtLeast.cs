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
            public Flow<string?> MatchSimilarityAtLeast(string? other, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringSimilarityAtLeast(flow.Value, other, threshold));
            }

            public Flow<string?> RejectSimilarityAtLeast(string? other, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringSimilarityAtLeast(flow.Value, other, threshold));
            }

            public Flow<string?> BreakSimilarityAtLeast(string? other, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringSimilarityAtLeast(flow.Value, other, threshold));
            }

            public Flow<string?> RequireSimilarityAtLeast(string? other, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringSimilarityAtLeast(flow.Value, other, threshold));
            }

            public Flow<string?> EnsureSimilarityAtLeast(string? other, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringSimilarityAtLeast(flow.Value, other, threshold));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchSimilarityAtLeast(string? other, double threshold)
            {
                return value.Start().MatchSimilarityAtLeast(other, threshold);
            }

            public Flow<string?> RejectSimilarityAtLeast(string? other, double threshold)
            {
                return value.Start().RejectSimilarityAtLeast(other, threshold);
            }

            public Flow<string?> BreakSimilarityAtLeast(string? other, double threshold)
            {
                return value.Start().BreakSimilarityAtLeast(other, threshold);
            }

            public Flow<string?> RequireSimilarityAtLeast(string? other, double threshold)
            {
                return value.Start().RequireSimilarityAtLeast(other, threshold);
            }

            public Flow<string?> EnsureSimilarityAtLeast(string? other, double threshold)
            {
                return value.Start().EnsureSimilarityAtLeast(other, threshold);
            }
        }
    }
}
