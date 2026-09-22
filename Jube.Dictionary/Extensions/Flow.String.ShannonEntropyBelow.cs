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
            public Flow<string?> MatchShannonEntropyBelow(double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.StringShannonEntropyBelow(flow.Value, threshold));
            }

            public Flow<string?> RejectShannonEntropyBelow(double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.StringShannonEntropyBelow(flow.Value, threshold));
            }

            public Flow<string?> BreakShannonEntropyBelow(double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.StringShannonEntropyBelow(flow.Value, threshold));
            }

            public Flow<string?> RequireShannonEntropyBelow(double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.StringShannonEntropyBelow(flow.Value, threshold));
            }

            public Flow<string?> EnsureShannonEntropyBelow(double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.StringShannonEntropyBelow(flow.Value, threshold));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchShannonEntropyBelow(double threshold)
            {
                return value.Start().MatchShannonEntropyBelow(threshold);
            }

            public Flow<string?> RejectShannonEntropyBelow(double threshold)
            {
                return value.Start().RejectShannonEntropyBelow(threshold);
            }

            public Flow<string?> BreakShannonEntropyBelow(double threshold)
            {
                return value.Start().BreakShannonEntropyBelow(threshold);
            }

            public Flow<string?> RequireShannonEntropyBelow(double threshold)
            {
                return value.Start().RequireShannonEntropyBelow(threshold);
            }

            public Flow<string?> EnsureShannonEntropyBelow(double threshold)
            {
                return value.Start().EnsureShannonEntropyBelow(threshold);
            }
        }
    }
}
