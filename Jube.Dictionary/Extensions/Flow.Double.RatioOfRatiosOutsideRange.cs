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
            public Flow<double> MatchRatioOfRatiosOutsideRange(double denominator, double otherNumerator, double otherDenominator, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleRatioOfRatiosOutsideRange(flow.Value, denominator, otherNumerator, otherDenominator, lowerBound, upperBound));
            }

            public Flow<double> RejectRatioOfRatiosOutsideRange(double denominator, double otherNumerator, double otherDenominator, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleRatioOfRatiosOutsideRange(flow.Value, denominator, otherNumerator, otherDenominator, lowerBound, upperBound));
            }

            public Flow<double> BreakRatioOfRatiosOutsideRange(double denominator, double otherNumerator, double otherDenominator, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleRatioOfRatiosOutsideRange(flow.Value, denominator, otherNumerator, otherDenominator, lowerBound, upperBound));
            }

            public Flow<double> RequireRatioOfRatiosOutsideRange(double denominator, double otherNumerator, double otherDenominator, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleRatioOfRatiosOutsideRange(flow.Value, denominator, otherNumerator, otherDenominator, lowerBound, upperBound));
            }

            public Flow<double> EnsureRatioOfRatiosOutsideRange(double denominator, double otherNumerator, double otherDenominator, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleRatioOfRatiosOutsideRange(flow.Value, denominator, otherNumerator, otherDenominator, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchRatioOfRatiosOutsideRange(double denominator, double otherNumerator, double otherDenominator, double lowerBound, double upperBound)
            {
                return value.Start().MatchRatioOfRatiosOutsideRange(denominator, otherNumerator, otherDenominator, lowerBound, upperBound);
            }

            public Flow<double> RejectRatioOfRatiosOutsideRange(double denominator, double otherNumerator, double otherDenominator, double lowerBound, double upperBound)
            {
                return value.Start().RejectRatioOfRatiosOutsideRange(denominator, otherNumerator, otherDenominator, lowerBound, upperBound);
            }

            public Flow<double> BreakRatioOfRatiosOutsideRange(double denominator, double otherNumerator, double otherDenominator, double lowerBound, double upperBound)
            {
                return value.Start().BreakRatioOfRatiosOutsideRange(denominator, otherNumerator, otherDenominator, lowerBound, upperBound);
            }

            public Flow<double> RequireRatioOfRatiosOutsideRange(double denominator, double otherNumerator, double otherDenominator, double lowerBound, double upperBound)
            {
                return value.Start().RequireRatioOfRatiosOutsideRange(denominator, otherNumerator, otherDenominator, lowerBound, upperBound);
            }

            public Flow<double> EnsureRatioOfRatiosOutsideRange(double denominator, double otherNumerator, double otherDenominator, double lowerBound, double upperBound)
            {
                return value.Start().EnsureRatioOfRatiosOutsideRange(denominator, otherNumerator, otherDenominator, lowerBound, upperBound);
            }
        }
    }
}
