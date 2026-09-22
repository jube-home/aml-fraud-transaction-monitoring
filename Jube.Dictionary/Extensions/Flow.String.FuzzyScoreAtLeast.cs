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
            public Flow<string?> MatchFuzzyScoreAtLeast(IEnumerable<string> list, double threshold, string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringFuzzyScoreAtLeast(flow.Value, list, threshold, options));
            }

            public Flow<string?> RejectFuzzyScoreAtLeast(IEnumerable<string> list, double threshold, string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringFuzzyScoreAtLeast(flow.Value, list, threshold, options));
            }

            public Flow<string?> BreakFuzzyScoreAtLeast(IEnumerable<string> list, double threshold, string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringFuzzyScoreAtLeast(flow.Value, list, threshold, options));
            }

            public Flow<string?> RequireFuzzyScoreAtLeast(IEnumerable<string> list, double threshold, string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringFuzzyScoreAtLeast(flow.Value, list, threshold, options));
            }

            public Flow<string?> EnsureFuzzyScoreAtLeast(IEnumerable<string> list, double threshold, string? options)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringFuzzyScoreAtLeast(flow.Value, list, threshold, options));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchFuzzyScoreAtLeast(IEnumerable<string> list, double threshold, string? options)
            {
                return value.Start().MatchFuzzyScoreAtLeast(list, threshold, options);
            }

            public Flow<string?> RejectFuzzyScoreAtLeast(IEnumerable<string> list, double threshold, string? options)
            {
                return value.Start().RejectFuzzyScoreAtLeast(list, threshold, options);
            }

            public Flow<string?> BreakFuzzyScoreAtLeast(IEnumerable<string> list, double threshold, string? options)
            {
                return value.Start().BreakFuzzyScoreAtLeast(list, threshold, options);
            }

            public Flow<string?> RequireFuzzyScoreAtLeast(IEnumerable<string> list, double threshold, string? options)
            {
                return value.Start().RequireFuzzyScoreAtLeast(list, threshold, options);
            }

            public Flow<string?> EnsureFuzzyScoreAtLeast(IEnumerable<string> list, double threshold, string? options)
            {
                return value.Start().EnsureFuzzyScoreAtLeast(list, threshold, options);
            }
        }
    }
}
