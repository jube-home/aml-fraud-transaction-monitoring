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
            [Description("True when: one minus the value (an approval ratio from a decline ratio) is above the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchComplementAbove([Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleComplementAbove(flow.Value, bound));
            }

            [Description("True when: one minus the value (an approval ratio from a decline ratio) is above the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectComplementAbove([Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleComplementAbove(flow.Value, bound));
            }

            [Description("True when: one minus the value (an approval ratio from a decline ratio) is above the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakComplementAbove([Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleComplementAbove(flow.Value, bound));
            }

            [Description("True when: one minus the value (an approval ratio from a decline ratio) is above the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireComplementAbove([Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleComplementAbove(flow.Value, bound));
            }

            [Description("True when: one minus the value (an approval ratio from a decline ratio) is above the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureComplementAbove([Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleComplementAbove(flow.Value, bound));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: one minus the value (an approval ratio from a decline ratio) is above the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchComplementAbove([Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().MatchComplementAbove(bound);
            }

            [Description("Begins a pipeline from the value. True when: one minus the value (an approval ratio from a decline ratio) is above the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectComplementAbove([Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RejectComplementAbove(bound);
            }

            [Description("Begins a pipeline from the value. True when: one minus the value (an approval ratio from a decline ratio) is above the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakComplementAbove([Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().BreakComplementAbove(bound);
            }

            [Description("Begins a pipeline from the value. True when: one minus the value (an approval ratio from a decline ratio) is above the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireComplementAbove([Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RequireComplementAbove(bound);
            }

            [Description("Begins a pipeline from the value. True when: one minus the value (an approval ratio from a decline ratio) is above the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureComplementAbove([Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().EnsureComplementAbove(bound);
            }
        }
    }
}
