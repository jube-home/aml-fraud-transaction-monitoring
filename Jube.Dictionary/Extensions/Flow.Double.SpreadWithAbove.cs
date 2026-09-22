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
            public Flow<double> MatchSpreadWithAbove(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleSpreadWithAbove(flow.Value, bound, others));
            }

            public Flow<double> RejectSpreadWithAbove(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleSpreadWithAbove(flow.Value, bound, others));
            }

            public Flow<double> BreakSpreadWithAbove(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleSpreadWithAbove(flow.Value, bound, others));
            }

            public Flow<double> RequireSpreadWithAbove(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleSpreadWithAbove(flow.Value, bound, others));
            }

            public Flow<double> EnsureSpreadWithAbove(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleSpreadWithAbove(flow.Value, bound, others));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchSpreadWithAbove(double bound, params double[] others)
            {
                return value.Start().MatchSpreadWithAbove(bound, others);
            }

            public Flow<double> RejectSpreadWithAbove(double bound, params double[] others)
            {
                return value.Start().RejectSpreadWithAbove(bound, others);
            }

            public Flow<double> BreakSpreadWithAbove(double bound, params double[] others)
            {
                return value.Start().BreakSpreadWithAbove(bound, others);
            }

            public Flow<double> RequireSpreadWithAbove(double bound, params double[] others)
            {
                return value.Start().RequireSpreadWithAbove(bound, others);
            }

            public Flow<double> EnsureSpreadWithAbove(double bound, params double[] others)
            {
                return value.Start().EnsureSpreadWithAbove(bound, others);
            }
        }
    }
}
