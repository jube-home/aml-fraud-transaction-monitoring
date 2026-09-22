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
            public Flow<double> MatchEntropyOfSharesWithAbove(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleEntropyOfSharesWithAbove(flow.Value, bound, others));
            }

            public Flow<double> RejectEntropyOfSharesWithAbove(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleEntropyOfSharesWithAbove(flow.Value, bound, others));
            }

            public Flow<double> BreakEntropyOfSharesWithAbove(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleEntropyOfSharesWithAbove(flow.Value, bound, others));
            }

            public Flow<double> RequireEntropyOfSharesWithAbove(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleEntropyOfSharesWithAbove(flow.Value, bound, others));
            }

            public Flow<double> EnsureEntropyOfSharesWithAbove(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleEntropyOfSharesWithAbove(flow.Value, bound, others));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchEntropyOfSharesWithAbove(double bound, params double[] others)
            {
                return value.Start().MatchEntropyOfSharesWithAbove(bound, others);
            }

            public Flow<double> RejectEntropyOfSharesWithAbove(double bound, params double[] others)
            {
                return value.Start().RejectEntropyOfSharesWithAbove(bound, others);
            }

            public Flow<double> BreakEntropyOfSharesWithAbove(double bound, params double[] others)
            {
                return value.Start().BreakEntropyOfSharesWithAbove(bound, others);
            }

            public Flow<double> RequireEntropyOfSharesWithAbove(double bound, params double[] others)
            {
                return value.Start().RequireEntropyOfSharesWithAbove(bound, others);
            }

            public Flow<double> EnsureEntropyOfSharesWithAbove(double bound, params double[] others)
            {
                return value.Start().EnsureEntropyOfSharesWithAbove(bound, others);
            }
        }
    }
}
