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
            public Flow<double> MatchMinMaxNormaliseOutsideRange(double minimum, double maximum, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleMinMaxNormaliseOutsideRange(flow.Value, minimum, maximum, lowerBound, upperBound));
            }

            public Flow<double> RejectMinMaxNormaliseOutsideRange(double minimum, double maximum, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleMinMaxNormaliseOutsideRange(flow.Value, minimum, maximum, lowerBound, upperBound));
            }

            public Flow<double> BreakMinMaxNormaliseOutsideRange(double minimum, double maximum, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleMinMaxNormaliseOutsideRange(flow.Value, minimum, maximum, lowerBound, upperBound));
            }

            public Flow<double> RequireMinMaxNormaliseOutsideRange(double minimum, double maximum, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleMinMaxNormaliseOutsideRange(flow.Value, minimum, maximum, lowerBound, upperBound));
            }

            public Flow<double> EnsureMinMaxNormaliseOutsideRange(double minimum, double maximum, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleMinMaxNormaliseOutsideRange(flow.Value, minimum, maximum, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchMinMaxNormaliseOutsideRange(double minimum, double maximum, double lowerBound, double upperBound)
            {
                return value.Start().MatchMinMaxNormaliseOutsideRange(minimum, maximum, lowerBound, upperBound);
            }

            public Flow<double> RejectMinMaxNormaliseOutsideRange(double minimum, double maximum, double lowerBound, double upperBound)
            {
                return value.Start().RejectMinMaxNormaliseOutsideRange(minimum, maximum, lowerBound, upperBound);
            }

            public Flow<double> BreakMinMaxNormaliseOutsideRange(double minimum, double maximum, double lowerBound, double upperBound)
            {
                return value.Start().BreakMinMaxNormaliseOutsideRange(minimum, maximum, lowerBound, upperBound);
            }

            public Flow<double> RequireMinMaxNormaliseOutsideRange(double minimum, double maximum, double lowerBound, double upperBound)
            {
                return value.Start().RequireMinMaxNormaliseOutsideRange(minimum, maximum, lowerBound, upperBound);
            }

            public Flow<double> EnsureMinMaxNormaliseOutsideRange(double minimum, double maximum, double lowerBound, double upperBound)
            {
                return value.Start().EnsureMinMaxNormaliseOutsideRange(minimum, maximum, lowerBound, upperBound);
            }
        }
    }
}
