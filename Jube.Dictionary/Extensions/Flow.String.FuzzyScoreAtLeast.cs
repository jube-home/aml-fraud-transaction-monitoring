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
            [Description("True when: the best score across the List token, from 0 to 1, is at least the threshold. Options string may be null. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchFuzzyScoreAtLeast([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The score the pipeline must have reached.")] double threshold,
                [Description("The fuzzy options string, for example jw 0.9 nocase; null for the default profile.")] string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringFuzzyScoreAtLeast(flow.Value, list, threshold, options));
            }

            [Description("True when: the best score across the List token, from 0 to 1, is at least the threshold. Options string may be null. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectFuzzyScoreAtLeast([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The score the pipeline must have reached.")] double threshold,
                [Description("The fuzzy options string, for example jw 0.9 nocase; null for the default profile.")] string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringFuzzyScoreAtLeast(flow.Value, list, threshold, options));
            }

            [Description("True when: the best score across the List token, from 0 to 1, is at least the threshold. Options string may be null. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakFuzzyScoreAtLeast([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The score the pipeline must have reached.")] double threshold,
                [Description("The fuzzy options string, for example jw 0.9 nocase; null for the default profile.")] string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringFuzzyScoreAtLeast(flow.Value, list, threshold, options));
            }

            [Description("True when: the best score across the List token, from 0 to 1, is at least the threshold. Options string may be null. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireFuzzyScoreAtLeast([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The score the pipeline must have reached.")] double threshold,
                [Description("The fuzzy options string, for example jw 0.9 nocase; null for the default profile.")] string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringFuzzyScoreAtLeast(flow.Value, list, threshold, options));
            }

            [Description("True when: the best score across the List token, from 0 to 1, is at least the threshold. Options string may be null. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureFuzzyScoreAtLeast([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The score the pipeline must have reached.")] double threshold,
                [Description("The fuzzy options string, for example jw 0.9 nocase; null for the default profile.")] string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringFuzzyScoreAtLeast(flow.Value, list, threshold, options));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: the best score across the List token, from 0 to 1, is at least the threshold. Options string may be null. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchFuzzyScoreAtLeast([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The score the pipeline must have reached.")] double threshold,
                [Description("The fuzzy options string, for example jw 0.9 nocase; null for the default profile.")] string? options)
            {
                return value.Start().MatchFuzzyScoreAtLeast(list, threshold, options);
            }

            [Description("Begins a pipeline from the value. True when: the best score across the List token, from 0 to 1, is at least the threshold. Options string may be null. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectFuzzyScoreAtLeast([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The score the pipeline must have reached.")] double threshold,
                [Description("The fuzzy options string, for example jw 0.9 nocase; null for the default profile.")] string? options)
            {
                return value.Start().RejectFuzzyScoreAtLeast(list, threshold, options);
            }

            [Description("Begins a pipeline from the value. True when: the best score across the List token, from 0 to 1, is at least the threshold. Options string may be null. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakFuzzyScoreAtLeast([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The score the pipeline must have reached.")] double threshold,
                [Description("The fuzzy options string, for example jw 0.9 nocase; null for the default profile.")] string? options)
            {
                return value.Start().BreakFuzzyScoreAtLeast(list, threshold, options);
            }

            [Description("Begins a pipeline from the value. True when: the best score across the List token, from 0 to 1, is at least the threshold. Options string may be null. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireFuzzyScoreAtLeast([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The score the pipeline must have reached.")] double threshold,
                [Description("The fuzzy options string, for example jw 0.9 nocase; null for the default profile.")] string? options)
            {
                return value.Start().RequireFuzzyScoreAtLeast(list, threshold, options);
            }

            [Description("Begins a pipeline from the value. True when: the best score across the List token, from 0 to 1, is at least the threshold. Options string may be null. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureFuzzyScoreAtLeast([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The score the pipeline must have reached.")] double threshold,
                [Description("The fuzzy options string, for example jw 0.9 nocase; null for the default profile.")] string? options)
            {
                return value.Start().EnsureFuzzyScoreAtLeast(list, threshold, options);
            }
        }
    }
}
