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
            [Description("True when: the average of the value and the others is below the lower or above the upper bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchMeanWithOutsideRange([Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound,
                [Description("The other numbers.")] params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleMeanWithOutsideRange(flow.Value, lowerBound, upperBound, others));
            }

            [Description("True when: the average of the value and the others is below the lower or above the upper bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectMeanWithOutsideRange([Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound,
                [Description("The other numbers.")] params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleMeanWithOutsideRange(flow.Value, lowerBound, upperBound, others));
            }

            [Description("True when: the average of the value and the others is below the lower or above the upper bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakMeanWithOutsideRange([Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound,
                [Description("The other numbers.")] params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleMeanWithOutsideRange(flow.Value, lowerBound, upperBound, others));
            }

            [Description("True when: the average of the value and the others is below the lower or above the upper bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireMeanWithOutsideRange([Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound,
                [Description("The other numbers.")] params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleMeanWithOutsideRange(flow.Value, lowerBound, upperBound, others));
            }

            [Description("True when: the average of the value and the others is below the lower or above the upper bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureMeanWithOutsideRange([Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound,
                [Description("The other numbers.")] params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleMeanWithOutsideRange(flow.Value, lowerBound, upperBound, others));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the average of the value and the others is below the lower or above the upper bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchMeanWithOutsideRange([Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound,
                [Description("The other numbers.")] params double[] others)
            {
                return value.Start().MatchMeanWithOutsideRange(lowerBound, upperBound, others);
            }

            [Description("Begins a pipeline from the value. True when: the average of the value and the others is below the lower or above the upper bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectMeanWithOutsideRange([Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound,
                [Description("The other numbers.")] params double[] others)
            {
                return value.Start().RejectMeanWithOutsideRange(lowerBound, upperBound, others);
            }

            [Description("Begins a pipeline from the value. True when: the average of the value and the others is below the lower or above the upper bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakMeanWithOutsideRange([Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound,
                [Description("The other numbers.")] params double[] others)
            {
                return value.Start().BreakMeanWithOutsideRange(lowerBound, upperBound, others);
            }

            [Description("Begins a pipeline from the value. True when: the average of the value and the others is below the lower or above the upper bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireMeanWithOutsideRange([Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound,
                [Description("The other numbers.")] params double[] others)
            {
                return value.Start().RequireMeanWithOutsideRange(lowerBound, upperBound, others);
            }

            [Description("Begins a pipeline from the value. True when: the average of the value and the others is below the lower or above the upper bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureMeanWithOutsideRange([Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound,
                [Description("The other numbers.")] params double[] others)
            {
                return value.Start().EnsureMeanWithOutsideRange(lowerBound, upperBound, others);
            }
        }
    }
}
