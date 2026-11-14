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
            [Description("True when: a robust z-score from the median and the median absolute deviation, unmoved by outliers is between the bounds, both included. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchModifiedZScoreInRange([Description("The median the value is measured from.")] double median,
                [Description("The median absolute deviation the distance is measured in.")] double medianAbsoluteDeviation,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleModifiedZScoreInRange(flow.Value, median, medianAbsoluteDeviation, lowerBound, upperBound));
            }

            [Description("True when: a robust z-score from the median and the median absolute deviation, unmoved by outliers is between the bounds, both included. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectModifiedZScoreInRange([Description("The median the value is measured from.")] double median,
                [Description("The median absolute deviation the distance is measured in.")] double medianAbsoluteDeviation,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleModifiedZScoreInRange(flow.Value, median, medianAbsoluteDeviation, lowerBound, upperBound));
            }

            [Description("True when: a robust z-score from the median and the median absolute deviation, unmoved by outliers is between the bounds, both included. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakModifiedZScoreInRange([Description("The median the value is measured from.")] double median,
                [Description("The median absolute deviation the distance is measured in.")] double medianAbsoluteDeviation,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleModifiedZScoreInRange(flow.Value, median, medianAbsoluteDeviation, lowerBound, upperBound));
            }

            [Description("True when: a robust z-score from the median and the median absolute deviation, unmoved by outliers is between the bounds, both included. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireModifiedZScoreInRange([Description("The median the value is measured from.")] double median,
                [Description("The median absolute deviation the distance is measured in.")] double medianAbsoluteDeviation,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleModifiedZScoreInRange(flow.Value, median, medianAbsoluteDeviation, lowerBound, upperBound));
            }

            [Description("True when: a robust z-score from the median and the median absolute deviation, unmoved by outliers is between the bounds, both included. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureModifiedZScoreInRange([Description("The median the value is measured from.")] double median,
                [Description("The median absolute deviation the distance is measured in.")] double medianAbsoluteDeviation,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleModifiedZScoreInRange(flow.Value, median, medianAbsoluteDeviation, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: a robust z-score from the median and the median absolute deviation, unmoved by outliers is between the bounds, both included. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchModifiedZScoreInRange([Description("The median the value is measured from.")] double median,
                [Description("The median absolute deviation the distance is measured in.")] double medianAbsoluteDeviation,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().MatchModifiedZScoreInRange(median, medianAbsoluteDeviation, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: a robust z-score from the median and the median absolute deviation, unmoved by outliers is between the bounds, both included. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectModifiedZScoreInRange([Description("The median the value is measured from.")] double median,
                [Description("The median absolute deviation the distance is measured in.")] double medianAbsoluteDeviation,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().RejectModifiedZScoreInRange(median, medianAbsoluteDeviation, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: a robust z-score from the median and the median absolute deviation, unmoved by outliers is between the bounds, both included. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakModifiedZScoreInRange([Description("The median the value is measured from.")] double median,
                [Description("The median absolute deviation the distance is measured in.")] double medianAbsoluteDeviation,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().BreakModifiedZScoreInRange(median, medianAbsoluteDeviation, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: a robust z-score from the median and the median absolute deviation, unmoved by outliers is between the bounds, both included. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireModifiedZScoreInRange([Description("The median the value is measured from.")] double median,
                [Description("The median absolute deviation the distance is measured in.")] double medianAbsoluteDeviation,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().RequireModifiedZScoreInRange(median, medianAbsoluteDeviation, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: a robust z-score from the median and the median absolute deviation, unmoved by outliers is between the bounds, both included. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureModifiedZScoreInRange([Description("The median the value is measured from.")] double median,
                [Description("The median absolute deviation the distance is measured in.")] double medianAbsoluteDeviation,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().EnsureModifiedZScoreInRange(median, medianAbsoluteDeviation, lowerBound, upperBound);
            }
        }
    }
}
