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
            public Flow<double> MatchModifiedZScoreInRange(double median, double medianAbsoluteDeviation, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleModifiedZScoreInRange(flow.Value, median, medianAbsoluteDeviation, lowerBound, upperBound));
            }

            public Flow<double> RejectModifiedZScoreInRange(double median, double medianAbsoluteDeviation, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleModifiedZScoreInRange(flow.Value, median, medianAbsoluteDeviation, lowerBound, upperBound));
            }

            public Flow<double> BreakModifiedZScoreInRange(double median, double medianAbsoluteDeviation, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleModifiedZScoreInRange(flow.Value, median, medianAbsoluteDeviation, lowerBound, upperBound));
            }

            public Flow<double> RequireModifiedZScoreInRange(double median, double medianAbsoluteDeviation, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleModifiedZScoreInRange(flow.Value, median, medianAbsoluteDeviation, lowerBound, upperBound));
            }

            public Flow<double> EnsureModifiedZScoreInRange(double median, double medianAbsoluteDeviation, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleModifiedZScoreInRange(flow.Value, median, medianAbsoluteDeviation, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchModifiedZScoreInRange(double median, double medianAbsoluteDeviation, double lowerBound, double upperBound)
            {
                return value.Start().MatchModifiedZScoreInRange(median, medianAbsoluteDeviation, lowerBound, upperBound);
            }

            public Flow<double> RejectModifiedZScoreInRange(double median, double medianAbsoluteDeviation, double lowerBound, double upperBound)
            {
                return value.Start().RejectModifiedZScoreInRange(median, medianAbsoluteDeviation, lowerBound, upperBound);
            }

            public Flow<double> BreakModifiedZScoreInRange(double median, double medianAbsoluteDeviation, double lowerBound, double upperBound)
            {
                return value.Start().BreakModifiedZScoreInRange(median, medianAbsoluteDeviation, lowerBound, upperBound);
            }

            public Flow<double> RequireModifiedZScoreInRange(double median, double medianAbsoluteDeviation, double lowerBound, double upperBound)
            {
                return value.Start().RequireModifiedZScoreInRange(median, medianAbsoluteDeviation, lowerBound, upperBound);
            }

            public Flow<double> EnsureModifiedZScoreInRange(double median, double medianAbsoluteDeviation, double lowerBound, double upperBound)
            {
                return value.Start().EnsureModifiedZScoreInRange(median, medianAbsoluteDeviation, lowerBound, upperBound);
            }
        }
    }
}
