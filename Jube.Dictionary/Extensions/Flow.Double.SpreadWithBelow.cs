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
            public Flow<double> MatchSpreadWithBelow(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleSpreadWithBelow(flow.Value, bound, others));
            }

            public Flow<double> RejectSpreadWithBelow(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleSpreadWithBelow(flow.Value, bound, others));
            }

            public Flow<double> BreakSpreadWithBelow(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleSpreadWithBelow(flow.Value, bound, others));
            }

            public Flow<double> RequireSpreadWithBelow(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleSpreadWithBelow(flow.Value, bound, others));
            }

            public Flow<double> EnsureSpreadWithBelow(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleSpreadWithBelow(flow.Value, bound, others));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchSpreadWithBelow(double bound, params double[] others)
            {
                return value.Start().MatchSpreadWithBelow(bound, others);
            }

            public Flow<double> RejectSpreadWithBelow(double bound, params double[] others)
            {
                return value.Start().RejectSpreadWithBelow(bound, others);
            }

            public Flow<double> BreakSpreadWithBelow(double bound, params double[] others)
            {
                return value.Start().BreakSpreadWithBelow(bound, others);
            }

            public Flow<double> RequireSpreadWithBelow(double bound, params double[] others)
            {
                return value.Start().RequireSpreadWithBelow(bound, others);
            }

            public Flow<double> EnsureSpreadWithBelow(double bound, params double[] others)
            {
                return value.Start().EnsureSpreadWithBelow(bound, others);
            }
        }
    }
}
