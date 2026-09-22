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
            public Flow<double> MatchWeightedMeanWithInRange(double weight, double other, double otherWeight, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleWeightedMeanWithInRange(flow.Value, weight, other, otherWeight, lowerBound, upperBound));
            }

            public Flow<double> RejectWeightedMeanWithInRange(double weight, double other, double otherWeight, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleWeightedMeanWithInRange(flow.Value, weight, other, otherWeight, lowerBound, upperBound));
            }

            public Flow<double> BreakWeightedMeanWithInRange(double weight, double other, double otherWeight, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleWeightedMeanWithInRange(flow.Value, weight, other, otherWeight, lowerBound, upperBound));
            }

            public Flow<double> RequireWeightedMeanWithInRange(double weight, double other, double otherWeight, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleWeightedMeanWithInRange(flow.Value, weight, other, otherWeight, lowerBound, upperBound));
            }

            public Flow<double> EnsureWeightedMeanWithInRange(double weight, double other, double otherWeight, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleWeightedMeanWithInRange(flow.Value, weight, other, otherWeight, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchWeightedMeanWithInRange(double weight, double other, double otherWeight, double lowerBound, double upperBound)
            {
                return value.Start().MatchWeightedMeanWithInRange(weight, other, otherWeight, lowerBound, upperBound);
            }

            public Flow<double> RejectWeightedMeanWithInRange(double weight, double other, double otherWeight, double lowerBound, double upperBound)
            {
                return value.Start().RejectWeightedMeanWithInRange(weight, other, otherWeight, lowerBound, upperBound);
            }

            public Flow<double> BreakWeightedMeanWithInRange(double weight, double other, double otherWeight, double lowerBound, double upperBound)
            {
                return value.Start().BreakWeightedMeanWithInRange(weight, other, otherWeight, lowerBound, upperBound);
            }

            public Flow<double> RequireWeightedMeanWithInRange(double weight, double other, double otherWeight, double lowerBound, double upperBound)
            {
                return value.Start().RequireWeightedMeanWithInRange(weight, other, otherWeight, lowerBound, upperBound);
            }

            public Flow<double> EnsureWeightedMeanWithInRange(double weight, double other, double otherWeight, double lowerBound, double upperBound)
            {
                return value.Start().EnsureWeightedMeanWithInRange(weight, other, otherWeight, lowerBound, upperBound);
            }
        }
    }
}
