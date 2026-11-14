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
            [Description("True when: the steady growth rate per period that takes the previous value to this one is below the lower or above the upper bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchCompoundGrowthRateOutsideRange([Description("The earlier value the change is measured from.")] double previous,
                [Description("The number of periods between the previous value and this one.")] double periods,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleCompoundGrowthRateOutsideRange(flow.Value, previous, periods, lowerBound, upperBound));
            }

            [Description("True when: the steady growth rate per period that takes the previous value to this one is below the lower or above the upper bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectCompoundGrowthRateOutsideRange([Description("The earlier value the change is measured from.")] double previous,
                [Description("The number of periods between the previous value and this one.")] double periods,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleCompoundGrowthRateOutsideRange(flow.Value, previous, periods, lowerBound, upperBound));
            }

            [Description("True when: the steady growth rate per period that takes the previous value to this one is below the lower or above the upper bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakCompoundGrowthRateOutsideRange([Description("The earlier value the change is measured from.")] double previous,
                [Description("The number of periods between the previous value and this one.")] double periods,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleCompoundGrowthRateOutsideRange(flow.Value, previous, periods, lowerBound, upperBound));
            }

            [Description("True when: the steady growth rate per period that takes the previous value to this one is below the lower or above the upper bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireCompoundGrowthRateOutsideRange([Description("The earlier value the change is measured from.")] double previous,
                [Description("The number of periods between the previous value and this one.")] double periods,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleCompoundGrowthRateOutsideRange(flow.Value, previous, periods, lowerBound, upperBound));
            }

            [Description("True when: the steady growth rate per period that takes the previous value to this one is below the lower or above the upper bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureCompoundGrowthRateOutsideRange([Description("The earlier value the change is measured from.")] double previous,
                [Description("The number of periods between the previous value and this one.")] double periods,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleCompoundGrowthRateOutsideRange(flow.Value, previous, periods, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the steady growth rate per period that takes the previous value to this one is below the lower or above the upper bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchCompoundGrowthRateOutsideRange([Description("The earlier value the change is measured from.")] double previous,
                [Description("The number of periods between the previous value and this one.")] double periods,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().MatchCompoundGrowthRateOutsideRange(previous, periods, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the steady growth rate per period that takes the previous value to this one is below the lower or above the upper bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectCompoundGrowthRateOutsideRange([Description("The earlier value the change is measured from.")] double previous,
                [Description("The number of periods between the previous value and this one.")] double periods,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().RejectCompoundGrowthRateOutsideRange(previous, periods, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the steady growth rate per period that takes the previous value to this one is below the lower or above the upper bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakCompoundGrowthRateOutsideRange([Description("The earlier value the change is measured from.")] double previous,
                [Description("The number of periods between the previous value and this one.")] double periods,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().BreakCompoundGrowthRateOutsideRange(previous, periods, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the steady growth rate per period that takes the previous value to this one is below the lower or above the upper bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireCompoundGrowthRateOutsideRange([Description("The earlier value the change is measured from.")] double previous,
                [Description("The number of periods between the previous value and this one.")] double periods,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().RequireCompoundGrowthRateOutsideRange(previous, periods, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the steady growth rate per period that takes the previous value to this one is below the lower or above the upper bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureCompoundGrowthRateOutsideRange([Description("The earlier value the change is measured from.")] double previous,
                [Description("The number of periods between the previous value and this one.")] double periods,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().EnsureCompoundGrowthRateOutsideRange(previous, periods, lowerBound, upperBound);
            }
        }
    }
}
