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
            public Flow<double> MatchMinMaxNormaliseInRange(double minimum, double maximum, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleMinMaxNormaliseInRange(flow.Value, minimum, maximum, lowerBound, upperBound));
            }

            public Flow<double> RejectMinMaxNormaliseInRange(double minimum, double maximum, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleMinMaxNormaliseInRange(flow.Value, minimum, maximum, lowerBound, upperBound));
            }

            public Flow<double> BreakMinMaxNormaliseInRange(double minimum, double maximum, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleMinMaxNormaliseInRange(flow.Value, minimum, maximum, lowerBound, upperBound));
            }

            public Flow<double> RequireMinMaxNormaliseInRange(double minimum, double maximum, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleMinMaxNormaliseInRange(flow.Value, minimum, maximum, lowerBound, upperBound));
            }

            public Flow<double> EnsureMinMaxNormaliseInRange(double minimum, double maximum, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleMinMaxNormaliseInRange(flow.Value, minimum, maximum, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchMinMaxNormaliseInRange(double minimum, double maximum, double lowerBound, double upperBound)
            {
                return value.Start().MatchMinMaxNormaliseInRange(minimum, maximum, lowerBound, upperBound);
            }

            public Flow<double> RejectMinMaxNormaliseInRange(double minimum, double maximum, double lowerBound, double upperBound)
            {
                return value.Start().RejectMinMaxNormaliseInRange(minimum, maximum, lowerBound, upperBound);
            }

            public Flow<double> BreakMinMaxNormaliseInRange(double minimum, double maximum, double lowerBound, double upperBound)
            {
                return value.Start().BreakMinMaxNormaliseInRange(minimum, maximum, lowerBound, upperBound);
            }

            public Flow<double> RequireMinMaxNormaliseInRange(double minimum, double maximum, double lowerBound, double upperBound)
            {
                return value.Start().RequireMinMaxNormaliseInRange(minimum, maximum, lowerBound, upperBound);
            }

            public Flow<double> EnsureMinMaxNormaliseInRange(double minimum, double maximum, double lowerBound, double upperBound)
            {
                return value.Start().EnsureMinMaxNormaliseInRange(minimum, maximum, lowerBound, upperBound);
            }
        }
    }
}
