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
            public Flow<double> MatchMaxWithOutsideRange(double lowerBound, double upperBound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleMaxWithOutsideRange(flow.Value, lowerBound, upperBound, others));
            }

            public Flow<double> RejectMaxWithOutsideRange(double lowerBound, double upperBound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleMaxWithOutsideRange(flow.Value, lowerBound, upperBound, others));
            }

            public Flow<double> BreakMaxWithOutsideRange(double lowerBound, double upperBound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleMaxWithOutsideRange(flow.Value, lowerBound, upperBound, others));
            }

            public Flow<double> RequireMaxWithOutsideRange(double lowerBound, double upperBound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleMaxWithOutsideRange(flow.Value, lowerBound, upperBound, others));
            }

            public Flow<double> EnsureMaxWithOutsideRange(double lowerBound, double upperBound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleMaxWithOutsideRange(flow.Value, lowerBound, upperBound, others));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchMaxWithOutsideRange(double lowerBound, double upperBound, params double[] others)
            {
                return value.Start().MatchMaxWithOutsideRange(lowerBound, upperBound, others);
            }

            public Flow<double> RejectMaxWithOutsideRange(double lowerBound, double upperBound, params double[] others)
            {
                return value.Start().RejectMaxWithOutsideRange(lowerBound, upperBound, others);
            }

            public Flow<double> BreakMaxWithOutsideRange(double lowerBound, double upperBound, params double[] others)
            {
                return value.Start().BreakMaxWithOutsideRange(lowerBound, upperBound, others);
            }

            public Flow<double> RequireMaxWithOutsideRange(double lowerBound, double upperBound, params double[] others)
            {
                return value.Start().RequireMaxWithOutsideRange(lowerBound, upperBound, others);
            }

            public Flow<double> EnsureMaxWithOutsideRange(double lowerBound, double upperBound, params double[] others)
            {
                return value.Start().EnsureMaxWithOutsideRange(lowerBound, upperBound, others);
            }
        }
    }
}
