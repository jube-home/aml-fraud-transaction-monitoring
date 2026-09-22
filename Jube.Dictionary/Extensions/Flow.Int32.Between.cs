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
        extension(Flow<int> flow)
        {
            public Flow<int> MatchBetween(int minimum, int maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.Int32Between(flow.Value, minimum, maximum));
            }

            public Flow<int> RejectBetween(int minimum, int maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.Int32Between(flow.Value, minimum, maximum));
            }

            public Flow<int> BreakBetween(int minimum, int maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.Int32Between(flow.Value, minimum, maximum));
            }

            public Flow<int> RequireBetween(int minimum, int maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.Int32Between(flow.Value, minimum, maximum));
            }

            public Flow<int> EnsureBetween(int minimum, int maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.Int32Between(flow.Value, minimum, maximum));
            }
        }

        extension(int value)
        {
            public Flow<int> MatchBetween(int minimum, int maximum)
            {
                return value.Start().MatchBetween(minimum, maximum);
            }

            public Flow<int> RejectBetween(int minimum, int maximum)
            {
                return value.Start().RejectBetween(minimum, maximum);
            }

            public Flow<int> BreakBetween(int minimum, int maximum)
            {
                return value.Start().BreakBetween(minimum, maximum);
            }

            public Flow<int> RequireBetween(int minimum, int maximum)
            {
                return value.Start().RequireBetween(minimum, maximum);
            }

            public Flow<int> EnsureBetween(int minimum, int maximum)
            {
                return value.Start().EnsureBetween(minimum, maximum);
            }
        }
    }
}
