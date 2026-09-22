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
            public Flow<double> MatchIsFinite()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleIsFinite(flow.Value));
            }

            public Flow<double> RejectIsFinite()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleIsFinite(flow.Value));
            }

            public Flow<double> BreakIsFinite()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleIsFinite(flow.Value));
            }

            public Flow<double> RequireIsFinite()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleIsFinite(flow.Value));
            }

            public Flow<double> EnsureIsFinite()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleIsFinite(flow.Value));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchIsFinite()
            {
                return value.Start().MatchIsFinite();
            }

            public Flow<double> RejectIsFinite()
            {
                return value.Start().RejectIsFinite();
            }

            public Flow<double> BreakIsFinite()
            {
                return value.Start().BreakIsFinite();
            }

            public Flow<double> RequireIsFinite()
            {
                return value.Start().RequireIsFinite();
            }

            public Flow<double> EnsureIsFinite()
            {
                return value.Start().EnsureIsFinite();
            }
        }
    }
}
