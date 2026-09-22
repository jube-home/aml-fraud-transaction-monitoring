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
            public Flow<double> MatchModifiedZScoreBelow(double median, double medianAbsoluteDeviation, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleModifiedZScoreBelow(flow.Value, median, medianAbsoluteDeviation, bound));
            }

            public Flow<double> RejectModifiedZScoreBelow(double median, double medianAbsoluteDeviation, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleModifiedZScoreBelow(flow.Value, median, medianAbsoluteDeviation, bound));
            }

            public Flow<double> BreakModifiedZScoreBelow(double median, double medianAbsoluteDeviation, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleModifiedZScoreBelow(flow.Value, median, medianAbsoluteDeviation, bound));
            }

            public Flow<double> RequireModifiedZScoreBelow(double median, double medianAbsoluteDeviation, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleModifiedZScoreBelow(flow.Value, median, medianAbsoluteDeviation, bound));
            }

            public Flow<double> EnsureModifiedZScoreBelow(double median, double medianAbsoluteDeviation, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleModifiedZScoreBelow(flow.Value, median, medianAbsoluteDeviation, bound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchModifiedZScoreBelow(double median, double medianAbsoluteDeviation, double bound)
            {
                return value.Start().MatchModifiedZScoreBelow(median, medianAbsoluteDeviation, bound);
            }

            public Flow<double> RejectModifiedZScoreBelow(double median, double medianAbsoluteDeviation, double bound)
            {
                return value.Start().RejectModifiedZScoreBelow(median, medianAbsoluteDeviation, bound);
            }

            public Flow<double> BreakModifiedZScoreBelow(double median, double medianAbsoluteDeviation, double bound)
            {
                return value.Start().BreakModifiedZScoreBelow(median, medianAbsoluteDeviation, bound);
            }

            public Flow<double> RequireModifiedZScoreBelow(double median, double medianAbsoluteDeviation, double bound)
            {
                return value.Start().RequireModifiedZScoreBelow(median, medianAbsoluteDeviation, bound);
            }

            public Flow<double> EnsureModifiedZScoreBelow(double median, double medianAbsoluteDeviation, double bound)
            {
                return value.Start().EnsureModifiedZScoreBelow(median, medianAbsoluteDeviation, bound);
            }
        }
    }
}
