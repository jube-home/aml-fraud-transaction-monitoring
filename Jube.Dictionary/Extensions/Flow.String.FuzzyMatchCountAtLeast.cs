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
            public Flow<string?> MatchFuzzyMatchCountAtLeast(IEnumerable<string> list, int count, string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringFuzzyMatchCountAtLeast(flow.Value, list, count, options));
            }

            public Flow<string?> RejectFuzzyMatchCountAtLeast(IEnumerable<string> list, int count, string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringFuzzyMatchCountAtLeast(flow.Value, list, count, options));
            }

            public Flow<string?> BreakFuzzyMatchCountAtLeast(IEnumerable<string> list, int count, string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringFuzzyMatchCountAtLeast(flow.Value, list, count, options));
            }

            public Flow<string?> RequireFuzzyMatchCountAtLeast(IEnumerable<string> list, int count, string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringFuzzyMatchCountAtLeast(flow.Value, list, count, options));
            }

            public Flow<string?> EnsureFuzzyMatchCountAtLeast(IEnumerable<string> list, int count, string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringFuzzyMatchCountAtLeast(flow.Value, list, count, options));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchFuzzyMatchCountAtLeast(IEnumerable<string> list, int count, string? options)
            {
                return value.Start().MatchFuzzyMatchCountAtLeast(list, count, options);
            }

            public Flow<string?> RejectFuzzyMatchCountAtLeast(IEnumerable<string> list, int count, string? options)
            {
                return value.Start().RejectFuzzyMatchCountAtLeast(list, count, options);
            }

            public Flow<string?> BreakFuzzyMatchCountAtLeast(IEnumerable<string> list, int count, string? options)
            {
                return value.Start().BreakFuzzyMatchCountAtLeast(list, count, options);
            }

            public Flow<string?> RequireFuzzyMatchCountAtLeast(IEnumerable<string> list, int count, string? options)
            {
                return value.Start().RequireFuzzyMatchCountAtLeast(list, count, options);
            }

            public Flow<string?> EnsureFuzzyMatchCountAtLeast(IEnumerable<string> list, int count, string? options)
            {
                return value.Start().EnsureFuzzyMatchCountAtLeast(list, count, options);
            }
        }
    }
}
