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
            public Flow<double> MatchPercentDifferenceFromInRange(double other, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoublePercentDifferenceFromInRange(flow.Value, other, lowerBound, upperBound));
            }

            public Flow<double> RejectPercentDifferenceFromInRange(double other, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoublePercentDifferenceFromInRange(flow.Value, other, lowerBound, upperBound));
            }

            public Flow<double> BreakPercentDifferenceFromInRange(double other, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoublePercentDifferenceFromInRange(flow.Value, other, lowerBound, upperBound));
            }

            public Flow<double> RequirePercentDifferenceFromInRange(double other, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoublePercentDifferenceFromInRange(flow.Value, other, lowerBound, upperBound));
            }

            public Flow<double> EnsurePercentDifferenceFromInRange(double other, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoublePercentDifferenceFromInRange(flow.Value, other, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchPercentDifferenceFromInRange(double other, double lowerBound, double upperBound)
            {
                return value.Start().MatchPercentDifferenceFromInRange(other, lowerBound, upperBound);
            }

            public Flow<double> RejectPercentDifferenceFromInRange(double other, double lowerBound, double upperBound)
            {
                return value.Start().RejectPercentDifferenceFromInRange(other, lowerBound, upperBound);
            }

            public Flow<double> BreakPercentDifferenceFromInRange(double other, double lowerBound, double upperBound)
            {
                return value.Start().BreakPercentDifferenceFromInRange(other, lowerBound, upperBound);
            }

            public Flow<double> RequirePercentDifferenceFromInRange(double other, double lowerBound, double upperBound)
            {
                return value.Start().RequirePercentDifferenceFromInRange(other, lowerBound, upperBound);
            }

            public Flow<double> EnsurePercentDifferenceFromInRange(double other, double lowerBound, double upperBound)
            {
                return value.Start().EnsurePercentDifferenceFromInRange(other, lowerBound, upperBound);
            }
        }
    }
}
