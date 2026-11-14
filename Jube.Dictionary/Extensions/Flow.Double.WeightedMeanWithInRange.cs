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
            [Description("True when: the weighted average of the value and another value is between the bounds, both included. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchWeightedMeanWithInRange([Description("The weight of the value.")] double weight,
                [Description("The other number.")] double other,
                [Description("The weight of the other value.")] double otherWeight,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleWeightedMeanWithInRange(flow.Value, weight, other, otherWeight, lowerBound, upperBound));
            }

            [Description("True when: the weighted average of the value and another value is between the bounds, both included. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectWeightedMeanWithInRange([Description("The weight of the value.")] double weight,
                [Description("The other number.")] double other,
                [Description("The weight of the other value.")] double otherWeight,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleWeightedMeanWithInRange(flow.Value, weight, other, otherWeight, lowerBound, upperBound));
            }

            [Description("True when: the weighted average of the value and another value is between the bounds, both included. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakWeightedMeanWithInRange([Description("The weight of the value.")] double weight,
                [Description("The other number.")] double other,
                [Description("The weight of the other value.")] double otherWeight,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleWeightedMeanWithInRange(flow.Value, weight, other, otherWeight, lowerBound, upperBound));
            }

            [Description("True when: the weighted average of the value and another value is between the bounds, both included. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireWeightedMeanWithInRange([Description("The weight of the value.")] double weight,
                [Description("The other number.")] double other,
                [Description("The weight of the other value.")] double otherWeight,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleWeightedMeanWithInRange(flow.Value, weight, other, otherWeight, lowerBound, upperBound));
            }

            [Description("True when: the weighted average of the value and another value is between the bounds, both included. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureWeightedMeanWithInRange([Description("The weight of the value.")] double weight,
                [Description("The other number.")] double other,
                [Description("The weight of the other value.")] double otherWeight,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleWeightedMeanWithInRange(flow.Value, weight, other, otherWeight, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the weighted average of the value and another value is between the bounds, both included. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchWeightedMeanWithInRange([Description("The weight of the value.")] double weight,
                [Description("The other number.")] double other,
                [Description("The weight of the other value.")] double otherWeight,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().MatchWeightedMeanWithInRange(weight, other, otherWeight, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the weighted average of the value and another value is between the bounds, both included. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectWeightedMeanWithInRange([Description("The weight of the value.")] double weight,
                [Description("The other number.")] double other,
                [Description("The weight of the other value.")] double otherWeight,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().RejectWeightedMeanWithInRange(weight, other, otherWeight, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the weighted average of the value and another value is between the bounds, both included. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakWeightedMeanWithInRange([Description("The weight of the value.")] double weight,
                [Description("The other number.")] double other,
                [Description("The weight of the other value.")] double otherWeight,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().BreakWeightedMeanWithInRange(weight, other, otherWeight, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the weighted average of the value and another value is between the bounds, both included. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireWeightedMeanWithInRange([Description("The weight of the value.")] double weight,
                [Description("The other number.")] double other,
                [Description("The weight of the other value.")] double otherWeight,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().RequireWeightedMeanWithInRange(weight, other, otherWeight, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the weighted average of the value and another value is between the bounds, both included. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureWeightedMeanWithInRange([Description("The weight of the value.")] double weight,
                [Description("The other number.")] double other,
                [Description("The weight of the other value.")] double otherWeight,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().EnsureWeightedMeanWithInRange(weight, other, otherWeight, lowerBound, upperBound);
            }
        }
    }
}
