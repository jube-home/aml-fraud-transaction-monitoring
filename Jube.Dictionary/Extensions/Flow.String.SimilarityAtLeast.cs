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
            [Description("True when: Levenshtein based similarity is at least the threshold, from 0 to 1. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchSimilarityAtLeast([Description("The text the value is compared with.")] string? other,
                [Description("The similarity from 0 to 1 that counts.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringSimilarityAtLeast(flow.Value, other, threshold));
            }

            [Description("True when: Levenshtein based similarity is at least the threshold, from 0 to 1. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectSimilarityAtLeast([Description("The text the value is compared with.")] string? other,
                [Description("The similarity from 0 to 1 that counts.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringSimilarityAtLeast(flow.Value, other, threshold));
            }

            [Description("True when: Levenshtein based similarity is at least the threshold, from 0 to 1. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakSimilarityAtLeast([Description("The text the value is compared with.")] string? other,
                [Description("The similarity from 0 to 1 that counts.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringSimilarityAtLeast(flow.Value, other, threshold));
            }

            [Description("True when: Levenshtein based similarity is at least the threshold, from 0 to 1. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireSimilarityAtLeast([Description("The text the value is compared with.")] string? other,
                [Description("The similarity from 0 to 1 that counts.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringSimilarityAtLeast(flow.Value, other, threshold));
            }

            [Description("True when: Levenshtein based similarity is at least the threshold, from 0 to 1. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureSimilarityAtLeast([Description("The text the value is compared with.")] string? other,
                [Description("The similarity from 0 to 1 that counts.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringSimilarityAtLeast(flow.Value, other, threshold));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: Levenshtein based similarity is at least the threshold, from 0 to 1. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchSimilarityAtLeast([Description("The text the value is compared with.")] string? other,
                [Description("The similarity from 0 to 1 that counts.")] double threshold)
            {
                return value.Start().MatchSimilarityAtLeast(other, threshold);
            }

            [Description("Begins a pipeline from the value. True when: Levenshtein based similarity is at least the threshold, from 0 to 1. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectSimilarityAtLeast([Description("The text the value is compared with.")] string? other,
                [Description("The similarity from 0 to 1 that counts.")] double threshold)
            {
                return value.Start().RejectSimilarityAtLeast(other, threshold);
            }

            [Description("Begins a pipeline from the value. True when: Levenshtein based similarity is at least the threshold, from 0 to 1. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakSimilarityAtLeast([Description("The text the value is compared with.")] string? other,
                [Description("The similarity from 0 to 1 that counts.")] double threshold)
            {
                return value.Start().BreakSimilarityAtLeast(other, threshold);
            }

            [Description("Begins a pipeline from the value. True when: Levenshtein based similarity is at least the threshold, from 0 to 1. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireSimilarityAtLeast([Description("The text the value is compared with.")] string? other,
                [Description("The similarity from 0 to 1 that counts.")] double threshold)
            {
                return value.Start().RequireSimilarityAtLeast(other, threshold);
            }

            [Description("Begins a pipeline from the value. True when: Levenshtein based similarity is at least the threshold, from 0 to 1. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureSimilarityAtLeast([Description("The text the value is compared with.")] string? other,
                [Description("The similarity from 0 to 1 that counts.")] double threshold)
            {
                return value.Start().EnsureSimilarityAtLeast(other, threshold);
            }
        }
    }
}
