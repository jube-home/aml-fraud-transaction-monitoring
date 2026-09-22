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
            public Flow<double> MatchWeightedMeanWithOutsideRange(double weight, double other, double otherWeight, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleWeightedMeanWithOutsideRange(flow.Value, weight, other, otherWeight, lowerBound, upperBound));
            }

            public Flow<double> RejectWeightedMeanWithOutsideRange(double weight, double other, double otherWeight, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleWeightedMeanWithOutsideRange(flow.Value, weight, other, otherWeight, lowerBound, upperBound));
            }

            public Flow<double> BreakWeightedMeanWithOutsideRange(double weight, double other, double otherWeight, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleWeightedMeanWithOutsideRange(flow.Value, weight, other, otherWeight, lowerBound, upperBound));
            }

            public Flow<double> RequireWeightedMeanWithOutsideRange(double weight, double other, double otherWeight, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleWeightedMeanWithOutsideRange(flow.Value, weight, other, otherWeight, lowerBound, upperBound));
            }

            public Flow<double> EnsureWeightedMeanWithOutsideRange(double weight, double other, double otherWeight, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleWeightedMeanWithOutsideRange(flow.Value, weight, other, otherWeight, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchWeightedMeanWithOutsideRange(double weight, double other, double otherWeight, double lowerBound, double upperBound)
            {
                return value.Start().MatchWeightedMeanWithOutsideRange(weight, other, otherWeight, lowerBound, upperBound);
            }

            public Flow<double> RejectWeightedMeanWithOutsideRange(double weight, double other, double otherWeight, double lowerBound, double upperBound)
            {
                return value.Start().RejectWeightedMeanWithOutsideRange(weight, other, otherWeight, lowerBound, upperBound);
            }

            public Flow<double> BreakWeightedMeanWithOutsideRange(double weight, double other, double otherWeight, double lowerBound, double upperBound)
            {
                return value.Start().BreakWeightedMeanWithOutsideRange(weight, other, otherWeight, lowerBound, upperBound);
            }

            public Flow<double> RequireWeightedMeanWithOutsideRange(double weight, double other, double otherWeight, double lowerBound, double upperBound)
            {
                return value.Start().RequireWeightedMeanWithOutsideRange(weight, other, otherWeight, lowerBound, upperBound);
            }

            public Flow<double> EnsureWeightedMeanWithOutsideRange(double weight, double other, double otherWeight, double lowerBound, double upperBound)
            {
                return value.Start().EnsureWeightedMeanWithOutsideRange(weight, other, otherWeight, lowerBound, upperBound);
            }
        }
    }
}
