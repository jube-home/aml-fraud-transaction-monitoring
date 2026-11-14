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
            [Description("True when: the Shannon entropy in bits of how the total is shared across the value and the others is below the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchEntropyOfSharesWithBelow([Description("The bound the calculation is compared with.")] double bound,
                [Description("The other numbers.")] params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleEntropyOfSharesWithBelow(flow.Value, bound, others));
            }

            [Description("True when: the Shannon entropy in bits of how the total is shared across the value and the others is below the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectEntropyOfSharesWithBelow([Description("The bound the calculation is compared with.")] double bound,
                [Description("The other numbers.")] params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleEntropyOfSharesWithBelow(flow.Value, bound, others));
            }

            [Description("True when: the Shannon entropy in bits of how the total is shared across the value and the others is below the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakEntropyOfSharesWithBelow([Description("The bound the calculation is compared with.")] double bound,
                [Description("The other numbers.")] params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleEntropyOfSharesWithBelow(flow.Value, bound, others));
            }

            [Description("True when: the Shannon entropy in bits of how the total is shared across the value and the others is below the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireEntropyOfSharesWithBelow([Description("The bound the calculation is compared with.")] double bound,
                [Description("The other numbers.")] params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleEntropyOfSharesWithBelow(flow.Value, bound, others));
            }

            [Description("True when: the Shannon entropy in bits of how the total is shared across the value and the others is below the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureEntropyOfSharesWithBelow([Description("The bound the calculation is compared with.")] double bound,
                [Description("The other numbers.")] params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleEntropyOfSharesWithBelow(flow.Value, bound, others));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the Shannon entropy in bits of how the total is shared across the value and the others is below the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchEntropyOfSharesWithBelow([Description("The bound the calculation is compared with.")] double bound,
                [Description("The other numbers.")] params double[] others)
            {
                return value.Start().MatchEntropyOfSharesWithBelow(bound, others);
            }

            [Description("Begins a pipeline from the value. True when: the Shannon entropy in bits of how the total is shared across the value and the others is below the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectEntropyOfSharesWithBelow([Description("The bound the calculation is compared with.")] double bound,
                [Description("The other numbers.")] params double[] others)
            {
                return value.Start().RejectEntropyOfSharesWithBelow(bound, others);
            }

            [Description("Begins a pipeline from the value. True when: the Shannon entropy in bits of how the total is shared across the value and the others is below the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakEntropyOfSharesWithBelow([Description("The bound the calculation is compared with.")] double bound,
                [Description("The other numbers.")] params double[] others)
            {
                return value.Start().BreakEntropyOfSharesWithBelow(bound, others);
            }

            [Description("Begins a pipeline from the value. True when: the Shannon entropy in bits of how the total is shared across the value and the others is below the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireEntropyOfSharesWithBelow([Description("The bound the calculation is compared with.")] double bound,
                [Description("The other numbers.")] params double[] others)
            {
                return value.Start().RequireEntropyOfSharesWithBelow(bound, others);
            }

            [Description("Begins a pipeline from the value. True when: the Shannon entropy in bits of how the total is shared across the value and the others is below the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureEntropyOfSharesWithBelow([Description("The bound the calculation is compared with.")] double bound,
                [Description("The other numbers.")] params double[] others)
            {
                return value.Start().EnsureEntropyOfSharesWithBelow(bound, others);
            }
        }
    }
}
