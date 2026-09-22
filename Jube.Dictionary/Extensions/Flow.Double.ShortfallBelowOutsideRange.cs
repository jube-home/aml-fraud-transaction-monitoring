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
            public Flow<double> MatchShortfallBelowOutsideRange(double minimum, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleShortfallBelowOutsideRange(flow.Value, minimum, lowerBound, upperBound));
            }

            public Flow<double> RejectShortfallBelowOutsideRange(double minimum, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleShortfallBelowOutsideRange(flow.Value, minimum, lowerBound, upperBound));
            }

            public Flow<double> BreakShortfallBelowOutsideRange(double minimum, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleShortfallBelowOutsideRange(flow.Value, minimum, lowerBound, upperBound));
            }

            public Flow<double> RequireShortfallBelowOutsideRange(double minimum, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleShortfallBelowOutsideRange(flow.Value, minimum, lowerBound, upperBound));
            }

            public Flow<double> EnsureShortfallBelowOutsideRange(double minimum, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleShortfallBelowOutsideRange(flow.Value, minimum, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchShortfallBelowOutsideRange(double minimum, double lowerBound, double upperBound)
            {
                return value.Start().MatchShortfallBelowOutsideRange(minimum, lowerBound, upperBound);
            }

            public Flow<double> RejectShortfallBelowOutsideRange(double minimum, double lowerBound, double upperBound)
            {
                return value.Start().RejectShortfallBelowOutsideRange(minimum, lowerBound, upperBound);
            }

            public Flow<double> BreakShortfallBelowOutsideRange(double minimum, double lowerBound, double upperBound)
            {
                return value.Start().BreakShortfallBelowOutsideRange(minimum, lowerBound, upperBound);
            }

            public Flow<double> RequireShortfallBelowOutsideRange(double minimum, double lowerBound, double upperBound)
            {
                return value.Start().RequireShortfallBelowOutsideRange(minimum, lowerBound, upperBound);
            }

            public Flow<double> EnsureShortfallBelowOutsideRange(double minimum, double lowerBound, double upperBound)
            {
                return value.Start().EnsureShortfallBelowOutsideRange(minimum, lowerBound, upperBound);
            }
        }
    }
}
