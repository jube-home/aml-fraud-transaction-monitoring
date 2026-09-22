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
            public Flow<double> MatchPercentOfRangeOutsideRange(double minimum, double maximum, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoublePercentOfRangeOutsideRange(flow.Value, minimum, maximum, lowerBound, upperBound));
            }

            public Flow<double> RejectPercentOfRangeOutsideRange(double minimum, double maximum, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoublePercentOfRangeOutsideRange(flow.Value, minimum, maximum, lowerBound, upperBound));
            }

            public Flow<double> BreakPercentOfRangeOutsideRange(double minimum, double maximum, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoublePercentOfRangeOutsideRange(flow.Value, minimum, maximum, lowerBound, upperBound));
            }

            public Flow<double> RequirePercentOfRangeOutsideRange(double minimum, double maximum, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoublePercentOfRangeOutsideRange(flow.Value, minimum, maximum, lowerBound, upperBound));
            }

            public Flow<double> EnsurePercentOfRangeOutsideRange(double minimum, double maximum, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoublePercentOfRangeOutsideRange(flow.Value, minimum, maximum, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchPercentOfRangeOutsideRange(double minimum, double maximum, double lowerBound, double upperBound)
            {
                return value.Start().MatchPercentOfRangeOutsideRange(minimum, maximum, lowerBound, upperBound);
            }

            public Flow<double> RejectPercentOfRangeOutsideRange(double minimum, double maximum, double lowerBound, double upperBound)
            {
                return value.Start().RejectPercentOfRangeOutsideRange(minimum, maximum, lowerBound, upperBound);
            }

            public Flow<double> BreakPercentOfRangeOutsideRange(double minimum, double maximum, double lowerBound, double upperBound)
            {
                return value.Start().BreakPercentOfRangeOutsideRange(minimum, maximum, lowerBound, upperBound);
            }

            public Flow<double> RequirePercentOfRangeOutsideRange(double minimum, double maximum, double lowerBound, double upperBound)
            {
                return value.Start().RequirePercentOfRangeOutsideRange(minimum, maximum, lowerBound, upperBound);
            }

            public Flow<double> EnsurePercentOfRangeOutsideRange(double minimum, double maximum, double lowerBound, double upperBound)
            {
                return value.Start().EnsurePercentOfRangeOutsideRange(minimum, maximum, lowerBound, upperBound);
            }
        }
    }
}
