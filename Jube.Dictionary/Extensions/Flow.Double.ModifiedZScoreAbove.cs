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
            [Description("True when: a robust z-score from the median and the median absolute deviation, unmoved by outliers is above the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchModifiedZScoreAbove([Description("The median the value is measured from.")] double median,
                [Description("The median absolute deviation the distance is measured in.")] double medianAbsoluteDeviation,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleModifiedZScoreAbove(flow.Value, median, medianAbsoluteDeviation, bound));
            }

            [Description("True when: a robust z-score from the median and the median absolute deviation, unmoved by outliers is above the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectModifiedZScoreAbove([Description("The median the value is measured from.")] double median,
                [Description("The median absolute deviation the distance is measured in.")] double medianAbsoluteDeviation,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleModifiedZScoreAbove(flow.Value, median, medianAbsoluteDeviation, bound));
            }

            [Description("True when: a robust z-score from the median and the median absolute deviation, unmoved by outliers is above the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakModifiedZScoreAbove([Description("The median the value is measured from.")] double median,
                [Description("The median absolute deviation the distance is measured in.")] double medianAbsoluteDeviation,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleModifiedZScoreAbove(flow.Value, median, medianAbsoluteDeviation, bound));
            }

            [Description("True when: a robust z-score from the median and the median absolute deviation, unmoved by outliers is above the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireModifiedZScoreAbove([Description("The median the value is measured from.")] double median,
                [Description("The median absolute deviation the distance is measured in.")] double medianAbsoluteDeviation,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleModifiedZScoreAbove(flow.Value, median, medianAbsoluteDeviation, bound));
            }

            [Description("True when: a robust z-score from the median and the median absolute deviation, unmoved by outliers is above the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureModifiedZScoreAbove([Description("The median the value is measured from.")] double median,
                [Description("The median absolute deviation the distance is measured in.")] double medianAbsoluteDeviation,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleModifiedZScoreAbove(flow.Value, median, medianAbsoluteDeviation, bound));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: a robust z-score from the median and the median absolute deviation, unmoved by outliers is above the bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchModifiedZScoreAbove([Description("The median the value is measured from.")] double median,
                [Description("The median absolute deviation the distance is measured in.")] double medianAbsoluteDeviation,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().MatchModifiedZScoreAbove(median, medianAbsoluteDeviation, bound);
            }

            [Description("Begins a pipeline from the value. True when: a robust z-score from the median and the median absolute deviation, unmoved by outliers is above the bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectModifiedZScoreAbove([Description("The median the value is measured from.")] double median,
                [Description("The median absolute deviation the distance is measured in.")] double medianAbsoluteDeviation,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RejectModifiedZScoreAbove(median, medianAbsoluteDeviation, bound);
            }

            [Description("Begins a pipeline from the value. True when: a robust z-score from the median and the median absolute deviation, unmoved by outliers is above the bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakModifiedZScoreAbove([Description("The median the value is measured from.")] double median,
                [Description("The median absolute deviation the distance is measured in.")] double medianAbsoluteDeviation,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().BreakModifiedZScoreAbove(median, medianAbsoluteDeviation, bound);
            }

            [Description("Begins a pipeline from the value. True when: a robust z-score from the median and the median absolute deviation, unmoved by outliers is above the bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireModifiedZScoreAbove([Description("The median the value is measured from.")] double median,
                [Description("The median absolute deviation the distance is measured in.")] double medianAbsoluteDeviation,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().RequireModifiedZScoreAbove(median, medianAbsoluteDeviation, bound);
            }

            [Description("Begins a pipeline from the value. True when: a robust z-score from the median and the median absolute deviation, unmoved by outliers is above the bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureModifiedZScoreAbove([Description("The median the value is measured from.")] double median,
                [Description("The median absolute deviation the distance is measured in.")] double medianAbsoluteDeviation,
                [Description("The bound the calculation is compared with.")] double bound)
            {
                return value.Start().EnsureModifiedZScoreAbove(median, medianAbsoluteDeviation, bound);
            }
        }
    }
}
