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
            public Flow<double> MatchSumWithAbove(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleSumWithAbove(flow.Value, bound, others));
            }

            public Flow<double> RejectSumWithAbove(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleSumWithAbove(flow.Value, bound, others));
            }

            public Flow<double> BreakSumWithAbove(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleSumWithAbove(flow.Value, bound, others));
            }

            public Flow<double> RequireSumWithAbove(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleSumWithAbove(flow.Value, bound, others));
            }

            public Flow<double> EnsureSumWithAbove(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleSumWithAbove(flow.Value, bound, others));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchSumWithAbove(double bound, params double[] others)
            {
                return value.Start().MatchSumWithAbove(bound, others);
            }

            public Flow<double> RejectSumWithAbove(double bound, params double[] others)
            {
                return value.Start().RejectSumWithAbove(bound, others);
            }

            public Flow<double> BreakSumWithAbove(double bound, params double[] others)
            {
                return value.Start().BreakSumWithAbove(bound, others);
            }

            public Flow<double> RequireSumWithAbove(double bound, params double[] others)
            {
                return value.Start().RequireSumWithAbove(bound, others);
            }

            public Flow<double> EnsureSumWithAbove(double bound, params double[] others)
            {
                return value.Start().EnsureSumWithAbove(bound, others);
            }
        }
    }
}
