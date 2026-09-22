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
            public Flow<string?> MatchJaroWinklerInList(IEnumerable<string> list, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringJaroWinklerInList(flow.Value, list, threshold));
            }

            public Flow<string?> RejectJaroWinklerInList(IEnumerable<string> list, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringJaroWinklerInList(flow.Value, list, threshold));
            }

            public Flow<string?> BreakJaroWinklerInList(IEnumerable<string> list, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringJaroWinklerInList(flow.Value, list, threshold));
            }

            public Flow<string?> RequireJaroWinklerInList(IEnumerable<string> list, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringJaroWinklerInList(flow.Value, list, threshold));
            }

            public Flow<string?> EnsureJaroWinklerInList(IEnumerable<string> list, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringJaroWinklerInList(flow.Value, list, threshold));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchJaroWinklerInList(IEnumerable<string> list, double threshold)
            {
                return value.Start().MatchJaroWinklerInList(list, threshold);
            }

            public Flow<string?> RejectJaroWinklerInList(IEnumerable<string> list, double threshold)
            {
                return value.Start().RejectJaroWinklerInList(list, threshold);
            }

            public Flow<string?> BreakJaroWinklerInList(IEnumerable<string> list, double threshold)
            {
                return value.Start().BreakJaroWinklerInList(list, threshold);
            }

            public Flow<string?> RequireJaroWinklerInList(IEnumerable<string> list, double threshold)
            {
                return value.Start().RequireJaroWinklerInList(list, threshold);
            }

            public Flow<string?> EnsureJaroWinklerInList(IEnumerable<string> list, double threshold)
            {
                return value.Start().EnsureJaroWinklerInList(list, threshold);
            }
        }
    }
}
