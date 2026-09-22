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
            public Flow<double> MatchIsZero()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleIsZero(flow.Value));
            }

            public Flow<double> RejectIsZero()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleIsZero(flow.Value));
            }

            public Flow<double> BreakIsZero()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleIsZero(flow.Value));
            }

            public Flow<double> RequireIsZero()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleIsZero(flow.Value));
            }

            public Flow<double> EnsureIsZero()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleIsZero(flow.Value));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchIsZero()
            {
                return value.Start().MatchIsZero();
            }

            public Flow<double> RejectIsZero()
            {
                return value.Start().RejectIsZero();
            }

            public Flow<double> BreakIsZero()
            {
                return value.Start().BreakIsZero();
            }

            public Flow<double> RequireIsZero()
            {
                return value.Start().RequireIsZero();
            }

            public Flow<double> EnsureIsZero()
            {
                return value.Start().EnsureIsZero();
            }
        }
    }
}
