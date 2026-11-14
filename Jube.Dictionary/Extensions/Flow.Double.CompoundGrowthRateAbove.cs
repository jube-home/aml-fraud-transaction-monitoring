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
            [Description("True when: the steady growth rate per period that takes the previous value to this one is above the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchCompoundGrowthRateAbove([Description("The earlier value the change is measured from.")] double previous,
                [Description("The number of periods between the previous value and this one.")] double periods,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleCompoundGrowthRateAbove(flow.Value, previous, periods, bound));
            }

            [Description("True when: the steady growth rate per period that takes the previous value to this one is above the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectCompoundGrowthRateAbove([Description("The earlier value the change is measured from.")] double previous,
                [Description("The number of periods between the previous value and this one.")] double periods,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleCompoundGrowthRateAbove(flow.Value, previous, periods, bound));
            }

            [Description("True when: the steady growth rate per period that takes the previous value to this one is above the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakCompoundGrowthRateAbove([Description("The earlier value the change is measured from.")] double previous,
                [Description("The number of periods between the previous value and this one.")] double periods,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleCompoundGrowthRateAbove(flow.Value, previous, periods, bound));
            }

            [Description("True when: the steady growth rate per period that takes the previous value to this one is above the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireCompoundGrowthRateAbove([Description("The earlier value the change is measured from.")] double previous,
                [Description("The number of periods between the previous value and this one.")] double periods,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleCompoundGrowthRateAbove(flow.Value, previous, periods, bound));
            }

            [Description("True when: the steady growth rate per period that takes the previous value to this one is above the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureCompoundGrowthRateAbove([Description("The earlier value the change is measured from.")] double previous,
                [Description("The number of periods between the previous value and this one.")] double periods,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleCompoundGrowthRateAbove(flow.Value, previous, periods, bound));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the steady growth rate per period that takes the previous value to this one is above the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchCompoundGrowthRateAbove([Description("The earlier value the change is measured from.")] double previous,
                [Description("The number of periods between the previous value and this one.")] double periods,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().MatchCompoundGrowthRateAbove(previous, periods, bound);
            }

            [Description("Begins a pipeline from the value. True when: the steady growth rate per period that takes the previous value to this one is above the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectCompoundGrowthRateAbove([Description("The earlier value the change is measured from.")] double previous,
                [Description("The number of periods between the previous value and this one.")] double periods,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RejectCompoundGrowthRateAbove(previous, periods, bound);
            }

            [Description("Begins a pipeline from the value. True when: the steady growth rate per period that takes the previous value to this one is above the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakCompoundGrowthRateAbove([Description("The earlier value the change is measured from.")] double previous,
                [Description("The number of periods between the previous value and this one.")] double periods,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().BreakCompoundGrowthRateAbove(previous, periods, bound);
            }

            [Description("Begins a pipeline from the value. True when: the steady growth rate per period that takes the previous value to this one is above the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireCompoundGrowthRateAbove([Description("The earlier value the change is measured from.")] double previous,
                [Description("The number of periods between the previous value and this one.")] double periods,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RequireCompoundGrowthRateAbove(previous, periods, bound);
            }

            [Description("Begins a pipeline from the value. True when: the steady growth rate per period that takes the previous value to this one is above the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureCompoundGrowthRateAbove([Description("The earlier value the change is measured from.")] double previous,
                [Description("The number of periods between the previous value and this one.")] double periods,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().EnsureCompoundGrowthRateAbove(previous, periods, bound);
            }
        }
    }
}
