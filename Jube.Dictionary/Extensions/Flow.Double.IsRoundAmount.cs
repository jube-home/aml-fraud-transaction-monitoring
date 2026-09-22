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
        extension(Flow<double> flow)
        {
            public Flow<double> MatchIsRoundAmount(double nearest)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleIsRoundAmount(flow.Value, nearest));
            }

            public Flow<double> RejectIsRoundAmount(double nearest)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleIsRoundAmount(flow.Value, nearest));
            }

            public Flow<double> BreakIsRoundAmount(double nearest)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleIsRoundAmount(flow.Value, nearest));
            }

            public Flow<double> RequireIsRoundAmount(double nearest)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleIsRoundAmount(flow.Value, nearest));
            }

            public Flow<double> EnsureIsRoundAmount(double nearest)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleIsRoundAmount(flow.Value, nearest));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchIsRoundAmount(double nearest)
            {
                return value.Start().MatchIsRoundAmount(nearest);
            }

            public Flow<double> RejectIsRoundAmount(double nearest)
            {
                return value.Start().RejectIsRoundAmount(nearest);
            }

            public Flow<double> BreakIsRoundAmount(double nearest)
            {
                return value.Start().BreakIsRoundAmount(nearest);
            }

            public Flow<double> RequireIsRoundAmount(double nearest)
            {
                return value.Start().RequireIsRoundAmount(nearest);
            }

            public Flow<double> EnsureIsRoundAmount(double nearest)
            {
                return value.Start().EnsureIsRoundAmount(nearest);
            }
        }
    }
}
