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
            [Description("True when: the value plus the others is between the bounds, both included. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchSumWithInRange([Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound,
                [Description("The other numbers.")] params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleSumWithInRange(flow.Value, lowerBound, upperBound, others));
            }

            [Description("True when: the value plus the others is between the bounds, both included. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectSumWithInRange([Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound,
                [Description("The other numbers.")] params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleSumWithInRange(flow.Value, lowerBound, upperBound, others));
            }

            [Description("True when: the value plus the others is between the bounds, both included. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakSumWithInRange([Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound,
                [Description("The other numbers.")] params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleSumWithInRange(flow.Value, lowerBound, upperBound, others));
            }

            [Description("True when: the value plus the others is between the bounds, both included. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireSumWithInRange([Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound,
                [Description("The other numbers.")] params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleSumWithInRange(flow.Value, lowerBound, upperBound, others));
            }

            [Description("True when: the value plus the others is between the bounds, both included. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureSumWithInRange([Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound,
                [Description("The other numbers.")] params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleSumWithInRange(flow.Value, lowerBound, upperBound, others));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the value plus the others is between the bounds, both included. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchSumWithInRange([Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound,
                [Description("The other numbers.")] params double[] others)
            {
                return value.Start().MatchSumWithInRange(lowerBound, upperBound, others);
            }

            [Description("Begins a pipeline from the value. True when: the value plus the others is between the bounds, both included. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectSumWithInRange([Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound,
                [Description("The other numbers.")] params double[] others)
            {
                return value.Start().RejectSumWithInRange(lowerBound, upperBound, others);
            }

            [Description("Begins a pipeline from the value. True when: the value plus the others is between the bounds, both included. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakSumWithInRange([Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound,
                [Description("The other numbers.")] params double[] others)
            {
                return value.Start().BreakSumWithInRange(lowerBound, upperBound, others);
            }

            [Description("Begins a pipeline from the value. True when: the value plus the others is between the bounds, both included. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireSumWithInRange([Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound,
                [Description("The other numbers.")] params double[] others)
            {
                return value.Start().RequireSumWithInRange(lowerBound, upperBound, others);
            }

            [Description("Begins a pipeline from the value. True when: the value plus the others is between the bounds, both included. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureSumWithInRange([Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound,
                [Description("The other numbers.")] params double[] others)
            {
                return value.Start().EnsureSumWithInRange(lowerBound, upperBound, others);
            }
        }
    }
}
