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
            [Description("True when: the gap between the value and the threshold in either direction is between the bounds, both included. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchDistanceToThresholdInRange([Description("The threshold the value is tested against.")] double threshold,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleDistanceToThresholdInRange(flow.Value, threshold, lowerBound, upperBound));
            }

            [Description("True when: the gap between the value and the threshold in either direction is between the bounds, both included. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectDistanceToThresholdInRange([Description("The threshold the value is tested against.")] double threshold,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleDistanceToThresholdInRange(flow.Value, threshold, lowerBound, upperBound));
            }

            [Description("True when: the gap between the value and the threshold in either direction is between the bounds, both included. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakDistanceToThresholdInRange([Description("The threshold the value is tested against.")] double threshold,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleDistanceToThresholdInRange(flow.Value, threshold, lowerBound, upperBound));
            }

            [Description("True when: the gap between the value and the threshold in either direction is between the bounds, both included. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireDistanceToThresholdInRange([Description("The threshold the value is tested against.")] double threshold,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleDistanceToThresholdInRange(flow.Value, threshold, lowerBound, upperBound));
            }

            [Description("True when: the gap between the value and the threshold in either direction is between the bounds, both included. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureDistanceToThresholdInRange([Description("The threshold the value is tested against.")] double threshold,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleDistanceToThresholdInRange(flow.Value, threshold, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the gap between the value and the threshold in either direction is between the bounds, both included. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchDistanceToThresholdInRange([Description("The threshold the value is tested against.")] double threshold,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().MatchDistanceToThresholdInRange(threshold, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the gap between the value and the threshold in either direction is between the bounds, both included. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectDistanceToThresholdInRange([Description("The threshold the value is tested against.")] double threshold,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().RejectDistanceToThresholdInRange(threshold, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the gap between the value and the threshold in either direction is between the bounds, both included. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakDistanceToThresholdInRange([Description("The threshold the value is tested against.")] double threshold,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().BreakDistanceToThresholdInRange(threshold, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the gap between the value and the threshold in either direction is between the bounds, both included. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireDistanceToThresholdInRange([Description("The threshold the value is tested against.")] double threshold,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().RequireDistanceToThresholdInRange(threshold, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the gap between the value and the threshold in either direction is between the bounds, both included. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureDistanceToThresholdInRange([Description("The threshold the value is tested against.")] double threshold,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().EnsureDistanceToThresholdInRange(threshold, lowerBound, upperBound);
            }
        }
    }
}
