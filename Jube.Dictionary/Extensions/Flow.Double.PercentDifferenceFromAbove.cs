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
            [Description("True when: the gap between the two as a percentage of their average size, the same either way round is above the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchPercentDifferenceFromAbove([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoublePercentDifferenceFromAbove(flow.Value, other, bound));
            }

            [Description("True when: the gap between the two as a percentage of their average size, the same either way round is above the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectPercentDifferenceFromAbove([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoublePercentDifferenceFromAbove(flow.Value, other, bound));
            }

            [Description("True when: the gap between the two as a percentage of their average size, the same either way round is above the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakPercentDifferenceFromAbove([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoublePercentDifferenceFromAbove(flow.Value, other, bound));
            }

            [Description("True when: the gap between the two as a percentage of their average size, the same either way round is above the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequirePercentDifferenceFromAbove([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoublePercentDifferenceFromAbove(flow.Value, other, bound));
            }

            [Description("True when: the gap between the two as a percentage of their average size, the same either way round is above the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsurePercentDifferenceFromAbove([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoublePercentDifferenceFromAbove(flow.Value, other, bound));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the gap between the two as a percentage of their average size, the same either way round is above the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchPercentDifferenceFromAbove([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().MatchPercentDifferenceFromAbove(other, bound);
            }

            [Description("Begins a pipeline from the value. True when: the gap between the two as a percentage of their average size, the same either way round is above the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectPercentDifferenceFromAbove([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RejectPercentDifferenceFromAbove(other, bound);
            }

            [Description("Begins a pipeline from the value. True when: the gap between the two as a percentage of their average size, the same either way round is above the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakPercentDifferenceFromAbove([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().BreakPercentDifferenceFromAbove(other, bound);
            }

            [Description("Begins a pipeline from the value. True when: the gap between the two as a percentage of their average size, the same either way round is above the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequirePercentDifferenceFromAbove([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RequirePercentDifferenceFromAbove(other, bound);
            }

            [Description("Begins a pipeline from the value. True when: the gap between the two as a percentage of their average size, the same either way round is above the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsurePercentDifferenceFromAbove([Description("The other number.")] double other,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().EnsurePercentDifferenceFromAbove(other, bound);
            }
        }
    }
}
