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
            public Flow<double> MatchAbsoluteDifferenceFromOutsideRange(double other, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleAbsoluteDifferenceFromOutsideRange(flow.Value, other, lowerBound, upperBound));
            }

            public Flow<double> RejectAbsoluteDifferenceFromOutsideRange(double other, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleAbsoluteDifferenceFromOutsideRange(flow.Value, other, lowerBound, upperBound));
            }

            public Flow<double> BreakAbsoluteDifferenceFromOutsideRange(double other, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleAbsoluteDifferenceFromOutsideRange(flow.Value, other, lowerBound, upperBound));
            }

            public Flow<double> RequireAbsoluteDifferenceFromOutsideRange(double other, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleAbsoluteDifferenceFromOutsideRange(flow.Value, other, lowerBound, upperBound));
            }

            public Flow<double> EnsureAbsoluteDifferenceFromOutsideRange(double other, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleAbsoluteDifferenceFromOutsideRange(flow.Value, other, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchAbsoluteDifferenceFromOutsideRange(double other, double lowerBound, double upperBound)
            {
                return value.Start().MatchAbsoluteDifferenceFromOutsideRange(other, lowerBound, upperBound);
            }

            public Flow<double> RejectAbsoluteDifferenceFromOutsideRange(double other, double lowerBound, double upperBound)
            {
                return value.Start().RejectAbsoluteDifferenceFromOutsideRange(other, lowerBound, upperBound);
            }

            public Flow<double> BreakAbsoluteDifferenceFromOutsideRange(double other, double lowerBound, double upperBound)
            {
                return value.Start().BreakAbsoluteDifferenceFromOutsideRange(other, lowerBound, upperBound);
            }

            public Flow<double> RequireAbsoluteDifferenceFromOutsideRange(double other, double lowerBound, double upperBound)
            {
                return value.Start().RequireAbsoluteDifferenceFromOutsideRange(other, lowerBound, upperBound);
            }

            public Flow<double> EnsureAbsoluteDifferenceFromOutsideRange(double other, double lowerBound, double upperBound)
            {
                return value.Start().EnsureAbsoluteDifferenceFromOutsideRange(other, lowerBound, upperBound);
            }
        }
    }
}
