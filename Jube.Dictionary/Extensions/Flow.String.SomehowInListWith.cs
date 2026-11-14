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
            [Description("True when: fuzzily in the List token using an options string. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchSomehowInListWith([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The fuzzy options string, for example jw 0.9 nocase.")] string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringSomehowInListWith(flow.Value, list, options));
            }

            [Description("True when: fuzzily in the List token using an options string. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectSomehowInListWith([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The fuzzy options string, for example jw 0.9 nocase.")] string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringSomehowInListWith(flow.Value, list, options));
            }

            [Description("True when: fuzzily in the List token using an options string. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakSomehowInListWith([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The fuzzy options string, for example jw 0.9 nocase.")] string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringSomehowInListWith(flow.Value, list, options));
            }

            [Description("True when: fuzzily in the List token using an options string. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireSomehowInListWith([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The fuzzy options string, for example jw 0.9 nocase.")] string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringSomehowInListWith(flow.Value, list, options));
            }

            [Description("True when: fuzzily in the List token using an options string. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureSomehowInListWith([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The fuzzy options string, for example jw 0.9 nocase.")] string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringSomehowInListWith(flow.Value, list, options));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: fuzzily in the List token using an options string. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchSomehowInListWith([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The fuzzy options string, for example jw 0.9 nocase.")] string? options)
            {
                return value.Start().MatchSomehowInListWith(list, options);
            }

            [Description("Begins a pipeline from the value. True when: fuzzily in the List token using an options string. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectSomehowInListWith([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The fuzzy options string, for example jw 0.9 nocase.")] string? options)
            {
                return value.Start().RejectSomehowInListWith(list, options);
            }

            [Description("Begins a pipeline from the value. True when: fuzzily in the List token using an options string. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakSomehowInListWith([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The fuzzy options string, for example jw 0.9 nocase.")] string? options)
            {
                return value.Start().BreakSomehowInListWith(list, options);
            }

            [Description("Begins a pipeline from the value. True when: fuzzily in the List token using an options string. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireSomehowInListWith([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The fuzzy options string, for example jw 0.9 nocase.")] string? options)
            {
                return value.Start().RequireSomehowInListWith(list, options);
            }

            [Description("Begins a pipeline from the value. True when: fuzzily in the List token using an options string. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureSomehowInListWith([Description("The List token whose entries are compared.")] IEnumerable<string> list,
                [Description("The fuzzy options string, for example jw 0.9 nocase.")] string? options)
            {
                return value.Start().EnsureSomehowInListWith(list, options);
            }
        }
    }
}
