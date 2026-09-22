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
            public Flow<double> MatchRetentionAfterInRange(double outflow, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleRetentionAfterInRange(flow.Value, outflow, lowerBound, upperBound));
            }

            public Flow<double> RejectRetentionAfterInRange(double outflow, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleRetentionAfterInRange(flow.Value, outflow, lowerBound, upperBound));
            }

            public Flow<double> BreakRetentionAfterInRange(double outflow, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleRetentionAfterInRange(flow.Value, outflow, lowerBound, upperBound));
            }

            public Flow<double> RequireRetentionAfterInRange(double outflow, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleRetentionAfterInRange(flow.Value, outflow, lowerBound, upperBound));
            }

            public Flow<double> EnsureRetentionAfterInRange(double outflow, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleRetentionAfterInRange(flow.Value, outflow, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchRetentionAfterInRange(double outflow, double lowerBound, double upperBound)
            {
                return value.Start().MatchRetentionAfterInRange(outflow, lowerBound, upperBound);
            }

            public Flow<double> RejectRetentionAfterInRange(double outflow, double lowerBound, double upperBound)
            {
                return value.Start().RejectRetentionAfterInRange(outflow, lowerBound, upperBound);
            }

            public Flow<double> BreakRetentionAfterInRange(double outflow, double lowerBound, double upperBound)
            {
                return value.Start().BreakRetentionAfterInRange(outflow, lowerBound, upperBound);
            }

            public Flow<double> RequireRetentionAfterInRange(double outflow, double lowerBound, double upperBound)
            {
                return value.Start().RequireRetentionAfterInRange(outflow, lowerBound, upperBound);
            }

            public Flow<double> EnsureRetentionAfterInRange(double outflow, double lowerBound, double upperBound)
            {
                return value.Start().EnsureRetentionAfterInRange(outflow, lowerBound, upperBound);
            }
        }
    }
}
