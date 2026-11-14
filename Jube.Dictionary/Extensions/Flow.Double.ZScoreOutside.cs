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
            [Description("True when: the absolute z-score is above the threshold, in either direction. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchZScoreOutside([Description("The mean the value is measured from.")] double mean,
                [Description("The standard deviation the distance is measured in.")] double standardDeviation,
                [Description("The z-score the value is tested against.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleZScoreOutside(flow.Value, mean, standardDeviation, threshold));
            }

            [Description("True when: the absolute z-score is above the threshold, in either direction. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectZScoreOutside([Description("The mean the value is measured from.")] double mean,
                [Description("The standard deviation the distance is measured in.")] double standardDeviation,
                [Description("The z-score the value is tested against.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleZScoreOutside(flow.Value, mean, standardDeviation, threshold));
            }

            [Description("True when: the absolute z-score is above the threshold, in either direction. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakZScoreOutside([Description("The mean the value is measured from.")] double mean,
                [Description("The standard deviation the distance is measured in.")] double standardDeviation,
                [Description("The z-score the value is tested against.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleZScoreOutside(flow.Value, mean, standardDeviation, threshold));
            }

            [Description("True when: the absolute z-score is above the threshold, in either direction. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireZScoreOutside([Description("The mean the value is measured from.")] double mean,
                [Description("The standard deviation the distance is measured in.")] double standardDeviation,
                [Description("The z-score the value is tested against.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleZScoreOutside(flow.Value, mean, standardDeviation, threshold));
            }

            [Description("True when: the absolute z-score is above the threshold, in either direction. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureZScoreOutside([Description("The mean the value is measured from.")] double mean,
                [Description("The standard deviation the distance is measured in.")] double standardDeviation,
                [Description("The z-score the value is tested against.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleZScoreOutside(flow.Value, mean, standardDeviation, threshold));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the absolute z-score is above the threshold, in either direction. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchZScoreOutside([Description("The mean the value is measured from.")] double mean,
                [Description("The standard deviation the distance is measured in.")] double standardDeviation,
                [Description("The z-score the value is tested against.")] double threshold)
            {
                return value.Start().MatchZScoreOutside(mean, standardDeviation, threshold);
            }

            [Description("Begins a pipeline from the value. True when: the absolute z-score is above the threshold, in either direction. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectZScoreOutside([Description("The mean the value is measured from.")] double mean,
                [Description("The standard deviation the distance is measured in.")] double standardDeviation,
                [Description("The z-score the value is tested against.")] double threshold)
            {
                return value.Start().RejectZScoreOutside(mean, standardDeviation, threshold);
            }

            [Description("Begins a pipeline from the value. True when: the absolute z-score is above the threshold, in either direction. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakZScoreOutside([Description("The mean the value is measured from.")] double mean,
                [Description("The standard deviation the distance is measured in.")] double standardDeviation,
                [Description("The z-score the value is tested against.")] double threshold)
            {
                return value.Start().BreakZScoreOutside(mean, standardDeviation, threshold);
            }

            [Description("Begins a pipeline from the value. True when: the absolute z-score is above the threshold, in either direction. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireZScoreOutside([Description("The mean the value is measured from.")] double mean,
                [Description("The standard deviation the distance is measured in.")] double standardDeviation,
                [Description("The z-score the value is tested against.")] double threshold)
            {
                return value.Start().RequireZScoreOutside(mean, standardDeviation, threshold);
            }

            [Description("Begins a pipeline from the value. True when: the absolute z-score is above the threshold, in either direction. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureZScoreOutside([Description("The mean the value is measured from.")] double mean,
                [Description("The standard deviation the distance is measured in.")] double standardDeviation,
                [Description("The z-score the value is tested against.")] double threshold)
            {
                return value.Start().EnsureZScoreOutside(mean, standardDeviation, threshold);
            }
        }
    }
}
