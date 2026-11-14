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
            [Description("True when: the value's share of the value plus the other (a decline ratio is declined over declined plus approved) is above the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchShareOfSumWithAbove([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleShareOfSumWithAbove(flow.Value, other, bound));
            }

            [Description("True when: the value's share of the value plus the other (a decline ratio is declined over declined plus approved) is above the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectShareOfSumWithAbove([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleShareOfSumWithAbove(flow.Value, other, bound));
            }

            [Description("True when: the value's share of the value plus the other (a decline ratio is declined over declined plus approved) is above the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakShareOfSumWithAbove([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleShareOfSumWithAbove(flow.Value, other, bound));
            }

            [Description("True when: the value's share of the value plus the other (a decline ratio is declined over declined plus approved) is above the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireShareOfSumWithAbove([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleShareOfSumWithAbove(flow.Value, other, bound));
            }

            [Description("True when: the value's share of the value plus the other (a decline ratio is declined over declined plus approved) is above the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureShareOfSumWithAbove([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleShareOfSumWithAbove(flow.Value, other, bound));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the value's share of the value plus the other (a decline ratio is declined over declined plus approved) is above the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchShareOfSumWithAbove([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().MatchShareOfSumWithAbove(other, bound);
            }

            [Description("Begins a pipeline from the value. True when: the value's share of the value plus the other (a decline ratio is declined over declined plus approved) is above the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectShareOfSumWithAbove([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RejectShareOfSumWithAbove(other, bound);
            }

            [Description("Begins a pipeline from the value. True when: the value's share of the value plus the other (a decline ratio is declined over declined plus approved) is above the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakShareOfSumWithAbove([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().BreakShareOfSumWithAbove(other, bound);
            }

            [Description("Begins a pipeline from the value. True when: the value's share of the value plus the other (a decline ratio is declined over declined plus approved) is above the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireShareOfSumWithAbove([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RequireShareOfSumWithAbove(other, bound);
            }

            [Description("Begins a pipeline from the value. True when: the value's share of the value plus the other (a decline ratio is declined over declined plus approved) is above the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureShareOfSumWithAbove([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().EnsureShareOfSumWithAbove(other, bound);
            }
        }
    }
}
