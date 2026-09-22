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
            public Flow<string?> MatchDiceInList(IEnumerable<string> list, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringDiceInList(flow.Value, list, threshold));
            }

            public Flow<string?> RejectDiceInList(IEnumerable<string> list, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringDiceInList(flow.Value, list, threshold));
            }

            public Flow<string?> BreakDiceInList(IEnumerable<string> list, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringDiceInList(flow.Value, list, threshold));
            }

            public Flow<string?> RequireDiceInList(IEnumerable<string> list, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringDiceInList(flow.Value, list, threshold));
            }

            public Flow<string?> EnsureDiceInList(IEnumerable<string> list, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringDiceInList(flow.Value, list, threshold));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchDiceInList(IEnumerable<string> list, double threshold)
            {
                return value.Start().MatchDiceInList(list, threshold);
            }

            public Flow<string?> RejectDiceInList(IEnumerable<string> list, double threshold)
            {
                return value.Start().RejectDiceInList(list, threshold);
            }

            public Flow<string?> BreakDiceInList(IEnumerable<string> list, double threshold)
            {
                return value.Start().BreakDiceInList(list, threshold);
            }

            public Flow<string?> RequireDiceInList(IEnumerable<string> list, double threshold)
            {
                return value.Start().RequireDiceInList(list, threshold);
            }

            public Flow<string?> EnsureDiceInList(IEnumerable<string> list, double threshold)
            {
                return value.Start().EnsureDiceInList(list, threshold);
            }
        }
    }
}
