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
            [Description("True when: the share of the value kept after the outflow, negative when more went out than came in is above the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchRetentionAfterAbove([Description("The amount that went out.")] double outflow,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleRetentionAfterAbove(flow.Value, outflow, bound));
            }

            [Description("True when: the share of the value kept after the outflow, negative when more went out than came in is above the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectRetentionAfterAbove([Description("The amount that went out.")] double outflow,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleRetentionAfterAbove(flow.Value, outflow, bound));
            }

            [Description("True when: the share of the value kept after the outflow, negative when more went out than came in is above the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakRetentionAfterAbove([Description("The amount that went out.")] double outflow,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleRetentionAfterAbove(flow.Value, outflow, bound));
            }

            [Description("True when: the share of the value kept after the outflow, negative when more went out than came in is above the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireRetentionAfterAbove([Description("The amount that went out.")] double outflow,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleRetentionAfterAbove(flow.Value, outflow, bound));
            }

            [Description("True when: the share of the value kept after the outflow, negative when more went out than came in is above the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureRetentionAfterAbove([Description("The amount that went out.")] double outflow,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleRetentionAfterAbove(flow.Value, outflow, bound));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the share of the value kept after the outflow, negative when more went out than came in is above the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchRetentionAfterAbove([Description("The amount that went out.")] double outflow,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().MatchRetentionAfterAbove(outflow, bound);
            }

            [Description("Begins a pipeline from the value. True when: the share of the value kept after the outflow, negative when more went out than came in is above the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectRetentionAfterAbove([Description("The amount that went out.")] double outflow,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RejectRetentionAfterAbove(outflow, bound);
            }

            [Description("Begins a pipeline from the value. True when: the share of the value kept after the outflow, negative when more went out than came in is above the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakRetentionAfterAbove([Description("The amount that went out.")] double outflow,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().BreakRetentionAfterAbove(outflow, bound);
            }

            [Description("Begins a pipeline from the value. True when: the share of the value kept after the outflow, negative when more went out than came in is above the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireRetentionAfterAbove([Description("The amount that went out.")] double outflow,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RequireRetentionAfterAbove(outflow, bound);
            }

            [Description("Begins a pipeline from the value. True when: the share of the value kept after the outflow, negative when more went out than came in is above the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureRetentionAfterAbove([Description("The amount that went out.")] double outflow,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().EnsureRetentionAfterAbove(outflow, bound);
            }
        }
    }
}
