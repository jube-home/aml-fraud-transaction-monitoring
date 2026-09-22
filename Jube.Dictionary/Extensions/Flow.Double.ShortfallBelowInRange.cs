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
            public Flow<double> MatchShortfallBelowInRange(double minimum, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleShortfallBelowInRange(flow.Value, minimum, lowerBound, upperBound));
            }

            public Flow<double> RejectShortfallBelowInRange(double minimum, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleShortfallBelowInRange(flow.Value, minimum, lowerBound, upperBound));
            }

            public Flow<double> BreakShortfallBelowInRange(double minimum, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleShortfallBelowInRange(flow.Value, minimum, lowerBound, upperBound));
            }

            public Flow<double> RequireShortfallBelowInRange(double minimum, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleShortfallBelowInRange(flow.Value, minimum, lowerBound, upperBound));
            }

            public Flow<double> EnsureShortfallBelowInRange(double minimum, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleShortfallBelowInRange(flow.Value, minimum, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchShortfallBelowInRange(double minimum, double lowerBound, double upperBound)
            {
                return value.Start().MatchShortfallBelowInRange(minimum, lowerBound, upperBound);
            }

            public Flow<double> RejectShortfallBelowInRange(double minimum, double lowerBound, double upperBound)
            {
                return value.Start().RejectShortfallBelowInRange(minimum, lowerBound, upperBound);
            }

            public Flow<double> BreakShortfallBelowInRange(double minimum, double lowerBound, double upperBound)
            {
                return value.Start().BreakShortfallBelowInRange(minimum, lowerBound, upperBound);
            }

            public Flow<double> RequireShortfallBelowInRange(double minimum, double lowerBound, double upperBound)
            {
                return value.Start().RequireShortfallBelowInRange(minimum, lowerBound, upperBound);
            }

            public Flow<double> EnsureShortfallBelowInRange(double minimum, double lowerBound, double upperBound)
            {
                return value.Start().EnsureShortfallBelowInRange(minimum, lowerBound, upperBound);
            }
        }
    }
}
