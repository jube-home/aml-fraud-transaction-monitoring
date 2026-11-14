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
            [Description("True when: one minus the value (an approval ratio from a decline ratio) is below the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchComplementBelow([Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleComplementBelow(flow.Value, bound));
            }

            [Description("True when: one minus the value (an approval ratio from a decline ratio) is below the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectComplementBelow([Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleComplementBelow(flow.Value, bound));
            }

            [Description("True when: one minus the value (an approval ratio from a decline ratio) is below the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakComplementBelow([Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleComplementBelow(flow.Value, bound));
            }

            [Description("True when: one minus the value (an approval ratio from a decline ratio) is below the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireComplementBelow([Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleComplementBelow(flow.Value, bound));
            }

            [Description("True when: one minus the value (an approval ratio from a decline ratio) is below the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureComplementBelow([Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleComplementBelow(flow.Value, bound));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: one minus the value (an approval ratio from a decline ratio) is below the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchComplementBelow([Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().MatchComplementBelow(bound);
            }

            [Description("Begins a pipeline from the value. True when: one minus the value (an approval ratio from a decline ratio) is below the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectComplementBelow([Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RejectComplementBelow(bound);
            }

            [Description("Begins a pipeline from the value. True when: one minus the value (an approval ratio from a decline ratio) is below the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakComplementBelow([Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().BreakComplementBelow(bound);
            }

            [Description("Begins a pipeline from the value. True when: one minus the value (an approval ratio from a decline ratio) is below the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireComplementBelow([Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RequireComplementBelow(bound);
            }

            [Description("Begins a pipeline from the value. True when: one minus the value (an approval ratio from a decline ratio) is below the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureComplementBelow([Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().EnsureComplementBelow(bound);
            }
        }
    }
}
