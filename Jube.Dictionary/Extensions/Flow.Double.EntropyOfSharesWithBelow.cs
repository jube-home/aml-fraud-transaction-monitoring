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
            public Flow<double> MatchEntropyOfSharesWithBelow(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleEntropyOfSharesWithBelow(flow.Value, bound, others));
            }

            public Flow<double> RejectEntropyOfSharesWithBelow(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleEntropyOfSharesWithBelow(flow.Value, bound, others));
            }

            public Flow<double> BreakEntropyOfSharesWithBelow(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleEntropyOfSharesWithBelow(flow.Value, bound, others));
            }

            public Flow<double> RequireEntropyOfSharesWithBelow(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleEntropyOfSharesWithBelow(flow.Value, bound, others));
            }

            public Flow<double> EnsureEntropyOfSharesWithBelow(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleEntropyOfSharesWithBelow(flow.Value, bound, others));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchEntropyOfSharesWithBelow(double bound, params double[] others)
            {
                return value.Start().MatchEntropyOfSharesWithBelow(bound, others);
            }

            public Flow<double> RejectEntropyOfSharesWithBelow(double bound, params double[] others)
            {
                return value.Start().RejectEntropyOfSharesWithBelow(bound, others);
            }

            public Flow<double> BreakEntropyOfSharesWithBelow(double bound, params double[] others)
            {
                return value.Start().BreakEntropyOfSharesWithBelow(bound, others);
            }

            public Flow<double> RequireEntropyOfSharesWithBelow(double bound, params double[] others)
            {
                return value.Start().RequireEntropyOfSharesWithBelow(bound, others);
            }

            public Flow<double> EnsureEntropyOfSharesWithBelow(double bound, params double[] others)
            {
                return value.Start().EnsureEntropyOfSharesWithBelow(bound, others);
            }
        }
    }
}
