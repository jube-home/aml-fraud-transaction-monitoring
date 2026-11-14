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
            [Description("True when: matches the regular expression. A bad pattern or a 100ms timeout is false, never an exception. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchMatches([Description("The regular expression pattern to match.")] string? pattern)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringMatches(flow.Value, pattern));
            }

            [Description("True when: matches the regular expression. A bad pattern or a 100ms timeout is false, never an exception. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectMatches([Description("The regular expression pattern to match.")] string? pattern)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringMatches(flow.Value, pattern));
            }

            [Description("True when: matches the regular expression. A bad pattern or a 100ms timeout is false, never an exception. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakMatches([Description("The regular expression pattern to match.")] string? pattern)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringMatches(flow.Value, pattern));
            }

            [Description("True when: matches the regular expression. A bad pattern or a 100ms timeout is false, never an exception. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireMatches([Description("The regular expression pattern to match.")] string? pattern)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringMatches(flow.Value, pattern));
            }

            [Description("True when: matches the regular expression. A bad pattern or a 100ms timeout is false, never an exception. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureMatches([Description("The regular expression pattern to match.")] string? pattern)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringMatches(flow.Value, pattern));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: matches the regular expression. A bad pattern or a 100ms timeout is false, never an exception. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchMatches([Description("The regular expression pattern to match.")] string? pattern)
            {
                return value.Start().MatchMatches(pattern);
            }

            [Description("Begins a pipeline from the value. True when: matches the regular expression. A bad pattern or a 100ms timeout is false, never an exception. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectMatches([Description("The regular expression pattern to match.")] string? pattern)
            {
                return value.Start().RejectMatches(pattern);
            }

            [Description("Begins a pipeline from the value. True when: matches the regular expression. A bad pattern or a 100ms timeout is false, never an exception. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakMatches([Description("The regular expression pattern to match.")] string? pattern)
            {
                return value.Start().BreakMatches(pattern);
            }

            [Description("Begins a pipeline from the value. True when: matches the regular expression. A bad pattern or a 100ms timeout is false, never an exception. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireMatches([Description("The regular expression pattern to match.")] string? pattern)
            {
                return value.Start().RequireMatches(pattern);
            }

            [Description("Begins a pipeline from the value. True when: matches the regular expression. A bad pattern or a 100ms timeout is false, never an exception. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureMatches([Description("The regular expression pattern to match.")] string? pattern)
            {
                return value.Start().EnsureMatches(pattern);
            }
        }
    }
}
