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
            [Description("True when: the share of the value kept after the outflow, negative when more went out than came in is between the bounds, both included. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchRetentionAfterInRange([Description("The amount that went out.")] double outflow,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleRetentionAfterInRange(flow.Value, outflow, lowerBound, upperBound));
            }

            [Description("True when: the share of the value kept after the outflow, negative when more went out than came in is between the bounds, both included. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectRetentionAfterInRange([Description("The amount that went out.")] double outflow,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleRetentionAfterInRange(flow.Value, outflow, lowerBound, upperBound));
            }

            [Description("True when: the share of the value kept after the outflow, negative when more went out than came in is between the bounds, both included. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakRetentionAfterInRange([Description("The amount that went out.")] double outflow,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleRetentionAfterInRange(flow.Value, outflow, lowerBound, upperBound));
            }

            [Description("True when: the share of the value kept after the outflow, negative when more went out than came in is between the bounds, both included. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireRetentionAfterInRange([Description("The amount that went out.")] double outflow,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleRetentionAfterInRange(flow.Value, outflow, lowerBound, upperBound));
            }

            [Description("True when: the share of the value kept after the outflow, negative when more went out than came in is between the bounds, both included. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureRetentionAfterInRange([Description("The amount that went out.")] double outflow,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleRetentionAfterInRange(flow.Value, outflow, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the share of the value kept after the outflow, negative when more went out than came in is between the bounds, both included. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchRetentionAfterInRange([Description("The amount that went out.")] double outflow,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().MatchRetentionAfterInRange(outflow, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the share of the value kept after the outflow, negative when more went out than came in is between the bounds, both included. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectRetentionAfterInRange([Description("The amount that went out.")] double outflow,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().RejectRetentionAfterInRange(outflow, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the share of the value kept after the outflow, negative when more went out than came in is between the bounds, both included. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakRetentionAfterInRange([Description("The amount that went out.")] double outflow,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().BreakRetentionAfterInRange(outflow, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the share of the value kept after the outflow, negative when more went out than came in is between the bounds, both included. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireRetentionAfterInRange([Description("The amount that went out.")] double outflow,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().RequireRetentionAfterInRange(outflow, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the share of the value kept after the outflow, negative when more went out than came in is between the bounds, both included. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureRetentionAfterInRange([Description("The amount that went out.")] double outflow,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().EnsureRetentionAfterInRange(outflow, lowerBound, upperBound);
            }
        }
    }
}
