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
            public Flow<double> MatchMaxWithBelow(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleMaxWithBelow(flow.Value, bound, others));
            }

            public Flow<double> RejectMaxWithBelow(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleMaxWithBelow(flow.Value, bound, others));
            }

            public Flow<double> BreakMaxWithBelow(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleMaxWithBelow(flow.Value, bound, others));
            }

            public Flow<double> RequireMaxWithBelow(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleMaxWithBelow(flow.Value, bound, others));
            }

            public Flow<double> EnsureMaxWithBelow(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleMaxWithBelow(flow.Value, bound, others));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchMaxWithBelow(double bound, params double[] others)
            {
                return value.Start().MatchMaxWithBelow(bound, others);
            }

            public Flow<double> RejectMaxWithBelow(double bound, params double[] others)
            {
                return value.Start().RejectMaxWithBelow(bound, others);
            }

            public Flow<double> BreakMaxWithBelow(double bound, params double[] others)
            {
                return value.Start().BreakMaxWithBelow(bound, others);
            }

            public Flow<double> RequireMaxWithBelow(double bound, params double[] others)
            {
                return value.Start().RequireMaxWithBelow(bound, others);
            }

            public Flow<double> EnsureMaxWithBelow(double bound, params double[] others)
            {
                return value.Start().EnsureMaxWithBelow(bound, others);
            }
        }
    }
}
