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
            [Description("True when: the size of the gap between the value and the other is below the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchAbsoluteDifferenceFromBelow([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleAbsoluteDifferenceFromBelow(flow.Value, other, bound));
            }

            [Description("True when: the size of the gap between the value and the other is below the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectAbsoluteDifferenceFromBelow([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleAbsoluteDifferenceFromBelow(flow.Value, other, bound));
            }

            [Description("True when: the size of the gap between the value and the other is below the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakAbsoluteDifferenceFromBelow([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleAbsoluteDifferenceFromBelow(flow.Value, other, bound));
            }

            [Description("True when: the size of the gap between the value and the other is below the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireAbsoluteDifferenceFromBelow([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleAbsoluteDifferenceFromBelow(flow.Value, other, bound));
            }

            [Description("True when: the size of the gap between the value and the other is below the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureAbsoluteDifferenceFromBelow([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleAbsoluteDifferenceFromBelow(flow.Value, other, bound));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the size of the gap between the value and the other is below the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchAbsoluteDifferenceFromBelow([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().MatchAbsoluteDifferenceFromBelow(other, bound);
            }

            [Description("Begins a pipeline from the value. True when: the size of the gap between the value and the other is below the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectAbsoluteDifferenceFromBelow([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RejectAbsoluteDifferenceFromBelow(other, bound);
            }

            [Description("Begins a pipeline from the value. True when: the size of the gap between the value and the other is below the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakAbsoluteDifferenceFromBelow([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().BreakAbsoluteDifferenceFromBelow(other, bound);
            }

            [Description("Begins a pipeline from the value. True when: the size of the gap between the value and the other is below the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireAbsoluteDifferenceFromBelow([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RequireAbsoluteDifferenceFromBelow(other, bound);
            }

            [Description("Begins a pipeline from the value. True when: the size of the gap between the value and the other is below the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureAbsoluteDifferenceFromBelow([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().EnsureAbsoluteDifferenceFromBelow(other, bound);
            }
        }
    }
}
