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
            public Flow<double> MatchShareOfMaxAbove(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleShareOfMaxAbove(flow.Value, bound, others));
            }

            public Flow<double> RejectShareOfMaxAbove(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleShareOfMaxAbove(flow.Value, bound, others));
            }

            public Flow<double> BreakShareOfMaxAbove(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleShareOfMaxAbove(flow.Value, bound, others));
            }

            public Flow<double> RequireShareOfMaxAbove(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleShareOfMaxAbove(flow.Value, bound, others));
            }

            public Flow<double> EnsureShareOfMaxAbove(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleShareOfMaxAbove(flow.Value, bound, others));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchShareOfMaxAbove(double bound, params double[] others)
            {
                return value.Start().MatchShareOfMaxAbove(bound, others);
            }

            public Flow<double> RejectShareOfMaxAbove(double bound, params double[] others)
            {
                return value.Start().RejectShareOfMaxAbove(bound, others);
            }

            public Flow<double> BreakShareOfMaxAbove(double bound, params double[] others)
            {
                return value.Start().BreakShareOfMaxAbove(bound, others);
            }

            public Flow<double> RequireShareOfMaxAbove(double bound, params double[] others)
            {
                return value.Start().RequireShareOfMaxAbove(bound, others);
            }

            public Flow<double> EnsureShareOfMaxAbove(double bound, params double[] others)
            {
                return value.Start().EnsureShareOfMaxAbove(bound, others);
            }
        }
    }
}
