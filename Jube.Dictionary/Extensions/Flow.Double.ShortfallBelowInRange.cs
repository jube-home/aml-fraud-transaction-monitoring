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
            [Description("True when: how far the value is under the minimum, zero when at or above it is between the bounds, both included. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchShortfallBelowInRange([Description("The lower bound.")] double minimum,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleShortfallBelowInRange(flow.Value, minimum, lowerBound, upperBound));
            }

            [Description("True when: how far the value is under the minimum, zero when at or above it is between the bounds, both included. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectShortfallBelowInRange([Description("The lower bound.")] double minimum,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleShortfallBelowInRange(flow.Value, minimum, lowerBound, upperBound));
            }

            [Description("True when: how far the value is under the minimum, zero when at or above it is between the bounds, both included. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakShortfallBelowInRange([Description("The lower bound.")] double minimum,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleShortfallBelowInRange(flow.Value, minimum, lowerBound, upperBound));
            }

            [Description("True when: how far the value is under the minimum, zero when at or above it is between the bounds, both included. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireShortfallBelowInRange([Description("The lower bound.")] double minimum,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleShortfallBelowInRange(flow.Value, minimum, lowerBound, upperBound));
            }

            [Description("True when: how far the value is under the minimum, zero when at or above it is between the bounds, both included. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureShortfallBelowInRange([Description("The lower bound.")] double minimum,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleShortfallBelowInRange(flow.Value, minimum, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: how far the value is under the minimum, zero when at or above it is between the bounds, both included. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchShortfallBelowInRange([Description("The lower bound.")] double minimum,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().MatchShortfallBelowInRange(minimum, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: how far the value is under the minimum, zero when at or above it is between the bounds, both included. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectShortfallBelowInRange([Description("The lower bound.")] double minimum,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().RejectShortfallBelowInRange(minimum, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: how far the value is under the minimum, zero when at or above it is between the bounds, both included. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakShortfallBelowInRange([Description("The lower bound.")] double minimum,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().BreakShortfallBelowInRange(minimum, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: how far the value is under the minimum, zero when at or above it is between the bounds, both included. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireShortfallBelowInRange([Description("The lower bound.")] double minimum,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().RequireShortfallBelowInRange(minimum, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: how far the value is under the minimum, zero when at or above it is between the bounds, both included. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureShortfallBelowInRange([Description("The lower bound.")] double minimum,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().EnsureShortfallBelowInRange(minimum, lowerBound, upperBound);
            }
        }
    }
}
