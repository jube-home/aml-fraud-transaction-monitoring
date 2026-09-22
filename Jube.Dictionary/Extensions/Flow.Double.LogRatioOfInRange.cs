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
            public Flow<double> MatchLogRatioOfInRange(double denominator, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleLogRatioOfInRange(flow.Value, denominator, lowerBound, upperBound));
            }

            public Flow<double> RejectLogRatioOfInRange(double denominator, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleLogRatioOfInRange(flow.Value, denominator, lowerBound, upperBound));
            }

            public Flow<double> BreakLogRatioOfInRange(double denominator, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleLogRatioOfInRange(flow.Value, denominator, lowerBound, upperBound));
            }

            public Flow<double> RequireLogRatioOfInRange(double denominator, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleLogRatioOfInRange(flow.Value, denominator, lowerBound, upperBound));
            }

            public Flow<double> EnsureLogRatioOfInRange(double denominator, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleLogRatioOfInRange(flow.Value, denominator, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchLogRatioOfInRange(double denominator, double lowerBound, double upperBound)
            {
                return value.Start().MatchLogRatioOfInRange(denominator, lowerBound, upperBound);
            }

            public Flow<double> RejectLogRatioOfInRange(double denominator, double lowerBound, double upperBound)
            {
                return value.Start().RejectLogRatioOfInRange(denominator, lowerBound, upperBound);
            }

            public Flow<double> BreakLogRatioOfInRange(double denominator, double lowerBound, double upperBound)
            {
                return value.Start().BreakLogRatioOfInRange(denominator, lowerBound, upperBound);
            }

            public Flow<double> RequireLogRatioOfInRange(double denominator, double lowerBound, double upperBound)
            {
                return value.Start().RequireLogRatioOfInRange(denominator, lowerBound, upperBound);
            }

            public Flow<double> EnsureLogRatioOfInRange(double denominator, double lowerBound, double upperBound)
            {
                return value.Start().EnsureLogRatioOfInRange(denominator, lowerBound, upperBound);
            }
        }
    }
}
