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
            [Description("True when: the value as a multiple of the previous value is below the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchGrowthFactorFromBelow([Description("The earlier value the change is measured from.")] double previous,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleGrowthFactorFromBelow(flow.Value, previous, bound));
            }

            [Description("True when: the value as a multiple of the previous value is below the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectGrowthFactorFromBelow([Description("The earlier value the change is measured from.")] double previous,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleGrowthFactorFromBelow(flow.Value, previous, bound));
            }

            [Description("True when: the value as a multiple of the previous value is below the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakGrowthFactorFromBelow([Description("The earlier value the change is measured from.")] double previous,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleGrowthFactorFromBelow(flow.Value, previous, bound));
            }

            [Description("True when: the value as a multiple of the previous value is below the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireGrowthFactorFromBelow([Description("The earlier value the change is measured from.")] double previous,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleGrowthFactorFromBelow(flow.Value, previous, bound));
            }

            [Description("True when: the value as a multiple of the previous value is below the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureGrowthFactorFromBelow([Description("The earlier value the change is measured from.")] double previous,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleGrowthFactorFromBelow(flow.Value, previous, bound));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the value as a multiple of the previous value is below the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchGrowthFactorFromBelow([Description("The earlier value the change is measured from.")] double previous,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().MatchGrowthFactorFromBelow(previous, bound);
            }

            [Description("Begins a pipeline from the value. True when: the value as a multiple of the previous value is below the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectGrowthFactorFromBelow([Description("The earlier value the change is measured from.")] double previous,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RejectGrowthFactorFromBelow(previous, bound);
            }

            [Description("Begins a pipeline from the value. True when: the value as a multiple of the previous value is below the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakGrowthFactorFromBelow([Description("The earlier value the change is measured from.")] double previous,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().BreakGrowthFactorFromBelow(previous, bound);
            }

            [Description("Begins a pipeline from the value. True when: the value as a multiple of the previous value is below the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireGrowthFactorFromBelow([Description("The earlier value the change is measured from.")] double previous,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RequireGrowthFactorFromBelow(previous, bound);
            }

            [Description("Begins a pipeline from the value. True when: the value as a multiple of the previous value is below the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureGrowthFactorFromBelow([Description("The earlier value the change is measured from.")] double previous,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().EnsureGrowthFactorFromBelow(previous, bound);
            }
        }
    }
}
