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
            public Flow<double> MatchZScoreBelow(double mean, double standardDeviation, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleZScoreBelow(flow.Value, mean, standardDeviation, threshold));
            }

            public Flow<double> RejectZScoreBelow(double mean, double standardDeviation, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleZScoreBelow(flow.Value, mean, standardDeviation, threshold));
            }

            public Flow<double> BreakZScoreBelow(double mean, double standardDeviation, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleZScoreBelow(flow.Value, mean, standardDeviation, threshold));
            }

            public Flow<double> RequireZScoreBelow(double mean, double standardDeviation, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleZScoreBelow(flow.Value, mean, standardDeviation, threshold));
            }

            public Flow<double> EnsureZScoreBelow(double mean, double standardDeviation, double threshold)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleZScoreBelow(flow.Value, mean, standardDeviation, threshold));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchZScoreBelow(double mean, double standardDeviation, double threshold)
            {
                return value.Start().MatchZScoreBelow(mean, standardDeviation, threshold);
            }

            public Flow<double> RejectZScoreBelow(double mean, double standardDeviation, double threshold)
            {
                return value.Start().RejectZScoreBelow(mean, standardDeviation, threshold);
            }

            public Flow<double> BreakZScoreBelow(double mean, double standardDeviation, double threshold)
            {
                return value.Start().BreakZScoreBelow(mean, standardDeviation, threshold);
            }

            public Flow<double> RequireZScoreBelow(double mean, double standardDeviation, double threshold)
            {
                return value.Start().RequireZScoreBelow(mean, standardDeviation, threshold);
            }

            public Flow<double> EnsureZScoreBelow(double mean, double standardDeviation, double threshold)
            {
                return value.Start().EnsureZScoreBelow(mean, standardDeviation, threshold);
            }
        }
    }
}
