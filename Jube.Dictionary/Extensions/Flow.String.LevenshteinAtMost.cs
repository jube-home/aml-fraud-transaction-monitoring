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
            [Description("True when: edit distance to the argument is at most the distance. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchLevenshteinAtMost([Description("The text the value is compared with.")] string? other,
                [Description("The most edits allowed.")] int distance)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringLevenshteinAtMost(flow.Value, other, distance));
            }

            [Description("True when: edit distance to the argument is at most the distance. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectLevenshteinAtMost([Description("The text the value is compared with.")] string? other,
                [Description("The most edits allowed.")] int distance)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringLevenshteinAtMost(flow.Value, other, distance));
            }

            [Description("True when: edit distance to the argument is at most the distance. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakLevenshteinAtMost([Description("The text the value is compared with.")] string? other,
                [Description("The most edits allowed.")] int distance)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringLevenshteinAtMost(flow.Value, other, distance));
            }

            [Description("True when: edit distance to the argument is at most the distance. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireLevenshteinAtMost([Description("The text the value is compared with.")] string? other,
                [Description("The most edits allowed.")] int distance)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringLevenshteinAtMost(flow.Value, other, distance));
            }

            [Description("True when: edit distance to the argument is at most the distance. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureLevenshteinAtMost([Description("The text the value is compared with.")] string? other,
                [Description("The most edits allowed.")] int distance)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringLevenshteinAtMost(flow.Value, other, distance));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: edit distance to the argument is at most the distance. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchLevenshteinAtMost([Description("The text the value is compared with.")] string? other,
                [Description("The most edits allowed.")] int distance)
            {
                return value.Start().MatchLevenshteinAtMost(other, distance);
            }

            [Description("Begins a pipeline from the value. True when: edit distance to the argument is at most the distance. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectLevenshteinAtMost([Description("The text the value is compared with.")] string? other,
                [Description("The most edits allowed.")] int distance)
            {
                return value.Start().RejectLevenshteinAtMost(other, distance);
            }

            [Description("Begins a pipeline from the value. True when: edit distance to the argument is at most the distance. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakLevenshteinAtMost([Description("The text the value is compared with.")] string? other,
                [Description("The most edits allowed.")] int distance)
            {
                return value.Start().BreakLevenshteinAtMost(other, distance);
            }

            [Description("Begins a pipeline from the value. True when: edit distance to the argument is at most the distance. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireLevenshteinAtMost([Description("The text the value is compared with.")] string? other,
                [Description("The most edits allowed.")] int distance)
            {
                return value.Start().RequireLevenshteinAtMost(other, distance);
            }

            [Description("Begins a pipeline from the value. True when: edit distance to the argument is at most the distance. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureLevenshteinAtMost([Description("The text the value is compared with.")] string? other,
                [Description("The most edits allowed.")] int distance)
            {
                return value.Start().EnsureLevenshteinAtMost(other, distance);
            }
        }
    }
}
