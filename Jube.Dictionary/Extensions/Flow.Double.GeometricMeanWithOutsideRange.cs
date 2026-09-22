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
            public Flow<double> MatchGeometricMeanWithOutsideRange(double other, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleGeometricMeanWithOutsideRange(flow.Value, other, lowerBound, upperBound));
            }

            public Flow<double> RejectGeometricMeanWithOutsideRange(double other, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleGeometricMeanWithOutsideRange(flow.Value, other, lowerBound, upperBound));
            }

            public Flow<double> BreakGeometricMeanWithOutsideRange(double other, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleGeometricMeanWithOutsideRange(flow.Value, other, lowerBound, upperBound));
            }

            public Flow<double> RequireGeometricMeanWithOutsideRange(double other, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleGeometricMeanWithOutsideRange(flow.Value, other, lowerBound, upperBound));
            }

            public Flow<double> EnsureGeometricMeanWithOutsideRange(double other, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleGeometricMeanWithOutsideRange(flow.Value, other, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchGeometricMeanWithOutsideRange(double other, double lowerBound, double upperBound)
            {
                return value.Start().MatchGeometricMeanWithOutsideRange(other, lowerBound, upperBound);
            }

            public Flow<double> RejectGeometricMeanWithOutsideRange(double other, double lowerBound, double upperBound)
            {
                return value.Start().RejectGeometricMeanWithOutsideRange(other, lowerBound, upperBound);
            }

            public Flow<double> BreakGeometricMeanWithOutsideRange(double other, double lowerBound, double upperBound)
            {
                return value.Start().BreakGeometricMeanWithOutsideRange(other, lowerBound, upperBound);
            }

            public Flow<double> RequireGeometricMeanWithOutsideRange(double other, double lowerBound, double upperBound)
            {
                return value.Start().RequireGeometricMeanWithOutsideRange(other, lowerBound, upperBound);
            }

            public Flow<double> EnsureGeometricMeanWithOutsideRange(double other, double lowerBound, double upperBound)
            {
                return value.Start().EnsureGeometricMeanWithOutsideRange(other, lowerBound, upperBound);
            }
        }
    }
}
