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
            public Flow<double> MatchRetentionAfterBelow(double outflow, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleRetentionAfterBelow(flow.Value, outflow, bound));
            }

            public Flow<double> RejectRetentionAfterBelow(double outflow, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleRetentionAfterBelow(flow.Value, outflow, bound));
            }

            public Flow<double> BreakRetentionAfterBelow(double outflow, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleRetentionAfterBelow(flow.Value, outflow, bound));
            }

            public Flow<double> RequireRetentionAfterBelow(double outflow, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleRetentionAfterBelow(flow.Value, outflow, bound));
            }

            public Flow<double> EnsureRetentionAfterBelow(double outflow, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleRetentionAfterBelow(flow.Value, outflow, bound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchRetentionAfterBelow(double outflow, double bound)
            {
                return value.Start().MatchRetentionAfterBelow(outflow, bound);
            }

            public Flow<double> RejectRetentionAfterBelow(double outflow, double bound)
            {
                return value.Start().RejectRetentionAfterBelow(outflow, bound);
            }

            public Flow<double> BreakRetentionAfterBelow(double outflow, double bound)
            {
                return value.Start().BreakRetentionAfterBelow(outflow, bound);
            }

            public Flow<double> RequireRetentionAfterBelow(double outflow, double bound)
            {
                return value.Start().RequireRetentionAfterBelow(outflow, bound);
            }

            public Flow<double> EnsureRetentionAfterBelow(double outflow, double bound)
            {
                return value.Start().EnsureRetentionAfterBelow(outflow, bound);
            }
        }
    }
}
