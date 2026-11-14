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
            [Description("True when: at least the count of entries in the List token match. Options string may be null. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchFuzzyMatchCountAtLeast([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("How many entries must match.")] int count,
                [Description("The fuzzy options string, for example jw 0.9 nocase; null for the default profile.")] string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringFuzzyMatchCountAtLeast(flow.Value, list, count, options));
            }

            [Description("True when: at least the count of entries in the List token match. Options string may be null. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectFuzzyMatchCountAtLeast([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("How many entries must match.")] int count,
                [Description("The fuzzy options string, for example jw 0.9 nocase; null for the default profile.")] string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringFuzzyMatchCountAtLeast(flow.Value, list, count, options));
            }

            [Description("True when: at least the count of entries in the List token match. Options string may be null. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakFuzzyMatchCountAtLeast([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("How many entries must match.")] int count,
                [Description("The fuzzy options string, for example jw 0.9 nocase; null for the default profile.")] string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringFuzzyMatchCountAtLeast(flow.Value, list, count, options));
            }

            [Description("True when: at least the count of entries in the List token match. Options string may be null. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireFuzzyMatchCountAtLeast([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("How many entries must match.")] int count,
                [Description("The fuzzy options string, for example jw 0.9 nocase; null for the default profile.")] string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringFuzzyMatchCountAtLeast(flow.Value, list, count, options));
            }

            [Description("True when: at least the count of entries in the List token match. Options string may be null. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureFuzzyMatchCountAtLeast([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("How many entries must match.")] int count,
                [Description("The fuzzy options string, for example jw 0.9 nocase; null for the default profile.")] string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringFuzzyMatchCountAtLeast(flow.Value, list, count, options));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: at least the count of entries in the List token match. Options string may be null. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchFuzzyMatchCountAtLeast([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("How many entries must match.")] int count,
                [Description("The fuzzy options string, for example jw 0.9 nocase; null for the default profile.")] string? options)
            {
                return value.Start().MatchFuzzyMatchCountAtLeast(list, count, options);
            }

            [Description("Begins a pipeline from the value. True when: at least the count of entries in the List token match. Options string may be null. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectFuzzyMatchCountAtLeast([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("How many entries must match.")] int count,
                [Description("The fuzzy options string, for example jw 0.9 nocase; null for the default profile.")] string? options)
            {
                return value.Start().RejectFuzzyMatchCountAtLeast(list, count, options);
            }

            [Description("Begins a pipeline from the value. True when: at least the count of entries in the List token match. Options string may be null. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakFuzzyMatchCountAtLeast([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("How many entries must match.")] int count,
                [Description("The fuzzy options string, for example jw 0.9 nocase; null for the default profile.")] string? options)
            {
                return value.Start().BreakFuzzyMatchCountAtLeast(list, count, options);
            }

            [Description("Begins a pipeline from the value. True when: at least the count of entries in the List token match. Options string may be null. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireFuzzyMatchCountAtLeast([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("How many entries must match.")] int count,
                [Description("The fuzzy options string, for example jw 0.9 nocase; null for the default profile.")] string? options)
            {
                return value.Start().RequireFuzzyMatchCountAtLeast(list, count, options);
            }

            [Description("Begins a pipeline from the value. True when: at least the count of entries in the List token match. Options string may be null. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureFuzzyMatchCountAtLeast([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("How many entries must match.")] int count,
                [Description("The fuzzy options string, for example jw 0.9 nocase; null for the default profile.")] string? options)
            {
                return value.Start().EnsureFuzzyMatchCountAtLeast(list, count, options);
            }
        }
    }
}
