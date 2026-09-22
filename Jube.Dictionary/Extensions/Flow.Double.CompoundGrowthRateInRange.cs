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
            public Flow<double> MatchCompoundGrowthRateInRange(double previous, double periods, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleCompoundGrowthRateInRange(flow.Value, previous, periods, lowerBound, upperBound));
            }

            public Flow<double> RejectCompoundGrowthRateInRange(double previous, double periods, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleCompoundGrowthRateInRange(flow.Value, previous, periods, lowerBound, upperBound));
            }

            public Flow<double> BreakCompoundGrowthRateInRange(double previous, double periods, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleCompoundGrowthRateInRange(flow.Value, previous, periods, lowerBound, upperBound));
            }

            public Flow<double> RequireCompoundGrowthRateInRange(double previous, double periods, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleCompoundGrowthRateInRange(flow.Value, previous, periods, lowerBound, upperBound));
            }

            public Flow<double> EnsureCompoundGrowthRateInRange(double previous, double periods, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleCompoundGrowthRateInRange(flow.Value, previous, periods, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchCompoundGrowthRateInRange(double previous, double periods, double lowerBound, double upperBound)
            {
                return value.Start().MatchCompoundGrowthRateInRange(previous, periods, lowerBound, upperBound);
            }

            public Flow<double> RejectCompoundGrowthRateInRange(double previous, double periods, double lowerBound, double upperBound)
            {
                return value.Start().RejectCompoundGrowthRateInRange(previous, periods, lowerBound, upperBound);
            }

            public Flow<double> BreakCompoundGrowthRateInRange(double previous, double periods, double lowerBound, double upperBound)
            {
                return value.Start().BreakCompoundGrowthRateInRange(previous, periods, lowerBound, upperBound);
            }

            public Flow<double> RequireCompoundGrowthRateInRange(double previous, double periods, double lowerBound, double upperBound)
            {
                return value.Start().RequireCompoundGrowthRateInRange(previous, periods, lowerBound, upperBound);
            }

            public Flow<double> EnsureCompoundGrowthRateInRange(double previous, double periods, double lowerBound, double upperBound)
            {
                return value.Start().EnsureCompoundGrowthRateInRange(previous, periods, lowerBound, upperBound);
            }
        }
    }
}
