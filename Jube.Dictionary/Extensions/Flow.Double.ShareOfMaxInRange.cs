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
            public Flow<double> MatchShareOfMaxInRange(double lowerBound, double upperBound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleShareOfMaxInRange(flow.Value, lowerBound, upperBound, others));
            }

            public Flow<double> RejectShareOfMaxInRange(double lowerBound, double upperBound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleShareOfMaxInRange(flow.Value, lowerBound, upperBound, others));
            }

            public Flow<double> BreakShareOfMaxInRange(double lowerBound, double upperBound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleShareOfMaxInRange(flow.Value, lowerBound, upperBound, others));
            }

            public Flow<double> RequireShareOfMaxInRange(double lowerBound, double upperBound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleShareOfMaxInRange(flow.Value, lowerBound, upperBound, others));
            }

            public Flow<double> EnsureShareOfMaxInRange(double lowerBound, double upperBound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleShareOfMaxInRange(flow.Value, lowerBound, upperBound, others));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchShareOfMaxInRange(double lowerBound, double upperBound, params double[] others)
            {
                return value.Start().MatchShareOfMaxInRange(lowerBound, upperBound, others);
            }

            public Flow<double> RejectShareOfMaxInRange(double lowerBound, double upperBound, params double[] others)
            {
                return value.Start().RejectShareOfMaxInRange(lowerBound, upperBound, others);
            }

            public Flow<double> BreakShareOfMaxInRange(double lowerBound, double upperBound, params double[] others)
            {
                return value.Start().BreakShareOfMaxInRange(lowerBound, upperBound, others);
            }

            public Flow<double> RequireShareOfMaxInRange(double lowerBound, double upperBound, params double[] others)
            {
                return value.Start().RequireShareOfMaxInRange(lowerBound, upperBound, others);
            }

            public Flow<double> EnsureShareOfMaxInRange(double lowerBound, double upperBound, params double[] others)
            {
                return value.Start().EnsureShareOfMaxInRange(lowerBound, upperBound, others);
            }
        }
    }
}
