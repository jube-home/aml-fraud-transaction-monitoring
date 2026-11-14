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
            [Description("True when: the geometric mean of the value and another, for growth factors and multiples is between the bounds, both included. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchGeometricMeanWithInRange([Description("The other number.")] double other,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleGeometricMeanWithInRange(flow.Value, other, lowerBound, upperBound));
            }

            [Description("True when: the geometric mean of the value and another, for growth factors and multiples is between the bounds, both included. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectGeometricMeanWithInRange([Description("The other number.")] double other,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleGeometricMeanWithInRange(flow.Value, other, lowerBound, upperBound));
            }

            [Description("True when: the geometric mean of the value and another, for growth factors and multiples is between the bounds, both included. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakGeometricMeanWithInRange([Description("The other number.")] double other,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleGeometricMeanWithInRange(flow.Value, other, lowerBound, upperBound));
            }

            [Description("True when: the geometric mean of the value and another, for growth factors and multiples is between the bounds, both included. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireGeometricMeanWithInRange([Description("The other number.")] double other,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleGeometricMeanWithInRange(flow.Value, other, lowerBound, upperBound));
            }

            [Description("True when: the geometric mean of the value and another, for growth factors and multiples is between the bounds, both included. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureGeometricMeanWithInRange([Description("The other number.")] double other,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleGeometricMeanWithInRange(flow.Value, other, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the geometric mean of the value and another, for growth factors and multiples is between the bounds, both included. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchGeometricMeanWithInRange([Description("The other number.")] double other,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().MatchGeometricMeanWithInRange(other, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the geometric mean of the value and another, for growth factors and multiples is between the bounds, both included. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectGeometricMeanWithInRange([Description("The other number.")] double other,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().RejectGeometricMeanWithInRange(other, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the geometric mean of the value and another, for growth factors and multiples is between the bounds, both included. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakGeometricMeanWithInRange([Description("The other number.")] double other,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().BreakGeometricMeanWithInRange(other, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the geometric mean of the value and another, for growth factors and multiples is between the bounds, both included. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireGeometricMeanWithInRange([Description("The other number.")] double other,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().RequireGeometricMeanWithInRange(other, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the geometric mean of the value and another, for growth factors and multiples is between the bounds, both included. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureGeometricMeanWithInRange([Description("The other number.")] double other,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().EnsureGeometricMeanWithInRange(other, lowerBound, upperBound);
            }
        }
    }
}
