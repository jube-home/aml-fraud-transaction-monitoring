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
            public Flow<double> MatchZScoreAbove(double mean, double standardDeviation, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleZScoreAbove(flow.Value, mean, standardDeviation, threshold));
            }

            public Flow<double> RejectZScoreAbove(double mean, double standardDeviation, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleZScoreAbove(flow.Value, mean, standardDeviation, threshold));
            }

            public Flow<double> BreakZScoreAbove(double mean, double standardDeviation, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleZScoreAbove(flow.Value, mean, standardDeviation, threshold));
            }

            public Flow<double> RequireZScoreAbove(double mean, double standardDeviation, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleZScoreAbove(flow.Value, mean, standardDeviation, threshold));
            }

            public Flow<double> EnsureZScoreAbove(double mean, double standardDeviation, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleZScoreAbove(flow.Value, mean, standardDeviation, threshold));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchZScoreAbove(double mean, double standardDeviation, double threshold)
            {
                return value.Start().MatchZScoreAbove(mean, standardDeviation, threshold);
            }

            public Flow<double> RejectZScoreAbove(double mean, double standardDeviation, double threshold)
            {
                return value.Start().RejectZScoreAbove(mean, standardDeviation, threshold);
            }

            public Flow<double> BreakZScoreAbove(double mean, double standardDeviation, double threshold)
            {
                return value.Start().BreakZScoreAbove(mean, standardDeviation, threshold);
            }

            public Flow<double> RequireZScoreAbove(double mean, double standardDeviation, double threshold)
            {
                return value.Start().RequireZScoreAbove(mean, standardDeviation, threshold);
            }

            public Flow<double> EnsureZScoreAbove(double mean, double standardDeviation, double threshold)
            {
                return value.Start().EnsureZScoreAbove(mean, standardDeviation, threshold);
            }
        }
    }
}
