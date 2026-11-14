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
            [Description("True when: how far the value is over the limit, zero when within it is below the lower or above the upper bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchExcessOverOutsideRange([Description("The limit.")] double limit,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleExcessOverOutsideRange(flow.Value, limit, lowerBound, upperBound));
            }

            [Description("True when: how far the value is over the limit, zero when within it is below the lower or above the upper bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectExcessOverOutsideRange([Description("The limit.")] double limit,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleExcessOverOutsideRange(flow.Value, limit, lowerBound, upperBound));
            }

            [Description("True when: how far the value is over the limit, zero when within it is below the lower or above the upper bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakExcessOverOutsideRange([Description("The limit.")] double limit,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleExcessOverOutsideRange(flow.Value, limit, lowerBound, upperBound));
            }

            [Description("True when: how far the value is over the limit, zero when within it is below the lower or above the upper bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireExcessOverOutsideRange([Description("The limit.")] double limit,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleExcessOverOutsideRange(flow.Value, limit, lowerBound, upperBound));
            }

            [Description("True when: how far the value is over the limit, zero when within it is below the lower or above the upper bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureExcessOverOutsideRange([Description("The limit.")] double limit,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleExcessOverOutsideRange(flow.Value, limit, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: how far the value is over the limit, zero when within it is below the lower or above the upper bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchExcessOverOutsideRange([Description("The limit.")] double limit,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().MatchExcessOverOutsideRange(limit, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: how far the value is over the limit, zero when within it is below the lower or above the upper bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectExcessOverOutsideRange([Description("The limit.")] double limit,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().RejectExcessOverOutsideRange(limit, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: how far the value is over the limit, zero when within it is below the lower or above the upper bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakExcessOverOutsideRange([Description("The limit.")] double limit,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().BreakExcessOverOutsideRange(limit, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: how far the value is over the limit, zero when within it is below the lower or above the upper bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireExcessOverOutsideRange([Description("The limit.")] double limit,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().RequireExcessOverOutsideRange(limit, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: how far the value is over the limit, zero when within it is below the lower or above the upper bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureExcessOverOutsideRange([Description("The limit.")] double limit,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().EnsureExcessOverOutsideRange(limit, lowerBound, upperBound);
            }
        }
    }
}
