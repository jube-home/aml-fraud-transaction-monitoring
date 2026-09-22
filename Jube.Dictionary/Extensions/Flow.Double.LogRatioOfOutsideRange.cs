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
            public Flow<double> MatchLogRatioOfOutsideRange(double denominator, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleLogRatioOfOutsideRange(flow.Value, denominator, lowerBound, upperBound));
            }

            public Flow<double> RejectLogRatioOfOutsideRange(double denominator, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleLogRatioOfOutsideRange(flow.Value, denominator, lowerBound, upperBound));
            }

            public Flow<double> BreakLogRatioOfOutsideRange(double denominator, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleLogRatioOfOutsideRange(flow.Value, denominator, lowerBound, upperBound));
            }

            public Flow<double> RequireLogRatioOfOutsideRange(double denominator, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleLogRatioOfOutsideRange(flow.Value, denominator, lowerBound, upperBound));
            }

            public Flow<double> EnsureLogRatioOfOutsideRange(double denominator, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleLogRatioOfOutsideRange(flow.Value, denominator, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchLogRatioOfOutsideRange(double denominator, double lowerBound, double upperBound)
            {
                return value.Start().MatchLogRatioOfOutsideRange(denominator, lowerBound, upperBound);
            }

            public Flow<double> RejectLogRatioOfOutsideRange(double denominator, double lowerBound, double upperBound)
            {
                return value.Start().RejectLogRatioOfOutsideRange(denominator, lowerBound, upperBound);
            }

            public Flow<double> BreakLogRatioOfOutsideRange(double denominator, double lowerBound, double upperBound)
            {
                return value.Start().BreakLogRatioOfOutsideRange(denominator, lowerBound, upperBound);
            }

            public Flow<double> RequireLogRatioOfOutsideRange(double denominator, double lowerBound, double upperBound)
            {
                return value.Start().RequireLogRatioOfOutsideRange(denominator, lowerBound, upperBound);
            }

            public Flow<double> EnsureLogRatioOfOutsideRange(double denominator, double lowerBound, double upperBound)
            {
                return value.Start().EnsureLogRatioOfOutsideRange(denominator, lowerBound, upperBound);
            }
        }
    }
}
