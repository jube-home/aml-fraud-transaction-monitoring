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
            public Flow<double> MatchGrowthFactorFromInRange(double previous, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleGrowthFactorFromInRange(flow.Value, previous, lowerBound, upperBound));
            }

            public Flow<double> RejectGrowthFactorFromInRange(double previous, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleGrowthFactorFromInRange(flow.Value, previous, lowerBound, upperBound));
            }

            public Flow<double> BreakGrowthFactorFromInRange(double previous, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleGrowthFactorFromInRange(flow.Value, previous, lowerBound, upperBound));
            }

            public Flow<double> RequireGrowthFactorFromInRange(double previous, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleGrowthFactorFromInRange(flow.Value, previous, lowerBound, upperBound));
            }

            public Flow<double> EnsureGrowthFactorFromInRange(double previous, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleGrowthFactorFromInRange(flow.Value, previous, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchGrowthFactorFromInRange(double previous, double lowerBound, double upperBound)
            {
                return value.Start().MatchGrowthFactorFromInRange(previous, lowerBound, upperBound);
            }

            public Flow<double> RejectGrowthFactorFromInRange(double previous, double lowerBound, double upperBound)
            {
                return value.Start().RejectGrowthFactorFromInRange(previous, lowerBound, upperBound);
            }

            public Flow<double> BreakGrowthFactorFromInRange(double previous, double lowerBound, double upperBound)
            {
                return value.Start().BreakGrowthFactorFromInRange(previous, lowerBound, upperBound);
            }

            public Flow<double> RequireGrowthFactorFromInRange(double previous, double lowerBound, double upperBound)
            {
                return value.Start().RequireGrowthFactorFromInRange(previous, lowerBound, upperBound);
            }

            public Flow<double> EnsureGrowthFactorFromInRange(double previous, double lowerBound, double upperBound)
            {
                return value.Start().EnsureGrowthFactorFromInRange(previous, lowerBound, upperBound);
            }
        }
    }
}
