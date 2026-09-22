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
            public Flow<double> MatchModifiedZScoreOutsideRange(double median, double medianAbsoluteDeviation, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleModifiedZScoreOutsideRange(flow.Value, median, medianAbsoluteDeviation, lowerBound, upperBound));
            }

            public Flow<double> RejectModifiedZScoreOutsideRange(double median, double medianAbsoluteDeviation, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleModifiedZScoreOutsideRange(flow.Value, median, medianAbsoluteDeviation, lowerBound, upperBound));
            }

            public Flow<double> BreakModifiedZScoreOutsideRange(double median, double medianAbsoluteDeviation, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleModifiedZScoreOutsideRange(flow.Value, median, medianAbsoluteDeviation, lowerBound, upperBound));
            }

            public Flow<double> RequireModifiedZScoreOutsideRange(double median, double medianAbsoluteDeviation, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleModifiedZScoreOutsideRange(flow.Value, median, medianAbsoluteDeviation, lowerBound, upperBound));
            }

            public Flow<double> EnsureModifiedZScoreOutsideRange(double median, double medianAbsoluteDeviation, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleModifiedZScoreOutsideRange(flow.Value, median, medianAbsoluteDeviation, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchModifiedZScoreOutsideRange(double median, double medianAbsoluteDeviation, double lowerBound, double upperBound)
            {
                return value.Start().MatchModifiedZScoreOutsideRange(median, medianAbsoluteDeviation, lowerBound, upperBound);
            }

            public Flow<double> RejectModifiedZScoreOutsideRange(double median, double medianAbsoluteDeviation, double lowerBound, double upperBound)
            {
                return value.Start().RejectModifiedZScoreOutsideRange(median, medianAbsoluteDeviation, lowerBound, upperBound);
            }

            public Flow<double> BreakModifiedZScoreOutsideRange(double median, double medianAbsoluteDeviation, double lowerBound, double upperBound)
            {
                return value.Start().BreakModifiedZScoreOutsideRange(median, medianAbsoluteDeviation, lowerBound, upperBound);
            }

            public Flow<double> RequireModifiedZScoreOutsideRange(double median, double medianAbsoluteDeviation, double lowerBound, double upperBound)
            {
                return value.Start().RequireModifiedZScoreOutsideRange(median, medianAbsoluteDeviation, lowerBound, upperBound);
            }

            public Flow<double> EnsureModifiedZScoreOutsideRange(double median, double medianAbsoluteDeviation, double lowerBound, double upperBound)
            {
                return value.Start().EnsureModifiedZScoreOutsideRange(median, medianAbsoluteDeviation, lowerBound, upperBound);
            }
        }
    }
}
