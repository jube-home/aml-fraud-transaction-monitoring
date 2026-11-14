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
            [Description("True when: the average of the value and the others is above the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchMeanWithAbove([Description("The bound the calculation is compared with.")] double bound,
                [Description("The other numbers.")] params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleMeanWithAbove(flow.Value, bound, others));
            }

            [Description("True when: the average of the value and the others is above the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectMeanWithAbove([Description("The bound the calculation is compared with.")] double bound,
                [Description("The other numbers.")] params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleMeanWithAbove(flow.Value, bound, others));
            }

            [Description("True when: the average of the value and the others is above the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakMeanWithAbove([Description("The bound the calculation is compared with.")] double bound,
                [Description("The other numbers.")] params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleMeanWithAbove(flow.Value, bound, others));
            }

            [Description("True when: the average of the value and the others is above the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireMeanWithAbove([Description("The bound the calculation is compared with.")] double bound,
                [Description("The other numbers.")] params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleMeanWithAbove(flow.Value, bound, others));
            }

            [Description("True when: the average of the value and the others is above the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureMeanWithAbove([Description("The bound the calculation is compared with.")] double bound,
                [Description("The other numbers.")] params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleMeanWithAbove(flow.Value, bound, others));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the average of the value and the others is above the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchMeanWithAbove([Description("The bound the calculation is compared with.")] double bound,
                [Description("The other numbers.")] params double[] others)
            {
                return value.Start().MatchMeanWithAbove(bound, others);
            }

            [Description("Begins a pipeline from the value. True when: the average of the value and the others is above the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectMeanWithAbove([Description("The bound the calculation is compared with.")] double bound,
                [Description("The other numbers.")] params double[] others)
            {
                return value.Start().RejectMeanWithAbove(bound, others);
            }

            [Description("Begins a pipeline from the value. True when: the average of the value and the others is above the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakMeanWithAbove([Description("The bound the calculation is compared with.")] double bound,
                [Description("The other numbers.")] params double[] others)
            {
                return value.Start().BreakMeanWithAbove(bound, others);
            }

            [Description("Begins a pipeline from the value. True when: the average of the value and the others is above the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireMeanWithAbove([Description("The bound the calculation is compared with.")] double bound,
                [Description("The other numbers.")] params double[] others)
            {
                return value.Start().RequireMeanWithAbove(bound, others);
            }

            [Description("Begins a pipeline from the value. True when: the average of the value and the others is above the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureMeanWithAbove([Description("The bound the calculation is compared with.")] double bound,
                [Description("The other numbers.")] params double[] others)
            {
                return value.Start().EnsureMeanWithAbove(bound, others);
            }
        }
    }
}
