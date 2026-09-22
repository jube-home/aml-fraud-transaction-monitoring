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
            public Flow<string?> MatchJaroWinklerAtLeast(string? other, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringJaroWinklerAtLeast(flow.Value, other, threshold));
            }

            public Flow<string?> RejectJaroWinklerAtLeast(string? other, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringJaroWinklerAtLeast(flow.Value, other, threshold));
            }

            public Flow<string?> BreakJaroWinklerAtLeast(string? other, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringJaroWinklerAtLeast(flow.Value, other, threshold));
            }

            public Flow<string?> RequireJaroWinklerAtLeast(string? other, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringJaroWinklerAtLeast(flow.Value, other, threshold));
            }

            public Flow<string?> EnsureJaroWinklerAtLeast(string? other, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringJaroWinklerAtLeast(flow.Value, other, threshold));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchJaroWinklerAtLeast(string? other, double threshold)
            {
                return value.Start().MatchJaroWinklerAtLeast(other, threshold);
            }

            public Flow<string?> RejectJaroWinklerAtLeast(string? other, double threshold)
            {
                return value.Start().RejectJaroWinklerAtLeast(other, threshold);
            }

            public Flow<string?> BreakJaroWinklerAtLeast(string? other, double threshold)
            {
                return value.Start().BreakJaroWinklerAtLeast(other, threshold);
            }

            public Flow<string?> RequireJaroWinklerAtLeast(string? other, double threshold)
            {
                return value.Start().RequireJaroWinklerAtLeast(other, threshold);
            }

            public Flow<string?> EnsureJaroWinklerAtLeast(string? other, double threshold)
            {
                return value.Start().EnsureJaroWinklerAtLeast(other, threshold);
            }
        }
    }
}
