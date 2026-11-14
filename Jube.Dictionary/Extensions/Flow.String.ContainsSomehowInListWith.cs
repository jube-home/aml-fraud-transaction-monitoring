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
            [Description("True when: some entry is found fuzzily inside the longer text, using an options string (scope is always within). Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchContainsSomehowInListWith([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The fuzzy options string, for example jw 0.9 nocase.")] string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringContainsSomehowInListWith(flow.Value, list, options));
            }

            [Description("True when: some entry is found fuzzily inside the longer text, using an options string (scope is always within). Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectContainsSomehowInListWith([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The fuzzy options string, for example jw 0.9 nocase.")] string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringContainsSomehowInListWith(flow.Value, list, options));
            }

            [Description("True when: some entry is found fuzzily inside the longer text, using an options string (scope is always within). Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakContainsSomehowInListWith([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The fuzzy options string, for example jw 0.9 nocase.")] string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringContainsSomehowInListWith(flow.Value, list, options));
            }

            [Description("True when: some entry is found fuzzily inside the longer text, using an options string (scope is always within). Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireContainsSomehowInListWith([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The fuzzy options string, for example jw 0.9 nocase.")] string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringContainsSomehowInListWith(flow.Value, list, options));
            }

            [Description("True when: some entry is found fuzzily inside the longer text, using an options string (scope is always within). Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureContainsSomehowInListWith([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The fuzzy options string, for example jw 0.9 nocase.")] string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringContainsSomehowInListWith(flow.Value, list, options));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: some entry is found fuzzily inside the longer text, using an options string (scope is always within). Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchContainsSomehowInListWith([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The fuzzy options string, for example jw 0.9 nocase.")] string? options)
            {
                return value.Start().MatchContainsSomehowInListWith(list, options);
            }

            [Description("Begins a pipeline from the value. True when: some entry is found fuzzily inside the longer text, using an options string (scope is always within). Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectContainsSomehowInListWith([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The fuzzy options string, for example jw 0.9 nocase.")] string? options)
            {
                return value.Start().RejectContainsSomehowInListWith(list, options);
            }

            [Description("Begins a pipeline from the value. True when: some entry is found fuzzily inside the longer text, using an options string (scope is always within). Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakContainsSomehowInListWith([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The fuzzy options string, for example jw 0.9 nocase.")] string? options)
            {
                return value.Start().BreakContainsSomehowInListWith(list, options);
            }

            [Description("Begins a pipeline from the value. True when: some entry is found fuzzily inside the longer text, using an options string (scope is always within). Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireContainsSomehowInListWith([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The fuzzy options string, for example jw 0.9 nocase.")] string? options)
            {
                return value.Start().RequireContainsSomehowInListWith(list, options);
            }

            [Description("Begins a pipeline from the value. True when: some entry is found fuzzily inside the longer text, using an options string (scope is always within). Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureContainsSomehowInListWith([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The fuzzy options string, for example jw 0.9 nocase.")] string? options)
            {
                return value.Start().EnsureContainsSomehowInListWith(list, options);
            }
        }
    }
}
