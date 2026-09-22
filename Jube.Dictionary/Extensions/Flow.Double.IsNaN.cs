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
            public Flow<double> MatchIsNaN()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleIsNaN(flow.Value));
            }

            public Flow<double> RejectIsNaN()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleIsNaN(flow.Value));
            }

            public Flow<double> BreakIsNaN()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleIsNaN(flow.Value));
            }

            public Flow<double> RequireIsNaN()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleIsNaN(flow.Value));
            }

            public Flow<double> EnsureIsNaN()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleIsNaN(flow.Value));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchIsNaN()
            {
                return value.Start().MatchIsNaN();
            }

            public Flow<double> RejectIsNaN()
            {
                return value.Start().RejectIsNaN();
            }

            public Flow<double> BreakIsNaN()
            {
                return value.Start().BreakIsNaN();
            }

            public Flow<double> RequireIsNaN()
            {
                return value.Start().RequireIsNaN();
            }

            public Flow<double> EnsureIsNaN()
            {
                return value.Start().EnsureIsNaN();
            }
        }
    }
}
