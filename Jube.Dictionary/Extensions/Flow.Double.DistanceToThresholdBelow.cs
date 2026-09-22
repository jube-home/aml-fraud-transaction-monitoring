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
            public Flow<double> MatchDistanceToThresholdBelow(double threshold, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleDistanceToThresholdBelow(flow.Value, threshold, bound));
            }

            public Flow<double> RejectDistanceToThresholdBelow(double threshold, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleDistanceToThresholdBelow(flow.Value, threshold, bound));
            }

            public Flow<double> BreakDistanceToThresholdBelow(double threshold, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleDistanceToThresholdBelow(flow.Value, threshold, bound));
            }

            public Flow<double> RequireDistanceToThresholdBelow(double threshold, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleDistanceToThresholdBelow(flow.Value, threshold, bound));
            }

            public Flow<double> EnsureDistanceToThresholdBelow(double threshold, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleDistanceToThresholdBelow(flow.Value, threshold, bound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchDistanceToThresholdBelow(double threshold, double bound)
            {
                return value.Start().MatchDistanceToThresholdBelow(threshold, bound);
            }

            public Flow<double> RejectDistanceToThresholdBelow(double threshold, double bound)
            {
                return value.Start().RejectDistanceToThresholdBelow(threshold, bound);
            }

            public Flow<double> BreakDistanceToThresholdBelow(double threshold, double bound)
            {
                return value.Start().BreakDistanceToThresholdBelow(threshold, bound);
            }

            public Flow<double> RequireDistanceToThresholdBelow(double threshold, double bound)
            {
                return value.Start().RequireDistanceToThresholdBelow(threshold, bound);
            }

            public Flow<double> EnsureDistanceToThresholdBelow(double threshold, double bound)
            {
                return value.Start().EnsureDistanceToThresholdBelow(threshold, bound);
            }
        }
    }
}
