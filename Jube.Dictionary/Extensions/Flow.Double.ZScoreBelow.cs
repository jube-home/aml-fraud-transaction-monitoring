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
            [Description("True when: the z-score is below the threshold. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchZScoreBelow([Description("The mean of the population.")] double mean,
                [Description("The standard deviation of the population.")] double standardDeviation,
                [Description("The z-score the value is tested against.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleZScoreBelow(flow.Value, mean, standardDeviation, threshold));
            }

            [Description("True when: the z-score is below the threshold. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectZScoreBelow([Description("The mean of the population.")] double mean,
                [Description("The standard deviation of the population.")] double standardDeviation,
                [Description("The z-score the value is tested against.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleZScoreBelow(flow.Value, mean, standardDeviation, threshold));
            }

            [Description("True when: the z-score is below the threshold. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakZScoreBelow([Description("The mean of the population.")] double mean,
                [Description("The standard deviation of the population.")] double standardDeviation,
                [Description("The z-score the value is tested against.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleZScoreBelow(flow.Value, mean, standardDeviation, threshold));
            }

            [Description("True when: the z-score is below the threshold. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireZScoreBelow([Description("The mean of the population.")] double mean,
                [Description("The standard deviation of the population.")] double standardDeviation,
                [Description("The z-score the value is tested against.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleZScoreBelow(flow.Value, mean, standardDeviation, threshold));
            }

            [Description("True when: the z-score is below the threshold. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureZScoreBelow([Description("The mean of the population.")] double mean,
                [Description("The standard deviation of the population.")] double standardDeviation,
                [Description("The z-score the value is tested against.")] double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleZScoreBelow(flow.Value, mean, standardDeviation, threshold));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the z-score is below the threshold. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchZScoreBelow([Description("The mean of the population.")] double mean,
                [Description("The standard deviation of the population.")] double standardDeviation,
                [Description("The z-score the value is tested against.")] double threshold)
            {
                return value.Start().MatchZScoreBelow(mean, standardDeviation, threshold);
            }

            [Description("Begins a pipeline from the value. True when: the z-score is below the threshold. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectZScoreBelow([Description("The mean of the population.")] double mean,
                [Description("The standard deviation of the population.")] double standardDeviation,
                [Description("The z-score the value is tested against.")] double threshold)
            {
                return value.Start().RejectZScoreBelow(mean, standardDeviation, threshold);
            }

            [Description("Begins a pipeline from the value. True when: the z-score is below the threshold. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakZScoreBelow([Description("The mean of the population.")] double mean,
                [Description("The standard deviation of the population.")] double standardDeviation,
                [Description("The z-score the value is tested against.")] double threshold)
            {
                return value.Start().BreakZScoreBelow(mean, standardDeviation, threshold);
            }

            [Description("Begins a pipeline from the value. True when: the z-score is below the threshold. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireZScoreBelow([Description("The mean of the population.")] double mean,
                [Description("The standard deviation of the population.")] double standardDeviation,
                [Description("The z-score the value is tested against.")] double threshold)
            {
                return value.Start().RequireZScoreBelow(mean, standardDeviation, threshold);
            }

            [Description("Begins a pipeline from the value. True when: the z-score is below the threshold. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureZScoreBelow([Description("The mean of the population.")] double mean,
                [Description("The standard deviation of the population.")] double standardDeviation,
                [Description("The z-score the value is tested against.")] double threshold)
            {
                return value.Start().EnsureZScoreBelow(mean, standardDeviation, threshold);
            }
        }
    }
}
