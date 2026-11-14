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
            [Description("True when: fuzzily like a single value, using an options string. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchSomehowLikeWith([Description("The text the value is compared with.")] string? other,
                [Description("The fuzzy options string, for example jw 0.9 nocase.")] string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringSomehowLikeWith(flow.Value, other, options));
            }

            [Description("True when: fuzzily like a single value, using an options string. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectSomehowLikeWith([Description("The text the value is compared with.")] string? other,
                [Description("The fuzzy options string, for example jw 0.9 nocase.")] string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringSomehowLikeWith(flow.Value, other, options));
            }

            [Description("True when: fuzzily like a single value, using an options string. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakSomehowLikeWith([Description("The text the value is compared with.")] string? other,
                [Description("The fuzzy options string, for example jw 0.9 nocase.")] string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringSomehowLikeWith(flow.Value, other, options));
            }

            [Description("True when: fuzzily like a single value, using an options string. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireSomehowLikeWith([Description("The text the value is compared with.")] string? other,
                [Description("The fuzzy options string, for example jw 0.9 nocase.")] string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringSomehowLikeWith(flow.Value, other, options));
            }

            [Description("True when: fuzzily like a single value, using an options string. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureSomehowLikeWith([Description("The text the value is compared with.")] string? other,
                [Description("The fuzzy options string, for example jw 0.9 nocase.")] string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringSomehowLikeWith(flow.Value, other, options));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: fuzzily like a single value, using an options string. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchSomehowLikeWith([Description("The text the value is compared with.")] string? other,
                [Description("The fuzzy options string, for example jw 0.9 nocase.")] string? options)
            {
                return value.Start().MatchSomehowLikeWith(other, options);
            }

            [Description("Begins a pipeline from the value. True when: fuzzily like a single value, using an options string. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectSomehowLikeWith([Description("The text the value is compared with.")] string? other,
                [Description("The fuzzy options string, for example jw 0.9 nocase.")] string? options)
            {
                return value.Start().RejectSomehowLikeWith(other, options);
            }

            [Description("Begins a pipeline from the value. True when: fuzzily like a single value, using an options string. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakSomehowLikeWith([Description("The text the value is compared with.")] string? other,
                [Description("The fuzzy options string, for example jw 0.9 nocase.")] string? options)
            {
                return value.Start().BreakSomehowLikeWith(other, options);
            }

            [Description("Begins a pipeline from the value. True when: fuzzily like a single value, using an options string. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireSomehowLikeWith([Description("The text the value is compared with.")] string? other,
                [Description("The fuzzy options string, for example jw 0.9 nocase.")] string? options)
            {
                return value.Start().RequireSomehowLikeWith(other, options);
            }

            [Description("Begins a pipeline from the value. True when: fuzzily like a single value, using an options string. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureSomehowLikeWith([Description("The text the value is compared with.")] string? other,
                [Description("The fuzzy options string, for example jw 0.9 nocase.")] string? options)
            {
                return value.Start().EnsureSomehowLikeWith(other, options);
            }
        }
    }
}
